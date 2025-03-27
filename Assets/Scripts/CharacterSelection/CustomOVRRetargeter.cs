/// <summary>
/// Custom retargeter class that extends the OVRUnityHumanoidSkeletonRetargeter.
/// Allows setting the skeleton type to FullBody for comprehensive retargeting.
/// </summary>
public class CustomOVRRetargeter : OVRUnityHumanoidSkeletonRetargeter
{
    /// <summary>
    /// Sets the skeleton type to FullBody.
    /// This method configures the retargeter to use a full-body skeleton for more accurate motion mapping.
    /// </summary>
    public void SetFullBody()
    {
        _skeletonType = OVRSkeleton.SkeletonType.FullBody;
    }
}
