using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RingPulse : MonoBehaviour
{
    public float pulseSpeed = 3.0f;
    public float scaleAmp = 0.08f;     
    public float alphaMin = 0.55f;
    public float alphaMax = 0.95f;

    SpriteRenderer _sr;
    Vector3 _baseScale;
    float _t;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _baseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;

        var c = _sr.color;
        if (c.r > c.b) _sr.color = new Color(0.25f, 0.75f, 1f, c.a);
    }

    void OnEnable() { _t = 0f; }

    void Update()
    {
        _t += Time.unscaledDeltaTime * pulseSpeed;
        float s = 1f + Mathf.Sin(_t) * scaleAmp;
        transform.localScale = Vector3.Lerp(transform.localScale, _baseScale * s, 0.25f);

        float a = Mathf.Lerp(alphaMin, alphaMax, (Mathf.Sin(_t) * 0.5f + 0.5f));
        var c = _sr.color; c.a = a; _sr.color = c;
    }
}
