using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CharacterSelectionManager : MonoBehaviour
{
    [Header("Mirror Avatar Prefabs")]
    public List<GameObject> mirrorAvatarPrefabs;

    [Header("UI Elements")]
    public TMP_Dropdown allCharactersDropdown; // Dropdown for all Mirror Avatars
    public TMP_Dropdown selectedCharactersDropdown; // Dropdown for selected Mirror Avatars

    [Header("Buttons")]
    public Button addButton;
    public Button removeButton;
    public Button nextButton; // Button to save and proceed

    [Header("Parent Transforms")]
    public Transform mirrorAvatarParent; // Parent Transform for Mirror Avatars

    [Header("Scenes")]
    // Current ReactiveScene ScriptableObject
    private ReactiveSceneSO currentReactiveSceneSO;

    // Flag to determine if it's a new scene
    private bool isNewScene = false;

    // Path to the folder containing ReactiveSceneSO assets
    private string scenesFolderPath = "Assets/ReactiveScenes";

    // Name of the next scene to load
    public string nextSceneName;

    // List to store selected character prefabs
    private List<GameObject> selectedCharacters = new List<GameObject>();

    // Currently displayed Mirror Avatar
    private GameObject currentMirrorAvatar;

    void Start()
    {
        PopulateAllCharactersDropdown();
        SetupListeners();
    }

    void OnEnable()
    {
        // Display the Mirror Avatar
        ShowMirrorAvatar();

        // Load selected characters or clear the list based on the scene
        LoadSelectedCharacters();
    }

    void OnDisable()
    {
        // Remove the current Mirror Avatar when the panel is disabled
        if (currentMirrorAvatar != null)
        {
            Destroy(currentMirrorAvatar);
            currentMirrorAvatar = null;
        }
    }

    /// <summary>
    /// Populates the dropdown with all available Mirror Avatar prefabs.
    /// </summary>
    private void PopulateAllCharactersDropdown()
    {
        allCharactersDropdown.ClearOptions();
        foreach (var characterPrefab in mirrorAvatarPrefabs)
        {
            allCharactersDropdown.options.Add(new TMP_Dropdown.OptionData(characterPrefab.name));
        }
        allCharactersDropdown.RefreshShownValue();
    }

    /// <summary>
    /// Sets up listeners for UI elements.
    /// </summary>
    private void SetupListeners()
    {
        addButton.onClick.AddListener(AddSelectedCharacter);
        removeButton.onClick.AddListener(RemoveSelectedCharacter);
        nextButton.onClick.AddListener(OnNextButtonClicked);
        allCharactersDropdown.onValueChanged.AddListener(OnAllCharactersDropdownChanged);
    }

    /// <summary>
    /// Updates the Mirror Avatar display when a different avatar is selected.
    /// </summary>
    /// <param name="index">Index of the selected avatar in the dropdown.</param>
    private void OnAllCharactersDropdownChanged(int index)
    {
        ShowMirrorAvatar();
    }

    /// <summary>
    /// Displays the selected Mirror Avatar in the scene.
    /// </summary>
    private void ShowMirrorAvatar()
    {
        // Check if the panel is active
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        int selectedIndex = allCharactersDropdown.value;
        GameObject selectedCharacterPrefab = mirrorAvatarPrefabs[selectedIndex];

        // Remove the old Mirror Avatar if it exists
        if (currentMirrorAvatar != null)
        {
            Destroy(currentMirrorAvatar);
        }

        // Instantiate the new Mirror Avatar
        currentMirrorAvatar = Instantiate(selectedCharacterPrefab, mirrorAvatarParent);
        currentMirrorAvatar.transform.localPosition = Vector3.zero;
        currentMirrorAvatar.transform.localRotation = Quaternion.identity;

        // Set up retargeting to mirror the player's movements
        RetargetMirrorAvatar(currentMirrorAvatar);
    }

    /// <summary>
    /// Adds the selected character to the list of selected characters.
    /// </summary>
    private void AddSelectedCharacter()
    {
        int selectedIndex = allCharactersDropdown.value;
        GameObject selectedCharacterPrefab = mirrorAvatarPrefabs[selectedIndex];

        if (!selectedCharacters.Contains(selectedCharacterPrefab))
        {
            selectedCharacters.Add(selectedCharacterPrefab);

            // Update the selected characters dropdown
            UpdateSelectedCharactersDropdown();
        }
    }

    /// <summary>
    /// Removes the selected character from the list of selected characters.
    /// </summary>
    private void RemoveSelectedCharacter()
    {
        int selectedIndex = selectedCharactersDropdown.value;
        if (selectedIndex >= 0 && selectedIndex < selectedCharacters.Count)
        {
            selectedCharacters.RemoveAt(selectedIndex);

            // Update the selected characters dropdown
            UpdateSelectedCharactersDropdown();
        }
    }

    /// <summary>
    /// Updates the selected characters dropdown with the current list.
    /// </summary>
    private void UpdateSelectedCharactersDropdown()
    {
        selectedCharactersDropdown.ClearOptions();
        foreach (var characterPrefab in selectedCharacters)
        {
            selectedCharactersDropdown.options.Add(new TMP_Dropdown.OptionData(characterPrefab.name));
        }
        selectedCharactersDropdown.RefreshShownValue();
    }

    /// <summary>
    /// Sets up retargeting so that the Mirror Avatar mirrors the player's movements.
    /// </summary>
    /// <param name="mirrorAvatar">The instantiated Mirror Avatar GameObject.</param>
    private void RetargetMirrorAvatar(GameObject mirrorAvatar)
    {
        // Find the player avatar in the scene (assumed to have the tag "PlayerAvatar")
        GameObject playerAvatar = GameObject.FindWithTag("PlayerAvatar");
        if (playerAvatar != null)
        {
            // Add the RetargetingHPH script if it doesn't exist
            RetargetingHPH mirrorScript = mirrorAvatar.GetComponent<RetargetingHPH>();
            if (mirrorScript == null)
            {
                mirrorScript = mirrorAvatar.AddComponent<RetargetingHPH>();
            }
            mirrorScript.src = playerAvatar;
        }
        else
        {
            Debug.LogError("Player avatar not found. Please ensure your player avatar has the tag 'PlayerAvatar'.");
        }
    }

    /// <summary>
    /// Sets whether the current scene is new or existing.
    /// </summary>
    /// <param name="value">True if it's a new scene; otherwise, false.</param>
    public void SetIsNewScene(bool value)
    {
        isNewScene = value;
    }

    /// <summary>
    /// Assigns the current ReactiveScene ScriptableObject.
    /// </summary>
    /// <param name="sceneSO">The ReactiveSceneSO to assign.</param>
    public void SetCurrentReactiveSceneSO(ReactiveSceneSO sceneSO)
    {
        currentReactiveSceneSO = sceneSO;
        // Assign to SceneDataHolder for global access
        SceneDataHolder.currentScene = sceneSO;
    }

    /// <summary>
    /// Handles the event when the "Next" button is clicked.
    /// </summary>
    private void OnNextButtonClicked()
    {
        if (isNewScene)
        {
            // Create a new ReactiveSceneSO if it's a new scene
            currentReactiveSceneSO = CreateNewReactiveSceneSO();
        }

        if (currentReactiveSceneSO != null)
        {
            // Save the current selection to the ScriptableObject
            currentReactiveSceneSO.selectedCharacterPrefabs = new List<GameObject>(selectedCharacters);

    #if UNITY_EDITOR
            // Mark the ScriptableObject as dirty and save assets
            EditorUtility.SetDirty(currentReactiveSceneSO);
            AssetDatabase.SaveAssets();
    #endif
            // Assign to SceneDataHolder
            SceneDataHolder.currentScene = currentReactiveSceneSO;

            // Load the next scene
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogError("No ReactiveSceneSO assigned.");
        }
    }

    /// <summary>
    /// Creates a new ReactiveSceneSO asset.
    /// </summary>
    /// <returns>The newly created ReactiveSceneSO.</returns>
    private ReactiveSceneSO CreateNewReactiveSceneSO()
    {
        // Generate a unique scene name
        string baseName = "NewReactiveScene";
        string sceneName = baseName;
        int index = 1;
        while (SceneNameExists(sceneName))
        {
            sceneName = baseName + index;
            index++;
        }

        // Create a new ReactiveSceneSO instance
        ReactiveSceneSO newSceneSO = ScriptableObject.CreateInstance<ReactiveSceneSO>();
        newSceneSO.sceneName = sceneName;
        newSceneSO.selectedCharacterPrefabs = new List<GameObject>();
        newSceneSO.reactiveCharacters = new List<ReactiveCharacterSO>();

    #if UNITY_EDITOR
        // Create folder structure: Assets/ReactiveScenes/{SceneName}/Characters/Animations
        string sceneFolderPath = $"{scenesFolderPath}/{sceneName}";
        if (!AssetDatabase.IsValidFolder(sceneFolderPath))
        {
            AssetDatabase.CreateFolder(scenesFolderPath, sceneName);
            AssetDatabase.Refresh();
        }

        string charactersFolderPath = $"{sceneFolderPath}/Characters";
        if (!AssetDatabase.IsValidFolder(charactersFolderPath))
        {
            AssetDatabase.CreateFolder(sceneFolderPath, "Characters");
            AssetDatabase.Refresh();
        }

        // Save the ReactiveSceneSO asset with "SO" suffix
        string soAssetName = $"{sceneName}SO.asset";
        string soAssetPath = $"{sceneFolderPath}/{soAssetName}";
        AssetDatabase.CreateAsset(newSceneSO, soAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    #endif

        // Assign to SceneDataHolder for global access
        SceneDataHolder.currentScene = newSceneSO;

        return newSceneSO;
    }

    /// <summary>
    /// Checks if a scene name already exists.
    /// </summary>
    /// <param name="sceneName">The scene name to check.</param>
    /// <returns>True if the scene name exists; otherwise, false.</returns>
    private bool SceneNameExists(string sceneName)
    {
    #if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:ReactiveSceneSO", new[] { scenesFolderPath });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            ReactiveSceneSO sceneSO = AssetDatabase.LoadAssetAtPath<ReactiveSceneSO>(assetPath);
            if (sceneSO != null && sceneSO.sceneName == sceneName)
            {
                return true;
            }
        }
    #endif
        return false;
    }

    /// <summary>
    /// Loads the selected characters from the current ReactiveSceneSO.
    /// </summary>
    public void LoadSelectedCharacters()
    {
        if (currentReactiveSceneSO != null && currentReactiveSceneSO.selectedCharacterPrefabs != null && !isNewScene)
        {
            selectedCharacters = new List<GameObject>(currentReactiveSceneSO.selectedCharacterPrefabs);
            UpdateSelectedCharactersDropdown();
        }
        else
        {
            selectedCharacters.Clear();
            UpdateSelectedCharactersDropdown();
        }
    }
}
