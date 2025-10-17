using UnityEngine;

public class DarkRoomExitTrigger : MonoBehaviour
{
    private bool triggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        var arrowMgr = FindObjectOfType<CorridorArrowManager>();
        if (arrowMgr != null)
        {
            arrowMgr.OnDarkRoomComplete();
            Debug.Log("Player exited dark room ");
        }
    }
}
