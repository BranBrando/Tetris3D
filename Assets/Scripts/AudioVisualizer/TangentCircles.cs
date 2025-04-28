using System.Collections.Generic;
using UnityEngine;

public class TangentCircles : CircleTangent
{
    [Header("Setup")]
    public GameObject _circlePrefab;
    private Vector4 _innerCircle, _outterCircle;
    public float _innerCircleRadius, _outerCircleRadius;
    private Vector4[] _tangentCircle;
    private GameObject[] _tangentObject;
    [Range(1, 64)]
    public int _circleAmount;

    [Header("Audio Visuals")]
    public Material _materialBase;
    private Material[] _material;
    public Gradient _gradient;
    public float _emissionMultiplier;
    public bool _emissionBuffer;
    [Range(0, 1)]
    public float _thresholdEmission;
    private float _rotateTangentObjects;
    public float _rotateSpeed;
    public bool _rotateBuffer;

    [Header("Emission")]
    public float emissionLerpSpeed = 5f; // Adjust this value to control the lerp speed
    private Color[] _targetEmissionColor;

    void Start()
    {
        _innerCircle = new Vector4(transform.position.x, transform.position.y, transform.position.z, _innerCircleRadius);
        _outterCircle = new Vector4(transform.position.x, transform.position.y, transform.position.z, _outerCircleRadius);

        _tangentCircle = new Vector4[_circleAmount];
        _tangentObject = new GameObject[_circleAmount];
        _material = new Material[_circleAmount];
        _targetEmissionColor = new Color[_circleAmount];

        for (int i = 0; i < _circleAmount; i++)
        {
            GameObject tangentInstance = (GameObject)Instantiate(_circlePrefab);
            _tangentObject[i] = tangentInstance;
            _tangentObject[i].transform.parent = this.transform;
            _material[i] = new Material(_materialBase);
            _material[i].EnableKeyword("_EMISSION");
            _material[i].SetColor("_BaseColor", new Color(0, 0, 0));
            _tangentObject[i].GetComponent<MeshRenderer>().material = _material[i];
        }
    }

    // Update is called once per frame
    void Update()
    {
        _innerCircle.x = transform.position.x;
        _innerCircle.y = transform.position.y;
        _innerCircle.z = transform.position.z;
        // _innerCircle.w remains _innerCircleRadius

        _outterCircle.x = transform.position.x;
        _outterCircle.y = transform.position.y;
        _outterCircle.z = transform.position.z;
        // _outterCircle.w remains _outerCircleRadius

        var averageAmplitude = AudioSpectrumProcessor.Instance.GetAverageAmplitudeInRange(2, _rotateSpeed);
        _rotateTangentObjects += _rotateSpeed * Time.deltaTime * averageAmplitude;

        for (int i = 0; i < _circleAmount; i++)
        {
            // int bandIndex = i * 8 / _circleAmount;
            _tangentCircle[i] = FindTangentCircle(_outterCircle, _innerCircle, 360f / _circleAmount * i + _rotateTangentObjects);
            Vector3 relativePosition = new Vector3(_tangentCircle[i].x, _tangentCircle[i].y, _tangentCircle[i].z);
            _tangentObject[i].transform.position = transform.position + relativePosition;
            _tangentObject[i].transform.localScale = new Vector3(_tangentCircle[i].w, _tangentCircle[i].w, _tangentCircle[i].w) * 2;
            
            // var amplitude = AudioSpectrumProcessor.Instance.GetAmplitudeForBand(2, bandIndex, _emissionMultiplier);
            var amplitude64 = AudioSpectrumProcessor.Instance.GetAmplitudeForBand64(2, i, _emissionMultiplier);
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
