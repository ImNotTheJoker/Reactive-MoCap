using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LookAtInterrupter checks if a target object is within a specified distance and angle to trigger an interruption.
/// </summary>
[CreateAssetMenu(fileName = "New LookAt Interrupter", menuName = "Interrupters/LookAt Interrupter")]
public class LookAtInterrupter : Interrupter
{
    private Dictionary<GameObject, float> characterTimers = new Dictionary<GameObject, float>();
    public float MaxAngle = 30f; // Maximum allowed angle to trigger the interruption

    /// <summary>
    /// Checks if the condition for the LookAt interrupter is met.
    /// </summary>
    /// <param name="character">The GameObject representing the character.</param>
    public override void CheckCondition(GameObject character)
    {
        CharacterEventReceiver receiver = character.GetComponent<CharacterEventReceiver>();
        if (receiver == null) return;

        foreach (var obj in RelevantGameObjects)
        {
            if (obj != null)
            {
                // Determine the target position based on the head of the target object
                Transform headTransform = FindHeadTransformRecursive(character.transform);
                Vector3 targetPosition;
                targetPosition = headTransform != null ? headTransform.position : character.transform.position;

                // Check distance to the character
                float distance = Vector3.Distance(targetPosition, obj.transform.position);

                // Calculate the angle between the object's forward direction and the character's position
                Vector3 directionToCharacter = (targetPosition - obj.transform.position).normalized;
                float angle = Vector3.Angle(obj.transform.forward, directionToCharacter);

                if (distance < MaxDistance && angle < MaxAngle)
                {
                    // Accumulate timer if the condition is met
                    if (!characterTimers.ContainsKey(character))
                    {
                        characterTimers[character] = 0f;
                    }

                    characterTimers[character] += Time.deltaTime;

                    /// Trigger the interruption if duration condition is satisfied
                    if (characterTimers[character] >= MinDuration && !receiver.HasReacted(this))
                    {
                        AnimationClip reactiveAnimation = (ReactiveAnimations != null && ReactiveAnimations.Count > 0) ? ReactiveAnimations[0] : null;

                        InterruptionConfig config = new InterruptionConfig(
                            InterrupterType.LookAt,
                            obj.transform.position,
                            MinDuration,
                            Priority,
                            MaxDistance - distance,
                            obj,
                            distance,
                            reactiveAnimation
                        );

                        NotifyCharacter(character, config, this);
                        receiver.SetHasReacted(this, true);
                    }
                }
                else
                {
                    // Reset if the conditions are not satisfied
                    characterTimers[character] = 0f;
                    receiver.SetHasReacted(this, false);
                    receiver.ResetInterrupter(this);
                }
            }
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
