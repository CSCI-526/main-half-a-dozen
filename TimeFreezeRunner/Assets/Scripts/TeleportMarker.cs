using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TeleportMarker : MonoBehaviour
{
    [Header("Blink")]
    public float pulseSpeed = 5f;
    public float minAlpha = 0.55f;
    public float maxAlpha = 1.0f;
    public float scalePulse = 0.08f;   

    SpriteRenderer _sr;
    Vector3 _baseScale;
    float _t;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _baseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;

        var c = _sr.color;
        if (c.r > c.b) _sr.color = new Color(0.2f, 0.8f, 1f, c.a); 
    }

    void Update()
    {
        _t += Time.unscaledDeltaTime * pulseSpeed; 
        float s = 1f + Mathf.Sin(_t) * scalePulse;
        transform.localScale = Vector3.Lerp(transform.localScale, _baseScale * s, 0.25f);

        float a = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(_t) * 0.5f + 0.5f));
        var c = _sr.color; c.a = a; _sr.color = c;
    }
}
