using UnityEngine;

public class ZzzBillboard : MonoBehaviour
{
    private Transform player;
    private float floatSpeed = 2f;
    private float floatHeight = 0.05f;
    private float yOffset = 0.9f; // 🔽 reduced gap to sit closer to player

    void Start()
    {
        // Cache the player's transform (parent)
        player = transform.parent;
        transform.rotation = Quaternion.identity;
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Keep rotation fixed upright (ignore player rotation)
        transform.rotation = Quaternion.identity;

        // Lock position just above player's world position
        float floatOffset = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        Vector3 targetPos = player.position + new Vector3(0f, yOffset + floatOffset, 0f);
        transform.position = targetPos;
    }
}