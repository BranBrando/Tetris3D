using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro; // Required for TextMeshPro Dropdown
using System.Collections.Generic; // Required for List

public class MainMenuController : MonoBehaviour
{
    // Name of the scene to load when Start is clicked
    public string gameSceneName = "TetrisGame"; // Make sure this matches your game scene filename

    // Reference to the Settings Panel GameObject
    public GameObject settingsPanel;
    // Reference to the Graphics Dropdown
    public TMP_Dropdown graphicsDropdown;

    void Start()
    {
        // Ensure settings panel is initially hidden
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        // Populate Graphics Dropdown
        if (graphicsDropdown != null)
        {
            graphicsDropdown.ClearOptions();
            // Get quality level names from QualitySettings
            graphicsDropdown.AddOptions(new List<string>(QualitySettings.names));
            // Set dropdown to current quality level
            graphicsDropdown.value = QualitySettings.GetQualityLevel();
            graphicsDropdown.RefreshShownValue();
            // Add listener for when the value changes
            // Note: Linking via Inspector is often preferred, but this shows programmatic linking.
            // If linked via Inspector, this line might cause double calls.
            graphicsDropdown.onValueChanged.AddListener(OnGraphicsQualityChanged);
        }
        else
        {
            Debug.LogWarning("Graphics Dropdown not assigned in MainMenuController.");
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

    // Function to be called by the Back Button within the Settings Panel
    public void CloseSettings()
    {
        Debug.Log("Close Settings Panel");
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    // Function to be called by the Volume Slider's OnValueChanged event
    public void OnVolumeChanged(float value)
    {
        // Placeholder: Implement volume control logic here
        // Example: AudioListener.volume = value;
        // Or better: Use AudioMixer exposed parameters
        Debug.Log("Volume changed to: " + value);
    }

    // Function to be called by the Graphics Dropdown's OnValueChanged event
    public void OnGraphicsQualityChanged(int index)
    {
        Debug.Log($"Graphics Quality changed to index: {index}, name: {QualitySettings.names[index]}");
        QualitySettings.SetQualityLevel(index);
    }

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
