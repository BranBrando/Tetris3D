using UnityEngine;
using UnityEditor;

namespace TetrisGame.Editor
{
    /// <summary>
    /// Custom editor for GridVisualizer to provide debugging and visualization options
    /// </summary>
    [CustomEditor(typeof(GridVisualizer))]
    public class GridVisualizerEditor : UnityEditor.Editor
    {
        // Transparency slider value
        private float transparencyValue = 0.8f;
        private bool showDebugOptions = false;
        private bool testLinesAdded = false;
        private Color testLineColor = Color.red;
        
        public override void OnInspectorGUI()
        {
            // Draw the default inspector
            DrawDefaultInspector();
            
            EditorGUILayout.Space(10);
            
            // Get the target GridVisualizer
            GridVisualizer gridVisualizer = (GridVisualizer)target;
            
            // Transparency slider
            EditorGUILayout.LabelField("Grid Visualization Options", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            transparencyValue = EditorGUILayout.Slider("Grid Transparency", transparencyValue, 0.0f, 1.0f);
            if (EditorGUI.EndChangeCheck())
            {
                // Apply transparency when slider changes
                if (Application.isPlaying)
                {
                    gridVisualizer.SetGridTransparency(transparencyValue);
                }
            }
            
            // Toggle grid visibility
            if (GUILayout.Button("Toggle Grid Visibility"))
            {
                if (Application.isPlaying)
                {
                    // Find the grid container
                    Transform gridContainer = FindGridContainer(gridVisualizer.transform);
                    if (gridContainer != null)
                    {
                        gridContainer.gameObject.SetActive(!gridContainer.gameObject.activeSelf);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Play the game to use this feature.", MessageType.Info);
                }
            }
            
            // Debugging options
            EditorGUILayout.Space(10);
            showDebugOptions = EditorGUILayout.Foldout(showDebugOptions, "Debug Options");
            if (showDebugOptions)
            {
                if (GUILayout.Button("Add Test Lines"))
                {
                    AddTestLines(gridVisualizer);
                    testLinesAdded = true;
                }
                
                if (testLinesAdded)
                {
                    testLineColor = EditorGUILayout.ColorField("Test Line Color", testLineColor);
                    if (GUILayout.Button("Update Test Line Color"))
                    {
                        UpdateTestLineColors(gridVisualizer, testLineColor);
                    }
                }
                
                if (GUILayout.Button("Find Line Renderers"))
                {
                    CountLineRenderers(gridVisualizer);
                }
                
                if (GUILayout.Button("Fix Material Settings"))
                {
                    FixMaterialSettings(gridVisualizer);
                }
                
                if (GUILayout.Button("Check Camera Setup"))
                {
                    CheckCameraSetup();
                }
            }
            
            // Helpful information for troubleshooting
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Troubleshooting Tips", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("If grid lines aren't visible, try these steps:\n" +
                                   "1. Make sure your camera is positioned to see the grid\n" +
                                   "2. Check that the LineRenderer materials are using the right shader\n" +
                                   "3. Add a LineRendererMaterialFixer component to this object\n" +
                                   "4. Try different shaders like 'Sprites/Default' or 'Universal Render Pipeline/Unlit'", 
                                   MessageType.Info);
        }
        
        private Transform FindGridContainer(Transform parent)
        {
            return parent.Find("GridVisualization");
        }
        
        private void AddTestLines(GridVisualizer gridVisualizer)
        {
            GameObject testObject = new GameObject("DebugTestLines");
            testObject.transform.parent = gridVisualizer.transform;
            
            // Add test lines in three axes
            AddTestLine(testObject.transform, Vector3.zero, Vector3.right * 10, "TestLine_X", Color.red);
            AddTestLine(testObject.transform, Vector3.zero, Vector3.up * 10, "TestLine_Y", Color.green);
            AddTestLine(testObject.transform, Vector3.zero, Vector3.forward * 10, "TestLine_Z", Color.blue);
            AddTestLine(testObject.transform, Vector3.zero, new Vector3(10, 10, 10), "TestLine_Diagonal", testLineColor);
            
            // Show notification
            EditorUtility.DisplayDialog("Test Lines Added", 
                "Test lines have been added as a child of the GridVisualizer. " +
                "If you can see these lines but not the grid, the issue is likely with the grid generation logic, not the line rendering.", 
                "OK");
        }
        
        private void AddTestLine(Transform parent, Vector3 start, Vector3 end, string name, Color color)
        {
            GameObject lineObj = new GameObject(name);
            lineObj.transform.parent = parent;
            
            LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            
            // Create material
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.material.color = color;
            
            // Configure for visibility
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.useWorldSpace = true;
        }
        
        private void UpdateTestLineColors(GridVisualizer gridVisualizer, Color color)
        {
            Transform testLines = gridVisualizer.transform.Find("DebugTestLines");
            if (testLines != null)
            {
                LineRenderer[] lineRenderers = testLines.GetComponentsInChildren<LineRenderer>();
                foreach (LineRenderer lr in lineRenderers)
                {
                    if (lr.name == "TestLine_Diagonal")
                    {
                        lr.startColor = color;
                        lr.endColor = color;
                        if (lr.material != null)
                            lr.material.color = color;
                    }
                }
            }
        }
        
        private void CountLineRenderers(GridVisualizer gridVisualizer)
        {
            // Count in grid container
            Transform gridContainer = FindGridContainer(gridVisualizer.transform);
            int gridLines = 0;
            
            if (gridContainer != null)
            {
                LineRenderer[] lineRenderers = gridContainer.GetComponentsInChildren<LineRenderer>(true);
                gridLines = lineRenderers.Length;
            }
            
            // Count total including test lines
            LineRenderer[] allLineRenderers = gridVisualizer.GetComponentsInChildren<LineRenderer>(true);
            
            EditorUtility.DisplayDialog("Line Renderer Count", 
                $"Found {gridLines} line renderers in the grid container.\n" +
                $"Found {allLineRenderers.Length} total line renderers in all children.\n\n" +
                (gridLines == 0 ? "No grid lines found! Check if CreateGridVisualization() is being called." : ""),
                "OK");
        }
        
        private void FixMaterialSettings(GridVisualizer gridVisualizer)
        {
            // Find a LineRendererMaterialFixer or add one
            LineRendererMaterialFixer fixer = gridVisualizer.GetComponent<LineRendererMaterialFixer>();
            if (fixer == null)
            {
                fixer = gridVisualizer.gameObject.AddComponent<LineRendererMaterialFixer>();
                EditorUtility.DisplayDialog("Added Component", 
                    "LineRendererMaterialFixer component was added to the GridVisualizer.\n" +
                    "This will attempt to fix any material issues with the line renderers.", 
                    "OK");
            }
            else
            {
                // Call the fix method
                fixer.RefreshLines();
                EditorUtility.DisplayDialog("Fixed Materials", 
                    "Material settings have been updated on all line renderers.", 
                    "OK");
            }
        }
        
        private void CheckCameraSetup()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                EditorUtility.DisplayDialog("Camera Issue", 
                    "No main camera found! Make sure you have a camera tagged as 'MainCamera'.", 
                    "OK");
                return;
            }
            
            string cameraInfo = $"Camera position: {mainCamera.transform.position}\n" +
                               $"Camera rotation: {mainCamera.transform.rotation.eulerAngles}\n" +
                               $"Rendering path: {mainCamera.renderingPath}\n" +
                               $"Clear flags: {mainCamera.clearFlags}\n" +
                               $"Using URP: {IsUsingURP()}";
            
            EditorUtility.DisplayDialog("Camera Setup", cameraInfo, "OK");
        }
        
        private bool IsUsingURP()
        {
            return UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null && 
                   UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("Universal");
        }
    }
}
