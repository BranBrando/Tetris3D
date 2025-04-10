using System;
using UnityEngine;

namespace TetrisGame
{
    /// <summary>
    /// Handles user input for the 3D Tetris game.
    /// This controller abstracts input handling from the game logic.
    /// </summary>
    public class InputController : MonoBehaviour
    {
        // Add reference to GameManager
        private GameManager gameManager;

        [Header("Movement Keys")]
        [SerializeField] private KeyCode moveLeftKey = KeyCode.A;
        [SerializeField] private KeyCode moveRightKey = KeyCode.D;
        [SerializeField] private KeyCode moveForwardKey = KeyCode.W;
        [SerializeField] private KeyCode moveBackwardKey = KeyCode.S;
        // Changed: Default value to Space
        [SerializeField] private KeyCode moveDownKey = KeyCode.Space;
        
        [Header("Rotation Keys")]
        [SerializeField] private KeyCode rotateXKey = KeyCode.J;
        [SerializeField] private KeyCode rotateYKey = KeyCode.K;
        [SerializeField] private KeyCode rotateZKey = KeyCode.L;

        // Removed: [Header("Grid Rotation Key")]
        // Removed: public KeyCode rotateGridKey = KeyCode.Space;

        // Added: New Grid Rotation Keys
        [Header("Grid Rotation Keys")]
        [SerializeField] private KeyCode rotateGridCWKey = KeyCode.Q; // Clockwise
        [SerializeField] private KeyCode rotateGridCCWKey = KeyCode.E; // Counter-Clockwise

        // Removed: [Header("Quick Fall Key")]
        // Removed: [SerializeField] private KeyCode quickFallKey = KeyCode.Space;

        [Header("Sensitivity")]
        [SerializeField] private float inputDelayTime = 0.1f;
        
        // Events that tetromino objects can subscribe to
        public event Action<Vector3> OnMovementInput;
        public event Action<Vector3> OnRotationInput;
        // Removed: public event Action<bool> OnSpeedInput;
        // Changed: Event signature to pass direction
        public event Action<int> OnGridRotateInput; // Event for grid rotation (1 for CW, -1 for CCW)

        // Timer variables for input delay
        private float horizontalTimer = 0f;
        private float verticalTimer = 0f;
        private float depthTimer = 0f;
        private float rotationTimer = 0f;

        // Find GameManager in Start
        private void Start()
        {
            gameManager = GameManager.Instance; // Get the singleton instance
            if (gameManager == null)
            {
                Debug.LogError("InputController could not find GameManager instance!");
            }
        }
        
        private void Update()
        {
            // Removed: HandleSpeedInput();
            HandleGridRotationInput();

            // Check if the grid is currently rotating
            if (gameManager != null && gameManager.IsRotating)
            {
                // If rotating, skip piece movement and piece rotation inputs
                return; // Exit before calling HandleMovementInput and HandleRotationInput
            }

            // If not rotating, handle piece movement and rotation as normal
            HandleMovementInput();
            HandleRotationInput();
        }

        // Changed: Method logic for Q/E keys and direction
        private void HandleGridRotationInput()
        {
            int direction = 0;
            if (Input.GetKeyDown(rotateGridCWKey)) // Q key
            {
                direction = 1; // Clockwise
            }
            else if (Input.GetKeyDown(rotateGridCCWKey)) // E key
            {
                direction = -1; // Counter-Clockwise
            }

            if (direction != 0)
            {
                OnGridRotateInput?.Invoke(direction);
            }
        }

        private void HandleMovementInput()
        {
            horizontalTimer += Time.deltaTime;
            verticalTimer += Time.deltaTime;
            depthTimer += Time.deltaTime;
            
            Vector3 movementDirection = Vector3.zero;
            
            // Horizontal movement (X axis)
            if (Input.GetKey(moveLeftKey) && horizontalTimer >= inputDelayTime)
            {
                movementDirection += Vector3.left;
                horizontalTimer = 0f;
            }
            else if (Input.GetKey(moveRightKey) && horizontalTimer >= inputDelayTime)
            {
                movementDirection += Vector3.right;
                horizontalTimer = 0f;
            }
            
            // Vertical movement (Y axis)
            if (Input.GetKey(moveDownKey) && verticalTimer >= inputDelayTime)
            {
                movementDirection += Vector3.down;
                verticalTimer = 0f;
            }
            
            // Depth movement (Z axis)
            if (Input.GetKey(moveForwardKey) && depthTimer >= inputDelayTime)
            {
                movementDirection += Vector3.forward;
                depthTimer = 0f;
            }
            else if (Input.GetKey(moveBackwardKey) && depthTimer >= inputDelayTime)
            {
                movementDirection += Vector3.back;
                depthTimer = 0f;
            }
            
            // Trigger movement event if there's any movement
            if (movementDirection != Vector3.zero)
            {
                OnMovementInput?.Invoke(movementDirection);
            }
        }
        
        private void HandleRotationInput()
        {
            rotationTimer += Time.deltaTime;
            
            if (rotationTimer < inputDelayTime)
                return;
                
            Vector3 rotationDirection = Vector3.zero;
            
            // X axis rotation
            if (Input.GetKey(rotateXKey))
            {
                rotationDirection = new Vector3(90f, 0f, 0f);
                rotationTimer = 0f;
            }
            // Y axis rotation
            else if (Input.GetKey(rotateYKey))
            {
                rotationDirection = new Vector3(0f, 90f, 0f);
                rotationTimer = 0f;
            }
            // Z axis rotation
            else if (Input.GetKey(rotateZKey))
            {
                rotationDirection = new Vector3(0f, 0f, 90f);
                rotationTimer = 0f;
            }
            
            // Trigger rotation event if there's any rotation
            if (rotationDirection != Vector3.zero)
            {
                OnRotationInput?.Invoke(rotationDirection);
            }
        }

        // Removed: HandleSpeedInput() method
        
        // Reset input state (useful when pausing or changing scenes)
        public void ResetInputState()
        {
            horizontalTimer = 0f;
            verticalTimer = 0f;
            depthTimer = 0f;
            rotationTimer = 0f;
        }
        
        // Clear all subscribed listeners (useful when destroying tetromino objects)
        public void ClearAllListeners()
        {
            OnMovementInput = null;
            OnRotationInput = null;
            // Removed: OnSpeedInput = null;
            // Changed: Comment reflects event signature change
            OnGridRotateInput = null; // Clear grid rotation listeners too (now Action<int>)
        }
    }
}
