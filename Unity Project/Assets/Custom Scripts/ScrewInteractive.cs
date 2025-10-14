using UnityEngine;
using MixedReality.Toolkit;

public class ScrewInteractive : MonoBehaviour
{

    private StatefulInteractable interactable; 
    public AssemblyManager assemblyManager; // Reference to the AssemblyCheck manager for UI feedback

    public GameObject screwGuide;

    private Vector3 initialLocalPosition; // Stores the screw's starting position
    private bool isInserted = false; // Tracks if the screw is currently "inserted"

    [Tooltip("The amount to shift the screw inwards when inserted.")]
    public Vector3 zShift = new Vector3(0, 0, 0.1f); // The amount to shift the screw inwards when inserted

    void Awake()
    {
        // Get the StatefulInteractable component attached to this GameObject
        interactable = GetComponent<StatefulInteractable>();
        if (interactable == null)
        {
            Debug.LogError($"ScrewInteractive: StatefulInteractable component missing on {gameObject.name}. Disabling script.");
            enabled = false;
            return;
        }
        initialLocalPosition = transform.localPosition;

        // Ensure the AssemblyManager is assigned in the inspector or find it in the scene
        if (assemblyManager == null)
        {   
            assemblyManager = FindFirstObjectByType<AssemblyManager>(); // Try to find the AssemblyManager in the scene if not assigned in the inspector
            if (assemblyManager == null)
            {
                Debug.LogError("ScrewInteractive: AssemblyManager not found in the scene. Please assign it in the inspector or ensure it's present.");
                enabled = false; 
            }
        }

    }

    void OnEnable()
    {   
        assemblyManager.UpdateScrewAssemblyStatus(gameObject.name, isInserted); // Update the assembly manager with the initial state of the screw
        // Subscribe to the OnClicked event from the StatefulInteractable.
        // This event triggers when a user performs a "select" action on the screw.
        interactable.OnClicked.AddListener(HandleScrewClicked);
    }

    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        interactable.OnClicked.RemoveListener(HandleScrewClicked);
    }

    /// <summary>
    /// This method is called by the StatefulInteractable when the screw is 'clicked'.
    /// </summary>
    private void HandleScrewClicked()
    {
        Debug.Log($"Screw {gameObject.name} clicked.");

        ToggleScrewPosition();  // Put screw in correct position

    }

    /// <summary>
    /// Toggles the screw's visual position between "inserted" and "removed" states.
    /// </summary>
    private void ToggleScrewPosition()
    {
        if (!isInserted)
        {
            // Move the screw inwards.
            transform.localPosition = initialLocalPosition + zShift;
            isInserted = true;
            Debug.Log($"Screw {gameObject.name} inserted.");

            // Turn Screw Guide off
            screwGuide.SetActive(false);
        }
        else
        {
            // Move the screw back to its initial, "removed" position.
            transform.localPosition = initialLocalPosition;
            isInserted = false;
            Debug.Log($"Screw {gameObject.name} removed.");

            // Turn Screw Guide on again
            screwGuide.SetActive(true);
        }

        // Inform the DoorAssemblyCheck manager about the screw's new state.
        // This allows the manager to update the UI feedback (e.g., "X out of Y screws inserted").
        if (assemblyManager != null)
        {
            assemblyManager.UpdateScrewAssemblyStatus(gameObject.name, isInserted);
        }
    }

    /// <summary>
    /// Method to check if screw is currently inserted.
    /// </summary>
    public bool IsScrewInserted()
    {
        return isInserted;
    }

}
