using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneSelectionManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Button newSceneButton;
    public Button existingSceneButton;
    public TMP_Dropdown existingScenesDropdown;

    [Header("Panels")]
    public GameObject characterSelectionPanel;

    [Header("Managers")]
    public CharacterSelectionManager characterSelectionManager;

    // Path to the folder containing ReactiveSceneSO assets
    private string scenesFolderPath = "Assets/ReactiveScenes";

    // List to store existing ReactiveSceneSO assets
    private List<ReactiveSceneSO> existingReactiveScenes = new List<ReactiveSceneSO>();

    void Start()
    {
        // Load existing scenes from the specified folder
        LoadExistingScenes();

        // Populate the dropdown with the loaded scenes
        PopulateExistingScenesDropdown();

        // Assign event listeners to buttons
        newSceneButton.onClick.AddListener(OnNewSceneButtonClicked);
        existingSceneButton.onClick.AddListener(OnExistingSceneButtonClicked);
    }

    /// <summary>
    /// Loads all ReactiveSceneSO assets from the specified folder.
    /// </summary>
    private void LoadExistingScenes()
    {
        existingReactiveScenes.Clear();

#if UNITY_EDITOR
        // Ensure the folder exists; if not, create it
        if (!AssetDatabase.IsValidFolder(scenesFolderPath))
        {
            AssetDatabase.CreateFolder("Assets", "ReactiveScenes");
            AssetDatabase.Refresh();
        }

        // Find all ReactiveSceneSO assets within the folder
        string[] guids = AssetDatabase.FindAssets("t:ReactiveSceneSO", new[] { scenesFolderPath });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            ReactiveSceneSO sceneSO = AssetDatabase.LoadAssetAtPath<ReactiveSceneSO>(assetPath);
            if (sceneSO != null)
            {
                // Check if the asset name ends with "SO.asset"
                if (assetPath.EndsWith("SO.asset"))
                {
                    existingReactiveScenes.Add(sceneSO);
                }
                else
                {
                    Debug.LogWarning($"ReactiveSceneSO found without 'SO' suffix: {assetPath}");
                }
            }
        }
#endif
    }

    /// <summary>
    /// Populates the existing scenes dropdown with loaded scenes.
    /// </summary>
    private void PopulateExistingScenesDropdown()
    {
        existingScenesDropdown.ClearOptions();
        foreach (var scene in existingReactiveScenes)
        {
            existingScenesDropdown.options.Add(new TMP_Dropdown.OptionData(scene.sceneName));
        }
        existingScenesDropdown.RefreshShownValue();
    }

    /// <summary>
    /// Handles the event when the "New Scene" button is clicked.
    /// </summary>
    private void OnNewSceneButtonClicked()
    {
#if UNITY_EDITOR
        // Switch to the character selection panel for a new scene
        characterSelectionManager.SetIsNewScene(true);
        characterSelectionManager.SetCurrentReactiveSceneSO(null);

        characterSelectionPanel.SetActive(true);
        this.gameObject.SetActive(false);
#endif
    }

    /// <summary>
    /// Handles the event when the "Existing Scene" button is clicked.
    /// </summary>
    private void OnExistingSceneButtonClicked()
    {
#if UNITY_EDITOR
        int selectedIndex = existingScenesDropdown.value;
        if (selectedIndex >= 0 && selectedIndex < existingReactiveScenes.Count)
        {
            ReactiveSceneSO selectedSceneSO = existingReactiveScenes[selectedIndex];

            // Switch to the character selection panel with the selected scene
            characterSelectionManager.SetIsNewScene(false);
            characterSelectionManager.SetCurrentReactiveSceneSO(selectedSceneSO);

            // Assign the selected scene to the SceneDataHolder
            SceneDataHolder.currentScene = selectedSceneSO;

            characterSelectionPanel.SetActive(true);
            this.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("No valid scene selected.");
        }
#endif
    }
}
