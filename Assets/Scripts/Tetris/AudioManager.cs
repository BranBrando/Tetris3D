using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Audio; // Needed for AudioMixer

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // Removed single audioSource and SourceComponent
    // AudioSources will now be managed per Sound object

    [Header("Mixer Settings")]
    public AudioMixer mainMixer; // Assign your main AudioMixer asset here
    public string masterVolumeParameter = "MasterVolume"; // Name of the exposed volume parameter in the mixer

    [Header("Spectrum Analysis Settings")]
    public int spectrumSize = 128; // Size of the spectrum data (must be a power of 2)
    public FFTWindow spectrumWindow = FFTWindow.BlackmanHarris; // FFT window type
    [Range(20, 20000)]
    public float spectrumMinFrequency = 20; // Minimum frequency for analysis
    [Range(20, 20000)]
    public float spectrumMaxFrequency = 20000; // Maximum frequency for analysis
    [Range(0.0001f, 1f)]
    public float spectrumThreshold = 0.01f; // Amplitude threshold

    [Header("Sound Definitions")]
    public Sound[] sounds; // Array to hold sound definitions

    private AudioClip[] loadedMusicClips;

    void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Make this object persistent across scenes

            // Subscribe to sceneLoaded event
            SceneManager.sceneLoaded += UpdateAudioSourcesForScene;

            // Initialize audio sources for the starting scene
            UpdateAudioSourcesForScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);

            // Load background music clips from Resources
            loadedMusicClips = Resources.LoadAll<AudioClip>("Audio/Sound tracks");
            if (loadedMusicClips == null || loadedMusicClips.Length == 0)
            {
                Debug.LogError("AudioManager: Failed to load any background music clips from Resources/Audio/Sound tracks. Ensure the path is correct and clips exist.");
            }
        }
        else if (Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= UpdateAudioSourcesForScene;
    }

    void UpdateAudioSourcesForScene(Scene scene, LoadSceneMode mode)
    {

        List<Sound> bgmSounds = new List<Sound>();

        // Background Music Logic
        if (loadedMusicClips != null && loadedMusicClips.Length > 0)
        {

            foreach (Sound s in sounds)
            {
                if (s.isBackgroundMusic)
                {
                    bgmSounds.Add(s);
                }
            }

            // Stop any currently playing background music
            foreach (Sound s in bgmSounds)
            {
                if (s.source != null && s.source.isPlaying)
                {
                    s.source.Stop();
                }
            }

            if (bgmSounds.Count > 0)
            {
                AudioClip randomClip = loadedMusicClips[Random.Range(0, loadedMusicClips.Length)];

                foreach (Sound s in bgmSounds)
                {
                    s.clip = randomClip;
                    if (s.source == null)
                    {
                        s.source = gameObject.AddComponent<AudioSource>();
                        s.source.volume = s.volume;
                        s.source.pitch = s.pitch;
                        s.source.loop = true;
                        s.source.playOnAwake = false;
                    }
                    s.source.clip = randomClip;
                    s.source.loop = true; // Ensure looping is enabled
                }

                // Play one of the background music tracks
                bgmSounds[0].source.Play();
            }
        }

        // Initialize AudioSpectrumProcessor
        if (bgmSounds.Count > 0 && bgmSounds[0].source != null)
        {
            AudioSpectrumProcessor.Instance.Initialize(bgmSounds[0].source, spectrumSize, spectrumWindow, spectrumMinFrequency, spectrumMaxFrequency, spectrumThreshold);
        }
        else
        {
            Debug.LogWarning($"AudioManager: No background music is playing, cannot initialize AudioSpectrumProcessor.");
        }
    }

    // Method to play all sounds whose names contain the provided keyword.
    public void PlaySound(string keyword) // Parameter name changed for clarity
    {
        bool soundPlayed = false; // Flag to track if any sound was played

        if (string.IsNullOrEmpty(keyword))
        {
            Debug.LogWarning("PlaySound called with an empty or null keyword.");
            return;
        }

        // Stop all currently playing sounds first
        foreach (Sound soundToStop in sounds)
        {
            if (soundToStop != null && soundToStop.source != null && soundToStop.source.isPlaying)
            {
                soundToStop.source.Stop();
            }
        }

        // Now, find and play sounds matching the keyword
        foreach (Sound s in sounds)
        {
            // Check if the sound name contains the keyword (case-insensitive check might be better depending on use case)
            // Example for case-insensitive: if (s != null && s.name != null && s.name.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
            if (s != null && s.name != null && s.name.Contains(keyword)) // Added null checks for safety
            {
                if (s.source != null)
                {
                    s.source.Play();
                    soundPlayed = true; // Mark that at least one sound was played
                }
                else
                {
                    Debug.LogError($"Sound '{s.name}' (matches keyword '{keyword}') has no associated AudioSource component.");
                }
            }
        }

        // Log a warning if no sounds matched the keyword
        if (!soundPlayed)
        {
            Debug.LogWarning($"PlaySound: No sounds found containing the keyword '{keyword}'.");
        }
    }


    // Method to stop a sound by name
    public void StopSound(string name)
    {
        Sound s = System.Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + name + " not found!");
            return;
        }
         if (s.source != null)
        {
            s.source.Stop();
        }
    }

    /// <summary>
    /// Gets the specific AudioSource associated with a sound definition.
    /// Useful for scripts like AudioSpectrumBinder that need direct access.
    /// Finds the *first* sound whose name contains the given keyword.
    /// </summary>
    /// <param name="keyword">The keyword to search for within sound names.</param>
    /// <returns>The associated AudioSource of the first matching sound, or null if none found.</returns>
    public AudioSource GetAudioSourceForSound(string keyword) // Parameter name changed
    {
        if (string.IsNullOrEmpty(keyword))
        {
            Debug.LogWarning("GetAudioSourceForSound called with an empty or null keyword.");
            return null;
        }

        foreach (Sound s in sounds)
        {
            // Check if the sound name contains the keyword
            // Example for case-insensitive: if (s != null && s.name != null && s.name.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
            if (s != null && s.name != null && s.name.Contains(keyword)) // Added null checks
            {
                // Return the source of the first match found
                return s.source;
            }
        }

        // If no sound containing the keyword was found after checking all sounds
        Debug.LogWarning($"GetAudioSourceForSound: No sound found containing the keyword '{keyword}'.");
        return null;
    }

    /// <summary>
    /// Sets the volume of an exposed parameter on the main AudioMixer.
    /// </summary>
    /// <param name="linearVolume">Volume level from 0.0 (silent) to 1.0 (full).</param>
    public void SetMasterVolume(float linearVolume)
    {
        if (mainMixer == null)
        {
            Debug.LogError("AudioMixer is not assigned in AudioManager!", this);
            return;
        }
        if (string.IsNullOrEmpty(masterVolumeParameter))
        {
             Debug.LogError("Master Volume Parameter name is not set in AudioManager!", this);
            return;
        }

        // Convert linear slider value (0.0001 to 1.0) to dB (-80 to 0)
        // Clamp linearVolume to avoid Log10(0) or negative values
        float dBVolume = Mathf.Log10(Mathf.Max(linearVolume, 0.0001f)) * 20f;
        Debug.Log($"Setting master volume to {dBVolume} dB (linear: {linearVolume})");
        
        bool result = mainMixer.SetFloat(masterVolumeParameter, dBVolume);

        if (!result)
        {
            Debug.LogError($"Failed to set AudioMixer parameter '{masterVolumeParameter}'. Ensure it is exposed.", this);
        }
    }

    /// <summary>
    /// Gets the current volume of an exposed parameter on the main AudioMixer.
    /// </summary>
    /// <returns>Volume level from 0.0 (silent) to 1.0 (full), or -1 if an error occurs.</returns>
    public float GetMasterVolume()
    {
        if (mainMixer == null)
        {
            Debug.LogError("AudioMixer is not assigned in AudioManager!", this);
            return -1f; // Indicate error
        }
        if (string.IsNullOrEmpty(masterVolumeParameter))
        {
             Debug.LogError("Master Volume Parameter name is not set in AudioManager!", this);
             return -1f; // Indicate error
        }

        float dBVolume;
        if (mainMixer.GetFloat(masterVolumeParameter, out dBVolume))
        {
            // Convert dB (-80 to 0) back to linear (0.0001 to 1.0)
            // Formula: 10^(dB/20)
            return Mathf.Pow(10f, dBVolume / 20f);
        }
        else
        {
            Debug.LogError($"Failed to get AudioMixer parameter '{masterVolumeParameter}'. Ensure it is exposed and the mixer is active.", this);
            return -1f; // Indicate error
        }
    }

    // Optional: Add methods for music, stopping sounds, etc. (e.g., StopAllMusicTracks)
}

// Structure for managing sounds
[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;
    [Range(.1f, 3f)]
    public float pitch = 1f;
    public bool loop = false;

    // Optional: Assign to a specific mixer group
    // public string outputMixerGroup;

    // Runtime-assigned source component
    [HideInInspector]
    public AudioSource source;
    public bool isBackgroundMusic = false;
}
