using System.Collections.Generic;
using UnityEngine;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Manages the configuration of Audio Interrupters, handling UI interactions and data updates.
/// </summary>
public class AudioInterrupterConfigManager : InterrupterConfigManager
{
    [Header("Audio Interrupter Specific UI Elements")]
    /// <summary>
    /// Dropdown for selecting the volume threshold.
    /// </summary>
    public TMP_Dropdown volumeThresholdDropdown;

    /// <summary>
    /// The AudioInterrupter currently being edited.
    /// </summary>
    private AudioInterrupter editingInterrupter = null;

    /// <summary>
    /// Overrides the OnEnable method to populate specific dropdowns before initializing the base class.
    /// </summary>
    protected override void OnEnable()
    {
#if UNITY_EDITOR
        // Populate volumeThresholdDropdown
        if (volumeThresholdDropdown != null)
        {
            volumeThresholdDropdown.ClearOptions();
            List<string> volumeOptions = new List<string>();
            for (int volume = 0; volume <= 10; volume += 1)
            {
                volumeOptions.Add(volume.ToString());
            }
            volumeThresholdDropdown.AddOptions(volumeOptions);
        }
#endif

        // Call the base class's OnEnable to handle common initialization
        base.OnEnable();
    }

    /// <summary>
    /// Configures the interrupter with the provided data.
    /// </summary>
    /// <param name="interrupter">The interrupter to configure.</param>
    public override void ConfigureInterrupter(Interrupter interrupter)
    {
#if UNITY_EDITOR
        if (interrupter is AudioInterrupter audioInterrupter)
        {
            editingInterrupter = audioInterrupter;

            // Load values into UI elements
            if (maxDistanceDropdown != null)
                maxDistanceDropdown.value = Mathf.Clamp(Mathf.RoundToInt(audioInterrupter.MaxDistance), 0, maxDistanceDropdown.options.Count - 1);
            
            if (minDurationDropdown != null)
                minDurationDropdown.value = Mathf.Clamp(Mathf.RoundToInt(audioInterrupter.MinDuration), 0, minDurationDropdown.options.Count - 1);
            
            if (priorityDropdown != null)
                priorityDropdown.value = Mathf.Clamp(audioInterrupter.Priority, 0, priorityDropdown.options.Count - 1);
            
            if (volumeThresholdDropdown != null)
                volumeThresholdDropdown.value = Mathf.Clamp(Mathf.RoundToInt(audioInterrupter.VolumeThreshold), 0, volumeThresholdDropdown.options.Count - 1);
            
            // Load sources and animations
            selectedSources = new List<string>(audioInterrupter.relevantGameObjectNames);
            RefreshSourcesScrollView();
            
            selectedAnimations = new List<AnimationClip>(audioInterrupter.ReactiveAnimations);
            RefreshAnimationsScrollView();

            // Update the Create button to Save
            createButton.GetComponentInChildren<TextMeshProUGUI>().text = "Save";
        }
        else
        {
            Debug.LogError("Incorrect interrupter type for AudioInterrupterConfigManager.");
        }
#endif
    }

    /// <summary>
    /// Handles the Create/Save button click event.
    /// Updates existing interrupters or creates new ones based on the current configuration.
    /// </summary>
    protected override void OnCreateButtonClicked()
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

        // Validate dropdown assignments
        if (maxDistanceDropdown == null || minDurationDropdown == null || priorityDropdown == null || volumeThresholdDropdown == null)
        {
            Debug.LogError("One or more dropdowns are not assigned.");
            return;
        }

        if (maxDistanceDropdown.options.Count == 0 || minDurationDropdown.options.Count == 0 || priorityDropdown.options.Count == 0 || volumeThresholdDropdown.options.Count == 0)
        {
            Debug.LogError("One or more dropdowns are empty.");
            return;
        }

        // Retrieve values from dropdowns
        float maxDistance = float.Parse(maxDistanceDropdown.options[maxDistanceDropdown.value].text);
        float minDuration = float.Parse(minDurationDropdown.options[minDurationDropdown.value].text);
        int priority = int.Parse(priorityDropdown.options[priorityDropdown.value].text);
        float volumeThreshold = float.Parse(volumeThresholdDropdown.options[volumeThresholdDropdown.value].text);

        if (currentCharacterSO == null)
        {
            Debug.LogError("currentCharacterSO is null.");
            return;
        }

        if (selectedSources == null)
        {
            Debug.LogError("selectedSources is null.");
            selectedSources = new List<string>();
        }

        if (selectedAnimations == null)
        {
            Debug.LogError("selectedAnimations is null.");
            selectedAnimations = new List<AnimationClip>();
        }

        if (editingInterrupter != null && editingInterrupter is AudioInterrupter audioInterrupter)
        {
            // Update existing interrupter
            audioInterrupter.MaxDistance = maxDistance;
            audioInterrupter.MinDuration = minDuration;
            audioInterrupter.Priority = priority;
            audioInterrupter.VolumeThreshold = volumeThreshold; // Additional parameter

            List<string> sources = new List<string>(selectedSources);
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i] == "User")
                {
                    sources[i] = "CenterEyeAnchor";
                }
            }

            audioInterrupter.relevantGameObjectNames = sources;
            audioInterrupter.ReactiveAnimations = new List<AnimationClip>(selectedAnimations);

            // Mark the ScriptableObject as dirty
            EditorUtility.SetDirty(audioInterrupter);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        else
        {
            // Create a new interrupter
            AudioInterrupter newInterrupter = ScriptableObject.CreateInstance<AudioInterrupter>();
            newInterrupter.interrupterName = "AudioInterrupter_" + (currentCharacterSO.interrupters.Count + 1);
            newInterrupter.MaxDistance = maxDistance;
            newInterrupter.MinDuration = minDuration;
            newInterrupter.Priority = priority;
            newInterrupter.VolumeThreshold = volumeThreshold; // Additional parameter
            
            List<string> sources = new List<string>(selectedSources);
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i] == "User")
                {
                    sources[i] = "CenterEyeAnchor";
                }
            }
            
            newInterrupter.relevantGameObjectNames = sources;
            newInterrupter.ReactiveAnimations = new List<AnimationClip>(selectedAnimations);

            // Save the interrupter in the correct folder
            string characterName = currentCharacterSO.characterName; // Correctly use the current character name
            string interrupterFolderPath = $"Assets/ReactiveScenes/{currentSceneSO.sceneName}/Characters/{characterName}/Interrupters";

            if (!AssetDatabase.IsValidFolder(interrupterFolderPath))
            {
                AssetDatabase.CreateFolder($"Assets/ReactiveScenes/{currentSceneSO.sceneName}/Characters/{characterName}", "Interrupters");
            }

            string interrupterAssetPath = $"{interrupterFolderPath}/{newInterrupter.interrupterName}.asset";
            AssetDatabase.CreateAsset(newInterrupter, interrupterAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Add to ReactiveCharacterSO
            currentCharacterSO.AddInterrupter(newInterrupter);
            EditorUtility.SetDirty(currentCharacterSO); // Mark the SO as dirty
        }

        // Update the dropdowns in CharacterOverviewManager
        characterOverviewManager.RefreshInterruptersScrollView();
        characterOverviewManager.PopulateInterrupterConfigureDropdown();

        // Navigate back to the CharacterOverviewPanel
        interrupterConfigPanel.SetActive(false);
        interrupterTypeSelectionPanel.SetActive(false);
        characterOverviewManager.characterOverviewPanel.SetActive(true);

        // Reset fields
        ResetFields();

        // Reset the Create button text back to "Create"
        createButton.GetComponentInChildren<TextMeshProUGUI>().text = "Create";

        // Reset the editingInterrupter
        editingInterrupter = null;
#endif
    }

    /// <summary>
    /// Resets fields specific to Audio Interrupter configuration.
    /// </summary>
    protected override void ResetSpecificFields()
    {
#if UNITY_EDITOR
        if (volumeThresholdDropdown != null && volumeThresholdDropdown.options.Count > 0)
        {
            volumeThresholdDropdown.value = 0;
        }
#endif
    }

    /// <summary>
    /// Initializes the AudioInterrupterConfigManager by populating specific dropdowns.
    /// </summary>
    protected override void Start()
    {
        base.Start();
        if (characterOverviewManager == null)
        {
            Debug.LogError("CharacterOverviewManager is not assigned.");
        }
    }
}
