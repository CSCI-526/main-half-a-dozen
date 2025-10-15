using UnityEngine;
using TMPro;

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

    void Start()
    {
        if (winPanel)  winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        if (exitHint)  exitHint.SetActive(false);
        if (idleToast) idleToast.SetActive(false);
        if (idleFailPanel) idleFailPanel.SetActive(false);
    }

    public void SetCoin(int total, int have)
    {
        if (coinText) coinText.text = $"Coins: {have}/{total}";
    }

    public void ShowWin()  { if (winPanel)  winPanel.SetActive(true);  }
    public void ShowLose() { if (losePanel) losePanel.SetActive(true); }

    public void ShowHowTo(bool on) { if (howToPanel) howToPanel.SetActive(on); }
    public void HideHowTo()        { if (howToPanel) howToPanel.SetActive(false); }

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

    public void ShowExitHint() { if (exitHint) exitHint.SetActive(true); }

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
    public void OnStartClicked()   
    {
        Debug.Log("OnStartClicked called!");
        GameManager.I?.StartGame();
    }
    public void OnRestartClicked() => GameManager.I?.Restart();
}
