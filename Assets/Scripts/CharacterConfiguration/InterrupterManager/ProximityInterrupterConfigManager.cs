using System.Collections.Generic;
using UnityEngine;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Manages the configuration of Proximity Interrupters, handling UI interactions and data updates.
/// </summary>
public class ProximityInterrupterConfigManager : InterrupterConfigManager
{
    /// <summary>
    /// The ProximityInterrupter currently being edited.
    /// </summary>
    private ProximityInterrupter editingInterrupter = null;

    /// <summary>
    /// Configures the interrupter with the provided data.
    /// </summary>
    /// <param name="interrupter">The interrupter to configure.</param>
    public override void ConfigureInterrupter(Interrupter interrupter)
    {
#if UNITY_EDITOR
        if (interrupter is ProximityInterrupter proximityInterrupter)
        {
            editingInterrupter = proximityInterrupter;

            // Load values into UI elements
            if (maxDistanceDropdown != null)
                maxDistanceDropdown.value = Mathf.Clamp(Mathf.RoundToInt(proximityInterrupter.MaxDistance), 0, maxDistanceDropdown.options.Count - 1);
            
            if (minDurationDropdown != null)
                minDurationDropdown.value = Mathf.Clamp(Mathf.RoundToInt(proximityInterrupter.MinDuration), 0, minDurationDropdown.options.Count - 1);
            
            if (priorityDropdown != null)
                priorityDropdown.value = Mathf.Clamp(proximityInterrupter.Priority, 0, priorityDropdown.options.Count - 1);
            
            // Load sources and animations
            selectedSources = new List<string>(proximityInterrupter.relevantGameObjectNames);
            RefreshSourcesScrollView();
            
            selectedAnimations = new List<AnimationClip>(proximityInterrupter.ReactiveAnimations);
            RefreshAnimationsScrollView();

            // Update the Create button to Save
            createButton.GetComponentInChildren<TextMeshProUGUI>().text = "Save";
        }
        else
        {
            Debug.LogError("Incorrect interrupter type for ProximityInterrupterConfigManager.");
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

        if (maxDistanceDropdown == null || minDurationDropdown == null || priorityDropdown == null)
        {
            Debug.LogError("Dropdowns are not assigned.");
            return;
        }

        if (maxDistanceDropdown.options.Count == 0 || minDurationDropdown.options.Count == 0 || priorityDropdown.options.Count == 0)
        {
            Debug.LogError("Dropdowns are empty.");
            return;
        }

        // Retrieve values from dropdowns
        float maxDistance = float.Parse(maxDistanceDropdown.options[maxDistanceDropdown.value].text);
        float minDuration = float.Parse(minDurationDropdown.options[minDurationDropdown.value].text);
        int priority = int.Parse(priorityDropdown.options[priorityDropdown.value].text);

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

        if (editingInterrupter != null && editingInterrupter is ProximityInterrupter proximityInterrupter)
        {
            // Update existing interrupter
            proximityInterrupter.MaxDistance = maxDistance;
            proximityInterrupter.MinDuration = minDuration;
            proximityInterrupter.Priority = priority;

            List<string> sources = new List<string>(selectedSources);
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i] == "User")
                {
                    sources[i] = "CenterEyeAnchor";
                }
            }

            proximityInterrupter.relevantGameObjectNames = sources;
            proximityInterrupter.ReactiveAnimations = new List<AnimationClip>(selectedAnimations);

            // Mark the ScriptableObject as dirty
            EditorUtility.SetDirty(proximityInterrupter);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        else
        {
            // Create a new interrupter
            ProximityInterrupter newInterrupter = ScriptableObject.CreateInstance<ProximityInterrupter>();
            newInterrupter.interrupterName = "ProximityInterrupter_" + (currentCharacterSO.interrupters.Count + 1);
            newInterrupter.MaxDistance = maxDistance;
            newInterrupter.MinDuration = minDuration;
            newInterrupter.Priority = priority;

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

        // Navigate back to the CharacterOverviewPanel
        interrupterConfigPanel.SetActive(false);
        interrupterTypeSelectionPanel.SetActive(false);
        characterOverviewManager.RefreshInterruptersScrollView();
        characterOverviewManager.PopulateInterrupterConfigureDropdown();
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
    /// Resets fields specific to Proximity Interrupter configuration.
    /// </summary>
    protected override void ResetSpecificFields(){}

    /// <summary>
    /// Initializes the ProximityInterrupterConfigManager.
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
