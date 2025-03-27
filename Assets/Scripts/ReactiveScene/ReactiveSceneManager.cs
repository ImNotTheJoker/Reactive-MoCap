using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Manages the loading and simulation of reactive scenes based on ScriptableObjects.
/// Handles UI interactions for scene selection and simulation control.
/// </summary>
public class ReactiveSceneManager : MonoBehaviour
{
    [Header("UI Elements")]
    /// <summary>
    /// Dropdown UI element for selecting scenes.
    /// </summary>
    public TMP_Dropdown sceneDropdown;

    /// <summary>
    /// Button to load the selected scene.
    /// </summary>
    public Button loadButton;

    /// <summary>
    /// Button to start the simulation.
    /// </summary>
    public Button startSimulationButton;

    /// <summary>
    /// Button to navigate to the previous scene.
    /// </summary>
    public Button previousSceneButton;

    /// <summary>
    /// Button to return to the first scene.
    /// </summary>
    public Button returnToStartButton;

    [Header("Scene Configuration")]
    /// <summary>
    /// Parent transform under which scenes and characters will be instantiated.
    /// </summary>
    public Transform sceneParent;

    /// <summary>
    /// Folder path where ReactiveSceneSO assets are located.
    /// </summary>
    public string sceneFolder = "Assets/ReactiveScenes";

    [Header("Scene Management")]
    /// <summary>
    /// List of loaded ReactiveSceneSO assets.
    /// </summary>
    private List<ReactiveSceneSO> loadedScenes = new List<ReactiveSceneSO>();

    /// <summary>
    /// Currently loaded scene.
    /// </summary>
    private ReactiveSceneSO currentScene;

    /// <summary>
    /// Name of the previously loaded scene.
    /// </summary>
    public string previousSceneName;

    /// <summary>
    /// Name of the first scene to return to.
    /// </summary>
    public string firstSceneName;

    /// <summary>
    /// Initialization of the ReactiveSceneManager.
    /// Loads Scene ScriptableObjects, populates the dropdown, and sets up UI listeners.
    /// </summary>
    void Start()
    {
        LoadSceneSOs();
        PopulateDropdown();
        SetupUIListeners();
    }

    /// <summary>
    /// Updates is called once per frame.
    /// Listens for keyboard shortcuts to load scenes or start simulations.
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            LoadSelectedScene();
        }
    }

    /// <summary>
    /// Loads all ReactiveSceneSO assets from the specified folder.
    /// </summary>
    private void LoadSceneSOs()
    {
#if UNITY_EDITOR
        loadedScenes.Clear();
        string[] guids = AssetDatabase.FindAssets("t:ReactiveSceneSO", new[] { sceneFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ReactiveSceneSO sceneAsset = AssetDatabase.LoadAssetAtPath<ReactiveSceneSO>(path);
            if (sceneAsset != null)
            {
                loadedScenes.Add(sceneAsset);
            }
        }
#endif
    }

    /// <summary>
    /// Populates the scene selection dropdown with loaded scene names.
    /// </summary>
    private void PopulateDropdown()
    {
        sceneDropdown.ClearOptions();
        List<string> sceneNames = new List<string>();
        foreach (var scene in loadedScenes)
        {
            sceneNames.Add(scene.sceneName);
        }
        sceneDropdown.AddOptions(sceneNames);
    }

    /// <summary>
    /// Sets up UI button listeners for scene loading and simulation control.
    /// </summary>
    private void SetupUIListeners()
    {
        loadButton.onClick.AddListener(LoadSelectedScene);
        previousSceneButton.onClick.AddListener(OnBackButtonClicked);
        returnToStartButton.onClick.AddListener(OnReturnToStartButtonClicked);
    }

    /// <summary>
    /// Loads the selected scene by instantiating its environment and characters.
    /// </summary>
    public void LoadSelectedScene()
    {
        if (loadedScenes.Count == 0 || sceneDropdown.value < 0 || sceneDropdown.value >= loadedScenes.Count)
        {
            Debug.LogWarning("No scene available to load.");
            return;
        }

        currentScene = loadedScenes[sceneDropdown.value];

        // Clear existing scene objects
        foreach (Transform child in sceneParent)
        {
            Destroy(child.gameObject);
        }

        List<CharacterEventReceiver> characterReceivers = new List<CharacterEventReceiver>();

        // Instantiate each reactive character
        foreach (var reactiveCharacter in currentScene.reactiveCharacters)
        {
            GameObject characterInstance = Instantiate(
                reactiveCharacter.characterPrefab,
                reactiveCharacter.characterPosition,
                Quaternion.Euler(reactiveCharacter.characterRotation),
                sceneParent
            );

            // Add required components
            var animController = characterInstance.AddComponent<CharacterAnimationControllerExtended>();
            var eventReceiver = characterInstance.AddComponent<CharacterEventReceiver>();
            var lookAtController = characterInstance.AddComponent<LookAtController>();

            // Add and configure CapsuleCollider
            var capsuleCollider = characterInstance.AddComponent<CapsuleCollider>();
            capsuleCollider.center = new Vector3(0, 1, 0); // Adjust height to character
            capsuleCollider.height = 2f; // Set height to match character size

            // Assign interrupters if available
            if (reactiveCharacter.interrupters != null && reactiveCharacter.interrupters.Count > 0)
            {
                eventReceiver.interrupters = reactiveCharacter.interrupters;
                characterReceivers.Add(eventReceiver);
            }
            else
            {
                eventReceiver.interrupters = new List<Interrupter>();
            }

            // Play all main animations
            foreach (var animationClip in reactiveCharacter.mainAnimations)
            {
                animController.PlayMainAnimation(animationClip, 0f);
            }
        }

        // Initialize interrupters for each character
        foreach (var eventReceiver in characterReceivers)
        {
            foreach (var interrupter in eventReceiver.interrupters)
            {
                if (interrupter != null)
                {
                    interrupter.Initialize(eventReceiver);
                }
            }
        }

        Debug.Log("Scene loaded successfully.");
    }

    /// <summary>
    /// Loads the previously loaded scene.
    /// </summary>
    private void OnBackButtonClicked()
    {
        if (string.IsNullOrEmpty(previousSceneName))
        {
            Debug.LogWarning("Previous scene name is not set.");
            return;
        }

        SceneManager.LoadScene(previousSceneName);
    }

    /// <summary>
    /// Returns to the first scene.
    /// </summary>
    private void OnReturnToStartButtonClicked()
    {
        if (string.IsNullOrEmpty(firstSceneName))
        {
            Debug.LogWarning("First scene name is not set.");
            return;
        }

        SceneManager.LoadScene(firstSceneName);
    }
}
