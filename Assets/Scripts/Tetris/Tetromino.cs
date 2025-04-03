using UnityEngine;
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
        [SerializeField] private float ghostAlpha = 0.3f;
        
        // Movement and state variables
        private float fallTimer = 0f;
        private bool isActive = true;
        private bool isQuickFalling = false;
        private GameObject ghostPieceObj;

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
            
            // Create ghost piece
            // CreateGhostPiece();
            // UpdateGhostPiece();
            
            // Show the ghost piece
            // ShowGhostPiece(true);
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
            Move(direction);
        }
        
        // Event handler for rotation input
        private void OnRotationHandler(Vector3 rotation)
        {
            if (!isActive) return;
            Rotate(rotation);
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
        }
        
        private void Rotate(Vector3 rotation)
        {
            // Store original rotation and position
            Quaternion originalRotation = transform.rotation;
            Vector3 originalPosition = transform.position;
            
            // Apply rotation
            transform.Rotate(rotation, Space.World);
            
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
        
        private void CreateGhostPiece()
        {
            if (ghostPieceObj != null) Destroy(ghostPieceObj);
            
            // Create a copy of the tetromino
            ghostPieceObj = Instantiate(gameObject, transform.position, transform.rotation);
            
            // Remove Tetromino component from ghost
            Destroy(ghostPieceObj.GetComponent<Tetromino>());
            
            // Setup ghost material
            foreach (Transform child in ghostPieceObj.transform)
            {
                Renderer renderer = child.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Create ghost material if not provided
                    if (ghostMaterial == null)
                    {
                        ghostMaterial = new Material(renderer.material);
                        Color ghostColor = ghostMaterial.color;
                        ghostColor.a = ghostAlpha;
                        ghostMaterial.color = ghostColor;
                    }
                    
                    renderer.material = ghostMaterial;
                    
                    // Disable colliders if they exist
                    Collider collider = child.GetComponent<Collider>();
                    if (collider != null) collider.enabled = false;
                }
            }
            
            // Initially hide ghost
            ghostPieceObj.SetActive(false);
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
