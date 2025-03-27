using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ProximityInterrupter triggers an interruption if a target object is within a specified distance for a set duration.
/// </summary>
[CreateAssetMenu(fileName = "New Proximity Interrupter", menuName = "Interrupters/Proximity Interrupter")]
public class ProximityInterrupter : Interrupter
{
    private Dictionary<GameObject, float> characterTimers = new Dictionary<GameObject, float>();

    /// <summary>
    /// Checks if the condition for the Proximity interrupter is met.
    /// </summary>
    /// <param name="character">The GameObject representing the character.</param>
    public override void CheckCondition(GameObject character)
    {
        CharacterEventReceiver receiver = character.GetComponent<CharacterEventReceiver>();
        if (receiver == null) return;

        foreach (var obj in RelevantGameObjects)
        {
            if (obj == null) continue;

            float distance = Vector3.Distance(character.transform.position, obj.transform.position);

            if (distance < MaxDistance)
            {
                if (!characterTimers.ContainsKey(character))
                {
                    characterTimers[character] = 0f;
                }
                characterTimers[character] += Time.deltaTime;

                if (characterTimers[character] >= MinDuration && !receiver.HasReacted(this))
                {
                    AnimationClip reactiveAnimation = HasReactiveAnimation() ? ReactiveAnimations[0] : null;

                    InterruptionConfig config = new InterruptionConfig(
                        InterrupterType.Proximity,
                        obj.transform.position,
                        MinDuration,
                        Priority,
                        MaxDistance - distance,
                        obj,
                        0f,
                        reactiveAnimation
                    );

                    NotifyCharacter(character, config, this);
                    receiver.SetHasReacted(this, true);
                }
            }
            else
            {
                // Reset the timer if the object is no longer in range
                characterTimers[character] = 0f;
                receiver.SetHasReacted(this, false);
                receiver.ResetInterrupter(this);
            }
        }
    }
}
