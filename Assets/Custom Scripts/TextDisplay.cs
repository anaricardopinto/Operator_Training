using UnityEngine;
using TMPro;
using System.Collections;

public class TextDisplay : MonoBehaviour
{
    public enum TextComponentType
    {
        TextMeshPro_3D,
        TextMeshPro_UGUI
    }

    public TextComponentType selectedTextType;

    private TextMeshPro _displayTextMesh3D;
    private TextMeshProUGUI _displayTextMeshUGUI;

    private TMP_Text _activeTextMesh;
    private Color _initialTextColor; // Stores the color of the text component when the script starts

    private Coroutine currentDisplayCoroutine;

    void Awake()
    {
        if (selectedTextType == TextComponentType.TextMeshPro_3D)
        {
            _displayTextMesh3D = GetComponent<TextMeshPro>();
            _activeTextMesh = _displayTextMesh3D;
        }
        else
        {
            _displayTextMeshUGUI = GetComponent<TextMeshProUGUI>();
            _activeTextMesh = _displayTextMeshUGUI;
        }

        if (_activeTextMesh == null)
        {
            Debug.LogError($"TemporaryTextDisplay: Expected a {selectedTextType} component on this GameObject, but none was found. Please ensure the correct TextMeshPro component is attached.");
            enabled = false;
        }
        else
        {
            _initialTextColor = _activeTextMesh.color; // Store the current color of the text
            _activeTextMesh.text = ""; // Clear text initially
        }
    }

    /// <summary>
    /// Sets the text to a message for a specified duration, then clears it.
    /// If another message is already being displayed, the previous one is cancelled.
    /// </summary>
    /// <param name="message">The text message to display.</param>
    /// <param name="duration">How long to display the message in seconds.</param>
    /// <param name="color">Optional: The color of the text. If null, uses the text's initial color.</param>
    public void ShowTemporaryText(string message, float duration, Color? color = null)
    {
        if (_activeTextMesh == null) return;

        // Stop any existing coroutine to prevent overlapping messages
        if (currentDisplayCoroutine != null)
        {
            StopCoroutine(currentDisplayCoroutine);
        }

        // Apply the specified color, or fall back to the initial color if none provided
        _activeTextMesh.color = color ?? _initialTextColor;

        // Start the new coroutine
        currentDisplayCoroutine = StartCoroutine(DisplayAndClearText(message, duration));
    }

    /// <summary>
    /// Coroutine to display text, wait, and then clear it.
    /// </summary>
    /// <param name="message">The text message to display.</param>
    /// <param name="duration">How long to display the message in seconds.</param>
    private IEnumerator DisplayAndClearText(string message, float duration)
    {
        // Set the text
        _activeTextMesh.text = message;
        _activeTextMesh.gameObject.SetActive(true); // Ensure the text GameObject is active

        // Wait for the specified duration
        yield return new WaitForSeconds(duration);

        // Clear the text and reset color
        _activeTextMesh.text = "";
        _activeTextMesh.color = _initialTextColor; // Revert to the original color
        currentDisplayCoroutine = null; // Mark that the coroutine has finished
    }

    /// <summary>
    /// Immediately clears any temporary feedback text and stops its timer.
    /// Resets text color to its initial state.
    /// </summary>
    public void ClearTextImmediately()
    {
        if (currentDisplayCoroutine != null)
        {
            StopCoroutine(currentDisplayCoroutine);
            currentDisplayCoroutine = null;
        }
        if (_activeTextMesh != null)
        {
            _activeTextMesh.text = "";
            _activeTextMesh.color = _initialTextColor; // Reset color
        }
    }

    /// <summary>
    /// Sets the text display to a persistent message.
    /// This text will remain until explicitly changed by another call to this or a temporary display method.
    /// Any ongoing temporary text display will be stopped.
    /// </summary>
    /// <param name="message">The text message to display persistently.</param>
    /// <param name="color">Optional: The color of the text. If null, uses the text's initial color.</param>
    public void SetPersistentText(string message, Color? color = null)
    {
        if (_activeTextMesh == null) return; // Exit if no active text component

        // Stop any ongoing temporary display coroutine
        if (currentDisplayCoroutine != null)
        {
            StopCoroutine(currentDisplayCoroutine);
            currentDisplayCoroutine = null; // Clear the coroutine reference
        }

        // Set the text and apply color
        _activeTextMesh.text = message;
        _activeTextMesh.color = color ?? _initialTextColor; // Apply the specified color, or initial color
        _activeTextMesh.gameObject.SetActive(true); // Ensure the text GameObject is active
    }

    /// <summary>
    /// Clears any currently displayed text (temporary or persistent) and resets its color to initial.
    /// This will also stop any ongoing temporary text display.
    /// </summary>
    public void ClearAllText()
    {
        ClearTextImmediately(); 
    }
}