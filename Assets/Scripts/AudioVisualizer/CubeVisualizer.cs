using Unity.Mathematics;
using UnityEngine;

public class CubeVisualizer : MonoBehaviour
{
    public GameObject cubePrefab;
    public int numberOfCubes = 8;
    public float cubeSpacing = 1.5f;
    public float heightScale = 10f;
    public float lerpSpeed = 5f;
    public int audioChannel = 2;
    public float minHeight = 0.1f;

    private GameObject[] cubes;
    private Vector3 startPosition;

    void Start()
    {
        if (cubePrefab == null)
        {
            Debug.LogError("CubeVisualizer: cubePrefab is not assigned! Disabling script.");
            this.enabled = false;
            return;
        }

        AudioSpectrumProcessor processor = AudioSpectrumProcessor.Instance;
        if (processor == null || !processor.IsInitialized)
        {
            Debug.LogError("CubeVisualizer: AudioSpectrumProcessor is not initialized! Make sure it's initialized before this script. Disabling script.");
            this.enabled = false;
            return;
        }

        // Calculate start position to center the cubes
        float totalWidth = (numberOfCubes - 1) * cubeSpacing;
        startPosition = transform.position - new Vector3(totalWidth / 2, 0, 0);

        cubes = new GameObject[numberOfCubes];
        for (int i = 0; i < numberOfCubes; i++)
        {
            Vector3 cubePosition = startPosition + transform.right * (i * cubeSpacing);
            GameObject cube = Instantiate(cubePrefab, cubePosition, Quaternion.identity, transform);
            cubes[i] = cube;
        }
    }

    void Update()
    {
        AudioSpectrumProcessor processor = AudioSpectrumProcessor.Instance;
        if (processor == null || !processor.IsInitialized) return;

        processor.UpdateSpectrum();

        for (int i = 0; i < numberOfCubes; i++)
        {
            float amplitude = processor.GetAmplitudeForBand(audioChannel, i, 1f);
            float targetYScale = Mathf.Max(minHeight, amplitude * heightScale);

            Vector3 currentScale = cubes[i].transform.localScale;
            Vector3 targetScale = new Vector3(currentScale.x, targetYScale, currentScale.z);

            cubes[i].transform.localScale = Vector3.Lerp(currentScale, targetScale, Time.deltaTime * lerpSpeed);
        }
    }
}
