using UnityEngine;

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
        }

        public Vector3Int GetGridSize()
        {
            return new Vector3Int(gridWidth, gridHeight, gridDepth);
        }

        public Vector3Int WorldToGridPosition(Vector3 worldPosition)
        {
            return new Vector3Int(
                Mathf.FloorToInt(worldPosition.x),
                Mathf.FloorToInt(worldPosition.y),
                Mathf.FloorToInt(worldPosition.z)
            );
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
            pieceSpawner.SpawnRandomPiece();
        }
    }
}
