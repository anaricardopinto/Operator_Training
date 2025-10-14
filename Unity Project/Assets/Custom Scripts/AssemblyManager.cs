using UnityEngine;
using Vuforia;
using System.Collections.Generic; // Required for List<ScrewInteractive>
using MixedReality.Toolkit.SpatialManipulation; // Required for SceneManager

public class AssemblyManager : MonoBehaviour
{
    // -- Assembly Mode --
    public ModeManager.TrainingMode assemblyMode = ModeManager.TrainingMode.None; // The current training mode, set by the ModeManager

    // -- Model Tracking --

    private ModelTargetBehaviour doorModelTargetBehaviour;   // The ModelTargetBehaviour component for the door model target
    private ModelTargetBehaviour mirrorModelTargetBehaviour; // The ModelTargetBehaviour component for the mirror model target

    public bool isDoorTracked = false;  // Internal state to track if the Model Target is currently being tracked
    public bool isMirrorTracked = false; // Internal state to track if the mirror Model Target is currently being tracked

    // --- Internal State Management ---

    public enum AssemblyPhase
    {   
        StartUp,           // Initial state before any assembly begins, waits for door to be tracked
        MirrorAssembly,      // Waits for user to confirm mirror placement
        MirrorAssemblyCheck,        // Checks mirror placement
        ScrewAssembly,       // Placing screws
        Complete             // All assembly steps are finished.
    }

    public AssemblyPhase currentPhase = AssemblyPhase.StartUp; // The current phase of the assembly process, starting with Door Setup

    // --- Public Variables to Assign in Inspector ---

    public TextDisplay assemblyDisplay; // Text display for assembly instructions and feedback

    public GameObject screwsObject; // The GameObject that contains the screws and guides.
    public TextDisplay screwsDisplay;   // Text display for screw count and feedback

    public GameObject quarterGlassObject; // The quarter glass object 

    // --- Assembly Parts ---

    private List<ScrewInteractive> individualScrews = new List<ScrewInteractive>();
    private AssemblyCheck mirrorAssembly; // The script that manages the mirror assembly logic.

    // --- Internal State Variables ---
    private bool isMirrorPlacedCorrectly = false; // Tracks if the mirror is placed correctly

    private void Awake()
    {
        if (ModeManager.Instance != null)
        {
            // Ensure the assembly mode is set before proceeding
            assemblyMode = ModeManager.Instance.CurrentAssemblyMode;
        }

        // Get door ModelTargetBehaviour component directly from parent GameObject
        doorModelTargetBehaviour = GetComponentInParent<ModelTargetBehaviour>();

        //Get mirror ModelTargetBehaviour component directly from child GameObject
        mirrorModelTargetBehaviour = GetComponentInChildren<ModelTargetBehaviour>();

        if (doorModelTargetBehaviour == null || mirrorModelTargetBehaviour == null)
        {
            Debug.LogError("AssemblyManager: ModelTargetBehaviour not found in Full Training mode. Disabling script.", this);
            enabled = false;
            return;
        }

        // Find mirror assembly script and assign assembly manager
        mirrorAssembly = GetComponentInChildren<AssemblyCheck>();
        if (mirrorAssembly == null)
        {
            Debug.LogError("AssemblyManager: AssemblyCheck script not found in children. Please ensure it is attached to a child GameObject.", this);
            enabled = false;
            return;
        }
        mirrorAssembly.assemblyManager = this; // Give the mirror assembly script a reference to this manager    

        // Find all ScrewInteractive scripts in children of screwsObject
        if (screwsObject != null)
        {
            individualScrews.AddRange(screwsObject.GetComponentsInChildren<ScrewInteractive>());

            if (individualScrews.Count == 0)
            {
                Debug.LogWarning("AssemblyManager: No ScrewInteractive scripts found as children of screwsObject. Screw phase will not function.");
            }

            // Now, iterate through the found screws and give them a reference back to this manager.
            foreach (ScrewInteractive screw in individualScrews)
            {
                screw.assemblyManager = this; // Give each screw a reference back to this manager
            }

        }
        else
        {
            Debug.LogError("AssemblyManager: 'Screws Object' is not assigned. Please assign it in the Inspector.", this);
            enabled = false;
            return;
        }

        if (assemblyDisplay == null)
        {
            Debug.LogError("AssemblyManager: 'Assembly Display' (TextMeshPro) is not assigned. Please assign it in the Inspector.", this);
            enabled = false;
            return;
        }

        if (screwsDisplay == null)
        {
            Debug.LogError("AssemblyManager: 'Screws Display' (TextMeshPro for screw count) is not assigned. Please assign it in the Inspector.", this);
            enabled = false;
            return;
        }

        if (individualScrews.Count == 0)
        {
            Debug.LogError("AssemblyManager: 'Individual Screws' list is empty. Please assign screw GameObjects to the list in the Inspector.", this);
            enabled = false;
            return;
        }

    }

    private void Start()
    {
       switch (assemblyMode)
        {
            case ModeManager.TrainingMode.VirtualTraining:

                // Disable Vuforia entirely in VirtualTraining mode
                VuforiaBehaviour.Instance.enabled = false;

                if (VuforiaApplication.Instance.IsRunning)
                    VuforiaApplication.Instance.Deinit(); // fully deinitializes Vuforia

                isDoorTracked = true; // Assume door is tracked in VirtualOnly mode
                isMirrorTracked = true; // Assume mirror is tracked in VirtualOnly mode

                // Turn off door tracking
                StopObjectTracking(doorModelTargetBehaviour); 

                // Turn off mirror tracking
                StopObjectTracking(mirrorModelTargetBehaviour);

                // Make door and mirror visible
                mirrorAssembly.gameObject.GetComponent<Renderer>().enabled = true;
                this.gameObject.GetComponent<Renderer>().enabled = true;

                break;

            case ModeManager.TrainingMode.MixedTraining:

                isDoorTracked = false; // Initially not tracked in MixedTraining mode
                isMirrorTracked = true; // Assume mirror is tracked in MixedTraining mode   

                // Enable door Model Target tracking
                TurnModelTargetOnandOff(doorModelTargetBehaviour, true);

                // Turn off mirror tracking
                StopObjectTracking(mirrorModelTargetBehaviour);

                // Disable quarter glass in MixedTraining mode
                quarterGlassObject.SetActive(false); 

                break;

            case ModeManager.TrainingMode.FullTraining:

                isDoorTracked = false; // Initially not tracked in FullTraining mode
                isMirrorTracked = false; // Initially not tracked in FullTraining mode

                // Enable door Model Target tracking
                TurnModelTargetOnandOff(doorModelTargetBehaviour, true);

                // Disable mirror Model Target tracking for now and stop mirror manipulation
                TurnModelTargetOnandOff(mirrorModelTargetBehaviour, false); // Start with mirror model target disabled until user confirms placement
                mirrorAssembly.gameObject.GetComponent<ObjectManipulator>().enabled = false; // Disable mirror manipulation script

                quarterGlassObject.SetActive(false); // Disable quarter glass in FullTraining mode

                break;
       }

        SetObjectState(screwsObject, false); // Hide screws and guides at start
    }

    void Update()
    {
        Transform screw1 = individualScrews[0].transform;
        Transform screw2 = individualScrews[1].transform;
        float distance = Vector3.Distance(screw1.position, screw2.position);
        Debug.Log($"Distance between screw 1 and 2: {distance:F4} meters");

        // ------- Visibility management ----------

        if (assemblyMode == ModeManager.TrainingMode.MixedTraining)
        {
            // Continuously make door transparent in MixedTraining mode
            MakeInvisible(this.gameObject); // Make the door assembly hologram transparent
        }
        if (assemblyMode == ModeManager.TrainingMode.FullTraining)
        {
            // Continuously make door and mirror transparent in FullTraining mode
            MakeInvisible(this.gameObject); // Door 
            MakeInvisible(mirrorAssembly.gameObject); // Mirror
        }

        // --- Core Assembly State Machine ---
        switch (currentPhase)
        {   

            case AssemblyPhase.StartUp:
                if (isDoorTracked)
                {
                    currentPhase = AssemblyPhase.MirrorAssembly; // Transition to mirror assembly phase
                    if (assemblyMode == ModeManager.TrainingMode.FullTraining)
                        mirrorAssembly.ShowMirrorAssemblyInstruction(); 
                }
                break;

            case AssemblyPhase.MirrorAssembly:


                if (assemblyMode != ModeManager.TrainingMode.FullTraining)
                {
                    currentPhase = AssemblyPhase.MirrorAssemblyCheck; // Transition to Mirror Detection phase
                }

                break;

            case AssemblyPhase.MirrorAssemblyCheck:

                if (isMirrorTracked)
                {
                    if (assemblyMode == ModeManager.TrainingMode.FullTraining)
                    {
                        mirrorAssembly.CheckMirrorPlacement();
                    }

                    if (isMirrorPlacedCorrectly)
                    {
                        currentPhase = AssemblyPhase.ScrewAssembly; // Transition to screw assembly phase
                        
                        if (assemblyMode == ModeManager.TrainingMode.FullTraining)
                        {
                            StopObjectTracking(mirrorModelTargetBehaviour); // Freeze mirror in place
                            TurnModelTargetOnandOff(doorModelTargetBehaviour, true); // Enable door model target to resume tracking
                        }

                        SetObjectState(screwsObject, true); // Show screws and guides
                        assemblyDisplay.ShowTemporaryText("Mirror placed correctly", 3.0f, Color.green);
                    }
                    else if (assemblyMode == ModeManager.TrainingMode.FullTraining) 
                    {
                        currentPhase = AssemblyPhase.MirrorAssembly; // Retry mirror placement

                        TurnModelTargetOnandOff(mirrorModelTargetBehaviour, false); // Disable mirror model target to pause tracking
                        TurnModelTargetOnandOff(doorModelTargetBehaviour, true); // Enable door model target to resume tracking
                        assemblyDisplay.ShowTemporaryText("Place mirror correctly", 2.0f, Color.red);

                        mirrorAssembly.ShowMirrorAssemblyInstruction();     // Show instructions again
                        
                    }
                }

                break;

            case AssemblyPhase.ScrewAssembly:

                //mirrorAssembly.gameObject.GetComponent<ObjectManipulator>().enabled = false; // Disable mirror manipulation script to prevent further movement

                if (!isMirrorPlacedCorrectly)
                {   
                    currentPhase = AssemblyPhase.MirrorAssembly; // Go back to mirror assembly phase if mirror is moved
                }

                break;

            case AssemblyPhase.Complete:

                assemblyDisplay.SetPersistentText("Assembly Complete", Color.green);

                if (!isMirrorPlacedCorrectly)
                {
                    currentPhase = AssemblyPhase.MirrorAssembly; // Go back to mirror assembly phase if mirror is moved
                }
                if (individualScrews.Exists(screw => !screw.IsScrewInserted()))
                {
                    currentPhase = AssemblyPhase.ScrewAssembly; // Go back to screw assembly phase if any screw is removed
                }

                break;
        }
    }

    // -- Methods to update assembly status --
    /// <summary>
    /// Method for MirrorAssembly script to call to update the mirror's position and rotation status.
    /// </summary> 
    public void UpdateMirrorAssemblyStatus(bool isAssemblyCorrect)
    {

        Debug.Log($"AssemblyManager: Mirror assembly status updated. Is assembly correct? {isAssemblyCorrect}");
        isMirrorPlacedCorrectly = isAssemblyCorrect;
    }
    public void ConfirmMirrorPlacement()
    {
        if (currentPhase == AssemblyPhase.MirrorAssembly)
        {
            Debug.Log("AssemblyManager: User confirmed mirror placement.");

            TurnModelTargetOnandOff(doorModelTargetBehaviour, false); // Disable door model target to stop tracking
            TurnModelTargetOnandOff(mirrorModelTargetBehaviour, true); // Enable mirror model target to start tracking

            // Transition to mirror check phase
            currentPhase = AssemblyPhase.MirrorAssemblyCheck;
        }
    }

    /// <summary>
    /// This method is called by each ScrewInteractive script whenever a screw's inserted state changes.
    /// It updates the total count of inserted screws and refreshes the UI feedback.
    /// </summary>
    /// <param name="screwName">The name of the screw that changed state (for logging).</param>
    /// <param name="inserted">True if the screw was inserted, false if removed.</param>

    public void UpdateScrewAssemblyStatus(string screwName, bool inserted)
    {
        int insertedScrewCount = 0;
        foreach (ScrewInteractive screw in individualScrews)
        {
            if (screw.IsScrewInserted())
            {
                insertedScrewCount++;
            }
        }

        if (currentPhase == AssemblyPhase.ScrewAssembly)
        {   

            if (insertedScrewCount == 0) // If no screws are inserted and we are in Screw phase
            {   
                screwsDisplay.SetPersistentText("Insert screws", Color.red); // Show red feedback
            }
            else if (insertedScrewCount < individualScrews.Count)    // If not all screws are inserted
            {   
                screwsDisplay.SetPersistentText($"{insertedScrewCount} / {individualScrews.Count} screws inserted", Color.yellow); 

            }
            else  // If all screws are inserted
            {   
                screwsDisplay.ShowTemporaryText("All screws inserted", 2.0f, Color.green); // Show success message

                // Transition to complete phase
                currentPhase = AssemblyPhase.Complete;
                Debug.Log("Assembly Complete");
            }
        }
        else 
        {
            if (insertedScrewCount < individualScrews.Count && currentPhase == AssemblyPhase.Complete)
            {
                currentPhase = AssemblyPhase.ScrewAssembly;
            }
        }
    }

    /// <summary>
    /// Makes the GameObject invisible by disabling its Renderer.
    /// This is used to make objects transparent or hidden in the scene without deactivating them.
    /// </summary>
    private void MakeInvisible(GameObject gameObject)
    {   
        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;   // Disable rendering to make it invisible
        //Debug.Log($" {gameObject.name} turned transparent");

    }
    private void SetObjectState(GameObject targetObject, bool isActive)
    {
        if (targetObject != null)
        {
            targetObject.SetActive(isActive);
        }
    }

    private void TurnModelTargetOnandOff(ModelTargetBehaviour modelTarget, bool turnOn)
    {
        if (modelTarget != null)
        {
            Debug.Log($"AssemblyManager: ModelTargetBehaviour for {modelTarget.gameObject.name} will be turned {(turnOn ? "on" : "off")}");
            modelTarget.enabled = turnOn;
            
            if (turnOn)
            {   
                if (modelTarget == doorModelTargetBehaviour)
                    assemblyDisplay.SetPersistentText("Look at door", Color.red); // Ask user to look at door
                if (modelTarget == mirrorModelTargetBehaviour)
                    assemblyDisplay.SetPersistentText("Look at mirror", Color.red); // Ask user to look at mirror
            }
        }
        else
        {
            Debug.LogWarning("AssemblyManager: Attempted to turn on/off a null ModelTargetBehaviour reference.");
        }
    }

    private void StopObjectTracking(ModelTargetBehaviour modelTargetBehaviour)
    {
        if (modelTargetBehaviour != null)
        {
            if (modelTargetBehaviour == mirrorModelTargetBehaviour)
            {
                mirrorAssembly.transform.SetParent(this.gameObject.transform, true); // Unparent the mirror representation to "freeze" it in world space.
                SetObjectState(modelTargetBehaviour.gameObject, false);  // Set mirror model target as inactive to prevent further tracking
            }
            else if (modelTargetBehaviour == doorModelTargetBehaviour)
            {
                // Freeze door in place by unparenting it from any model target and disabling tracking
                this.gameObject.transform.SetParent(null, true); // Set door assembly as a child of this GameObject
                SetObjectState(modelTargetBehaviour.gameObject, false);  //  Disable door model target to prevent further tracking
            }
            else
            {
                Debug.LogWarning("AssemblyManager: Attempted to stop an unrecognized Model Target tracking.");
            }
        }
        
    }
    public void SetDoorAsTracked()
    {  
        isDoorTracked = true;
        assemblyDisplay.ClearAllText(); // Clear any previous messages

        // Parent the door assembly to the model target to enable tracking
        this.gameObject.transform.SetParent(doorModelTargetBehaviour.transform, true); // Set this GameObject as a child of the door model target

        Debug.Log("AssemblyManager: Door is now tracked.");
    }   

    public void SetDoorAsUntracked()
    {   
        isDoorTracked = false;
        assemblyDisplay.SetPersistentText("Look at door", Color.red); // Show error message

        // Freeze door in place by unparenting it from any model target
        this.gameObject.transform.SetParent(null, true); // Set door assembly as a child of this GameObject

        Debug.Log("AssemblyManager: Door is now untracked.");
    }

    public void SetMirrorAsTracked()
    {
        isMirrorTracked = true;
        assemblyDisplay.ClearAllText(); // Clear any previous messages

        Debug.Log("AssemblyManager: Mirror is now tracked.");
    }

    public void SetMirrorAsUntracked()
    {
        isMirrorTracked = false;
        if (currentPhase == AssemblyPhase.MirrorAssemblyCheck)
        {
            assemblyDisplay.SetPersistentText("Look at mirror", Color.red); // Show error message
        }

        Debug.Log("AssemblyManager: Mirror is now untracked.");
    }

}