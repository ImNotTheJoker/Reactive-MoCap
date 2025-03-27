using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// VelocityInterrupter triggers an interruption if a target object's velocity exceeds a specified threshold.
/// </summary>
[CreateAssetMenu(fileName = "New Velocity Interrupter", menuName = "Interrupters/Velocity Interrupter")]
public class VelocityInterrupter : Interrupter
{
    /// <summary>
    /// Stores the timers for each relevant GameObject to track condition duration.
    /// </summary>
    private Dictionary<GameObject, float> characterTimers = new Dictionary<GameObject, float>();

    /// <summary>
    /// Threshold velocity required to trigger the interrupter.
    /// </summary>
    public float VelocityThreshold;

    /// <summary>
    /// Reference to the CharacterAnimationControllerExtended for animation velocity.
    /// </summary>
    private CharacterAnimationControllerExtended characterAnimationControllerExtended;

    /// <summary>
    /// Reference to the UserVelocityHandler for user-controlled velocity.
    /// </summary>
    private UserVelocityHandler userVelocityHandler;

    /// <summary>
    /// Checks if the condition for the Velocity interrupter is met.
    /// </summary>
    /// <param name="character">The GameObject representing the character.</param>
    public override void CheckCondition(GameObject character)
    {
        // Get the event receiver component
        CharacterEventReceiver receiver = character.GetComponent<CharacterEventReceiver>();
        if (receiver == null) return;

        // Iterate through all relevant GameObjects
        foreach (var obj in RelevantGameObjects)
        {
            if (obj == null) continue;

            // Check the distance to the character
            float distance = Vector3.Distance(character.transform.position, obj.transform.position);

            if (distance > MaxDistance)
            {
                // Reset the timer if the object is out of range
                characterTimers[obj] = 0f;
                receiver.SetHasReacted(this, false);
                receiver.ResetInterrupter(this);
                continue;
            }

            // Handle velocity checks for non-user controlled objects
            if (obj.name != "CenterEyeAnchor")
            {
                characterAnimationControllerExtended = obj.GetComponent<CharacterAnimationControllerExtended>();
                if (characterAnimationControllerExtended != null)
                {
                    HandleVelocityCheck(obj, characterAnimationControllerExtended.smoothVelocity, distance, receiver);
                }
            }
            else
            {
                // Handle user-controlled velocity for the "CenterEyeAnchor"
                userVelocityHandler = obj.GetComponent<UserVelocityHandler>();
                if (userVelocityHandler != null)
                {
                    HandleVelocityCheck(obj, userVelocityHandler.smoothVelocity, distance, receiver);
                }
            }
        }
    }

    /// <summary>
    /// Handles the velocity check and condition evaluation for a target object.
    /// </summary>
    /// <param name="obj">The GameObject being evaluated.</param>
    /// <param name="velocityMagnitude">The velocity magnitude of the object.</param>
    /// <param name="distance">The distance between the character and the object.</param>
    /// <param name="receiver">The CharacterEventReceiver handling interruptions.</param>
    private void HandleVelocityCheck(GameObject obj, float velocityMagnitude, float distance, CharacterEventReceiver receiver)
    {
        Debug.Log($"Velocity of {obj.name}: {velocityMagnitude}");

        // Check if the velocity exceeds the threshold
        if (velocityMagnitude >= VelocityThreshold)
        {
            // Initialize or increment the timer for the object
            if (!characterTimers.ContainsKey(obj))
            {
                characterTimers[obj] = 0f;
            }
            characterTimers[obj] += Time.deltaTime;

            // Trigger interruption if the minimum duration condition is satisfied
            if (characterTimers[obj] >= MinDuration && !receiver.HasReacted(this))
            {
                AnimationClip reactiveAnimation = HasReactiveAnimation() ? ReactiveAnimations[0] : null;

                InterruptionConfig config = new InterruptionConfig(
                    InterrupterType.Velocity,
                    obj.transform.position,
                    MinDuration,
                    Priority,
                    velocityMagnitude,
                    obj,
                    distance,
                    reactiveAnimation
                );

                NotifyCharacter(receiver.gameObject, config, this);
                receiver.SetHasReacted(this, true);
            }
        }
        else
        {
            // Reset the timer if the velocity is below the threshold
            characterTimers[obj] = 0f;
            receiver.SetHasReacted(this, false);
            receiver.ResetInterrupter(this);
        }
    }
}
