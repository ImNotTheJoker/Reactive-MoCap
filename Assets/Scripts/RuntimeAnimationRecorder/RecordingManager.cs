using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.Animations;

/// <summary>
/// Manages the recording process of human poses, handles UI interactions, and manages scene transitions.
/// Includes functionalities for starting/stopping recordings, replaying animations, and navigating between scenes.
/// </summary>
public class RecordingManager : MonoBehaviour
{
    [Header("UI Elements")]
    /// <summary>
    /// Dropdown for selecting characters.
    /// </summary>
    public TMP_Dropdown selectedCharactersDropdown;

    /// <summary>
    /// Dropdown for selecting recording durations.
    /// </summary>
    public TMP_Dropdown recordingDurationDropdown;

    /// <summary>
    /// Dropdown for selecting animations.
    /// </summary>
    public TMP_Dropdown animationsDropdown;

    /// <summary>
    /// Button to start recording.
    /// </summary>
    public Button startRecordingButton;

    /// <summary>
    /// Button to stop recording.
    /// </summary>
    public Button stopRecordingButton;

    /// <summary>
    /// Button to replay the recorded animation.
    /// </summary>
    public Button replayButton;

    /// <summary>
    /// Button to navigate to the next scene.
    /// </summary>
    public Button nextSceneButton;

    /// <summary>
    /// Button to navigate to the previous scene.
    /// </summary>
    public Button previousSceneButton;

    /// <summary>
    /// Text element to display the start timer.
    /// </summary>
    public TextMeshProUGUI startTimerText;

    /// <summary>
    /// Text element to display the recording timer.
    /// </summary>
    public TextMeshProUGUI recordingTimerText;

    /// <summary>
    /// Toggle to show or hide the mirror avatar.
    /// </summary>
    public Toggle showMirrorAvatarToggle;

    [Header("Avatar Parents")]
    /// <summary>
    /// Parent transform for the mirror avatar.
    /// </summary>
    public Transform mirrorAvatarParent;

    /// <summary>
    /// Parent transform for the replay avatar.
    /// </summary>
    public Transform replayAvatarParent;

    [Header("Mirror Avatar Prefabs")]
    /// <summary>
    /// List of mirror avatar prefabs available for selection.
    /// </summary>
    public List<GameObject> mirrorAvatarPrefabs;

    [Header("Recorder")]
    /// <summary>
    /// Reference to the HumanPoseRecorder script responsible for recording poses.
    /// </summary>
    public HumanPoseRecorder recorder;

    [Header("Grabbable Menu")]
    /// <summary>
    /// Button to start recording in the grabbable menu.
    /// </summary>
    public Button grabbableStartRecordingButton;

    /// <summary>
    /// Button to stop recording in the grabbable menu.
    /// </summary>
    public Button grabbableStopRecordingButton;

    /// <summary>
    /// Button to replay the animation in the grabbable menu.
    /// </summary>
    public Button grabbableReplayButton;

    /// <summary>
    /// Text element to display the start timer in the grabbable menu.
    /// </summary>
    public TextMeshProUGUI grabbableStartTimerText;

    /// <summary>
    /// Text element to display the recording timer in the grabbable menu.
    /// </summary>
    public TextMeshProUGUI grabbableRecordingTimerText;

    /// <summary>
    /// The currently active mirror avatar instance.
    /// </summary>
    private GameObject currentMirrorAvatar;

    /// <summary>
    /// The currently active replay avatar instance.
    /// </summary>
    private GameObject currentReplayAvatar;

    /// <summary>
    /// The current ReactiveScene ScriptableObject.
    /// </summary>
    private ReactiveSceneSO currentSceneSO;

    /// <summary>
    /// Coroutine reference for the recording process.
    /// </summary>
    private Coroutine recordingCoroutine;

    /// <summary>
    /// Coroutine reference for the recording timer.
    /// </summary>
    private Coroutine timerCoroutine;

    /// <summary>
    /// The duration of the current recording session in seconds.
    /// </summary>
    private float recordingDuration;

    /// <summary>
    /// The name of the currently selected character.
    /// </summary>
    private string selectedCharacterName;

    /// <summary>
    /// The current PlayableGraph for managing animation playbacks.
    /// </summary>
    private PlayableGraph currentGraph;

    /// <summary>
    /// Coroutine reference for the replay process.
    /// </summary>
    private Coroutine replayCoroutine;

    /// <summary>
    /// The name of the previous scene for navigation.
    /// </summary>
    public string previousSceneName;

    /// <summary>
    /// The name of the next scene for navigation.
    /// </summary>
    public string nextSceneName;

    /// <summary>
    /// The name of the last recorded animation clip.
    /// </summary>
    private string lastRecordedClipName;

    /// <summary>
    /// Initializes the recording manager by setting up UI elements, populating dropdowns, and configuring initial states.
    /// </summary>
    void Start()
    {
        // Retrieve the current ReactiveSceneSO from the SceneDataHolder
        currentSceneSO = SceneDataHolder.currentScene;

        if (currentSceneSO == null)
        {
            Debug.LogError("No ReactiveSceneSO found. Please ensure a ReactiveScene is selected.");
            return;
        }

#if UNITY_EDITOR
        // Set up folder structure in the Unity Editor
        CreateFolderStructure();
#endif

        // Populate the character selection dropdown
        PopulateSelectedCharactersDropdown();

        // Populate the recording duration dropdown
        PopulateRecordingDurationDropdown();

        // Populate the animations dropdown based on the first selected character
        if (currentSceneSO.selectedCharacterPrefabs.Count > 0)
        {
            selectedCharacterName = currentSceneSO.selectedCharacterPrefabs[0].name;
            PopulateAnimationsDropdownForCharacter(selectedCharacterName);
        }

        // Set up button listeners
        startRecordingButton.onClick.AddListener(OnStartRecordingClicked);
        stopRecordingButton.onClick.AddListener(OnStopRecordingClicked);
        replayButton.onClick.AddListener(OnReplayClicked);
        nextSceneButton.onClick.AddListener(OnNextSceneClicked);
        previousSceneButton.onClick.AddListener(OnPreviousSceneClicked);

        if (grabbableStartRecordingButton != null)
            grabbableStartRecordingButton.onClick.AddListener(OnStartRecordingClicked);
        if (grabbableStopRecordingButton != null)
            grabbableStopRecordingButton.onClick.AddListener(OnStopRecordingClicked);
        if (grabbableReplayButton != null)
            grabbableReplayButton.onClick.AddListener(OnReplayClicked);

        // Set up toggle listener for mirror avatar visibility
        showMirrorAvatarToggle.onValueChanged.AddListener(OnShowMirrorAvatarToggled);

        // Set up listener for character selection changes
        selectedCharactersDropdown.onValueChanged.AddListener(OnSelectedCharacterChanged);

        // Initialize button states
        stopRecordingButton.interactable = false;
        grabbableStopRecordingButton.interactable = false;

        // Display the mirror avatar
        ShowMirrorAvatar();

        // Initialize toggle state
        showMirrorAvatarToggle.isOn = true;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Creates the necessary folder structure for storing animations and ReactiveSceneSO assets.
    /// </summary>
    private void CreateFolderStructure()
    {
        string baseFolder = $"Assets/ReactiveScenes/{currentSceneSO.sceneName}";

        if (!AssetDatabase.IsValidFolder(baseFolder))
        {
            AssetDatabase.CreateFolder("Assets/ReactiveScenes", currentSceneSO.sceneName);
            AssetDatabase.Refresh();
        }

        // Create the Characters folder
        string charactersFolder = $"{baseFolder}/Characters";
        if (!AssetDatabase.IsValidFolder(charactersFolder))
        {
            AssetDatabase.CreateFolder(baseFolder, "Characters");
            AssetDatabase.Refresh();
        }

        // Create character-specific folders and their Animations subfolders
        foreach (var characterPrefab in currentSceneSO.selectedCharacterPrefabs)
        {
            string characterFolder = $"{charactersFolder}/{characterPrefab.name}";
            if (!AssetDatabase.IsValidFolder(characterFolder))
            {
                AssetDatabase.CreateFolder(charactersFolder, characterPrefab.name);
                AssetDatabase.Refresh();
            }

            string animationsFolder = $"{characterFolder}/Animations";
            if (!AssetDatabase.IsValidFolder(animationsFolder))
            {
                AssetDatabase.CreateFolder(characterFolder, "Animations");
                AssetDatabase.Refresh();
            }
        }

        // Check if the ReactiveSceneSO already exists in the folder
        string soPath = $"{baseFolder}/{currentSceneSO.sceneName}SO.asset";
        if (AssetDatabase.LoadAssetAtPath<ReactiveSceneSO>(soPath) == null)
        {
            AssetDatabase.CreateAsset(currentSceneSO, soPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"ReactiveSceneSO created and saved at: {soPath}");
        }
    }
#endif

    /// <summary>
    /// Populates the selected characters dropdown with available character prefabs.
    /// </summary>
    private void PopulateSelectedCharactersDropdown()
    {
        selectedCharactersDropdown.options.Clear();
        foreach (var characterPrefab in currentSceneSO.selectedCharacterPrefabs)
        {
            selectedCharactersDropdown.options.Add(new TMP_Dropdown.OptionData(characterPrefab.name));
        }
        selectedCharactersDropdown.RefreshShownValue();
    }

    /// <summary>
    /// Populates the recording duration dropdown with predefined durations.
    /// </summary>
    private void PopulateRecordingDurationDropdown()
    {
        recordingDurationDropdown.options.Clear();
        string[] durations = { "5 sec", "10 sec", "20 sec", "30 sec", "1 min", "2 min", "3 min" };
        foreach (var duration in durations)
        {
            recordingDurationDropdown.options.Add(new TMP_Dropdown.OptionData(duration));
        }
        recordingDurationDropdown.value = 0;
        recordingDurationDropdown.RefreshShownValue();
    }

    /// <summary>
    /// Populates the animations dropdown with available animations for the selected character.
    /// </summary>
    private void PopulateAnimationsDropdown()
    {
        animationsDropdown.options.Clear();
        PopulateAnimationsDropdownForCharacter(selectedCharacterName);
        animationsDropdown.RefreshShownValue();
    }

    /// <summary>
    /// Populates the animations dropdown for a specific character.
    /// </summary>
    /// <param name="characterName">The name of the character whose animations are to be listed.</param>
    private void PopulateAnimationsDropdownForCharacter(string characterName)
    {
        animationsDropdown.options.Clear();
#if UNITY_EDITOR
        string animationsFolderPath = $"Assets/ReactiveScenes/{currentSceneSO.sceneName}/Characters/{characterName}/Animations";

        if (AssetDatabase.IsValidFolder(animationsFolderPath))
        {
            string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { animationsFolderPath });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
                if (clip != null)
                {
                    animationsDropdown.options.Add(new TMP_Dropdown.OptionData(clip.name));
                }
            }
        }
#endif
        animationsDropdown.RefreshShownValue();
    }

    /// <summary>
    /// Handler for the Start Recording button click event.
    /// Initiates the recording process.
    /// </summary>
    private void OnStartRecordingClicked()
    {
        if (recordingCoroutine != null)
        {
            StopCoroutine(recordingCoroutine);
        }
        recordingCoroutine = StartCoroutine(StartRecordingProcess());
    }

    /// <summary>
    /// Coroutine that manages the recording process, including countdown and duration handling.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution.</returns>
    private IEnumerator StartRecordingProcess()
    {
        // Disable Start Recording Button and enable Stop Recording Button
        startRecordingButton.interactable = false;
        stopRecordingButton.interactable = true;
        grabbableStartRecordingButton.interactable = false;
        grabbableStopRecordingButton.interactable = true;

        // Display start timer countdown from 3
        startTimerText.gameObject.SetActive(true);
        startTimerText.text = "Start in...";
        grabbableStartTimerText.gameObject.SetActive(true);
        grabbableStartTimerText.text = "Start in...";
        for (int i = 3; i > 0; i--)
        {
            recordingTimerText.text = i.ToString();
            grabbableRecordingTimerText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }
        startTimerText.text = "Recording...";
        grabbableStartTimerText.text = "Recording...";

        // Determine the selected recording duration
        string selectedDuration = recordingDurationDropdown.options[recordingDurationDropdown.value].text;
        recordingDuration = ParseDuration(selectedDuration);

        // Get the selected character name
        selectedCharacterName = selectedCharactersDropdown.options[selectedCharactersDropdown.value].text;

#if UNITY_EDITOR
        // Configure and start the recorder
        string saveFolderPath = $"Assets/ReactiveScenes/{currentSceneSO.sceneName}/Characters/{selectedCharacterName}/Animations";
        // Ensure the Animations folder exists
        if (!AssetDatabase.IsValidFolder(saveFolderPath))
        {
            AssetDatabase.CreateFolder($"Assets/ReactiveScenes/{currentSceneSO.sceneName}/Characters/{selectedCharacterName}", "Animations");
            AssetDatabase.Refresh();
        }

        string clipName = $"Animation_{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}";
        lastRecordedClipName = clipName; // Store the clip name
        recorder.savePath = saveFolderPath;
        recorder.clipName = clipName;
        recorder.StartRecording(saveFolderPath, clipName);
#endif

        // Start the recording timer coroutine
        timerCoroutine = StartCoroutine(RecordingTimerCoroutine(recordingDuration));
    }

    /// <summary>
    /// Parses the duration string and returns the duration in seconds.
    /// </summary>
    /// <param name="durationStr">The duration string (e.g., "1 min", "30 sec").</param>
    /// <returns>The duration in seconds.</returns>
    private float ParseDuration(string durationStr)
    {
        if (durationStr.Contains("min"))
        {
            int minutes = int.Parse(durationStr.Replace(" min", ""));
            return minutes * 60f;
        }
        else if (durationStr.Contains("sec"))
        {
            int seconds = int.Parse(durationStr.Replace(" sec", ""));
            return seconds;
        }
        return 5f; // Default value
    }

#if UNITY_EDITOR
    /// <summary>
    /// Stops the recording process and saves the recorded AnimationClip.
    /// </summary>
    private void StopRecordingAndSave()
    {
        if (recorder == null)
        {
            Debug.LogError("Recorder is not assigned.");
            return;
        }

        recorder.StopRecording();

        // Refresh the AssetDatabase and repopulate the animations dropdown
        AssetDatabase.Refresh();
        PopulateAnimationsDropdown();
    }
#endif

    /// <summary>
    /// Stops the recording process and updates UI elements accordingly.
    /// </summary>
    private void StopRecording()
    {
#if UNITY_EDITOR
        StopRecordingAndSave();
#endif

        // Enable Start Recording Button and disable Stop Recording Button
        startRecordingButton.interactable = true;
        stopRecordingButton.interactable = false;
        grabbableStartRecordingButton.interactable = true;
        grabbableStopRecordingButton.interactable = false;

        // Stop the recording timer coroutine if it's active
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        // Reset timer texts
        startTimerText.text = "Status";
        recordingTimerText.text = "Time";
        grabbableStartTimerText.text = "Status";
        grabbableRecordingTimerText.text = "Time";

#if UNITY_EDITOR
        // Set the animationsDropdown to the last recorded animation
        if (!string.IsNullOrEmpty(lastRecordedClipName))
        {
            int index = animationsDropdown.options.FindIndex(option => option.text == lastRecordedClipName);
            if (index != -1)
            {
                animationsDropdown.value = index;
                animationsDropdown.RefreshShownValue();
                Debug.Log($"animationsDropdown set to '{lastRecordedClipName}'.");
            }
            else
            {
                Debug.LogWarning($"Animation '{lastRecordedClipName}' not found in animationsDropdown.");
            }
        }
#endif
    }

    /// <summary>
    /// Coroutine that manages the recording timer and stops recording after the duration elapses.
    /// </summary>
    /// <param name="duration">The duration of the recording in seconds.</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
    private IEnumerator RecordingTimerCoroutine(float duration)
    {
        recordingTimerText.gameObject.SetActive(true);
        grabbableRecordingTimerText.gameObject.SetActive(true);
        float remaining = duration;
        while (remaining > 0)
        {
            recordingTimerText.text = Mathf.CeilToInt(remaining).ToString() + " sec";
            grabbableRecordingTimerText.text = Mathf.CeilToInt(remaining).ToString() + " sec";
            remaining -= Time.deltaTime;
            yield return null;
        }
        recordingTimerText.text = "0 sec";
        grabbableRecordingTimerText.text = "0 sec";

        StopRecording();
    }

    /// <summary>
    /// Handler for the Stop Recording button click event.
    /// Stops the recording process.
    /// </summary>
    private void OnStopRecordingClicked()
    {
        if (recordingCoroutine != null)
        {
            StopCoroutine(recordingCoroutine);
            recordingCoroutine = null;
        }
        StopRecording();
    }

    /// <summary>
    /// Handler for the Replay button click event.
    /// Replays the selected animation on a replay avatar.
    /// </summary>
    private void OnReplayClicked()
    {
        string selectedCharacterName = selectedCharactersDropdown.options[selectedCharactersDropdown.value].text;
        string selectedAnimationName = animationsDropdown.options[animationsDropdown.value].text;

        if (string.IsNullOrEmpty(selectedAnimationName))
        {
            Debug.LogError("No animation selected for playback.");
            return;
        }

        // Check if a replay is already running
        if (currentReplayAvatar != null)
        {
            // Stop and destroy the existing replay avatar
            Destroy(currentReplayAvatar);
            if (currentGraph.IsValid())
            {
                currentGraph.Destroy();
            }
            currentGraph = default;

            // Stop the existing replay coroutine if active
            if (replayCoroutine != null)
            {
                StopCoroutine(replayCoroutine);
                replayCoroutine = null;
            }
            currentReplayAvatar = null;
        }

        // Find the selected character prefab
        GameObject replayCharacterPrefab = currentSceneSO.selectedCharacterPrefabs.Find(p => p.name == selectedCharacterName);
        if (replayCharacterPrefab == null)
        {
            Debug.LogError("Replay character prefab not found: " + selectedCharacterName);
            return;
        }

        // Instantiate the replay avatar
        currentReplayAvatar = Instantiate(replayCharacterPrefab, replayAvatarParent);
        currentReplayAvatar.transform.localPosition = Vector3.zero;
        currentReplayAvatar.transform.localRotation = Quaternion.identity;

#if UNITY_EDITOR
        string animationsFolderPath = $"Assets/ReactiveScenes/{currentSceneSO.sceneName}/Characters/{selectedCharacterName}/Animations";
        string[] guids = AssetDatabase.FindAssets($"t:AnimationClip {selectedAnimationName}", new[] { animationsFolderPath });
        AnimationClip clip = null;
        if (guids.Length > 0)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        }

        if (clip != null)
        {
            Animator animator = currentReplayAvatar.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Animator component not found on replay character.");
                Destroy(currentReplayAvatar);
                return;
            }

            // Create a new PlayableGraph or destroy the existing one
            if (currentGraph.IsValid())
            {
                currentGraph.Destroy();
            }

            currentGraph = PlayableGraph.Create("AnimationGraph");
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(currentGraph, "Animation", animator);
            AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(currentGraph, clip);
            clipPlayable.SetApplyFootIK(true);
            output.SetSourcePlayable(clipPlayable);
            currentGraph.Play();

            // Start coroutine to destroy the replay avatar after the animation completes
            replayCoroutine = StartCoroutine(DestroyReplayCharacterAfterAnimation(currentGraph, clip.length));
        }
        else
        {
            Debug.LogError("AnimationClip not found: " + selectedAnimationName);
            Destroy(currentReplayAvatar);
        }
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// Coroutine that destroys the replay avatar after the animation duration elapses.
    /// </summary>
    /// <param name="graph">The PlayableGraph associated with the animation playback.</param>
    /// <param name="duration">The duration of the animation in seconds.</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
    private IEnumerator DestroyReplayCharacterAfterAnimation(PlayableGraph graph, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (currentReplayAvatar != null)
        {
            Destroy(currentReplayAvatar);
        }
        if (graph.IsValid())
        {
            graph.Destroy();
        }
        currentReplayAvatar = null;
        currentGraph = default;
        replayCoroutine = null;
    }
#endif

    /// <summary>
    /// Handler for changes in the selected character dropdown.
    /// Updates the animations dropdown and mirror avatar accordingly.
    /// </summary>
    /// <param name="index">The index of the selected option.</param>
    private void OnSelectedCharacterChanged(int index)
    {
        // Update the selected character name
        selectedCharacterName = selectedCharactersDropdown.options[index].text;
        PopulateAnimationsDropdownForCharacter(selectedCharacterName);
        ShowMirrorAvatar();
    }

    /// <summary>
    /// Handler for the mirror avatar toggle value change event.
    /// Shows or hides the mirror avatar based on the toggle state.
    /// </summary>
    /// <param name="isOn">True if the mirror avatar should be shown; otherwise, false.</param>
    private void OnShowMirrorAvatarToggled(bool isOn)
    {
        ToggleMirrorAvatar(isOn);
    }

    /// <summary>
    /// Toggles the visibility of the mirror avatar.
    /// </summary>
    /// <param name="isOn">True to show the mirror avatar; false to hide it.</param>
    private void ToggleMirrorAvatar(bool isOn)
    {
        if (currentMirrorAvatar != null)
        {
            currentMirrorAvatar.SetActive(isOn);
        }
    }

    /// <summary>
    /// Displays the mirror avatar based on the selected character.
    /// </summary>
    private void ShowMirrorAvatar()
    {
        // Find the selected character prefab
        string characterName = selectedCharactersDropdown.options[selectedCharactersDropdown.value].text;
        GameObject characterPrefab = currentSceneSO.selectedCharacterPrefabs.Find(p => p.name == characterName);
        if (characterPrefab == null)
        {
            Debug.LogError("Character prefab not found: " + characterName);
            return;
        }

        // Destroy the existing mirror avatar if it exists
        if (currentMirrorAvatar != null)
        {
            Destroy(currentMirrorAvatar);
        }

        // Instantiate the mirror avatar
        currentMirrorAvatar = Instantiate(characterPrefab, mirrorAvatarParent);
        currentMirrorAvatar.transform.localPosition = Vector3.zero;
        currentMirrorAvatar.transform.localRotation = Quaternion.identity;

        // Set up retargeting for the mirror avatar
        RetargetMirrorAvatar(currentMirrorAvatar);

        // Set the mirror avatar's active state based on the toggle
        currentMirrorAvatar.SetActive(showMirrorAvatarToggle.isOn);
    }

    /// <summary>
    /// Sets up retargeting for the mirror avatar to follow the player avatar's movements.
    /// </summary>
    /// <param name="mirrorAvatar">The mirror avatar GameObject to retarget.</param>
    private void RetargetMirrorAvatar(GameObject mirrorAvatar)
    {
        // Find the player avatar by tag
        GameObject playerAvatar = GameObject.FindWithTag("PlayerAvatar");
        if (playerAvatar != null)
        {
            // Add or retrieve the RetargetingHPH script and assign the source avatar
            RetargetingHPH mirrorScript = mirrorAvatar.GetComponent<RetargetingHPH>();
            if (mirrorScript == null)
            {
                mirrorScript = mirrorAvatar.AddComponent<RetargetingHPH>();
            }
            mirrorScript.src = playerAvatar;
        }
        else
        {
            Debug.LogError("PlayerAvatar not found. Please ensure the player avatar has the tag 'PlayerAvatar'.");
        }
    }

    /// <summary>
    /// Handler for the Next Scene button click event.
    /// Navigates to the next specified scene.
    /// </summary>
    private void OnNextSceneClicked()
    {
        SceneManager.LoadScene(nextSceneName);
    }

    /// <summary>
    /// Handler for the Previous Scene button click event.
    /// Navigates to the previous specified scene.
    /// </summary>
    private void OnPreviousSceneClicked()
    {
        SceneManager.LoadScene(previousSceneName);
    }
}
