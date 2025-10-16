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

// void Start()
// {
//     totalCoins = FindObjectsOfType<Coin>().Length;
//     ui?.SetCoin(totalCoins, coinsCollected);
//     if (exitDoor) exitDoor.ActivateExit(false);

//     FreezeAllEnemies(true);

//     // 🔸 Handle Level 2 dark maze unlocking logic
//     if (LevelManager.I != null && LevelManager.I.currentLevel == 2)
// {
//     // restore player state
//     var s = LevelManager.I.savedState;
//     if (s != null && s.position != Vector3.zero)
//     {
//         player.transform.position = s.position;
//         coinsCollected = s.coinsCollected;
//         ui?.SetCoin(totalCoins, coinsCollected);
//         if (s.exitUnlocked && exitDoor != null)
//             exitDoor.ActivateExit(true);

//         Debug.Log($"♻️ Restored player position: {s.position}");
//     }
// }

//     // 🔸 NEW — show intro panel (Level 1, Level 2, etc.)
//     if (LevelManager.I != null)
//     {
//         UILevelPanel.ShowIntro(LevelManager.I.currentLevel);
//     }
//     else
//     {
//         // fallback if LevelManager missing
//         ui?.ShowHowTo(true);
//         Debug.LogWarning("⚠️ LevelManager missing! Game starting directly.");
//         StartGame();
//     }
// }

// void Update()
// {
//     if (Input.GetKeyDown(KeyCode.R))
//         Restart();

//     // 🔸 Prevent game from starting while Level Intro is still showing
//     if (UILevelPanel.IsIntroVisible)
//         return;

//     if (!IsPlaying && (Input.GetKeyDown(KeyCode.Space) ||
//                        Input.GetKeyDown(KeyCode.Return) ||
//                        Input.GetMouseButtonDown(0)))
//     {
//         StartGame();
//     }

//     if (IsPlaying)
//     {
//         if (!IsPlayerMoving)
//         {
//             idleTimer += Time.deltaTime;
//             if (idleTimer >= idleThreshold)
//             {
//                 if (idleWarnings == 0)
//                 {
//                     idleWarnings = 1;
//                     ui?.ShowIdleToast("Oops—thinking a bit long! Keep moving. (1/2)");
//                     idleTimer = 0f;
//                 }
//                 else
//                 {
//                     IsPlaying = false; 
//                     ui?.ShowIdleFail("Stopped twice too long—restarting…");
//                     StartCoroutine(RestartAfter(1.25f));
//                 }
//             }
//         }
//         else
//         {
//             idleTimer = 0f;
//         }
//     }
// }

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

//     // public void OnCoinCollected()
//     // {
//     //     coinsCollected++;
//     //     ui?.SetCoin(totalCoins, coinsCollected);
//     //     if (coinsCollected >= totalCoins)
//     //     {
//     //         exitDoor?.ActivateExit(true);
//     //         ui?.ShowExitHint();
//     //     }
//     // }
// public void OnCoinCollected()
// {
//     coinsCollected++;
//     ui?.SetCoin(totalCoins, coinsCollected);

//     if (coinsCollected >= totalCoins)
//     {
//         // unlock the corridor trigger instead of exit door
//         var corridorTrigger = FindObjectOfType<SceneTransition>();
//         if (corridorTrigger != null)
//         {
//             corridorTrigger.gameObject.SetActive(true);

//             // optional: add a pulsing visual glow
//             var sr = corridorTrigger.GetComponent<SpriteRenderer>();
//             if (sr != null) StartCoroutine(PulseColor(sr));
//         }

//         ui?.ShowIdleToast("🔍 Explore the right side passage!");
//     }
// }

// // simple pulsing highlight
// System.Collections.IEnumerator PulseColor(SpriteRenderer sr)
// {
//     Color baseColor = sr.color;
//     float t = 0f;
//     while (sr != null)
//     {
//         sr.color = baseColor * (1.0f + Mathf.Sin(t * 4f) * 0.3f);
//         t += Time.deltaTime;
//         yield return null;
//     }
// }

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

//         // 🔸 Trigger Level Complete screen
//         LevelManager.I?.OnLevelComplete();
//     }

//     public void Restart()
//     {
//         var scene = SceneManager.GetActiveScene();
//         SceneManager.LoadScene(scene.buildIndex);
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
// }


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

    // 🔸 Show intro panel for the level
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
}

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            Restart();

        // Prevent starting while intro is visible
        if (UILevelPanel.IsIntroVisible)
            return;

        if (!IsPlaying && (Input.GetKeyDown(KeyCode.Space) ||
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

    // public void OnCoinCollected()
    // {
    //     coinsCollected++;
    //     ui?.SetCoin(totalCoins, coinsCollected);

    //     if (coinsCollected < totalCoins) return;

    //     // ✅ LEVEL 1: exit unlocks normally
    //     if (LevelManager.I != null && LevelManager.I.currentLevel == 1)
    //     {
    //         exitDoor?.ActivateExit(true);
    //         ui?.ShowExitHint();
    //         Debug.Log("✅ Level 1: Exit door unlocked!");
    //         return;
    //     }

    //     // ✅ LEVEL 2: show corridor trigger instead
    //     if (LevelManager.I != null && LevelManager.I.currentLevel == 2)
    //     {
    //         var corridorTrigger = FindObjectOfType<SceneTransition>();
    //         if (corridorTrigger != null)
    //         {
    //             corridorTrigger.gameObject.SetActive(true);
    //             var sr = corridorTrigger.GetComponent<SpriteRenderer>();
    //             if (sr != null) StartCoroutine(PulseColor(sr));
    //         }
    //         ui?.ShowIdleToast("🔍 Explore the right-side passage!");
    //         Debug.Log("🟡 Level 2: Corridor trigger unlocked — door stays locked until key!");
    //     }
    // }

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

    System.Collections.IEnumerator PulseColor(SpriteRenderer sr)
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
}