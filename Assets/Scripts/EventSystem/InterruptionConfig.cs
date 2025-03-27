using UnityEngine;

/// <summary>
/// Configuration for interruptions, containing relevant data for handling an interrupter event.
/// </summary>
public class InterruptionConfig
{
    public InterrupterType Type { get; private set; } // Type of the interrupter
    public Vector3 TargetPosition { get; private set; } // Position of the target object
    public float Duration { get; private set; } // Minimum duration for the interruption
    public int Priority { get; private set; } // Priority of the interrupter
    public float Sensitivity { get; private set; } // Sensitivity value (if applicable)
    public GameObject RelevantObject { get; private set; } // Relevant target GameObject
    public float ActiveDuration { get; private set; } // Active duration for the interruption
    public AnimationClip ReactiveAnimation { get; private set; } // Animation to trigger (optional)

    /// <summary>
    /// Constructor for creating an interruption configuration.
    /// </summary>
    public InterruptionConfig(
        InterrupterType type, 
        Vector3 targetPosition, 
        float duration, 
        int priority, 
        float sensitivity, 
        GameObject relevantObject, 
        float activeDuration, 
        AnimationClip reactiveAnimation = null)
    {
        Type = type;
        TargetPosition = targetPosition;
        Duration = duration;
        Priority = priority;
        Sensitivity = sensitivity;
        RelevantObject = relevantObject;
        ActiveDuration = activeDuration;
        ReactiveAnimation = reactiveAnimation;
    }
}

/// <summary>
/// Enumeration of possible interrupter types.
/// </summary>
public enum InterrupterType
{
    Audio,
    Velocity,
    Proximity,
    LookAt
}
