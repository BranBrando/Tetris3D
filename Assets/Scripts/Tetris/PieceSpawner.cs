using UnityEngine;
using System.Collections.Generic; // Required for Dictionary

namespace TetrisGame
{
    public class PieceSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject[] tetrominoPrefabs;
        private Vector3 spawnPosition;

        // Removed: private GameObject fallingPiece; // Not used in this script

        private Material defaultBlockMaterial; // Base material for blocks
        private Material lineOutlineMaterial; // Shared material for line renderers

        // Dictionary to store shared materials for each color
        private Dictionary<Color, Material> sharedBlockMaterials = new Dictionary<Color, Material>();

        private void Start()
        {
            // Get spawn position from GameObject transform
            spawnPosition = transform.position;

            // Load the base material for blocks
            defaultBlockMaterial = Resources.Load<Material>("Materials/DefaultMaterial");
            if (defaultBlockMaterial == null)
            {
                Debug.LogError("PieceSpawner: Failed to load 'Materials/DefaultMaterial'. Dynamic prefab creation may fail.");
                // Fallback to a basic material if the default is missing
                defaultBlockMaterial = new Material(Shader.Find("Standard"));
            }

            // Create the shared material for line outlines
            Shader lineShader = Shader.Find("Custom/LineShader");
            if (lineShader == null)
            {
                Debug.LogWarning("PieceSpawner: Custom/LineShader not found. Using default material for line outlines.");
                lineShader = Shader.Find("Standard");
            }
            if (lineShader != null)
            {
                lineOutlineMaterial = new Material(lineShader);
                lineOutlineMaterial.color = Color.black;
            }
            else
            {
                Debug.LogError("Failed to load default shader for line outline material");
            }

            // Create 3D tetromino prefabs if none exist
            if (tetrominoPrefabs == null || tetrominoPrefabs.Length == 0)
            {
                CreateDefaultTetrominoPrefabs();
            }

            SpawnRandomPiece();
        }

        private void CreateDefaultTetrominoPrefabs()
        {
            // Define the colors used by the default tetrominos
            Color cyan = Color.cyan;
            Color yellow = Color.yellow;
            Color magenta = Color.magenta;
            Color orange = new Color(1.0f, 0.5f, 0.0f);
            Color green = Color.green;

            // Create shared material instances for each color
            CreateSharedBlockMaterial(cyan);
            CreateSharedBlockMaterial(yellow);
            CreateSharedBlockMaterial(magenta);
            CreateSharedBlockMaterial(orange);
            CreateSharedBlockMaterial(green);


            tetrominoPrefabs = new GameObject[5]; // Standard 5 tetromino shapes

            // I-piece (3-block straight)
            tetrominoPrefabs[0] = CreateTetrominoPrefab(
                new Vector3[] {
                    new Vector3(0, 0, 0),
                    new Vector3(1, 0, 0),
                    new Vector3(2, 0, 0)
                },
                cyan, // Use the defined color
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
                yellow, // Use the defined color
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
                magenta, // Use the defined color
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
                orange, // Use the defined color
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
                green, // Use the defined color
                "S_Tetromino"
            );

            for (int i = 0; i < tetrominoPrefabs.Length; i++)
            {
                if (tetrominoPrefabs[i] != null) // Add null check
                {
                    tetrominoPrefabs[i].SetActive(false);
                }
            }
            // 3D-specific shapes could be added here
        }

        // Helper to create and store shared material instances
        private void CreateSharedBlockMaterial(Color color)
        {
            if (defaultBlockMaterial == null) return; // Cannot create if base material is missing

            // Create a new material instance based on the default material
            Material sharedMat = new Material(defaultBlockMaterial);
            sharedMat.color = color; // Set the specific color

            // Store it in the dictionary
            if (!sharedBlockMaterials.ContainsKey(color))
            {
                sharedBlockMaterials.Add(color, sharedMat);
            }
            else
            {
                // If a material for this color already exists, destroy the new one
                Destroy(sharedMat);
            }
        }


        private Bounds CalculatePieceBounds(GameObject piece)
        {
            Bounds bounds = new Bounds(piece.transform.position, Vector3.zero);
            foreach (Transform child in piece.transform)
            {
                // Ensure the child has a renderer before encapsulating
                Renderer childRenderer = child.GetComponent<Renderer>();
                if (childRenderer != null)
                {
                     bounds.Encapsulate(childRenderer.bounds);
                }
                else
                {
                    // If no renderer, just encapsulate the position
                    bounds.Encapsulate(child.position);
                }
            }
            return bounds;
        }

        private GameObject CreateTetrominoPrefab(Vector3[] blockPositions, Color color, string name)
        {
            GameObject tetromino = new GameObject(name);
            tetromino.AddComponent<Tetromino>();

            // Get the shared material for this color
            Material blockMaterialToAssign = defaultBlockMaterial; // Fallback
            if (sharedBlockMaterials.TryGetValue(color, out Material sharedMat))
            {
                blockMaterialToAssign = sharedMat;
            }
            else
            {
                 Debug.LogWarning($"PieceSpawner: Shared material for color {color} not found. Using default material for {name}.");
            }


            // Create blocks
            foreach (Vector3 position in blockPositions)
            {
                GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.transform.parent = tetromino.transform;
                block.transform.localPosition = position;
                // Color blockColor = color; // Color is now handled by the shared material
                // blockColor.a = 1.0f; // Ensure fully opaque

                // Assign the shared material to the block
                MeshRenderer meshRenderer = block.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.material = blockMaterialToAssign; // Assign the shared material
                    // meshRenderer.material.color = blockColor; // No need to set color here, it's on the shared material
                }
                else
                {
                    Debug.LogError($"PieceSpawner: No MeshRenderer found on block of {name}!");
                }


                // Add LineRenderer
                LineRenderer lineRenderer = block.AddComponent<LineRenderer>();
                if (lineOutlineMaterial != null)
                {
                    lineRenderer.material = lineOutlineMaterial; // Assign the shared line material
                    // Line properties are set on the LineRenderer component below
                }
                else
                {
                    // Fallback if line material creation failed
                    lineRenderer.material = new Material(Shader.Find("Standard"));
                    lineRenderer.material.color = Color.black;
                }

                // Set LineRenderer properties (moved from material setup)
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

                // Enable the MeshRenderer (it's enabled by default for primitives, but good to be explicit)
                // block.GetComponent<MeshRenderer>().enabled = true; // This is redundant
            }

            return tetromino;
        }

        public void SpawnRandomPiece()
        {
            if (tetrominoPrefabs == null || tetrominoPrefabs.Length == 0) // Added null check for array
            {
                 Debug.LogError("PieceSpawner: No tetromino prefabs assigned or created.");
                 return;
            }


            int randomIndex = Random.Range(0, tetrominoPrefabs.Length);
            GameObject prefabToSpawn = tetrominoPrefabs[randomIndex];

            if (prefabToSpawn == null) // Add null check for the selected prefab
            {
                 Debug.LogError($"PieceSpawner: Tetromino prefab at index {randomIndex} is null.");
                 return;
            }

            GameObject newPiece = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
            newPiece.SetActive(true);

            // Set the parent to this spawner's transform
            newPiece.transform.SetParent(transform);

            // Get grid dimensions from GameManager
            if (GameManager.Instance == null)
            {
                 Debug.LogError("PieceSpawner: GameManager instance not found.");
                 return;
            }
            Vector3Int gridSize = GameManager.Instance.GetGridSize();

            // Get tetromino component
            Tetromino tetromino = newPiece.GetComponent<Tetromino>();
            if (tetromino == null)
            {
                 Debug.LogError("PieceSpawner: Spawned piece does not have a Tetromino component.");
                 return;
            }


            // Calculate piece bounds
            Bounds pieceBounds = CalculatePieceBounds(newPiece);

            // Adjust position to keep piece within grid
            Vector3 adjustedPosition = spawnPosition;

            // Removed Debug.Log calls

            // Check X bounds
            if (pieceBounds.min.x < 0)
            {
                adjustedPosition.x -= pieceBounds.min.x;
            }
            else if (pieceBounds.max.x > gridSize.x) // Changed >= to > for correct boundary check
            {
                adjustedPosition.x -= pieceBounds.max.x - gridSize.x;
            }

            // Check Z bounds
            if (pieceBounds.min.z < 0)
            {
                adjustedPosition.z -= pieceBounds.min.z;
            }
            else if (pieceBounds.max.z > gridSize.z) // Changed >= to > for correct boundary check
            {
                adjustedPosition.z -= pieceBounds.max.z - gridSize.z;
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
                if (!tetromino.IsValidPosition()) // Simplified check
                {
                    // If invalid position after rotation, reset rotation
                    newPiece.transform.rotation = Quaternion.identity;
                }
            }

        }
    }
}
