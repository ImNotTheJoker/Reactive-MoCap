using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Audio Interrupter", menuName = "Interrupters/Audio Interrupter")]
public class AudioInterrupter : Interrupter
{
    [Header("Audio Settings")]
    public float VolumeThreshold = 0.5f; // Schwellenwert für die Lautstärke
    public int minFreqIndex = 0;          // Mindestfrequenzindex für die Analyse
    public int maxFreqIndex = 128;        // Höchstfrequenzindex für die Analyse

    // Timer für jede Charakterinstanz
    private Dictionary<GameObject, float> characterTimers = new Dictionary<GameObject, float>();

    public override void CheckCondition(GameObject character)
    {
        if (character == null)
        {
            Debug.LogWarning("AudioInterrupter: Charakter ist null.");
            return;
        }

        CharacterEventReceiver receiver = character.GetComponent<CharacterEventReceiver>();
        if (receiver == null)
        {
            Debug.LogWarning("AudioInterrupter: CharacterEventReceiver ist null.");
            return;
        }

        foreach (var obj in RelevantGameObjects)
        {
            if (obj == null)
            {
                Debug.LogWarning("AudioInterrupter: Ein relevantes GameObject ist null.");
                continue;
            }

            // Berechne die Distanz zum Charakter
            float distance = Vector3.Distance(character.transform.position, obj.transform.position);

            if (distance > MaxDistance)
            {
                // Objekt ist zu weit entfernt, überspringen
                continue;
            }

            float currentVolume = 0f;

            // Prüfen, ob das GameObject der Benutzer ist (CenterEyeAnchor)
            if (obj.name == "CenterEyeAnchor")
            {
                currentVolume = MicrophoneLoudnessDetector.MicrophoneLoudness;
            }
            else
            {
                AudioSource audioSource = obj.GetComponent<AudioSource>();
                if (audioSource != null && audioSource.isPlaying)
                {
                    currentVolume = GetCurrentVolume(audioSource);
                }
                else
                {
                    continue;
                }
            }

            // Debugging
            // Debug.Log($"AudioInterrupter: Aktuelle Lautstärke von {obj.name}: {currentVolume}, Distanz: {distance}");

            if (currentVolume >= VolumeThreshold)
            {
                if (!characterTimers.ContainsKey(character))
                {
                    characterTimers[character] = 0f;
                }

                characterTimers[character] += Time.deltaTime;

                // Bedingung für Mindestdauer
                if (characterTimers[character] >= MinDuration && !receiver.HasReacted(this))
                {
                    AnimationClip reactiveAnimation = (ReactiveAnimations != null && ReactiveAnimations.Count > 0) ? ReactiveAnimations[0] : null;

                    InterruptionConfig config = new InterruptionConfig(
                        InterrupterType.Audio,
                        obj.transform.position,
                        MinDuration,
                        Priority,
                        currentVolume,
                        obj,
                        distance,
                        reactiveAnimation // Kann null sein
                    );
                    NotifyCharacter(character, config, this);
                    receiver.SetHasReacted(this, true);
                }
            }
            else
            {
                if (characterTimers.ContainsKey(character))
                {
                    characterTimers[character] = 0f;
                }
                receiver.SetHasReacted(this, false);
                receiver.ResetInterrupter(this); // Entferne den Interrupter von der Warteliste
            }
        }
    }

    private float GetCurrentVolume(AudioSource audioSource)
    {
        float[] spectrum = new float[256];
        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.Rectangular);

        float sum = 0f;
        for (int i = minFreqIndex; i <= maxFreqIndex; i++)
        {
            sum += spectrum[i];
        }

        return sum;
    }
}
