using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.SpatialManipulation;

/// <summary>
/// A subclass of the MRTK Dialog component that disables automatic following
/// and button-based dismissal.
/// </summary>
public class StaticDialog : Dialog
{
    protected override void Awake()
    {
        // Call the base Awake method to ensure all base functionality is initialized correctly.
        base.Awake();

        // Immediately find and disable the SolverHandler and Follow components to prevent the dialog from following the user.
        StopFollowing();
    }

    /// <summary>
    /// Overrides the base method to prevent the dialog from automatically
    /// dismissing after a button is pressed.
    /// </summary>
    public override void Dismiss()
    {
        // Intentionally overriding this to do nothing.
    }

    /// <summary>
    /// A public method to be called from a button or another script
    /// to manually dismiss the dialog.
    /// </summary>
    public void ManuallyDismiss()
    {
        // Calls the base Dismiss method to safely hide the dialog and run all necessary cleanup.
        base.Dismiss();
    }

    /// <summary>
    /// Disables the SolverHandler and Follow components to prevent the dialog
    /// from following the user's head.
    /// </summary>
    private void StopFollowing()
    {
        // Find the solver handler on this GameObject.
        var solverHandler = GetComponent<SolverHandler>();
        if (solverHandler != null)
        {
            solverHandler.enabled = false;
        }

        // Find the follow component on this GameObject.
        var follow = GetComponent<Follow>();
        if (follow != null)
        {
            follow.enabled = false;
        }
    }
}
