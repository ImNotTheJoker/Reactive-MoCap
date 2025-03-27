using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ScriptableObject representing a reactive scene containing multiple reactive characters and selected character prefabs.
/// </summary>
[CreateAssetMenu(fileName = "NewReactiveScene", menuName = "ReactiveScene", order = 52)]
public class ReactiveSceneSO : ScriptableObject
{
    [Header("Scene Information")]
    /// <summary>
    /// Name of the scene.
    /// </summary>
    public string sceneName;

    [Header("Scene Components")]
    /// <summary>
    /// List of reactive characters associated with the scene.
    /// </summary>
    public List<ReactiveCharacterSO> reactiveCharacters = new List<ReactiveCharacterSO>();

    /// <summary>
    /// List of selected character prefabs to be instantiated in the scene.
    /// </summary>
    public List<GameObject> selectedCharacterPrefabs = new List<GameObject>();

    [Header("Scene Settings")]
    /// <summary>
    /// Indicates whether the scene is reactive.
    /// </summary>
    public bool isReactiveScene = true;

    /// <summary>
    /// Initializes the ReactiveSceneSO with specified parameters.
    /// </summary>
    /// <param name="name">Name of the scene.</param>
    /// <param name="environment">Environment GameObject for the scene.</param>
    public void Initialize(string name, GameObject environment)
    {
        sceneName = name;
        selectedCharacterPrefabs = new List<GameObject>();
    }

    /// <summary>
    /// Adds a reactive character to the scene if it doesn't already exist in the list.
    /// </summary>
    /// <param name="character">The ReactiveCharacterSO to add.</param>
    public void AddCharacter(ReactiveCharacterSO character)
    {
        if (character == null)
        {
            Debug.LogWarning("Attempted to add a null ReactiveCharacterSO to reactiveCharacters.");
            return;
        }

        if (!reactiveCharacters.Contains(character))
        {
            reactiveCharacters.Add(character);
#if UNITY_EDITOR
            EditorUtility.SetDirty(this); // Mark the ScriptableObject as dirty to save changes in the editor
#endif
        }
    }

    /// <summary>
    /// Removes a reactive character from the scene's reactive characters list.
    /// </summary>
    /// <param name="character">The ReactiveCharacterSO to remove.</param>
    public void RemoveCharacter(ReactiveCharacterSO character)
    {
        if (reactiveCharacters.Contains(character))
        {
            reactiveCharacters.Remove(character);
#if UNITY_EDITOR
            EditorUtility.SetDirty(this); // Mark the ScriptableObject as dirty to save changes in the editor
#endif
        }
        else
        {
            Debug.LogWarning($"ReactiveCharacterSO '{character?.characterName}' not found in reactiveCharacters.");
        }
    }

    /// <summary>
    /// Adds a selected character prefab to the scene if it doesn't already exist in the list.
    /// </summary>
    /// <param name="characterPrefab">The GameObject prefab to add.</param>
    public void AddSelectedCharacterPrefab(GameObject characterPrefab)
    {
        if (characterPrefab == null)
        {
            Debug.LogWarning("Attempted to add a null GameObject to selectedCharacterPrefabs.");
            return;
        }

        if (!selectedCharacterPrefabs.Contains(characterPrefab))
        {
            selectedCharacterPrefabs.Add(characterPrefab);
#if UNITY_EDITOR
            EditorUtility.SetDirty(this); // Mark the ScriptableObject as dirty to save changes in the editor
#endif
        }
    }

    /// <summary>
    /// Removes a selected character prefab from the scene's selected character prefabs list.
    /// </summary>
    /// <param name="characterPrefab">The GameObject prefab to remove.</param>
    public void RemoveSelectedCharacterPrefab(GameObject characterPrefab)
    {
        if (selectedCharacterPrefabs.Contains(characterPrefab))
        {
            selectedCharacterPrefabs.Remove(characterPrefab);
#if UNITY_EDITOR
            EditorUtility.SetDirty(this); // Mark the ScriptableObject as dirty to save changes in the editor
#endif
        }
        else
        {
            Debug.LogWarning($"GameObject prefab '{characterPrefab?.name}' not found in selectedCharacterPrefabs.");
        }
    }
}
