using UnityEngine;
using System.Collections.Generic;

public class CubeCircleSpawner : MonoBehaviour
{
    [Tooltip("The number of cubes to spawn in the circle.")]
    public int numberOfCubes = 12;

    [Tooltip("The radius of the circle.")]
    public float radius = 5.0f;

    [Tooltip("The Prefab to use for the cubes. If null, primitive cubes will be created.")]
    public GameObject cubePrefab;


    [Tooltip("The channel to use for spectrum analysis. 0 = Left, 1 = Right, 2 = stereo.")]
    public int channel = 2; // Audio channel to use for spectrum analysis
    public float amplitudeScale = 10.0f; // Scale for amplitude visualization
    public int totalBands = 8; // Total number of frequency bands to divide the spectrum into
    public float colorCycleSpeed = 0.5f; // Speed of the color change
    public float lerpSpeed = 5f; // Speed of the lerp for scaling
    private GameObject[] spawnedCubes; // Array to store spawned cubes
    private Renderer[] cubeRenderers; // Array to store renderers of spawned cubes
    private float[] cubeHues; // Array to store base hues for each cube
    private AudioSpectrumProcessor spectrumProcessor; // Cached AudioSpectrumProcessor instance
    private Vector3 newScale; // Reusable Vector3 for scaling

    // Added MaterialPropertyBlock for efficient color updates
    private MaterialPropertyBlock propertyBlock;
    // Added property ID for the color property
    private int colorPropertyID;


    void Start()
    {
        // Initialize MaterialPropertyBlock and get color property ID
        propertyBlock = new MaterialPropertyBlock();
        // Assuming the shader uses "_Color" for the main color property.
        // If using URP Lit, it might be "_BaseColor". Check your shader.
        colorPropertyID = Shader.PropertyToID("_Color");
        // If using URP Lit, you might need:
        // colorPropertyID = Shader.PropertyToID("_BaseColor");

        SpawnCubes();
        spectrumProcessor = AudioSpectrumProcessor.Instance; // Cache the instance
        newScale = Vector3.one; // Initialize the reusable Vector3
    }

    void Update()
    {
        if (spectrumProcessor != null && spectrumProcessor.IsInitialized && spawnedCubes != null && propertyBlock != null) // Added propertyBlock null check
        {
            spectrumProcessor.UpdateSpectrum();

            for (int i = 0; i < spawnedCubes.Length; i++)
            {
                if (spawnedCubes[i] != null && cubeRenderers[i] != null) // Added renderer null check
                {
                    int bandIndex = i * totalBands / numberOfCubes;
                    // Ensure bandIndex is within the valid range for GetAmplitudeForBand (0 to totalBands - 1)
                    bandIndex = Mathf.Clamp(bandIndex, 0, totalBands - 1);

                    float amplitude = spectrumProcessor.GetAmplitudeForBand(channel, bandIndex, amplitudeScale);

                    float scaleY = Mathf.Clamp(amplitude, 0.1f, 10f);
                    Vector3 currentScale = spawnedCubes[i].transform.localScale;
                    newScale.x = currentScale.x;
                    newScale.y = scaleY;
                    newScale.z = currentScale.z;
                    spawnedCubes[i].transform.localScale = Vector3.Lerp(currentScale, newScale, Time.deltaTime * lerpSpeed);

                    // Update the color using MaterialPropertyBlock
                    float currentHue = Mathf.Repeat(cubeHues[i] + Time.time * colorCycleSpeed, 1f);
                    Color newColor = Color.HSVToRGB(currentHue, 1f, 1f);

                    // Get the current property block data (important if other properties are set elsewhere)
                    cubeRenderers[i].GetPropertyBlock(propertyBlock);
                    // Set the color property
                    propertyBlock.SetColor(colorPropertyID, newColor);
                    // Apply the updated property block to the renderer
                    cubeRenderers[i].SetPropertyBlock(propertyBlock);

                    // Removed: Direct material color access
                    // cubeRenderers[i].material.color = Color.HSVToRGB(currentHue, 1f, 1f);
                }
            }
        }
    }

    void SpawnCubes()
    {
        Vector3 center = transform.position;
        if (numberOfCubes <= 0) {
            // Debug.LogWarning("Number of cubes must be positive.", this); // Removed debug log
            return;
        }

        spawnedCubes = new GameObject[numberOfCubes]; // Initialize the array with the number of cubes
        cubeRenderers = new Renderer[numberOfCubes]; // Initialize the renderer array
        cubeHues = new float[numberOfCubes]; // Initialize the hue array

        for (int i = 0; i < numberOfCubes; i++)
        {
            // Calculate the angle for this cube
            float angle = i * Mathf.PI * 2f / numberOfCubes; // Use radians for Cos/Sin

            // Calculate the position on the circle (XZ plane)
            float x = center.x + radius * Mathf.Cos(angle);
            float z = center.z + radius * Mathf.Sin(angle);
            Vector3 pos = new Vector3(x, center.y, z); // Keep the same Y level as the spawner

            GameObject spawnedCube;

            // Instantiate the prefab or a primitive cube
            if (cubePrefab != null)
            {
                spawnedCube = Instantiate(cubePrefab, pos, Quaternion.identity);
            }
            else
            {
                spawnedCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spawnedCube.transform.position = pos;
            }

            // Calculate and set the scale based on the number of cubes
            if (numberOfCubes > 0) // Avoid division by zero
            {
                float scaleValue = 10f / numberOfCubes;
                spawnedCube.transform.localScale = new Vector3(scaleValue, scaleValue, scaleValue);
            }

            // Make the cube face the center
            spawnedCube.transform.LookAt(center);

            // Set the parent to this spawner object for organization
            spawnedCube.transform.parent = transform;

            // Optional: Name the cubes for easier identification
            spawnedCube.name = $"CircleCube_{i}";

            // Add the spawned cube to the array
            spawnedCubes[i] = spawnedCube;

            // Get the renderer and set the initial color
            Renderer cubeRenderer = spawnedCube.GetComponent<Renderer>();
            if (cubeRenderer != null)
            {
                cubeRenderers[i] = cubeRenderer;
                cubeHues[i] = (float)i / numberOfCubes; // Calculate hue based on position

                // Set initial color using MaterialPropertyBlock
                // Get the current property block data (important if other properties are set elsewhere)
                cubeRenderer.GetPropertyBlock(propertyBlock);
                // Set the color property
                propertyBlock.SetColor(colorPropertyID, Color.HSVToRGB(cubeHues[i], 1f, 1f));
                // Apply the updated property block to the renderer
                cubeRenderer.SetPropertyBlock(propertyBlock);

                // Removed: Direct material color access
                // cubeRenderer.material.color = Color.HSVToRGB(cubeHues[i], 1f, 1f);
            }
            else
            {
                Debug.LogError($"CubeCircleSpawner: No renderer found on cube {i}!", this);
            }
        }
    }
    // Added a helper method to get band count from AudioSpectrumProcessor
    // This assumes AudioSpectrumProcessor has a public method like GetBandCount()
    // If not, you might need to adjust how bandIndex is clamped or add such a method.
    // Based on the AudioSpectrumProcessor code, it has calculatedFreqBandsLeft/Right arrays of size 8.
    // So the band count for GetAmplitudeForBand is 8.
    // And calculatedFreqBands64Left/Right arrays of size 64.
    // So the band count for GetAmplitudeForBand64 is 64.
    // The CubeCircleSpawner uses GetAmplitudeForBand, so the band count is 8.
    // The clamp should be based on totalBands, which is 8 by default.
    // Let's adjust the clamp in Update to use totalBands directly.
}
