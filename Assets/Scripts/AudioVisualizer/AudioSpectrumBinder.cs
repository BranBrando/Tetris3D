using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Assertions;

[RequireComponent(typeof(AudioSource))]
public class AudioSpectrumBinder : MonoBehaviour
{
    [Tooltip("The Visual Effect component to bind the spectrum data to.")]
    public VisualEffect targetVisualEffect;

    [Tooltip("The name of the Texture2D property exposed in the VFX Graph.")]
    public string spectrumTextureProperty = "_SpectrumTexture";

    [Tooltip("The number of samples to retrieve from the audio spectrum. Must be a power of 2 (e.g., 64, 128, 256, 512).")]
    public int spectrumSize = 128;

    [Tooltip("The FFT window type to use for spectrum analysis.")]
    public FFTWindow fftWindow = FFTWindow.BlackmanHarris;

    [Tooltip("A multiplier to scale the raw amplitude values before sending them to the VFX Graph.")]
    public float amplitudeScale = 10.0f; // Increased default for better visibility initially

    [Header("Color Change Settings")]
    [Tooltip("Minimum time in seconds between color changes.")]
    public float minColorChangeTime = 10.0f;
    [Tooltip("Maximum time in seconds between color changes.")]
    public float maxColorChangeTime = 30.0f;
    [Tooltip("The name of the Color (Vector4) property exposed in the VFX Graph for the target color.")]
    public string targetColorProperty = "_TargetColor";
    [Tooltip("The name of the Float property exposed in the VFX Graph to control overall amplitude scaling.")]
    public string amplitudeScaleGraphProperty = "_AmplitudeScale"; // Added for binding script scale to graph

    // Private variables for spectrum
    private AudioSource audioSource;
    private float[] spectrumData;
    private Texture2D spectrumTexture;
    private Color[] textureColors; // Buffer to hold pixel data

    // Private variables for color change
    private float timeSinceLastColorChange = 0f;
    private float currentInterval = 0f;
    private Color currentTargetColor;

    // Property IDs for faster lookups
    private int spectrumTexturePropertyID;
    private int targetColorPropertyID;
    private int amplitudeScaleGraphPropertyID; // Added

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        Assert.IsNotNull(audioSource, "AudioSource component not found!");
        Assert.IsNotNull(targetVisualEffect, "Target Visual Effect component is not assigned!");
        Assert.IsTrue(Mathf.IsPowerOfTwo(spectrumSize), "Spectrum Size must be a power of two!");

        // Validate if the VFX Graph actually has the property
        if (!targetVisualEffect.HasTexture(spectrumTextureProperty))
        {
            Debug.LogError($"VFX Graph '{targetVisualEffect.visualEffectAsset.name}' does not have an exposed Texture2D property named '{spectrumTextureProperty}'.", targetVisualEffect);
            this.enabled = false; // Disable script if property is missing
            return;
        }
        // Also validate the color property
        if (!targetVisualEffect.HasVector4(targetColorProperty)) // Colors are often Vector4 in VFX Graph
        {
            Debug.LogError($"VFX Graph '{targetVisualEffect.visualEffectAsset.name}' does not have an exposed Vector4 property named '{targetColorProperty}'.", targetVisualEffect);
            this.enabled = false; // Disable script if property is missing
            return;
        }
        // Also validate the amplitude scale property
        if (!targetVisualEffect.HasFloat(amplitudeScaleGraphProperty))
        {
            Debug.LogError($"VFX Graph '{targetVisualEffect.visualEffectAsset.name}' does not have an exposed Float property named '{amplitudeScaleGraphProperty}'.", targetVisualEffect);
            this.enabled = false; // Disable script if property is missing
            return;
        }


        spectrumTexturePropertyID = Shader.PropertyToID(spectrumTextureProperty);
        targetColorPropertyID = Shader.PropertyToID(targetColorProperty);
        amplitudeScaleGraphPropertyID = Shader.PropertyToID(amplitudeScaleGraphProperty); // Added

        // Initialize spectrum data array and texture
        spectrumData = new float[spectrumSize];
        // Use RFloat format for single-channel, high-precision data
        spectrumTexture = new Texture2D(spectrumSize, 1, TextureFormat.RFloat, false);
        spectrumTexture.filterMode = FilterMode.Point; // Use Point filter for sharp data
        spectrumTexture.wrapMode = TextureWrapMode.Clamp;
        textureColors = new Color[spectrumSize]; // Initialize color buffer

        // Initial clear of texture (optional, but good practice)
        for (int i = 0; i < spectrumSize; i++)
        {
            textureColors[i] = new Color(0, 0, 0, 0);
        }
        spectrumTexture.SetPixels(textureColors);
        spectrumTexture.Apply();

        // Set the texture initially on the VFX Graph
        targetVisualEffect.SetTexture(spectrumTexturePropertyID, spectrumTexture);

        // Initialize color change logic
        InitializeColorChange();
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

    void Update()
    {
        if (targetVisualEffect == null || !this.enabled) return;

        // Get the spectrum data from the AudioSource
        // Channel 0 = Left, Channel 1 = Right. Using 0 for simplicity.
        audioSource.GetSpectrumData(spectrumData, 0, fftWindow);

        // Process the spectrum data and write it to the texture buffer
        for (int i = 0; i < spectrumSize; i++)
        {
            // Apply scaling and potentially other processing (e.g., logarithmic scale)
            float processedValue = spectrumData[i] * amplitudeScale;
            // Store the value in the Red channel of the color buffer
            textureColors[i].r = processedValue;
            // Other channels (G, B, A) are unused but need to be set
            textureColors[i].g = 0;
            textureColors[i].b = 0;
            textureColors[i].a = 1;
        }


        // Apply the changes to the spectrum texture
        spectrumTexture.SetPixels(textureColors);
        spectrumTexture.Apply();

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
        // Clean up the created texture when the component is destroyed
        if (spectrumTexture != null)
        {
            Destroy(spectrumTexture);
            spectrumTexture = null;
        }
    }
}
