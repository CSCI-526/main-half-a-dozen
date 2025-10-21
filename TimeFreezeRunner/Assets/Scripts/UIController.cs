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
    public Button playAgainButton; // green “Restart” button on Win panel

    // Optional: if you want to assign specific buttons in Inspector, you can.
    [SerializeField] private Button howToNextButton;     // Level 3: Next on How-To

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

        SetupLevel3Flow(); // safe no-op on other levels
    }

    // ========== Level 3: How-To → Start Game ==========
    void SetupLevel3Flow()
    {
        if (LevelManager.I == null || LevelManager.I.currentLevel != 3) return;

        // Wire the "Next" button on the How-To panel → start game directly
        if (howToNextButton == null && howToPanel != null)
        {
            // Prefer a specifically named child; otherwise use the first Button under howToPanel
            howToNextButton = FindChildButton(howToPanel.transform, "NextButton");
            if (howToNextButton == null)
                howToNextButton = howToPanel.GetComponentInChildren<Button>(true);
        }
        if (howToNextButton != null)
        {
            howToNextButton.onClick.RemoveAllListeners();
            howToNextButton.onClick.AddListener(OnHowToNextClicked);
        }
        else
        {
            Debug.Log("UIController (L3): No How-To Next button found. (Name it 'NextButton' or assign in Inspector.)");
        }
    }

    Button FindChildButton(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrEmpty(childName)) return null;
        var t = parent.Find(childName);
        return t ? t.GetComponent<Button>() : null;
    }

    // Called by Level 3 How-To "Next" button
    public void OnHowToNextClicked()
    {
        // Close How-To and start game directly
        if (howToPanel) howToPanel.SetActive(false);
        GameManager.I?.StartGame();
    }

    // =======================================================================

    public void SetCoin(int total, int have)
    {
        if (coinText) coinText.text = $"Coins: {have}/{total}";
    }

    // ✅ Win panel logic — controls Play Again visibility
    public void ShowWin()
    {
        if (!winPanel) return;
        winPanel.SetActive(true);

        if (playAgainButton)
            playAgainButton.gameObject.SetActive(false);

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

        // Re-hook Level 3 flow whenever How-To is shown
        if (on) SetupLevel3Flow();
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

    // Inspector-friendly handlers (still valid for Level 1 flow)
    public void OnStartClicked() => GameManager.I?.StartGame();
    public void OnRestartClicked() 
    {
        // For Level 1, go directly to Level 1 panel instead of restarting scene
        if (LevelManager.I != null && LevelManager.I.currentLevel == 1)
        {
            if (losePanel) losePanel.SetActive(false);
            
            // Reset game state for fresh start
            if (LevelManager.I != null)
            {
                LevelManager.I.ResetProgress();
                LevelManager.I.currentLevel = 1;
            }
            
            if (GameManager.I != null)
            {
                // Reset GameManager state
                GameManager.I.coinsCollected = 0;
                
                // Reset player position to starting position
                if (GameManager.I.player != null)
                {
                    GameManager.I.player.transform.position = Vector3.zero;
                }
                
                // Reset coins to original positions
                GameManager.I.ResetCoinsToOriginalPositions();
                
                // Reset enemies to original positions and freeze them
                GameManager.I.ResetEnemiesToOriginalPositions();
                
                // Reset position switch chances to 2
                var positionSwitchSystem = FindObjectOfType<PositionSwitchSystem>();
                if (positionSwitchSystem != null)
                {
                    positionSwitchSystem.ResetPositionSwitchChances();
                }
                
                // Update UI
                GameManager.I.ui?.SetCoin(GameManager.I.totalCoins, 0);
                
                // Reset exit door
                if (GameManager.I.exitDoor != null)
                    GameManager.I.exitDoor.ActivateExit(false);
            }
            
            UILevelPanel.ShowIntro(1);
        }
        else
        {
            // For other levels, restart the scene as usual
            GameManager.I?.Restart();
        }
    }
    
    // Show Level 1 panel after howToPanel Next button click
    public void OnHowToNextShowLevel1()
    {
        if (howToPanel) howToPanel.SetActive(false);
        UILevelPanel.ShowIntro(1);
    }

    // ✅ Play Again logic — resets full game from Level 1
    public void OnPlayAgainClicked()
    {
        Debug.Log("🔁 Restarting game from Level 1...");
        if (LevelManager.I != null)
            LevelManager.I.ResetProgress();

        SceneManager.LoadScene("Main");
    }
}