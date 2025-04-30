using UnityEngine;
using System.Collections; // Required for Coroutines
using UnityEngine.SceneManagement; // Needed for SceneManager

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
        private AudioManager audioManager; // Reference to AudioManager
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
            // Ensure AudioManager exists
            audioManager = AudioManager.Instance;
            if (audioManager == null)
            {
                audioManager = FindFirstObjectByType<AudioManager>();
                if (audioManager == null)
                {
                    GameObject audioManagerGO = new GameObject("AudioManager");
                    audioManager = audioManagerGO.AddComponent<AudioManager>();
                    Debug.Log("AudioManager instance created by GameManager.");
                }
                else
                {
                    Debug.Log("AudioManager instance found in scene by GameManager.");
                }
            }
            else
            {
                 Debug.Log("AudioManager singleton instance found by GameManager.");
            }

            // Find necessary components in the scene
            inputController = FindFirstObjectByType<InputController>();
            gridVisualizer = FindFirstObjectByType<GridVisualizer>(); // Assumes GridVisualizer exists in the scene

            // Subscribe to the grid rotation input event
            if (inputController != null)
            {
                // Changed: Subscription uses the new signature
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

            // Play background sound based on scene name
            PlaySceneBackgroundSound();
        }

        private void PlaySceneBackgroundSound()
        {
            if (AudioManager.Instance != null)
            {
                string soundName = "BGM";
                AudioManager.Instance.PlaySound(soundName); // Assumes a sound named after the scene exists
            }
            else
            {
                Debug.LogWarning("AudioManager instance not found. Cannot play scene background sound.");
            }
        }

        // Unsubscribe when the GameManager is destroyed
        private void OnDestroy()
        {
            if (inputController != null)
            {
                // Changed: Unsubscription uses the new signature
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
            float worldX = worldPosition.x;
            float worldZ = worldPosition.z;

            // Calculate Y index (unaffected by Y rotation)
            y = Mathf.FloorToInt(worldPosition.y);

            // Use Mathf.Approximately for float comparison
            if (Mathf.Approximately(currentGridRotationY, 90f))
            {
                // Rotated 90 degrees CW: World X -> Grid Z, World Z -> Grid -X
                x = gridDepth - 1 - Mathf.FloorToInt(worldZ); // Grid X depends on World Z and Grid Depth
                z = Mathf.FloorToInt(worldX);                 // Grid Z depends on World X
            }
            else if (Mathf.Approximately(currentGridRotationY, 180f))
            {
                // Rotated 180 degrees: World X -> Grid -X, World Z -> Grid -Z
                x = gridWidth - 1 - Mathf.FloorToInt(worldX);
                z = gridDepth - 1 - Mathf.FloorToInt(worldZ);
            }
            else if (Mathf.Approximately(currentGridRotationY, 270f))
            {
                // Rotated 270 degrees CW: World X -> Grid -Z, World Z -> Grid X
                x = Mathf.FloorToInt(worldZ);                 // Grid X depends on World Z
                z = gridWidth - 1 - Mathf.FloorToInt(worldX); // Grid Z depends on World X and Grid Width
            }
            else // Default to 0 degrees
            {
                // Not rotated: Standard mapping
                x = Mathf.FloorToInt(worldX);
                z = Mathf.FloorToInt(worldZ);
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
                        Debug.Log("Game over");
                        ScoreManager.Instance.CheckAndSaveBestScore(); // Check and save best score
                        return true; // Game over if top row is occupied
                    }
                }
            }
            return false; // If no blocks found in the top row, game is not over
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


        // Changed: Method signature accepts direction, calculates 90 degree rotation
        // Handles the grid rotation input event from InputController
        private void HandleGridRotation(int direction) // Accepts 1 for CW, -1 for CCW
        {
            if (isRotating) return; // Don't start a new rotation if one is in progress

            // Calculate the next target angle (90 degrees CW or CCW)
            // Add 360f before modulo to handle potential negative results correctly
            targetGridRotationY = (currentGridRotationY + (direction * 90f) + 360f) % 360f;
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
