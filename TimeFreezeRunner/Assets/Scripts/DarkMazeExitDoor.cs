using UnityEngine;
using UnityEngine.SceneManagement;

public class DarkMazeExitDoor : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            LevelManager.I.darkMazeCleared = true;
            SceneManager.LoadScene("Main"); // back to main maze, door unlocked
        }
    }
}