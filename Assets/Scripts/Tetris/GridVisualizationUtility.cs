using UnityEngine;

namespace TetrisGame
{
    /// <summary>
    /// A standalone utility for fixing grid visualization issues in 3D Tetris.
    /// This script can be added to any GameObject in the scene to improve line rendering.
    /// </summary>
    public class GridVisualizationUtility : MonoBehaviour
    {
        [Header("Line Rendering Settings")]
        [SerializeField] private float lineThickness = 0.05f;
        [SerializeField] private Color lineColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        [SerializeField] private int renderQueue = 3000;
        
        [Header("Shader Options")]
        [SerializeField] private string[] shaderNames = new string[] 
        {
            "Custom/AlwaysVisibleLine",
            "Sprites/Default",
            "Universal Render Pipeline/Unlit",
            "Legacy Shaders/Transparent/Diffuse",
            "Standard"
        };
        
        [Header("Debug Options")]
        [SerializeField] private bool createTestLinesOnStart = false;

        private void Start()
        {
            if (createTestLinesOnStart)
            {
                CreateTestLines();
            }
        }

        [ContextMenu("Fix All LineRenderers In Scene")]
        public void FixAllLineRenderersInScene()
        {
            LineRenderer[] allLineRenderers = FindObjectsByType<LineRenderer>(FindObjectsSortMode.None);
            if (allLineRenderers.Length == 0)
            {
                Debug.LogWarning("No LineRenderer components found in scene.");
                return;
            }
            
            Debug.Log($"Found {allLineRenderers.Length} LineRenderer components to fix.");
            
            Material lineMaterial = CreateLineMaterial();
            ApplyMaterialToLineRenderers(allLineRenderers, lineMaterial);
        }
        
        [ContextMenu("Fix LineRenderers In Children")]
        public void FixLineRenderersInChildren()
        {
            LineRenderer[] childLineRenderers = GetComponentsInChildren<LineRenderer>(true);
            if (childLineRenderers.Length == 0)
            {
                Debug.LogWarning("No LineRenderer components found in children.");
                return;
            }
            
            Debug.Log($"Found {childLineRenderers.Length} LineRenderer components to fix.");
            
            Material lineMaterial = CreateLineMaterial();
            ApplyMaterialToLineRenderers(childLineRenderers, lineMaterial);
        }
        
        [ContextMenu("Create Test Lines")]
        public void CreateTestLines()
        {
            // Clear any existing test lines
            Transform existingTestLines = transform.Find("TestLines");
            if (existingTestLines != null)
            {
                DestroyImmediate(existingTestLines.gameObject);
            }
            
            // Create a container for test lines
            GameObject container = new GameObject("TestLines");
            container.transform.parent = transform;
            
            // Create RGB axis lines
            CreateTestLine(container.transform, Vector3.zero, Vector3.right * 10f, "X_Axis", Color.red);
            CreateTestLine(container.transform, Vector3.zero, Vector3.up * 10f, "Y_Axis", Color.green);
            CreateTestLine(container.transform, Vector3.zero, Vector3.forward * 10f, "Z_Axis", Color.blue);
            
            // Create diagonal line
            CreateTestLine(container.transform, Vector3.zero, new Vector3(5f, 5f, 5f), "Diagonal", lineColor);
            
            // Add a grid cube outline
            CreateCubeOutline(container.transform, new Vector3(2.5f, 2.5f, 2.5f), new Vector3(5f, 5f, 5f));
            
            // Fix the newly created lines
            FixLineRenderersInChildren();
            
            Debug.Log("Created test lines - check if they're visible in the Game view.");
        }
        
        private void CreateTestLine(Transform parent, Vector3 start, Vector3 end, string name, Color color)
        {
            GameObject lineObj = new GameObject(name);
            lineObj.transform.parent = parent;
            
            LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
            lineRenderer.startWidth = lineThickness;
            lineRenderer.endWidth = lineThickness;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
        
        private void CreateCubeOutline(Transform parent, Vector3 center, Vector3 size)
        {
            Vector3 halfSize = size * 0.5f;
            
            // Calculate the 8 corners of the cube
            Vector3[] corners = new Vector3[8];
            corners[0] = center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
            corners[1] = center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
            corners[2] = center + new Vector3(halfSize.x, -halfSize.y, halfSize.z);
            corners[3] = center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
            corners[4] = center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
            corners[5] = center + new Vector3(halfSize.x, halfSize.y, -halfSize.z);
            corners[6] = center + new Vector3(halfSize.x, halfSize.y, halfSize.z);
            corners[7] = center + new Vector3(-halfSize.x, halfSize.y, halfSize.z);
            
            // Create the 12 edges of the cube
            GameObject cubeObj = new GameObject("GridCube");
            cubeObj.transform.parent = parent;
            
            // Bottom face edges
            CreateCubeEdge(cubeObj.transform, corners[0], corners[1], "Edge_0_1", Color.white);
            CreateCubeEdge(cubeObj.transform, corners[1], corners[2], "Edge_1_2", Color.white);
            CreateCubeEdge(cubeObj.transform, corners[2], corners[3], "Edge_2_3", Color.white);
            CreateCubeEdge(cubeObj.transform, corners[3], corners[0], "Edge_3_0", Color.white);
            
            // Top face edges
            CreateCubeEdge(cubeObj.transform, corners[4], corners[5], "Edge_4_5", Color.white);
            CreateCubeEdge(cubeObj.transform, corners[5], corners[6], "Edge_5_6", Color.white);
            CreateCubeEdge(cubeObj.transform, corners[6], corners[7], "Edge_6_7", Color.white);
            CreateCubeEdge(cubeObj.transform, corners[7], corners[4], "Edge_7_4", Color.white);
            
            // Vertical edges
            CreateCubeEdge(cubeObj.transform, corners[0], corners[4], "Edge_0_4", Color.white);
            CreateCubeEdge(cubeObj.transform, corners[1], corners[5], "Edge_1_5", Color.white);
            CreateCubeEdge(cubeObj.transform, corners[2], corners[6], "Edge_2_6", Color.white);
            CreateCubeEdge(cubeObj.transform, corners[3], corners[7], "Edge_3_7", Color.white);
        }
        
        private void CreateCubeEdge(Transform parent, Vector3 start, Vector3 end, string name, Color color)
        {
            GameObject lineObj = new GameObject(name);
            lineObj.transform.parent = parent;
            
            LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
            lineRenderer.startWidth = lineThickness * 0.8f;
            lineRenderer.endWidth = lineThickness * 0.8f;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
        
        private Material CreateLineMaterial()
        {
            // Try each shader in the list until we find one that works
            Material material = null;
            
            foreach (string shaderName in shaderNames)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader != null)
                {
                    material = new Material(shader);
                    Debug.Log($"Using shader: {shaderName} for line rendering");
                    
                    // Set properties based on shader type
                    if (shaderName.Contains("AlwaysVisibleLine"))
                    {
                        material.SetColor("_Color", lineColor);
                        material.SetFloat("_Width", lineThickness);
                    }
                    else if (shaderName.Contains("Universal"))
                    {
                        material.SetColor("_BaseColor", lineColor);
                        material.SetFloat("_Surface", 1); // Transparent
                        material.SetFloat("_Blend", 0);   // Alpha
                        material.SetFloat("_AlphaClip", 0);
                    }
                    else
                    {
                        material.color = lineColor;
                    }
                    
                    break;
                }
            }
            
            // If no shader was found, create a default material
            if (material == null)
            {
                material = new Material(Shader.Find("Sprites/Default"));
                material.color = lineColor;
                Debug.LogWarning("No custom shader found, using default Sprites/Default shader");
            }
            
            // Configure material properties for proper line rendering
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 1); // Enable depth writing
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = renderQueue;
            
            return material;
        }
        
        private void ApplyMaterialToLineRenderers(LineRenderer[] lineRenderers, Material material)
        {
            foreach (LineRenderer lr in lineRenderers)
            {
                // Apply material and color
                lr.material = material;
                lr.startColor = lineColor;
                lr.endColor = lineColor;
                
                // Configure for better visibility
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.allowOcclusionWhenDynamic = false;
                lr.useWorldSpace = true;
                lr.generateLightingData = false;
                
                // Force refresh
                lr.enabled = false;
                lr.enabled = true;
            }
            
            Debug.Log($"Applied material fixes to {lineRenderers.Length} LineRenderer components");
        }
        
        [ContextMenu("Clear All Test Lines")]
        public void ClearAllTestLines()
        {
            Transform testLines = transform.Find("TestLines");
            if (testLines != null)
            {
                DestroyImmediate(testLines.gameObject);
                Debug.Log("Test lines cleared");
            }
            else
            {
                Debug.Log("No test lines to clear");
            }
        }
    }
}
