// // BETA METRIC4 CHANGE AnalyticsLogger.cs  (attach to a bootstrap "Systems" GameObject in your first scene)

// using UnityEngine;
// using UnityEngine.Networking;
// using System.Collections;
// using System.Collections.Generic;
// using System.Globalization;

// public class AnalyticsLogger : MonoBehaviour
// {
//     public static AnalyticsLogger I;

//     [Header("Google Form")]
//     [Tooltip("https://docs.google.com/forms/d/e/1FAIpQLSfP_wqoJVUj-QgOBAx0rQV7W3DGIqZAtKFL-r1zbdTSSgQKHA/formResponse")]
//     public string formPostUrl = "https://docs.google.com/forms/d/e/1FAIpQLSfP_wqoJVUj-QgOBAx0rQV7W3DGIqZAtKFL-r1zbdTSSgQKHA/formResponse";

//     // field ids from your prefill link
//     const string F_ts = "entry.1973858073";
//     const string F_sessionId = "entry.238033426";
//     const string F_runId = "entry.2053032902";
//     const string F_eventType = "entry.1805387438";
//     const string F_level = "entry.1336689245";
//     const string F_powerUpType = "entry.138424869";
//     const string F_timeSince = "entry.1695725548";

//     [Header("Identity")]
//     public string sessionId;
//     int runId = 0;

//     readonly Queue<Dictionary<string,string>> _queue = new();
//     Coroutine _pump;

//     void Awake()
//     {
//         if (I != null && I != this) { Destroy(gameObject); return; }
//         I = this;
//         DontDestroyOnLoad(gameObject);

//         if (string.IsNullOrEmpty(sessionId))
//             sessionId = System.Guid.NewGuid().ToString("N").Substring(0, 16);

//         _pump = StartCoroutine(Pump());
//     }

//     // void Awake()
//     // {
//     //     if (I != null && I != this) { Destroy(gameObject); return; }
//     //     I = this;
//     //     DontDestroyOnLoad(gameObject);

//     //     // Persist sessionId across editor runs / builds unless user typed one in Inspector
//     //     if (string.IsNullOrWhiteSpace(sessionId))
//     //     {
//     //         var saved = PlayerPrefs.GetString("analytics.sessionId", "");
//     //         if (string.IsNullOrEmpty(saved))
//     //         {
//     //             saved = System.Guid.NewGuid().ToString("N"); // 32-char GUID
//     //             PlayerPrefs.SetString("analytics.sessionId", saved);
//     //             PlayerPrefs.Save();
//     //         }
//     //         sessionId = saved;
//     //     }

//     //     // (Optional) sanity check: Google Form endpoint must end with /formResponse
//     //     if (!string.IsNullOrEmpty(formPostUrl) && !formPostUrl.EndsWith("/formResponse"))
//     //         Debug.LogWarning("[Analytics] formPostUrl should end with /formResponse");

//     //     _pump = StartCoroutine(Pump());
//     // }

//     public void StartNewRun()
//     {
//         runId++;
//         Debug.Log($"[Analytics] New run: {runId} (session {sessionId})");
//     }

//     public void LogPowerUpUse(string levelName, string powerUpType, float timeSinceLevelStart)
//     {
//         var row = new Dictionary<string, string>
//         {
//             { F_ts, System.DateTime.UtcNow.ToString("o") }, // ISO8601 UTC
//             { F_sessionId, sessionId },
//             { F_runId, runId.ToString() },
//             { F_eventType, "PowerUpUse" },
//             { F_level, levelName },
//             { F_powerUpType, powerUpType },
//             { F_timeSince, timeSinceLevelStart.ToString(CultureInfo.InvariantCulture) }
//         };

//         _queue.Enqueue(row);
//         Debug.Log($"[Analytics] Queued PowerUpUse: {levelName}, {powerUpType}, t={timeSinceLevelStart:0.00}s");
//     }

//     IEnumerator Pump()
//     {
//         var wait = new WaitForSeconds(0.75f);
//         while (true)
//         {
//             while (_queue.Count > 0)
//             {
//                 var payload = _queue.Dequeue();
//                 WWWForm form = new WWWForm();
//                 foreach (var kv in payload) form.AddField(kv.Key, kv.Value);

//                 using var req = UnityWebRequest.Post(formPostUrl, form);
//                 yield return req.SendWebRequest();

//                 // Log response to diagnose
//                 Debug.Log($"[Analytics] POST -> {req.responseCode} / {req.result} / {req.error}");
//                 // Note: Google returns HTML; success is usually HTTP 200.
//             }
//             yield return wait;
//         }
//     }
// }


using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

public class AnalyticsLogger : MonoBehaviour
{
    public static AnalyticsLogger I;

    [Header("Google Form")]
    [Tooltip("Must end with /formResponse")]
    public string formPostUrl =
        "https://docs.google.com/forms/d/e/1FAIpQLSfP_wqoJVUj-QgOBAx0rQV7W3DGIqZAtKFL-r1zbdTSSgQKHA/formResponse";

    // Google Form field IDs
    const string F_ts        = "entry.1973858073";
    const string F_sessionId = "entry.238033426";
    const string F_runId     = "entry.2053032902";
    const string F_eventType = "entry.1805387438";
    const string F_level     = "entry.1336689245";
    const string F_powerUpType = "entry.138424869";
    const string F_timeSince = "entry.1695725548";

    [Header("Identity")]
    [Tooltip("Leave empty to auto-generate each session.")]
    [SerializeField] private string sessionId = "";
    [Tooltip("If ON, reuse the same sessionId across Editor plays/app restarts (stored in PlayerPrefs).")]
    public bool persistSessionBetweenPlays = false;

    // per-level attempt counter
    private readonly Dictionary<string,int> _runIdByLevel = new();
    private string _activeLevel = "Unknown";
    private int _activeRunId = 0;

    private readonly Queue<Dictionary<string,string>> _queue = new();
    private Coroutine _pump;

    // If you use Fast Enter Play Mode without domain reload,
    // this ensures statics are reset on each Play.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStaticsOnPlay()
    {
        I = null;
    }

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        InitializeSessionId();
        if (!string.IsNullOrEmpty(formPostUrl) && !formPostUrl.EndsWith("/formResponse"))
            Debug.LogWarning("[Analytics] formPostUrl should end with /formResponse");

        _pump = StartCoroutine(Pump());
    }

    // -------------------------
    // Session control
    // -------------------------
    void InitializeSessionId()
    {
        if (persistSessionBetweenPlays)
        {
            var saved = PlayerPrefs.GetString("analytics.sessionId", "");
            if (string.IsNullOrWhiteSpace(saved))
            {
                saved = System.Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString("analytics.sessionId", saved);
                PlayerPrefs.Save();
            }
            sessionId = saved; // override Inspector if persistence is on
        }
        else
        {
            // Per-run session; regenerate if Inspector field is empty
            if (string.IsNullOrWhiteSpace(sessionId))
                sessionId = System.Guid.NewGuid().ToString("N").Substring(0, 16);
        }
        Debug.Log($"[Analytics] SessionId = {sessionId} (persist={persistSessionBetweenPlays})");
    }

    /// Call this when you want a brand-new session while the app is still running.
    public void StartNewSession()
    {
        sessionId = System.Guid.NewGuid().ToString("N").Substring(0, 16);
        _runIdByLevel.Clear();
        _activeLevel = "Unknown";
        _activeRunId = 0;
        Debug.Log($"[Analytics] New session started: {sessionId}");
    }

    // -------------------------
    // Per-level attempt control
    // -------------------------
    public void StartNewRun(string levelName)
    {
        if (string.IsNullOrEmpty(levelName)) levelName = "Unknown";
        _activeLevel = levelName;

        int cur = 0;
        _runIdByLevel.TryGetValue(levelName, out cur);
        _runIdByLevel[levelName] = ++cur;
        _activeRunId = cur;

        Debug.Log($"[Analytics] New run -> Level={levelName}, Attempt={_activeRunId}, Session={sessionId}");
    }

    // Backward-compat for existing calls:
    public void StartNewRun()
    {
        string lvl = (LevelManager.I != null)
            ? $"Level{LevelManager.I.currentLevel}"
            : (_activeLevel ?? "Unknown");
        StartNewRun(lvl);
    }

    // -------------------------
    // Events
    // -------------------------
    public void LogPowerUpUse(string levelName, string powerUpType, float timeSinceLevelStart)
    {
        // ensure we have an attempt counter for this level
        if (string.IsNullOrEmpty(levelName)) levelName = _activeLevel ?? "Unknown";
        if (!_runIdByLevel.ContainsKey(levelName))
        {
            _runIdByLevel[levelName] = 1;
            _activeRunId = 1;
        }

        var row = new Dictionary<string, string>
        {
            { F_ts, System.DateTime.UtcNow.ToString("o") },
            { F_sessionId, sessionId },
            { F_runId, _runIdByLevel[levelName].ToString() },
            { F_eventType, "PowerUpUse" },
            { F_level, levelName },
            { F_powerUpType, powerUpType },
            { F_timeSince, timeSinceLevelStart.ToString(CultureInfo.InvariantCulture) }
        };

        _queue.Enqueue(row);
        Debug.Log($"[Analytics] Queued PowerUpUse: {levelName}, {powerUpType}, t={timeSinceLevelStart:0.00}s, run={_runIdByLevel[levelName]}, session={sessionId}");
    }

    // -------------------------
    // Network pump
    // -------------------------
    IEnumerator Pump()
    {
        var wait = new WaitForSeconds(0.75f);
        while (true)
        {
            while (_queue.Count > 0)
            {
                var payload = _queue.Dequeue();
                WWWForm form = new WWWForm();
                foreach (var kv in payload) form.AddField(kv.Key, kv.Value);

                using var req = UnityWebRequest.Post(formPostUrl, form);
                yield return req.SendWebRequest();

                Debug.Log($"[Analytics] POST -> {req.responseCode} / {req.result} / {req.error}");
            }
            yield return wait;
        }
    }
}