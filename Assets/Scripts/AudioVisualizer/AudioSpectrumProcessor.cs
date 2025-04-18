using UnityEngine;

public class AudioSpectrumProcessor
{
    private static AudioSpectrumProcessor instance;

    public static AudioSpectrumProcessor Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new AudioSpectrumProcessor();
            }
            return instance;
        }
    }

    private AudioSpectrumProcessor() { } // Private constructor to enforce singleton pattern

    private AudioSource audioSource;
    private int spectrumSize;
    private FFTWindow fftWindow;
    private float minFrequency;
    private float maxFrequency;
    private float threshold;

    private float[] spectrumData;
    private int minBinIndex;
    private int maxBinIndex;

    public bool IsInitialized { get; private set; } = false;

    public void Initialize(AudioSource source, int size, FFTWindow window, float minFreq, float maxFreq, float thresh)
    {
        if (source == null)
        {
            Debug.LogError("AudioSpectrumProcessor cannot initialize with a null AudioSource.");
            return;
        }
        if (!Mathf.IsPowerOfTwo(size))
        {
             Debug.LogError($"AudioSpectrumProcessor: Spectrum Size ({size}) must be a power of two!");
             return;
        }
         if (!(minFreq >= 0 && maxFreq > minFreq))
        {
             Debug.LogError($"AudioSpectrumProcessor: Frequency range must be valid (Min: {minFreq}, Max: {maxFreq}).");
             return;
        }


        this.audioSource = source;
        this.spectrumSize = size;
        this.fftWindow = window;
        this.minFrequency = minFreq;
        this.maxFrequency = maxFreq;
        this.threshold = thresh; // Store threshold

        this.spectrumData = new float[spectrumSize];

        CalculateFrequencyBinIndices();
        IsInitialized = true;
         Debug.Log($"AudioSpectrumProcessor Initialized. Freq Range: {minFrequency}-{maxFrequency} Hz -> Bins: {minBinIndex}-{maxBinIndex}");
    }

    private void CalculateFrequencyBinIndices()
    {
        if (spectrumSize <= 0) return; // Prevent division by zero

        float nyquistFrequency = AudioSettings.outputSampleRate / 2.0f;
        if (nyquistFrequency <= 0) {
             Debug.LogError("AudioSpectrumProcessor: Nyquist frequency is zero or negative. Cannot calculate bins.");
             IsInitialized = false;
             return;
        }

        float frequencyResolution = nyquistFrequency / spectrumSize;
        if (frequencyResolution <= 0) {
             Debug.LogError("AudioSpectrumProcessor: Frequency resolution is zero or negative. Cannot calculate bins.");
             IsInitialized = false;
             return;
        }


        minBinIndex = Mathf.Clamp(Mathf.FloorToInt(minFrequency / frequencyResolution), 0, spectrumSize - 1);
        maxBinIndex = Mathf.Clamp(Mathf.CeilToInt(maxFrequency / frequencyResolution), minBinIndex, spectrumSize - 1);

        // Add warnings similar to the original script if frequencies are too high
        if (minFrequency / frequencyResolution > spectrumSize -1) {
             Debug.LogWarning($"AudioSpectrumProcessor: Min Frequency ({minFrequency} Hz) might be too high for the current spectrum size ({spectrumSize}) and sample rate ({AudioSettings.outputSampleRate} Hz). Clamping to max bin.");
        }
         if (maxFrequency / frequencyResolution > spectrumSize -1) {
             Debug.LogWarning($"AudioSpectrumProcessor: Max Frequency ({maxFrequency} Hz) might be too high for the current spectrum size ({spectrumSize}) and sample rate ({AudioSettings.outputSampleRate} Hz). Clamping to max bin.");
        }
    }

    public void UpdateSpectrum()
    {
        if (!IsInitialized || audioSource == null || !audioSource.isPlaying)
        {
            // Clear spectrum data if not playing or not initialized
            System.Array.Clear(spectrumData, 0, spectrumData.Length);
            return;
        }
        // Channel 0 = Left, Channel 1 = Right. Using 0 for simplicity.
        audioSource.GetSpectrumData(spectrumData, 0, fftWindow);
    }

    public float GetAverageAmplitudeInRange(float scale)
    {
        if (!IsInitialized) return 0f;

        float rangeSum = 0f;
        int numberOfBinsInRange = maxBinIndex - minBinIndex + 1;

        if (numberOfBinsInRange > 0)
        {
            for (int i = minBinIndex; i <= maxBinIndex; i++)
            {
                // Basic check to prevent index out of bounds
                if (i >= 0 && i < spectrumData.Length)
                {
                    rangeSum += spectrumData[i];
                }
            }
            float averageInRange = rangeSum / numberOfBinsInRange;

            // Apply the overall scaling passed from the binder
            float finalValue = averageInRange * scale;

            // Apply threshold
            finalValue = finalValue > threshold ? finalValue : 0f;
            return finalValue;
        }
        else
        {
            return 0f; // Return 0 if the range is invalid or zero width
        }
    }

    /// <summary>
    /// Calculates the average amplitude for a specific frequency band within the spectrum data.
    /// </summary>
    /// <param name="bandIndex">The index of the band (0-based).</param>
    /// <param name="totalBands">The total number of bands to divide the spectrum into.</param>
    /// <param name="scale">A multiplier to apply to the calculated amplitude.</param>
    /// <returns>The scaled average amplitude for the specified band, or 0 if inputs are invalid.</returns>
    public float GetAmplitudeForBand(int bandIndex, int totalBands, float scale)
    {
        if (!IsInitialized || totalBands <= 0 || bandIndex < 0 || bandIndex >= totalBands)
        {
            // Debug.LogWarning($"GetAmplitudeForBand: Invalid input. Initialized: {IsInitialized}, TotalBands: {totalBands}, BandIndex: {bandIndex}");
            return 0f;
        }

        // Determine the range of spectrum data indices for this band
        int bandWidth = spectrumData.Length / totalBands;
        int startBin = bandIndex * bandWidth;
        // Ensure the last band covers remaining bins if spectrumSize is not perfectly divisible
        int endBin = (bandIndex == totalBands - 1) ? spectrumData.Length - 1 : startBin + bandWidth - 1;

        // Clamp indices just in case
        startBin = Mathf.Clamp(startBin, 0, spectrumData.Length - 1);
        endBin = Mathf.Clamp(endBin, startBin, spectrumData.Length - 1);

        float bandSum = 0f;
        int binsInThisBand = endBin - startBin + 1;

        if (binsInThisBand > 0)
        {
            for (int i = startBin; i <= endBin; i++)
            {
                 // Basic check to prevent index out of bounds (should be covered by clamp)
                if (i >= 0 && i < spectrumData.Length)
                {
                    bandSum += spectrumData[i];
                }
            }
            float averageInBand = bandSum / binsInThisBand;

            // Apply the overall scaling passed from the caller
            float finalValue = averageInBand * scale;

            // Apply threshold (using the processor's threshold)
            finalValue = finalValue > threshold ? finalValue : 0f;
            return finalValue;
        }
        else
        {
            // Debug.LogWarning($"GetAmplitudeForBand: Zero bins calculated for band {bandIndex}. Start: {startBin}, End: {endBin}");
            return 0f; // Return 0 if the band width is zero
        }
    }
}
