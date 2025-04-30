using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro; // Required for TextMeshPro Dropdown
using System.Collections.Generic; // Required for List

public class MainMenuController : MonoBehaviour
{
    // Name of the scene to load when Start is clicked
    public string gameSceneName = "Scene1"; // Make sure this matches your game scene filename

    // Reference to the Settings Panel GameObject
    public GameObject settingsPanel;
    // Removed: References to graphics dropdown and volume slider are now in SettingsController

    void Start()
    {
        // Ensure settings panel is initially hidden
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        // Removed: Initialization logic for graphics/volume is now in SettingsController
        // Play background sound based on scene name
        PlaySceneBackgroundSound();
    }

    private void PlaySceneBackgroundSound()
    {
        if (AudioManager.Instance != null)
        {
            string soundName = "BGM";
            AudioManager.Instance.PlaySound(soundName); // Assumes a sound named after the scene exists
        }
        else
        {
            Debug.LogWarning("AudioManager instance not found. Cannot play scene background sound.");
        }
    }


    // Function to be called by the Start Button's OnClick event
    public void StartGame()
    {
        Debug.Log($"Attempting to load scene: {gameSceneName}");
        SceneManager.LoadScene(gameSceneName);
    }

    // Function to be called by the Settings Button's OnClick event
    public void OpenSettings()
    {
        Debug.Log("Open Settings Panel");
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("Settings Panel not assigned in MainMenuController.");
        }
    }

    // Removed: CloseSettings logic is now handled by SettingsController.ClosePanel()
    // Removed: OnVolumeChanged logic is now in SettingsController
    // Removed: OnGraphicsQualityChanged logic is now in SettingsController

    // Function to be called by the Quit Button's OnClick event
    public void QuitGame()
    {
        Debug.Log("Quit Button Clicked");
        // Application.Quit() only works in standalone builds, not in the Editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

}
