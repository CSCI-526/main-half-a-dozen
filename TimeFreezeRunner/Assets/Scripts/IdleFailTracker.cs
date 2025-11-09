using UnityEngine;
using System;

public class IdleFailTracker : MonoBehaviour
{
    public static IdleFailTracker Instance { get; private set; }

    [Header("Behavior")]
    [Tooltip("Post the summary row when idle-death count reaches this number.")]
    public int submitOnNth = 2;  // per your requirement

    [Tooltip("Idle threshold (sec) used by gameplay; for logging only.")]
    public float idleThresholdSec = 3f;

    [Header("Identity/Context")]
    public string buildVersion = "alpha-milestone";
    public string client = "WebGL"; // or Standalone

    // Runtime state (per level)
    private int _idleDeathCountThisLevel = 0;
    private float _levelStartTime;

    // Provided by your existing attempt/run system if available:
    public Func<int> GetAttemptIndex;        // optional delegate
    public Func<int> GetLevelIndex;          // optional delegate
    public Func<int> GetCoinsCollected;      // optional
    public Func<int> GetPowerupsUsed;        // optional

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _levelStartTime = Time.time;
    }

    public void OnLevelStarted()
    {
        _idleDeathCountThisLevel = 0;
        _levelStartTime = Time.time;
    }

    // Call this AFTER gameplay has already processed an Idle death.
    public void OnIdleDeath()
    {
        _idleDeathCountThisLevel++;

        // Always log the individual idle-death event row (fine-grained)
        SendIdleRow(eventType: "IdleDeath", secondStrike: false);

        // If this is the 2nd time, also send a summary row as requested
        if (_idleDeathCountThisLevel == submitOnNth)
        {
            SendIdleRow(eventType: "IdleSummary", secondStrike: true);
        }
    }

    private void SendIdleRow(string eventType, bool secondStrike)
    {
        var payload = new IdlePayload
        {
            ts = DateTime.UtcNow.ToString("o"),
            sessionId = SystemInfo.deviceUniqueIdentifier,
            buildVersion = buildVersion,
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            levelIndex = GetLevelIndex?.Invoke() ?? -1,
            attemptIndex = GetAttemptIndex?.Invoke() ?? -1,
            eventType = eventType,
            idleThresholdSec = idleThresholdSec,
            idleDeathCountThisLevel = _idleDeathCountThisLevel,
            timeSinceLevelStartSec = Time.time - _levelStartTime,
            coinsCollected = GetCoinsCollected?.Invoke() ?? -1,
            powerupsUsed = GetPowerupsUsed?.Invoke() ?? -1,
            submitOnNth = submitOnNth,
            secondStrike = secondStrike,
            client = client
        };

        SendToGoogleIdle.Instance?.Post(payload);
    }

    [Serializable]
    public class IdlePayload
    {
        public string ts, sessionId, buildVersion, sceneName, eventType, client;
        public int levelIndex, attemptIndex, idleDeathCountThisLevel, coinsCollected, powerupsUsed, submitOnNth;
        public float idleThresholdSec, timeSinceLevelStartSec;
        public bool secondStrike;
    }
}
