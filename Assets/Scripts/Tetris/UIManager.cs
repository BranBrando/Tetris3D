using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace TetrisGame
{
    public class UIManager : MonoBehaviour
    {
        [Header("Score UI")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text planesText;
        [SerializeField] private TMP_Text bestScoreText;

        [Header("Game Info UI")]
        [SerializeField] private GameObject settingsPanel; // Renamed from controlsPanel
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Button restartButton;

        [Header("Optional 3D Helpers")]
        [SerializeField] private GameObject gridVisualization;
        [SerializeField] private Toggle gridToggle;
        [SerializeField] private Slider transparencySlider;

        private bool isGameOver = false;

        // Cached reference to the GridVisualizer
        private GridVisualizer gridVisualizer;

        private void Start()
        {
            // Setup initial UI
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);

            if (settingsPanel != null) // Renamed from controlsPanel
                settingsPanel.SetActive(false); // Renamed from controlsPanel

            if (restartButton != null)
                restartButton.onClick.AddListener(RestartGame);

            if (gridToggle != null)
                gridToggle.onValueChanged.AddListener(ToggleGridVisualization);

            if (transparencySlider != null)
                transparencySlider.onValueChanged.AddListener(AdjustGridTransparency);

            // Initialize grid visualization
            if (gridVisualization != null)
            {
                gridVisualization.SetActive(true);
                // Cache the GridVisualizer component
                gridVisualizer = gridVisualization.GetComponent<GridVisualizer>();
                if (gridVisualizer == null)
                {
                    Debug.LogError("UIManager: GridVisualization GameObject does not have a GridVisualizer component!");
                }
            }

            if (ScoreManager.Instance != null && bestScoreText != null)
            {
                bestScoreText.text = $"Best Score: {ScoreManager.Instance.GetBestScore()}";
            }
        }

        private void Update()
        {
            if (ScoreManager.Instance != null)
            {
                if (scoreText != null)
                    scoreText.text = $"Score: {ScoreManager.Instance.GetScore()}";

                if (levelText != null)
                    levelText.text = $"Level: {ScoreManager.Instance.GetLevel()}";

                if (planesText != null)
                    planesText.text = $"Planes: {ScoreManager.Instance.GetLines()}";
            }

            // Check for game over
            if (!isGameOver && GameManager.Instance != null && GameManager.Instance.IsGameOver())
            {
                ShowGameOver();
            }

            // Toggle settings panel (using escape key for now)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleSettingsPanel(); // Renamed method call
            }
        }

        // Renamed method
        private void ShowSettingsTemporarily()
        {
            if (settingsPanel != null) // Renamed variable
            {
                settingsPanel.SetActive(true); // Renamed variable
                Invoke("HideSettings", 5f); // Renamed method call
            }
        }

        // Renamed method
        private void HideSettings()
        {
            if (settingsPanel != null) // Renamed variable
            {
                settingsPanel.SetActive(false); // Renamed variable
            }
        }

        // Renamed method
        private void ToggleSettingsPanel()
        {
            if (settingsPanel != null) // Renamed variable
            {
                settingsPanel.SetActive(!settingsPanel.activeSelf); // Renamed variable
            }
        }

        private void ShowGameOver()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.CheckAndSaveBestScore(); // Check and save score first
                if (bestScoreText != null) // Update the display after potentially saving
                {
                    bestScoreText.text = $"Best: {ScoreManager.Instance.GetBestScore()}";
                }
            }

            isGameOver = true;
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
        }

        private void RestartGame()
        {
            // Reset game state
            isGameOver = false;

            // Hide game over panel
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            // Reload the scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        private void ToggleGridVisualization(bool isVisible)
        {
            if (gridVisualization != null)
            {
                gridVisualization.SetActive(isVisible);
            }
        }

        private void AdjustGridTransparency(float value)
        {
            // Call the SetLineTransparency method on the GridVisualizer
            if (gridVisualizer != null)
            {
                gridVisualizer.SetLineTransparency(value);
            }
            else
            {
                Debug.LogWarning("UIManager: GridVisualizer reference is null. Cannot adjust grid transparency.");
            }
        }
    }
}
