using UnityEngine;
using UnityEngine.SceneManagement;

public class ModeManager : MonoBehaviour
{
    public static ModeManager Instance { get; private set; }
    public enum TrainingMode
    {
        None, // Default or unselected state
        VirtualTraining,
        MixedTraining,
        FullTraining
    }

    public TrainingMode CurrentAssemblyMode = TrainingMode.None;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    private void SetModeAndLoadMainScene(TrainingMode modeToLoad) // Changed parameter type to enum
    {
        // First, set the global mode variable
        CurrentAssemblyMode = modeToLoad;
        Debug.Log($"ModeManager: Mode set to: {CurrentAssemblyMode}");

        // Then, load the main scene
        if (CurrentAssemblyMode != TrainingMode.None)
        {
            SceneManager.LoadScene("MainScene"); 
        }
        else
        {
            Debug.LogError("ModeManager: No valid mode selected. Cannot load scene.");
        }
    }
    public void OnVirtualTrainingButtonClicked()
    {
        SetModeAndLoadMainScene(ModeManager.TrainingMode.VirtualTraining);
    }
    public void OnMixedTrainingButtonClicked()
    {
        SetModeAndLoadMainScene(ModeManager.TrainingMode.MixedTraining);
    }
    public void OnFullTrainingButtonClicked()
    {
        SetModeAndLoadMainScene(ModeManager.TrainingMode.FullTraining);
    }
}