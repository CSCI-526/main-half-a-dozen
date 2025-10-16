using UnityEngine;

public class ExitDoorController : MonoBehaviour
{
    public Collider2D doorCollider;
    public SpriteRenderer doorSprite;
    public float openDuration = 2f;
    public float closedDuration = 2f;

    private bool cycling = false;

    public void ActivateDoorCycle()
    {
        if (!cycling) StartCoroutine(OpenCloseLoop());
    }

    private System.Collections.IEnumerator OpenCloseLoop()
    {
        cycling = true;
        while (true)
        {
            // OPEN
            doorCollider.enabled = false;
            doorSprite.color = Color.green;
            yield return new WaitForSeconds(openDuration);

            // CLOSE
            doorCollider.enabled = true;
            doorSprite.color = Color.red;
            yield return new WaitForSeconds(closedDuration);
        }
    }
}