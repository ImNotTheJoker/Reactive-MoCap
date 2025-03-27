using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the user interface for the Reactive Character Configuration scene.
/// Handles character selection, configuration, saving, undo operations, and scene navigation.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    /// <summary>
    /// Button to navigate back to the previous scene.
    /// </summary>
    public Button backButton;

    /// <summary>
    /// Button to save configurations and navigate to the next scene.
    /// </summary>
    public Button nextButton;

    /// <summary>
    /// Dropdown menu for selecting characters.
    /// </summary>
    public TMP_Dropdown characterDropdown;

    /// <summary>
    /// Button to configure the selected character.
    /// </summary>
    public Button configureButton;

    /// <summary>
    /// Scroll view to display configured characters.
    /// </summary>
    public ScrollRect configuredCharactersScrollView;

    /// <summary>
    /// Button to save all configurations.
    /// </summary>
    public Button saveAllButton;

    /// <summary>
    /// Button to undo the last configuration action.
    /// </summary>
    public Button undoButton;

    /// <summary>
    /// Button to play the main animation.
    /// </summary>
    public Button playMainButton;

    /// <summary>
    /// Button to stop all animations.
    /// </summary>
    public Button stopButton;

    [Header("Panels")]
    /// <summary>
    /// Panel for character selection.
    /// </summary>
    public GameObject characterSelectionPanel;

    /// <summary>
    /// Panel for character overview.
    /// </summary>
    public GameObject characterOverviewPanel;

    [Header("Prefab Assignments")]
    /// <summary>
    /// Prefab for character buttons in the scroll view.
    /// </summary>
    public GameObject characterButtonPrefab;

    [Header("Prefab Settings")]
    /// <summary>
    /// Point where the character is initially placed.
    /// </summary>
    public Transform characterPlacementPoint;

    [Header("Notification Elements")]
    /// <summary>
    /// UI text element for displaying notifications.
    /// </summary>
    public TextMeshProUGUI notificationText;

    /// <summary>
    /// Duration for which the notification is displayed.
    /// </summary>
    public float notificationDuration = 2f;

    /// <summary>
    /// Reference to the CharacterListManager script.
    /// </summary>
    private CharacterListManager characterListManager;

    /// <summary>
    /// Reference to the CharacterInstantiator script.
    /// </summary>
    private CharacterInstantiator characterInstantiator;

    /// <summary>
    /// Reference to the SaveManager script.
    /// </summary>
    private SaveManager saveManager;

    /// <summary>
    /// Name of the previous scene for navigation.
    /// </summary>
    public string previousSceneName;

    /// <summary>
    /// Name of the next scene for navigation.
    /// </summary>
    public string nextSceneName;

    /// <summary>
    /// Name of the last recorded animation clip.
    /// </summary>
    private string lastRecordedClipName;

    /// <summary>
    /// Initializes the UI Manager by setting up references and event listeners.
    /// </summary>
    void Awake()
    {
        // Initialize managers
        characterListManager = GetComponent<CharacterListManager>();
        characterInstantiator = GetComponent<CharacterInstantiator>();
        saveManager = GetComponent<SaveManager>();

        // Validate managers
        if (characterListManager == null)
        {
            Debug.LogError("CharacterListManager not found.");
        }
        if (characterInstantiator == null)
        {
            Debug.LogError("CharacterInstantiator not found.");
        }
        if (saveManager == null)
        {
            Debug.LogError("SaveManager not found.");
        }

        // Add event listeners to buttons
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
        else
        {
            Debug.LogError("BackButton is not assigned.");
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
        else
        {
            Debug.LogError("NextButton is not assigned.");
        }

        if (configureButton != null)
        {
            configureButton.onClick.AddListener(OnConfigureButtonClicked);
        }
        else
        {
            Debug.LogError("ConfigureButton is not assigned.");
        }

        if (saveAllButton != null)
        {
            saveAllButton.onClick.AddListener(OnSaveAllButtonClicked);
        }
        else
        {
            Debug.LogError("SaveAllButton is not assigned.");
        }

        // Add event listener to Undo button
        if (undoButton != null)
        {
            undoButton.onClick.AddListener(OnUndoButtonClicked);
        }
        else
        {
            Debug.LogError("UndoButton is not assigned.");
        }

        // Add event listener to Play Main button
        if (playMainButton != null)
        {
            playMainButton.onClick.AddListener(OnPlayMainButtonClicked);
        }
        else
        {
            Debug.LogError("PlayMainButton is not assigned.");
        }

        // Add event listener to Stop button
        if (stopButton != null)
        {
            stopButton.onClick.AddListener(OnStopButtonClicked);
        }
        else
        {
            Debug.LogError("StopButton is not assigned.");
        }
    }

    /// <summary>
    /// Sets up the initial UI state and populates dropdowns.
    /// </summary>
    void Start()
    {
        if (characterListManager != null)
        {
            characterListManager.PopulateCharacterDropdown(characterDropdown);
            characterListManager.LoadConfiguredCharacters(); // Load configured characters
        }
        else
        {
            Debug.LogError("CharacterListManager not found.");
        }
    }

    /// <summary>
    /// Handles the Back button click event by navigating to the previous scene.
    /// </summary>
    private void OnBackButtonClicked()
    {
        SceneManager.LoadScene(previousSceneName);
    }

    /// <summary>
    /// Handles the Next button click event by navigating to the next scene.
    /// </summary>
    private void OnNextButtonClicked()
    {
        SceneManager.LoadScene(nextSceneName);
    }

    /// <summary>
    /// Handles the Configure button click event by initiating character configuration.
    /// </summary>
    private void OnConfigureButtonClicked()
    {
        if (characterInstantiator != null)
        {
            characterInstantiator.HandleConfigureButton(characterDropdown);
        }
        else
        {
            Debug.LogError("CharacterInstantiator is not assigned.");
        }
    }

    /// <summary>
    /// Handles the Save All button click event by saving all configurations.
    /// </summary>
    private void OnSaveAllButtonClicked()
    {
        if (saveManager != null)
        {
            saveManager.HandleSaveAll();
        }
        else
        {
            Debug.LogError("SaveManager is not assigned.");
        }
    }

    /// <summary>
    /// Handles the Undo button click event by undoing the last deletion.
    /// </summary>
    private void OnUndoButtonClicked()
    {
        if (characterListManager != null)
        {
            characterListManager.UndoLastDeletion();
        }
        else
        {
            Debug.LogError("CharacterListManager is not assigned.");
        }
    }

    /// <summary>
    /// Handles the Play Main button click event by playing main animations on all instantiated characters.
    /// </summary>
    private void OnPlayMainButtonClicked()
    {
        if (characterListManager != null)
        {
            var instantiatedCharacters = characterListManager.GetInstantiatedCharacters();

            foreach (var characterEntry in instantiatedCharacters)
            {
                GameObject characterObj = characterEntry.Value;
                CharacterAnimationControllerSetup animController = characterObj.GetComponent<CharacterAnimationControllerSetup>();

                if (animController != null)
                {
                    animController.PlayMainAnimation();
                }
                else
                {
                    Debug.LogWarning($"CharacterAnimationControllerSetup for {characterEntry.Key} not found.");
                }
            }

            ShowNotification("Main animations started.");
        }
        else
        {
            Debug.LogError("CharacterListManager is not assigned.");
        }
    }

    /// <summary>
    /// Handles the Stop button click event by stopping all animations and resetting characters.
    /// </summary>
    private void OnStopButtonClicked()
    {
        if (characterListManager != null)
        {
            var instantiatedCharacters = characterListManager.GetInstantiatedCharacters();

            foreach (var characterEntry in instantiatedCharacters)
            {
                GameObject characterObj = characterEntry.Value;
                CharacterAnimationControllerSetup animController = characterObj.GetComponent<CharacterAnimationControllerSetup>();

                if (animController != null)
                {
                    animController.StopAnimation();
                }
                else
                {
                    Debug.LogWarning($"CharacterAnimationControllerSetup for {characterEntry.Key} not found.");
                }
            }

            ShowNotification("All animations stopped and characters reset.");
        }
        else
        {
            Debug.LogError("CharacterListManager is not assigned.");
        }
    }

    /// <summary>
    /// Displays a notification message for a specified duration.
    /// </summary>
    /// <param name="message">The notification message to display.</param>
    public void ShowNotification(string message)
    {
        if (notificationText != null)
        {
            notificationText.text = message;
            StartCoroutine(ClearNotificationAfterDelay(notificationDuration));
        }
        else
        {
            Debug.LogWarning("NotificationText is not assigned.");
        }
    }

    /// <summary>
    /// Coroutine that clears the notification text after a delay.
    /// </summary>
    /// <param name="delay">The delay in seconds before clearing the notification.</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
    private IEnumerator ClearNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (notificationText != null)
        {
            notificationText.text = "";
        }
    }

    /// <summary>
    /// Switches the UI to the character overview panel.
    /// </summary>
    public void SwitchToOverviewPanel()
    {
        if (characterOverviewPanel != null && characterSelectionPanel != null)
        {
            characterOverviewPanel.SetActive(true);
            characterSelectionPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("Panels are not correctly assigned.");
        }
    }

    /// <summary>
    /// Switches the UI to the character selection panel.
    /// </summary>
    public void SwitchToSelectionPanel()
    {
        if (characterOverviewPanel != null && characterSelectionPanel != null)
        {
            characterOverviewPanel.SetActive(false);
            characterSelectionPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("Panels are not correctly assigned.");
        }
    }
}
