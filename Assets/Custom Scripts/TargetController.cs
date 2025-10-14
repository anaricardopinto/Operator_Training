using UnityEngine;
using Vuforia;

public class ModelTargetController : MonoBehaviour
{
    public GameObject doorTarget;   // Assign in Inspector
    public GameObject mirrorTarget; // Assign in Inspector

    private void Awake()
    {
        var mode = ModeManager.Instance?.CurrentAssemblyMode ?? ModeManager.TrainingMode.None;
        Debug.Log($"ModelTargetController: Current mode is {mode}");

        if (mode == ModeManager.TrainingMode.VirtualTraining)
        {
            // Disable Vuforia entirely
            VuforiaBehaviour.Instance.enabled = false;
            if (doorTarget) doorTarget.SetActive(false);
            if (mirrorTarget) mirrorTarget.SetActive(false);
        }
        else
        {
            // Hook into the Vuforia started event
            VuforiaApplication.Instance.OnVuforiaStarted += () =>
            {
                Debug.Log("Vuforia started, configuring targets.");

                if (doorTarget) doorTarget.SetActive(false);
                if (mirrorTarget) mirrorTarget.SetActive(false);

                if (mode == ModeManager.TrainingMode.FullTraining)
                {
                    if (doorTarget) doorTarget.SetActive(true);
                    if (mirrorTarget) mirrorTarget.SetActive(true);
                }
                else if (mode == ModeManager.TrainingMode.MixedTraining)
                {
                    if (doorTarget) doorTarget.SetActive(true); // You can change this logic depending on the specific goal
                }
            };
        }
    }
}

