// using UnityEngine;
// using UnityEngine.SceneManagement;

// public class GameManager : MonoBehaviour
// {
//     public static GameManager I;

//     [Header("Refs")]
//     public PlayerController player;
//     public UIController ui;
//     public ExitDoor exitDoor;

//     [Header("Counts")]
//     public int totalCoins;
//     public int coinsCollected;

//     // === Add in GameManager fields ===
//     [Header("Enemy Nuke Power")]
//     public KeyCode killKey = KeyCode.K;
//     public float killDurationSeconds = 5f;
//     public int extraEnemiesPerUse = 4;

//     bool _nukeBusy = false;
//     readonly System.Collections.Generic.List<Vector2> _baselineEnemyPositions = new();
//     EnemySpawner _spawner;


//     [Header("Idle Settings")]
//     public float idleThreshold = 3f; 
//     float idleTimer = 0f;
//     int idleWarnings = 0;

//     public bool IsPlayerMoving => player != null && player.isMoving;
//     public bool IsPlaying { get; private set; } = false;

//     void Awake()
//     {
//         if (I != null && I != this) { Destroy(gameObject); return; }
//         I = this;
//     }

//     void Start()
//     {
//         totalCoins = FindObjectsOfType<Coin>().Length;
//         ui?.SetCoin(totalCoins, coinsCollected);
//         if (exitDoor) exitDoor.ActivateExit(false);

//         ui?.ShowHowTo(true);
//         FreezeAllEnemies(true);
//         _spawner = FindObjectOfType<EnemySpawner>();
//         StartCoroutine(CaptureInitialEnemyPositionsEndOfFrame());
//     }

//     void Update()
//     {
//         if (Input.GetKeyDown(KeyCode.R)) Restart();

//         if (!IsPlaying && (Input.GetKeyDown(KeyCode.Space) ||
//                            Input.GetKeyDown(KeyCode.Return) ||
//                            Input.GetMouseButtonDown(0)))
//         {
//             StartGame();
//         }

//         if (IsPlaying)
//         {
//             if (!IsPlayerMoving)
//             {
//                 idleTimer += Time.deltaTime;
//                 if (idleTimer >= idleThreshold)
//                 {
//                     if (idleWarnings == 0)
//                     {
//                         idleWarnings = 1;
//                         ui?.ShowIdleToast("Oops—thinking a bit long! Keep moving. (1/2)");
//                         idleTimer = 0f;
//                     }
//                     else
//                     {
//                         IsPlaying = false; 
//                         ui?.ShowIdleFail("Stopped twice too long—restarting…");
//                         StartCoroutine(RestartAfter(1.25f));
//                     }
//                 }
//             }
//             else
//             {
//                 idleTimer = 0f;
//             }

//             // === Add in Update(), inside `if (IsPlaying)` block ===
//             if (Input.GetKeyDown(killKey) && !_nukeBusy)
//             {
//                 StartCoroutine(NukeEnemiesAndRespawn());
//             }

//         }
//     }

//     public void StartGame()
//     {
//         if (IsPlaying) return;
//         IsPlaying = true;
//         ui?.HideHowTo();
//         ui?.ShowStartHint();
//         FreezeAllEnemies(false);
//         idleTimer = 0f;
//         idleWarnings = 0;
//     }

//     public void OnCoinCollected()
//     {
//         coinsCollected++;
//         ui?.SetCoin(totalCoins, coinsCollected);
//         if (coinsCollected >= totalCoins)
//         {
//             exitDoor?.ActivateExit(true);
//             ui?.ShowExitHint();
//         }
//     }

//     public void OnPlayerCaught()
//     {
//         if (!IsPlaying) return;
//         IsPlaying = false;
//         player?.OnLose();
//         ui?.ShowLose();
//         FreezeAllEnemies(true);
//     }

//     public void OnPlayerWin()
//     {
//         if (!IsPlaying) return;
//         IsPlaying = false;
//         player?.OnWin();
//         ui?.ShowWin();
//         FreezeAllEnemies(true);
//     }

//     public void Restart()
//     {
//         var scene = SceneManager.GetActiveScene();
//         SceneManager.LoadScene(scene.buildIndex);
//     }

//         // === Add in GameManager ===
//     System.Collections.IEnumerator NukeEnemiesAndRespawn()
//     {
//         if (_spawner == null)
//             _spawner = FindObjectOfType<EnemySpawner>();

//         _nukeBusy = true;

//         // 1) Despawn (kill) all current enemies
//         var enemies = FindObjectsOfType<EnemyChaser>();
//         for (int i = 0; i < enemies.Length; i++)
//         {
//             if (enemies[i])
//                 Destroy(enemies[i].gameObject);
//         }

//         ui?.ShowIdleToast("Enemies vaporized for " + killDurationSeconds.ToString("0") + "s…");

//         // 2) Wait for power duration
//         float t = 0f;
//         while (t < killDurationSeconds)
//         {
//             t += Time.deltaTime;
//             yield return null;
//         }

//         // 3) Respawn at original (baseline) positions
//         if (_spawner != null && _baselineEnemyPositions.Count > 0)
//         {
//             _spawner.SpawnAtPositions(_baselineEnemyPositions);
//         }

//         // 4) Add cost: spawn extra enemies and remember their positions
//         if (_spawner != null && extraEnemiesPerUse > 0)
//         {
//             var added = _spawner.SpawnExtra(extraEnemiesPerUse);
//             // Grow the baseline so future nukes bring back everyone (old + new)
//             _baselineEnemyPositions.AddRange(added);
//             ui?.ShowIdleToast("They multiplied! +" + added.Count + " enemies.");
//         }

//         _nukeBusy = false;
//     }


//     System.Collections.IEnumerator RestartAfter(float delay)
//     {
//         yield return new WaitForSeconds(delay);
//         Restart();
//     }

//     public void FreezeAllEnemies(bool frozen)
//     {
//         foreach (var e in FindObjectsOfType<EnemyChaser>())
//             e.SetFrozenVisual(frozen);
//     }
//     // === Add this helper in GameManager ===
//     System.Collections.IEnumerator CaptureInitialEnemyPositionsEndOfFrame()
//     {
//         // Wait a frame to ensure EnemySpawner.Start() has run and enemies exist
//         yield return null;

//         _baselineEnemyPositions.Clear();
//         var enemies = FindObjectsOfType<EnemyChaser>();
//         for (int i = 0; i < enemies.Length; i++)
//             if (enemies[i]) _baselineEnemyPositions.Add(enemies[i].transform.position);
//     }

// }

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

    // === Enemy Nuke Power ===
    [Header("Enemy Nuke Power")]
    public KeyCode killKey = KeyCode.K;
    public float killDurationSeconds = 5f;
    public int extraEnemiesPerUse = 2;

    // Internal state for nuke power
    private bool _nukeBusy = false;
    private readonly List<Vector2> _baselineEnemyPositions = new();
    private EnemySpawner _spawner;

    [Header("Idle Settings")]
    public float idleThreshold = 3f;
    private float idleTimer = 0f;
    private int idleWarnings = 0;

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

        _spawner = FindObjectOfType<EnemySpawner>();

        // Capture initial enemy positions one frame later (after spawner has run).
        StartCoroutine(CaptureInitialEnemyPositionsEndOfFrame());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) Restart();

        if (!IsPlaying &&
            (Input.GetKeyDown(KeyCode.Space) ||
             Input.GetKeyDown(KeyCode.Return) ||
             Input.GetMouseButtonDown(0)))
        {
            StartGame();
        }

        if (IsPlaying)
        {
            // --- Idle logic (your existing behavior) ---
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

            // --- K power trigger ---
            if (Input.GetKeyDown(killKey) && !_nukeBusy)
            {
                StartCoroutine(NukeEnemiesAndRespawn());
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

    // ===================== New bits below =====================

    // Wait one frame so EnemySpawner.Start() can create the initial enemies, then record their positions.
    IEnumerator CaptureInitialEnemyPositionsEndOfFrame()
    {
        yield return null;

        _baselineEnemyPositions.Clear();
        var enemies = FindObjectsOfType<EnemyChaser>();
        foreach (var e in enemies)
        {
            if (e) _baselineEnemyPositions.Add(e.transform.position);
        }
    }

    // K key behavior: wipe enemies for killDurationSeconds, then respawn + add more.
    IEnumerator NukeEnemiesAndRespawn()
    {
        _nukeBusy = true;

        // Ensure we have a spawner reference
        if (_spawner == null) _spawner = FindObjectOfType<EnemySpawner>();

        // 1) Kill all current enemies
        var enemies = FindObjectsOfType<EnemyChaser>();
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i]) Destroy(enemies[i].gameObject);
        }

        ui?.ShowIdleToast($"Enemies vaporized for {killDurationSeconds:0}s…");

        // 2) Wait for the safe window
        float t = 0f;
        while (t < killDurationSeconds)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // 3) Respawn baseline enemies at their original positions (captured at start and grown after each use)
        if (_spawner != null && _baselineEnemyPositions.Count > 0)
        {
            _spawner.SpawnAtPositions(_baselineEnemyPositions);
        }
        else if (_spawner != null && _baselineEnemyPositions.Count == 0)
        {
            // Fallback: if there was no baseline yet, create an initial wave and remember it.
            var created = _spawner.SpawnExtra(_spawner.enemyCount);
            _baselineEnemyPositions.AddRange(created);
        }

        // 4) Penalty: add extra enemies and fold them into the baseline so next K brings everyone back
        if (_spawner != null && extraEnemiesPerUse > 0)
        {
            var added = _spawner.SpawnExtra(extraEnemiesPerUse);
            _baselineEnemyPositions.AddRange(added);
            if (added.Count > 0) ui?.ShowIdleToast($"They multiplied! +{added.Count}");
        }

        _nukeBusy = false;
    }
}
