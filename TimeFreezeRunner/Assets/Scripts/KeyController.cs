using UnityEngine;
using UnityEngine.SceneManagement;

public class KeyController : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Destroy(gameObject);
        LevelManager.I?.MarkDarkMazeCleared();

        if (LevelManager.I != null)
        {
            var s = LevelManager.I.savedState;
            s.position = other.transform.position;
            s.lastScene = SceneManager.GetActiveScene().name;
            s.nextScene = "Corridor";
        }

        Debug.Log("🔑 Key collected — returning to Corridor...");
        SceneManager.LoadScene("Corridor");
    }
}