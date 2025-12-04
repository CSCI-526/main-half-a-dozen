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
    [Header("UI – Enemy Wipe Tutorial")]
    public TMPro.TMP_Text enemyWipeHintText;
    [Header("UI – Enemy Wipe Countdown")]
    public TMPro.TMP_Text enemyWipeCountdownText;

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

    public bool IsPlayerMoving => !_enemyWipeTutorialLocked && player != null && player.isMoving;
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
    

    [Header("Enemy Wipe Warning Settings")]
    [SerializeField] float warningLeadTimeSeconds = 2f;  // last 2 seconds show ghosts

    // runtime list of ghost warning enemies
    List<GameObject> _enemyWarningGhosts = new List<GameObject>();

    [HideInInspector]
    public bool ignoreTeleportUse = false;

    // NEW: Level 3 Enemy Wipe tutorial state (per run)
    bool _enemyWipeTutorialPending = false;      // we still need to show & run the free tutorial use
    bool _enemyWipeTutorialPromptShown = false;  // we already showed "Press Shift..." once
    bool _enemyWipeTutorialLocked = false;       // movement/enemies are locked until first Enemy Wipe in tutorial
    

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

        // NEW: Enemy Wipe tutorial is only available the first time the player reaches Level 3
        // _enemyWipeTutorialLocked = false;
        // _enemyWipeTutorialPromptShown = false;
        // _enemyWipeTutorialPending = enableNukePower &&
        //                             LevelManager.I != null &&
        //                             !LevelManager.I.hasSeenEnemyWipeTutorial;

        // NEW: Enemy Wipe tutorial is only available the first time the player reaches Level 3
        _enemyWipeTutorialLocked = false;
        _enemyWipeTutorialPromptShown = false;
        _enemyWipeTutorialPending = enableNukePower &&
                                    LevelManager.I != null &&
                                    !LevelManager.I.hasSeenEnemyWipeTutorial;

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

            // NEW: don't punish idle while the tutorial has movement locked
            if (!_enemyWipeTutorialLocked && !IsPlayerMoving && !switching)
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

            // NEW: When Level 3 tutorial starts and the player first moves,
            // show the hint and LOCK player movement + enemies
            if (enableNukePower &&
                _enemyWipeTutorialPending &&
                !_enemyWipeTutorialPromptShown &&
                IsPlayerMoving)
            {
                _enemyWipeTutorialPromptShown = true;
                _enemyWipeTutorialLocked = true;

                if (enemyWipeHintText != null)
                {
                    enemyWipeHintText.text = "Press SHIFT to use free Enemy Wipe!";
                    enemyWipeHintText.gameObject.SetActive(true);
                }
                if (player != null)
                    player.enabled = false;   // disable PlayerController so they can't move
                FreezeAllEnemies(true);       // optional: freeze enemies too, so it's safe
            }

            if (enableNukePower && !_nukeBusy && Time.time >= _nukeReadyAt)
            {
                bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                if (shiftPressed)
                {
                    // NEW: First-ever use in Level 3 is a free tutorial wipe
                    if (_enemyWipeTutorialPending)
                    {
                        _enemyWipeTutorialPending = false;
                        _enemyWipeTutorialLocked = false;
                        if (enemyWipeHintText != null)
                            enemyWipeHintText.gameObject.SetActive(false);

                        // Remember globally that the player has learned Enemy Wipe
                        if (LevelManager.I != null)
                            LevelManager.I.hasSeenEnemyWipeTutorial = true;

                        // Re-enable player movement
                        if (player != null)
                            player.enabled = true;

                        // Enemies will be destroyed by the wipe anyway; new ones will spawn active
                        StartCoroutine(NukeEnemiesAndRespawn_Tutorial());
                        ui?.ShowIdleToast("Enemy Wipe: free try. Next uses add enemies.");
                        Debug.Log("💥 Enemy Wipe tutorial (free) triggered with Shift!");
                    }
                    else if (currentNukeUses >= maxNukeUses)
                    {
                        ui?.ShowIdleToast("No more Enemy Wipes left!");
                    }
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

    // void UpdateEnemyWipeUI()
    // {
    //     if (enemyWipeText == null) return;
    //     enemyWipeText.text = string.Format(enemyWipeFormat, currentNukeUses, maxNukeUses);
    // }

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

        // NEW: capture an enemy prefab template to use for ghosts/fallback spawns
        if (_enemyTemplateHiddenClone == null && enemies.Length > 0)
        {
            _enemyTemplateHiddenClone = Instantiate(enemies[0].gameObject);
            _enemyTemplateHiddenClone.name = "EnemyTemplate";
            _enemyTemplateHiddenClone.SetActive(false);  // keep template hidden
        }

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
        if (!enableNukePower) yield break;
        _nukeBusy = true;
        // NEW: tell the player the wipe is active
        // ui?.ShowIdleToast("Enemies wiped for 5s.");
         // show "Enemies respawning in 5...4..." countdown
        UpdateEnemyWipeCountdown(killDurationSeconds);
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

        float elapsed = 0f;
        bool ghostsSpawned = false;
        List<Vector2> added = null;

        while (elapsed < killDurationSeconds)
        {
            elapsed += Time.deltaTime;
            float remaining = killDurationSeconds - elapsed;

            UpdateEnemyWipeCountdown(remaining);

            // spawn ghosts in the last warningLeadTimeSeconds seconds
            if (!ghostsSpawned && remaining <= warningLeadTimeSeconds)
            {
                ghostsSpawned = true;
                SpawnEnemyWarningGhosts();

                // ALSO spawn the extra enemies now so they can blink during the warning window
                if (extraEnemiesPerUse > 0 && added == null)
                {
                    added = TrySpawnerSpawnExtra(extraEnemiesPerUse)
                            ?? FallbackSpawnExtraFromTemplate(extraEnemiesPerUse);

                    if (added == null)
                        added = new List<Vector2>();

                    // track them as part of baseline for future wipes
                    _baselineEnemyPositions.AddRange(added);

                    // make the new enemies harmless + blinking until the wipe ends
                    if (added.Count > 0)
                        StartCoroutine(BlinkNewEnemiesSafe(added));
                }
            }

            // blink ghosts while they are visible
            if (ghostsSpawned)
            {
                float blink = Mathf.Abs(Mathf.Sin(Time.time * 8f));    // speed of blink
                float alpha = Mathf.Lerp(0.15f, 0.6f, blink);          // min/max alpha

                foreach (var ghost in _enemyWarningGhosts)
                {
                    if (ghost == null) continue;
                    var sr = ghost.GetComponentInChildren<SpriteRenderer>();
                    if (sr == null) continue;

                    var c = sr.color;
                    c.a = alpha;
                    sr.color = c;
                }
            }

            yield return null;
        }

        // clear live blinking extras before respawn
        var liveEnemies = FindObjectsOfType<EnemyChaser>();
        for (int i = 0; i < liveEnemies.Length; i++)
        {
            if (liveEnemies[i])
                Destroy(liveEnemies[i].gameObject);
        }

        if (_baselineEnemyPositions.Count > 0)
        {
            if (!TrySpawnerSpawnAtPositions(_baselineEnemyPositions))
            {
                for (int i = 0; i < _baselineEnemyPositions.Count; i++)
                    SpawnFromTemplate(_baselineEnemyPositions[i]);
            }
        }
 
        // clear warning ghosts and hide countdown
        ClearEnemyWarningGhosts();
        UpdateEnemyWipeCountdown(0f);

        // (optional) tell player how many enemies joined
        if (added != null && added.Count > 0)
        {
            ui?.ShowIdleToast($"+{added.Count} enemies joined.");
        }
        _nukeBusy = false;
    }

    // NEW: Tutorial variant – same wipe window, but does NOT consume a use
    // and does NOT add extra enemies afterwards.
    // NEW: Tutorial variant – same wipe window, but does NOT consume a use
// and does NOT add extra enemies afterwards.
    IEnumerator NukeEnemiesAndRespawn_Tutorial()
    {
        if (!enableNukePower) yield break;
        _nukeBusy = true;

        {
            string levelName = "Level3";  
            float logTime = LevelTimer.IsRunning ? LevelTimer.Elapsed : Time.timeSinceLevelLoad;
            AnalyticsLogger.I?.LogPowerUpUse(levelName, "EnemyWipe", logTime);
        }

        // NOTE: do NOT change currentNukeUses or UI here – this is a free tutorial use
        _nukeReadyAt = Time.time + nukeCooldownSeconds + killDurationSeconds;

        var enemies = FindObjectsOfType<EnemyChaser>();
        for (int i = 0; i < enemies.Length; i++)
            if (enemies[i]) Destroy(enemies[i].gameObject);

        float elapsed = 0f;
        bool ghostsSpawned = false;
        while (elapsed < killDurationSeconds)
        {
            elapsed += Time.deltaTime;
            float remaining = killDurationSeconds - elapsed;

            UpdateEnemyWipeCountdown(remaining);

            if (!ghostsSpawned && remaining <= warningLeadTimeSeconds)
            {
                ghostsSpawned = true;
                SpawnEnemyWarningGhosts();
            }

            if (ghostsSpawned)
            {
                float blink = Mathf.Abs(Mathf.Sin(Time.time * 8f));
                float alpha = Mathf.Lerp(0.15f, 0.6f, blink);

                foreach (var ghost in _enemyWarningGhosts)
                {
                    if (ghost == null) continue;
                    var sr = ghost.GetComponentInChildren<SpriteRenderer>();
                    if (sr == null) continue;

                    var c = sr.color;
                    c.a = alpha;
                    sr.color = c;
                }
            }

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

        // no extra enemies on tutorial, but clean up visuals
        ClearEnemyWarningGhosts();
        UpdateEnemyWipeCountdown(0f);

        // Important: no extra enemies spawned for the tutorial use
        _nukeBusy = false;
    }

    void UpdateEnemyWipeCountdown(float remainingSeconds)
    {
        if (enemyWipeCountdownText == null) return;

        if (remainingSeconds > 0f)
        {
            int seconds = Mathf.CeilToInt(remainingSeconds);
            enemyWipeCountdownText.text = $"Enemies respawning in {seconds}...";
            if (!enemyWipeCountdownText.gameObject.activeSelf)
                enemyWipeCountdownText.gameObject.SetActive(true);
        }
        else
        {
            if (enemyWipeCountdownText.gameObject.activeSelf)
                enemyWipeCountdownText.gameObject.SetActive(false);
        }
    }

    void SpawnEnemyWarningGhosts()
    {
        if (_baselineEnemyPositions == null || _baselineEnemyPositions.Count == 0) return;
        if (_enemyTemplateHiddenClone == null) return;

        // clear any old ghosts first
        ClearEnemyWarningGhosts();

        foreach (var pos in _baselineEnemyPositions)
        {
            var ghost = Instantiate(_enemyTemplateHiddenClone, pos, Quaternion.identity);
            ghost.name = "EnemyWarningGhost";
            ghost.SetActive(true);

            // disable AI & collisions so they're just visuals
            var chaser = ghost.GetComponent<EnemyChaser>();
            if (chaser != null) chaser.enabled = false;

            var col2D = ghost.GetComponent<Collider2D>();
            if (col2D != null) col2D.enabled = false;

            // stop physics so ghosts don't fall
            var rb2D = ghost.GetComponent<Rigidbody2D>();
            if (rb2D != null)
            {
                rb2D.velocity = Vector2.zero;
                rb2D.angularVelocity = 0f;
                rb2D.gravityScale = 0f;
                rb2D.bodyType = RigidbodyType2D.Kinematic;
            }

            // make them faded
            var sr = ghost.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                var c = sr.color;
                c.a = 0.35f;
                sr.color = c;
            }

            _enemyWarningGhosts.Add(ghost);
        }
    }

    void ClearEnemyWarningGhosts()
    {
        for (int i = 0; i < _enemyWarningGhosts.Count; i++)
        {
            if (_enemyWarningGhosts[i] != null)
                Destroy(_enemyWarningGhosts[i]);
        }
        _enemyWarningGhosts.Clear();
    }

    IEnumerator BlinkNewEnemiesSafe(List<Vector2> spawnPositions)
    {
        if (spawnPositions == null || spawnPositions.Count == 0) yield break;

        // Find the EnemyChaser objects that were spawned at these positions
        var allEnemies = FindObjectsOfType<EnemyChaser>();
        var targets = new List<EnemyChaser>();
        const float MAX_DIST = 0.4f;

        foreach (var pos in spawnPositions)
        {
            EnemyChaser best = null;
            float bestDist = MAX_DIST;

            foreach (var e in allEnemies)
            {
                if (!e) continue;
                float d = Vector2.Distance(pos, (Vector2)e.transform.position);
                if (d < bestDist && !targets.Contains(e))
                {
                    best = e;
                    bestDist = d;
                }
            }

            if (best != null)
                targets.Add(best);
        }

        if (targets.Count == 0) yield break;

        // Put them into a harmless "ghost" state
        var renderers = new List<SpriteRenderer>();
        foreach (var e in targets)
        {
            if (!e) continue;

            // disable AI
            e.enabled = false;

            // disable hitbox
            var col = e.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            // stop gravity making them fall
            var rb = e.GetComponent<Rigidbody2D>();
            if (rb != null) rb.gravityScale = 0f;

            var sr = e.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) renderers.Add(sr);
        }

        // Blink for a short warning window (same as your warningLeadTimeSeconds)
        float duration = warningLeadTimeSeconds;  // e.g. 2 seconds
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float blink = Mathf.Abs(Mathf.Sin(Time.time * 8f));
            float alpha = Mathf.Lerp(0.15f, 0.6f, blink);

            foreach (var sr in renderers)
            {
                if (!sr) continue;
                var c = sr.color;
                c.a = alpha;
                sr.color = c;
            }

            yield return null;
        }

        // Restore them as normal enemies
        foreach (var e in targets)
        {
            if (!e) continue;

            var col = e.GetComponent<Collider2D>();
            if (col != null) col.enabled = true;

            e.enabled = true; // AI back on

            var sr = e.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                var c = sr.color;
                c.a = 1f;
                sr.color = c;
            }
        }
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

