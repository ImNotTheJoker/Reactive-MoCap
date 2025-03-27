using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the selection and navigation of different interrupter configuration panels within the UI.
/// Handles user interactions for selecting interrupter types and navigating between panels.
/// </summary>
public class InterrupterTypeSelectionManager : MonoBehaviour
{
    [Header("Interrupter Type Buttons")]
    /// <summary>
    /// Button to select and configure an Audio Interrupter.
    /// </summary>
    public Button audioInterrupterButton;

    /// <summary>
    /// Button to select and configure a Proximity Interrupter.
    /// </summary>
    public Button proximityInterrupterButton;

    /// <summary>
    /// Button to select and configure a Velocity Interrupter.
    /// </summary>
    public Button velocityInterrupterButton;

    /// <summary>
    /// Button to select and configure a LookAt Interrupter.
    /// </summary>
    public Button lookAtInterrupterButton;

    /// <summary>
    /// Button to navigate back to the Character Overview Panel.
    /// </summary>
    public Button backButton;

    [Header("Interrupter Configuration Panels")]
    /// <summary>
    /// Panel for configuring Audio Interrupters.
    /// </summary>
    public GameObject audioInterrupterPanel;

    /// <summary>
    /// Panel for configuring Proximity Interrupters.
    /// </summary>
    public GameObject proximityInterrupterPanel;

    /// <summary>
    /// Panel for configuring Velocity Interrupters.
    /// </summary>
    public GameObject velocityInterrupterPanel;

    /// <summary>
    /// Panel for configuring LookAt Interrupters.
    /// </summary>
    public GameObject lookAtInterrupterPanel;

    [Header("Navigation Panels")]
    /// <summary>
    /// Panel for character overview.
    /// </summary>
    public GameObject characterOverviewPanel;

    /// <summary>
    /// Panel for selecting interrupter types.
    /// </summary>
    public GameObject interrupterTypeSelectionPanel;

    /// <summary>
    /// Initializes the button listeners when the script starts.
    /// </summary>
    void Start()
    {
        // Assign event listeners to interrupter type buttons
        audioInterrupterButton.onClick.AddListener(OpenAudioInterrupterPanel);
        proximityInterrupterButton.onClick.AddListener(OpenProximityInterrupterPanel);
        velocityInterrupterButton.onClick.AddListener(OpenVelocityInterrupterPanel);
        lookAtInterrupterButton.onClick.AddListener(OpenLookAtInterrupterPanel);
        
        // Assign event listener to back button
        backButton.onClick.AddListener(ReturnToCharacterOverviewPanel);
    }

    /// <summary>
    /// Opens the Audio Interrupter Configuration Panel.
    /// </summary>
    private void OpenAudioInterrupterPanel()
    {
        interrupterTypeSelectionPanel.SetActive(false);
        audioInterrupterPanel.SetActive(true);
    }

    /// <summary>
    /// Opens the Proximity Interrupter Configuration Panel.
    /// </summary>
    private void OpenProximityInterrupterPanel()
    {
        interrupterTypeSelectionPanel.SetActive(false);
        proximityInterrupterPanel.SetActive(true);
    }

    /// <summary>
    /// Opens the Velocity Interrupter Configuration Panel.
    /// </summary>
    private void OpenVelocityInterrupterPanel()
    {
        interrupterTypeSelectionPanel.SetActive(false);
        velocityInterrupterPanel.SetActive(true);
    }

    /// <summary>
    /// Opens the LookAt Interrupter Configuration Panel.
    /// </summary>
    private void OpenLookAtInterrupterPanel()
    {
        interrupterTypeSelectionPanel.SetActive(false);
        lookAtInterrupterPanel.SetActive(true);
    }

    /// <summary>
    /// Returns to the Character Overview Panel from the Interrupter Type Selection Panel.
    /// </summary>
    private void ReturnToCharacterOverviewPanel()
    {
        interrupterTypeSelectionPanel.SetActive(false);
        characterOverviewPanel.SetActive(true);
    }
}
