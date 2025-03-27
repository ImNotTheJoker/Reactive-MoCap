using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Abstract base class for managing the configuration of different interrupter types.
/// Handles common UI interactions and setup required for interrupter configuration.
/// </summary>
public abstract class InterrupterConfigManager : MonoBehaviour
{
    [Header("Common UI Elements")]
    /// <summary>
    /// Dropdown for selecting the maximum distance.
    /// </summary>
    public TMP_Dropdown maxDistanceDropdown;

    /// <summary>
    /// Dropdown for selecting the minimum duration.
    /// </summary>
    public TMP_Dropdown minDurationDropdown;

    /// <summary>
    /// Dropdown for selecting the priority level.
    /// </summary>
    public TMP_Dropdown priorityDropdown;

    /// <summary>
    /// Scroll view for displaying interrupter sources.
    /// </summary>
    public ScrollRect interrupterSourcesScrollView;

    /// <summary>
    /// Content transform for the interrupter sources scroll view.
    /// </summary>
    protected Transform sourcesContent;

    /// <summary>
    /// Dropdown for selecting the source type.
    /// </summary>
    public TMP_Dropdown sourceDropdown;

    /// <summary>
    /// Button to add a new source.
    /// </summary>
    public Button addSourceButton;

    /// <summary>
    /// Prefab for creating source buttons.
    /// </summary>
    public GameObject sourceButtonPrefab;

    [Header("Reactive Animations UI")]
    /// <summary>
    /// Scroll view for displaying reactive animations.
    /// </summary>
    public ScrollRect reactiveAnimationsScrollView;

    /// <summary>
    /// Content transform for the reactive animations scroll view.
    /// </summary>
    protected Transform animationsContent;

    /// <summary>
    /// Dropdown for selecting animations.
    /// </summary>
    public TMP_Dropdown animationsDropdown;

    /// <summary>
    /// Button to add a new reactive animation.
    /// </summary>
    public Button addReactiveAnimationButton;

    /// <summary>
    /// Prefab for creating animation buttons.
    /// </summary>
    public GameObject animationButtonPrefab;

    [Header("Navigation Buttons")]
    /// <summary>
    /// Button to navigate back to the previous panel.
    /// </summary>
    public Button backButton;

    /// <summary>
    /// Button to create or finalize the interrupter configuration.
    /// </summary>
    public Button createButton;

    [Header("Panels")]
    /// <summary>
    /// Panel for selecting interrupter types.
    /// </summary>
    public GameObject interrupterTypeSelectionPanel;

    /// <summary>
    /// Panel for configuring the selected interrupter.
    /// </summary>
    public GameObject interrupterConfigPanel;

    /// <summary>
    /// Reference to the current ReactiveCharacter ScriptableObject.
    /// </summary>
    protected ReactiveCharacterSO currentCharacterSO;

    /// <summary>
    /// Reference to the current ReactiveScene ScriptableObject.
    /// </summary>
    protected ReactiveSceneSO currentSceneSO;

    /// <summary>
    /// List of selected interrupter sources.
    /// </summary>
    protected List<string> selectedSources = new List<string>();

    /// <summary>
    /// List of selected animations.
    /// </summary>
    protected List<AnimationClip> selectedAnimations = new List<AnimationClip>();

    [Header("Manager References")]
    /// <summary>
    /// Reference to the CharacterOverviewManager.
    /// </summary>
    public CharacterOverviewManager characterOverviewManager; // Assign via Inspector

    [Header("Replay Functionality")]
    /// <summary>
    /// Button to initiate replay of animations.
    /// </summary>
    public Button replayButton;

    /// <summary>
    /// Button to stop replay of animations.
    /// </summary>
    public Button stopReplayButton;

    /// <summary>
    /// Reference to the AnimationReplayer component.
    /// </summary>
    public AnimationReplayer animationReplayer; // Assign via Inspector

    /// <summary>
    /// Parent transform for the replayed character avatar.
    /// </summary>
    public Transform replayCharacterParent; // Assign via Inspector

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes references and sets up event listeners.
    /// </summary>
    protected virtual void OnEnable()
    {
#if UNITY_EDITOR
        // Load the current scene and character data
        currentSceneSO = SceneDataHolder.currentScene;
        currentCharacterSO = SceneDataHolder.selectedCharacterSO;

        if (currentSceneSO == null || currentCharacterSO == null)
        {
            Debug.LogError("Scene or Character not found.");
            return;
        }

        // Get content transforms for scroll views
        sourcesContent = interrupterSourcesScrollView.content;
        animationsContent = reactiveAnimationsScrollView.content;

        // Initialize lists
        selectedSources = new List<string>();
        selectedAnimations = new List<AnimationClip>();

        // Populate dropdowns
        PopulateDistanceDropdowns();
        PopulatePriorityDropdown();
        PopulateSourceDropdown();
        PopulateAnimationsDropdown();

        // Remove existing listeners and add new ones
        addSourceButton.onClick.RemoveAllListeners();
        addReactiveAnimationButton.onClick.RemoveAllListeners();
        backButton.onClick.RemoveAllListeners();
        createButton.onClick.RemoveAllListeners();

        addSourceButton.onClick.AddListener(OnAddSourceClicked);
        addReactiveAnimationButton.onClick.AddListener(OnAddAnimationClicked);
        backButton.onClick.AddListener(OnBackButtonClicked);
        createButton.onClick.AddListener(OnCreateButtonClicked);

        // Setup Replay Button listeners
        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(OnReplayButtonClicked);
        }

        if (stopReplayButton != null)
        {
            stopReplayButton.onClick.RemoveAllListeners();
            stopReplayButton.onClick.AddListener(OnStopReplayClicked);
            stopReplayButton.interactable = false; // Initially disabled
        }
#endif
    }

    protected virtual void Start(){}

    /// <summary>
    /// Populates the distance-related dropdowns with options.
    /// </summary>
    protected void PopulateDistanceDropdowns()
    {
        maxDistanceDropdown.ClearOptions();
        minDurationDropdown.ClearOptions();

        List<string> options = new List<string>();
        for (int i = 0; i <= 10; i++)
        {
            options.Add(i.ToString());
        }
        maxDistanceDropdown.AddOptions(options);
        minDurationDropdown.AddOptions(options);
    }

    /// <summary>
    /// Populates the priority dropdown with options.
    /// </summary>
    protected void PopulatePriorityDropdown()
    {
        priorityDropdown.ClearOptions();

        List<string> options = new List<string>();
        for (int i = 0; i <= 10; i++)
        {
            options.Add(i.ToString());
        }
        priorityDropdown.AddOptions(options);
    }

    /// <summary>
    /// Populates the source dropdown with available character sources.
    /// </summary>
    protected void PopulateSourceDropdown()
    {
        sourceDropdown.ClearOptions();
        sourceDropdown.AddOptions(new List<string> { "User" });

        List<string> characterNames = new List<string>();

        // Add all other characters in the scene
        foreach (var character in currentSceneSO.reactiveCharacters)
        {
            if (character.characterName != currentCharacterSO.characterName)
            {
                characterNames.Add(character.characterName + "(Clone)");
            }
        }
        sourceDropdown.AddOptions(characterNames);
    }

    /// <summary>
    /// Populates the animations dropdown with available animations.
    /// </summary>
    protected void PopulateAnimationsDropdown()
    {
#if UNITY_EDITOR
        animationsDropdown.ClearOptions();

        List<string> animationNames = new List<string>();

        // Path to the character's animations folder
        string sceneName = currentSceneSO.sceneName;
        string characterName = currentCharacterSO.characterName;
        string animationsFolderPath = $"Assets/ReactiveScenes/{sceneName}/Characters/{characterName}/Animations";

        // Check if the animations folder exists
        if (!AssetDatabase.IsValidFolder(animationsFolderPath))
        {
            Debug.LogWarning($"Animations folder does not exist: {animationsFolderPath}");
            return;
        }

        // Find all AnimationClips in the folder
        string[] animationGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { animationsFolderPath });

        foreach (string guid in animationGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null)
            {
                animationNames.Add(clip.name);
            }
        }

        if (animationNames.Count == 0)
        {
            Debug.LogWarning($"No animations found in {animationsFolderPath}");
        }

        animationsDropdown.AddOptions(animationNames);
#endif
    }

    /// <summary>
    /// Handler for the Add Source button click event.
    /// Adds the selected source to the list of selected sources.
    /// </summary>
    protected void OnAddSourceClicked()
    {
        if (sourceDropdown == null)
        {
            Debug.LogError("sourceDropdown is not assigned.");
            return;
        }

        if (sourceDropdown.options.Count == 0)
        {
            Debug.LogWarning("sourceDropdown has no options.");
            return;
        }

        string selectedSource = sourceDropdown.options[sourceDropdown.value].text;
        if (!selectedSources.Contains(selectedSource))
        {
            selectedSources.Add(selectedSource);
            RefreshSourcesScrollView();
        }
        else
        {
            Debug.LogWarning($"Source {selectedSource} is already in the list.");
        }
    }

    /// <summary>
    /// Refreshes the sources scroll view to display the current list of selected sources.
    /// </summary>
    protected void RefreshSourcesScrollView()
    {
        if (sourcesContent == null)
        {
            Debug.LogError("sourcesContent is not assigned.");
            return;
        }

#if UNITY_EDITOR
        // Remove existing buttons
        foreach (Transform child in sourcesContent)
        {
            Destroy(child.gameObject);
        }

        // Create buttons for each selected source
        foreach (var source in selectedSources)
        {
            GameObject sourceButtonObj = Instantiate(sourceButtonPrefab, sourcesContent);
            sourceButtonObj.name = source + "Button";

            // Set button text
            TextMeshProUGUI buttonText = sourceButtonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = source;
            }
            else
            {
                Debug.LogError("TextMeshProUGUI component not found in sourceButtonPrefab.");
            }

            // Assign remove listener
            Button sourceButton = sourceButtonObj.GetComponent<Button>();
            if (sourceButton != null)
            {
                string sourceCopy = source; // Local copy for the listener
                sourceButton.onClick.AddListener(() => OnRemoveSourceClicked(sourceCopy, sourceButtonObj));
            }
            else
            {
                Debug.LogError("Button component not found in sourceButtonPrefab.");
            }
        }
#endif
    }

    /// <summary>
    /// Handler for removing a source from the selected sources list.
    /// </summary>
    /// <param name="source">The source to remove.</param>
    /// <param name="buttonObj">The button GameObject associated with the source.</param>
    protected void OnRemoveSourceClicked(string source, GameObject buttonObj)
    {
        if (selectedSources.Contains(source))
        {
            selectedSources.Remove(source);
            Destroy(buttonObj);
        }
    }

    /// <summary>
    /// Handler for the Add Animation button click event.
    /// Adds the selected animation to the list of selected animations.
    /// </summary>
    protected void OnAddAnimationClicked()
    {
#if UNITY_EDITOR
        if (animationsDropdown == null)
        {
            Debug.LogError("animationsDropdown is not assigned.");
            return;
        }

        if (animationsDropdown.options.Count == 0)
        {
            Debug.LogWarning("animationsDropdown has no options.");
            return;
        }

        string selectedAnimationName = animationsDropdown.options[animationsDropdown.value].text;

        // Path to the character's animations folder
        string sceneName = currentSceneSO.sceneName;
        string characterName = currentCharacterSO.characterName;
        string animationsFolderPath = $"Assets/ReactiveScenes/{sceneName}/Characters/{characterName}/Animations";
        string animationPath = $"{animationsFolderPath}/{selectedAnimationName}.anim";

        AnimationClip selectedAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>(animationPath);

        if (selectedAnimation != null && !selectedAnimations.Contains(selectedAnimation))
        {
            selectedAnimations.Add(selectedAnimation);
            RefreshAnimationsScrollView();
        }
        else if (selectedAnimation == null)
        {
            Debug.LogError($"AnimationClip not found: {animationPath}");
        }
        else
        {
            Debug.LogWarning($"Animation {selectedAnimation.name} is already in the list.");
        }
#endif
    }

    /// <summary>
    /// Refreshes the animations scroll view to display the current list of selected animations.
    /// </summary>
    protected void RefreshAnimationsScrollView()
    {
        if (animationsContent == null)
        {
            Debug.LogError("animationsContent is not assigned.");
            return;
        }

#if UNITY_EDITOR
        // Remove existing buttons
        foreach (Transform child in animationsContent)
        {
            Destroy(child.gameObject);
        }

        // Create buttons for each selected animation
        foreach (var anim in selectedAnimations)
        {
            GameObject animButtonObj = Instantiate(animationButtonPrefab, animationsContent);
            animButtonObj.name = anim.name + "Button";

            // Set button text
            TextMeshProUGUI buttonText = animButtonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = anim.name;
            }
            else
            {
                Debug.LogError("TextMeshProUGUI component not found in animationButtonPrefab.");
            }

            // Assign remove listener
            Button animButton = animButtonObj.GetComponent<Button>();
            if (animButton != null)
            {
                AnimationClip animCopy = anim; // Local copy for the listener
                animButton.onClick.AddListener(() => OnRemoveAnimationClicked(animCopy, animButtonObj));
            }
            else
            {
                Debug.LogError("Button component not found in animationButtonPrefab.");
            }
        }
#endif
    }

    /// <summary>
    /// Handler for removing an animation from the selected animations list.
    /// </summary>
    /// <param name="anim">The animation to remove.</param>
    /// <param name="buttonObj">The button GameObject associated with the animation.</param>
    protected void OnRemoveAnimationClicked(AnimationClip anim, GameObject buttonObj)
    {
        if (selectedAnimations.Contains(anim))
        {
            selectedAnimations.Remove(anim);
            Destroy(buttonObj);
        }
    }

    /// <summary>
    /// Handler for the Back button click event.
    /// Navigates back to the interrupter type selection panel and stops any ongoing replay.
    /// </summary>
    protected virtual void OnBackButtonClicked()
    {
#if UNITY_EDITOR
        // Stop replay if it's running
        if (animationReplayer != null)
        {
            animationReplayer.StopReplay();
        }

        // Disable the Stop Replay button
        if (stopReplayButton != null)
        {
            stopReplayButton.interactable = false;
        }
#endif
        // Navigate back to the interrupter type selection panel
        interrupterConfigPanel.SetActive(false);
        interrupterTypeSelectionPanel.SetActive(true);
    }

    /// <summary>
    /// Resets common fields and UI elements to their default states.
    /// </summary>
    protected void ResetFields()
    {
        // Reset dropdowns to default values
        if (maxDistanceDropdown.options.Count > 0)
            maxDistanceDropdown.value = 0;

        if (minDurationDropdown.options.Count > 0)
            minDurationDropdown.value = 0;

        if (priorityDropdown.options.Count > 0)
            priorityDropdown.value = 0;

        // Reset specific fields in derived classes
        ResetSpecificFields();

        // Clear selected sources and animations
        selectedSources.Clear();
        selectedAnimations.Clear();

        // Refresh scroll views to reflect cleared lists
        RefreshSourcesScrollView();
        RefreshAnimationsScrollView();
    }

    /// <summary>
    /// Virtual method to reset fields specific to derived interrupter configuration managers.
    /// Can be overridden by derived classes to handle additional reset logic.
    /// </summary>
    protected virtual void ResetSpecificFields() { }

    /// <summary>
    /// Abstract method to handle the creation of a new interrupter.
    /// Must be implemented by derived classes.
    /// </summary>
    protected abstract void OnCreateButtonClicked();

    /// <summary>
    /// Abstract method to configure an existing interrupter.
    /// Must be implemented by derived classes.
    /// </summary>
    /// <param name="interrupter">The interrupter to configure.</param>
    public abstract void ConfigureInterrupter(Interrupter interrupter);

    /// <summary>
    /// Handler for the Replay button click event.
    /// Initiates the replay of a selected animation.
    /// </summary>
    protected void OnReplayButtonClicked()
    {
#if UNITY_EDITOR
        if (animationsDropdown == null)
        {
            Debug.LogError("animationsDropdown is not assigned.");
            return;
        }

        if (animationsDropdown.options.Count == 0)
        {
            Debug.LogError("animationsDropdown has no options.");
            return;
        }

        string selectedAnimationName = animationsDropdown.options[animationsDropdown.value].text;
        if (string.IsNullOrEmpty(selectedAnimationName))
        {
            Debug.LogError("No animation selected for replay.");
            return;
        }

        // Path to the character's animations folder
        string animationsFolderPath = $"Assets/ReactiveScenes/{currentSceneSO.sceneName}/Characters/{currentCharacterSO.characterName}/Animations";
        string[] guids = AssetDatabase.FindAssets($"t:AnimationClip {selectedAnimationName}", new[] { animationsFolderPath });
        AnimationClip clip = null;
        if (guids.Length > 0)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        }

        if (clip != null)
        {
            // Use the AnimationReplayer to play the animation
            if (animationReplayer != null && replayCharacterParent != null)
            {
                // Find the character prefab based on the name
                GameObject characterPrefab = currentSceneSO.selectedCharacterPrefabs.Find(p => p.name == currentCharacterSO.characterName);
                if (characterPrefab != null)
                {
                    animationReplayer.ReplayAnimation(clip, characterPrefab, replayCharacterParent);

                    // Enable the Stop Replay button
                    if (stopReplayButton != null)
                    {
                        stopReplayButton.interactable = true;
                    }
                }
                else
                {
                    Debug.LogError($"Character prefab for '{currentCharacterSO.characterName}' not found.");
                }
            }
            else
            {
                if (animationReplayer == null)
                    Debug.LogError("AnimationReplayer is not assigned.");
                if (replayCharacterParent == null)
                    Debug.LogError("replayCharacterParent is not assigned.");
            }
        }
        else
        {
            Debug.LogError($"AnimationClip not found: {selectedAnimationName}");
        }
#endif
    }

    /// <summary>
    /// Handler for the Stop Replay button click event.
    /// Stops any ongoing animation replay.
    /// </summary>
    protected void OnStopReplayClicked()
    {
#if UNITY_EDITOR
        if (animationReplayer != null)
        {
            animationReplayer.StopReplay();
        }
        else
        {
            Debug.LogError("AnimationReplayer is not assigned.");
        }

        // Disable the Stop Replay button
        if (stopReplayButton != null)
        {
            stopReplayButton.interactable = false;
        }
#endif
    }
}
