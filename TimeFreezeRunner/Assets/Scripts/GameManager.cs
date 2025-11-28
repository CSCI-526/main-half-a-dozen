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
    public TMPro.TMP_Text enemyWipeText;               // drag your counter TMP here
    public string enemyWipeFormat = "Enemy Wipe: {0}/{1}"; // {0} = used, {1} = max

    [Header("Counts")]
    public int totalCoins;
    public int coinsCollected;

    [Header("Idle Settings")]
    public float idleThreshold = 3f;
    float idleTimer = 0f;
    int idleWarnings = 0;

    public bool IsPlayerMoving => player != null && player.isMoving;
    public bool IsPlaying { get; private set; } = false;

    [Header("Level 3 – Nuke Power")]
    [SerializeField] bool enableNukePower = false;  
    [SerializeField] float killDurationSeconds = 5f;
    [SerializeField] int extraEnemiesPerUse = 2;
    [SerializeField] float nukeCooldownSeconds = 0f;

    [SerializeField] int maxNukeUses = 2;      
    int currentNukeUses = 0;

    bool _nukeBusy = false;
    float _nukeReadyAt = 0f;
    List<Vector2> _baselineEnemyPositions = new List<Vector2>();
    List<Vector2> _baselineCoinPositions = new List<Vector2>();
    EnemySpawner _spawner;
    GameObject _enemyTemplateHiddenClone;
    GameObject _coinTemplateHiddenClone;
    bool _wasIntroVisible = false;

    [HideInInspector]
    public bool ignoreTeleportUse = false;


    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;

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

                    if (LevelManager.I != null && LevelManager.I.currentLevel == 2)
                    {
                        if (LevelManager.I.savedState.allCoinsCollected)
                        {
                            foreach (var coin in FindObjectsOfType<Coin>())
                                coin.gameObject.SetActive(false);

                            ui?.SetCoin(totalCoins, totalCoins);
                            Debug.Log("💰 All coins already collected — hiding them.");
                        }
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
                {
                    corridorTrigger.gameObject.SetActive(false);
                    Debug.Log("🚪 Corridor trigger disabled — key collected.");
                }
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
                {
                    ui?.ShowHowTo(true);
                    Debug.Log("📋 Main scene: Showing howToPanel first");
                }
                else if (sceneName == "Level2_DarkMaze")
                {
                    Debug.Log("🌑 Dark Maze scene detected - calling UILevelPanel.ShowIntro(2)");
                    UILevelPanel.ShowIntro(2);
                }
                else
                {
                    UILevelPanel.ShowIntro(LevelManager.I.currentLevel);
                }
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
            Debug.LogWarning("⚠️ LevelManager missing! Game starting directly.");
            StartGame();
        }

        enableNukePower = (LevelManager.I != null && LevelManager.I.currentLevel == 3);
        currentNukeUses = 0;
        UpdateEnemyWipeUI();   // initialize indicator to 0 / max

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
                if (idleTimer >= idleThreshold)
                {
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
            else idleTimer = 0f;

            if (enableNukePower && !_nukeBusy && Time.time >= _nukeReadyAt)
            {
                bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                if (!shiftPressed && (Event.current != null && Event.current.shift))
                    shiftPressed = true;

                if (shiftPressed)
                {
                    if (currentNukeUses >= maxNukeUses)
                    {
                        ui?.ShowIdleToast("No more Enemy Wipes left!");   // keep this one
                    }
                    else
                    {
                        StartCoroutine(NukeEnemiesAndRespawn());
                        Debug.Log("💥 Enemy Wipe triggered with Shift!");
                    }
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

        string currentScene = SceneManager.GetActiveScene().name;
        if (LevelManager.I != null && currentScene == "Level2_DarkMaze")
        {
            LevelManager.I.savedState.lastScene = "Level2_DarkMaze";
            Debug.Log("💾 Preserving Dark Maze retry state.");
        }
        DeathEventTracker.I?.LogDeathAt(player != null ? player.transform.position : Vector3.zero);

        player?.OnLose();

        // ⭐ Reset tutorial immediately when player dies
        FindObjectOfType<Level1InGameTutorial>()?.ResetTutorialState();
        
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

        if (currentScene == "Level2_DarkMaze")
        {
            if (LevelManager.I != null)
            {
                LevelManager.I.savedState.lastScene = "Level2_DarkMaze";
            }
            Debug.Log("🔄 Restarting Dark Maze only (keeping Level2 progress).");
            SceneManager.LoadScene("Level2_DarkMaze");
            return;
        }

        if (currentScene == "MainForLevel2")
        {
            Debug.Log("🔄 Restarting MainForLevel2 scene.");
            SceneManager.LoadScene("MainForLevel2");
            return;
        }

        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
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

    public void ResetCoinsToOriginalPositions()
    {
        var existingCoins = FindObjectsOfType<Coin>();
        foreach (var coin in existingCoins)
            if (coin != null) Destroy(coin.gameObject);

        if (_baselineCoinPositions.Count > 0 && _coinTemplateHiddenClone != null)
        {
            for (int i = 0; i < _baselineCoinPositions.Count; i++)
            {
                var coin = Instantiate(_coinTemplateHiddenClone, _baselineCoinPositions[i], Quaternion.identity);
                coin.name = "Coin";
                coin.SetActive(true);
            }
        }
        totalCoins = _baselineCoinPositions.Count;
    }

    bool IsUIBlockingInput()
    {
        if (ui != null)
        {
            if (ui.howToPanel != null && ui.howToPanel.activeInHierarchy)
                return true;

            if ((ui.winPanel != null && ui.winPanel.activeInHierarchy) ||
                (ui.losePanel != null && ui.losePanel.activeInHierarchy))
                return true;
        }
        return false;
    }

    IEnumerator CaptureInitialEnemyPositionsEndOfFrame()
    {
        yield return null;
        _baselineEnemyPositions.Clear();
        _baselineCoinPositions.Clear();

        var enemies = FindObjectsOfType<EnemyChaser>();
        for (int i = 0; i < enemies.Length; i++)
            if (enemies[i]) _baselineEnemyPositions.Add(enemies[i].transform.position);

        var coins = FindObjectsOfType<Coin>();
        for (int i = 0; i < coins.Length; i++)
            if (coins[i]) _baselineCoinPositions.Add(coins[i].transform.position);

        if (enemies.Length > 0 && enemies[0] != null && _enemyTemplateHiddenClone == null)
        {
            _enemyTemplateHiddenClone = Instantiate(enemies[0].gameObject);
            _enemyTemplateHiddenClone.name = "[EnemyTemplate_Hidden]";
            _enemyTemplateHiddenClone.SetActive(false);
            _enemyTemplateHiddenClone.hideFlags = HideFlags.HideInHierarchy;
        }

        if (coins.Length > 0 && coins[0] != null && _coinTemplateHiddenClone == null)
        {
            _coinTemplateHiddenClone = Instantiate(coins[0].gameObject);
            _coinTemplateHiddenClone.name = "[CoinTemplate_Hidden]";
            _coinTemplateHiddenClone.SetActive(false);
            _coinTemplateHiddenClone.hideFlags = HideFlags.HideInHierarchy;
        }
    }

    IEnumerator NukeEnemiesAndRespawn()
    {
        if (!enableNukePower) yield break;
        _nukeBusy = true;
        currentNukeUses++;

        {
            string levelName = "Level3";  
            float logTime = LevelTimer.IsRunning ? LevelTimer.Elapsed : Time.timeSinceLevelLoad;
            AnalyticsLogger.I?.LogPowerUpUse(levelName, "EnemyWipe", logTime);
        }

        // update indicator instead of showing usage toast
        UpdateEnemyWipeUI();
        _nukeReadyAt = Time.time + nukeCooldownSeconds + killDurationSeconds;

        var enemies = FindObjectsOfType<EnemyChaser>();
        for (int i = 0; i < enemies.Length; i++)
            if (enemies[i]) Destroy(enemies[i].gameObject);

        float t = 0f;
        while (t < killDurationSeconds)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (_baselineEnemyPositions.Count > 0)
        {
            if (!TrySpawnerSpawnAtPositions(_baselineEnemyPositions))
            {
                for (int i = 0; i < _baselineEnemyPositions.Count; i++)
                    SpawnFromTemplate(_baselineEnemyPositions[i]);
            }
        }

        var added = TrySpawnerSpawnExtra(extraEnemiesPerUse)
                    ?? FallbackSpawnExtraFromTemplate(extraEnemiesPerUse);

        _baselineEnemyPositions.AddRange(added);
        // removed "+X enemies joined" toast to avoid extra enemy-wipe messages
        _nukeBusy = false;
    }

    bool TrySpawnerSpawnAtPositions(IEnumerable<Vector2> positions)
    {
        if (_spawner == null) _spawner = FindObjectOfType<EnemySpawner>();
        if (_spawner == null) return false;
        var mi = _spawner.GetType().GetMethod("SpawnAtPositions", new System.Type[] { typeof(IEnumerable<Vector2>) });
        if (mi == null) return false;
        mi.Invoke(_spawner, new object[] { positions });
        return true;
    }

    List<Vector2> TrySpawnerSpawnExtra(int count)
    {
        if (_spawner == null) _spawner = FindObjectOfType<EnemySpawner>();
        if (_spawner == null) return null;
        var mi = _spawner.GetType().GetMethod("SpawnExtra", new System.Type[] { typeof(int) });
        if (mi == null) return null;
        var result = mi.Invoke(_spawner, new object[] { count });
        return result as List<Vector2>;
    }

    List<Vector2> FallbackSpawnExtraFromTemplate(int count)
    {
        var list = new List<Vector2>();
        if (count <= 0) return list;
        var playerPos = (player != null) ? (Vector2)player.transform.position : Vector2.zero;
        int placed = 0;
        int attempts = 0;
        const int MAX_ATTEMPTS = 200;

        while (placed < count && attempts < MAX_ATTEMPTS)
        {
            attempts++;
            float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float r = 8f + Random.Range(-1.5f, 1.5f);
            Vector2 pos = playerPos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
            bool far = true;
            for (int i = 0; i < _baselineEnemyPositions.Count; i++)
                if (Vector2.Distance(_baselineEnemyPositions[i], pos) < 2.5f)
                { far = false; break; }
            if (!far) continue;
            SpawnFromTemplate(pos);
            list.Add(pos);
            placed++;
        }
        return list;
    }

    void SpawnFromTemplate(Vector2 pos)
    {
        if (_enemyTemplateHiddenClone != null)
        {
            var go = Instantiate(_enemyTemplateHiddenClone, pos, Quaternion.identity);
            go.name = "Enemy(Clone)";
            go.SetActive(true);
            var ch = go.GetComponent<EnemyChaser>();
            if (ch != null && ch.player == null && player != null)
                ch.player = player.transform;
        }
    }

    void UpdateEnemyWipeUI()
    {
        if (enemyWipeText == null) return;
        enemyWipeText.text = string.Format(enemyWipeFormat, currentNukeUses, maxNukeUses);
    }
}
