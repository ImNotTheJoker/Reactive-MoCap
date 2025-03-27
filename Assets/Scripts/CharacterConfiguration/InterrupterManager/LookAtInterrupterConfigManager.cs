using System.Collections.Generic;
using UnityEngine;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Manages the configuration of LookAt Interrupters, handling UI interactions and data updates.
/// </summary>
public class LookAtInterrupterConfigManager : InterrupterConfigManager
{
    [Header("LookAt Specific UI Elements")]
    /// <summary>
    /// Dropdown for selecting the maximum angle.
    /// </summary>
    public TMP_Dropdown maxAngleDropdown;

    /// <summary>
    /// The LookAtInterrupter currently being edited.
    /// </summary>
    private LookAtInterrupter editingInterrupter = null;

    /// <summary>
    /// Overrides the OnEnable method to populate specific dropdowns before initializing the base class.
    /// </summary>
    protected override void OnEnable()
    {
#if UNITY_EDITOR
        // Populate maxAngleDropdown
        if (maxAngleDropdown != null)
        {
            maxAngleDropdown.ClearOptions();
            List<string> options = new List<string>();
            for (int angle = 0; angle <= 360; angle += 10)
            {
                options.Add(angle.ToString());
            }
            maxAngleDropdown.AddOptions(options);
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
        if (interrupter is LookAtInterrupter lookAtInterrupter)
        {
            editingInterrupter = lookAtInterrupter;

            // Load values into UI elements
            if (maxDistanceDropdown != null)
                maxDistanceDropdown.value = Mathf.Clamp(Mathf.RoundToInt(lookAtInterrupter.MaxDistance), 0, maxDistanceDropdown.options.Count - 1);
            
            if (minDurationDropdown != null)
                minDurationDropdown.value = Mathf.Clamp(Mathf.RoundToInt(lookAtInterrupter.MinDuration), 0, minDurationDropdown.options.Count - 1);
            
            if (priorityDropdown != null)
                priorityDropdown.value = Mathf.Clamp(lookAtInterrupter.Priority, 0, priorityDropdown.options.Count - 1);
            
            if (maxAngleDropdown != null)
                maxAngleDropdown.value = Mathf.Clamp(Mathf.RoundToInt(lookAtInterrupter.MaxAngle / 10f), 0, maxAngleDropdown.options.Count - 1); // Assuming values are in 10-degree steps
            
            // Load sources and animations
            selectedSources = new List<string>(lookAtInterrupter.relevantGameObjectNames);
            RefreshSourcesScrollView();
            
            selectedAnimations = new List<AnimationClip>(lookAtInterrupter.ReactiveAnimations);
            RefreshAnimationsScrollView();

            // Update the Create button to Save
            createButton.GetComponentInChildren<TextMeshProUGUI>().text = "Save";
        }
        else
        {
            Debug.LogError("Incorrect interrupter type for LookAtInterrupterConfigManager.");
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
        if (maxDistanceDropdown == null || minDurationDropdown == null || priorityDropdown == null || maxAngleDropdown == null)
        {
            Debug.LogError("One or more dropdowns are not assigned.");
            return;
        }

        if (maxDistanceDropdown.options.Count == 0 || minDurationDropdown.options.Count == 0 || priorityDropdown.options.Count == 0 || maxAngleDropdown.options.Count == 0)
        {
            Debug.LogError("One or more dropdowns are empty.");
            return;
        }

        // Retrieve values from dropdowns
        float maxDistance = float.Parse(maxDistanceDropdown.options[maxDistanceDropdown.value].text);
        float minDuration = float.Parse(minDurationDropdown.options[minDurationDropdown.value].text);
        int priority = int.Parse(priorityDropdown.options[priorityDropdown.value].text);
        float maxAngle = float.Parse(maxAngleDropdown.options[maxAngleDropdown.value].text);

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

        if (editingInterrupter != null && editingInterrupter is LookAtInterrupter lookAtInterrupter)
        {
            // Update existing interrupter
            lookAtInterrupter.MaxDistance = maxDistance;
            lookAtInterrupter.MinDuration = minDuration;
            lookAtInterrupter.Priority = priority;
            lookAtInterrupter.MaxAngle = maxAngle; // Additional parameter

            List<string> sources = new List<string>(selectedSources);
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i] == "User")
                {
                    sources[i] = "CenterEyeAnchor";
                }
            }

            lookAtInterrupter.relevantGameObjectNames = sources;
            lookAtInterrupter.ReactiveAnimations = new List<AnimationClip>(selectedAnimations);

            // Mark the ScriptableObject as dirty
            EditorUtility.SetDirty(lookAtInterrupter);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        else
        {
            // Create a new interrupter
            LookAtInterrupter newInterrupter = ScriptableObject.CreateInstance<LookAtInterrupter>();
            newInterrupter.interrupterName = "LookAtInterrupter_" + (currentCharacterSO.interrupters.Count + 1);
            newInterrupter.MaxDistance = maxDistance;
            newInterrupter.MinDuration = minDuration;
            newInterrupter.Priority = priority;
            newInterrupter.MaxAngle = maxAngle; // Additional parameter
            
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
                Debug.Log($"Interrupter folder created: {interrupterFolderPath}");
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
    /// Resets fields specific to LookAt Interrupter configuration.
    /// </summary>
    protected override void ResetSpecificFields()
    {
#if UNITY_EDITOR
        if (maxAngleDropdown != null && maxAngleDropdown.options.Count > 0)
        {
            maxAngleDropdown.value = 0;
        }
#endif
    }

    /// <summary>
    /// Initializes the LookAtInterrupterConfigManager by populating specific dropdowns.
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
