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

    private float[] spectrumDataLeft;
    private float[] spectrumDataRight;
    private int minBinIndex;
    private int maxBinIndex;

    private float[] calculatedFreqBandsLeft = new float[8];
    private float[] calculatedFreqBandsRight = new float[8];

    private float[] calculatedFreqBands64Left = new float[64];
    private float[] calculatedFreqBands64Right = new float[64];

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

        this.spectrumDataLeft = new float[spectrumSize];
        this.spectrumDataRight = new float[spectrumSize];

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
            System.Array.Clear(spectrumDataLeft, 0, spectrumDataLeft.Length);
            System.Array.Clear(spectrumDataRight, 0, spectrumDataRight.Length);
            return;
        }
        // Channel 0 = Left, Channel 1 = Right
        audioSource.GetSpectrumData(spectrumDataLeft, 0, fftWindow);
        audioSource.GetSpectrumData(spectrumDataRight, 1, fftWindow);
        CalculateFrequencyBandsFromSpectrum();
    }

    private void CalculateFrequencyBandsFromSpectrum()
    {
        CalculateFrequencyBandsForChannel(spectrumDataLeft, calculatedFreqBandsLeft);
        CalculateFrequencyBandsForChannel(spectrumDataRight, calculatedFreqBandsRight);
        CalculateFrequencyBands64ForChannel(spectrumDataLeft, calculatedFreqBands64Left);
        CalculateFrequencyBands64ForChannel(spectrumDataRight, calculatedFreqBands64Right);
    }

    private void CalculateFrequencyBandsForChannel(float[] spectrumData, float[] calculatedFreqBands)
    {
        int count = 0;

        // spectrum size is 512, so we can calculate 8 bands
        for (int i = 0; i < 8; i++)
        {
            float average = 0;
            int sampleCount = (int)Mathf.Pow(2, i) * 2;
            if (i == 7)
            {
                sampleCount += 2;
            }
            for (int j = 0; j < sampleCount; j++)
            {
                // Ensure count does not exceed spectrumData length
                if (count < spectrumData.Length)
                {
                    average += spectrumData[count] * (count + 1);
                    count++;
                }
                else
                {
                    // Handle case where count exceeds spectrumData length, e.g., break or log warning
                    // For now, just break to prevent IndexOutOfRangeException
                    break;
                }
            }
             // Check if count is zero to avoid division by zero
            if (count > 0)
            {
                average /= count;
            }
            else
            {
                average = 0; // Or handle as appropriate
            }
            calculatedFreqBands[i] = average * 10;
        }
    }

    private void CalculateFrequencyBands64ForChannel(float[] spectrumData, float[] calculatedFreqBands64)
    {
        int count = 0;
        int sampleCount = 1;
        int power = 0;

        for (int i = 0; i < 64; i++)
        {
            float average = 0;

            if (i == 16 || i == 32 || i == 40 || i == 48 || i == 56)
            {
                power++;
                sampleCount = (int)Mathf.Pow(2, power);
                if (power == 3)
                {
                    sampleCount -= 2;
                }
            }

            for (int j = 0; j < sampleCount; j++)
            {
                if (count < spectrumData.Length)
                {
                    average += spectrumData[count] * (count + 1);
                    count++;
                }
                else
                {
                    break;
                }
            }

            average /= count;
            calculatedFreqBands64[i] = average * 80;
        }
    }

    /// <summary>
    /// Calculates the average amplitude across a specified frequency range for a given channel.
    /// </summary>
    /// <param name="channel">The audio channel (0 for left, 1 for right).</param>
    /// <param name="scale">A multiplier to apply to the calculated amplitude.</param>
    /// <returns>The scaled average amplitude for the specified range and channel.</returns>
    public float GetAverageAmplitudeInRange(int channel, float scale)
    {
        if (!IsInitialized) return 0f;
        float[] targetSpectrumData;
        if (channel == 0)
        {
            targetSpectrumData = spectrumDataLeft;
        }
        else if (channel == 1)
        {
            targetSpectrumData = spectrumDataRight;
        }
        else
        {
            targetSpectrumData = new float[spectrumDataLeft.Length];
            for (int i = 0; i < spectrumDataLeft.Length; i++)
            {
                targetSpectrumData[i] = spectrumDataLeft[i] + spectrumDataRight[i];
            }
        }
        float rangeSum = 0f;
        int numberOfBinsInRange = maxBinIndex - minBinIndex + 1;

        if (numberOfBinsInRange > 0)
        {
            for (int i = minBinIndex; i <= maxBinIndex; i++)
            {
                // Basic check to prevent index out of bounds
                if (i >= 0 && i < targetSpectrumData.Length)
                {
                    rangeSum += targetSpectrumData[i];
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
    /// Gets the pre-calculated amplitude for a specific frequency band for a given channel.
    /// </summary>
    /// <param name="channel">The audio channel (0 for left, 1 for right).</param>
    /// <param name="bandIndex">The index of the band (0-7).</param>
    /// <summary>
    /// Gets the pre-calculated amplitude for a specific frequency band for a given channel.
    /// </summary>
    /// <param name="channel">The audio channel (0 for left, 1 for right).</param>
    /// <param name="bandIndex">The index of the band (0-7).</param>
    /// <param name="scale">A multiplier to apply to the calculated amplitude.</param>
    /// <returns>The scaled amplitude for the specified band and channel.</returns>
    public float GetAmplitudeForBand(int channel, int bandIndex, float scale)
    {
        if (!IsInitialized) return 0f;
        if (bandIndex < 0 || bandIndex >= 8)
        {
            Debug.LogWarning($"GetAmplitudeForBand: Band index must be between 0 and 7. Received: {bandIndex}");
            return 0f;
        }

        float bandValue;
        // Select the appropriate band value based on the channel, left, right, or stereo
        if (channel == 0)
        {
            bandValue = calculatedFreqBandsLeft[bandIndex];
        }
        else if (channel == 1)
        {
            bandValue = calculatedFreqBandsRight[bandIndex];
        }
        else
        {
            bandValue = calculatedFreqBandsLeft[bandIndex] + calculatedFreqBandsRight[bandIndex];
        }

        // Apply the overall scaling passed from the caller
        float finalValue = bandValue * scale;

        // Apply threshold
        finalValue = finalValue > threshold ? finalValue : 0f;
        return finalValue;
    }

    /// <summary>
    /// Gets the pre-calculated amplitude for a specific 64 frequency band for a given channel.
    /// </summary>
    /// <param name="channel">The audio channel (0 for left, 1 for right).</param>
    /// <param name="bandIndex">The index of the band (0-63).</param>
    /// <param name="scale">A multiplier to apply to the calculated amplitude.</param>
    /// <returns>The scaled amplitude for the specified band and channel.</returns>
    public float GetAmplitudeForBand64(int channel, int bandIndex, float scale)
    {
        if (!IsInitialized) return 0f;
        if (bandIndex < 0 || bandIndex >= 64)
        {
            Debug.LogWarning($"GetAmplitudeForBand64: Band index must be between 0 and 63. Received: {bandIndex}");
            return 0f;
        }

        float bandValue;
        // Select the appropriate band value based on the channel, left, right, or stereo
        if (channel == 0)
        {
            bandValue = calculatedFreqBands64Left[bandIndex];
        }
        else if (channel == 1)
        {
            bandValue = calculatedFreqBands64Right[bandIndex];
        }
        else
        {
            bandValue = calculatedFreqBands64Left[bandIndex] + calculatedFreqBands64Right[bandIndex];
        }

        // Apply the overall scaling passed from the caller
        float finalValue = bandValue * scale;

        // Apply threshold
        finalValue = finalValue > threshold ? finalValue : 0f;
        return finalValue;
    }
}
