using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager I;

    [Header("Refs")]
    public PlayerController player;
    public UIController ui;
    public ExitDoor exitDoor;

    [Header("UI – Enemy Wipe Indicator")]
    public TMPro.TMP_Text enemyWipeText;
    public string enemyWipeFormat = "Enemy Wipe: {0}/{1}";

    [Header("Counts")]
    public int totalCoins;
    public int coinsCollected;

    [Header("Idle Settings")]
    public float idleThreshold = 5f; // ⏳ changed to 5 seconds
    private float idleTimer = 0f;
    private int idleWarnings = 0;

    [Header("Idle Feedback")]
    public GameObject zzzBubblePrefab;
    private GameObject activeZzz;

    public bool IsPlayerMoving => player != null && player.isMoving;
    public bool IsPlaying { get; private set; } = false;

    [Header("Level 3 – Nuke Power")]
    [SerializeField] bool enableNukePower = false;
    [SerializeField] float killDurationSeconds = 5f;
    [SerializeField] int extraEnemiesPerUse = 2;
    [SerializeField] float nukeCooldownSeconds = 0f;

    [SerializeField] int maxNukeUses = 2;
    private int currentNukeUses = 0;

    private bool _nukeBusy = false;
    private float _nukeReadyAt = 0f;
    private List<Vector2> _baselineEnemyPositions = new List<Vector2>();
    private List<Vector2> _baselineCoinPositions = new List<Vector2>();
    private EnemySpawner _spawner;
    private GameObject _enemyTemplateHiddenClone;
    private GameObject _coinTemplateHiddenClone;
    private bool _wasIntroVisible = false;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        // ----------------------------
// 💥 LEVEL 3 CLEAN START FIX
// ----------------------------
if (LevelManager.I != null && LevelManager.I.currentLevel == 3)
{
    Debug.Log("🧹 Level 3 start — resetting all state.");

    // Reset coin values
    coinsCollected = 0;
    totalCoins = FindObjectsOfType<Coin>().Length;
    ui?.SetCoin(totalCoins, coinsCollected);

    // Reset exit door (should start locked)
    if (exitDoor != null)
        exitDoor.ActivateExit(false);

    // Reset teleports
    LevelManager.I.switchesUsed = 0;

    // Clear saved state so Level 3 is not treated as "returning"
    LevelManager.I.savedState = new LevelManager.PlayerState();

    // Force Level 3 intro
    UILevelPanel.ShowIntro(3);
}

        // 🧠 Reset position switch count when entering a new main level
        if (LevelManager.I != null && LevelManager.I.savedState != null)
        {
            // Detect fresh entry scene like "MainForLevel2", "MainForLevel3", etc.
            if (sceneName.StartsWith("MainForLevel") && string.IsNullOrEmpty(LevelManager.I.savedState.lastScene))
            {
                LevelManager.I.savedState.switchesUsed = 0;
                Debug.Log("🔁 Resetting position switch count for new level start.");
            }
        }

        if (sceneName == "Level2_DarkMaze")
        {
            if (ui != null && ui.coinText != null)
                ui.coinText.gameObject.SetActive(false);
        }
        else
        {
            totalCoins = FindObjectsOfType<Coin>().Length;
            ui?.SetCoin(totalCoins, coinsCollected);
        }

        if (exitDoor) exitDoor.ActivateExit(false);
        FreezeAllEnemies(true);

        if (LevelManager.I != null)
        {
            var s = LevelManager.I.savedState;
            if (s != null && s.position != Vector3.zero)
            {
                if (s.lastScene == "Corridor" || s.lastScene == "Level2_DarkMaze")
                {
                    player.transform.position = s.position;
                    coinsCollected = s.coinsCollected;
                    ui?.SetCoin(totalCoins, coinsCollected);

                    // if (LevelManager.I.currentLevel == 2 && LevelManager.I.savedState.allCoinsCollected)
                    if (LevelManager.I.currentLevel == 2 && LevelManager.I.savedState.allCoinsCollected 
    && LevelManager.I.savedState.lastScene == "Level2_DarkMaze")
                    {
                        foreach (var coin in FindObjectsOfType<Coin>())
                            coin.gameObject.SetActive(false);
                        ui?.SetCoin(totalCoins, totalCoins);
                        Debug.Log("💰 All coins already collected — hiding them.");
                    }

                    if (s.exitUnlocked && exitDoor != null)
                        exitDoor.ActivateExit(true);

                    Debug.Log($"♻️ Restored from {s.lastScene} → {SceneManager.GetActiveScene().name}");
                }
            }

            if (LevelManager.I.currentLevel == 2 && LevelManager.I.darkMazeCleared)
                exitDoor?.ActivateExit(true);

            if (LevelManager.I.darkMazeCleared)
            {
                var corridorTrigger = FindObjectOfType<SceneTransition>();
                if (corridorTrigger != null)
                    corridorTrigger.gameObject.SetActive(false);
                Debug.Log("🚪 Corridor trigger disabled — key collected.");
            }
        }

        if (LevelManager.I != null)
        {
            var s = LevelManager.I.savedState;
            bool returning = s != null &&
                             (s.lastScene == "Corridor" || s.lastScene == "Level2_DarkMaze");

            if (!returning)
            {
                if (sceneName == "Main")
                    ui?.ShowHowTo(true);
                else if (sceneName == "Level2_DarkMaze")
                    UILevelPanel.ShowIntro(2);
                else
                    UILevelPanel.ShowIntro(LevelManager.I.currentLevel);
            }
            else
            {
                Debug.Log($"↩️ Returning to {SceneManager.GetActiveScene().name} — skipping level intro.");
                StartGame();
            }
        }
        else
        {
            ui?.ShowHowTo(true);
            StartGame();
        }

        enableNukePower = (LevelManager.I != null && LevelManager.I.currentLevel == 3);
        currentNukeUses = 0;
        UpdateEnemyWipeUI();

        _spawner = FindObjectOfType<EnemySpawner>();
        StartCoroutine(CaptureInitialEnemyPositionsEndOfFrame());
        _wasIntroVisible = UILevelPanel.IsIntroVisible;

        if (IdleFailTracker.Instance != null)
        {
            IdleFailTracker.Instance.idleThresholdSec = idleThreshold;
            IdleFailTracker.Instance.OnLevelStarted();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            Restart();

        bool nowIntro = UILevelPanel.IsIntroVisible;
        if (_wasIntroVisible && !nowIntro && !IsPlaying && !IsUIBlockingInput())
            StartGame();
        _wasIntroVisible = nowIntro;
        if (nowIntro) return;

        if (!IsPlaying && !IsUIBlockingInput() &&
            (Input.GetKeyDown(KeyCode.Space) ||
             Input.GetKeyDown(KeyCode.Return) ||
             Input.GetMouseButtonDown(0)))
        {
            StartGame();
        }

        if (IsPlaying)
        {
            bool switching = PositionSwitchSystem.IsTargetingGlobal;
            if (!IsPlayerMoving && !switching)
            {
                idleTimer += Time.deltaTime;

                // 💤 Spawn Zzz bubble after 1 second idle
                if (idleTimer >= 1f && activeZzz == null && player != null && zzzBubblePrefab != null)
                {
                    Vector3 pos = player.transform.position + Vector3.up * 1.2f;
                    activeZzz = Instantiate(zzzBubblePrefab, pos, Quaternion.identity);
                    activeZzz.AddComponent<ZzzBillboard>(); // keep upright + float
                    activeZzz.transform.SetParent(player.transform, true);
                }

                if (idleTimer >= idleThreshold)
                {
                    // Destroy bubble once warning triggers
                    if (activeZzz != null)
                    {
                        Destroy(activeZzz);
                        activeZzz = null;
                    }

                    if (idleWarnings == 0)
                    {
                        idleWarnings = 1;
                        ui?.ShowIdleToast("Oops. Thinking a bit long! Keep moving. (1/2)");
                        idleTimer = 0f;
                    }
                    else
                    {
                        IsPlaying = false;
                        ui?.ShowIdleFail("Stopped twice too long, restarting…");
                        IdleFailTracker.Instance?.OnIdleDeath();
                        StartCoroutine(RestartAfter(3f));
                    }
                }
            }
            else
            {
                idleTimer = 0f;
                // Reset bubble when movement resumes
                if (activeZzz != null)
                {
                    Destroy(activeZzz);
                    activeZzz = null;
                }
            }

            if (enableNukePower && !_nukeBusy && Time.time >= _nukeReadyAt)
            {
                bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                if (shiftPressed)
                {
                    if (currentNukeUses >= maxNukeUses)
                        ui?.ShowIdleToast("No more Enemy Wipes left!");
                    else
                        StartCoroutine(NukeEnemiesAndRespawn());
                }
            }
        }
    }

    public void StartGame()
    {
        if (IsPlaying) return;
        IsPlaying = true;
        ui?.HideHowTo();
        ui?.ShowStartHint();
        FreezeAllEnemies(false);
        idleTimer = 0f;
        idleWarnings = 0;
        RunAttemptTracker.I?.StartAttempt();

        string lvl = $"Level{LevelManager.I.currentLevel}";
        AnalyticsLogger.I?.StartNewRun(lvl);
        LevelTimer.Begin();
    }

    public void OnCoinCollected()
    {
        coinsCollected++;
        ui?.SetCoin(totalCoins, coinsCollected);
        if (LevelManager.I != null)
            LevelManager.I.savedState.coinsCollected = coinsCollected;
        if (coinsCollected < totalCoins) return;
        if (LevelManager.I != null)
            LevelManager.I.savedState.allCoinsCollected = true;

        if (LevelManager.I.currentLevel == 1)
        {
            exitDoor?.ActivateExit(true);
            ui?.ShowExitHint();
            return;
        }

        if (LevelManager.I.currentLevel == 2)
        {
            var corridorTrigger = FindObjectOfType<SceneTransition>();
            if (corridorTrigger != null) corridorTrigger.gameObject.SetActive(true);
            ui?.ShowIdleToast("🔍 Explore the right-side passage!");
            return;
        }

        if (LevelManager.I.currentLevel == 3)
        {
            exitDoor?.ActivateExit(true);
            ui?.ShowExitHint();
        }
    }

    public void OnPlayerCaught()
    {
        if (!IsPlaying) return;
        IsPlaying = false;
        RunAttemptTracker.I?.LogRunEndFail();
        DeathEventTracker.I?.LogDeathAt(player != null ? player.transform.position : Vector3.zero);
        player?.OnLose();
        ui?.ShowLose();
        FreezeAllEnemies(true);
    }

    public void OnPlayerWin()
    {
        if (!IsPlaying) return;
        IsPlaying = false;
        RunAttemptTracker.I?.LogRunEndSuccess();
        player?.OnWin();
        ui?.ShowWin();
        FreezeAllEnemies(true);
        LevelManager.I?.OnLevelComplete();
    }

    public void Restart()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }

    IEnumerator RestartAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        Restart();
    }

    public void FreezeAllEnemies(bool frozen)
    {
        foreach (var e in FindObjectsOfType<EnemyChaser>())
            e.SetFrozenVisual(frozen);
    }

    void UpdateEnemyWipeUI()
    {
        if (enemyWipeText == null) return;
        enemyWipeText.text = string.Format(enemyWipeFormat, currentNukeUses, maxNukeUses);
    }

    // IEnumerator CaptureInitialEnemyPositionsEndOfFrame()
    // {
    //     yield return null;
    //     _baselineEnemyPositions.Clear();
    //     _baselineCoinPositions.Clear();
    //     foreach (var e in FindObjectsOfType<EnemyChaser>())
    //         _baselineEnemyPositions.Add(e.transform.position);
    //     foreach (var c in FindObjectsOfType<Coin>())
    //         _baselineCoinPositions.Add(c.transform.position);
    // }
    IEnumerator CaptureInitialEnemyPositionsEndOfFrame()
    {
        yield return null;

        _baselineEnemyPositions.Clear();
        _baselineCoinPositions.Clear();

        var enemies = FindObjectsOfType<EnemyChaser>();
        var coins = FindObjectsOfType<Coin>();

        // Save original enemy positions
        foreach (var e in enemies)
            _baselineEnemyPositions.Add(e.transform.position);

        // Save original coin positions
        foreach (var c in coins)
            _baselineCoinPositions.Add(c.transform.position);

        // 🔥 FIX: Capture a coin prefab template to clone later
        if (coins.Length > 0)
        {
            _coinTemplateHiddenClone = Instantiate(coins[0].gameObject);
            _coinTemplateHiddenClone.name = "CoinTemplate";
            _coinTemplateHiddenClone.SetActive(false);   // hide template
        }
    }

    IEnumerator NukeEnemiesAndRespawn()
    {
        if (!enableNukePower || _nukeBusy) yield break;
        _nukeBusy = true;

        currentNukeUses++;
        UpdateEnemyWipeUI();

        _nukeReadyAt = Time.time + nukeCooldownSeconds + killDurationSeconds;

        // Capture all enemies alive right now
        var enemies = new List<EnemyChaser>(FindObjectsOfType<EnemyChaser>());

        // Save their positions so we can reuse them for spawning
        var spawnPositions = new List<Vector3>();
        foreach (var e in enemies)
        {
            if (e != null)
                spawnPositions.Add(e.transform.position);
        }

        // If somehow no enemies, just bail out
        if (spawnPositions.Count == 0)
        {
            _nukeBusy = false;
            yield break;
        }

        // TEMPORARILY disable them
        foreach (var e in enemies)
        {
            if (e != null)
                e.gameObject.SetActive(false);
        }

        // “Gone for 5 seconds”
        yield return new WaitForSeconds(killDurationSeconds);

        // RE-ENABLE them (they return!)
        foreach (var e in enemies)
        {
            if (e != null)
                e.gameObject.SetActive(true);
        }

        // ⬇️ EXTRA PART: spawn +2 enemies (or whatever extraEnemiesPerUse is) each time

        // Use the first valid enemy as a template to clone
        EnemyChaser template = null;
        foreach (var e in enemies)
        {
            if (e != null)
            {
                template = e;
                break;
            }
        }

        if (template != null && extraEnemiesPerUse > 0)
        {
            for (int i = 0; i < extraEnemiesPerUse; i++)
            {
                // Pick a random existing spawn position
                Vector3 basePos = spawnPositions[Random.Range(0, spawnPositions.Count)];

                // Small random offset so they don’t all stack on top of each other
                Vector2 offset2D = Random.insideUnitCircle * 1.5f;
                Vector3 spawnPos = basePos + new Vector3(offset2D.x, offset2D.y, 0f);

                GameObject clone = Instantiate(template.gameObject, spawnPos, Quaternion.identity);
                clone.SetActive(true);

                var ch = clone.GetComponent<EnemyChaser>();
                if (ch != null && player != null)
                    ch.player = player.transform;
            }
        }

        _nukeBusy = false;
    }


    bool IsUIBlockingInput()
    {
        if (ui == null) return false;
        return (ui.howToPanel != null && ui.howToPanel.activeInHierarchy)
            || (ui.winPanel != null && ui.winPanel.activeInHierarchy)
            || (ui.losePanel != null && ui.losePanel.activeInHierarchy);
    }

    // 🔁 restore Level-1 reset helpers so UIController works
    public void ResetEnemiesToOriginalPositions()
    {
        var enemies = FindObjectsOfType<EnemyChaser>();
        if (_baselineEnemyPositions.Count > 0 && enemies.Length > 0)
        {
            for (int i = 0; i < enemies.Length && i < _baselineEnemyPositions.Count; i++)
            {
                if (enemies[i] != null)
                    enemies[i].transform.position = _baselineEnemyPositions[i];
            }
        }
        FreezeAllEnemies(true);
    }

    // public void ResetCoinsToOriginalPositions()
    // {
    //     var existingCoins = FindObjectsOfType<Coin>();
    //     foreach (var coin in existingCoins)
    //         if (coin != null) Destroy(coin.gameObject);

    //     if (_baselineCoinPositions.Count > 0 && _coinTemplateHiddenClone != null)
    //     {
    //         for (int i = 0; i < _baselineCoinPositions.Count; i++)
    //         {
    //             var coin = Instantiate(_coinTemplateHiddenClone, _baselineCoinPositions[i], Quaternion.identity);
    //             coin.name = "Coin";
    //             coin.SetActive(true);
    //         }
    //     }
    //     totalCoins = _baselineCoinPositions.Count;
    // }
    public void ResetCoinsToOriginalPositions()
{
    // Remove old coins
    foreach (var coin in FindObjectsOfType<Coin>())
        Destroy(coin.gameObject);

    // Safety: If template missing, do nothing
    if (_coinTemplateHiddenClone == null) return;

    // Re-spawn coins in original spots
    foreach (var pos in _baselineCoinPositions)
    {
        var newCoin = Instantiate(_coinTemplateHiddenClone, pos, Quaternion.identity);
        newCoin.name = "Coin";
        newCoin.SetActive(true);
    }

    totalCoins = _baselineCoinPositions.Count;
    coinsCollected = 0;
}
}