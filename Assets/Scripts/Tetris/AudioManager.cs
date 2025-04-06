using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Audio; // Needed for AudioMixer

[RequireComponent(typeof(AudioSource))] // Ensure AudioSource component is present
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private AudioSource audioSource;
    public AudioSource SourceComponent { get; private set; } // Public accessor for the AudioSource

    [Header("Mixer Settings")]
    public AudioMixer mainMixer; // Assign your main AudioMixer asset here
    public string masterVolumeParameter = "MasterVolume"; // Name of the exposed volume parameter in the mixer

    [Header("Sound Definitions")]
    public Sound[] sounds; // Array to hold sound definitions

    void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Make this object persistent across scenes
            audioSource = GetComponent<AudioSource>(); // Get the AudioSource component
            SourceComponent = audioSource; // Assign the public accessor
        }
        else if (Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
    }

    // Method to play a sound by name
    public void PlaySound(string name)
    {
        Sound s = System.Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + name + " not found!");
            return;
        }

        // Configure the main AudioSource (can be expanded for multiple sources)
        audioSource.clip = s.clip;
        audioSource.volume = s.volume;
        audioSource.pitch = s.pitch;
        audioSource.loop = s.loop;

        audioSource.Play(); // Use Play for one-shot sounds usually
        // Use PlayOneShot for overlapping sounds: audioSource.PlayOneShot(s.clip, s.volume);
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

    // Optional: Add methods for music, stopping sounds, etc.
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

    // Hide the source field in the inspector as we manage it internally
    // [HideInInspector]
    // public AudioSource source; // Could be used if you want separate sources per sound type
}
