using UnityEngine;
using System.Collections.Generic; // For List and Array

namespace TetrisGame
{
    public class GridVisualizer : MonoBehaviour
    {
        [Header("Grid Appearance")]
        [SerializeField] private Color gridLineColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private float lineWidth = 0.05f;
        [SerializeField] private Material lineMaterial; // Shared material for grid lines
        [SerializeField] private bool useCustomShader = true;
        [SerializeField] private string customShaderName = "Custom/AlwaysVisibleLine";

        [Header("Visibility Settings")]
        [SerializeField] private bool showBoundaryCube = true;
        [SerializeField] private float boundaryCubeTransparency = 0.1f;
        [SerializeField] private bool autoGenerateGridOnStart = true;

        private Vector3Int gridSize;
        private GameObject gridContainer; // Container for lines/cube

        // Cached references for performance
        private LineRenderer[] cachedLineRenderers;
        private Renderer boundaryCubeRenderer;
        private Material boundaryCubeMaterialInstance; // Unique material instance for the boundary cube

        private void Start()
        {
            if (autoGenerateGridOnStart)
            {
                if (GameManager.Instance == null)
                {
                    Debug.LogError("GridVisualizer: GameManager not found! Using default grid size.");
                    gridSize = new Vector3Int(3, 10, 3); // Default size
                }
                else
                {
                    gridSize = GameManager.Instance.GetGridSize();
                }

                CreateGridVisualization();

                // Check if we should add material fixer - using string-based approach to avoid direct dependency
                // This part might be redundant if CreateGridVisualization handles material setup correctly,
                // but keeping it for now based on original code structure.
                if (useCustomShader && GetComponent("LineRendererMaterialFixer") == null)
                {
                    // Add the component using AddComponent with string name to avoid direct type reference
                    // Ensure the type exists in the project
                    System.Type fixerType = System.Type.GetType("TetrisGame.LineRendererMaterialFixer");
                    if (fixerType != null)
                    {
                         Component fixer = gameObject.AddComponent(fixerType);
                         // Let the fixer handle material setup
                         Invoke("TriggerMaterialFixer", 0.2f);
                    }
                    else
                    {
                         Debug.LogWarning("GridVisualizer: LineRendererMaterialFixer type not found. Cannot add fixer component.");
                    }
                }
            }
        }

        private void TriggerMaterialFixer()
        {
            // Get material fixer using GetComponent with string to avoid direct dependency
            // Ensure the type exists in the project
            System.Type fixerType = System.Type.GetType("TetrisGame.LineRendererMaterialFixer");
            if (fixerType != null)
            {
                Component fixer = GetComponent(fixerType);
                if (fixer != null)
                {
                    // Invoke the method using reflection to avoid direct type reference
                    System.Reflection.MethodInfo method = fixerType.GetMethod("RefreshLines");
                    if (method != null)
                    {
                        method.Invoke(fixer, null);
                    }
                    else
                    {
                         Debug.LogWarning("GridVisualizer: RefreshLines method not found on LineRendererMaterialFixer.");
                    }
                }
                else
                {
                     Debug.LogWarning("GridVisualizer: LineRendererMaterialFixer component not found after adding.");
                }
            }
        }

        private void CreateGridVisualization()
        {
            // Create a container for all grid lines
            if (gridContainer != null)
            {
                // Destroy existing container and its children
                Destroy(gridContainer);
            }

            gridContainer = new GameObject("GridVisualization");
            gridContainer.transform.parent = transform;
            gridContainer.transform.localPosition = Vector3.zero;
            gridContainer.transform.localRotation = Quaternion.identity;

            // Create default material if none assigned (this will be the shared material)
            if (lineMaterial == null)
            {
                if (useCustomShader)
                {
                    // Try to use our custom line shader
                    Shader customShader = Shader.Find(customShaderName);
                    if (customShader != null)
                    {
                        lineMaterial = new Material(customShader);
                        lineMaterial.SetColor("_Color", gridLineColor);
                        lineMaterial.SetFloat("_Width", lineWidth);
                        // Debug.Log("Using custom line shader for grid visualization."); // Removed debug log
                    }
                }

                // Fallback to sprites shader if custom shader not available
                if (lineMaterial == null)
                {
                    Shader fallbackShader = Shader.Find("Sprites/Default");
                    if (fallbackShader != null)
                    {
                        lineMaterial = new Material(fallbackShader);
                        lineMaterial.color = gridLineColor;

                        // Set material properties for proper line rendering
                        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        lineMaterial.SetInt("_ZWrite", 1); // Enable depth writing for better visibility
                        lineMaterial.DisableKeyword("_ALPHATEST_ON");
                        lineMaterial.EnableKeyword("_ALPHABLEND_ON");
                        lineMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        lineMaterial.renderQueue = 3000; // Higher render queue to ensure visibility
                        // Debug.Log("Using Sprites/Default fallback shader for grid visualization."); // Removed debug log
                    }
                    else
                    {
                         Debug.LogError("GridVisualizer: Failed to find Sprites/Default shader. Line material creation failed.");
                         // Fallback to a very basic material if all else fails
                         lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
                         lineMaterial.color = gridLineColor;
                    }
                }
            }

            // Create only the bottom and two adjacent sides
            CreateBottomAndTwoSides();

            // Create boundary cube (optional)
            CreateBoundaryCube();

            // Cache renderers AFTER creating all lines and the boundary cube
            if (gridContainer != null)
            {
                cachedLineRenderers = gridContainer.GetComponentsInChildren<LineRenderer>();
                Transform boundaryTransform = gridContainer.transform.Find("BoundaryCube");
                if (boundaryTransform != null)
                {
                    boundaryCubeRenderer = boundaryTransform.GetComponent<Renderer>();
                    // Cache the material instance created in CreateBoundaryCube
                    if (boundaryCubeRenderer != null)
                    {
                         boundaryCubeMaterialInstance = boundaryCubeRenderer.material; // This gets the instance created in CreateBoundaryCube
                    }
                }
            }
        }

        private void CreateXLines()
        {
            // Create lines along the X axis
            for (int y = 0; y <= gridSize.y; y++)
            {
                for (int z = 0; z <= gridSize.z; z++)
                {
                    CreateLine(
                        new Vector3(0, y, z),
                        new Vector3(gridSize.x, y, z),
                        "X_Line_" + y + "_" + z
                    );
                }
            }
        }

        private void CreateYLines()
        {
            // Create lines along the Y axis
            for (int x = 0; x <= gridSize.x; x++)
            {
                for (int z = 0; z <= gridSize.z; z++)
                {
                    CreateLine(
                        new Vector3(x, 0, z),
                        new Vector3(x, gridSize.y, z),
                        "Y_Line_" + x + "_" + z
                    );
                }
            }
        }

        private void CreateZLines()
        {
            // Create lines along the Z axis
            for (int x = 0; x <= gridSize.x; x++)
            {
                for (int y = 0; y <= gridSize.y; y++)
                {
                    CreateLine(
                        new Vector3(x, y, 0),
                        new Vector3(x, y, gridSize.z),
                        "Z_Line_" + x + "_" + y
                    );
                }
            }
        }

        private void CreateBottomAndTwoSides()
        {
            // Create bottom face (floor)
            for (int x = 0; x <= gridSize.x; x++)
            {
                // Create Z lines on the floor (y=0)
                CreateLine(
                    new Vector3(x, 0, 0),
                    new Vector3(x, 0, gridSize.z),
                    "Bottom_Z_Line_" + x
                );
            }

            for (int z = 0; z <= gridSize.z; z++)
            {
                // Create X lines on the floor (y=0)
                CreateLine(
                    new Vector3(0, 0, z),
                    new Vector3(gridSize.x, 0, z),
                    "Bottom_X_Line_" + z
                );
            }

            // Create right wall (x=gridSize.x)
            for (int y = 0; y <= gridSize.y; y++)
            {
                // Create Z lines on the right wall (x=gridSize.x)
                CreateLine(
                    new Vector3(gridSize.x, y, 0),
                    new Vector3(gridSize.x, y, gridSize.z),
                    "RightWall_Z_Line_" + y
                );
            }

            for (int z = 0; z <= gridSize.z; z++)
            {
                // Create Y lines on the right wall (x=gridSize.x)
                CreateLine(
                    new Vector3(gridSize.x, 0, z),
                    new Vector3(gridSize.x, gridSize.y, z),
                    "RightWall_Y_Line_" + z
                );
            }

            // Create front wall (z=gridSize.z)
            for (int x = 0; x <= gridSize.x; x++)
            {
                // Create Y lines on the front wall (z=gridSize.z)
                CreateLine(
                    new Vector3(x, 0, gridSize.z),
                    new Vector3(x, gridSize.y, gridSize.z),
                    "FrontWall_Y_Line_" + x
                );
            }

            for (int y = 0; y <= gridSize.y; y++)
            {
                // Create X lines on the front wall (z=gridSize.z)
                CreateLine(
                    new Vector3(0, y, gridSize.z),
                    new Vector3(gridSize.x, y, gridSize.z),
                    "FrontWall_X_Line_" + y
                );
            }
        }

        private void CreateLine(Vector3 start, Vector3 end, string name)
        {
            GameObject line = new GameObject(name);
            line.transform.parent = gridContainer.transform;

            LineRenderer lineRenderer = line.AddComponent<LineRenderer>();
            // Assign the shared line material
            if (lineMaterial != null)
            {
                 lineRenderer.material = lineMaterial;
            }
            else
            {
                 // Fallback material if shared material creation failed
                 lineRenderer.material = new Material(Shader.Find("Standard"));
                 lineRenderer.material.color = gridLineColor;
            }

            lineRenderer.startColor = gridLineColor;
            lineRenderer.endColor = gridLineColor;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);

            // Configure LineRenderer for better visibility
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.allowOcclusionWhenDynamic = false;
            lineRenderer.useWorldSpace = true;

            // Ensure the line is always visible
            lineRenderer.generateLightingData = false;
        }

        private void CreateBoundaryCube()
        {
            // Skip if boundary cube not requested
            if (!showBoundaryCube)
                return;

            // Create a semi-transparent cube to visualize the game boundaries
            GameObject boundary = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boundary.name = "BoundaryCube";
            boundary.transform.parent = gridContainer.transform;

            // Position at the center of the grid
            boundary.transform.position = new Vector3(
                gridSize.x / 2f,
                gridSize.y / 2f,
                gridSize.z / 2f
            );

            // Scale to match grid size
            boundary.transform.localScale = new Vector3(
                gridSize.x,
                gridSize.y,
                gridSize.z
            );

            // Make it semi-transparent
            Renderer renderer = boundary.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Try to use a shader that works well with transparency
                Material boundaryMat;
                Shader transparentShader = Shader.Find("Transparent/Diffuse");
                if (transparentShader != null)
                {
                    boundaryMat = new Material(transparentShader);
                }
                else
                {
                    Shader standardShader = Shader.Find("Standard");
                    if (standardShader != null)
                    {
                        boundaryMat = new Material(standardShader);
                        boundaryMat.SetFloat("_Mode", 3); // Transparent mode in Standard shader
                    }
                    else
                    {
                         // Fallback
                         boundaryMat = new Material(Shader.Find("Hidden/Internal-Colored"));
                    }
                }

                boundaryMat.color = new Color(0.8f, 0.8f, 0.8f, boundaryCubeTransparency);

                // Set transparent material settings for better visibility
                boundaryMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                boundaryMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                boundaryMat.SetInt("_ZWrite", 1); // Enable depth writing
                boundaryMat.DisableKeyword("_ALPHATEST_ON");
                boundaryMat.EnableKeyword("_ALPHABLEND_ON");
                boundaryMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                boundaryMat.renderQueue = 3000;

                renderer.material = boundaryMat; // Assign the unique material instance
            }
            else
            {
                 Debug.LogError("GridVisualizer: No Renderer found on BoundaryCube!");
            }
        }

        // Public method to toggle grid visibility
        public void SetGridVisibility(bool isVisible)
        {
            if (gridContainer != null)
            {
                gridContainer.SetActive(isVisible);
            }
        }

        // Public method to update grid transparency
        public void SetGridTransparency(float alpha)
        {
            // Update the alpha of the shared line material
            if (lineMaterial != null)
            {
                Color lineColor = lineMaterial.color;
                lineColor.a = alpha;
                lineMaterial.color = lineColor;
                // Note: startColor/endColor on LineRenderer are often overridden by material color
                // if the material uses the main color property.
            }
            else
            {
                 Debug.LogWarning("GridVisualizer: Line material is null. Cannot set line transparency.");
            }


            // Update the alpha of the boundary cube's material instance
            if (boundaryCubeMaterialInstance != null)
            {
                Color boundaryColor = boundaryCubeMaterialInstance.color;
                // Make boundary more transparent than grid lines, scaled by the input alpha
                boundaryColor.a = alpha * boundaryCubeTransparency;
                boundaryCubeMaterialInstance.color = boundaryColor;

                // Ensure transparency settings are correct on the boundary material instance
                // These might need to be reapplied if the shader changes or is reset
                 boundaryCubeMaterialInstance.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                 boundaryCubeMaterialInstance.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                 boundaryCubeMaterialInstance.SetInt("_ZWrite", 1);
                 boundaryCubeMaterialInstance.DisableKeyword("_ALPHATEST_ON");
                 boundaryCubeMaterialInstance.EnableKeyword("_ALPHABLEND_ON");
                 boundaryCubeMaterialInstance.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                 boundaryCubeMaterialInstance.renderQueue = 3000;
            }
            else
            {
                 Debug.LogWarning("GridVisualizer: Boundary cube material instance is null. Cannot set boundary transparency.");
            }
        }

        // Public method to update grid transparency
        public void SetLineTransparency(float alpha)
        {
            // Update the alpha of the shared line material
            if (lineMaterial != null)
            {
                Color lineColor = lineMaterial.color;
                lineColor.a = alpha;
                lineMaterial.color = lineColor;
                // Note: startColor/endColor on LineRenderer are often overridden by material color
                // if the material uses the main color property.
            }
            else
            {
                 Debug.LogWarning("GridVisualizer: Line material is null. Cannot set line transparency.");
            }


            // Update the alpha of the boundary cube's material instance
            if (boundaryCubeMaterialInstance != null)
            {
                Color boundaryColor = boundaryCubeMaterialInstance.color;
                // Make boundary more transparent than grid lines, scaled by the input alpha
                boundaryColor.a = alpha * boundaryCubeTransparency;
                boundaryCubeMaterialInstance.color = boundaryColor;

                // Ensure transparency settings are correct on the boundary material instance
                // These might need to be reapplied if the shader changes or is reset
                 boundaryCubeMaterialInstance.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                 boundaryCubeMaterialInstance.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                 boundaryCubeMaterialInstance.SetInt("_ZWrite", 1);
                 boundaryCubeMaterialInstance.DisableKeyword("_ALPHATEST_ON");
                 boundaryCubeMaterialInstance.EnableKeyword("_ALPHABLEND_ON");
                 boundaryCubeMaterialInstance.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                 boundaryCubeMaterialInstance.renderQueue = 3000;
            }
            else
            {
                 Debug.LogWarning("GridVisualizer: Boundary cube material instance is null. Cannot set boundary transparency.");
            }
        }

        // Debug method to verify line rendering
        private void Update()
        {
            // Draw debug lines to help identify if line rendering is working at all
            // These will only be visible in the Scene view, not Game view
            // Debug.DrawLine(Vector3.zero, new Vector3(5, 5, 5), Color.red); // Removed debug draw

            // Make sure our lines are visible (Removed inefficient GetComponentsInChildren call)
            // This logic seems like a workaround. If lines are getting disabled,
            // the root cause should be addressed. Removing this per-frame check.
            // if (gridContainer != null)
            // {
            //     LineRenderer[] lineRenderers = gridContainer.GetComponentsInChildren<LineRenderer>();
            //     if (lineRenderers.Length > 0 && !lineRenderers[0].enabled)
            //     {
            //         foreach (LineRenderer lr in lineRenderers)
            //         {
            //             lr.enabled = true;
            //         }
            //     }
            // }
        }

        // Method to force grid regeneration (can be called from editor)
        public void RegenerateGrid()
        {
            if (GameManager.Instance != null)
            {
                gridSize = GameManager.Instance.GetGridSize();
            }

            if (gridContainer != null)
            {
                Destroy(gridContainer);
            }

            // Clear cached references before creating new ones
            cachedLineRenderers = null;
            boundaryCubeRenderer = null;
            boundaryCubeMaterialInstance = null;

            CreateGridVisualization();

            // Delay trigger material fixer to ensure it's properly initialized
            Invoke("TriggerMaterialFixer", 0.2f);
        }

        // This can be called from the inspector to test grid generation
        [ContextMenu("Generate Test Grid")]
        private void GenerateTestGrid()
        {
            gridSize = new Vector3Int(3, 10, 3);
            RegenerateGrid();
        }

        // Helper method to manually fix the line materials
        [ContextMenu("Fix Line Materials")]
        private void FixLineMaterials()
        {
            // Find all LineRenderer components (using GetComponentsInChildren here is acceptable for an editor utility)
            LineRenderer[] lineRenderers = GetComponentsInChildren<LineRenderer>(true);
            if (lineRenderers.Length == 0)
            {
                Debug.LogWarning("GridVisualizer: No LineRenderer components found in children for manual fix.");
                return;
            }

            // Debug.Log($"Found {lineRenderers.Length} LineRenderer components to fix manually."); // Removed debug log

            // Create material using our custom shader (this will be a new instance for this fix)
            Material fixMaterial = null; // Renamed to avoid confusion with the main lineMaterial
            Shader customShader = Shader.Find(customShaderName);
            if (customShader != null)
            {
                fixMaterial = new Material(customShader);
                fixMaterial.SetColor("_Color", gridLineColor);
                fixMaterial.SetFloat("_Width", lineWidth);
            }
            else
            {
                // Fallback to sprites shader
                Shader fallbackShader = Shader.Find("Sprites/Default");
                if (fallbackShader != null)
                {
                    fixMaterial = new Material(fallbackShader);
                    fixMaterial.color = gridLineColor;
                }
                else
                {
                     Debug.LogError("GridVisualizer: Failed to find fallback shader for manual fix.");
                     return; // Cannot fix without a material
                }
            }

            // Configure material properties
            fixMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            fixMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            fixMaterial.SetInt("_ZWrite", 1);
            fixMaterial.EnableKeyword("_ALPHABLEND_ON");
            fixMaterial.renderQueue = 3000;

            // Apply to all LineRenderers
            foreach (LineRenderer lr in lineRenderers)
            {
                // Assign the *new* fix material instance
                lr.material = fixMaterial;
                // Note: startColor/endColor are often overridden by material color
                lr.startColor = gridLineColor;
                lr.endColor = gridLineColor;
                lr.startWidth = lineWidth;
                lr.endWidth = lineWidth;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.useWorldSpace = true;
            }

            // Debug.Log("Applied material fixes manually to all LineRenderer components."); // Removed debug log

            // Note: This manual fix creates a new material instance.
            // The main grid visualization uses the 'lineMaterial' field.
            // If you want this manual fix to update the main 'lineMaterial',
            // you would need to assign 'fixMaterial' to 'lineMaterial' and
            // potentially re-apply it to the cached renderers.
        }

        // Ensure material instances are destroyed when the object is destroyed
        private void OnDestroy()
        {
            if (lineMaterial != null)
            {
                Destroy(lineMaterial);
            }
            if (boundaryCubeMaterialInstance != null)
            {
                Destroy(boundaryCubeMaterialInstance);
            }
            // Note: Materials created by the manual FixLineMaterials context menu
            // are not tracked by this script and would need separate cleanup
            // if they persist beyond the editor session.
        }
    }
}
