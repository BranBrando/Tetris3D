using UnityEngine;
using System.Collections.Generic; // Added for List<> and HashSet<>
// using System;

namespace TetrisGame
{
    /// <summary>
    /// Controls the behavior of a single tetromino piece
    /// </summary>
    public class Tetromino : MonoBehaviour
    {
        // Removed: [Header("Movement Settings")]
        // Removed: [SerializeField] private float quickFallMultiplier = 5f;

        [Header("Ghost Piece Settings")]
        [SerializeField] private Material ghostMaterial; // Assign a base ghost material here
        [SerializeField] private float ghostAlpha = 0.5f; // Changed default value to be semi-transparent

        // Movement and state variables
        private float fallTimer = 0f;
        private bool isActive = true;
        // Removed: private bool isQuickFalling = false;
        private GameObject ghostPieceObj;
        private GridVisualizer gridVisualizer; // Reference for shadow updates
        private PieceSpawner pieceSpawner; // Added reference for parenting ghost
        private List<Renderer> ghostBlockRenderers = new List<Renderer>(); // Added cache for ghost renderers
        private bool pendingMoveDown = false;

        // Cached component references
        private InputController inputController;
        private GameManager gameManager;

        // Reusable HashSet for ghost block visibility check
        private HashSet<Vector3Int> activeBlockGridPositions = new HashSet<Vector3Int>();

        // Shared material instance for all ghost blocks
        private Material sharedGhostMaterialInstance;


        private void Start()
        {
            // Cache component references
            gameManager = GameManager.Instance; // Get the singleton instance
            if (gameManager == null)
            {
                Debug.LogError("Tetromino could not find GameManager instance!");
                this.enabled = false; // Disable if GameManager is missing
                return;
            }

            inputController = FindFirstObjectByType<InputController>();
            if (inputController == null)
            {
                // Create a new InputController if it doesn't exist
                GameObject inputObj = new GameObject("InputController");
                inputController = inputObj.AddComponent<InputController>();
                Debug.LogWarning("Created new InputController because none was found."); // Changed to Warning
            }

            // Find the GridVisualizer (optional dependency)
            gridVisualizer = FindFirstObjectByType<GridVisualizer>();
            if (gridVisualizer == null)
            {
                Debug.LogWarning("Tetromino could not find GridVisualizer!");
            }

            // Find the PieceSpawner (optional dependency for ghost parenting)
            pieceSpawner = FindFirstObjectByType<PieceSpawner>();
            if (pieceSpawner == null)
            {
                Debug.LogWarning("Tetromino could not find PieceSpawner! Ghost piece will not be parented.");
            }

            // Subscribe to input events - using local delegates to avoid issues
            if (inputController != null)
            {
                inputController.OnMovementInput += OnMovementHandler;
                inputController.OnRotationInput += OnRotationHandler;
                // Removed: inputController.OnSpeedInput += OnSpeedHandler;
            }


            // Create and configure the shared ghost material instance
            CreateSharedGhostMaterial();

            // Create ghost piece
            CreateGhostPiece();

            // Show the ghost piece
            ShowGhostPiece(true);
        }

        private void CreateSharedGhostMaterial()
        {
            // If a ghost material is assigned in the inspector, use it as the base
            // Otherwise, try to find a standard shader or create a basic one
            Material baseMat = ghostMaterial;
            if (baseMat == null)
            {
                Shader standardShader = Shader.Find("Standard");
                if (standardShader != null)
                {
                    baseMat = new Material(standardShader);
                }
                else
                {
                    baseMat = new Material(Shader.Find("Hidden/Internal-Colored"));
                }
                 Debug.LogWarning("Tetromino: Ghost Material not assigned in Inspector. Using a default material."); // Changed to Warning
            }

            // Create the single shared instance
            sharedGhostMaterialInstance = new Material(baseMat);

            // Apply the alpha from the script variable
            Color ghostColor = Color.gray; // Start with gray, alpha will be set below
            sharedGhostMaterialInstance.color = ghostColor; // Set base color

            // --- Force Transparency Settings (Assuming URP/Lit or Standard Shader) ---
            // These properties are common for transparency on many shaders.
            try
            {
                // Set rendering mode to Transparent
                if (sharedGhostMaterialInstance.HasProperty("_Mode"))
                {
                    sharedGhostMaterialInstance.SetFloat("_Mode", 3); // 0=Opaque, 1=Cutout, 2=Fade, 3=Transparent
                }
                // Set standard blend modes
                if (sharedGhostMaterialInstance.HasProperty("_SrcBlend"))
                {
                    sharedGhostMaterialInstance.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                }
                if (sharedGhostMaterialInstance.HasProperty("_DstBlend"))
                {
                    sharedGhostMaterialInstance.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                }
                // Disable ZWrite for proper transparency sorting
                if (sharedGhostMaterialInstance.HasProperty("_ZWrite"))
                {
                    sharedGhostMaterialInstance.SetInt("_ZWrite", 0);
                }
                // Enable keywords for transparency
                sharedGhostMaterialInstance.EnableKeyword("_ALPHABLEND_ON");
                sharedGhostMaterialInstance.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                sharedGhostMaterialInstance.DisableKeyword("_ALPHATEST_ON");

                // Set render queue to Transparent + 1 to potentially help sorting
                sharedGhostMaterialInstance.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 1;

                // Set the alpha property (common names are _Color or _BaseColor)
                 if (sharedGhostMaterialInstance.HasProperty("_Color"))
                 {
                     Color matColor = sharedGhostMaterialInstance.color;
                     matColor.a = ghostAlpha;
                     sharedGhostMaterialInstance.color = matColor;
                 }
                 else if (sharedGhostMaterialInstance.HasProperty("_BaseColor")) // For URP/Lit
                 {
                     Color matColor = sharedGhostMaterialInstance.GetColor("_BaseColor");
                     matColor.a = ghostAlpha;
                     sharedGhostMaterialInstance.SetColor("_BaseColor", matColor);
                 }
                 else
                 {
                     Debug.LogWarning("Tetromino: Could not find _Color or _BaseColor property on ghost material to set alpha.");
                 }

            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Tetromino: Could not set transparency properties on shared ghost material '{sharedGhostMaterialInstance.name}'. Shader might not be compatible. Error: {ex.Message}");
            }
            // --- End Transparency Settings ---
        }


        private void OnDestroy()
        {
            // Find the InputController instance again to unsubscribe
            // Using cached reference is better
            if (inputController != null)
            {
                // Unsubscribe from events using the same delegates
                inputController.OnMovementInput -= OnMovementHandler;
                inputController.OnRotationInput -= OnRotationHandler;
                // Removed: inputController.OnSpeedInput -= OnSpeedHandler;
            }

            // Destroy ghost piece if it exists
            if (ghostPieceObj != null)
            {
                // Ensure renderers are enabled before destroying, just in case
                foreach (Renderer rend in ghostBlockRenderers)
                {
                    if (rend != null) rend.enabled = true;
                }
                Destroy(ghostPieceObj);
            }

            // Destroy the shared ghost material instance when the main piece is destroyed
            if (sharedGhostMaterialInstance != null)
            {
                Destroy(sharedGhostMaterialInstance);
            }

        } // Correct closing brace for OnDestroy

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

        // Removed: OnSpeedHandler method

        private void Update()
        {
            if (!isActive) return;

            // Check if GameManager exists (using cached reference)
            if (gameManager == null) return;

            // Check if it is game over
            if (gameManager.IsGameOver()) return;

            // Handle automatic falling
            fallTimer += Time.deltaTime;
            float currentFallTime = gameManager.GetCurrentFallTime();

            // Removed: Quick fall logic
            // if (isQuickFalling)
            // {
            //     currentFallTime /= quickFallMultiplier;
            // }

            if (fallTimer >= currentFallTime)
            {
                // Using cached reference
                if (gameManager.IsRotating)
                {
                    // If rotating, flag that a move down is pending but don't execute yet
                    pendingMoveDown = true;
                }
                else
                {
                    // If not rotating, execute MoveDown immediately
                    MoveDown();
                }
                fallTimer = 0f; // Reset timer regardless
            }

            // Check if a move down was pending and rotation has now stopped (using cached reference)
            if (pendingMoveDown && !gameManager.IsRotating)
            {
                MoveDown();
                pendingMoveDown = false; // Reset the flag
            }

            // Update ghost piece position
            UpdateGhostPiece();

            // Update ghost block visibility based on active piece overlap
            UpdateGhostBlockVisibility(); // Added call

            // Update visualizer shadows (Removed allocation)
            // UpdateVisualizerShadows(); // Removed call as method is incomplete/allocates
        }

        private void MoveDown()
        {
            transform.position += Vector3.down;
            if (!IsValidPosition())
            {
                transform.position += Vector3.up;
                isActive = false;

                // Using cached reference
                if (gameManager != null)
                {
                    gameManager.PlacePiece(this);
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
                UpdateGhostPiece(); // Ensure ghost is updated after move
                UpdateGhostBlockVisibility(); // Update visibility after move
                // UpdateVisualizerShadows(); // Removed call
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
                    UpdateGhostPiece(); // Ensure ghost is updated after rotation/kick
                    UpdateGhostBlockVisibility(); // Update visibility after rotation/kick
                    // UpdateVisualizerShadows(); // Removed call
                }
            }
            else
            {
                UpdateGhostPiece(); // Ensure ghost is updated after rotation
                UpdateGhostBlockVisibility(); // Update visibility after rotation
                // UpdateVisualizerShadows(); // Removed call
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
            // Using cached reference
            if (gameManager == null) return false;

            foreach (Transform block in transform)
            {
                // Skip inactive blocks
                if (!block.gameObject.activeSelf) continue;

                // Using cached reference
                Vector3Int gridPos = gameManager.WorldToGridPosition(block.position);
                // Using cached reference
                if (!gameManager.IsPositionValid(gridPos))
                {
                    return false;
                }
            }
            return true;
        }

        // Helper method to update the grid visualizer shadows using the ghost piece position
        // Removed allocation and call from Update as it was incomplete
        private void UpdateVisualizerShadows()
        {
            // This method was incomplete and allocated a List<Vector3> per frame.
            // Its intended functionality (interacting with GridVisualizer) is not implemented.
            // Removed the allocation and the call from Update().
            // If shadow visualization is needed, this method should be reimplemented
            // to interact with GridVisualizer efficiently (e.g., passing ghost block positions
            // to a method on GridVisualizer that handles the rendering without per-frame allocations).
        }

        // --- Added Method for Ghost Block Visibility ---
        private void UpdateGhostBlockVisibility()
        {
            // Using cached reference
            if (ghostPieceObj == null || !ghostPieceObj.activeSelf || !isActive || gameManager == null)
            {
                return; // Nothing to do if ghost isn't ready or gameManager is missing
            }

            // 1. Get grid positions of all active piece blocks (reusing the HashSet)
            activeBlockGridPositions.Clear(); // Clear the reusable HashSet
            foreach (Transform activeBlock in transform)
            {
                if (activeBlock.gameObject.activeSelf)
                {
                    // Using cached reference
                    activeBlockGridPositions.Add(gameManager.WorldToGridPosition(activeBlock.position));
                }
            }

            // 2. Iterate through cached ghost renderers and set visibility based on overlap
            foreach (Renderer ghostRenderer in ghostBlockRenderers)
            {
                if (ghostRenderer != null) // Check if renderer is valid
                {
                    // Using cached reference
                    Vector3Int ghostGridPos = gameManager.WorldToGridPosition(ghostRenderer.transform.position);

                    // Disable ghost renderer if its grid position overlaps with an active block's position
                    bool overlaps = activeBlockGridPositions.Contains(ghostGridPos);
                    ghostRenderer.enabled = !overlaps; // Use renderer.enabled instead of SetActive
                }
            }
        }
        // --- End Added Method ---


        // Removed: SetGridRotation method

        private void CreateGhostPiece()
        {
            if (ghostPieceObj != null) Destroy(ghostPieceObj);

            // Create a copy of the tetromino
            ghostPieceObj = Instantiate(gameObject, transform.position, transform.rotation);

            // Set parent to PieceSpawner if found (using cached reference)
            if (pieceSpawner != null)
            {
                ghostPieceObj.transform.SetParent(pieceSpawner.transform, true); // Keep world position
            }

            ghostPieceObj.transform.localScale = new Vector3(0.99f, 0.99f, 0.99f);

            // Clear and cache renderers, setup material
            ghostBlockRenderers.Clear(); // Clear list before populating
            foreach (Transform child in ghostPieceObj.transform)
            {
                Renderer renderer = child.GetComponent<Renderer>();
                if (renderer != null)
                {
                    ghostBlockRenderers.Add(renderer); // Add renderer to cache

                    // Assign the single shared ghost material instance
                    if (sharedGhostMaterialInstance != null)
                    {
                        renderer.material = sharedGhostMaterialInstance; // Assign the shared instance
                    }
                    else
                    {
                        Debug.LogError("Tetromino: Shared ghost material instance is null!");
                        // Fallback to creating a new material instance if the shared one failed
                        renderer.material = new Material(Shader.Find("Standard"));
                        renderer.material.color = new Color(Color.gray.r, Color.gray.g, Color.gray.b, ghostAlpha);
                    }


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
            // Skip update if the grid is currently rotating (using cached reference)
            if (gameManager != null && gameManager.IsRotating) return;

            // Using cached reference
            if (ghostPieceObj == null || !ghostPieceObj.activeSelf || !isActive || gameManager == null) return;

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
            // Using cached reference
            if (gameManager == null) return false;

            foreach (Transform block in ghostPieceObj.transform)
            {
                // Skip inactive blocks
                if (!block.gameObject.activeSelf) continue;

                // Using cached reference
                Vector3Int gridPos = gameManager.WorldToGridPosition(block.position);
                // Using cached reference
                if (!gameManager.IsPositionValid(gridPos))
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
                if (show)
                {
                    UpdateGhostPiece();
                    UpdateGhostBlockVisibility(); // Also update visibility when shown
                }
                else
                {
                    // Ensure all renderers are re-enabled when hiding the ghost object
                    foreach (Renderer rend in ghostBlockRenderers)
                    {
                        if (rend != null) rend.enabled = true;
                    }
                }
            }
        }
    }
}
