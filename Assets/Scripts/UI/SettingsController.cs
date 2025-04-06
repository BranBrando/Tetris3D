using UnityEngine;
using UnityEngine.UI;
using TMPro; // Required for TextMeshPro Dropdown
using System.Collections.Generic; // Required for List
using UnityEngine.SceneManagement; // Required for loading scenes

public class SettingsController : MonoBehaviour
{
    // References to UI elements within the settings panel
    public TMP_Dropdown graphicsDropdown;
    public Slider volumeSlider;
    public GameObject backButton;    // Assign in Inspector
    public GameObject continueButton; // Assign in Inspector
    public GameObject mainMenuButton;     // Assign in Inspector
    public GameObject quitButton;     // Assign in Inspector

    // Called when the GameObject becomes enabled and active
    void OnEnable()
    {
        // Update button visibility based on the current scene
        UpdateButtonVisibility();
    }

    void Start()
    {
        InitializeGraphicsDropdown();
        InitializeVolumeSlider();
        // Initial setup on first load (optional, as OnEnable covers it)
        // UpdateButtonVisibility();
    }

    void UpdateButtonVisibility()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        bool isMainMenu = currentSceneName == "MainMenu";

        if (backButton != null) backButton.SetActive(isMainMenu);
        if (continueButton != null) continueButton.SetActive(!isMainMenu);
        if (mainMenuButton != null) mainMenuButton.SetActive(!isMainMenu); // Main Menu is active when NOT in main menu
        if (quitButton != null) quitButton.SetActive(!isMainMenu); // Quit is active when NOT in main menu
    }

    void InitializeGraphicsDropdown()
    {
        if (graphicsDropdown != null)
        {
            graphicsDropdown.ClearOptions();
            // Get quality level names from QualitySettings
            graphicsDropdown.AddOptions(new List<string>(QualitySettings.names));
            // Set dropdown to current quality level
            graphicsDropdown.value = QualitySettings.GetQualityLevel();
            graphicsDropdown.RefreshShownValue();
            // Add listener for when the value changes
            // Remove existing listeners first to prevent duplicates
            graphicsDropdown.onValueChanged.RemoveAllListeners();
            graphicsDropdown.onValueChanged.AddListener(OnGraphicsQualityChanged);
        }
        else
        {
            Debug.LogWarning("Graphics Dropdown not assigned in SettingsController.", this);
        }
    }

    void InitializeVolumeSlider()
    {
        if (volumeSlider != null)
        {
            // Remove existing listeners to prevent duplicates
            volumeSlider.onValueChanged.RemoveAllListeners();
            // Add our listener function
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

            // Set initial slider value from AudioManager
            if (AudioManager.Instance != null)
            {
                float currentVolume = AudioManager.Instance.GetMasterVolume();
                if (currentVolume >= 0) // Check if GetMasterVolume succeeded
                {
                    volumeSlider.value = currentVolume;
                }
                else
                {
                    Debug.LogWarning("Could not retrieve initial volume from AudioManager. Slider not set.", this);
                }
            }
            else
            {
                Debug.LogWarning("AudioManager instance not found at Start. Cannot set initial slider volume.", this);
            }
        }
        else
        {
            Debug.LogWarning("Volume Slider not assigned in SettingsController.", this);
        }
    }

    // Function to be called by the Volume Slider's OnValueChanged event
    public void OnVolumeChanged(float value)
    {
        // Call the AudioManager to set the master volume via the mixer
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(value);
        }
        else
        {
            Debug.LogWarning("AudioManager instance not found. Cannot set volume.", this);
        }
    }

    // Function to be called by the Graphics Dropdown's OnValueChanged event
    public void OnGraphicsQualityChanged(int index)
    {
        Debug.Log($"Graphics Quality changed to index: {index}, name: {QualitySettings.names[index]}");
        QualitySettings.SetQualityLevel(index);
    }

    // Renamed function to hide the panel - used by Back and Continue buttons
    public void HideSettingsPanel()
    {
        Debug.Log("Hide Settings Panel requested.");
        // Deactivate the panel this script is attached to
        gameObject.SetActive(false);
    }

    // Function to be called by a Return to Main Menu Button (if you still need this separate button)
    public void ReturnToMainMenu()
    {
        Debug.Log("Returning to Main Menu.");
        SceneManager.LoadScene("MainMenu"); // Make sure "MainMenu" is the exact name of your scene file (without .unity) and it's added to Build Settings
    }

    // Function to be called by the Quit Button (only active when not in MainMenu)
    public void QuitGame()
    {
        Debug.Log("Quit Game requested.");
#if UNITY_EDITOR
        // Stop playing the scene in the editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Quit the application in a build
        Application.Quit();
#endif
    }
}
