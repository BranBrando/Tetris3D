using UnityEngine;

namespace TetrisGame
{
    public class GridVisualizer : MonoBehaviour
    {
        [Header("Grid Appearance")]
        [SerializeField] private Color gridLineColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private float lineWidth = 0.05f;
        [SerializeField] private Material lineMaterial;
        [SerializeField] private bool useCustomShader = true;
        [SerializeField] private string customShaderName = "Custom/AlwaysVisibleLine";

        [Header("Visibility Settings")]
        [SerializeField] private bool showBoundaryCube = true;
        [SerializeField] private float boundaryCubeTransparency = 0.1f;
        [SerializeField] private bool autoGenerateGridOnStart = true;

        private Vector3Int gridSize;
        private GameObject gridContainer;

        private void Start()
        {
            if (autoGenerateGridOnStart)
            {
                if (GameManager.Instance == null)
                {
                    Debug.LogError("GameManager not found! Using default grid size.");
                    gridSize = new Vector3Int(3, 10, 3); // Default size
                }
                else
                {
                    gridSize = GameManager.Instance.GetGridSize();
                }

                CreateGridVisualization();

                // Check if we should add material fixer - using string-based approach to avoid direct dependency
                if (useCustomShader && GetComponent("LineRendererMaterialFixer") == null)
                {
                    // Add the component using AddComponent with string name to avoid direct type reference
                    gameObject.AddComponent(System.Type.GetType("LineRendererMaterialFixer"));
                    // Let the fixer handle material setup
                    Invoke("TriggerMaterialFixer", 0.2f);
                }
            }
        }

        private void TriggerMaterialFixer()
        {
            // Get material fixer using GetComponent with string to avoid direct dependency
            Component fixer = GetComponent("LineRendererMaterialFixer");
            if (fixer != null)
            {
                // Invoke the method using reflection to avoid direct type reference
                System.Reflection.MethodInfo method = fixer.GetType().GetMethod("RefreshLines");
                if (method != null)
                {
                    method.Invoke(fixer, null);
                }
            }
        }

        private void CreateGridVisualization()
        {
            // Create a container for all grid lines
            if (gridContainer != null)
            {
                Destroy(gridContainer);
            }

            gridContainer = new GameObject("GridVisualization");
            gridContainer.transform.parent = transform;

            // Create default material if none assigned
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
                        Debug.Log("Using custom line shader for grid visualization.");
                    }
                }

                // Fallback to sprites shader if custom shader not available
                if (lineMaterial == null)
                {
                    lineMaterial = new Material(Shader.Find("Sprites/Default"));
                    lineMaterial.color = gridLineColor;

                    // Set material properties for proper line rendering
                    lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    lineMaterial.SetInt("_ZWrite", 1); // Enable depth writing for better visibility
                    lineMaterial.DisableKeyword("_ALPHATEST_ON");
                    lineMaterial.EnableKeyword("_ALPHABLEND_ON");
                    lineMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    lineMaterial.renderQueue = 3000; // Higher render queue to ensure visibility
                }
            }

            // Create only the bottom and two adjacent sides
            CreateBottomAndTwoSides();

            // Create boundary cube (optional - comment out if not needed)
            CreateBoundaryCube();
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
            lineRenderer.material = lineMaterial;
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

            // Try to use a shader that works well with transparency
            Material boundaryMat;
            Shader transparentShader = Shader.Find("Transparent/Diffuse");
            if (transparentShader != null)
            {
                boundaryMat = new Material(transparentShader);
            }
            else
            {
                boundaryMat = new Material(Shader.Find("Standard"));
                boundaryMat.SetFloat("_Mode", 3); // Transparent mode in Standard shader
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

            renderer.material = boundaryMat;
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
            if (gridContainer != null)
            {
                gridLineColor.a = alpha;

                // Update all line renderers
                LineRenderer[] lineRenderers = gridContainer.GetComponentsInChildren<LineRenderer>();
                foreach (LineRenderer lr in lineRenderers)
                {
                    Color color = gridLineColor;
                    lr.startColor = color;
                    lr.endColor = color;
                }

                // Update boundary cube
                Transform boundary = gridContainer.transform.Find("BoundaryCube");
                if (boundary != null)
                {
                    Renderer renderer = boundary.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        Color color = renderer.material.color;
                        color.a = alpha * 0.2f; // Make boundary more transparent than grid lines
                        renderer.material.color = color;
                    }
                }
            }
        }

        // Debug method to verify line rendering
        private void Update()
        {
            // Draw debug lines to help identify if line rendering is working at all
            // These will only be visible in the Scene view, not Game view
            Debug.DrawLine(Vector3.zero, new Vector3(5, 5, 5), Color.red);

            // Make sure our lines are visible
            if (gridContainer != null)
            {
                LineRenderer[] lineRenderers = gridContainer.GetComponentsInChildren<LineRenderer>();
                if (lineRenderers.Length > 0 && !lineRenderers[0].enabled)
                {
                    foreach (LineRenderer lr in lineRenderers)
                    {
                        lr.enabled = true;
                    }
                }
            }
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
            // Find all LineRenderer components
            LineRenderer[] lineRenderers = GetComponentsInChildren<LineRenderer>(true);
            if (lineRenderers.Length == 0)
            {
                Debug.LogWarning("No LineRenderer components found in children.");
                return;
            }

            Debug.Log($"Found {lineRenderers.Length} LineRenderer components to fix.");

            // Create material using our custom shader
            Material lineMaterial = null;
            Shader customShader = Shader.Find(customShaderName);
            if (customShader != null)
            {
                lineMaterial = new Material(customShader);
                lineMaterial.SetColor("_Color", gridLineColor);
                lineMaterial.SetFloat("_Width", lineWidth);
            }
            else
            {
                // Fallback to sprites shader
                lineMaterial = new Material(Shader.Find("Sprites/Default"));
                lineMaterial.color = gridLineColor;
            }

            // Configure material properties
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_ZWrite", 1);
            lineMaterial.EnableKeyword("_ALPHABLEND_ON");
            lineMaterial.renderQueue = 3000;

            // Apply to all LineRenderers
            foreach (LineRenderer lr in lineRenderers)
            {
                lr.material = lineMaterial;
                lr.startColor = gridLineColor;
                lr.endColor = gridLineColor;
                lr.startWidth = lineWidth;
                lr.endWidth = lineWidth;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.useWorldSpace = true;
            }

            Debug.Log("Applied material fixes manually to all LineRenderer components.");
        }
    }
}
