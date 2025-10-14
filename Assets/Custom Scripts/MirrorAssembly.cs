using UnityEngine;
using System.Collections;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.SpatialManipulation;

public class AssemblyCheck : MonoBehaviour
{

    public AssemblyManager assemblyManager;
    public GameObject placeObject; // The guide object to compare against
    public GameObject mirrorDialogObject; // Dialog to wait for user input that they understand the assembly instructions 
    public ObjectManipulator objectManipulator; // ObjectManipulator component for enabling/disabling manipulation

    private TextDisplay textDisplay; // TextDisplay component for showing messages
    private StaticDialog mirrorDialog;  // Subclass of Dialog component that doesn't follow user

    public float placeAnimationOffset = 0.1f; // Offset for the placement animation

    private Coroutine mirrorPlaceLoopCoroutine;

    // --- Tolerance settings for accuracy ---
    public float positionTolerance = 0.10f; // Meters (e.g., 5 cm)
    public float rotationToleranceDegrees = 7.0f; // Degrees

    private bool prevPositionCorrect = false, prevRotationCorrect = false;
    private bool positionCorrect = false, rotationCorrect = false;

    private Vector3 endPos;
    
    private void Awake()
    {
        // Get the TextDisplay component from any child GameObject
        textDisplay = placeObject.GetComponentInChildren<TextDisplay>();

        // Always good to check if it was found
        if (textDisplay == null)
        {
            Debug.LogError($"ParentScript: TextDisplay component not found on any child of {gameObject.name}. Please ensure it's attached to a child GameObject.");
            enabled = false;
            return;
        }

        mirrorDialog = mirrorDialogObject.GetComponent<StaticDialog>();

        if (mirrorDialog == null)
        {
            Debug.LogError($"ParentScript: StaticDialog component not found on any child of {gameObject.name}. Please ensure it's attached to a child GameObject.");
            enabled = false;
            return;
        }

        objectManipulator = GetComponent<ObjectManipulator>();

    }
    private void Start()
    {
        if (assemblyManager == null || placeObject == null)
        {
            Debug.LogError("AssemblyCheck: Missing required references.");
            enabled = false;
            return;
        }

        if (assemblyManager.assemblyMode != ModeManager.TrainingMode.FullTraining)
        {
            DeactivateDialog(null);

        }

    }

    private void Update()
    {
        if (assemblyManager.assemblyMode == ModeManager.TrainingMode.VirtualTraining || assemblyManager.assemblyMode == ModeManager.TrainingMode.MixedTraining)
        {
            CheckMirrorPlacement();
        }

    }
    public void CheckMirrorPlacement()
    {
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = transform.rotation;

        // --- Get Target Position/Rotation (from the guide object) 
        Vector3 targetLocalPosition = placeObject.transform.position;
        Quaternion targetLocalRotation = placeObject.transform.rotation;

        Debug.Log($"Current pos: {currentPosition} -> Target pos {targetLocalPosition}");

        // --- Compare Positions ---
        positionCorrect = Vector3.Distance(currentPosition, targetLocalPosition) <= positionTolerance;

        // --- Compare Rotations ---
        float angleDiff = Quaternion.Angle(currentRotation, targetLocalRotation);
        rotationCorrect = angleDiff <= rotationToleranceDegrees;

        if (prevPositionCorrect != positionCorrect || prevRotationCorrect != rotationCorrect)   // If there is a position change
        {
            // --- Debugging Information ---
            Debug.Log($"Current pos: {currentPosition} -> Target pos {targetLocalPosition}");
            Debug.Log($"Current rot: {currentRotation.eulerAngles} -> Target rot {targetLocalRotation.eulerAngles}");

            // --- Update the AssemblyManager ---
            assemblyManager.UpdateMirrorAssemblyStatus(positionCorrect && rotationCorrect);

            if (positionCorrect && rotationCorrect)
            {
                placeObject.SetActive(false); // Hide the guide object when assembly is correct

                if (assemblyManager.assemblyMode != ModeManager.TrainingMode.FullTraining)
                {
                    if (objectManipulator != null)
                    {
                        objectManipulator.enabled = false;
                    }

                    // Clip both position AND rotation in World Space
                    this.gameObject.transform.position = placeObject.transform.position;
                    this.gameObject.transform.rotation = placeObject.transform.rotation;
                }
            }
            else  // If assembly is incorrect
            {
                placeObject.gameObject.SetActive(true); // Show the guide object when assembly is incorrect
                string feedback = "";
                if (!positionCorrect) feedback += "Position Off ";
                if (!rotationCorrect) feedback += "Rotation Off ";

                Debug.Log($"MirrorAssembly: {feedback}");

                if (assemblyManager.assemblyMode != ModeManager.TrainingMode.FullTraining)
                {
                    textDisplay.SetPersistentText(feedback, Color.red); // Show error message

                }
                else // Full training mode
                {
                    textDisplay.ClearAllText(); // Clear persistent text
                    textDisplay.ShowTemporaryText(feedback, 2.0f, Color.red); // Show error message
                }
            }
            prevPositionCorrect = positionCorrect;
            prevRotationCorrect = rotationCorrect;

        }
    }
    private void StartMirrorDisplayLoop()
    {
        if (mirrorPlaceLoopCoroutine == null)
        {
            mirrorPlaceLoopCoroutine = StartCoroutine(MirrorDisplayCoroutine());
        }
    }
    private IEnumerator MirrorDisplayCoroutine()
    {
        if (placeObject == null)
        {
            Debug.LogWarning("Place Object is not assigned. Please assign it in the Inspector.");
            yield break;
        }

        // Consistent animation duration and loop times.
        float animationDuration = 1f;
        float visibleTime = 3f;
        float hiddenTime = 1f;

        endPos = placeObject.transform.localPosition;

        while (true) 
        {
            placeObject.SetActive(true);

            Vector3 startPos = endPos + (Vector3.forward * placeAnimationOffset);

            placeObject.transform.localPosition = startPos;

            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                placeObject.transform.localPosition = Vector3.Lerp(startPos, endPos, elapsed / animationDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            placeObject.transform.localPosition = endPos;

            yield return new WaitForSeconds(visibleTime - animationDuration);

            placeObject.SetActive(false);

            yield return new WaitForSeconds(hiddenTime);
        }
    }

    private void StopMirrorDisplayLoop()
    {
        if (mirrorPlaceLoopCoroutine != null)
        {
            // Put place object back to original position and hide it

            placeObject.SetActive(false); // Hide the guide object when stopping the loop
            StopCoroutine(mirrorPlaceLoopCoroutine);
            placeObject.transform.localPosition = endPos;
            mirrorPlaceLoopCoroutine = null;
        }
    }

    public void ShowMirrorAssemblyInstruction()
    {
        StartMirrorDisplayLoop();
        MirrorAnimationDialog();
    }

    public void HideMirrorAssemblyInstruction()
    {
        StopMirrorDisplayLoop();
    }

    // Dialog to dismiss animation
    private void MirrorAnimationDialog()
    {   
        ActivateDialog();

        mirrorDialog.SetHeader("Confirm if you understand the assembly instruction");
        mirrorDialog.SetNeutral("Understood", MirrorAssembledDialog);

        mirrorDialog.Show();

        Debug.Log("Mirror Assembly: Mirror animation dialog activated.");
    }

    // Dialog to confirm mirror placement
    private void MirrorAssembledDialog(DialogButtonEventArgs args)
    {
        StopMirrorDisplayLoop();
        ActivateDialog();   
        mirrorDialog.SetHeader("Confirm when the mirror is assembled correctly");

        mirrorDialog.SetNeutral("Done", (args) => {

            assemblyManager.ConfirmMirrorPlacement();
            DeactivateDialog(args);
                       
        });

        mirrorDialog.Show();

        Debug.Log("Mirror Assembly: Mirror assemble confirmation dialog activated.");

    }

    private void DeactivateDialog(DialogButtonEventArgs args)
    {
        Debug.Log("Deactivating mirror dialog...");

        if (mirrorDialogObject != null)
        {
            mirrorDialogObject.SetActive(false);
        }

    }

    private void ActivateDialog()
    {
        Debug.Log("Activating mirror dialog...");

        if (mirrorDialogObject != null)
        {
            mirrorDialogObject.SetActive(true);
        }
    }


}
