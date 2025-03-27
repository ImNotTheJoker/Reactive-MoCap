using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Singleton class that holds and manages scene-related data across different scenes.
/// Ensures that only one instance exists and persists between scene loads.
/// </summary>
public class SceneDataHolder : MonoBehaviour
{
    /// <summary>
    /// Singleton instance of the SceneDataHolder.
    /// </summary>
    public static SceneDataHolder Instance { get; private set; }

    /// <summary>
    /// The currently active ReactiveScene ScriptableObject.
    /// </summary>
    public static ReactiveSceneSO currentScene;

    /// <summary>
    /// The currently selected character instance in the scene.
    /// </summary>
    public static GameObject selectedCharacterInstance;

    /// <summary>
    /// The ReactiveCharacter ScriptableObject of the selected character.
    /// </summary>
    public static ReactiveCharacterSO selectedCharacterSO;

    /// <summary>
    /// The name of the currently selected character.
    /// </summary>
    public static string currentCharacterName;

    /// <summary>
    /// The prefab of the currently selected character.
    /// </summary>
    public static GameObject selectedCharacterPrefab;

    /// <summary>
    /// Initializes the singleton instance and loads the current scene data.
    /// </summary>
    void Awake()
    {
        // Ensure only one instance exists
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
        
        // Set currentScene based on the active scene
        string activeSceneName = SceneManager.GetActiveScene().name;
        string reactiveScenePath = $"ReactiveScenes/{activeSceneName}/ReactiveSceneSO.asset";

#if UNITY_EDITOR
        // Load the ReactiveSceneSO asset from the specified path
        currentScene = AssetDatabase.LoadAssetAtPath<ReactiveSceneSO>($"Assets/{reactiveScenePath}");
        if (currentScene == null)
        {
            Debug.LogError($"ReactiveSceneSO not found at path: Assets/{reactiveScenePath}");
        }
        else
        {
            Debug.Log($"ReactiveSceneSO for {activeSceneName} loaded successfully.");
        }
#else
        Debug.LogError("ReactiveSceneSO loading for runtime builds is not implemented.");
#endif
    }
}
