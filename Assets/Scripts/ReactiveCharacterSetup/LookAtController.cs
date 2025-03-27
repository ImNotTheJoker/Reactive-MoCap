using System.Collections;
using UnityEngine;

/// <summary>
/// Enum representing the different types of LookAt transitions.
/// Must be public to be accessible by public methods.
/// </summary>
public enum LookAtTransitionType
{
    FirstLook,        // Initial look at a target
    ReturnToDefault,  // Return to the default position
    SwitchTarget,     // Switch between targets
    FollowTarget      // Continuously follow the moving target
}

[RequireComponent(typeof(Animator))]
public class LookAtController : MonoBehaviour
{
    [Header("LookAt Settings")]
    /// <summary>
    /// Weight for the head during LookAt.
    /// </summary>
    public float headWeight = 1.0f;

    /// <summary>
    /// Weight for the body during LookAt.
    /// </summary>
    public float bodyWeight = 0.2f;

    /// <summary>
    /// Weight for the eyes during LookAt.
    /// </summary>
    public float eyesWeight = 1.0f;

    /// <summary>
    /// Clamping weight to limit the LookAt influence.
    /// </summary>
    public float clampWeight = 0.5f;

    [Header("Transition Times")]
    /// <summary>
    /// Smooth transition time for the first look.
    /// </summary>
    public float smoothTimeFirstLook = 0.4f;

    /// <summary>
    /// Smooth transition time for returning to default.
    /// </summary>
    public float smoothTimeReturn = 0.7f;

    /// <summary>
    /// Smooth transition time for switching between targets.
    /// </summary>
    public float smoothTimeSwitch = 0.3f;

    /// <summary>
    /// Smooth transition time for following the target.
    /// </summary>
    public float smoothTimeFollow = 0.1f;

    /// <summary>
    /// Reference to the Animator component.
    /// </summary>
    private Animator animator;

    /// <summary>
    /// Virtual position the character is looking at.
    /// </summary>
    private Vector3 virtualLookAtPosition;

    /// <summary>
    /// Velocity vector for smooth damping.
    /// </summary>
    private Vector3 velocity = Vector3.zero;

    /// <summary>
    /// Currently active LookAt target.
    /// </summary>
    private Transform currentLookAtTarget;

    /// <summary>
    /// Current weight of the LookAt influence.
    /// </summary>
    private float currentLookAtWeight = 0f;

    /// <summary>
    /// Target weight for the LookAt influence.
    /// </summary>
    private float targetLookAtWeight = 0f;

    /// <summary>
    /// Default LookAt target position (child transform).
    /// </summary>
    private Transform defaultLookAtTarget;

    /// <summary>
    /// Current type of LookAt transition.
    /// </summary>
    private LookAtTransitionType currentTransitionType = LookAtTransitionType.ReturnToDefault;

    /// <summary>
    /// Threshold to determine if the transition is complete.
    /// </summary>
    private float positionThreshold = 0.1f; // Reduced to 0.1f for more accurate detection

    /// <summary>
    /// Flag to prevent multiple transition completions within the same frame.
    /// </summary>
    private bool hasTransitionedThisFrame = false;

    /// <summary>
    /// Initializes the LookAtController by setting up the Animator and default LookAt target.
    /// </summary>
    void Start()
    {
        animator = GetComponent<Animator>();

        // Create a child GameObject as the default LookAt target
        GameObject defaultTargetGO = new GameObject("DefaultLookAtTarget");
        defaultTargetGO.transform.SetParent(transform);
        defaultTargetGO.transform.localPosition = transform.forward * 5f; // 5 units forward
        defaultLookAtTarget = defaultTargetGO.transform;

        // Initialize the virtual LookAt position
        virtualLookAtPosition = defaultLookAtTarget.position;
        currentLookAtWeight = 0f; // Initially not looking at anything
    }

    /// <summary>
    /// Updates the default LookAt target position based on the character's forward direction.
    /// </summary>
    void Update()
    {
        // Update the Default LookAt Position based on the current forward direction
        defaultLookAtTarget.position = transform.position + transform.forward * 5f;
    }

    /// <summary>
    /// Handles the IK for LookAt each frame.
    /// </summary>
    /// <param name="layerIndex">The layer index for IK.</param>
    private void OnAnimatorIK(int layerIndex)
    {
        hasTransitionedThisFrame = false; // Reset the flag at the start of each IK calculation

        Vector3 targetPosition;

        if (currentLookAtTarget != null)
        {
            // Determine the target position based on the head of the target object
            Transform headTransform = FindHeadTransformRecursive(currentLookAtTarget);
            targetPosition = headTransform != null ? headTransform.position : currentLookAtTarget.position;

            // Select the appropriate smooth time based on the transition type
            float smoothTime = GetSmoothTime(currentTransitionType);

            // Smoothly move the virtual LookAt position towards the target position
            virtualLookAtPosition = Vector3.SmoothDamp(virtualLookAtPosition, targetPosition, ref velocity, smoothTime);

            // Smoothly adjust the LookAt weight towards the target weight
            currentLookAtWeight = Mathf.MoveTowards(currentLookAtWeight, targetLookAtWeight, Time.deltaTime / smoothTime);

            // Apply the LookAt position and weights to the Animator
            animator.SetLookAtPosition(virtualLookAtPosition);
            animator.SetLookAtWeight(currentLookAtWeight, bodyWeight, headWeight, eyesWeight, clampWeight);

            // Check if the transition is complete
            if (!hasTransitionedThisFrame &&
                (currentTransitionType == LookAtTransitionType.FirstLook || currentTransitionType == LookAtTransitionType.SwitchTarget) &&
                Vector3.Distance(virtualLookAtPosition, targetPosition) < positionThreshold)
            {
                // Transition is complete, switch to following the target
                currentTransitionType = LookAtTransitionType.FollowTarget;
                hasTransitionedThisFrame = true;
            }
        }
        else
        {
            // No current target, return to the default LookAt position

            // Select the appropriate smooth time based on the transition type
            float smoothTime = GetSmoothTime(currentTransitionType);

            // Smoothly move the virtual LookAt position towards the default position
            virtualLookAtPosition = Vector3.SmoothDamp(virtualLookAtPosition, defaultLookAtTarget.position, ref velocity, smoothTime);

            // Smoothly adjust the LookAt weight towards zero
            currentLookAtWeight = Mathf.MoveTowards(currentLookAtWeight, 0f, Time.deltaTime / smoothTime);

            // Apply the LookAt position and weights to the Animator
            animator.SetLookAtPosition(virtualLookAtPosition);
            animator.SetLookAtWeight(currentLookAtWeight, bodyWeight, headWeight, eyesWeight, clampWeight);

            // Check if the transition back to default is complete
            if (!hasTransitionedThisFrame &&
                currentTransitionType == LookAtTransitionType.ReturnToDefault &&
                Vector3.Distance(virtualLookAtPosition, defaultLookAtTarget.position) < positionThreshold)
            {
                // Transition is complete
                hasTransitionedThisFrame = true;
            }
        }
    }

    /// <summary>
    /// Sets the current LookAt target with a specified transition type.
    /// </summary>
    /// <param name="target">The new target to look at.</param>
    /// <param name="transitionType">The type of transition to perform.</param>
    public void SetLookAtTarget(Transform target, LookAtTransitionType transitionType = LookAtTransitionType.SwitchTarget)
    {
        if (target == null)
        {
            ResetLookAtTarget(LookAtTransitionType.ReturnToDefault);
            return;
        }

        // Find the head transform of the target recursively
        currentLookAtTarget = FindHeadTransformRecursive(target) ?? target;
        targetLookAtWeight = 1f; // Set weight to 1 to actively look at the target

        // Set the current transition type based on the specified type
        currentTransitionType = transitionType;
    }

    /// <summary>
    /// Resets the LookAt target to the default position with a specified transition type.
    /// </summary>
    /// <param name="transitionType">The type of transition to perform.</param>
    public void ResetLookAtTarget(LookAtTransitionType transitionType = LookAtTransitionType.ReturnToDefault)
    {
        currentLookAtTarget = null;
        targetLookAtWeight = 0f; // Set weight to 0 to stop looking at any target

        // Set the current transition type based on the specified type
        currentTransitionType = transitionType;
    }

    /// <summary>
    /// Retrieves the appropriate smooth time based on the transition type.
    /// </summary>
    /// <param name="transitionType">The current transition type.</param>
    /// <returns>The corresponding smooth time.</returns>
    private float GetSmoothTime(LookAtTransitionType transitionType)
    {
        switch (transitionType)
        {
            case LookAtTransitionType.FirstLook:
                return smoothTimeFirstLook;
            case LookAtTransitionType.ReturnToDefault:
                return smoothTimeReturn;
            case LookAtTransitionType.SwitchTarget:
                return smoothTimeSwitch;
            case LookAtTransitionType.FollowTarget:
                return smoothTimeFollow;
            default:
                return smoothTimeSwitch;
        }
    }

    /// <summary>
    /// Recursively searches for the head transform within a target hierarchy.
    /// </summary>
    /// <param name="parent">The parent transform to start the search from.</param>
    /// <returns>The head transform if found; otherwise, null.</returns>
    private Transform FindHeadTransformRecursive(Transform parent)
    {
        if (parent == null)
            return null;

        if (parent.name.ToLower().Contains("head"))
            return parent;

        foreach (Transform child in parent)
        {
            Transform result = FindHeadTransformRecursive(child);
            if (result != null)
                return result;
        }
        return null;
    }
}
