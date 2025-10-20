using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CorridorLightPulse : MonoBehaviour
{
    private Light2D light2D;

    [Header("Pulse Settings")]
    public float baseIntensity = 0.6f;
    public float amplitude = 0.2f;
    public float speed = 2f;

    void Start() => light2D = GetComponent<Light2D>();

    void Update()
    {
        if (light2D)
            light2D.intensity = baseIntensity + Mathf.Sin(Time.time * speed) * amplitude;
    }
}