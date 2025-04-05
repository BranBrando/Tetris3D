using UnityEngine;
using System.Collections; // Required for Coroutines

namespace TetrisGame
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public GameObject VFXGround;
        [SerializeField] private int gridWidth = 3;
        [SerializeField] private int gridHeight = 10;
        [SerializeField] private int gridDepth = 3;

        private GridSystem gridSystem;
        private PieceSpawner pieceSpawner;
        private InputController inputController; // Reference to InputController
        private GridVisualizer gridVisualizer; // Reference to GridVisualizer
        // Removed: private Tetromino activeTetromino;
        private float currentGridRotationY = 0f; // Current visual rotation state of the grid
        private float targetGridRotationY = 0f; // Target rotation state
        private bool isRotating = false; // Flag to prevent concurrent rotations
        [SerializeField] private float rotationDuration = 0.5f; // Duration for smooth rotation

        // Public property to check if the grid is currently rotating
        public bool IsRotating => isRotating;

        private float baseFallTime = 3f;
        private float currentFallTime;

        private float gridOffset = 0.5f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
            }
        }

        private void Start()
        {
            // Find necessary components in the scene
            inputController = FindFirstObjectByType<InputController>();
            gridVisualizer = FindFirstObjectByType<GridVisualizer>(); // Assumes GridVisualizer exists in the scene

            // Subscribe to the grid rotation input event
            if (inputController != null)
            {
                inputController.OnGridRotateInput += HandleGridRotation;
            }
            else
            {
                Debug.LogError("GameManager could not find InputController!");
            }

            gridSystem = new GameObject("GridSystem").AddComponent<GridSystem>();
            gridSystem.Initialize(gridWidth, gridHeight, gridDepth);
            gridSystem.transform.position = Vector3.zero;
            // gridSystem.transform.position = new Vector3(-5, gridHeight, -5);
            
            pieceSpawner = new GameObject("PieceSpawner").AddComponent<PieceSpawner>();
            pieceSpawner.transform.parent = gridSystem.transform;
            pieceSpawner.transform.position = new Vector3(
                (gridWidth % 2 == 0) ? (gridWidth / 2f - 0.5f) : (gridWidth / 2f),
                gridHeight + 0.5f,
                (gridDepth % 2 == 0) ? (gridDepth / 2f - 0.5f) : (gridDepth / 2f)
            );
            
            // VFXGround.transform.position = new Vector3(
            //     (gridWidth % 2 == 0) ? (gridWidth / 2f - 0.5f) : (gridWidth / 2f),
            //     0,
            //     (gridDepth % 2 == 0) ? (gridDepth / 2f - 0.5f) : (gridDepth / 2f)
            // );

            // Create and set up ScoreManager
            new GameObject("ScoreManager").AddComponent<ScoreManager>();
            
            // Note: CameraController and GridVisualizer should be added manually in the Unity Editor
            // or after all scripts are compiled.

            currentFallTime = baseFallTime;

            // Initial piece spawn
            // SpawnNewPiece();
        }

        // Unsubscribe when the GameManager is destroyed
        private void OnDestroy()
        {
            if (inputController != null)
            {
                inputController.OnGridRotateInput -= HandleGridRotation;
            }
            // Prevent memory leaks if this instance was the singleton
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public Vector3Int GetGridSize()
        {
            return new Vector3Int(gridWidth, gridHeight, gridDepth);
        }

        public Vector3Int WorldToGridPosition(Vector3 worldPosition)
        {
            int x, y, z;

            // Calculate Y index (unaffected by Y rotation)
            y = Mathf.FloorToInt(worldPosition.y);

            // Check if grid is rotated (allow for small floating point inaccuracies)
            if (Mathf.Abs(currentGridRotationY - 180f) < 0.1f)
            {
                // Rotated 180 degrees: Map world coordinates to opposite side of the grid array
                // Assuming grid pivot is at (0,0,0) world space for index calculation
                x = gridWidth - 1 - Mathf.FloorToInt(worldPosition.x);
                z = gridDepth - 1 - Mathf.FloorToInt(worldPosition.z);
            }
            else
            {
                // Not rotated (or close to 0): Standard mapping
                x = Mathf.FloorToInt(worldPosition.x);
                z = Mathf.FloorToInt(worldPosition.z);
            }

            return new Vector3Int(x, y, z);
        }

        public bool IsPositionValid(Vector3Int gridPosition)
        {
            return gridSystem.IsPositionValid(gridPosition);
            // return gridPosition.x >= 0 && gridPosition.x < gridWidth &&
            //     gridPosition.y >= 0 && gridPosition.y < gridHeight &&
            //     gridPosition.z >= 0 && gridPosition.z < gridDepth;
        }

        public void IncreaseSpeed()
        {
            currentFallTime = Mathf.Max(0.1f, baseFallTime - (ScoreManager.Instance.GetLevel() * 0.1f));
        }

        public float GetCurrentFallTime() 
        {
            return currentFallTime;
        }

        public void PlacePiece(Tetromino piece)
        {
            foreach (Transform block in piece.transform)
            {
                Vector3Int gridPos = WorldToGridPosition(block.position);
                gridSystem.StoreBlock(gridPos, block);
            }
            
            CheckForCompletedPlanes();
            if(!IsGameOver())
            {
                SpawnNewPiece();
            }
        }

        private void CheckForCompletedPlanes()
        {
            int planesCleared = 0;
            
            // Check XZ planes (horizontal layers)
            for (int y = 0; y < gridHeight; y++)
            {
                if (gridSystem.CheckXZPlane(y))
                {
                    gridSystem.ClearXZPlane(y);
                    gridSystem.ShiftDownAbove(y);
                    y--; // Recheck the same layer after shifting
                    planesCleared++;
                }
            }
            
            // Check XY planes (depth layers)
            for (int z = 0; z < gridDepth; z++)
            {
                if (gridSystem.CheckXYPlane(z))
                {
                    gridSystem.ClearXYPlane(z);
                    gridSystem.ShiftForwardBehind(z);
                    z--; // Recheck the same layer after shifting
                    planesCleared++;
                }
            }
            
            // Check YZ planes (width layers)
            for (int x = 0; x < gridWidth; x++)
            {
                if (gridSystem.CheckYZPlane(x))
                {
                    gridSystem.ClearYZPlane(x);
                    gridSystem.ShiftRightToLeft(x);
                    x--; // Recheck the same layer after shifting
                    planesCleared++;
                }
            }

            if (planesCleared > 0)
            {
                ScoreManager.Instance.AddScore(planesCleared);
            }
        }

        public bool IsGameOver()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridDepth; z++)
                {
                    if (gridSystem.IsPositionOccupied(new Vector3Int(x, gridHeight - 1, z)))
                    {
                        Debug.LogWarning("Game over");
                        return true;
                    }
                }
            }
            return false;
        }

        private void SpawnNewPiece()
        {
            if (pieceSpawner == null)
            {
                Debug.LogError("PieceSpawner is null in GameManager. Cannot spawn piece.");
                return;
            }
            pieceSpawner.SpawnRandomPiece(); // Still need to call spawn

        }

        // Handles the grid rotation input event from InputController
        private void HandleGridRotation()
        {
            if (isRotating) return; // Don't start a new rotation if one is in progress

            targetGridRotationY = (currentGridRotationY + 180f) % 360f; // Calculate the next target angle
            StartCoroutine(SmoothRotateGrid(targetGridRotationY));
        }

        // Coroutine for smooth rotation
        private IEnumerator SmoothRotateGrid(float targetAngleY)
        {
            isRotating = true;
            float timeElapsed = 0f;
            Quaternion startRotation = Quaternion.Euler(0, currentGridRotationY, 0);
            Quaternion targetRotation = Quaternion.Euler(0, targetAngleY, 0);

            Debug.Log($"Starting smooth rotation from {currentGridRotationY} to {targetAngleY}");

            while (timeElapsed < rotationDuration)
            {
                float t = timeElapsed / rotationDuration;
                // Optional: Add easing (e.g., SmoothStep)
                // t = t * t * (3f - 2f * t);

                Quaternion currentRotation = Quaternion.Slerp(startRotation, targetRotation, t);

                // Apply rotation to visualizer (which parents lines) and spawner
                if (gridVisualizer != null)
                {
                    gridVisualizer.transform.rotation = currentRotation; // Rotate the main visualizer transform
                }
                else
                {
                    Debug.LogWarning("GameManager: GridVisualizer reference is missing during rotation.");
                }

                // Add back PieceSpawner rotation update
                if (pieceSpawner != null)
                {
                    pieceSpawner.transform.rotation = currentRotation;
                }
                else
                {
                    Debug.LogWarning("GameManager: PieceSpawner reference is missing during rotation.");
                }

                timeElapsed += Time.deltaTime;
                yield return null; // Wait for the next frame
            }

            // Ensure final rotation is exact for the visualizer and spawner
            Quaternion finalRotation = Quaternion.Euler(0, targetAngleY, 0);
             if (gridVisualizer != null) gridVisualizer.transform.rotation = finalRotation; // Rotate main visualizer transform
             if (pieceSpawner != null) pieceSpawner.transform.rotation = finalRotation;

            currentGridRotationY = targetAngleY; // Update the current state
            isRotating = false;
            Debug.Log($"Smooth rotation finished at {currentGridRotationY}");

            // Optional: Camera adjustment if needed
            // CameraController cameraController = FindObjectOfType<CameraController>();
            // if (cameraController != null) cameraController.AdjustForGridRotation(currentGridRotationY);
        }
    }
}
