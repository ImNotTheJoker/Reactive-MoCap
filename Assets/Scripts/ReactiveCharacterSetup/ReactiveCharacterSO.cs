using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ScriptableObject representing a reactive character with associated animations and interrupters.
/// </summary>
[CreateAssetMenu(fileName = "NewReactiveCharacter", menuName = "ReactiveCharacter", order = 51)]
public class ReactiveCharacterSO : ScriptableObject
{
    [Header("Character Information")]
    /// <summary>
    /// Name of the character.
    /// </summary>
    public string characterName;

    [Header("Character Setup")]
    /// <summary>
    /// Prefab representing the character in the game.
    /// </summary>
    public GameObject characterPrefab;

    /// <summary>
    /// Initial position where the character will be placed in the scene.
    /// </summary>
    public Vector3 characterPosition;

    /// <summary>
    /// Initial rotation of the character when placed in the scene.
    /// </summary>
    public Vector3 characterRotation;

    [Header("Animations and Interrupters")]
    /// <summary>
    /// List of main animations associated with the character.
    /// </summary>
    public List<AnimationClip> mainAnimations = new List<AnimationClip>();

    /// <summary>
    /// List of interrupters that can trigger reactions in the character.
    /// </summary>
    public List<Interrupter> interrupters = new List<Interrupter>();

    /// <summary>
    /// Initializes the ReactiveCharacterSO with specified parameters.
    /// </summary>
    /// <param name="name">Name of the character.</param>
    /// <param name="prefab">Prefab representing the character.</param>
    /// <param name="pos">Initial position of the character.</param>
    /// <param name="rot">Initial rotation of the character.</param>
    public void Initialize(string name, GameObject prefab, Vector3 pos, Vector3 rot)
    {
        characterName = name;
        characterPrefab = prefab;
        characterPosition = pos;
        characterRotation = rot;
    }

    /// <summary>
    /// Adds a main animation to the character if it doesn't already exist in the list.
    /// </summary>
    /// <param name="animation">The AnimationClip to add.</param>
    public void AddMainAnimation(AnimationClip animation)
    {
        if (animation == null)
        {
            Debug.LogWarning("Attempted to add a null AnimationClip to mainAnimations.");
            return;
        }

        if (!mainAnimations.Contains(animation))
        {
            mainAnimations.Add(animation);
#if UNITY_EDITOR
            EditorUtility.SetDirty(this); // Mark the ScriptableObject as dirty to save changes in the editor
#endif
        }
    }

    /// <summary>
    /// Removes a main animation from the character's animation list.
    /// </summary>
    /// <param name="animation">The AnimationClip to remove.</param>
    public void RemoveMainAnimation(AnimationClip animation)
    {
        if (mainAnimations.Contains(animation))
        {
            mainAnimations.Remove(animation);
#if UNITY_EDITOR
            EditorUtility.SetDirty(this); // Mark the ScriptableObject as dirty to save changes in the editor
#endif
        }
        else
        {
            Debug.LogWarning($"AnimationClip '{animation.name}' not found in mainAnimations.");
        }
    }

    /// <summary>
    /// Adds an interrupter to the character if it doesn't already exist in the list.
    /// </summary>
    /// <param name="interrupter">The Interrupter to add.</param>
    public void AddInterrupter(Interrupter interrupter)
    {
        if (interrupter == null)
        {
            Debug.LogWarning("Attempted to add a null Interrupter to interrupters.");
            return;
        }

        if (!interrupters.Contains(interrupter))
        {
            interrupters.Add(interrupter);
#if UNITY_EDITOR
            EditorUtility.SetDirty(this); // Mark the ScriptableObject as dirty to save changes in the editor
#endif
        }
    }

    /// <summary>
    /// Removes an interrupter from the character's interrupter list.
    /// </summary>
    /// <param name="interrupter">The Interrupter to remove.</param>
    public void RemoveInterrupter(Interrupter interrupter)
    {
        if (interrupters.Contains(interrupter))
        {
            interrupters.Remove(interrupter);
#if UNITY_EDITOR
            EditorUtility.SetDirty(this); // Mark the ScriptableObject as dirty to save changes in the editor
#endif
        }
        else
        {
            Debug.LogWarning($"Interrupter '{interrupter.interrupterName}' not found in interrupters.");
        }
    }
}
