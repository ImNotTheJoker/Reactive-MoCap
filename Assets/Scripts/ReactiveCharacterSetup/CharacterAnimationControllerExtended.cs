using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>
/// Extended animation controller for managing main and reactive animations with smooth transitions.
/// Handles PlayableGraph setup and animation state management.
/// </summary>
public class CharacterAnimationControllerExtended : MonoBehaviour
{
    private PlayableGraph playableGraph; // The PlayableGraph managing the animations
    private AnimationMixerPlayable mainMixer; // Mixer with 3 inputs: Main, Reactive1, Reactive2
    private AnimationClipPlayable mainPlayable; // Playable for the main animation
    private AnimationClipPlayable reactivePlayable1; // Playable for the first reactive animation
    private AnimationClipPlayable reactivePlayable2; // Playable for the second reactive animation

    private bool isInitialized = false; // Indicates if the PlayableGraph has been initialized
    private int activeReactiveIndex = 1; // Toggles between reactive slots (1 or 2)
    private bool isTransitioning = false; // Prevents overlapping transitions

    private bool isPlayingMain = false; // Indicates if the main animation is currently playing

    private Vector3 previousPosition; // Stores the previous position for velocity calculation
    public float currentVelocity; // Raw velocity
    public float smoothVelocity = 0f; // Smoothed velocity
    private float smoothingFactor = 0.01f; // Smoothing factor for velocity

    /// <summary>
    /// Initializes the PlayableGraph and sets up the animation mixer.
    /// </summary>
    void Awake()
    {
        InitializeGraph();
        previousPosition = transform.position;
        GetComponent<Animator>().applyRootMotion = true;
    }

    /// <summary>
    /// Initializes the PlayableGraph and AnimationMixerPlayable.
    /// </summary>
    private void InitializeGraph()
    {
        if (isInitialized) return;

        playableGraph = PlayableGraph.Create("CharacterAnimationGraph");
        var playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", GetComponent<Animator>());

        // Create the AnimationMixerPlayable with 3 inputs: Main, Reactive1, Reactive2
        mainMixer = AnimationMixerPlayable.Create(playableGraph, 3);
        playableOutput.SetSourcePlayable(mainMixer);

        // Initialize mixer weights
        mainMixer.SetInputWeight(0, 0f); // Main inactive initially
        mainMixer.SetInputWeight(1, 0f); // Reactive1 inactive
        mainMixer.SetInputWeight(2, 0f); // Reactive2 inactive

        playableGraph.Play();
        isInitialized = true;
    }

    /// <summary>
    /// Calculates the character's velocity each frame.
    /// </summary>
    void Update()
    {
        Vector3 deltaPosition = transform.position - previousPosition;
        float rawVelocity = deltaPosition.magnitude / Time.deltaTime;
        currentVelocity = rawVelocity;
        smoothVelocity = Mathf.Lerp(smoothVelocity, rawVelocity, smoothingFactor); // Smooth the velocity

        previousPosition = transform.position;
    }

    /// <summary>
    /// Plays the main animation with an optional transition.
    /// </summary>
    /// <param name="mainClip">The main animation clip.</param>
    /// <param name="transitionDuration">Duration of the transition in seconds.</param>
    public void PlayMainAnimation(AnimationClip mainClip, float transitionDuration)
    {
        if (isPlayingMain)
        {
            // If the main animation is already playing, stop it before starting a new one
            StopMainAnimation();
        }

        if (mainClip == null)
        {
            Debug.LogError("Main Animation Clip is null!");
            return;
        }

        // Create the Playable for the main animation
        mainPlayable = AnimationClipPlayable.Create(playableGraph, mainClip);
        mainPlayable.SetApplyPlayableIK(true); // Enable IK for this animation
        mainPlayable.SetApplyFootIK(true);
        mainPlayable.SetTime(0); // Reset time to the start

        // Connect the main Playable to mixer input 0 (Main)
        playableGraph.Connect(mainPlayable, 0, mainMixer, 0);
        mainMixer.SetInputWeight(0, 1.0f); // Activate main animation

        mainPlayable.Play();

        isPlayingMain = true;
    }

    /// <summary>
    /// Pauses the main animation.
    /// </summary>
    public void PauseMainAnimation()
    {
        if (isPlayingMain && mainPlayable.IsValid())
        {
            mainPlayable.Pause();
        }
    }

    /// <summary>
    /// Resumes the main animation.
    /// </summary>
    public void ResumeMainAnimation()
    {
        if (isPlayingMain && mainPlayable.IsValid())
        {
            mainPlayable.Play();
        }
    }

    /// <summary>
    /// Stops the main animation and cleans up the Playable.
    /// </summary>
    public void StopMainAnimation()
    {
        if (isPlayingMain && mainPlayable.IsValid())
        {
            mainPlayable.Pause();
            mainMixer.SetInputWeight(0, 0f); // Deactivate main animation
            playableGraph.DestroyPlayable(mainPlayable);
            isPlayingMain = false;
        }
    }

    /// <summary>
    /// Plays a reactive animation with a smooth transition.
    /// </summary>
    /// <param name="reactiveClip">The reactive animation clip.</param>
    /// <param name="transitionDuration">Duration of the transition in seconds.</param>
    public void PlayReactiveAnimation(AnimationClip reactiveClip, float transitionDuration)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("Cannot start a new transition while another is in progress.");
            return;
        }

        if (reactiveClip == null)
        {
            Debug.LogWarning("ReactiveAnimation is null, cannot play.");
            return;
        }

        AnimationClipPlayable newReactivePlayable = AnimationClipPlayable.Create(playableGraph, reactiveClip);
        newReactivePlayable.SetApplyPlayableIK(true); // Enable IK for this animation
        newReactivePlayable.SetApplyFootIK(true);

        // Determine which reactive slot to use
        if (activeReactiveIndex == 1)
        {
            if (reactivePlayable1.IsValid()) DisconnectPlayable(reactivePlayable1, 1);
            reactivePlayable1 = newReactivePlayable;
            StartCoroutine(SmoothTransition(reactivePlayable1, 1, transitionDuration));
        }
        else
        {
            if (reactivePlayable2.IsValid()) DisconnectPlayable(reactivePlayable2, 2);
            reactivePlayable2 = newReactivePlayable;
            StartCoroutine(SmoothTransition(reactivePlayable2, 2, transitionDuration));
        }

        // Toggle the active reactive index for next time
        activeReactiveIndex = (activeReactiveIndex == 1) ? 2 : 1;
    }

    /// <summary>
    /// Coroutine for smoothly transitioning between animations.
    /// </summary>
    /// <param name="newPlayable">The new reactive Playable.</param>
    /// <param name="inputIndex">The mixer input index (1 or 2).</param>
    /// <param name="transitionDuration">Duration of the transition in seconds.</param>
    /// <returns>IEnumerator for the coroutine.</returns>
    private IEnumerator SmoothTransition(AnimationClipPlayable newPlayable, int inputIndex, float transitionDuration)
    {
        isTransitioning = true;

        // Connect the new reactive Playable to the mixer with initial weight 0
        ConnectPlayable(newPlayable, inputIndex, 0f);

        float timeElapsed = 0f;
        while (timeElapsed < transitionDuration)
        {
            timeElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timeElapsed / transitionDuration);

            if (inputIndex == 1 && mainMixer.GetInputWeight(2) > 0f)
            {
                // Transition from Reactive2 to Reactive1
                mainMixer.SetInputWeight(2, 1f - t); // Fade out Reactive2
                mainMixer.SetInputWeight(1, t);      // Fade in Reactive1
            }
            else if (inputIndex == 1)
            {
                // Transition from Main to Reactive1
                mainMixer.SetInputWeight(0, 1f - t); // Fade out Main
                mainMixer.SetInputWeight(1, t);     // Fade in Reactive1
                mainMixer.SetInputWeight(2, 0f);     // Ensure Reactive2 is inactive
            }
            else if (inputIndex == 2)
            {
                // Transition from Reactive1 to Reactive2
                mainMixer.SetInputWeight(0, 0f);          // Main is inactive
                mainMixer.SetInputWeight(1, 1f - t);      // Fade out Reactive1
                mainMixer.SetInputWeight(2, t);          // Fade in Reactive2
            }

            yield return null;
        }

        // Ensure the correct input weights after the transition
        if (inputIndex == 1)
        {
            mainMixer.SetInputWeight(0, 0f); // Main inactive
            mainMixer.SetInputWeight(1, 1f); // Reactive1 fully active
            mainMixer.SetInputWeight(2, 0f); // Reactive2 inactive
            DisconnectPlayable(reactivePlayable2, 2); // Release Reactive2 slot
        }
        else if (inputIndex == 2)
        {
            mainMixer.SetInputWeight(0, 0f); // Main inactive
            mainMixer.SetInputWeight(1, 0f); // Reactive1 inactive
            mainMixer.SetInputWeight(2, 1f); // Reactive2 fully active
            DisconnectPlayable(reactivePlayable1, 1); // Release Reactive1 slot
        }

        isTransitioning = false;
    }

    /// <summary>
    /// Smoothly transitions back to the main animation.
    /// </summary>
    /// <param name="transitionDuration">Duration of the transition in seconds.</param>
    public void ReturnToMainAnimation(float transitionDuration)
    {
        if (isTransitioning) return;
        StartCoroutine(SmoothReturnToMain(transitionDuration));
    }

    /// <summary>
    /// Coroutine for smoothly transitioning back to the main animation.
    /// </summary>
    /// <param name="transitionDuration">Duration of the transition in seconds.</param>
    /// <returns>IEnumerator for the coroutine.</returns>
    private IEnumerator SmoothReturnToMain(float transitionDuration)
    {
        isTransitioning = true;

        float timeElapsed = 0f;
        while (timeElapsed < transitionDuration)
        {
            timeElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timeElapsed / transitionDuration);

            if (mainMixer.GetInputWeight(2) > 0f) // Transition from Reactive2 to Main
            {
                mainMixer.SetInputWeight(2, 1f - t); // Fade out Reactive2
                mainMixer.SetInputWeight(0, t);      // Fade in Main
            }
            else if (mainMixer.GetInputWeight(1) > 0f) // Transition from Reactive1 to Main
            {
                mainMixer.SetInputWeight(1, 1f - t); // Fade out Reactive1
                mainMixer.SetInputWeight(0, t);      // Fade in Main
            }

            yield return null;
        }

        // Ensure the main animation is fully active
        mainMixer.SetInputWeight(0, 1f); // Main fully active
        mainMixer.SetInputWeight(1, 0f); // Reactive1 inactive
        mainMixer.SetInputWeight(2, 0f); // Reactive2 inactive

        DisconnectPlayable(reactivePlayable1, 1);
        DisconnectPlayable(reactivePlayable2, 2);

        activeReactiveIndex = 1;
        isTransitioning = false;
    }

    /// <summary>
    /// Stops all animations and destroys the PlayableGraph.
    /// </summary>
    private void StopAnimation()
    {
        StopMainAnimation();
    }

    /// <summary>
    /// Logs the current weights of the mixer inputs for debugging purposes.
    /// </summary>
    private void LogMixerWeights()
    {
        float weightMain = mainMixer.GetInputWeight(0);
        float weightReactive1 = mainMixer.GetInputWeight(1);
        float weightReactive2 = mainMixer.GetInputWeight(2);

        Debug.Log($"Mixer Weights: Main: {weightMain}, Reactive 1: {weightReactive1}, Reactive 2: {weightReactive2}");
    }

    /// <summary>
    /// Connects a Playable to the mixer at the specified input index with an initial weight.
    /// </summary>
    /// <param name="playable">The Playable to connect.</param>
    /// <param name="inputIndex">The mixer input index (1 or 2).</param>
    /// <param name="initialWeight">The initial weight for the Playable.</param>
    private void ConnectPlayable(AnimationClipPlayable playable, int inputIndex, float initialWeight)
    {
        mainMixer.ConnectInput(inputIndex, playable, 0);
        mainMixer.SetInputWeight(inputIndex, initialWeight);
    }

    /// <summary>
    /// Disconnects a Playable from the mixer and destroys it.
    /// </summary>
    /// <param name="playable">The Playable to disconnect.</param>
    /// <param name="inputIndex">The mixer input index (1 or 2).</param>
    private void DisconnectPlayable(AnimationClipPlayable playable, int inputIndex)
    {
        if (playable.IsValid())
        {
            playableGraph.DestroyPlayable(playable);
            mainMixer.SetInputWeight(inputIndex, 0f);
        }
    }

    /// <summary>
    /// Destroys the PlayableGraph when the object is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        if (playableGraph.IsValid())
        {
            playableGraph.Destroy();
        }
    }
}
