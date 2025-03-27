using UnityEngine;

/// <summary>
/// Handles the rotation of a parent character based on the rotation changes of this GameObject.
/// Applies delta rotations calculated each frame to ensure smooth and continuous character movement.
/// </summary>
public class RotationHandler : MonoBehaviour
{
    [Header("Character Configuration")]
    [Tooltip("The parent character that should be rotated.")]
    /// <summary>
    /// The parent character Transform that will be rotated.
    /// </summary>
    public Transform characterTransform;

    [Header("Rotation Tracking")]
    /// <summary>
    /// Stores the last local rotation of this GameObject to calculate delta rotations.
    /// </summary>
    private Quaternion lastLocalRotation;

    /// <summary>
    /// Initializes the RotationHandler by validating the characterTransform and setting the initial rotation.
    /// </summary>
    void Start()
    {
        if (characterTransform == null)
        {
            Debug.LogError("CharacterTransform is not assigned in RotationHandler.");
            enabled = false;
            return;
        }

        lastLocalRotation = transform.localRotation;
        Debug.Log("RotationHandler initialized.");
    }

    /// <summary>
    /// Applies the delta rotation calculated since the last frame to the parent character.
    /// Resets the local rotation to identity to prepare for the next frame's rotation.
    /// </summary>
    void LateUpdate()
    {
        if (characterTransform == null)
            return;

        // Calculate the change in rotation since the last frame (delta rotation)
        Quaternion currentLocalRotation = transform.localRotation;
        Quaternion deltaRotation = currentLocalRotation * Quaternion.Inverse(lastLocalRotation);

        // Apply the delta rotation to the parent character's global rotation
        characterTransform.rotation = deltaRotation * characterTransform.rotation;

        // Reset the local rotation to identity for continuous rotation tracking
        transform.localRotation = Quaternion.identity;

        // Update the last local rotation to the current (now reset) rotation
        lastLocalRotation = transform.localRotation;
    }
}
