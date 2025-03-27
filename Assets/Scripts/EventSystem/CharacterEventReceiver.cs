using System.Collections.Generic;
using System.Collections;
using UnityEngine;

/// <summary>
/// Handles interruptions for a character, managing reactive animations and look-only reactions.
/// Ensures proper handling of priorities and overlays.
/// </summary>
public class CharacterEventReceiver : MonoBehaviour
{
    [Header("Interrupters Configuration")]
    /// <summary>
    /// List of base interrupters assigned to the character.
    /// </summary>
    public List<Interrupter> interrupters;

    /// <summary>
    /// List of cloned interrupters specific to each relevant GameObject.
    /// </summary>
    private List<Interrupter> characterInterrupters = new List<Interrupter>();

    [Header("Components References")]
    /// <summary>
    /// Reference to the extended animation controller handling reactive animations.
    /// </summary>
    private CharacterAnimationControllerExtended animController;

    /// <summary>
    /// Reference to the LookAtController handling look-at behaviors.
    /// </summary>
    private LookAtController lookAtController;

    [Header("Interruption Management")]
    /// <summary>
    /// Tracks the reaction state of each interrupter.
    /// </summary>
    private Dictionary<Interrupter, bool> interrupterStates = new Dictionary<Interrupter, bool>();

    /// <summary>
    /// Currently active interrupter.
    /// </summary>
    private Interrupter activeInterrupter;

    /// <summary>
    /// List of interrupters waiting to be handled based on priority.
    /// </summary>
    private List<Interrupter> waitingInterrupters = new List<Interrupter>();

    /// <summary>
    /// Set of interrupters that have been suppressed by higher priority interrupters.
    /// </summary>
    private HashSet<Interrupter> suppressedInterrupts = new HashSet<Interrupter>();

    /// <summary>
    /// Target GameObject for rotation (used in LookAt behavior).
    /// </summary>
    private GameObject currentTargetObject;

    /// <summary>
    /// Indicates if the first look-at has occurred to handle transition types.
    /// </summary>
    private bool hasFirstLookOccurred = false;

    [Header("Coroutine References")]
    /// <summary>
    /// Reference to the coroutine handling reactive animations.
    /// </summary>
    private Coroutine reactiveCoroutine;

    /// <summary>
    /// Reference to the coroutine handling look-only reactions.
    /// </summary>
    private Coroutine lookOnlyCoroutine;

    /// <summary>
    /// Initializes interrupters by cloning and setting them up for each relevant GameObject.
    /// </summary>
    void Start()
    {
        // Get required components
        lookAtController = GetComponent<LookAtController>();
        animController = GetComponent<CharacterAnimationControllerExtended>();

        if (animController == null)
        {
            Debug.LogError("CharacterAnimationControllerExtended not found!");
        }

        // Clone and initialize interrupters for each relevant GameObject
        foreach (var interrupter in interrupters)
        {
            foreach (var objName in interrupter.relevantGameObjectNames)
            {
                // Skip if the interrupter references the character itself
                if (objName == this.gameObject.name)
                {
                    continue;
                }

                // Clone the interrupter for the specific GameObject
                Interrupter clonedInterrupter = interrupter.Clone();

                // Clear existing relevant GameObject names and set the specific one
                clonedInterrupter.relevantGameObjectNames.Clear();
                clonedInterrupter.relevantGameObjectNames.Add(objName);

                // Initialize the cloned interrupter with this receiver
                clonedInterrupter.Initialize(this);

                // Add to the list of character-specific interrupters
                characterInterrupters.Add(clonedInterrupter);
            }
        }
    }

    /// <summary>
    /// Handles an incoming interruption based on the provided configuration and interrupter.
    /// </summary>
    /// <param name="config">Configuration of the interruption.</param>
    /// <param name="interrupter">The interrupter causing the interruption.</param>
    public void HandleInterruption(InterruptionConfig config, Interrupter interrupter)
    {
        if (config == null) return;

        Debug.Log($"Interruption triggered: {config.Type} for {gameObject.name} by {interrupter.interrupterName} with priority {config.Priority}");

        // Ignore if the interrupter is currently suppressed
        if (suppressedInterrupts.Contains(interrupter))
        {
            Debug.Log($"{interrupter.interrupterName} is suppressed and won't trigger again.");
            return;
        }

        bool isReactive = config.ReactiveAnimation != null;

        // Determine if the new interrupter has higher priority than the current active one
        if (activeInterrupter == null || config.Priority > activeInterrupter.Priority)
        {
            Debug.Log("New interrupter has higher priority.");

            // Suppress the currently active interrupter if any
            if (activeInterrupter != null)
            {
                Debug.Log("Suppressing active interrupter.");
                suppressedInterrupts.Add(activeInterrupter);
                activeInterrupter.Overlay();

                // Stop the active coroutine based on interrupter type
                if (activeInterrupter.HasReactiveAnimation())
                {
                    if (reactiveCoroutine != null)
                    {
                        StopCoroutine(reactiveCoroutine);
                        reactiveCoroutine = null;
                    }

                    // Optionally resume the main animation if needed
                    // animController.ReturnToMainAnimation(0.5f);
                    // animController.PauseMainAnimation();
                }
                else
                {
                    Debug.Log("Active interrupter was look-only.");
                    if (lookOnlyCoroutine != null)
                    {
                        StopCoroutine(lookOnlyCoroutine);
                        Debug.Log("Look-only coroutine stopped.");
                        lookOnlyCoroutine = null;

                        // Reset LookAt behavior if the suppressed interrupter was look-only
                        if (lookAtController != null)
                        {
                            lookAtController.ResetLookAtTarget(LookAtTransitionType.SwitchTarget);
                        }
                    }
                }
            }

            // Trigger the new interrupter's reaction
            if (isReactive)
            {
                animController.PauseMainAnimation();
                PlayReactiveAnimation(config.ReactiveAnimation, interrupter);
            }
            else
            {
                animController.ReturnToMainAnimation(0.5f);
                PlayLookOnlyReaction(interrupter);
            }
        }
        else
        {
            // If the new interrupter has lower or equal priority, add it to the waiting list
            if (!waitingInterrupters.Contains(interrupter) && interrupter != activeInterrupter)
            {
                waitingInterrupters.Add(interrupter);
                Debug.Log($"Added {interrupter.interrupterName} to waiting list.");
            }
        }
    }

    /// <summary>
    /// Plays a reactive animation in response to an interrupter.
    /// </summary>
    /// <param name="reactiveAnimation">The reactive AnimationClip to play.</param>
    /// <param name="interrupter">The interrupter causing the reactive animation.</param>
    private void PlayReactiveAnimation(AnimationClip reactiveAnimation, Interrupter interrupter)
    {
        if (reactiveAnimation == null)
        {
            Debug.LogWarning("ReactiveAnimation is null, cannot play.");
            return;
        }

        // Set the interrupter as active
        activeInterrupter = interrupter;
        interrupter.SetPlaying(true);
        interrupter.IsOverlaid = false;

        // Play the reactive animation
        animController.PlayReactiveAnimation(reactiveAnimation, 0.5f);

        // Set the LookAt target
        currentTargetObject = GameObject.Find(interrupter.relevantGameObjectNames[0]);

        if (currentTargetObject != null)
        {
            Debug.Log("CharacterEventReceiver: Target object set to " + currentTargetObject.name);
        }
        else
        {
            Debug.LogWarning("CharacterEventReceiver: Target object not found!");
        }

        Transform targetTransform = currentTargetObject != null ? currentTargetObject.transform : null;
        if (lookAtController != null && targetTransform != null)
        {
            if (!hasFirstLookOccurred)
            {
                lookAtController.SetLookAtTarget(targetTransform, LookAtTransitionType.FirstLook);
                hasFirstLookOccurred = true;
            }
            else
            {
                lookAtController.SetLookAtTarget(targetTransform, LookAtTransitionType.SwitchTarget);
            }
        }

        // Start a coroutine to wait for the reactive animation to finish
        reactiveCoroutine = StartCoroutine(WaitForAnimationToFinish(reactiveAnimation.length, () =>
        {
            // If the interrupter wasn't overlaid, reset and check for next interrupter
            if (!interrupter.IsOverlaid)
            {
                interrupter.SetPlaying(false);
                activeInterrupter = null;
                // Optionally resume main animation
                // animController.ResumeMainAnimation();
                CheckForNextInterruption();
            }
            else
            {
                // If overlaid, do not transition to main animation
                Debug.Log("Interrupter was overlaid, no transition to main.");
            }
        }));
    }

    /// <summary>
    /// Plays a look-only reaction in response to an interrupter.
    /// </summary>
    /// <param name="interrupter">The interrupter causing the look-only reaction.</param>
    private void PlayLookOnlyReaction(Interrupter interrupter)
    {
        // Set the interrupter as active
        activeInterrupter = interrupter;

        // Set the LookAt target
        GameObject targetObject = GameObject.Find(interrupter.relevantGameObjectNames[0]);
        currentTargetObject = targetObject;

        if (currentTargetObject != null)
        {
            Debug.Log("CharacterEventReceiver: Target object set to " + currentTargetObject.name);
        }
        else
        {
            Debug.LogWarning("CharacterEventReceiver: Target object not found!");
        }

        Transform targetTransform = currentTargetObject != null ? currentTargetObject.transform : null;
        if (lookAtController != null && targetTransform != null)
        {
            lookAtController.SetLookAtTarget(targetTransform, LookAtTransitionType.FirstLook);
        }

        // Resume and return to main animation
        animController.ResumeMainAnimation();
        animController.ReturnToMainAnimation(0.5f);

        // Start a coroutine to handle the duration of the look-only reaction
        lookOnlyCoroutine = StartCoroutine(HandleLookOnlyReaction(interrupter, 5f)); // Duration set to 5 seconds
    }

    /// <summary>
    /// Coroutine to handle the duration of a look-only reaction.
    /// </summary>
    /// <param name="interrupter">The interrupter causing the look-only reaction.</param>
    /// <param name="duration">Duration of the look-only reaction.</param>
    /// <returns></returns>
    private IEnumerator HandleLookOnlyReaction(Interrupter interrupter, float duration)
    {
        yield return new WaitForSeconds(duration);

        Debug.Log("Look-only reaction finished.");

        // Reset the LookAt target to default
        if (lookAtController != null)
        {
            lookAtController.ResetLookAtTarget(LookAtTransitionType.ReturnToDefault);
        }

        // Reset the active interrupter
        activeInterrupter = null;

        // Clear the coroutine reference
        lookOnlyCoroutine = null;

        // Resume main animation if no other interruptions are active
        animController.ResumeMainAnimation();

        // Check for the next interrupter in the waiting list
        CheckForNextInterruption();
    }

    /// <summary>
    /// Coroutine to wait for the duration of a reactive animation before proceeding.
    /// </summary>
    /// <param name="duration">Duration of the reactive animation.</param>
    /// <param name="onComplete">Action to invoke after waiting.</param>
    /// <returns></returns>
    private IEnumerator WaitForAnimationToFinish(float duration, System.Action onComplete)
    {
        yield return new WaitForSeconds(duration);
        onComplete?.Invoke();
    }

    /// <summary>
    /// Checks and handles the next interrupter in the waiting list based on priority.
    /// </summary>
    public void CheckForNextInterruption()
    {
        Interrupter nextInterrupter = null;
        int highestPriority = -1;

        // Find the interrupter with the highest priority in the waiting list
        foreach (var interrupter in waitingInterrupters)
        {
            if (interrupter.Priority > highestPriority && !suppressedInterrupts.Contains(interrupter))
            {
                nextInterrupter = interrupter;
                highestPriority = interrupter.Priority;
            }
        }

        // If a new interrupter is found
        if (nextInterrupter != null)
        {
            // Remove it from the waiting list
            waitingInterrupters.Remove(nextInterrupter);

            // Trigger its reaction based on whether it's reactive or look-only
            if (nextInterrupter.ReactiveAnimations.Count > 0)
            {
                PlayReactiveAnimation(nextInterrupter.ReactiveAnimations[0], nextInterrupter);
            }
            else
            {
                // For look-only interrupters, handle accordingly
                animController.ReturnToMainAnimation(0.5f);
                animController.ResumeMainAnimation();
                PlayLookOnlyReaction(nextInterrupter);
            }
        }
        else
        {
            Debug.Log("No valid interrupters left.");

            // Determine if the active interrupter was reactive and not overlaid
            bool wasReactive = activeInterrupter != null && activeInterrupter.HasReactiveAnimation();

            // Ensure main animation resumes if no active interrupter or active was reactive and not overlaid
            if (activeInterrupter == null || (!activeInterrupter.IsOverlaid && wasReactive))
            {
                Debug.Log("Returning to main animation from last interrupter.");
                animController.ReturnToMainAnimation(0.5f);
                animController.ResumeMainAnimation();

                // Reset the LookAt target to default
                if (lookAtController != null)
                {
                    lookAtController.ResetLookAtTarget(LookAtTransitionType.ReturnToDefault);
                }
                hasFirstLookOccurred = false;
            }
            else
            {
                Debug.Log("Overlaid interrupter or active interrupter was look-only, no return to main.");
            }
        }
    }

    /// <summary>
    /// Resets the suppression status of an interrupter.
    /// </summary>
    /// <param name="interrupter">The interrupter to reset.</param>
    public void ResetInterrupter(Interrupter interrupter)
    {
        if (suppressedInterrupts.Contains(interrupter))
        {
            suppressedInterrupts.Remove(interrupter);
        }
    }

    /// <summary>
    /// Checks if an interrupter has already reacted.
    /// </summary>
    /// <param name="interrupter">The interrupter to check.</param>
    /// <returns>True if reacted, otherwise false.</returns>
    public bool HasReacted(Interrupter interrupter)
    {
        if (interrupterStates.ContainsKey(interrupter))
        {
            return interrupterStates[interrupter];
        }
        return false;
    }

    /// <summary>
    /// Sets the reacted status of an interrupter.
    /// </summary>
    /// <param name="interrupter">The interrupter to set.</param>
    /// <param name="hasReacted">Whether it has reacted.</param>
    public void SetHasReacted(Interrupter interrupter, bool hasReacted)
    {
        interrupterStates[interrupter] = hasReacted;
    }

    /// <summary>
    /// Rotates the character towards the interrupter's target.
    /// </summary>
    /// <param name="target">The target GameObject to look at.</param>
    private void RotateTowardsInterrupterTarget(GameObject target)
    {
        Vector3 direction = target.transform.position - transform.position;
        direction.y = 0; // Optional: Restrict rotation to the horizontal plane
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f); // Smooth rotation
    }

    /// <summary>
    /// Sets the active interrupter.
    /// </summary>
    /// <param name="interrupter">The interrupter to set as active.</param>
    public void SetActiveInterrupter(Interrupter interrupter)
    {
        this.activeInterrupter = interrupter;
    }

    /// <summary>
    /// Updates and checks conditions for all character-specific interrupters every frame.
    /// </summary>
    void Update()
    {
        // Iterate through all character-specific interrupters and check their conditions
        foreach (var interrupter in characterInterrupters)
        {
            interrupter.CheckCondition(gameObject);
        }

        Debug.Log("activeInterrupter Status: " + activeInterrupter);
    }
}
