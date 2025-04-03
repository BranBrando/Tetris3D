using UnityEngine;

namespace TetrisGame
{
    /// <summary>
    /// Controls the camera movement and rotation for the 3D Tetris game.
    /// Allows the player to rotate around and zoom in/out from the game grid.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Camera Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = Vector3.zero;
        
        [Header("Camera Movement")]
        [SerializeField] private float orbitSpeed = 100f;
        [SerializeField] private float zoomSpeed = 500f;
        [SerializeField] private float minDistance = 5f;
        [SerializeField] private float maxDistance = 20f;
        [SerializeField] private float smoothTime = 0.3f;
        
        [Header("Controls")]
        [SerializeField] private KeyCode orbitKey = KeyCode.Mouse1; // Right mouse button
        [SerializeField] private bool invertOrbit = false;
        [SerializeField] private bool invertZoom = false;
        
        private float currentDistance;
        private Vector3 currentVelocity = Vector3.zero;
        private Vector3 targetPosition;
        
        private void Start()
        {
            if (target == null)
            {
                // Try to find the GridSystem in the scene
                GridSystem gridSystem = FindFirstObjectByType<GridSystem>();
                if (gridSystem != null)
                {
                    target = gridSystem.transform;
                    
                    // Adjust target position based on grid size
                    if (GameManager.Instance != null)
                    {
                        Vector3Int gridSize = GameManager.Instance.GetGridSize();
                        targetOffset = new Vector3(gridSize.x / 2f, gridSize.y / 2f, gridSize.z / 2f);
                    }
                }
                else
                {
                    Debug.LogWarning("Camera target not assigned and no GridSystem found. Using world origin.");
                    GameObject targetObj = new GameObject("CameraTarget");
                    targetObj.transform.position = new Vector3(0, 3, 0);
                    target = targetObj.transform;
                }
            }
            
            // Calculate initial distance based on current position
            currentDistance = Vector3.Distance(transform.position, target.position + targetOffset);
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
        }
        
        private void LateUpdate()
        {
            if (target == null) return;
            
            // Update target position with offset
            targetPosition = target.position + targetOffset;
            
            // Handle orbit around target
            if (Input.GetKey(orbitKey))
            {
                float horizontalInput = Input.GetAxis("Mouse X") * (invertOrbit ? -1 : 1);
                float verticalInput = Input.GetAxis("Mouse Y") * (invertOrbit ? -1 : 1);
                
                transform.RotateAround(targetPosition, Vector3.up, horizontalInput * orbitSpeed * Time.deltaTime);
                transform.RotateAround(targetPosition, transform.right, -verticalInput * orbitSpeed * Time.deltaTime);
            }
            
            // Handle zoom with mouse wheel
            float zoomInput = Input.GetAxis("Mouse ScrollWheel") * (invertZoom ? -1 : 1);
            currentDistance -= zoomInput * zoomSpeed * Time.deltaTime;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
            
            // Calculate desired camera position
            Vector3 direction = (transform.position - targetPosition).normalized;
            Vector3 desiredPosition = targetPosition + direction * currentDistance;
            
            // Smoothly move camera
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);
            
            // Always look at target
            transform.LookAt(targetPosition);
        }
        
        /// <summary>
        /// Sets a new target for the camera to orbit around.
        /// </summary>
        public void SetTarget(Transform newTarget, Vector3 offset)
        {
            target = newTarget;
            targetOffset = offset;
        }
        
        /// <summary>
        /// Changes the zoom level of the camera.
        /// </summary>
        public void SetZoom(float zoomLevel)
        {
            // zoomLevel should be between 0 (minDistance) and 1 (maxDistance)
            zoomLevel = Mathf.Clamp01(zoomLevel);
            currentDistance = Mathf.Lerp(minDistance, maxDistance, zoomLevel);
        }
    }
}
