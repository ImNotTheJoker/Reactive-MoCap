using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Abstract base class for all interrupters.
/// Handles core logic, such as relevant objects, reactive animations, and basic initialization.
/// </summary>
public abstract class Interrupter : ScriptableObject
{
    public string interrupterName; // Name of the interrupter
    public List<string> relevantGameObjectNames; // Names of relevant GameObjects
    public float MaxDistance; // Maximum distance to trigger interruption
    public float MinDuration; // Minimum duration before interruption is valid
    public int Priority; // Priority of the interrupter
    public List<AnimationClip> ReactiveAnimations; // List of reactive animations

    protected List<GameObject> RelevantGameObjects = new List<GameObject>(); // Cached relevant GameObjects
    private bool isPlaying; // Is the interrupter currently active
    public bool IsOverlaid { get; set; } // Is the interrupter overlaid by a higher priority one

    /// <summary>
    /// Creates a clone of the interrupter for a specific character instance.
    /// </summary>
    public virtual Interrupter Clone()
    {
        return Instantiate(this);
    }

    /// <summary>
    /// Initializes the interrupter by finding and caching relevant GameObjects.
    /// </summary>
    /// <param name="receiver">CharacterEventReceiver reference.</param>
    public void Initialize(CharacterEventReceiver receiver)
    {
        RelevantGameObjects.Clear();
        foreach (var objName in relevantGameObjectNames)
        {
            var obj = GameObject.Find(objName);
            if (obj != null)
            {
                RelevantGameObjects.Add(obj);
                Debug.Log($"GameObject '{objName}' found and added to relevant objects.");
            }
            else
            {
                Debug.LogWarning($"GameObject '{objName}' not found.");
            }
        }
    }

    /// <summary>
    /// Checks the specific condition for the interrupter. Must be implemented by derived classes.
    /// </summary>
    /// <param name="character">The GameObject of the character.</param>
    public abstract void CheckCondition(GameObject character);

    /// <summary>
    /// Notifies the character event receiver about the interruption.
    /// </summary>
    /// <param name="character">The character to notify.</param>
    /// <param name="config">The interruption configuration.</param>
    /// <param name="interrupter">The interrupter triggering the interruption.</param>
    protected void NotifyCharacter(GameObject character, InterruptionConfig config, Interrupter interrupter)
    {
        CharacterEventReceiver receiver = character.GetComponent<CharacterEventReceiver>();
        if (receiver != null)
        {
            receiver.HandleInterruption(config, interrupter);
        }
    }

    /// <summary>
    /// Returns whether the interrupter is currently playing.
    /// </summary>
    public bool IsPlaying()
    {
        return isPlaying;
    }

    /// <summary>
    /// Sets the playing status of the interrupter.
    /// </summary>
    public void SetPlaying(bool value)
    {
        isPlaying = value;
    }

    /// <summary>
    /// Marks the interrupter as overlaid (suppressed by a higher priority interrupter).
    /// </summary>
    public void Overlay()
    {
        IsOverlaid = true;
    }

    /// <summary>
    /// Checks if the interrupter has any reactive animations assigned.
    /// </summary>
    public bool HasReactiveAnimation()
    {
        return ReactiveAnimations != null && ReactiveAnimations.Count > 0;
    }
}
