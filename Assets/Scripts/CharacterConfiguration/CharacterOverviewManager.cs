using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Manages the character overview interface, including animation and interrupter configurations.
/// Handles UI interactions for adding/removing animations and interrupters, and manages the transition between panels.
/// </summary>
public class CharacterOverviewManager : MonoBehaviour
{
    [Header("UI Elements")]
    /// <summary>
    /// The Back button in the UI.
    /// </summary>
    public Button backButton;

    /// <summary>
    /// The Save button in the UI.
    /// </summary>
    public Button saveButton;

    /// <summary>
    /// Text element displaying instructions to the user.
    /// </summary>
    public TextMeshProUGUI instructionsText;

    /// <summary>
    /// Text element displaying the character's name.
    /// </summary>
    public TextMeshProUGUI characterNameText;

    [Header("Main Animations UI")]
    /// <summary>
    /// Dropdown for selecting animations to add to the character.
    /// </summary>
    public TMP_Dropdown animationsDropdown;

    /// <summary>
    /// Button to add the selected animation to the character.
    /// </summary>
    public Button addAnimationButton;

    /// <summary>
    /// Scroll view containing the list of main animations assigned to the character.
    /// </summary>
    public ScrollRect mainAnimationsScrollView;

    /// <summary>
    /// Content transform of the main animations scroll view.
    /// </summary>
    private Transform mainAnimationsContent;

    /// <summary>
    /// Prefab used to create animation buttons in the scroll view.
    /// </summary>
    public GameObject animationButtonPrefab;

    [Header("Interrupters UI")]
    /// <summary>
    /// Button to add a new interrupter to the character.
    /// </summary>
    public Button addInterrupterButton;

    /// <summary>
    /// Scroll view containing the list of interrupters assigned to the character.
    /// </summary>
    public ScrollRect interruptersScrollView;

    /// <summary>
    /// Content transform of the interrupters scroll view.
    /// </summary>
    private Transform interruptersContent;

    /// <summary>
    /// Prefab used to create interrupter buttons in the scroll view.
    /// </summary>
    public GameObject interrupterButtonPrefab;

    [Header("Panels")]
    /// <summary>
    /// Panel for character selection.
    /// </summary>
    public GameObject characterSelectionPanel;

    /// <summary>
    /// Panel displaying the character overview.
    /// </summary>
    public GameObject characterOverviewPanel;

    /// <summary>
    /// Panel for selecting the type of interrupter to add.
    /// </summary>
    public GameObject interrupterTypeSelectionPanel;

    /// <summary>
    /// Instance of the character being configured.
    /// </summary>
    private GameObject characterInstance;

    /// <summary>
    /// The current ReactiveCharacterSO containing character-specific data.
    /// </summary>
    private ReactiveCharacterSO currentCharacterSO;

    /// <summary>
    /// The current ReactiveSceneSO containing scene-specific data.
    /// </summary>
    private ReactiveSceneSO currentSceneSO;

    // Lists to manage animations and interrupters
    /// <summary>
    /// List of selected AnimationClips assigned to the character.
    /// </summary>
    private List<AnimationClip> selectedAnimations = new List<AnimationClip>();

    /// <summary>
    /// List of selected Interrupters assigned to the character.
    /// </summary>
    private List<Interrupter> selectedInterrupters = new List<Interrupter>();

    /// <summary>
    /// Reference to the CharacterListManager.
    /// </summary>
    private CharacterListManager characterListManager;

    [Header("Interrupter Configuration UI")]
    /// <summary>
    /// Dropdown for selecting interrupters to configure.
    /// </summary>
    public TMP_Dropdown interrupterConfigureDropdown;

    /// <summary>
    /// Button to initiate the configuration of the selected interrupter.
    /// </summary>
    public Button configureInterrupterButton;

    [Header("Interrupter Configuration Managers")]
    /// <summary>
    /// Manager for configuring ProximityInterrupters.
    /// </summary>
    public ProximityInterrupterConfigManager proximityInterrupterConfigManager;

    /// <summary>
    /// Manager for configuring AudioInterrupters.
    /// </summary>
    public AudioInterrupterConfigManager audioInterrupterConfigManager;

    /// <summary>
    /// Manager for configuring VelocityInterrupters.
    /// </summary>
    public VelocityInterrupterConfigManager velocityInterrupterConfigManager;

    /// <summary>
    /// Manager for configuring LookAtInterrupters.
    /// </summary>
    public LookAtInterrupterConfigManager lookAtInterrupterConfigManager;

    [Header("Interrupter Configuration Panels")]
    /// <summary>
    /// Panel for configuring ProximityInterrupters.
    /// </summary>
    public GameObject proximityInterrupterConfigPanel;

    /// <summary>
    /// Panel for configuring AudioInterrupters.
    /// </summary>
    public GameObject audioInterrupterConfigPanel;

    /// <summary>
    /// Panel for configuring VelocityInterrupters.
    /// </summary>
    public GameObject velocityInterrupterConfigPanel;

    /// <summary>
    /// Panel for configuring LookAtInterrupters.
    /// </summary>
    public GameObject lookAtInterrupterConfigPanel;

    /// <summary>
    /// Initializes references to necessary managers.
    /// </summary>
    void Awake()
    {
        characterListManager = FindObjectOfType<CharacterListManager>();
        if (characterListManager == null)
        {
            Debug.LogError("CharacterListManager not found in the scene.");
        }
    }

    /// <summary>
    /// Called when the script is enabled. Initializes the UI.
    /// </summary>
    void OnEnable()
    {
        InitializeUI();
    }

    /// <summary>
    /// Initializes the UI elements with current character and scene data.
    /// </summary>
    private void InitializeUI()
    {
        // Verify that all necessary UI elements are assigned
        if (backButton == null || saveButton == null ||
            characterNameText == null ||
            animationsDropdown == null || addAnimationButton == null ||
            mainAnimationsScrollView == null || addInterrupterButton == null ||
            interruptersScrollView == null || characterSelectionPanel == null || characterOverviewPanel == null ||
            animationButtonPrefab == null || interrupterButtonPrefab == null)
        {
            Debug.LogError("UI elements are not correctly assigned in the Inspector.");
            return;
        }

        // Set up event listeners (ensure they are not duplicated)
        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(OnBackButtonClicked);
        saveButton.onClick.RemoveAllListeners();
        saveButton.onClick.AddListener(OnSaveButtonClicked);
        addAnimationButton.onClick.RemoveAllListeners();
        addAnimationButton.onClick.AddListener(OnAddAnimationClicked);
        addInterrupterButton.onClick.RemoveAllListeners();
        addInterrupterButton.onClick.AddListener(OnAddInterrupterClicked);
        configureInterrupterButton.onClick.RemoveAllListeners();
        configureInterrupterButton.onClick.AddListener(OnConfigureInterrupterClicked);

        // Reference to the content areas of the ScrollViews
        mainAnimationsContent = mainAnimationsScrollView.content;
        interruptersContent = interruptersScrollView.content;

        // Load the current ReactiveSceneSO from SceneDataHolder
        currentSceneSO = SceneDataHolder.currentScene;

        if (currentSceneSO == null)
        {
            Debug.LogError("No ReactiveSceneSO assigned. Ensure that SceneDataHolder.currentScene is set.");
            return;
        }

        // Load the character to configure from SceneDataHolder
        characterInstance = SceneDataHolder.selectedCharacterInstance;
        currentCharacterSO = SceneDataHolder.selectedCharacterSO;

        if (characterInstance == null)
        {
            Debug.LogError("No Character Instance found. Ensure that SceneDataHolder.selectedCharacterInstance is set.");
            return;
        }

        // Extract the clean character name without "(Clone)"
        string rawCharacterName = characterInstance.name;
        string characterName = StripCloneSuffix(rawCharacterName);
        characterNameText.text = characterName;

#if UNITY_EDITOR
        if (currentCharacterSO != null)
        {
            selectedAnimations = new List<AnimationClip>(currentCharacterSO.mainAnimations);
            selectedInterrupters = new List<Interrupter>(currentCharacterSO.interrupters);
        }
        else
        {
            // If ReactiveCharacterSO does not exist, initialize the lists
            selectedAnimations = new List<AnimationClip>();
            selectedInterrupters = new List<Interrupter>();
        }

        RefreshInterruptersScrollView();
        PopulateAnimationsDropdown();
        PopulateInterrupterConfigureDropdown();

        RefreshMainAnimationsScrollView();
        RefreshInterruptersScrollView();
#endif
    }

    /// <summary>
    /// Removes the "(Clone)" suffix from a GameObject name if present.
    /// </summary>
    /// <param name="name">The name of the GameObject.</param>
    /// <returns>The cleaned name without the "(Clone)" suffix.</returns>
    private string StripCloneSuffix(string name)
    {
        if (name.EndsWith("(Clone)"))
        {
            return name.Substring(0, name.Length - 7).Trim();
        }
        return name;
    }

    /// <summary>
    /// Populates the Animations Dropdown with available animation clips.
    /// </summary>
    private void PopulateAnimationsDropdown()
    {
#if UNITY_EDITOR
        // Path to the Animations folder of the character
        string sceneName = currentSceneSO.sceneName;
        string characterName = characterNameText.text;
        string animationsFolderPath = $"Assets/ReactiveScenes/{sceneName}/Characters/{characterName}/Animations";

        // Check if the Animations folder exists
        if (!AssetDatabase.IsValidFolder(animationsFolderPath))
        {
            Debug.LogWarning($"Animations folder does not exist: {animationsFolderPath}");
            animationsDropdown.ClearOptions();
            return;
        }

        // Find all AnimationClips in this folder
        string[] animationGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { animationsFolderPath });

        List<string> animationNames = new List<string>();
        foreach (string guid in animationGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null)
            {
                animationNames.Add(clip.name);
            }
        }

        // Clear and populate the Dropdown
        animationsDropdown.ClearOptions();
        animationsDropdown.AddOptions(animationNames);
#endif
    }

    /// <summary>
    /// Populates the Interrupter Configure Dropdown with available interrupters.
    /// </summary>
    public void PopulateInterrupterConfigureDropdown()
    {
#if UNITY_EDITOR
        interrupterConfigureDropdown.ClearOptions();
        List<string> interrupterNames = new List<string>();
        foreach (var interrupter in selectedInterrupters)
        {
            interrupterNames.Add(interrupter.interrupterName);
        }
        interrupterConfigureDropdown.AddOptions(interrupterNames);
#endif
    }

    /// <summary>
    /// Handles the Configure Interrupter button click event.
    /// Opens the appropriate configuration panel based on the selected interrupter type.
    /// </summary>
    private void OnConfigureInterrupterClicked()
    {
#if UNITY_EDITOR
        if (interrupterConfigureDropdown.options.Count == 0)
        {
            Debug.LogWarning("No interrupters available for configuration.");
            return;
        }

        string selectedInterrupterName = interrupterConfigureDropdown.options[interrupterConfigureDropdown.value].text;
        Interrupter selectedInterrupter = selectedInterrupters.Find(i => i.interrupterName == selectedInterrupterName);

        if (selectedInterrupter == null)
        {
            Debug.LogError($"Interrupter with name {selectedInterrupterName} not found.");
            return;
        }

        // Disable the CharacterOverviewPanel
        characterOverviewPanel.SetActive(false);

        // Determine the interrupter type and open the corresponding panel
        if (selectedInterrupter is ProximityInterrupter proximityInterrupter)
        {
            OpenProximityInterrupterConfigPanel(proximityInterrupter);
        }
        else if (selectedInterrupter is AudioInterrupter audioInterrupter)
        {
            OpenAudioInterrupterConfigPanel(audioInterrupter);
        }
        else if (selectedInterrupter is VelocityInterrupter velocityInterrupter)
        {
            OpenVelocityInterrupterConfigPanel(velocityInterrupter);
        }
        else if (selectedInterrupter is LookAtInterrupter lookAtInterrupter)
        {
            OpenLookAtInterrupterConfigPanel(lookAtInterrupter);
        }
        else
        {
            Debug.LogError($"Unknown interrupter type for {selectedInterrupter.interrupterName}.");
        }
#endif
    }

    /// <summary>
    /// Opens the ProximityInterrupter configuration panel.
    /// </summary>
    /// <param name="interrupter">The ProximityInterrupter to configure.</param>
    private void OpenProximityInterrupterConfigPanel(ProximityInterrupter interrupter)
    {
        interrupterTypeSelectionPanel.SetActive(false);
        proximityInterrupterConfigPanel.SetActive(true);
        proximityInterrupterConfigManager.ConfigureInterrupter(interrupter);
    }

    /// <summary>
    /// Opens the AudioInterrupter configuration panel.
    /// </summary>
    /// <param name="interrupter">The AudioInterrupter to configure.</param>
    private void OpenAudioInterrupterConfigPanel(AudioInterrupter interrupter)
    {
        interrupterTypeSelectionPanel.SetActive(false);
        audioInterrupterConfigPanel.SetActive(true);
        audioInterrupterConfigManager.ConfigureInterrupter(interrupter);
    }

    /// <summary>
    /// Opens the VelocityInterrupter configuration panel.
    /// </summary>
    /// <param name="interrupter">The VelocityInterrupter to configure.</param>
    private void OpenVelocityInterrupterConfigPanel(VelocityInterrupter interrupter)
    {
        interrupterTypeSelectionPanel.SetActive(false);
        velocityInterrupterConfigPanel.SetActive(true);
        velocityInterrupterConfigManager.ConfigureInterrupter(interrupter);
    }

    /// <summary>
    /// Opens the LookAtInterrupter configuration panel.
    /// </summary>
    /// <param name="interrupter">The LookAtInterrupter to configure.</param>
    private void OpenLookAtInterrupterConfigPanel(LookAtInterrupter interrupter)
    {
        interrupterTypeSelectionPanel.SetActive(false);
        lookAtInterrupterConfigPanel.SetActive(true);
        lookAtInterrupterConfigManager.ConfigureInterrupter(interrupter);
    }

    /// <summary>
    /// Handles the Add Animation button click event.
    /// Adds the selected animation to the character's animation list.
    /// </summary>
    private void OnAddAnimationClicked()
    {
#if UNITY_EDITOR
        if (animationsDropdown.options.Count == 0)
        {
            Debug.LogWarning("Animations dropdown is empty. No animations available to add.");
            return;
        }

        string selectedAnimationName = animationsDropdown.options[animationsDropdown.value].text;
        if (string.IsNullOrEmpty(selectedAnimationName))
        {
            Debug.LogWarning("No animation selected.");
            return;
        }

        // Path to the selected animation
        string sceneName = currentSceneSO.sceneName;
        string characterName = characterNameText.text;
        string animationsPath = $"Assets/ReactiveScenes/{sceneName}/Characters/{characterName}/Animations/{selectedAnimationName}.anim";
        AnimationClip selectedAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>(animationsPath);

        if (selectedAnimation == null)
        {
            Debug.LogError($"AnimationClip not found: {animationsPath}");
            return;
        }

        // Add to the list if not already present
        if (!selectedAnimations.Contains(selectedAnimation))
        {
            selectedAnimations.Add(selectedAnimation);
            RefreshMainAnimationsScrollView();
            SaveMainAnimations();
        }
        else
        {
            Debug.LogWarning("Animation already added.");
        }
#endif
    }

    /// <summary>
    /// Saves the main animations to the ReactiveCharacterSO.
    /// </summary>
    private void SaveMainAnimations()
    {
#if UNITY_EDITOR
        if (currentCharacterSO == null)
        {
            Debug.LogError("No ReactiveCharacterSO available to save.");
            return;
        }

        // Update the main animations in the ScriptableObject
        currentCharacterSO.mainAnimations = new List<AnimationClip>(selectedAnimations);

        // Mark the ScriptableObject as dirty to detect changes
        EditorUtility.SetDirty(currentCharacterSO);

        // Save the changes
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
#endif
    }

    /// <summary>
    /// Refreshes the Main Animations ScrollView to display the current list of selected animations.
    /// </summary>
    private void RefreshMainAnimationsScrollView()
    {
#if UNITY_EDITOR
        // Remove existing buttons
        foreach (Transform child in mainAnimationsContent)
        {
            Destroy(child.gameObject);
        }

        // Create buttons for each selected animation
        foreach (var anim in selectedAnimations)
        {
            GameObject animButtonObj = Instantiate(animationButtonPrefab, mainAnimationsContent);
            animButtonObj.name = anim.name + "Button";

            // Set the button text
            TextMeshProUGUI buttonText = animButtonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = anim.name;
            }

            // Set up the button to remove the animation
            Button animButton = animButtonObj.GetComponent<Button>();
            if (animButton != null)
            {
                AnimationClip animCopy = anim; // Local copy for the listener
                animButton.onClick.AddListener(() => OnRemoveAnimationClicked(animCopy, animButtonObj));
            }
        }
#endif
    }

    /// <summary>
    /// Handles the removal of an animation when its corresponding button is clicked.
    /// </summary>
    /// <param name="anim">The AnimationClip to remove.</param>
    /// <param name="buttonObj">The button GameObject that was clicked.</param>
    private void OnRemoveAnimationClicked(AnimationClip anim, GameObject buttonObj)
    {
#if UNITY_EDITOR
        if (selectedAnimations.Contains(anim))
        {
            selectedAnimations.Remove(anim);
            Destroy(buttonObj);
            SaveMainAnimations();
        }
#endif
    }

    /// <summary>
    /// Handles the Add Interrupter button click event.
    /// Opens the InterrupterTypeSelectionPanel for the user to select the interrupter type.
    /// </summary>
    private void OnAddInterrupterClicked()
    {
        // Switch to the InterrupterTypeSelectionPanel
        characterOverviewPanel.SetActive(false);
        interrupterTypeSelectionPanel.SetActive(true);
    }

    /// <summary>
    /// Creates a new Interrupter asset in the project.
    /// </summary>
    /// <param name="name">The name of the new Interrupter.</param>
    /// <returns>The created Interrupter ScriptableObject.</returns>
    private Interrupter CreateInterrupter(string name)
    {
#if UNITY_EDITOR
        string sceneName = currentSceneSO.sceneName;
        string characterName = characterNameText.text;
        string interrupterFolderPath = $"Assets/ReactiveScenes/{sceneName}/Characters/{characterName}/Interrupters";

        // Ensure the interrupter folder exists
        if (!AssetDatabase.IsValidFolder(interrupterFolderPath))
        {
            AssetDatabase.CreateFolder($"Assets/ReactiveScenes/{sceneName}/Characters/{characterName}", "Interrupters");
        }

        // Create a new Interrupter
        Interrupter interrupter = ScriptableObject.CreateInstance<Interrupter>();
        interrupter.interrupterName = name;

        // Save the Interrupter asset
        string interrupterAssetPath = $"{interrupterFolderPath}/{name}.asset";
        AssetDatabase.CreateAsset(interrupter, interrupterAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return interrupter;
#else
        Debug.LogError("Interrupters can only be created in the Editor.");
        return null;
#endif
    }

    /// <summary>
    /// Refreshes the Interrupters ScrollView to display the current list of selected interrupters.
    /// </summary>
    public void RefreshInterruptersScrollView()
    {
        // Remove existing buttons
        foreach (Transform child in interruptersContent)
        {
            Destroy(child.gameObject);
        }

        // Create buttons for each interrupter
        foreach (var interrupter in currentCharacterSO.interrupters)
        {
            GameObject interrupterButtonObj = Instantiate(interrupterButtonPrefab, interruptersContent);
            interrupterButtonObj.name = interrupter.interrupterName + "Button";

            // Set the button text
            TextMeshProUGUI buttonText = interrupterButtonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = interrupter.interrupterName;
            }

            // Set up the button to remove the interrupter
            Button interrupterButton = interrupterButtonObj.GetComponent<Button>();
            if (interrupterButton != null)
            {
                Interrupter interrupterCopy = interrupter; // Local copy for the listener
                interrupterButton.onClick.AddListener(() => OnRemoveInterrupterClicked(interrupterCopy, interrupterButtonObj));
            }
        }
    }

    /// <summary>
    /// Handles the removal of an interrupter when its corresponding button is clicked.
    /// </summary>
    /// <param name="interrupter">The Interrupter to remove.</param>
    /// <param name="buttonObj">The button GameObject that was clicked.</param>
    private void OnRemoveInterrupterClicked(Interrupter interrupter, GameObject buttonObj)
    {
#if UNITY_EDITOR
        if (selectedInterrupters.Contains(interrupter))
        {
            selectedInterrupters.Remove(interrupter);
            Destroy(buttonObj);
        }
        else
        {
            Debug.LogWarning($"Interrupter {interrupter.interrupterName} is not in the selected interrupters list.");
        }
#endif
    }

    /// <summary>
    /// Handles the Back button click event by navigating back to the CharacterSelectionPanel.
    /// </summary>
    private void OnBackButtonClicked()
    {
#if UNITY_EDITOR
        // Switch back to the CharacterSelectionPanel
        characterOverviewPanel.SetActive(false);
        characterSelectionPanel.SetActive(true);

        // Reset SceneDataHolder variables
        SceneDataHolder.selectedCharacterInstance = null;
        SceneDataHolder.selectedCharacterSO = null;
        SceneDataHolder.currentCharacterName = null;
        SceneDataHolder.selectedCharacterPrefab = null;

        // Update the CharacterListManager's ScrollView
        if (characterListManager != null)
        {
            characterListManager.LoadConfiguredCharacters();
            characterListManager.RefreshConfiguredCharactersScrollView();
        }
        else
        {
            Debug.LogError("CharacterListManager is not assigned.");
        }
#endif
    }

    /// <summary>
    /// Handles the Save button click event by saving the current configuration of the character.
    /// </summary>
    private void OnSaveButtonClicked()
    {
#if UNITY_EDITOR
        if (characterInstance == null)
        {
            Debug.LogError("No character found to save.");
            return;
        }

        // Capture the current position and rotation of the character
        Vector3 characterPosition = characterInstance.transform.position;
        Vector3 characterRotation = characterInstance.transform.eulerAngles;

        // Capture the name of the character
        string characterName = characterNameText.text.Trim();
        if (string.IsNullOrEmpty(characterName))
        {
            Debug.LogError("Character name cannot be empty.");
            return;
        }

        // Path to the character's folder
        string sceneName = currentSceneSO.sceneName;
        string characterFolderPath = $"Assets/ReactiveScenes/{sceneName}/Characters/{characterName}";

        // Ensure the character folder exists
        if (!AssetDatabase.IsValidFolder(characterFolderPath))
        {
            AssetDatabase.CreateFolder($"Assets/ReactiveScenes/{sceneName}/Characters", characterName);
        }

        // Path to the ReactiveCharacterSO
        string soPath = $"{characterFolderPath}/{characterName}SO.asset";

        // Check if ReactiveCharacterSO exists
        ReactiveCharacterSO existingSO = AssetDatabase.LoadAssetAtPath<ReactiveCharacterSO>(soPath);

        if (existingSO == null)
        {
            // Create a new ReactiveCharacterSO
            currentCharacterSO = CreateReactiveCharacterSO(characterName, characterPosition, characterRotation);
            if (currentCharacterSO == null)
            {
                Debug.LogError("Error creating ReactiveCharacterSO.");
                return;
            }
            AssetDatabase.CreateAsset(currentCharacterSO, soPath);
        }
        else
        {
            // Update the existing ReactiveCharacterSO
            existingSO.characterName = characterName;
            existingSO.characterPrefab = SceneDataHolder.selectedCharacterPrefab; // Ensure the prefab is set
            existingSO.characterPosition = characterPosition;
            existingSO.characterRotation = characterRotation;

            // Find interrupters that have been removed
            List<Interrupter> interruptersToDelete = new List<Interrupter>();
            foreach (var interrupter in existingSO.interrupters)
            {
                if (!selectedInterrupters.Contains(interrupter))
                {
                    interruptersToDelete.Add(interrupter);
                }
            }

            // Delete removed interrupters from the filesystem
            foreach (var interrupter in interruptersToDelete)
            {
                string assetPath = AssetDatabase.GetAssetPath(interrupter);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
                else
                {
                    Debug.LogWarning($"Asset path for {interrupter.interrupterName} could not be found.");
                }
            }

            // Update the interrupters list
            existingSO.mainAnimations = new List<AnimationClip>(selectedAnimations);
            existingSO.interrupters = new List<Interrupter>(selectedInterrupters);

            EditorUtility.SetDirty(existingSO);
            currentCharacterSO = existingSO;
        }

        // Save the ScriptableObject
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Add the ReactiveCharacterSO to the ReactiveSceneSO list if not already present
        if (!currentSceneSO.reactiveCharacters.Contains(currentCharacterSO))
        {
            currentSceneSO.AddCharacter(currentCharacterSO);
        }

        // Update SceneDataHolder.selectedCharacterSO
        SceneDataHolder.selectedCharacterSO = currentCharacterSO;

        // Update the CharacterListManager's ScrollView
        if (characterListManager != null)
        {
            characterListManager.LoadConfiguredCharacters();
            characterListManager.RefreshConfiguredCharactersScrollView();
        }
        else
        {
            Debug.LogError("CharacterListManager is not assigned.");
        }

        // Switch back to the CharacterSelectionPanel
        characterOverviewPanel.SetActive(false);
        characterSelectionPanel.SetActive(true);

        // Reset SceneDataHolder variables
        SceneDataHolder.selectedCharacterInstance = null;
        SceneDataHolder.selectedCharacterSO = null;
        SceneDataHolder.currentCharacterName = null;
        SceneDataHolder.selectedCharacterPrefab = null;
#endif
    }

    /// <summary>
    /// Creates a new ReactiveCharacterSO with the specified parameters.
    /// </summary>
    /// <param name="name">The name of the character.</param>
    /// <param name="pos">The position of the character.</param>
    /// <param name="rot">The rotation of the character.</param>
    /// <returns>The created ReactiveCharacterSO.</returns>
    private ReactiveCharacterSO CreateReactiveCharacterSO(string name, Vector3 pos, Vector3 rot)
    {
#if UNITY_EDITOR
        ReactiveCharacterSO characterSO = ScriptableObject.CreateInstance<ReactiveCharacterSO>();

        GameObject characterPrefab = SceneDataHolder.selectedCharacterPrefab;
        if (characterPrefab == null)
        {
            Debug.LogError("No character prefab found. Cannot create ReactiveCharacterSO.");
            return null;
        }

        characterSO.Initialize(name, characterPrefab, pos, rot);
        characterSO.mainAnimations = new List<AnimationClip>(selectedAnimations);
        characterSO.interrupters = new List<Interrupter>(selectedInterrupters);
        return characterSO;
#else
        return null;
#endif
    }
}
