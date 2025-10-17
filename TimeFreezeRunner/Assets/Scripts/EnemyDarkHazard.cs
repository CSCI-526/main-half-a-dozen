using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyDarkHazard : MonoBehaviour
{
    [Header("Colors")]
    public Color activeColor = Color.red;                         
    public Color frozenColor = new Color(0.6f, 0.1f, 0.1f, 0.7f); 

    [Header("Visibility by Light Distance (to player)")]
    [Tooltip("Inside this radius: fully visible")]
    public float fullVisibleRadius = 1.6f;
    [Tooltip("Beyond this radius: fully hidden")]
    public float fullyHiddenRadius = 3.5f;

    [Tooltip("Optional: set to your player's light transform; if left empty, will use GameManager.I.player")]
    public Transform lightSource;

    private SpriteRenderer sr;
    private CircleCollider2D col;
    private Rigidbody2D rb;
    private bool armed = false;

    void Awake()
    {
        sr  = GetComponent<SpriteRenderer>();
        col = GetComponent<CircleCollider2D>();
        rb  = GetComponent<Rigidbody2D>();

        var mat = Shader.Find("Sprites/Default");
        if (mat != null) sr.material = new Material(mat);

        sr.sortingOrder = 30;
        sr.color = frozenColor * new Color(1,1,1,0); 

        col.isTrigger = true;
        rb.bodyType   = RigidbodyType2D.Kinematic;
        rb.simulated  = true;

        if (fullyHiddenRadius < fullVisibleRadius)
            fullyHiddenRadius = fullVisibleRadius + 0.01f;
    }

    void Update()
    {
        bool isPlaying = (GameManager.I != null && GameManager.I.IsPlaying);
        if (isPlaying != armed) armed = isPlaying;

        var baseCol = armed ? activeColor : frozenColor;

        Transform src = lightSource;
        if (src == null && GameManager.I != null && GameManager.I.player != null)
            src = GameManager.I.player.transform;
        if (src == null)
        {
            sr.color = baseCol * new Color(1,1,1,0f);
            return;
        }

        float dist = Vector2.Distance(transform.position, src.position);

        float vis;
        if (dist <= fullVisibleRadius) vis = 1f;
        else if (dist >= fullyHiddenRadius) vis = 0f;
        else
        {
            float t = Mathf.InverseLerp(fullyHiddenRadius, fullVisibleRadius, dist);
            vis = Mathf.SmoothStep(0f, 1f, t);
        }

        Color c = baseCol;
        c.a = c.a * vis;
        sr.color = c;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!armed) return;                  
        if (!other.CompareTag("Player")) return;
        GameManager.I?.OnPlayerCaught();     
    }

   
    void OnCollisionEnter2D(Collision2D other)
    {
        if (!armed) return;
        if (!other.collider.CompareTag("Player")) return;
        GameManager.I?.OnPlayerCaught();
    }
}
