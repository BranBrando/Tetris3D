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
    private AudioSpectrumProcessor spectrumProcessor; // New processor instance
    // Removed spectrumData, min/max bin indices

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
        // Get Bass AudioSource from AudioManager using the new method
        if (AudioManager.Instance != null)
        {
            // Assuming the bass track is defined with the name "Bass" in AudioManager's sounds array
            audioSource = AudioManager.Instance.GetAudioSourceForSound("Drums");

            if (audioSource == null)
            {
                 Debug.LogError("AudioSpectrumBinder could not find an AudioSource named 'Drums' in AudioManager! Disabling binder.", this);
                 this.enabled = false;
                 return;
            }
        }
        else
        {
            Debug.LogError("AudioSpectrumBinder could not find AudioManager instance! Disabling binder.", this);
            this.enabled = false; // Disable the script if we can't find the AudioManager
            return; // Stop further execution in Start if AudioManager is missing
        }

        // Existing assertions (audioSource should now be assigned if we reached here)
        Assert.IsNotNull(audioSource, "Drums AudioSource component could not be retrieved from AudioManager!"); // Updated assertion message
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

        // --- Initialize the Spectrum Processor ---
        spectrumProcessor = AudioSpectrumProcessor.Instance;
        spectrumProcessor.Initialize(audioSource, spectrumSize, fftWindow, min808Frequency, max808Frequency, threshold);

        // Check if processor initialized correctly
        if (!spectrumProcessor.IsInitialized)
        {
            Debug.LogError("AudioSpectrumProcessor failed to initialize. Disabling AudioSpectrumBinder.", this);
            this.enabled = false;
            return;
        }
        // -----------------------------------------

        // Initialize color change logic
        InitializeColorChange();

        // Set initial bass value (optional, defaults to 0)
        // Set initial bass value (optional, defaults to 0 via processor)
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

    // Removed CalculateFrequencyBinIndices method


    void Update()
    {
        // Added check for spectrumProcessor initialization
        if (targetVisualEffect == null || !this.enabled || spectrumProcessor == null || !spectrumProcessor.IsInitialized) return;

        // --- Use Spectrum Processor ---
        spectrumProcessor.UpdateSpectrum();
        // Using channel 0 (left) for the average amplitude calculation
        float final808Value = spectrumProcessor.GetAverageAmplitudeInRange(0, amplitudeScale);
        targetVisualEffect.SetFloat(bassAmplitudePropertyID, final808Value);
        // -----------------------------

        // Update color change logic (remains the same)
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

    // Removed empty OnDestroy method
}
