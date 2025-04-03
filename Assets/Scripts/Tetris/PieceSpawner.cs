using UnityEngine;

namespace TetrisGame
{
    public class PieceSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject[] tetrominoPrefabs;
        private Vector3 spawnPosition;

        private GameObject fallingPiece;

        private void Start()
        {
            // Get spawn position from GameObject transform
            spawnPosition = transform.position;

            // Create 3D tetromino prefabs if none exist
            if (tetrominoPrefabs == null || tetrominoPrefabs.Length == 0)
            {
                CreateDefaultTetrominoPrefabs();
            }

            SpawnRandomPiece();
        }

        private void CreateDefaultTetrominoPrefabs()
        {
            tetrominoPrefabs = new GameObject[5]; // Standard 5 tetromino shapes

            // I-piece (3-block straight)
            tetrominoPrefabs[0] = CreateTetrominoPrefab(
                new Vector3[] {
                    new Vector3(0, 0, 0),
                    new Vector3(1, 0, 0),
                    new Vector3(2, 0, 0)
                },
                Color.cyan,
                "I_Tetromino"
            );

            // O-piece (Square)
            tetrominoPrefabs[1] = CreateTetrominoPrefab(
                new Vector3[] {
                    new Vector3(0, 0, 0),
                    new Vector3(1, 0, 0),
                    new Vector3(0, 0, 1),
                    new Vector3(1, 0, 1)
                },
                Color.yellow,
                "O_Tetromino"
            );

            // T-piece (3D cross)
            tetrominoPrefabs[2] = CreateTetrominoPrefab(
                new Vector3[] {
                    new Vector3(1, 0, 1), // Center
                    new Vector3(0, 0, 1), // Left arm
                    new Vector3(2, 0, 1), // Right arm
                    new Vector3(1, 0, 0)  // Front arm
                },
                Color.magenta,
                "T_Tetromino"
            );

            // L-piece (3D corner)
            tetrominoPrefabs[3] = CreateTetrominoPrefab(
                new Vector3[] {
                    new Vector3(0, 0, 0),
                    new Vector3(1, 0, 0),
                    new Vector3(0, 0, 1),
                    new Vector3(0, 1, 0)
                },
                new Color(1.0f, 0.5f, 0.0f), // Orange
                "L_Tetromino"
            );

            // S-piece (3D zigzag)
            tetrominoPrefabs[4] = CreateTetrominoPrefab(
                new Vector3[] {
                    new Vector3(0, 0, 0),
                    new Vector3(1, 0, 0),
                    new Vector3(1, 0, 1),
                    new Vector3(1, 1, 1)
                },
                Color.green,
                "S_Tetromino"
            );

            for (int i = 0; i < tetrominoPrefabs.Length; i++)
            {
                tetrominoPrefabs[i].SetActive(false);
            }
            // 3D-specific shapes could be added here
        }

        private Bounds CalculatePieceBounds(GameObject piece)
        {
            Bounds bounds = new Bounds(piece.transform.position, Vector3.zero);
            foreach (Transform child in piece.transform)
            {
                bounds.Encapsulate(child.position);
            }
            return bounds;
        }

        private GameObject CreateTetrominoPrefab(Vector3[] blockPositions, Color color, string name)
        {
            GameObject tetromino = new GameObject(name);
            tetromino.AddComponent<Tetromino>();

            // Create blocks
            foreach (Vector3 position in blockPositions)
            {
                GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.transform.parent = tetromino.transform;
                block.transform.localPosition = position;
                Color blockColor = color;
                blockColor.a = 1.0f; // Ensure fully opaque
                block.GetComponent<MeshRenderer>().material.color = blockColor;

                // Add LineRenderer
                LineRenderer lineRenderer = block.AddComponent<LineRenderer>();
                lineRenderer.material = new Material(Shader.Find("Custom/AlwaysVisibleLine"));
                lineRenderer.material.color = Color.black;
                lineRenderer.startWidth = 0.03f;
                lineRenderer.endWidth = 0.03f;
                lineRenderer.useWorldSpace = false;
                lineRenderer.loop = true;

                // Define the corner points of the cube
                Vector3[] corners = new Vector3[16];
                float halfSize = 0.5f;
                corners[0] = new Vector3(-halfSize, -halfSize, -halfSize);
                corners[1] = new Vector3(halfSize, -halfSize, -halfSize);
                corners[2] = new Vector3(halfSize, -halfSize, halfSize);
                corners[3] = new Vector3(-halfSize, -halfSize, halfSize);
                corners[4] = new Vector3(-halfSize, -halfSize, -halfSize);
                
                corners[5] = new Vector3(-halfSize, halfSize, -halfSize);
                corners[6] = new Vector3(halfSize, halfSize, -halfSize);
                corners[7] = new Vector3(halfSize, halfSize, halfSize);
                corners[8] = new Vector3(-halfSize, halfSize, halfSize);
                corners[9] = new Vector3(-halfSize, halfSize, -halfSize);
                
                corners[10] = new Vector3(halfSize, halfSize, -halfSize);
                corners[11] = new Vector3(halfSize, -halfSize, -halfSize);
                
                corners[12] = new Vector3(halfSize, -halfSize, halfSize);
                corners[13] = new Vector3(halfSize, halfSize, halfSize);
                
                corners[14] = new Vector3(-halfSize, halfSize, halfSize);
                corners[15] = new Vector3(-halfSize, -halfSize, halfSize);
                

                lineRenderer.positionCount = corners.Length;
                lineRenderer.SetPositions(corners);

                // Enable the MeshRenderer
                block.GetComponent<MeshRenderer>().enabled = true;
            }

            return tetromino;
        }

        public void SpawnRandomPiece()
        {
            if (tetrominoPrefabs.Length == 0) return;

            int randomIndex = Random.Range(0, tetrominoPrefabs.Length);
            GameObject newPiece = Instantiate(tetrominoPrefabs[randomIndex], spawnPosition, Quaternion.identity);
            newPiece.SetActive(true);

            // Set the parent to this spawner's transform
            newPiece.transform.SetParent(transform);

            // Get grid dimensions from GameManager
            if (GameManager.Instance == null) return;
            Vector3Int gridSize = GameManager.Instance.GetGridSize();

            // Get tetromino component
            Tetromino tetromino = newPiece.GetComponent<Tetromino>();
            if (tetromino == null) return;

            // Calculate piece bounds
            Bounds pieceBounds = CalculatePieceBounds(newPiece);

            // Adjust position to keep piece within grid
            Vector3 adjustedPosition = spawnPosition;

            Debug.Log("=============");
            Debug.Log("Adjusted Position: " + adjustedPosition);
            Debug.Log("Min pieceBounds: " + pieceBounds.min);
            Debug.Log("Max pieceBounds: " + pieceBounds.max);
            // Check X bounds
            if (pieceBounds.min.x < 0)
            {
                adjustedPosition.x -= pieceBounds.min.x;
            }
            else if (pieceBounds.max.x >= gridSize.x)
            {
                adjustedPosition.x -= pieceBounds.max.x - gridSize.x + 0.5f;
            }

            // Check Z bounds
            if (pieceBounds.min.z < 0)
            {
                adjustedPosition.z -= pieceBounds.min.z;
            }
            else if (pieceBounds.max.z >= gridSize.z)
            {
                adjustedPosition.z -= pieceBounds.max.z - gridSize.z + 1;
            }

            // Apply adjusted position
            newPiece.transform.position = adjustedPosition;

            // Randomly rotate new piece for more variety (optional)
            if (Random.value > 0.5f)
            {
                int rotationAxis = Random.Range(0, 3); // 0 = X, 1 = Y, 2 = Z
                int rotationAngle = Random.Range(0, 4) * 90; // 0, 90, 180, or 270 degrees

                Vector3 rotation = Vector3.zero;
                switch (rotationAxis)
                {
                    case 0: rotation = new Vector3(rotationAngle, 0, 0); break;
                    case 1: rotation = new Vector3(0, rotationAngle, 0); break;
                    case 2: rotation = new Vector3(0, 0, rotationAngle); break;
                }

                newPiece.transform.Rotate(rotation);

                // Verify position is valid after rotation
                if (tetromino != null && !tetromino.IsValidPosition())
                {
                    // If invalid position after rotation, reset rotation
                    newPiece.transform.rotation = Quaternion.identity;
                }
            }

        }
    }
}
