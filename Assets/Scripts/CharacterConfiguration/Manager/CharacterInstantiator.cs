using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using TMPro;
using UnityEngine.SceneManagement;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.DistanceReticles;

/// <summary>
/// Manages the instantiation and configuration of characters within the Reactive Character Configuration scene.
/// Handles character creation, assignment of necessary components, and integration with the UI and scene data.
/// </summary>
public class CharacterInstantiator : MonoBehaviour
{
    [Header("Prefab Settings")]
    /// <summary>
    /// The transform point where characters are instantiated in the scene.
    /// </summary>
    public Transform characterPlacementPoint;

    [Header("Manager References")]
    /// <summary>
    /// Reference to the CharacterListManager script.
    /// </summary>
    private CharacterListManager characterListManager;

    /// <summary>
    /// Reference to the UIManager script.
    /// </summary>
    private UIManager uiManager;

    [Header("Scene Configuration")]
    /// <summary>
    /// The current ReactiveScene ScriptableObject containing scene-specific data.
    /// </summary>
    private ReactiveSceneSO currentSceneSO;

    /// <summary>
    /// Dictionary mapping character names to their instantiated GameObjects.
    /// </summary>
    private Dictionary<string, GameObject> instantiatedCharacters = new Dictionary<string, GameObject>();

    /// <summary>
    /// Initializes references to managers and validates essential components.
    /// </summary>
    void Awake()
    {
        // Retrieve references to CharacterListManager and UIManager
        characterListManager = GetComponent<CharacterListManager>();
        uiManager = GetComponent<UIManager>();

        // Validate references
        if (characterListManager == null)
        {
            Debug.LogError("CharacterListManager not found.");
        }
        if (uiManager == null)
        {
            Debug.LogError("UIManager not found.");
        }

        // Validate characterPlacementPoint
        if (characterPlacementPoint == null)
        {
            Debug.LogError("CharacterPlacementPoint is not assigned in the Inspector.");
        }
    }

    /// <summary>
    /// Handles the configuration process when the Configure button is clicked.
    /// Instantiates or retrieves existing characters based on user selection and updates the scene data.
    /// </summary>
    /// <param name="characterDropdown">The dropdown containing character options.</param>
    public void HandleConfigureButton(TMP_Dropdown characterDropdown)
    {
#if UNITY_EDITOR
        // Load the current ReactiveSceneSO from the SceneDataHolder
        currentSceneSO = SceneDataHolder.currentScene;
        if (currentSceneSO == null)
        {
            Debug.LogError("No ReactiveSceneSO assigned. Ensure that SceneDataHolder.currentScene is set.");
            return;
        }

        // Get the selected character name from the dropdown
        string selectedCharacterName = characterDropdown.options[characterDropdown.value].text;
        GameObject selectedCharacterPrefab = currentSceneSO.selectedCharacterPrefabs.Find(c => c.name == selectedCharacterName);

        if (selectedCharacterPrefab == null)
        {
            Debug.LogError($"Character prefab not found: {selectedCharacterName}");
            return;
        }

        // Check if a ReactiveCharacterSO already exists for the selected character
        ReactiveCharacterSO existingCharacterSO = currentSceneSO.reactiveCharacters.Find(c => c.characterName == selectedCharacterName);

        if (existingCharacterSO != null)
        {
            // Character already exists, set SceneDataHolder and open OverviewPanel
            SceneDataHolder.selectedCharacterSO = existingCharacterSO;

            if (characterListManager.GetInstantiatedCharacters().ContainsKey(selectedCharacterName))
            {
                SceneDataHolder.selectedCharacterInstance = characterListManager.GetInstantiatedCharacters()[selectedCharacterName];
            }
            else
            {
                // If the instance does not exist, instantiate it
                GameObject characterInstance = InstantiateAndConfigureCharacter(existingCharacterSO);
                if (characterInstance != null)
                {
                    characterListManager.GetInstantiatedCharacters().Add(selectedCharacterName, characterInstance);
                    SceneDataHolder.selectedCharacterInstance = characterInstance;
                }
                else
                {
                    Debug.LogError($"Instantiation of character {selectedCharacterName} failed.");
                }
            }

            // Set the prefab and current character name in SceneDataHolder
            SceneDataHolder.selectedCharacterPrefab = existingCharacterSO.characterPrefab;
            SceneDataHolder.currentCharacterName = existingCharacterSO.characterName;
        }
        else
        {
            // Character does not exist, instantiate and create a new ReactiveCharacterSO
            if (characterPlacementPoint == null)
            {
                Debug.LogError("CharacterPlacementPoint is not assigned. Cannot instantiate character.");
                return;
            }

            // Instantiate the character at the specified placement point with a default rotation
            GameObject characterInstance = Instantiate(selectedCharacterPrefab, characterPlacementPoint.position, Quaternion.Euler(0, 180, 0));
            characterInstance.name = selectedCharacterName;

            // Assign necessary components and child objects
            AddRequiredComponents(characterInstance);
            AddGrabInteractableComponents(characterInstance);

            // Add the instantiated character to the CharacterListManager
            characterListManager.GetInstantiatedCharacters().Add(selectedCharacterName, characterInstance);
            SceneDataHolder.selectedCharacterInstance = characterInstance;

            // Create a new ReactiveCharacterSO
            ReactiveCharacterSO newCharacterSO = ScriptableObject.CreateInstance<ReactiveCharacterSO>();
            newCharacterSO.Initialize(selectedCharacterName, selectedCharacterPrefab, characterInstance.transform.position, characterInstance.transform.eulerAngles);

            // Add the CharacterAnimationControllerSetup component
            CharacterAnimationControllerSetup animController = characterInstance.GetComponent<CharacterAnimationControllerSetup>();
            if (animController == null)
            {
                animController = characterInstance.AddComponent<CharacterAnimationControllerSetup>();
                animController.Initialize(newCharacterSO); // Ensure newCharacterSO is available
            }
            else
            {
                animController.Initialize(newCharacterSO);
            }

            // Define the path to the character's folder
            string sceneName = currentSceneSO.sceneName;
            string characterFolderPath = $"Assets/ReactiveScenes/{sceneName}/Characters/{selectedCharacterName}";

            // Ensure the folder exists
            if (!AssetDatabase.IsValidFolder(characterFolderPath))
            {
                AssetDatabase.CreateFolder($"Assets/ReactiveScenes/{sceneName}/Characters", selectedCharacterName);
            }

            // Save the new ReactiveCharacterSO asset
            string soPath = $"{characterFolderPath}/{selectedCharacterName}SO.asset";
            AssetDatabase.CreateAsset(newCharacterSO, soPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Add the new ReactiveCharacterSO to the ReactiveSceneSO
            currentSceneSO.AddCharacter(newCharacterSO);

            // Update SceneDataHolder with the new character's details
            SceneDataHolder.selectedCharacterSO = newCharacterSO;
            SceneDataHolder.selectedCharacterPrefab = selectedCharacterPrefab;
            SceneDataHolder.currentCharacterName = selectedCharacterName;
        }

        // Switch to the CharacterOverviewPanel
        if (uiManager != null)
        {
            uiManager.SwitchToOverviewPanel();
        }
        else
        {
            Debug.LogError("UIManager is not assigned.");
        }
#endif
    }

    /// <summary>
    /// Handles the Back button click event by navigating to the RecordingScene.
    /// </summary>
    public void HandleBackButton()
    {
#if UNITY_EDITOR
        // Navigate back to the "RecordingScene" or main menu
        SceneManager.LoadScene("RecordingScene");
#endif
    }

    /// <summary>
    /// Handles the Next button click event by navigating to the ReactiveScene.
    /// </summary>
    public void HandleNextButton()
    {
#if UNITY_EDITOR
        // Navigate forward to the "ReactiveScene"
        SceneManager.LoadScene("ReactiveScene");
#endif
    }

    /// <summary>
    /// Instantiates and configures a character based on the provided ReactiveCharacterSO.
    /// Assigns necessary components and sets up the character for interaction.
    /// </summary>
    /// <param name="characterSO">The ReactiveCharacterSO containing character data.</param>
    /// <returns>The instantiated and configured GameObject of the character.</returns>
    public GameObject InstantiateAndConfigureCharacter(ReactiveCharacterSO characterSO)
    {
#if UNITY_EDITOR
        if (characterSO == null)
        {
            Debug.LogError("CharacterSO is null.");
            return null;
        }

        if (characterSO.characterPrefab == null)
        {
            Debug.LogError($"CharacterPrefab for {characterSO.characterName} is null.");
            return null;
        }

        // Instantiate the character at the saved position and rotation
        GameObject characterInstance = Instantiate(characterSO.characterPrefab, characterSO.characterPosition, Quaternion.Euler(characterSO.characterRotation));
        characterInstance.name = characterSO.characterName; // Remove the "(Clone)" suffix

        // Initialize the Animation Controller
        CharacterAnimationControllerSetup animController = characterInstance.GetComponent<CharacterAnimationControllerSetup>();
        if (animController != null)
        {
            animController.Initialize(characterSO);
        }
        else
        {
            animController = characterInstance.AddComponent<CharacterAnimationControllerSetup>();
            animController.Initialize(characterSO);
        }

        // Assign necessary components and child objects
        AddRequiredComponents(characterInstance);
        AddGrabInteractableComponents(characterInstance);

        return characterInstance;
#else
        return null;
#endif
    }

    /// <summary>
    /// Adds essential components and child objects required for character interaction and manipulation.
    /// </summary>
    /// <param name="character">The character GameObject to configure.</param>
    private void AddRequiredComponents(GameObject character)
    {
#if UNITY_EDITOR
        // Add Rigidbody if not present
        Rigidbody rb = character.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = character.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Prevent the character from being affected by gravity
        }

        // Add BoxCollider if not present
        if (character.GetComponent<BoxCollider>() == null)
        {
            BoxCollider boxCollider = character.AddComponent<BoxCollider>();
            boxCollider.isTrigger = false; // Set based on requirements
        }

        // Add Grabbable component if not present
        Grabbable grabbable = character.GetComponent<Grabbable>();
        if (grabbable == null)
        {
            grabbable = character.AddComponent<Grabbable>();
        }
        grabbable.InjectOptionalRigidbody(rb); // Assign Rigidbody to Grabbable
        grabbable.InjectOptionalThrowWhenUnselected(false); // Disable ThrowWhenUnselected

        // Add GrabFreeTransformer component if not present
        GrabFreeTransformer grabFreeTransformer = character.GetComponentInChildren<GrabFreeTransformer>();
        if (grabFreeTransformer == null)
        {
            grabFreeTransformer = character.AddComponent<GrabFreeTransformer>();
        }

        // Configure Position Constraints
        grabFreeTransformer.InjectOptionalPositionConstraints(new TransformerUtils.PositionConstraints()
        {
            XAxis = new TransformerUtils.ConstrainedAxis() { ConstrainAxis = false },
            YAxis = new TransformerUtils.ConstrainedAxis() { ConstrainAxis = true },
            ZAxis = new TransformerUtils.ConstrainedAxis() { ConstrainAxis = false }
        });

        // Configure Rotation Constraints
        grabFreeTransformer.InjectOptionalRotationConstraints(new TransformerUtils.RotationConstraints()
        {
            XAxis = new TransformerUtils.ConstrainedAxis()
            {
                ConstrainAxis = true,
                AxisRange = new TransformerUtils.FloatRange() { Min = 0f, Max = 0f }
            },
            YAxis = new TransformerUtils.ConstrainedAxis()
            {
                ConstrainAxis = false, // Y-axis free for rotation
                AxisRange = new TransformerUtils.FloatRange() { Min = -360f, Max = 360f }
            },
            ZAxis = new TransformerUtils.ConstrainedAxis()
            {
                ConstrainAxis = true,
                AxisRange = new TransformerUtils.FloatRange() { Min = 0f, Max = 0f }
            }
        });

        // Assign GrabFreeTransformer to Grabbable
        grabbable.InjectOptionalOneGrabTransformer(grabFreeTransformer);

        // Add the RotationSphere as a child object for rotation handling
        AddRotationSphere(character);
#endif
    }

    /// <summary>
    /// Adds a RotationSphere to the character for handling rotation interactions.
    /// Configures necessary components and constraints on the RotationSphere.
    /// </summary>
    /// <param name="character">The character GameObject to which the RotationSphere is added.</param>
    private void AddRotationSphere(GameObject character)
    {
#if UNITY_EDITOR
        // Create the RotationSphere as a child of the character
        GameObject rotationSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rotationSphere.name = "RotationSphere";
        rotationSphere.transform.SetParent(character.transform, false);

        // Position the RotationSphere above the character based on the character's collider bounds
        Collider characterCollider = character.GetComponent<Collider>();
        if (characterCollider != null)
        {
            Bounds characterBounds = characterCollider.bounds;
            // Calculate the local position based on bounds
            Vector3 worldPosition = new Vector3(characterBounds.center.x, characterBounds.max.y + 0.5f, characterBounds.center.z);
            Vector3 localPosition = character.transform.InverseTransformPoint(worldPosition);
            rotationSphere.transform.localPosition = localPosition;
        }
        else
        {
            rotationSphere.transform.localPosition = new Vector3(0, 1f, 0); // Default position if no collider is present
        }

        // Scale the RotationSphere to make it recognizable as a grab point
        rotationSphere.transform.localScale = Vector3.one * 0.2f;

        // Optionally disable visibility of the RotationSphere
        MeshRenderer meshRenderer = rotationSphere.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }

        // Add Grabbable component to the RotationSphere
        Grabbable sphereGrabbable = rotationSphere.AddComponent<Grabbable>();
        Rigidbody sphereRb = rotationSphere.AddComponent<Rigidbody>();
        sphereRb.isKinematic = true;
        sphereGrabbable.InjectOptionalRigidbody(sphereRb);
        sphereGrabbable.InjectOptionalThrowWhenUnselected(false);

        // Add GrabFreeTransformer component to the RotationSphere
        GrabFreeTransformer grabFreeTransformer = rotationSphere.AddComponent<GrabFreeTransformer>();

        // Configure Position Constraints
        grabFreeTransformer.InjectOptionalPositionConstraints(new TransformerUtils.PositionConstraints()
        {
            XAxis = new TransformerUtils.ConstrainedAxis() { ConstrainAxis = true },
            YAxis = new TransformerUtils.ConstrainedAxis() { ConstrainAxis = true },
            ZAxis = new TransformerUtils.ConstrainedAxis() { ConstrainAxis = true },
            ConstraintsAreRelative = true
        });

        // Configure Rotation Constraints
        grabFreeTransformer.InjectOptionalRotationConstraints(new TransformerUtils.RotationConstraints()
        {
            XAxis = new TransformerUtils.ConstrainedAxis()
            {
                ConstrainAxis = true,
                AxisRange = new TransformerUtils.FloatRange() { Min = 0f, Max = 0f }
            },
            YAxis = new TransformerUtils.ConstrainedAxis()
            {
                ConstrainAxis = false, // Y-axis free for rotation
                AxisRange = new TransformerUtils.FloatRange() { Min = -360f, Max = 360f }
            },
            ZAxis = new TransformerUtils.ConstrainedAxis()
            {
                ConstrainAxis = true,
                AxisRange = new TransformerUtils.FloatRange() { Min = 0f, Max = 0f }
            }
        });

        // Assign GrabFreeTransformer to Grabbable
        sphereGrabbable.InjectOptionalOneGrabTransformer(grabFreeTransformer);

        // Add HandGrabInteractable components to the RotationSphere
        GameObject sphereHandGrabInteractable = new GameObject("SphereHandGrabInteractable");
        sphereHandGrabInteractable.transform.SetParent(rotationSphere.transform, false);

        // Add HandGrabInteractable component
        var sphereHandGrabComp = sphereHandGrabInteractable.AddComponent<HandGrabInteractable>();
        sphereHandGrabComp.InjectRigidbody(sphereRb); // Assign Rigidbody
        sphereHandGrabComp.InjectOptionalPointableElement(sphereGrabbable); // Assign PointableElement
        sphereHandGrabComp.ResetGrabOnGrabsUpdated = false; // Disable ResetGrabOnGrabsUpdated

        // Add DistanceHandGrabInteractable component
        var sphereDistanceHandGrabComp = sphereHandGrabInteractable.AddComponent<DistanceHandGrabInteractable>();
        sphereDistanceHandGrabComp.InjectRigidbody(sphereRb); // Assign Rigidbody
        sphereDistanceHandGrabComp.InjectOptionalPointableElement(sphereGrabbable); // Assign PointableElement
        sphereDistanceHandGrabComp.ResetGrabOnGrabsUpdated = false; // Disable ResetGrabOnGrabsUpdated
        sphereDistanceHandGrabComp.HandAlignment = HandAlignType.None;

        // Add MoveFromTargetProvider component
        MoveFromTargetProvider sphereMoveFromTargetProvider = sphereHandGrabInteractable.AddComponent<MoveFromTargetProvider>();
        sphereDistanceHandGrabComp.InjectOptionalMovementProvider(sphereMoveFromTargetProvider); // Assign MovementProvider

        // Add RotationHandler script to handle rotation logic
        RotationHandler rotationHandler = rotationSphere.AddComponent<RotationHandler>();
        rotationHandler.characterTransform = character.transform; // Set the character as the target for rotation
#endif
    }

    /// <summary>
    /// Adds Grabbable and HandGrabInteractable components to the character for interaction purposes.
    /// </summary>
    /// <param name="character">The character GameObject to configure.</param>
    private void AddGrabInteractableComponents(GameObject character)
    {
#if UNITY_EDITOR
        // Retrieve the Rigidbody from the character
        Rigidbody characterRigidbody = character.GetComponent<Rigidbody>();
        if (characterRigidbody == null)
        {
            Debug.LogError("Rigidbody is missing from the character.");
            return;
        }

        // Retrieve the Grabbable component from the character
        Grabbable grabbable = character.GetComponent<Grabbable>();
        if (grabbable == null)
        {
            Debug.LogError("Grabbable component is missing on the character.");
            return;
        }

        // Create and configure the HandGrabInteractable child object
        GameObject handGrabInteractable = new GameObject("HandGrabInteractable");
        handGrabInteractable.transform.SetParent(character.transform, false);

        // Add HandGrabInteractable component
        var handGrabComp = handGrabInteractable.AddComponent<HandGrabInteractable>();
        handGrabComp.InjectRigidbody(characterRigidbody); // Assign Rigidbody
        handGrabComp.InjectOptionalPointableElement(grabbable); // Assign PointableElement
        handGrabComp.ResetGrabOnGrabsUpdated = false; // Disable ResetGrabOnGrabsUpdated

        // Add DistanceHandGrabInteractable component
        var distanceHandGrabComp = handGrabInteractable.AddComponent<DistanceHandGrabInteractable>();
        distanceHandGrabComp.InjectRigidbody(characterRigidbody); // Assign Rigidbody
        distanceHandGrabComp.InjectOptionalPointableElement(grabbable); // Assign PointableElement
        distanceHandGrabComp.ResetGrabOnGrabsUpdated = false; // Disable ResetGrabOnGrabsUpdated
        distanceHandGrabComp.HandAlignment = HandAlignType.None;

        // Add MoveFromTargetProvider component
        MoveFromTargetProvider moveFromTargetProvider = handGrabInteractable.AddComponent<MoveFromTargetProvider>();
        distanceHandGrabComp.InjectOptionalMovementProvider(moveFromTargetProvider); // Assign MovementProvider

        // Add ReticleDataIcon component
        // var reticleDataIcon = handGrabInteractable.AddComponent<ReticleDataIcon>();

        // Create and configure the GrabInteractable child object
        GameObject grabInteractable = new GameObject("GrabInteractable");
        grabInteractable.transform.SetParent(character.transform, false);

        // Add GrabInteractable component
        var grabInteractableComp = grabInteractable.AddComponent<GrabInteractable>();
        grabInteractableComp.InjectRigidbody(characterRigidbody); // Assign Rigidbody
        grabInteractableComp.InjectOptionalPointableElement(grabbable); // Assign PointableElement
        grabInteractableComp.ResetGrabOnGrabsUpdated = false; // Disable ResetGrabOnGrabsUpdated

        // Add DistanceGrabInteractable component to the same child object
        var distanceGrabOnGrabInteractable = grabInteractable.AddComponent<DistanceGrabInteractable>();
        distanceGrabOnGrabInteractable.InjectRigidbody(characterRigidbody); // Assign Rigidbody
        distanceGrabOnGrabInteractable.InjectOptionalPointableElement(grabbable); // Assign PointableElement
        distanceGrabOnGrabInteractable.ResetGrabOnGrabsUpdated = false; // Disable ResetGrabOnGrabsUpdated
#endif
    }
}
