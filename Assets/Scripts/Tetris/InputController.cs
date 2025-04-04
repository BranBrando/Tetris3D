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
        [Header("Movement Keys")]
        [SerializeField] private KeyCode moveLeftKey = KeyCode.A;
        [SerializeField] private KeyCode moveRightKey = KeyCode.D;
        [SerializeField] private KeyCode moveForwardKey = KeyCode.W;
        [SerializeField] private KeyCode moveBackwardKey = KeyCode.S;
        [SerializeField] private KeyCode moveDownKey = KeyCode.DownArrow;
        
        [Header("Rotation Keys")]
        [SerializeField] private KeyCode rotateXKey = KeyCode.J;
        [SerializeField] private KeyCode rotateYKey = KeyCode.K;
        [SerializeField] private KeyCode rotateZKey = KeyCode.L;

        [Header("Grid Rotation Key")]
        public KeyCode rotateGridKey = KeyCode.Space; // Public for potential external access/config

        [Header("Quick Fall Key")]
        [SerializeField] private KeyCode quickFallKey = KeyCode.LeftShift; // Changed from Space

        [Header("Sensitivity")]
        [SerializeField] private float inputDelayTime = 0.1f;
        
        // Events that tetromino objects can subscribe to
        public event Action<Vector3> OnMovementInput;
        public event Action<Vector3> OnRotationInput;
        public event Action<bool> OnSpeedInput;
        public event Action OnGridRotateInput; // Event for grid rotation

        // Timer variables for input delay
        private float horizontalTimer = 0f;
        private float verticalTimer = 0f;
        private float depthTimer = 0f;
        private float rotationTimer = 0f;
        
        private void Update()
        {
            HandleMovementInput();
            HandleRotationInput();
            HandleSpeedInput();
            HandleGridRotationInput(); // Call the new handler
        }

        private void HandleGridRotationInput()
        {
            if (Input.GetKeyDown(rotateGridKey))
            {
                OnGridRotateInput?.Invoke();
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
        private void HandleSpeedInput()
        {
            // Check if quick fall key is being pressed (Using the updated key)
            bool isQuickFalling = Input.GetKey(quickFallKey); // Now checks LeftShift by default

            // Trigger speed event with the current quick fall state
            OnSpeedInput?.Invoke(isQuickFalling);
        }
        
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
            OnSpeedInput = null;
            OnGridRotateInput = null; // Clear grid rotation listeners too
        }
    }
}
