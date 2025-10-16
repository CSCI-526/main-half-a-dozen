using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ExitDoor : MonoBehaviour
{
    public bool isActive = false;
    public SpriteRenderer doorSprite;
    public Color lockedColor = new Color(1f, 0.5f, 0f, 0.6f);
    public Color activeColor = new Color(0.2f, 0.8f, 0.2f, 1f);

    void Awake()
    {
        if (!doorSprite) doorSprite = GetComponent<SpriteRenderer>();
        UpdateColor();
    }

    public void ActivateExit(bool active)
    {
        isActive = active;
        UpdateColor();
    }

    void UpdateColor()
    {
        if (doorSprite) doorSprite.color = isActive ? activeColor : lockedColor;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;
        if (other.attachedRigidbody && other.attachedRigidbody.GetComponent<PlayerController>())
        {
            Debug.Log("Player Reached Exit");
            GameManager.I?.OnPlayerWin();

            if (LevelManager.I != null && LevelManager.I.currentLevel == 1)
            LevelManager.I.OnLevelComplete();
        }
    }
}
