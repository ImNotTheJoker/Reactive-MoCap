using UnityEngine;

/// <summary>
/// Handles retargeting of the player's avatar to a mirror avatar by synchronizing their poses.
/// </summary>
public class RetargetingHPH : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The source Player Avatar GameObject to mirror.")]
    public GameObject src; // Player Avatar

    [Header("Pose Handlers")]
    [Tooltip("Handles the pose of the source avatar.")]
    private HumanPoseHandler m_srcPoseHandler;

    [Tooltip("Handles the pose of the destination (mirror) avatar.")]
    private HumanPoseHandler m_destPoseHandler;

    [Tooltip("Stores the current human pose data.")]
    private HumanPose m_humanPose;

    /// <summary>
    /// Initializes the HumanPoseHandlers for both the source and destination avatars.
    /// </summary>
    void Start()
    {
        InitializePoseHandlers();
    }

    /// <summary>
    /// Continuously updates the mirror avatar's pose to match the source avatar's pose.
    /// </summary>
    void LateUpdate()
    {
        UpdateMirrorPose();
    }

    /// <summary>
    /// Sets a new source avatar and reinitializes the source pose handler.
    /// </summary>
    /// <param name="newSrc">The new source Player Avatar GameObject.</param>
    public void SetSourceAvatar(GameObject newSrc)
    {
        if (newSrc == null)
        {
            Debug.LogError("New source avatar is null. Please provide a valid GameObject.");
            return;
        }

        src = newSrc;
        InitializeSourcePoseHandler();
    }

    /// <summary>
    /// Initializes the HumanPoseHandlers for the source and destination avatars.
    /// </summary>
    private void InitializePoseHandlers()
    {
        if (src == null)
        {
            Debug.LogError("Source avatar (src) is not assigned.");
            return;
        }

        Animator srcAnimator = src.GetComponent<Animator>();
        Animator destAnimator = GetComponent<Animator>();

        if (srcAnimator == null)
        {
            Debug.LogError("Source avatar does not have an Animator component.");
            return;
        }

        if (destAnimator == null)
        {
            Debug.LogError("Destination avatar does not have an Animator component.");
            return;
        }

        // Initialize the pose handlers for source and destination avatars
        m_srcPoseHandler = new HumanPoseHandler(srcAnimator.avatar, src.transform);
        m_destPoseHandler = new HumanPoseHandler(destAnimator.avatar, transform);
        m_humanPose = new HumanPose();
    }

    /// <summary>
    /// Initializes or reinitializes the source pose handler when the source avatar changes.
    /// </summary>
    private void InitializeSourcePoseHandler()
    {
        if (src == null)
        {
            Debug.LogError("Source avatar (src) is not assigned.");
            return;
        }

        Animator srcAnimator = src.GetComponent<Animator>();
        if (srcAnimator == null)
        {
            Debug.LogError("New source avatar does not have an Animator component.");
            return;
        }

        // Reinitialize the source pose handler with the new source avatar
        m_srcPoseHandler = new HumanPoseHandler(srcAnimator.avatar, src.transform);
    }

    /// <summary>
    /// Updates the mirror avatar's pose to match the source avatar's pose.
    /// </summary>
    private void UpdateMirrorPose()
    {
        if (m_srcPoseHandler == null || m_destPoseHandler == null)
        {
            Debug.LogError("Pose handlers are not initialized.");
            return;
        }

        // Get the current pose from the source avatar
        m_srcPoseHandler.GetHumanPose(ref m_humanPose);

        // Apply the pose to the destination (mirror) avatar
        m_destPoseHandler.SetHumanPose(ref m_humanPose);
    }
}
