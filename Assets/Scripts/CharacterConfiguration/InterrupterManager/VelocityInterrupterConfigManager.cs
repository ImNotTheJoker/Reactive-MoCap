using System.Collections.Generic;
using UnityEngine;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Manages the configuration of Velocity Interrupters, handling UI interactions and data updates.
/// </summary>
public class VelocityInterrupterConfigManager : InterrupterConfigManager
{
    [Header("Velocity Specific UI Elements")]
    /// <summary>
    /// Dropdown for selecting the velocity threshold.
    /// </summary>
    public TMP_Dropdown velocityThresholdDropdown;

    /// <summary>
    /// The VelocityInterrupter currently being edited.
    /// </summary>
    private VelocityInterrupter editingInterrupter = null;

    /// <summary>
    /// Overrides the OnEnable method to populate specific dropdowns before initializing the base class.
    /// </summary>
    protected override void OnEnable()
    {
#if UNITY_EDITOR
        // Populate velocityThresholdDropdown
        if (velocityThresholdDropdown != null)
        {
            velocityThresholdDropdown.ClearOptions();
            List<string> options = new List<string>();
            for (float i = 0; i <= 1; i += 0.1f)
            {
                options.Add(i.ToString("0.0"));
            }
            velocityThresholdDropdown.AddOptions(options);
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
        if (interrupter is VelocityInterrupter velocityInterrupter)
        {
            editingInterrupter = velocityInterrupter;

            // Load values into UI elements
            if (maxDistanceDropdown != null)
                maxDistanceDropdown.value = Mathf.Clamp(Mathf.RoundToInt(velocityInterrupter.MaxDistance), 0, maxDistanceDropdown.options.Count - 1);
            
            if (minDurationDropdown != null)
                minDurationDropdown.value = Mathf.Clamp(Mathf.RoundToInt(velocityInterrupter.MinDuration), 0, minDurationDropdown.options.Count - 1);
            
            if (priorityDropdown != null)
                priorityDropdown.value = Mathf.Clamp(velocityInterrupter.Priority, 0, priorityDropdown.options.Count - 1);
            
            if (velocityThresholdDropdown != null)
                velocityThresholdDropdown.value = Mathf.Clamp(Mathf.RoundToInt(velocityInterrupter.VelocityThreshold * 10), 0, velocityThresholdDropdown.options.Count - 1);
            
            // Load sources and animations
            selectedSources = new List<string>(velocityInterrupter.relevantGameObjectNames);
            RefreshSourcesScrollView();
            
            selectedAnimations = new List<AnimationClip>(velocityInterrupter.ReactiveAnimations);
            RefreshAnimationsScrollView();

            // Update the Create button to Save
            createButton.GetComponentInChildren<TextMeshProUGUI>().text = "Save";
        }
        else
        {
            Debug.LogError("Incorrect interrupter type for VelocityInterrupterConfigManager.");
        }
#endif
    }

    /// <summary>
    /// Handles the Create button click event.
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
        if (maxDistanceDropdown == null || minDurationDropdown == null || priorityDropdown == null || velocityThresholdDropdown == null)
        {
            Debug.LogError("One or more dropdowns are not assigned.");
            return;
        }

        if (maxDistanceDropdown.options.Count == 0 || minDurationDropdown.options.Count == 0 || priorityDropdown.options.Count == 0 || velocityThresholdDropdown.options.Count == 0)
        {
            Debug.LogError("One or more dropdowns are empty.");
            // Uncomment the following line if you want to prevent further execution when dropdowns are empty
            // return;
        }

        // Retrieve values from dropdowns
        float maxDistance = float.Parse(maxDistanceDropdown.options[maxDistanceDropdown.value].text);
        float minDuration = float.Parse(minDurationDropdown.options[minDurationDropdown.value].text);
        int priority = int.Parse(priorityDropdown.options[priorityDropdown.value].text);
        float velocityThreshold = float.Parse(velocityThresholdDropdown.options[velocityThresholdDropdown.value].text);

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

        if (editingInterrupter != null && editingInterrupter is VelocityInterrupter velocityInterrupter)
        {
            // Update existing interrupter
            velocityInterrupter.MaxDistance = maxDistance;
            velocityInterrupter.MinDuration = minDuration;
            velocityInterrupter.Priority = priority;
            velocityInterrupter.VelocityThreshold = velocityThreshold; // Additional parameter

            List<string> sources = new List<string>(selectedSources);
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i] == "User")
                {
                    sources[i] = "CenterEyeAnchor";
                }
            }

            velocityInterrupter.relevantGameObjectNames = sources;
            velocityInterrupter.ReactiveAnimations = new List<AnimationClip>(selectedAnimations);

            // Mark the ScriptableObject as dirty
            EditorUtility.SetDirty(velocityInterrupter);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        else
        {
            // Create a new interrupter
            VelocityInterrupter newInterrupter = ScriptableObject.CreateInstance<VelocityInterrupter>();
            newInterrupter.interrupterName = "VelocityInterrupter_" + (currentCharacterSO.interrupters.Count + 1);
            newInterrupter.MaxDistance = maxDistance;
            newInterrupter.MinDuration = minDuration;
            newInterrupter.Priority = priority;
            newInterrupter.VelocityThreshold = velocityThreshold; // Additional parameter
            
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
                Debug.LogError($"Interrupter folder created: {interrupterFolderPath}");
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

        // Reset fields in config manager
        ResetFields();

        // Reset the Create button text back to "Create"
        createButton.GetComponentInChildren<TextMeshProUGUI>().text = "Create";

        // Reset the editingInterrupter
        editingInterrupter = null;
#endif
    }

    /// <summary>
    /// Resets fields specific to Velocity Interrupter configuration.
    /// </summary>
    protected override void ResetSpecificFields()
    {
#if UNITY_EDITOR
        if (velocityThresholdDropdown != null && velocityThresholdDropdown.options.Count > 0)
        {
            velocityThresholdDropdown.value = 0;
        }
#endif
    }

    /// <summary>
    /// Initializes the VelocityInterrupterConfigManager by populating specific dropdowns.
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
