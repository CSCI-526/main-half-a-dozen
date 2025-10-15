using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager I;

    [Header("Refs")]
    public PlayerController player;
    public UIController ui;
    public ExitDoor exitDoor;

    [Header("Counts")]
    public int totalCoins;
    public int coinsCollected;

    [Header("Idle Settings")]
    public float idleThreshold = 3f; 
    float idleTimer = 0f;
    int idleWarnings = 0;

    public bool IsPlayerMoving => player != null && player.isMoving;
    public bool IsPlaying { get; private set; } = false;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    void Start()
    {
        totalCoins = FindObjectsOfType<Coin>().Length;
        ui?.SetCoin(totalCoins, coinsCollected);
        if (exitDoor) exitDoor.ActivateExit(false);

        ui?.ShowHowTo(true);
        FreezeAllEnemies(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) Restart();

        // Only respond to input if no UI panels are blocking interaction
        if (!IsPlaying && !IsUIBlockingInput() && (Input.GetKeyDown(KeyCode.Space) ||
                           Input.GetKeyDown(KeyCode.Return) ||
                           Input.GetMouseButtonDown(0)))
        {
            StartGame();
        }

        if (IsPlaying)
        {
            if (!IsPlayerMoving)
            {
                idleTimer += Time.deltaTime;
                if (idleTimer >= idleThreshold)
                {
                    if (idleWarnings == 0)
                    {
                        idleWarnings = 1;
                        ui?.ShowIdleToast("Oops—thinking a bit long! Keep moving. (1/2)");
                        idleTimer = 0f;
                    }
                    else
                    {
                        IsPlaying = false; 
                        ui?.ShowIdleFail("Stopped twice too long—restarting…");
                        StartCoroutine(RestartAfter(1.25f));
                    }
                }
            }
            else
            {
                idleTimer = 0f;
            }
        }
    }

    public void StartGame()
    {
        Debug.Log("StartGame called!");
        if (IsPlaying) return;
        IsPlaying = true;
        ui?.HideHowTo();
        ui?.ShowStartHint();
        FreezeAllEnemies(false);
        idleTimer = 0f;
        idleWarnings = 0;
    }

    public void OnCoinCollected()
    {
        coinsCollected++;
        ui?.SetCoin(totalCoins, coinsCollected);
        if (coinsCollected >= totalCoins)
        {
            exitDoor?.ActivateExit(true);
            ui?.ShowExitHint();
        }
    }

    public void OnPlayerCaught()
    {
        if (!IsPlaying) return;
        IsPlaying = false;
        player?.OnLose();
        ui?.ShowLose();
        FreezeAllEnemies(true);
    }

    public void OnPlayerWin()
    {
        if (!IsPlaying) return;
        IsPlaying = false;
        player?.OnWin();
        ui?.ShowWin();
        FreezeAllEnemies(true);
    }

    public void Restart()
    {
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    System.Collections.IEnumerator RestartAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        Restart();
    }

    public void FreezeAllEnemies(bool frozen)
    {
        foreach (var e in FindObjectsOfType<EnemyChaser>())
            e.SetFrozenVisual(frozen);
    }

        bool IsUIBlockingInput()
        {
            // Check if any UI panels are active that should prevent game start
            if (ui != null)
            {
                // Check if how-to panel is active (this should block input to prevent game start)
                if (ui.howToPanel != null && ui.howToPanel.activeInHierarchy)
                {
                    Debug.Log("How-to panel is active, blocking input to prevent game start");
                    return true; // Block input when how-to panel is active
                }
                
                // Check if win/lose panels are active
                if ((ui.winPanel != null && ui.winPanel.activeInHierarchy) ||
                    (ui.losePanel != null && ui.losePanel.activeInHierarchy))
                {
                    Debug.Log("Win/Lose panel is active, blocking input");
                    return true; // Block input when game is over
                }
            }
            
            // Check if player perks panel is active (this should block game start)
            var panelSwitcher = FindObjectOfType<PanelSwitcher>();
            if (panelSwitcher != null && panelSwitcher.playerPerksPanel != null && 
                panelSwitcher.playerPerksPanel.activeInHierarchy)
            {
                Debug.Log("Player perks panel is active, blocking input");
                return true; // Block input when perks panel is showing
            }
            
            Debug.Log("No UI blocking input");
            return false;
        }
}
