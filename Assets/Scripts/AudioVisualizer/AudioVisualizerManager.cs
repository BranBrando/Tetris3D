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

    void Start()
    {
        // New logic: Randomly enable/disable visualizers
        if (visualizers != null && visualizers.Length > 0)
        {
            foreach (var visualizer in visualizers)
            {
                if (visualizer != null)
                {
                    bool shouldBeActive = Random.value > 0.5f; // 50% chance
                    visualizer.SetActive(shouldBeActive);
                }
            }
        }
        else
        {
             Debug.LogWarning("AudioVisualizerManager: No visualizers assigned to randomly enable/disable.");
        }
    }

    void Update()
    {
    }
}
