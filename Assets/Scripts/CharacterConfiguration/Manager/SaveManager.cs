using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Manages the saving of character configurations, including their positions and rotations.
/// Interacts with the CharacterListManager and SaveManager to ensure all configurations are properly saved.
/// </summary>
public class SaveManager : MonoBehaviour
{
    [Header("Scene Configuration")]
    /// <summary>
    /// The current ReactiveScene ScriptableObject containing scene-specific data.
    /// </summary>
    private ReactiveSceneSO currentSceneSO;

    [Header("Character Management")]
    /// <summary>
    /// Dictionary mapping character names to their instantiated GameObjects.
    /// </summary>
    private Dictionary<string, GameObject> instantiatedCharacters;

    /// <summary>
    /// Initializes references to necessary managers and character instances.
    /// </summary>
    void Awake()
    {
        // Retrieve the CharacterListManager component
        CharacterListManager characterListManager = GetComponent<CharacterListManager>();
        if (characterListManager != null)
        {
            instantiatedCharacters = characterListManager.GetInstantiatedCharacters();
        }
        else
        {
            Debug.LogError("CharacterListManager not found.");
        }
    }

    /// <summary>
    /// Handles the saving of all character configurations, including their positions and rotations.
    /// This method updates the ReactiveSceneSO with the current state of each character.
    /// </summary>
    public void HandleSaveAll()
    {
#if UNITY_EDITOR
        // Retrieve the current ReactiveSceneSO from the SceneDataHolder
        currentSceneSO = SceneDataHolder.currentScene;

        if (currentSceneSO == null)
        {
            Debug.LogError("No ReactiveSceneSO assigned.");
            return;
        }

        // Iterate through each instantiated character and update their configurations
        foreach (var kvp in instantiatedCharacters)
        {
            string characterName = kvp.Key;
            GameObject characterInstance = kvp.Value;

            // Find the corresponding ReactiveCharacterSO for the character
            ReactiveCharacterSO characterSO = currentSceneSO.reactiveCharacters.Find(c => c.characterName == characterName);
            if (characterSO != null)
            {
                // Update character position and rotation in the ScriptableObject
                characterSO.characterPosition = characterInstance.transform.position;
                characterSO.characterRotation = characterInstance.transform.eulerAngles;

                // Mark the ScriptableObject as dirty to ensure changes are saved
                EditorUtility.SetDirty(characterSO);
                Debug.Log($"Position and rotation for {characterName} updated.");
            }
            else
            {
                Debug.LogWarning($"No ReactiveCharacterSO found for {characterName}.");
            }
        }

        // Save all changes to the AssetDatabase
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Refresh the configured characters scroll view in the CharacterListManager
        CharacterListManager characterListManager = GetComponent<CharacterListManager>();
        if (characterListManager != null)
        {
            characterListManager.RefreshConfiguredCharactersScrollView();
        }
        else
        {
            Debug.LogError("CharacterListManager not found.");
        }

        Debug.Log("All character positions and rotations have been saved.");
#endif
    }
}
