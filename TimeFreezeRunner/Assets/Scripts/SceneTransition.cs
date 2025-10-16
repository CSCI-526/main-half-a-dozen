using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public string targetScene;
    public bool autoLoad = false;
    public float delayBeforeLoad = 1f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Transition triggered → Loading {targetScene}");
            StartCoroutine(LoadScene());
        }
    }

    private System.Collections.IEnumerator LoadScene()
    {
        if (LevelManager.I != null && GameManager.I != null)
{
    var s = LevelManager.I.savedState;
    s.position = GameManager.I.player.transform.position;
    s.coinsCollected = GameManager.I.coinsCollected;
    s.exitUnlocked = GameManager.I.exitDoor != null && GameManager.I.exitDoor.isActiveAndEnabled;
    s.lastScene = SceneManager.GetActiveScene().name;
    s.nextScene = targetScene;

    Debug.Log($"💾 Saved state: {s.lastScene} → {s.nextScene}, position={s.position}");
}

        yield return new WaitForSeconds(delayBeforeLoad);
        SceneManager.LoadScene(targetScene);
    }
}