using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.Collections;

/// <summary>
/// Handles the replaying of animations on a designated Replay Avatar using Unity's Playable API.
/// </summary>
public class AnimationReplayer : MonoBehaviour
{
    private PlayableGraph playableGraph;
    private GameObject replayAvatar;

    /// <summary>
    /// Plays an animation on a Replay Avatar.
    /// </summary>
    /// <param name="clip">The animation to play.</param>
    /// <param name="characterPrefab">The character prefab.</param>
    /// <param name="parent">The parent transform for the Replay Avatar.</param>
    public void ReplayAnimation(AnimationClip clip, GameObject characterPrefab, Transform parent)
    {
        if (clip == null)
        {
            Debug.LogError("AnimationClip is null.");
            return;
        }

        if (characterPrefab == null)
        {
            Debug.LogError("CharacterPrefab is null.");
            return;
        }

        if (parent == null)
        {
            Debug.LogError("Parent Transform is null.");
            return;
        }

        // Destroy existing Replay Avatar
        if (replayAvatar != null)
        {
            Destroy(replayAvatar);
        }

        // Destroy existing PlayableGraph
        if (playableGraph.IsValid())
        {
            playableGraph.Destroy();
        }

        // Instantiate the Replay Avatar
        replayAvatar = Instantiate(characterPrefab, parent);
        replayAvatar.transform.localPosition = Vector3.zero;
        replayAvatar.transform.localRotation = Quaternion.identity;

        // Add Animator if not present
        Animator animator = replayAvatar.GetComponent<Animator>();
        if (animator == null)
        {
            animator = replayAvatar.AddComponent<Animator>();
        }

        // Set up PlayableGraph
        playableGraph = PlayableGraph.Create("AnimationGraph");
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(playableGraph, "Animation", animator);
        AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(playableGraph, clip);
        clipPlayable.SetApplyFootIK(true);
        output.SetSourcePlayable(clipPlayable);
        playableGraph.Play();

        // Destroy after the duration of the animation
        StartCoroutine(DestroyReplayAvatarAfterDuration(clip.length));
    }

    /// <summary>
    /// Stops the currently running replay.
    /// </summary>
    public void StopReplay()
    {
        // Destroy the Replay Avatar
        if (replayAvatar != null)
        {
            Destroy(replayAvatar);
            replayAvatar = null;
        }

        // Destroy the PlayableGraph
        if (playableGraph.IsValid())
        {
            playableGraph.Destroy();
        }

        // Stop all Coroutines that could destroy the avatar
        StopAllCoroutines();
    }

    /// <summary>
    /// Destroys the Replay Avatar after a specific duration.
    /// </summary>
    /// <param name="duration">Duration in seconds.</param>
    private IEnumerator DestroyReplayAvatarAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        StopReplay();
    }

    /// <summary>
    /// Ensures that all resources are cleaned up when the script is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        StopReplay();
    }
}
