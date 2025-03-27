using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.Collections;

/// <summary>
/// Controls the animation playback for a character, managing PlayableGraph and AnimationMixer for smooth transitions.
/// </summary>
public class CharacterAnimationControllerSetup : MonoBehaviour
{
    [Header("Animation Configuration")]
    /// <summary>
    /// The PlayableGraph used to manage animations.
    /// </summary>
    private PlayableGraph playableGraph;

    /// <summary>
    /// The AnimationMixerPlayable used to blend animations.
    /// </summary>
    private AnimationMixerPlayable mixerPlayable;

    /// <summary>
    /// The primary AnimationClipPlayable for the main animation.
    /// </summary>
    private AnimationClipPlayable mainAnimationPlayable;

    /// <summary>
    /// The secondary AnimationClipPlayable for blending (if needed).
    /// </summary>
    private AnimationClipPlayable mainAnimationPlayable2;

    /// <summary>
    /// The output for the AnimationPlayableOutput.
    /// </summary>
    private AnimationPlayableOutput animationOutput;

    [Header("Character Data")]
    /// <summary>
    /// The ReactiveCharacterSO containing character-specific data.
    /// </summary>
    private ReactiveCharacterSO characterSO;

    /// <summary>
    /// Indicates whether an animation is currently playing.
    /// </summary>
    private bool isPlaying = false;

    /// <summary>
    /// Indicates if the animation loop is on its first cycle.
    /// </summary>
    private bool firstLoop = true;

    /// <summary>
    /// Reference to the coroutine handling animation loops.
    /// </summary>
    private Coroutine loopCoroutine;

    /// <summary>
    /// Initializes the animation controller with the provided character data.
    /// </summary>
    /// <param name="character">The ReactiveCharacterSO containing character-specific data.</param>
    public void Initialize(ReactiveCharacterSO character)
    {
        characterSO = character;
        Animator animator = GetComponent<Animator>();

        if (characterSO.mainAnimations.Count == 0)
        {
            Debug.LogWarning($"Character {characterSO.characterName} has no main animations.");
            return;
        }
    }

    /// <summary>
    /// Starts playing the main animation using PlayableGraph and AnimationMixerPlayable.
    /// </summary>
    public void PlayMainAnimation()
    {
        if (characterSO.mainAnimations.Count == 0)
        {
            Debug.LogWarning($"Character {characterSO.characterName} has no main animations.");
            return;
        }

        if (isPlaying)
        {
            // Animation is already playing; restart if necessary
            StopAnimation();
        }

        // Create the PlayableGraph
        playableGraph = PlayableGraph.Create($"{characterSO.characterName}_PlayableGraph");
        animationOutput = AnimationPlayableOutput.Create(playableGraph, "AnimationOutput", GetComponent<Animator>());

        // Create an AnimationMixerPlayable with one input
        mixerPlayable = AnimationMixerPlayable.Create(playableGraph, 1);
        animationOutput.SetSourcePlayable(mixerPlayable);

        // Create the Playable for the main animation
        mainAnimationPlayable = AnimationClipPlayable.Create(playableGraph, characterSO.mainAnimations[0]);
        mainAnimationPlayable.SetApplyFootIK(true);
        mainAnimationPlayable.SetTime(0); // Start from the beginning

        // Connect the Playable to the mixer
        playableGraph.Connect(mainAnimationPlayable, 0, mixerPlayable, 0);

        // Set input weights
        mixerPlayable.SetInputWeight(0, 1.0f); // Playable1 is fully active

        // Play the graph
        playableGraph.Play();
        isPlaying = true;
    }

    /// <summary>
    /// Coroutine for continuously looping animations with blending.
    /// </summary>
    /// <returns>IEnumerator for the coroutine.</returns>
    private IEnumerator LoopAnimation()
    {
        float clipLength = characterSO.mainAnimations[0].length;
        float blendDuration = 1.0f; // Duration of blending in seconds (adjustable)

        while (isPlaying)
        {
            // Wait until shortly before the animation ends
            if (firstLoop)
            {
                firstLoop = false;
                yield return new WaitForSeconds(clipLength - blendDuration);
            }
            else
            {
                yield return new WaitForSeconds(clipLength - 2 * blendDuration);
            }

            // Start blending to Playable2 if applicable
            if (characterSO.mainAnimations.Count > 1)
            {
                mainAnimationPlayable2.SetTime(0);
                mainAnimationPlayable2.Play();

                float elapsed = 0f;
                while (elapsed < blendDuration)
                {
                    float t = elapsed / blendDuration;
                    mixerPlayable.SetInputWeight(0, 1.0f - t); // Fade out Playable1
                    mixerPlayable.SetInputWeight(1, t);         // Fade in Playable2
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                // Ensure Playable2 is fully weighted
                mixerPlayable.SetInputWeight(0, 0.0f);
                mixerPlayable.SetInputWeight(1, 1.0f);
            }

            // Wait before looping back
            yield return new WaitForSeconds(clipLength - 2 * blendDuration);

            // Reset Playable1 for the next loop
            mainAnimationPlayable.SetTime(0);
            mainAnimationPlayable.Play();
        }
    }

    /// <summary>
    /// Stops the currently playing animation and destroys the PlayableGraph.
    /// </summary>
    public void StopAnimation()
    {
        if (isPlaying)
        {
            isPlaying = false;

            // Stop and destroy the PlayableGraph
            if (playableGraph.IsValid())
            {
                playableGraph.Stop();
                playableGraph.Destroy();
            }

            // Optionally reset the animator to the first state
            ResetPose();
        }
    }

    /// <summary>
    /// Resets the character's pose and position to the initial state.
    /// </summary>
    public void ResetPose()
    {
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.Play(0, 0, 0f); // Play the first state at normalized time 0
            animator.Update(0f);      // Apply the reset immediately
        }

        transform.position = characterSO.characterPosition;
        transform.rotation = Quaternion.Euler(characterSO.characterRotation);
    }

    /// <summary>
    /// Ensures that the PlayableGraph and coroutine are properly cleaned up upon destruction.
    /// </summary>
    private void OnDestroy()
    {
        if (playableGraph.IsValid())
        {
            playableGraph.Destroy();
        }

        if (loopCoroutine != null)
        {
            StopCoroutine(loopCoroutine);
        }
    }
}
