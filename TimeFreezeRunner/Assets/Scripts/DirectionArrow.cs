using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DirectionArrow : MonoBehaviour
{
    [Header("Pulse + Fade Settings")]
    public float pulseSpeed = 4f;
    public float pulseScale = 0.07f;
    public float alphaMin = 0.35f, alphaMax = 1.0f;

    [Header("Arrow Rotation (built-in Triangle points DOWN)")]
    public float rightZ = -90f;  
    public float leftZ  =  90f;  // left arrow

    [Header("Extra visuals that should hide/show with the arrow")]
    public GameObject[] linkedVisuals; 

    SpriteRenderer sr;
    Vector3 baseScale;
    float t;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 20;

        sr.enabled = false;
        SetLinkedActive(false);
    }

    void Update()
    {
        if (!sr.enabled) return;

        t += Time.deltaTime;

        float s = 1f + Mathf.Sin(t * pulseSpeed) * pulseScale;   
        transform.localScale = baseScale * s;

        var c = sr.color;                                        
        c.a = Mathf.Lerp(alphaMin, alphaMax, (Mathf.Sin(t * pulseSpeed) + 1f) * 0.5f);
        sr.color = c;
    }

    public void PointRight() => transform.rotation = Quaternion.Euler(0, 0, rightZ);
    public void PointLeft()  => transform.rotation = Quaternion.Euler(0, 0, leftZ);

    public void Show(bool show)
    {
        if (sr != null) sr.enabled = show;
        SetLinkedActive(show);
    }

    void SetLinkedActive(bool active)
    {
        if (linkedVisuals == null) return;
        for (int i = 0; i < linkedVisuals.Length; i++)
            if (linkedVisuals[i] != null) linkedVisuals[i].SetActive(active);
    }
}
