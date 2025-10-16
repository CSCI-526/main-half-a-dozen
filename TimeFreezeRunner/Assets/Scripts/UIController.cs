using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [Header("HUD")]
    public TMP_Text coinText;

    [Header("Panels")]
    public GameObject howToPanel;
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("Hints (optional)")]
    public GameObject startHint;
    public GameObject exitHint;

    [Header("Idle UI (optional)")]
    public GameObject idleToast;
    public TMP_Text idleToastText;
    public GameObject idleFailPanel;
    public TMP_Text idleFailText;

    [Header("Buttons")]
    public Button playAgainButton; // ✅ assign your green “Restart” button here in Canvas (Inspector)

    void Start()
    {
        // Hide everything on start
        if (howToPanel) howToPanel.SetActive(false);
        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        if (exitHint) exitHint.SetActive(false);
        if (idleToast) idleToast.SetActive(false);
        if (idleFailPanel) idleFailPanel.SetActive(false);
        if (playAgainButton) playAgainButton.gameObject.SetActive(false);
    }

    public void SetCoin(int total, int have)
    {
        if (coinText) coinText.text = $"Coins: {have}/{total}";
    }

    // ✅ Win panel logic — controls Play Again visibility
    public void ShowWin()
    {
        if (!winPanel) return;
        winPanel.SetActive(true);

        // Hide by default
        if (playAgainButton)
            playAgainButton.gameObject.SetActive(false);

        // Show “Play Again” only after Level 2 or higher
        if (LevelManager.I != null && LevelManager.I.currentLevel >= 2)
        {
            playAgainButton?.gameObject.SetActive(true);
            Debug.Log("🎮 Showing Play Again button (Level 2+).");
        }
        else
        {
            Debug.Log("🕹️ Level 1 win — Play Again hidden.");
        }
    }

    public void ShowLose()
    {
        if (losePanel) losePanel.SetActive(true);
    }

    public void ShowHowTo(bool on)
    {
        if (howToPanel) howToPanel.SetActive(on);
    }

    public void HideHowTo()
    {
        if (howToPanel) howToPanel.SetActive(false);
    }

    public void ShowStartHint()
    {
        if (!startHint) return;
        startHint.SetActive(true);
        StartCoroutine(HideOnFirstMove());
    }

    System.Collections.IEnumerator HideOnFirstMove()
    {
        yield return null;
        while (GameManager.I != null && !GameManager.I.IsPlayerMoving)
            yield return null;
        if (startHint) startHint.SetActive(false);
    }

    public void ShowExitHint()
    {
        if (exitHint) exitHint.SetActive(true);
    }

    public void ShowIdleToast(string msg, float duration = 1.75f)
    {
        if (!idleToast || !idleToastText) return;
        idleToastText.text = msg;
        idleToast.SetActive(true);
        StopCoroutine(nameof(HideIdleToastAfter));
        StartCoroutine(HideIdleToastAfter(duration));
    }

    System.Collections.IEnumerator HideIdleToastAfter(float t)
    {
        yield return new WaitForSeconds(t);
        if (idleToast) idleToast.SetActive(false);
    }

    public void ShowIdleFail(string msg)
    {
        if (idleFailPanel && idleFailText)
        {
            idleFailText.text = msg;
            idleFailPanel.SetActive(true);
        }
        else
        {
            ShowIdleToast(msg, 1.2f);
        }
    }

    public void OnStartClicked() => GameManager.I?.StartGame();
    
    public void OnRestartClicked() => GameManager.I?.Restart();

    // ✅ Play Again logic — resets full game from Level 1
    public void OnPlayAgainClicked()
    {
        Debug.Log("🔁 Restarting game from Level 1...");
        if (LevelManager.I != null)
            LevelManager.I.ResetProgress();

        SceneManager.LoadScene("Main");
    }
}