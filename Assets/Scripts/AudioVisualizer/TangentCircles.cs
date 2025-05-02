using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class TangentCircles : CircleTangent
{
    [Header("Setup")]
    public GameObject _circlePrefab;
    public int channel = 2; // Audio channel to visualize

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

    private Vector4 _innerCircle, _outterCircle;
    public float _innerCircleRadius, _outerCircleRadius;
    private Vector4[] _tangentCircle;
    private GameObject[] _tangentObject;
    [Range(1, 64)]
    public int _circleAmount;

    [Header("Audio Visuals")]
    private Material _materialBase;
    private Material[] _material;
    public Gradient _gradient;
    private float _rotateTangentObjects;
    public float _rotateSpeed;

    [Header("Scale")]
    public bool _scaleYOnAudio;
    [Range(0, 1)]
    public float _scaleThreshold;
    public Vector2 _scaleMinMax;

    [Header("Emission")]
    public float _emissionMultiplier;
    [Range(0, 1)]
    public float _thresholdEmission;
    public float emissionLerpSpeed = 5f; // Adjust this value to control the lerp speed
    private Color[] _targetEmissionColor;

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
        }
    }

    void Start()
    {
        _innerCircle = new Vector4(transform.position.x, transform.position.y, transform.position.z, _innerCircleRadius);
        _outterCircle = new Vector4(transform.position.x, transform.position.y, transform.position.z, _outerCircleRadius);

        _tangentCircle = new Vector4[_circleAmount];
        _tangentObject = new GameObject[_circleAmount];
        _material = new Material[_circleAmount];
        _targetEmissionColor = new Color[_circleAmount];

        _materialBase = Resources.Load<Material>("Materials/Circle");
        for (int i = 0; i < _circleAmount; i++)
        {
            GameObject tangentInstance = (GameObject)Instantiate(_circlePrefab);
            _tangentObject[i] = tangentInstance;
            _tangentObject[i].transform.parent = this.transform;
            _material[i] = new Material(_materialBase);
            _material[i].EnableKeyword("_EMISSION");
            _tangentObject[i].GetComponent<MeshRenderer>().material = _material[i];
        }

        GenerateInitialRandomTargets();
        InitializeCurrentValues();
        StartCoroutine(RandomlyUpdateTangentTargets());
    }

    // Update is called once per frame
    void Update()
    {
        currentEmission = Mathf.Lerp(currentEmission, targetEmission, Time.deltaTime * tangentCirclesLerpSpeed);
        currentScaleStart = Mathf.Lerp(currentScaleStart, targetScaleStart, Time.deltaTime * tangentCirclesLerpSpeed);
        currentScaleMinMax = Vector2.Lerp(currentScaleMinMax, targetScaleMinMax, Time.deltaTime * tangentCirclesLerpSpeed);
        currentInnerRadius = Mathf.Lerp(currentInnerRadius, targetInnerRadius, Time.deltaTime * tangentCirclesLerpSpeed);
        currentOuterRadius = Mathf.Lerp(currentOuterRadius, targetOuterRadius, Time.deltaTime * tangentCirclesLerpSpeed);

        _gradient = targetGradient;

        _innerCircle.x = transform.position.x;
        _innerCircle.y = transform.position.y;
        _innerCircle.z = transform.position.z;
        _innerCircle.w = currentInnerRadius;

        _outterCircle.x = transform.position.x;
        _outterCircle.y = transform.position.y;
        _outterCircle.z = transform.position.z;
        _outterCircle.w = currentOuterRadius;

        AudioSpectrumProcessor.Instance.UpdateSpectrum();
        var averageAmplitude = AudioSpectrumProcessor.Instance.GetAverageAmplitudeInRange(channel, _rotateSpeed);
        _rotateTangentObjects += _rotateSpeed * Time.deltaTime * averageAmplitude;
        Debug.Log("rotate averageAmplitude: " + averageAmplitude);
        for (int i = 0; i < _circleAmount; i++)
        {
            // int bandIndex = i * 8 / _circleAmount;
            _tangentCircle[i] = FindTangentCircle(_outterCircle, _innerCircle, 360f / _circleAmount * i + _rotateTangentObjects);
            Vector3 relativePosition = new Vector3(_tangentCircle[i].x, _tangentCircle[i].y, _tangentCircle[i].z);
            _tangentObject[i].transform.position = transform.position + relativePosition;
            _tangentObject[i].transform.localScale = new Vector3(_tangentCircle[i].w, _tangentCircle[i].w, _tangentCircle[i].w) * 2;

            // var amplitude = AudioSpectrumProcessor.Instance.GetAmplitudeForBand(2, bandIndex, _emissionMultiplier);
            var amplitude64 = AudioSpectrumProcessor.Instance.GetAmplitudeForBand64(channel, i, _emissionMultiplier);
            Debug.Log("amp64: " + amplitude64);
            if (_scaleYOnAudio)
            {
                if (amplitude64 > _scaleThreshold)
                {
                    _tangentObject[i].transform.localScale = new Vector3(_tangentCircle[i].w, currentScaleStart + Mathf.Lerp(_scaleMinMax.x, _scaleMinMax.y, amplitude64), _tangentCircle[i].w);

                }
                else
                {
                    _tangentObject[i].transform.localScale = new Vector3(_tangentCircle[i].w, currentScaleStart, _tangentCircle[i].w) * 2;
                }
            }
            else
            {
                _tangentObject[i].transform.localScale = new Vector3(_tangentCircle[i].w, _tangentCircle[i].w, _tangentCircle[i].w) * 2;
            }

            Color calculatedEmissionColor;
            if (amplitude64 > _thresholdEmission)
            {
                calculatedEmissionColor = _gradient.Evaluate(1f / _circleAmount * i) * amplitude64;
            }
            else
            {
                calculatedEmissionColor = new Color(0, 0, 0);
            }
            _targetEmissionColor[i] = calculatedEmissionColor;

            _material[i].SetColor("_EmissionColor", Color.Lerp(_material[i].GetColor("_EmissionColor"), _targetEmissionColor[i], Time.deltaTime * emissionLerpSpeed));
        }
    }
}
