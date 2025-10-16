using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerLightController : MonoBehaviour
{
    public Light2D playerLight;
    public float moveRadius = 2.8f;
    public float idleRadius = 1.2f;
    public float smoothSpeed = 3f;

    private Vector2 lastPosition;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        bool isMoving = (Vector2)transform.position != lastPosition;
        float targetOuterRadius = isMoving ? moveRadius : idleRadius;

        playerLight.pointLightOuterRadius = Mathf.Lerp(
            playerLight.pointLightOuterRadius,
            targetOuterRadius,
            Time.deltaTime * smoothSpeed
        );

        lastPosition = transform.position;
    }
}