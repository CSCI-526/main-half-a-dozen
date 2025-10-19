using UnityEngine;

public class ArrowIndicator : MonoBehaviour
{
    private Vector3 startPos;

    void Start() { startPos = transform.position; }

    void Update()
    {
        // gentle bobbing
        transform.position = startPos + new Vector3(0, Mathf.Sin(Time.time * 2f) * 0.15f, 0);
        // constant facing up
        transform.rotation = Quaternion.Euler(0, 0, -180);
    }
}