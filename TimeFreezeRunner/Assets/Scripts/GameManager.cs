// LEVEL 3 TESTING CODE

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

    [Header("Counts")]
    public int totalCoins;
    public int coinsCollected;

    [Header("Idle Settings")]
    public float idleThreshold = 3f;
    float idleTimer = 0f;
    int idleWarnings = 0;

    public bool IsPlayerMoving => player != null && player.isMoving;
    public bool IsPlaying { get; private set; } = false;

    // ===== Level 3 Nuke Power (Add-only) =====
    [Header("Level 3 – Nuke Power")]
    [SerializeField] bool enableNukePower = false;   // auto-enabled only for Level 3
    [SerializeField] KeyCode killKey = KeyCode.K;
    [SerializeField] float killDurationSeconds = 5f;
    [SerializeField] int extraEnemiesPerUse = 2;
    [SerializeField] float nukeCooldownSeconds = 0f;
    // NEW: Limit uses
    [SerializeField] int maxNukeUses = 2;        // player can use K twice per level
    int currentNukeUses = 0;

    bool _nukeBusy = false;
    float _nukeReadyAt = 0f;
    List<Vector2> _baselineEnemyPositions = new List<Vector2>();
    EnemySpawner _spawner;
    GameObject _enemyTemplateHiddenClone;

    

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

        FreezeAllEnemies(true);

        // ✅ Restore player & scene state intelligently
        if (LevelManager.I != null)
        {
            var s = LevelManager.I.savedState;

            if (s != null && s.position != Vector3.zero)
            {
                // Only restore when returning from Corridor or DarkMaze
                if (s.lastScene == "Corridor" || s.lastScene == "Level2_DarkMaze")
                {
                    player.transform.position = s.position;
                    coinsCollected = s.coinsCollected;
                    ui?.SetCoin(totalCoins, coinsCollected);

                    // ✅ Hide all coins if already collected in Level 2
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

            // If coming back to Level 2 and dark maze cleared, open exit
            if (LevelManager.I.currentLevel == 2 && LevelManager.I.darkMazeCleared)
                exitDoor?.ActivateExit(true);

            // 🧭 Disable corridor trigger after key is collected
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

        // 🔸 Show intro ONLY if entering a fresh level, not returning from Corridor/DarkMaze
        if (LevelManager.I != null)
        {
            var s = LevelManager.I.savedState;
            bool returning = s != null &&
                             (s.lastScene == "Corridor" || s.lastScene == "Level2_DarkMaze");

            if (!returning)
            {
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
            Debug.LogWarning("⚠️ LevelManager missing! Game starting directly.");
            StartGame();
        }

        // 🔸 NEW (nuke): enable only on Level 3
        enableNukePower = (LevelManager.I != null && LevelManager.I.currentLevel == 3);

        // 🔸 reset use counter each level load
        currentNukeUses = 0;

        // 🔸 Cache spawner (if present) and capture baseline next frame
        _spawner = FindObjectOfType<EnemySpawner>();
        StartCoroutine(CaptureInitialEnemyPositionsEndOfFrame());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            Restart();

        // Prevent starting while intro is visible
        if (UILevelPanel.IsIntroVisible)
            return;

        if (!IsPlaying && !IsUIBlockingInput() && (Input.GetKeyDown(KeyCode.Space) ||
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

            // 🔸 NEW (nuke): Level-3-only K power
            if (enableNukePower && Input.GetKeyDown(killKey) && !_nukeBusy && Time.time >= _nukeReadyAt)
            {
                // stop if no uses left
                if (currentNukeUses >= maxNukeUses)
                {
                    ui?.ShowIdleToast("No more Enemy Wipes left!");
                }
                else
                {
                    StartCoroutine(NukeEnemiesAndRespawn());
                }
            }
        }
    }

    public void StartGame()
    {
        Debug.Log($"StartGame called! IsPlaying: {IsPlaying}");
        if (IsPlaying) 
        {
            Debug.Log("Game is already playing, ignoring StartGame call");
            return;
        }
        Debug.Log("Starting game...");
        IsPlaying = true;
        ui?.HideHowTo();
        ui?.ShowStartHint();
        FreezeAllEnemies(false);
        idleTimer = 0f;
        idleWarnings = 0;
        Debug.Log("Game started successfully!");
    }

    public void OnCoinCollected()
    {
        coinsCollected++;
        ui?.SetCoin(totalCoins, coinsCollected);

        // 🔹 Save live progress
        if (LevelManager.I != null)
            LevelManager.I.savedState.coinsCollected = coinsCollected;

        // Stop if not all coins yet
        if (coinsCollected < totalCoins) return;

        // ✅ Mark all coins collected — ensures coins won’t reappear
        if (LevelManager.I != null)
            LevelManager.I.savedState.allCoinsCollected = true;

        // ✅ LEVEL 1 — normal unlock
        if (LevelManager.I != null && LevelManager.I.currentLevel == 1)
        {
            exitDoor?.ActivateExit(true);
            ui?.ShowExitHint();
            Debug.Log("✅ Level 1: Exit door unlocked!");
            return;
        }

        // ✅ LEVEL 2 — corridor trigger unlock, door still locked
        if (LevelManager.I != null && LevelManager.I.currentLevel == 2)
        {
            var corridorTrigger = FindObjectOfType<SceneTransition>();
            if (corridorTrigger != null)
            {
                corridorTrigger.gameObject.SetActive(true);

                // optional visual pulse
                var sr = corridorTrigger.GetComponent<SpriteRenderer>();
                if (sr != null) StartCoroutine(PulseColor(sr));
            }

            ui?.ShowIdleToast("🔍 Explore the right-side passage!");
            Debug.Log("🟡 Level 2: Corridor trigger unlocked — door stays locked until key!");
        }
    }

    IEnumerator PulseColor(SpriteRenderer sr)
    {
        Color baseColor = sr.color;
        float t = 0f;
        while (sr != null)
        {
            sr.color = baseColor * (1.0f + Mathf.Sin(t * 4f) * 0.3f);
            t += Time.deltaTime;
            yield return null;
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

        // Trigger Level Complete
        LevelManager.I?.OnLevelComplete();
    }

    public void Restart()
    {
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
    bool IsUIBlockingInput()
        {
            // Check if any UI panels are active that should prevent game start
            if (ui != null)
            {
                // Check if how-to panel is active (this should block keyboard input to prevent game start)
                if (ui.howToPanel != null && ui.howToPanel.activeInHierarchy)
                {
                    Debug.Log("How-to panel is active, blocking keyboard input to prevent game start");
                    return true; // Block keyboard input when how-to panel is active
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

    // ===================== Level 3 Nuke Power (impl) =====================

    // Wait one frame so spawner (if any) has spawned; then record baseline positions.
    IEnumerator CaptureInitialEnemyPositionsEndOfFrame()
    {
        yield return null;

        _baselineEnemyPositions.Clear();

        var enemies = FindObjectsOfType<EnemyChaser>();
        for (int i = 0; i < enemies.Length; i++)
            if (enemies[i]) _baselineEnemyPositions.Add(enemies[i].transform.position);

        // Fallback template: keep a hidden clone to instantiate if spawner lacks helpers.
        if (enemies.Length > 0 && enemies[0] != null && _enemyTemplateHiddenClone == null)
        {
            _enemyTemplateHiddenClone = Instantiate(enemies[0].gameObject);
            _enemyTemplateHiddenClone.name = "[EnemyTemplate_Hidden]";
            _enemyTemplateHiddenClone.SetActive(false);
            _enemyTemplateHiddenClone.hideFlags = HideFlags.HideInHierarchy;
        }
    }

    IEnumerator NukeEnemiesAndRespawn()
    {
        if (!enableNukePower) yield break;

        _nukeBusy = true;

        // track and display usage
        currentNukeUses++;
        ui?.ShowIdleToast($"Enemy Wipe {currentNukeUses}/{maxNukeUses} used");

        _nukeReadyAt = Time.time + nukeCooldownSeconds + killDurationSeconds;

        // 1) Despawn all live enemies
        var enemies = FindObjectsOfType<EnemyChaser>();
        for (int i = 0; i < enemies.Length; i++)
            if (enemies[i]) Destroy(enemies[i].gameObject);

        ui?.ShowIdleToast($"Enemies gone for {killDurationSeconds:0}s…");

        // 2) Wait safe window
        float t = 0f;
        while (t < killDurationSeconds)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // 3) Respawn baseline positions via spawner if possible; else via template
        if (_baselineEnemyPositions.Count > 0)
        {
            if (!TrySpawnerSpawnAtPositions(_baselineEnemyPositions))
            {
                for (int i = 0; i < _baselineEnemyPositions.Count; i++)
                    SpawnFromTemplate(_baselineEnemyPositions[i]);
            }
        }
        else
        {
            // if no baseline yet, try to read any enemies now (some other system may have created them)
            var after = FindObjectsOfType<EnemyChaser>();
            for (int i = 0; i < after.Length; i++)
                if (after[i]) _baselineEnemyPositions.Add(after[i].transform.position);
        }

        // 4) Add penalty: exactly +2 (or whatever set in Inspector)
        var added = TrySpawnerSpawnExtra(extraEnemiesPerUse)
                    ?? FallbackSpawnExtraFromTemplate(extraEnemiesPerUse);

        _baselineEnemyPositions.AddRange(added);

        if (added.Count > 0)
            ui?.ShowIdleToast($"+{added.Count} enemies joined!");

        _nukeBusy = false;
    }

    // Spawner-first helpers (via reflection so we don’t require specific method signatures in this branch)
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

    // Template-based fallback if spawner doesn’t expose helpers in this branch
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

            // keep a little distance from baseline so they don't overlap instantly
            bool far = true;
            for (int i = 0; i < _baselineEnemyPositions.Count; i++)
            {
                if (Vector2.Distance(_baselineEnemyPositions[i], pos) < 2.5f)
                { far = false; break; }
            }
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

}
