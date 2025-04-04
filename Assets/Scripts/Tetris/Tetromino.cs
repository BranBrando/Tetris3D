using UnityEngine;
using System.Collections.Generic; // Added for List<>
// using System;

namespace TetrisGame
{
    /// <summary>
    /// Controls the behavior of a single tetromino piece
    /// </summary>
    public class Tetromino : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float quickFallMultiplier = 5f;
        
        [Header("Ghost Piece Settings")]
        [SerializeField] private Material ghostMaterial;
        [SerializeField] private float ghostAlpha = 1f; // Changed default value
        
        // Movement and state variables
        private float fallTimer = 0f;
        private bool isActive = true;
        private bool isQuickFalling = false;
        private GameObject ghostPieceObj;
        private GridVisualizer gridVisualizer; // Reference for shadow updates

        private void Start()
        {
            // Find the singleton InputController
            var inputController = FindFirstObjectByType<InputController>();
            if (inputController == null)
            {
                // Create a new InputController if it doesn't exist
                GameObject inputObj = new GameObject("InputController");
                inputController = inputObj.AddComponent<InputController>();
                Debug.Log("Created new InputController");
            }
            
            // Subscribe to input events - using local delegates to avoid issues
            inputController.OnMovementInput += OnMovementHandler;
            inputController.OnRotationInput += OnRotationHandler;
            inputController.OnSpeedInput += OnSpeedHandler;

            // Find the GridVisualizer
            gridVisualizer = FindFirstObjectByType<GridVisualizer>();
            if (gridVisualizer == null)
            {
                Debug.LogWarning("Tetromino could not find GridVisualizer!");
            }

            // Create ghost piece
            CreateGhostPiece(); // Uncommented
            // UpdateGhostPiece(); // Update is called in Update() anyway
            
            // Show the ghost piece
            ShowGhostPiece(true); // Uncommented
        }
        
        private void OnDestroy()
        {
            // Find the InputController instance again to unsubscribe
            var inputController = FindFirstObjectByType<InputController>();
            if (inputController != null)
            {
                // Unsubscribe from events using the same delegates
                inputController.OnMovementInput -= OnMovementHandler;
                inputController.OnRotationInput -= OnRotationHandler;
                inputController.OnSpeedInput -= OnSpeedHandler;
            }
            
            // Destroy ghost piece if it exists
            if (ghostPieceObj != null)
            {
                Destroy(ghostPieceObj);
            }
        }
        
        // Event handler for movement input
        private void OnMovementHandler(Vector3 direction)
        {
            if (!isActive) return;
            
            // Special case for down movement
            if (direction.y < 0)
            {
                MoveDown();
                return;
            }

            // Handle horizontal and depth movement
            Move(direction); // Reverted: Use direction directly
        }

        // Event handler for rotation input
        private void OnRotationHandler(Vector3 rotation)
        {
            if (!isActive) return;
            Rotate(rotation); // Reverted: Apply rotation directly
        }

        // Event handler for speed input
        private void OnSpeedHandler(bool isSpeedUp)
        {
            if (!isActive) return;
            isQuickFalling = isSpeedUp;
        }

        private void Update()
        {
            if (!isActive) return;
            
            // Check if GameManager exists
            if (GameManager.Instance == null) return;

            // Check if it is game over
            if (GameManager.Instance.IsGameOver()) return;

            // Handle automatic falling
            fallTimer += Time.deltaTime;
            float currentFallTime = GameManager.Instance.GetCurrentFallTime();
            
            // Apply quick fall multiplier if active
            if (isQuickFalling)
            {
                currentFallTime /= quickFallMultiplier;
            }
            
            if (fallTimer >= currentFallTime)
            {
                MoveDown();
                fallTimer = 0f;
            }
            
            // Update ghost piece position
            UpdateGhostPiece();

            // Update visualizer shadows
            UpdateVisualizerShadows();
        }

        private void MoveDown()
        {
            transform.position += Vector3.down;
            if (!IsValidPosition())
            {
                transform.position += Vector3.up;
                isActive = false;
                
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.PlacePiece(this);
                }
                
                // Hide ghost piece when tetromino is placed
                ShowGhostPiece(false);
            }
        }
        
        private void Move(Vector3 direction)
        {
            transform.position += direction;
            if (!IsValidPosition())
            {
                transform.position -= direction;
            }
            else
            {
                UpdateVisualizerShadows(); // Update shadows after successful move
            }
        }

        private void Rotate(Vector3 rotation)
        {
            // Store original rotation and position
            Quaternion originalRotation = transform.rotation;
            Vector3 originalPosition = transform.position;

            // Apply rotation
            transform.Rotate(rotation, Space.World); // Reverted: Apply rotation directly

            // Validate position after rotation
            if (!IsValidPosition())
            {
                // Try wall kicks (shifting piece if it's against a wall)
                bool validPositionFound = TryWallKicks();
                
                // If no valid position found with wall kicks, revert back to original rotation
                if (!validPositionFound)
                {
                    transform.rotation = originalRotation;
                    transform.position = originalPosition;
                }
                else
                {
                     UpdateVisualizerShadows(); // Update shadows after successful rotation/kick
                }
            }
            else
            {
                 UpdateVisualizerShadows(); // Update shadows after successful rotation
            }
        }

        private bool TryWallKicks()
        {
            // Try moving one unit in each direction to see if rotation becomes valid
            Vector3[] kickOffsets = new Vector3[] {
                // Vector3.right * 0.5f, Vector3.left * 0.5f,
                // Vector3.forward * 0.5f, Vector3.back * 0.5f,
                // Vector3.up * 0.5f, Vector3.down * 0.5f,
                
                Vector3.right, Vector3.left,
                Vector3.forward, Vector3.back,
                Vector3.up, Vector3.down,
            };
            
            foreach (Vector3 offset in kickOffsets)
            {
                transform.position += offset;
                if (IsValidPosition())
                {
                    // Found a valid position
                    return true;
                }
                // Revert offset and try next one
                transform.position -= offset;
            }
            
            // No valid position found
            return false;
        }

        public bool IsValidPosition()
        {
            if (GameManager.Instance == null) return false;
            
            foreach (Transform block in transform)
            {
                // Skip inactive blocks
                if (!block.gameObject.activeSelf) continue;
                
                Vector3Int gridPos = GameManager.Instance.WorldToGridPosition(block.position);
                if (!GameManager.Instance.IsPositionValid(gridPos))
                {
                    return false;
                }
            }
            return true;
        }

        // Helper method to update the grid visualizer shadows using the ghost piece position
        private void UpdateVisualizerShadows()
        {
            // Ensure ghost piece exists, is active, and visualizer is available
            if (ghostPieceObj != null && ghostPieceObj.activeSelf)
            {
                List<Vector3> ghostBlockWorldPositions = new List<Vector3>();
                foreach (Transform block in ghostPieceObj.transform) // Use ghostPieceObj's blocks
                {
                    // Get the world position of each active block in the ghost piece
                    if (block.gameObject.activeSelf)
                    {
                        ghostBlockWorldPositions.Add(block.position);
                    }
                }
            }
        }


        // Removed: SetGridRotation method

        private void CreateGhostPiece()
        {
            if (ghostPieceObj != null) Destroy(ghostPieceObj);
            
            // Create a copy of the tetromino
            ghostPieceObj = Instantiate(gameObject, transform.position, transform.rotation);
            
            // Setup ghost material
            foreach (Transform child in ghostPieceObj.transform)
            {
                Renderer renderer = child.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Determine the base material (either from Inspector or original renderer)
                    Material baseMat = (ghostMaterial != null) ? ghostMaterial : renderer.material;
                    
                    // ALWAYS create a new instance for the ghost
                    Material ghostInstanceMat = new Material(baseMat); 
                    
                    // Apply the alpha from the script variable
                    Color ghostColor = ghostInstanceMat.color;
                    ghostColor.a = ghostAlpha;
                    ghostInstanceMat.color = ghostColor;

                    // --- Force Transparency Settings (Assuming URP/Lit Shader) ---
                    // If using a different shader, these property names might need changing.
                    try
                    {
                        // Set Surface Type to Transparent (1.0f for URP/Lit)
                        if (ghostInstanceMat.HasProperty("_Surface"))
                        {
                            ghostInstanceMat.SetFloat("_Surface", 1.0f);
                        }
                        // Set Blend Mode to Alpha (0.0f for URP/Lit)
                        if (ghostInstanceMat.HasProperty("_Blend"))
                        {
                             ghostInstanceMat.SetFloat("_Blend", 0.0f);
                        }
                        // Set standard blend modes
                        if (ghostInstanceMat.HasProperty("_SrcBlend"))
                        {
                            ghostInstanceMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        }
                        if (ghostInstanceMat.HasProperty("_DstBlend"))
                        {
                             ghostInstanceMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        }
                        // Disable ZWrite for proper transparency sorting
                        if (ghostInstanceMat.HasProperty("_ZWrite"))
                        {
                             ghostInstanceMat.SetInt("_ZWrite", 0);
                        }
                        // Set render queue to Transparent
                        ghostInstanceMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Tetromino: Could not set URP transparency properties on ghost material '{ghostInstanceMat.name}'. Shader might not be URP/Lit or compatible. Error: {ex.Message}", child.gameObject);
                    }
                    // --- End Transparency Settings ---

                    // Assign the new instance with correct alpha and transparency settings
                    renderer.material = ghostInstanceMat;

                    // Disable colliders if they exist
                    Collider collider = child.GetComponent<Collider>();
                    if (collider != null) collider.enabled = false;
                }
            }
            
            // Initially hide ghost
            ghostPieceObj.SetActive(false);

            // Remove Tetromino component from ghost AFTER setup
            Destroy(ghostPieceObj.GetComponent<Tetromino>());
        }
        
        private void UpdateGhostPiece()
        {
            if (ghostPieceObj == null || !ghostPieceObj.activeSelf || !isActive || GameManager.Instance == null) return;
            
            // Match position and rotation of real piece
            ghostPieceObj.transform.position = transform.position;
            ghostPieceObj.transform.rotation = transform.rotation;
            
            // Drop the ghost piece as far as it will go
            while (IsGhostPositionValid())
            {
                ghostPieceObj.transform.position += Vector3.down;
            }
            
            // Move back up one unit (the last position was invalid)
            ghostPieceObj.transform.position += Vector3.up;
        }
        
        private bool IsGhostPositionValid()
        {
            if (GameManager.Instance == null) return false;
            
            foreach (Transform block in ghostPieceObj.transform)
            {
                // Skip inactive blocks
                if (!block.gameObject.activeSelf) continue;
                
                Vector3Int gridPos = GameManager.Instance.WorldToGridPosition(block.position);
                if (!GameManager.Instance.IsPositionValid(gridPos))
                {
                    return false;
                }
            }
            return true;
        }
        
        // Preview the shadow (ghost piece) of where the tetromino will land
        public void ShowGhostPiece(bool show)
        {
            if (ghostPieceObj != null)
            {
                ghostPieceObj.SetActive(show);
                if (show) UpdateGhostPiece();
            }
        }
    }
}
