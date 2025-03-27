using UnityEngine;

/// <summary>
/// Detects microphone input loudness and calculates its average volume over a sample window.
/// </summary>
public class MicrophoneLoudnessDetector : MonoBehaviour
{
    /// <summary>
    /// Static property to store the detected microphone loudness value.
    /// </summary>
    public static float MicrophoneLoudness { get; private set; } = 0f;

    /// <summary>
    /// The name of the microphone to be used. Leave empty for the default microphone.
    /// </summary>
    public string microphoneName = "";

    /// <summary>
    /// The number of samples to consider for measuring loudness.
    /// </summary>
    public int sampleWindow = 64;

    /// <summary>
    /// Sensitivity factor for scaling the loudness value.
    /// </summary>
    public float sensitivity = 100f;

    /// <summary>
    /// Threshold to filter out quiet noises below a certain level.
    /// </summary>
    public float threshold = 0.1f;

    /// <summary>
    /// The AudioClip used to capture microphone input.
    /// </summary>
    private AudioClip microphoneClip;

    /// <summary>
    /// Starts the microphone recording when the script initializes.
    /// </summary>
    void Start()
    {
        StartMicrophone();
    }

    /// <summary>
    /// Continuously measures the microphone loudness in every frame.
    /// </summary>
    void Update()
    {
        MeasureMicrophoneLoudness();
    }

    /// <summary>
    /// Initializes the microphone and starts capturing audio input.
    /// </summary>
    void StartMicrophone()
    {
        // Check if a microphone is available
        if (Microphone.devices.Length > 0)
        {
            // Use the default microphone if none is specified
            if (string.IsNullOrEmpty(microphoneName))
            {
                microphoneName = Microphone.devices[0]; // Change index for HMD input to 1 (index 0 is the default pc microphone)
            }

            // Start recording with the specified microphone
            microphoneClip = Microphone.Start(microphoneName, true, 1, AudioSettings.outputSampleRate);
            Debug.Log($"Microphone started: {microphoneName}");
        }
        else
        {
            Debug.LogError("No microphone found!");
        }
    }

    /// <summary>
    /// Measures the loudness of the current microphone input.
    /// </summary>
    void MeasureMicrophoneLoudness()
    {
        // Exit if no microphone clip is available
        if (microphoneClip == null) return;

        // Get the current position of the recording
        int position = Microphone.GetPosition(microphoneName) - sampleWindow + 1;
        if (position < 0)
        {
            MicrophoneLoudness = 0f;
            return;
        }

        // Retrieve audio data within the sample window
        float[] waveData = new float[sampleWindow];
        microphoneClip.GetData(waveData, position);

        // Calculate total loudness by summing the absolute values of the samples
        float totalLoudness = 0f;
        for (int i = 0; i < waveData.Length; i++)
        {
            totalLoudness += Mathf.Abs(waveData[i]);
        }

        // Compute the average loudness and apply sensitivity and threshold
        float averageLoudness = totalLoudness / sampleWindow;
        MicrophoneLoudness = (averageLoudness * sensitivity >= threshold) ? averageLoudness * sensitivity : 0f;

        // Debug.Log($"Microphone Loudness: {MicrophoneLoudness}");
    }

    /// <summary>
    /// Stops the microphone recording when the script is destroyed.
    /// </summary>
    void OnDestroy()
    {
        if (Microphone.IsRecording(microphoneName))
        {
            Microphone.End(microphoneName);
        }
    }
}
