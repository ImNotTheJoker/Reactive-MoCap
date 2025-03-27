using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using TMPro;
using UnityEngine.UI;

#if UNITY_EDITOR
/// <summary>
/// Represents a deleted interrupter, storing its ScriptableObject and asset path.
/// </summary>
public class DeletedInterrupter
{
    /// <summary>
    /// The ScriptableObject of the deleted interrupter.
    /// </summary>
    public Interrupter interrupterSO;

    /// <summary>
    /// The asset path of the deleted interrupter.
    /// </summary>
    public string assetPath;

    /// <summary>
    /// Initializes a new instance of the DeletedInterrupter class.
    /// </summary>
    /// <param name="so">The Interrupter ScriptableObject.</param>
    /// <param name="path">The asset path of the interrupter.</param>
    public DeletedInterrupter(Interrupter so, string path)
    {
        interrupterSO = so;
        assetPath = path;
    }
}

/// <summary>
/// Represents a deleted character, storing its ScriptableObject, instance, asset path, and any deleted interrupters.
/// </summary>
public class DeletedCharacter
{
    /// <summary>
    /// The ScriptableObject of the deleted character.
    /// </summary>
    public ReactiveCharacterSO characterSO;

    /// <summary>
    /// The instantiated GameObject of the deleted character.
    /// </summary>
    public GameObject characterInstance;

    /// <summary>
    /// The asset path of the deleted character.
    /// </summary>
    public string assetPath;

    /// <summary>
    /// List of deleted interrupters associated with the character.
    /// </summary>
    public List<DeletedInterrupter> deletedInterrupters;

    /// <summary>
    /// Initializes a new instance of the DeletedCharacter class.
    /// </summary>
    /// <param name="so">The ReactiveCharacterSO ScriptableObject.</param>
    /// <param name="instance">The instantiated GameObject.</param>
    /// <param name="path">The asset path of the character.</param>
    public DeletedCharacter(ReactiveCharacterSO so, GameObject instance, string path)
    {
        characterSO = so;
        characterInstance = instance;
        assetPath = path;
        deletedInterrupters = new List<DeletedInterrupter>();
    }

    /// <summary>
    /// Adds a deleted interrupter to the character's deleted interrupters list.
    /// </summary>
    /// <param name="interrupter">The Interrupter ScriptableObject.</param>
    /// <param name="path">The asset path of the interrupter.</param>
    public void AddDeletedInterrupter(Interrupter interrupter, string path)
    {
        deletedInterrupters.Add(new DeletedInterrupter(interrupter, path));
    }
}
#endif

/// <summary>
/// Manages the list of characters, including their instantiation, configuration, deletion, and undo operations.
/// Interacts with UI elements to display and manage configured characters.
/// </summary>
public class CharacterListManager : MonoBehaviour
{
    [Header("Prefab Assignments")]
    /// <summary>
    /// Prefab for character buttons in the scroll view.
    /// </summary>
    public GameObject characterButtonPrefab;

    [Header("Scene Configuration")]
    /// <summary>
    /// The current ReactiveScene ScriptableObject containing scene-specific data.
    /// </summary>
    private ReactiveSceneSO currentSceneSO;

    /// <summary>
    /// List of configured ReactiveCharacter ScriptableObjects.
    /// </summary>
    private List<ReactiveCharacterSO> configuredCharacters = new List<ReactiveCharacterSO>();

    [Header("Character Management")]
    /// <summary>
    /// Dictionary mapping character names to their instantiated GameObjects.
    /// </summary>
    private Dictionary<string, GameObject> instantiatedCharacters = new Dictionary<string, GameObject>();

    [Header("UI References")]
    /// <summary>
    /// Reference to the content transform of the configured characters scroll view.
    /// </summary>
    private Transform scrollContent;

    [Header("Manager References")]
    /// <summary>
    /// Reference to the CharacterInstantiator script.
    /// </summary>
    private CharacterInstantiator characterInstantiator;

    /// <summary>
    /// Reference to the UIManager script.
    /// </summary>
    private UIManager uiManager;

    [Header("Undo Management")]
    /// <summary>
    /// Queue to store deleted characters for undo functionality.
    /// </summary>
    private Queue<DeletedCharacter> deletedCharactersQueue = new Queue<DeletedCharacter>();

    /// <summary>
    /// Maximum number of undo steps allowed.
    /// </summary>
    private const int MaxUndoSteps = 10;

    /// <summary>
    /// Initializes references to managers and sets up UI elements.
    /// </summary>
    void Awake()
    {
        // Retrieve references to UIManager and CharacterInstantiator
        uiManager = GetComponent<UIManager>();
        characterInstantiator = GetComponent<CharacterInstantiator>();

        // Retrieve the current ReactiveSceneSO from the SceneDataHolder
        currentSceneSO = SceneDataHolder.currentScene;

        if (currentSceneSO == null)
        {
            Debug.LogError("currentSceneSO is null. Ensure that SceneDataHolder.currentScene is correctly set.");
        }

        // Retrieve the content transform of the configured characters scroll view
        if (uiManager != null && uiManager.configuredCharactersScrollView != null)
        {
            scrollContent = uiManager.configuredCharactersScrollView.content;
            Debug.Log("ScrollContent has been set.");
        }
        else
        {
            Debug.LogError("Configured Characters Scroll View is not assigned in the Inspector or UIManager/CharacterInstantiator is missing.");
        }

        if (characterInstantiator == null)
        {
            Debug.LogError("CharacterInstantiator is not assigned or is on a different GameObject.");
        }
    }

    /// <summary>
    /// Populates the character selection dropdown with available character prefabs.
    /// </summary>
    /// <param name="dropdown">The TMP_Dropdown to populate.</param>
    public void PopulateCharacterDropdown(TMP_Dropdown dropdown)
    {
        dropdown.ClearOptions();
        List<string> characterNames = new List<string>();

        // Ensure currentSceneSO is assigned
        currentSceneSO = SceneDataHolder.currentScene;

        if (currentSceneSO == null)
        {
            Debug.LogError("No ReactiveSceneSO assigned. Ensure that SceneDataHolder.currentScene is set.");
            return;
        }

        // Add character names from selectedCharacterPrefabs to the dropdown
        foreach (var characterPrefab in currentSceneSO.selectedCharacterPrefabs)
        {
            characterNames.Add(characterPrefab.name);
        }

        dropdown.AddOptions(characterNames);
        Debug.Log("Character Dropdown has been populated.");
    }

    /// <summary>
    /// Loads all configured characters from the ReactiveSceneSO and instantiates them in the scene.
    /// </summary>
    public void LoadConfiguredCharacters()
    {
#if UNITY_EDITOR
        if (currentSceneSO == null)
        {
            Debug.LogError("currentSceneSO is null. Cannot load configured characters.");
            return;
        }

        configuredCharacters.Clear();

        // Retrieve the current ReactiveSceneSO from the SceneDataHolder
        currentSceneSO = SceneDataHolder.currentScene;
        if (currentSceneSO.reactiveCharacters == null)
        {
            Debug.LogError("ReactiveSceneSO.reactiveCharacters is null. Cannot load configured characters.");
            return;
        }

        // Iterate through each ReactiveCharacterSO and instantiate/configure characters
        foreach (var characterSO in currentSceneSO.reactiveCharacters)
        {
            if (characterSO != null)
            {
                if (!configuredCharacters.Contains(characterSO))
                {
                    configuredCharacters.Add(characterSO);
                }

                if (!instantiatedCharacters.ContainsKey(characterSO.characterName))
                {
                    // Delegate instantiation and configuration to CharacterInstantiator
                    GameObject characterInstance = characterInstantiator.InstantiateAndConfigureCharacter(characterSO);
                    if (characterInstance != null)
                    {
                        instantiatedCharacters.Add(characterSO.characterName, characterInstance);
                        Debug.Log($"Character {characterSO.characterName} has been instantiated.");
                    }
                }
            }
            else
            {
                Debug.LogWarning("An entry in ReactiveSceneSO.reactiveCharacters is null.");
            }
        }

        // Update the ScrollView to display configured characters
        RefreshConfiguredCharactersScrollView();
#endif
    }

    /// <summary>
    /// Refreshes the configured characters scroll view by clearing existing entries and adding current configurations.
    /// </summary>
    public void RefreshConfiguredCharactersScrollView()
    {
#if UNITY_EDITOR
        if (scrollContent == null)
        {
            Debug.LogError("ScrollContent is not set.");
            return;
        }

        // Remove all existing buttons from the scroll view
        foreach (Transform child in scrollContent)
        {
            Destroy(child.gameObject);
        }

        // Add buttons for each configured character
        foreach (var characterSO in configuredCharacters)
        {
            AddCharacterToScrollView(characterSO);
        }

        Debug.Log("Configured Characters ScrollView has been updated.");
#endif
    }

    /// <summary>
    /// Adds a character entry to the configured characters scroll view.
    /// </summary>
    /// <param name="characterSO">The ReactiveCharacterSO of the character to add.</param>
    private void AddCharacterToScrollView(ReactiveCharacterSO characterSO)
    {
#if UNITY_EDITOR
        // Instantiate a button for the configured character
        GameObject buttonObject = Instantiate(characterButtonPrefab, scrollContent);
        buttonObject.name = characterSO.characterName + "Button";

        // Update the button text with the character's name
        TextMeshProUGUI buttonText = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = characterSO.characterName;
        }

        // Add a listener to the button to handle character deletion
        Button button = buttonObject.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnDeleteCharacterButtonClicked(characterSO, buttonObject));
            Debug.Log($"Delete button for {characterSO.characterName} has been added.");
        }
        else
        {
            Debug.LogWarning("Button component not found in the Button Prefab.");
        }
#endif
    }

    /// <summary>
    /// Handles the deletion of a character when its corresponding delete button is clicked.
    /// </summary>
    /// <param name="characterSO">The ReactiveCharacterSO of the character to delete.</param>
    /// <param name="buttonObj">The button GameObject that was clicked.</param>
    private void OnDeleteCharacterButtonClicked(ReactiveCharacterSO characterSO, GameObject buttonObj)
    {
#if UNITY_EDITOR
        if (currentSceneSO.reactiveCharacters.Contains(characterSO))
        {
            // Retrieve the asset path of the character before deletion
            string assetPath = AssetDatabase.GetAssetPath(characterSO);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError($"Asset path for {characterSO.characterName} is null or empty. Cannot delete character.");
                return;
            }

            GameObject characterInstance = null;
            if (instantiatedCharacters.ContainsKey(characterSO.characterName))
            {
                characterInstance = instantiatedCharacters[characterSO.characterName];
            }

            // Clone the CharacterSO before deletion for undo functionality
            ReactiveCharacterSO clonedCharacterSO = ScriptableObject.Instantiate(characterSO);
            clonedCharacterSO.interrupters = new List<Interrupter>(); // Clear the interrupters list in the clone

            // Create a DeletedCharacter object and enqueue it for undo
            DeletedCharacter deletedCharacter = new DeletedCharacter(clonedCharacterSO, characterInstance, assetPath);
            deletedCharactersQueue.Enqueue(deletedCharacter);
            Debug.Log($"Character {characterSO.characterName} has been added to the Undo queue.");

            // Limit the size of the undo queue
            if (deletedCharactersQueue.Count > MaxUndoSteps)
            {
                var removedCharacter = deletedCharactersQueue.Dequeue(); // Remove the oldest entry
                Debug.Log($"Undo queue limited to {MaxUndoSteps} steps. Character {removedCharacter.characterSO.characterName} has been removed.");
            }

            // Clone and delete interrupters associated with the character
            foreach (var interrupter in characterSO.interrupters)
            {
                // Retrieve the asset path of the interrupter
                string interrupterAssetPath = AssetDatabase.GetAssetPath(interrupter);
                if (string.IsNullOrEmpty(interrupterAssetPath))
                {
                    Debug.LogWarning($"Asset path for Interrupter {interrupter.interrupterName} is null or empty. Cannot delete interrupter.");
                    continue;
                }

                // Clone the interrupter
                Interrupter clonedInterrupter = interrupter.Clone();

                // Add the cloned interrupter to the DeletedCharacter
                deletedCharacter.AddDeletedInterrupter(clonedInterrupter, interrupterAssetPath);

                // Delete the interrupter asset
                AssetDatabase.DeleteAsset(interrupterAssetPath);
                Debug.Log($"Interrupter {interrupter.interrupterName} has been deleted at path: {interrupterAssetPath}");
            }

            // Remove the character from the ReactiveSceneSO's reactiveCharacters list
            currentSceneSO.reactiveCharacters.Remove(characterSO);

            // Delete the character asset
            AssetDatabase.DeleteAsset(assetPath);
            Debug.Log($"ReactiveCharacterSO for {characterSO.characterName} has been deleted at path: {assetPath}");

            // Remove the button from the UI
            Destroy(buttonObj);

            // Destroy the instantiated character in the scene
            if (characterInstance != null)
            {
                Destroy(characterInstance);
                Debug.Log($"Instance of {characterSO.characterName} has been removed from the scene.");
            }

            // Remove the character from the instantiatedCharacters dictionary
            instantiatedCharacters.Remove(characterSO.characterName);

            // Mark the ReactiveSceneSO as dirty to ensure changes are saved
            EditorUtility.SetDirty(currentSceneSO);

            // Save and refresh the AssetDatabase
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Reload the configured characters to update the UI
            LoadConfiguredCharacters();

            Debug.Log($"ReactiveCharacterSO for {characterSO.characterName} has been removed and ReactiveSceneSO has been updated.");
        }
#endif
    }

    /// <summary>
    /// Undoes the last character deletion by restoring the character and its interrupters.
    /// </summary>
    public void UndoLastDeletion()
    {
#if UNITY_EDITOR
        if (deletedCharactersQueue.Count > 0)
        {
            DeletedCharacter lastDeleted = deletedCharactersQueue.Dequeue();

            // Ensure the asset path is valid
            if (string.IsNullOrEmpty(lastDeleted.assetPath))
            {
                Debug.LogError($"Cannot undo deletion for {lastDeleted.characterSO.characterName}: assetPath is null or empty.");
                return;
            }

            // Check if the character already exists to prevent duplicates
            if (currentSceneSO.reactiveCharacters.Exists(c => c.characterName == lastDeleted.characterSO.characterName))
            {
                Debug.LogWarning($"Character {lastDeleted.characterSO.characterName} already exists in the scene. Cannot restore.");
                // Optionally, display a UI notification
                if (uiManager != null)
                {
                    uiManager.ShowNotification($"Character {lastDeleted.characterSO.characterName} already exists and cannot be restored.");
                }
                return;
            }

            // Ensure the character folder exists
            string sceneName = currentSceneSO.sceneName;
            string characterFolderPath = $"Assets/ReactiveScenes/{sceneName}/Characters/{lastDeleted.characterSO.characterName}";

            if (!AssetDatabase.IsValidFolder(characterFolderPath))
            {
                AssetDatabase.CreateFolder($"Assets/ReactiveScenes/{sceneName}/Characters", lastDeleted.characterSO.characterName);
                Debug.Log($"Character folder created: {characterFolderPath}");
            }

            // Restore the CharacterSO asset
            AssetDatabase.CreateAsset(lastDeleted.characterSO, lastDeleted.assetPath);
            EditorUtility.SetDirty(lastDeleted.characterSO);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"ReactiveCharacterSO for {lastDeleted.characterSO.characterName} has been restored at path: {lastDeleted.assetPath}");

            // Add the restored CharacterSO back to the ReactiveSceneSO's reactiveCharacters list
            currentSceneSO.reactiveCharacters.Add(lastDeleted.characterSO);
            EditorUtility.SetDirty(currentSceneSO);

            // Restore interrupters
            foreach (var deletedInterrupter in lastDeleted.deletedInterrupters)
            {
                if (string.IsNullOrEmpty(deletedInterrupter.assetPath))
                {
                    Debug.LogWarning($"Cannot restore Interrupter {deletedInterrupter.interrupterSO.interrupterName}: assetPath is null or empty.");
                    continue;
                }

                // Ensure the interrupter folder exists
                string interrupterFolderPath = $"Assets/ReactiveScenes/{sceneName}/Characters/{lastDeleted.characterSO.characterName}/Interrupters";
                if (!AssetDatabase.IsValidFolder(interrupterFolderPath))
                {
                    AssetDatabase.CreateFolder($"Assets/ReactiveScenes/{sceneName}/Characters/{lastDeleted.characterSO.characterName}", "Interrupters");
                    Debug.Log($"Interrupter folder created: {interrupterFolderPath}");
                }

                // Restore the Interrupter asset
                AssetDatabase.CreateAsset(deletedInterrupter.interrupterSO, deletedInterrupter.assetPath);
                EditorUtility.SetDirty(deletedInterrupter.interrupterSO);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Interrupter {deletedInterrupter.interrupterSO.interrupterName} has been restored at path: {deletedInterrupter.assetPath}");

                // Add the interrupter back to the character's interrupters list
                lastDeleted.characterSO.interrupters.Add(deletedInterrupter.interrupterSO);
                Debug.Log($"Interrupter {deletedInterrupter.interrupterSO.interrupterName} has been added to {lastDeleted.characterSO.characterName}'s interrupters list.");
            }

            // Mark the CharacterSO as dirty to ensure changes are saved
            EditorUtility.SetDirty(lastDeleted.characterSO);

            // Save and refresh the AssetDatabase
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("All assets have been restored.");

            // Restore the character instance in the scene via CharacterInstantiator
            if (lastDeleted.characterInstance != null)
            {
                GameObject restoredInstance = characterInstantiator.InstantiateAndConfigureCharacter(lastDeleted.characterSO);
                if (restoredInstance != null)
                {
                    instantiatedCharacters.Add(lastDeleted.characterSO.characterName, restoredInstance);
                    Debug.Log($"Character instance for {lastDeleted.characterSO.characterName} has been restored.");
                }
                else
                {
                    Debug.LogError($"Character instance for {lastDeleted.characterSO.characterName} could not be restored.");
                }
            }

            // Reload the configured characters to update the UI
            LoadConfiguredCharacters();

            Debug.Log($"Character {lastDeleted.characterSO.characterName} has been successfully restored.");
        }
#endif
    }

    /// <summary>
    /// Starts playing the main animations for all configured characters.
    /// </summary>
    public void PlayMainAnimations()
    {
#if UNITY_EDITOR
        foreach (var characterSO in configuredCharacters)
        {
            if (instantiatedCharacters.ContainsKey(characterSO.characterName))
            {
                GameObject characterInstance = instantiatedCharacters[characterSO.characterName];
                CharacterAnimationControllerSetup animationController = characterInstance.GetComponent<CharacterAnimationControllerSetup>();
                if (animationController != null)
                {
                    animationController.PlayMainAnimation();
                    Debug.Log($"Main Animation for {characterSO.characterName} has started.");
                }
                else
                {
                    Debug.LogWarning($"CharacterAnimationControllerSetup for {characterSO.characterName} not found.");
                }
            }
            else
            {
                Debug.LogWarning($"Instance of {characterSO.characterName} not found.");
            }
        }

        Debug.Log("Main Animations for all characters have been started.");
#endif
    }

    /// <summary>
    /// Stops all animations and resets the poses of all configured characters.
    /// </summary>
    public void StopAnimationsAndResetPoses()
    {
#if UNITY_EDITOR
        foreach (var characterSO in configuredCharacters)
        {
            if (instantiatedCharacters.ContainsKey(characterSO.characterName))
            {
                GameObject characterInstance = instantiatedCharacters[characterSO.characterName];
                CharacterAnimationControllerSetup animationController = characterInstance.GetComponent<CharacterAnimationControllerSetup>();
                if (animationController != null)
                {
                    animationController.StopAnimation();
                    Debug.Log($"Animation for {characterSO.characterName} has been stopped and pose reset.");
                }
                else
                {
                    Debug.LogWarning($"CharacterAnimationControllerSetup for {characterSO.characterName} not found.");
                }
            }
            else
            {
                Debug.LogWarning($"Instance of {characterSO.characterName} not found.");
            }
        }

        Debug.Log("All animations have been stopped and characters have been reset.");
#endif
    }

    /// <summary>
    /// Retrieves the dictionary of instantiated characters.
    /// </summary>
    /// <returns>A dictionary mapping character names to their instantiated GameObjects.</returns>
    public Dictionary<string, GameObject> GetInstantiatedCharacters()
    {
        return instantiatedCharacters;
    }

    /// <summary>
    /// Retrieves the list of configured ReactiveCharacter ScriptableObjects.
    /// </summary>
    /// <returns>A list of configured ReactiveCharacterSO objects.</returns>
    public List<ReactiveCharacterSO> GetConfiguredCharacters()
    {
        return configuredCharacters;
    }
}
