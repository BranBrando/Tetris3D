using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for List

// Ensure this namespace matches the folder structure if you use namespaces
// namespace AudioVisualizer { 

public class AudioVisualizerManager : MonoBehaviour
{
    [Header("Managed visualizers")]
    [Tooltip("Assign the visualizer visualizers (e.g., CubeCircleSpawner, TangentCircles, CubeVisualizer) here.")]
    public GameObject[] visualizers;

    [Header("Randomization Settings")]
    public float minTimeBetweenUpdates = 5f;
    public float maxTimeBetweenUpdates = 15f;

    [Header("Emission Randomization")]
    public float minEmissionMultiplier = 1f;
    public float maxEmissionMultiplier = 5f;

    [Header("Scale Randomization")]
    public float minScaleStart = 0f;
    public float maxScaleStart = 1f;
    public float minScaleMin = 0.5f;
    public float maxScaleMin = 1f;
    public float minScaleMax = 1f;
    public float maxScaleMax = 2f;

    [Header("Radius Randomization")]
    public float minInnerRadius = -10f;
    public float maxInnerRadius = 5f;
    public float minOuterRadius = 6f;
    public float maxOuterRadius = 20f;

    [Header("Lerp Settings")]
    public float tangentCirclesLerpSpeed = 5f; // Controls transition speed

    // Target values (set by coroutine)
    private Gradient targetGradient;
    private float targetEmission;
    private float targetScaleStart;
    private Vector2 targetScaleMinMax;
    private float targetInnerRadius;
    private float targetOuterRadius;

    // Current values (lerped in Update)
    private float currentEmission;
    private float currentScaleStart;
    private Vector2 currentScaleMinMax;
    private float currentInnerRadius;
    private float currentOuterRadius;

    void Start()
    {
        GenerateInitialRandomTargets();
        InitializeCurrentValues();
        ChangeTangentCirclesGradient(targetGradient);
        StartCoroutine(RandomlyUpdateTangentTargets()); // Renamed coroutine
    }

    void Update()
    {
        // Lerp current numeric values towards targets
        currentEmission = Mathf.Lerp(currentEmission, targetEmission, Time.deltaTime * tangentCirclesLerpSpeed);
        currentScaleStart = Mathf.Lerp(currentScaleStart, targetScaleStart, Time.deltaTime * tangentCirclesLerpSpeed);
        currentScaleMinMax = Vector2.Lerp(currentScaleMinMax, targetScaleMinMax, Time.deltaTime * tangentCirclesLerpSpeed);
        currentInnerRadius = Mathf.Lerp(currentInnerRadius, targetInnerRadius, Time.deltaTime * tangentCirclesLerpSpeed);
        currentOuterRadius = Mathf.Lerp(currentOuterRadius, targetOuterRadius, Time.deltaTime * tangentCirclesLerpSpeed);

        // Apply lerped values (except gradient)
        ChangeTangentCirclesEmission(currentEmission);
        ChangeTangentCirclesScale(currentScaleStart, currentScaleMinMax.x, currentScaleMinMax.y);
        ChangeTangentCirclesRadii(currentInnerRadius, currentOuterRadius);
    }

    private IEnumerator RandomlyUpdateTangentTargets()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minTimeBetweenUpdates, maxTimeBetweenUpdates));

            // Generate new random target values
            targetGradient = CreateColorfulRandomGradient();
            targetEmission = Random.Range(minEmissionMultiplier, maxEmissionMultiplier);
            targetScaleStart = Random.Range(minScaleStart, maxScaleStart);
            targetScaleMinMax = new Vector2(
                Random.Range(minScaleMin, maxScaleMin),
                Random.Range(minScaleMax, maxScaleMax)
            );
            targetInnerRadius = Random.Range(minInnerRadius, maxInnerRadius);
            targetOuterRadius = Random.Range(minOuterRadius, maxOuterRadius);

            // Apply gradient instantly
            ChangeTangentCirclesGradient(targetGradient);
        }
    }

    private Gradient CreateColorfulRandomGradient()
    {
        Gradient gradient = new Gradient();
        int keyCount = Random.Range(2, 4); // Use 2 or 3 color keys
        GradientColorKey[] colorKeys = new GradientColorKey[keyCount];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2]; // Keep alpha simple (start/end)

        for (int i = 0; i < keyCount; i++)
        {
            colorKeys[i].color = Random.ColorHSV(0f, 1f, 0.8f, 1f, 0.8f, 1f); // Ensure bright colors
            colorKeys[i].time = (float)i / (keyCount - 1); // Distribute keys evenly
        }

        alphaKeys[0].alpha = 1f;
        alphaKeys[0].time = 0f;
        alphaKeys[1].alpha = 1f;
        alphaKeys[1].time = 1f;

        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }

    public void ChangeTangentCirclesGradient(Gradient newGradient)
    {
        foreach (var visualizer in visualizers)
        {
            if (visualizer != null && visualizer.TryGetComponent<TangentCircles>(out var circles))
            {
                circles._gradient = newGradient;
            }
        }
    }

    public void ChangeTangentCirclesEmission(float value)
    {
        foreach (var visualizer in visualizers)
        {
            if (visualizer != null && visualizer.TryGetComponent<TangentCircles>(out var circles))
            {
                circles._emissionMultiplier = value;
            }
        }
    }
    public void ChangeTangentCirclesScale(float start, float min, float max)
    {
        foreach (var visualizer in visualizers)
        {
            if (visualizer != null && visualizer.TryGetComponent<TangentCircles>(out var circles))
            {
                circles._scaleStart = start;
                circles._scaleMinMax = new Vector2(min, max);
            }
        }
    }

    public void ChangeTangentCirclesRadii(float innerRadius, float outerRadius)
    {
        foreach (var visualizer in visualizers)
        {
            if (visualizer != null && visualizer.TryGetComponent<TangentCircles>(out var circles))
            {
                circles._innerCircleRadius = innerRadius;
                circles._outerCircleRadius = outerRadius;
            }
        }
    }

    private void GenerateInitialRandomTargets()
    {
        targetGradient = CreateColorfulRandomGradient();
        targetEmission = Random.Range(minEmissionMultiplier, maxEmissionMultiplier);
        targetScaleStart = Random.Range(minScaleStart, maxScaleStart);
        targetScaleMinMax = new Vector2(
            Random.Range(minScaleMin, maxScaleMin),
            Random.Range(minScaleMax, maxScaleMax)
        );
        targetInnerRadius = Random.Range(minInnerRadius, maxInnerRadius);
        targetOuterRadius = Random.Range(minOuterRadius, maxOuterRadius);
    }

    private void InitializeCurrentValues()
    {
        currentEmission = targetEmission;
        currentScaleStart = targetScaleStart;
        currentScaleMinMax = targetScaleMinMax;
        currentInnerRadius = targetInnerRadius;
        currentOuterRadius = targetOuterRadius;
    }
}

// } // End namespace if used
