using UnityEngine;

namespace TetrisGame
{
    /// <summary>
    /// A utility class to fix line renderer material issues, especially when using URP.
    /// This can be attached to the same object as GridVisualizer.
    /// </summary>
    public class LineRendererMaterialFixer : MonoBehaviour
    {
        [Tooltip("Reference to our custom line shader")]
        [SerializeField] private string customShaderName = "Custom/AlwaysVisibleLine";
        
        [Tooltip("Reference to a built-in shader that works well with lines, like 'Sprites/Default'")]
        [SerializeField] private string fallbackShaderName = "Sprites/Default";
        
        [Tooltip("Reference to a URP shader that works with lines, like 'Universal Render Pipeline/Unlit'")]
        [SerializeField] private string urpShaderName = "Universal Render Pipeline/Unlit";
        
        [Tooltip("Color for the grid lines")]
        [SerializeField] private Color lineColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        
        [Tooltip("Thickness of the grid lines")]
        [SerializeField] private float lineThickness = 0.03f;
        
        private void Start()
        {
            // Wait a frame to ensure GridVisualizer has created all lines
            Invoke("FixLineMaterials", 0.1f);
        }
        
        public void FixLineMaterials()
        {
            LineRenderer[] lineRenderers = GetComponentsInChildren<LineRenderer>(true);
            if (lineRenderers.Length == 0)
            {
                Debug.LogWarning("No LineRenderer components found in children.");
                return;
            }
            
            Debug.Log($"Found {lineRenderers.Length} LineRenderer components to fix.");
            
            Material lineMaterial = null;
            
            // First attempt - try our custom shader designed specifically for lines
            Shader customShader = Shader.Find(customShaderName);
            if (customShader != null)
            {
                lineMaterial = new Material(customShader);
                lineMaterial.SetColor("_Color", lineColor);
                lineMaterial.SetFloat("_Width", lineThickness);
                Debug.Log("Created material with custom line shader.");
            }
            // Second attempt - try URP shader if we're using URP
            else if (IsUsingURP())
            {
                Shader urpShader = Shader.Find(urpShaderName);
                if (urpShader != null)
                {
                    lineMaterial = new Material(urpShader);
                    lineMaterial.SetColor("_BaseColor", lineColor); // URP uses _BaseColor
                    
                    // For URP Unlit shader
                    if (urpShaderName.Contains("Unlit"))
                    {
                        lineMaterial.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent
                        lineMaterial.SetFloat("_Blend", 0);   // 0 = Alpha, 1 = Premultiply, 2 = Additive, 3 = Multiply
                        lineMaterial.SetFloat("_AlphaClip", 0); // 0 = off, 1 = on
                        lineMaterial.renderQueue = 3000;
                    }
                    
                    Debug.Log("Created URP compatible material for lines.");
                }
            }
            
            // Third attempt - fallback to built-in shader
            if (lineMaterial == null)
            {
                Shader fallbackShader = Shader.Find(fallbackShaderName);
                if (fallbackShader != null)
                {
                    lineMaterial = new Material(fallbackShader);
                    lineMaterial.color = lineColor;
                    Debug.Log("Created fallback material for lines.");
                }
                else
                {
                    // Last resort - use a very basic shader that should work anywhere
                    lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
                    lineMaterial.color = lineColor;
                    Debug.Log("Created basic colored material for lines as last resort.");
                }
            }
            
            // Configure the material for transparency
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_ZWrite", 1);  // Enable Z writing
            lineMaterial.EnableKeyword("_ALPHABLEND_ON");
            
            // Apply to all line renderers
            foreach (LineRenderer lineRenderer in lineRenderers)
            {
                lineRenderer.material = lineMaterial;
                lineRenderer.startColor = lineColor;
                lineRenderer.endColor = lineColor;
                lineRenderer.startWidth = lineThickness;
                lineRenderer.endWidth = lineThickness;
                
                // Additional settings to improve visibility
                lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lineRenderer.receiveShadows = false;
                lineRenderer.allowOcclusionWhenDynamic = false;
                lineRenderer.useWorldSpace = true;
                lineRenderer.generateLightingData = false;
                
                // Force the line renderer to refresh
                lineRenderer.enabled = false;
                lineRenderer.enabled = true;
            }
            
            Debug.Log("Applied material fixes to all LineRenderer components.");
        }
        
        private bool IsUsingURP()
        {
            // Check if we're using URP based on the active render pipeline
            return UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null && 
                   UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("Universal");
        }
        
        private void OnValidate()
        {
            // Add shader path validation
            if (string.IsNullOrEmpty(customShaderName))
            {
                customShaderName = "Custom/AlwaysVisibleLine";
            }
            
            if (string.IsNullOrEmpty(fallbackShaderName))
            {
                fallbackShaderName = "Sprites/Default";
            }
            
            if (string.IsNullOrEmpty(urpShaderName))
            {
                urpShaderName = "Universal Render Pipeline/Unlit";
            }
        }
        
        // Utility method that can be called from the editor or code
        public void RefreshLines()
        {
            FixLineMaterials();
        }
    }
}
