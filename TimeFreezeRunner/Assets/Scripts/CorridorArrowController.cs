using UnityEngine;
using TMPro;

public class CorridorArrowController : MonoBehaviour
{
    [Header("Arrow References")]
    public GameObject leftArrow;
    public GameObject rightArrow;

    [Header("Spawn Points")]
    public Transform spawnFromLevel2;
    public Transform spawnFromDarkMaze;

    [Header("Optional Local Hint Fallback")]
    public TextMeshProUGUI hintTextTMP;  

    void Start()
    {
        StartCoroutine(InitAfterDelay());
    }

    private System.Collections.IEnumerator InitAfterDelay()
    {
        yield return new WaitForSeconds(0.15f);

        if (GameManager.I == null)
        {
            GameManager gm = FindObjectOfType<GameManager>();
            if (gm != null)
            {
                GameManager.I = gm;
                Debug.Log("✅ GameManager reference restored in Corridor.");
            }
            else
            {
                Debug.LogWarning("⚠️ GameManager missing — will use local TMP fallback for hints.");
            }
        }

        if (!leftArrow) leftArrow = GameObject.Find("LeftArrow");
        if (!rightArrow) rightArrow = GameObject.Find("RightArrow");

        if (GameManager.I != null && GameManager.I.player != null)
        {
            Transform spawn = LevelManager.I.darkMazeCleared
                ? spawnFromDarkMaze
                : spawnFromLevel2;
            GameManager.I.player.transform.position = spawn.position;
        }

        UpdateArrows();

        yield return new WaitForSeconds(0.2f);
        ShowHint();
    }

    private void UpdateArrows()
    {
        if (LevelManager.I == null) return;

        bool cleared = LevelManager.I.darkMazeCleared;
        if (leftArrow) leftArrow.SetActive(cleared);
        if (rightArrow) rightArrow.SetActive(!cleared);
    }

    private void ShowHint()
    {
        string msg = LevelManager.I.darkMazeCleared
            ? "Key secured! Head back toward the green exit door."
            : "The Dark Maze awaits, venture ahead!";

        if (GameManager.I != null && GameManager.I.ui != null)
        {
            GameManager.I.ui.ShowIdleToast(msg, 9f);
        }
        else if (hintTextTMP != null)
        {
            hintTextTMP.text = msg;
            hintTextTMP.gameObject.SetActive(true);
            CancelInvoke(nameof(HideHint));
            Invoke(nameof(HideHint), 4f);
        }
    }

    private void HideHint()
    {
        if (hintTextTMP != null)
            hintTextTMP.gameObject.SetActive(false);
    }
}