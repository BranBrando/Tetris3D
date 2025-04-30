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
                gridVisualization.SetActive(true);
                
            // Show controls for the first few seconds
            // ShowControlsTemporarily();

            if (ScoreManager.Instance != null && bestScoreText != null)
            {
                bestScoreText.text = $"Best: {ScoreManager.Instance.GetBestScore()}";
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
            if (gridVisualization != null)
            {
                // Get all renderers in the grid visualization
                Renderer[] renderers = gridVisualization.GetComponentsInChildren<Renderer>();
                
                // Update material transparency
                foreach (Renderer renderer in renderers)
                {
                    Material mat = renderer.material;
                    Color color = mat.color;
                    color.a = value;
                    mat.color = color;
                    
                    // Make sure we're using the right rendering mode for transparency
                    if (value < 1.0f)
                    {
                        mat.SetFloat("_Mode", 3); // Transparent mode
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                        mat.DisableKeyword("_ALPHATEST_ON");
                        mat.EnableKeyword("_ALPHABLEND_ON");
                        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.renderQueue = 3000;
                    }
                    else
                    {
                        mat.SetFloat("_Mode", 0); // Opaque mode
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        mat.SetInt("_ZWrite", 1);
                        mat.DisableKeyword("_ALPHATEST_ON");
                        mat.DisableKeyword("_ALPHABLEND_ON");
                        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.renderQueue = -1;
                    }
                }
            }
        }
    }
}
