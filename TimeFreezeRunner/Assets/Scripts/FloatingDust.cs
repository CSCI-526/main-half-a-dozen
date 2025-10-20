using UnityEngine;

public class FloatingDust : MonoBehaviour
{
    public float speed = 0.2f;
    public float range = 0.5f;
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position + new Vector3(Random.Range(-1f,1f), Random.Range(-1f,1f), 0);
        transform.position = startPos;
    }

    void Update()
    {
        float offsetX = Mathf.Sin(Time.time * speed + startPos.x) * range;
        float offsetY = Mathf.Cos(Time.time * speed * 1.2f + startPos.y) * range;
        transform.position = startPos + new Vector3(offsetX, offsetY, 0);
    }
}