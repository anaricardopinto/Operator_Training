using UnityEngine;
using System.Diagnostics;

public class VSLogRedirect : MonoBehaviour
{
    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        System.Diagnostics.Debug.WriteLine($"[{type}] {logString}");
    }
}
