using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Assertions;

// Removed: [RequireComponent(typeof(AudioSource))]
public class AudioSpectrumBinder : MonoBehaviour
{
    [Tooltip("The Visual Effect component to bind the spectrum data to.")]
    public VisualEffect targetVisualEffect;

    // Removed spectrumTextureProperty as we now send a single bass value

    [Tooltip("The number of samples to retrieve from the audio spectrum. Must be a power of 2 (e.g., 64, 128, 256, 512).")]
    public int spectrumSize = 128;

    [Tooltip("The FFT window type to use for spectrum analysis.")]
    public FFTWindow fftWindow = FFTWindow.BlackmanHarris;

    [Tooltip("A multiplier to scale the calculated bass amplitude before sending it to the VFX Graph.")]
    public float amplitudeScale = 10.0f; // Scales the final 808 value

    [Header("808 Detection (Frequency Range)")]
    [Tooltip("The lower frequency bound for 808 detection (Hz).")]
    public float min808Frequency = 40.0f;
    [Tooltip("The upper frequency bound for 808 detection (Hz).")]
    public float max808Frequency = 80.0f;
    [Tooltip("The threshold for the bass amplitude to be considered significant.")]
    public float threshold = 1.0f;
    [Tooltip("Name of the Float property in VFX Graph to send the 808 level to.")]
    public string bassAmplitudeProperty = "_BassAmplitude"; // Keeping name for simplicity, but it's now 808 focused

    [Header("Color Change Settings")]
    [Tooltip("Minimum time in seconds between color changes.")]
    public float minColorChangeTime = 10.0f;
    [Tooltip("Maximum time in seconds between color changes.")]
    public float maxColorChangeTime = 30.0f;
    [Tooltip("The name of the Color (Vector4) property exposed in the VFX Graph for the target color.")]
    public string targetColorProperty = "_TargetColor";
    [Tooltip("The name of the Float property exposed in the VFX Graph to control overall amplitude scaling.")]
    public string amplitudeScaleGraphProperty = "_AmplitudeScale"; // Added for binding script scale to graph

    // Private variables for spectrum/bass
    private AudioSource audioSource;
    private float[] spectrumData;
    private int min808BinIndex; // Calculated index for min frequency
    private int max808BinIndex; // Calculated index for max frequency
    // Removed texture variables

    // Private variables for color change
    private float timeSinceLastColorChange = 0f;
    private float currentInterval = 0f;
    private Color currentTargetColor;

    // Property IDs for faster lookups
    // Removed spectrumTexturePropertyID
    private int targetColorPropertyID;
    private int amplitudeScaleGraphPropertyID; // For sending script's amplitudeScale value
    private int bassAmplitudePropertyID; // Added for bass value

    void Start()
    {
        // Get AudioSource from AudioManager singleton
        if (AudioManager.Instance != null && AudioManager.Instance.SourceComponent != null)
        {
            audioSource = AudioManager.Instance.SourceComponent;
        }
        else
        {
            Debug.LogError("AudioSpectrumBinder could not find AudioManager instance or its AudioSource component! Disabling binder.", this);
            this.enabled = false; // Disable the script if we can't find the source
            return; // Stop further execution in Start if source is missing
        }

        // Existing assertions (audioSource should now be assigned if we reached here)
        Assert.IsNotNull(audioSource, "AudioSource component could not be retrieved from AudioManager!");
        Assert.IsNotNull(targetVisualEffect, "Target Visual Effect component is not assigned!");
        Assert.IsTrue(Mathf.IsPowerOfTwo(spectrumSize), "Spectrum Size must be a power of two!");
        Assert.IsTrue(min808Frequency >= 0 && max808Frequency > min808Frequency, "808 Frequency range must be valid (Min >= 0, Max > Min).");

        // Validate VFX Graph properties
        bool propertiesValid = true;
        if (!targetVisualEffect.HasVector4(targetColorProperty)) // Colors are often Vector4 in VFX Graph
        {
            Debug.LogError($"VFX Graph '{targetVisualEffect.visualEffectAsset.name}' does not have an exposed Vector4 property named '{targetColorProperty}'.", targetVisualEffect);
            propertiesValid = false;
        }
        if (!targetVisualEffect.HasFloat(amplitudeScaleGraphProperty))
        {
            Debug.LogError($"VFX Graph '{targetVisualEffect.visualEffectAsset.name}' does not have an exposed Float property named '{amplitudeScaleGraphProperty}'.", targetVisualEffect);
            propertiesValid = false;
        }
        // Validate the new bass property
        if (!targetVisualEffect.HasFloat(bassAmplitudeProperty))
        {
            Debug.LogError($"VFX Graph '{targetVisualEffect.visualEffectAsset.name}' does not have an exposed Float property named '{bassAmplitudeProperty}'.", targetVisualEffect);
            propertiesValid = false;
        }

        if (!propertiesValid) {
            this.enabled = false; // Disable script if property is missing
            return;
        }
        // Also validate the amplitude scale property
        if (!targetVisualEffect.HasFloat(amplitudeScaleGraphProperty))
        {
            Debug.LogError($"VFX Graph '{targetVisualEffect.visualEffectAsset.name}' does not have an exposed Float property named '{amplitudeScaleGraphProperty}'.", targetVisualEffect);
            this.enabled = false;
            return;
        }

        // Get property IDs
        targetColorPropertyID = Shader.PropertyToID(targetColorProperty);
        amplitudeScaleGraphPropertyID = Shader.PropertyToID(amplitudeScaleGraphProperty);
        bassAmplitudePropertyID = Shader.PropertyToID(bassAmplitudeProperty);

        // Calculate frequency bin indices
        CalculateFrequencyBinIndices();

        // Initialize spectrum data array (texture stuff removed)
        spectrumData = new float[spectrumSize];

        // Initialize color change logic
        InitializeColorChange();

        // Set initial bass value (optional, defaults to 0)
        targetVisualEffect.SetFloat(bassAmplitudePropertyID, 0f);
    }

    void InitializeColorChange()
    {
        // Generate initial bright/saturated random color
        currentTargetColor = Random.ColorHSV(0f, 1f, 0.8f, 1f, 0.8f, 1f);
        // Set the initial interval
        currentInterval = Random.Range(minColorChangeTime, maxColorChangeTime);
        timeSinceLastColorChange = 0f; // Reset timer just in case Start runs multiple times

        // Send the initial color to the graph
        targetVisualEffect.SetVector4(targetColorPropertyID, currentTargetColor);
    }

    void CalculateFrequencyBinIndices()
    {
        float nyquistFrequency = AudioSettings.outputSampleRate / 2.0f;
        float frequencyResolution = nyquistFrequency / spectrumSize;

        min808BinIndex = Mathf.Clamp(Mathf.FloorToInt(min808Frequency / frequencyResolution), 0, spectrumSize - 1);
        max808BinIndex = Mathf.Clamp(Mathf.CeilToInt(max808Frequency / frequencyResolution), min808BinIndex, spectrumSize - 1); // Ensure max >= min

        if (min808Frequency / frequencyResolution > spectrumSize -1) {
             Debug.LogWarning($"Min 808 Frequency ({min808Frequency} Hz) is too high for the current spectrum size ({spectrumSize}) and sample rate ({AudioSettings.outputSampleRate} Hz). Clamping to max bin.");
        }
         if (max808Frequency / frequencyResolution > spectrumSize -1) {
             Debug.LogWarning($"Max 808 Frequency ({max808Frequency} Hz) is too high for the current spectrum size ({spectrumSize}) and sample rate ({AudioSettings.outputSampleRate} Hz). Clamping to max bin.");
        }
    }


    void Update()
    {
        if (targetVisualEffect == null || !this.enabled) return;

        // Get the spectrum data from the AudioSource
        // Channel 0 = Left, Channel 1 = Right. Using 0 for simplicity.
        audioSource.GetSpectrumData(spectrumData, 0, fftWindow);

        // Calculate average amplitude within the 808 frequency range
        float rangeSum = 0f;
        int numberOfBinsInRange = max808BinIndex - min808BinIndex + 1;

        if (numberOfBinsInRange > 0)
        {
            for (int i = min808BinIndex; i <= max808BinIndex; i++)
            {
                // Basic check to prevent index out of bounds, though Clamp in Start should handle it
                if (i >= 0 && i < spectrumData.Length)
                {
                    rangeSum += spectrumData[i];
                }
            }
            float averageInRange = rangeSum / numberOfBinsInRange;

            // Apply the overall scaling from the Inspector
            float final808Value = averageInRange * amplitudeScale;
            final808Value = final808Value > threshold ? final808Value : 0; 

            // Send the single 808 value to the graph
            targetVisualEffect.SetFloat(bassAmplitudePropertyID, final808Value);
        }
        else
        {
             // Send 0 if the range is invalid or zero width
             targetVisualEffect.SetFloat(bassAmplitudePropertyID, 0f);
        }


        // Update color change logic
        UpdateColorChange();

        // Send script's amplitude scale to the graph property
        targetVisualEffect.SetFloat(amplitudeScaleGraphPropertyID, amplitudeScale);
    }

    void UpdateColorChange()
    {
        timeSinceLastColorChange += Time.deltaTime;
        if (timeSinceLastColorChange >= currentInterval)
        {
            // Generate new target color (ensure it's reasonably bright/saturated)
            currentTargetColor = Random.ColorHSV(0f, 1f, 0.8f, 1f, 0.8f, 1f);

            // Send new color to VFX Graph
            targetVisualEffect.SetVector4(targetColorPropertyID, currentTargetColor);

            // Reset timer and pick next interval
            timeSinceLastColorChange = 0f;
            currentInterval = Random.Range(minColorChangeTime, maxColorChangeTime);
        }
    }


    void OnDestroy()
    {
        // No texture to clean up anymore
    }
}
