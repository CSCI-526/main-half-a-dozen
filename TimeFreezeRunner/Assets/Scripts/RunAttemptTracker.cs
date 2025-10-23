using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tracks Attempts-to-Complete and time per attempt, and posts one RunEnd row to Google Forms.
/// IMPORTANT: StartAttempt() will NOT reset the timer if the same attempt is already running,
/// so a single attempt can span corridor/dark-room scene hops. The start is cleared only when
/// the attempt ends (fail/success).
/// </summary>
public class RunAttemptTracker : MonoBehaviour
{
    public static RunAttemptTracker I;

    [Header("Wiring")]
    public SendToGoogle sender;

    [Header("Options")]
    [Tooltip("Reset the attempt counter for a level after a success.")]
    public bool resetAttemptsOnSuccess = true;

    [Tooltip("Send events while in the Unity Editor (enable for quick tests).")]
    public bool sendInEditor = true;

    // session + per-level state
    string _sessionId;
    // attempt index (1,2,3...) tracked per logical level key
    readonly Dictionary<string,int> _attemptIndexByLevel = new();
    // attempt start time (Time.time) per level; exists only while an attempt is running
    readonly Dictionary<string,float> _attemptStartByLevel = new();

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        _sessionId = Guid.NewGuid().ToString();
    }

    // ---- Helpers ----
    string CurrentLevelName()
    {
        // Use LevelManager if present, else scene name.
        if (LevelManager.I != null)
            return $"Level{LevelManager.I.currentLevel}";
        return SceneManager.GetActiveScene().name;
    }

    string IsoNowUtc() => DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

    // ---- Public API (called from GameManager) ----

    /// <summary>
    /// Marks the start of an attempt for the current (or provided) level.
    /// If an attempt is already running for this level, DO NOT overwrite the start time.
    /// This lets an attempt span corridor/dark-room sub-scenes.
    /// </summary>
    public void StartAttempt(string levelName = null)
    {
        levelName ??= CurrentLevelName();

        // Only set the start time if there isn't one yet for this attempt.
        if (!_attemptStartByLevel.ContainsKey(levelName))
        {
            _attemptStartByLevel[levelName] = Time.time;
#if UNITY_EDITOR
            if (sendInEditor) Debug.Log($"[AttemptTracker] StartAttempt {levelName} (timer set)");
#endif
        }
#if UNITY_EDITOR
        else if (sendInEditor)
        {
            Debug.Log($"[AttemptTracker] StartAttempt {levelName} (already running, timer not reset)");
        }
#endif
    }

    public void LogRunEndFail(string levelName = null)
    {
        levelName ??= CurrentLevelName();
        LogRunEnd(levelName, "fail");
    }

    public void LogRunEndSuccess(string levelName = null)
    {
        levelName ??= CurrentLevelName();
        LogRunEnd(levelName, "success");

        if (resetAttemptsOnSuccess)
            _attemptIndexByLevel[levelName] = 0; // next attempt on this level starts fresh
    }

    // ---- Core logging ----
    void LogRunEnd(string levelName, string result)
    {
        // Compute elapsed from when the attempt started (default to now if missing for safety).
        if (!_attemptStartByLevel.TryGetValue(levelName, out float startedAt))
            startedAt = Time.time;
        float secondsElapsed = Mathf.Max(0f, Time.time - startedAt);

        // Increment attempt index for this level
        _attemptIndexByLevel.TryGetValue(levelName, out int idx);
        idx += 1;
        _attemptIndexByLevel[levelName] = idx;

        // Clear the running attempt start so the next StartAttempt will set a fresh timer.
        _attemptStartByLevel.Remove(levelName);

#if UNITY_EDITOR
        if (!sendInEditor) return;
#endif
        if (sender == null)
        {
            Debug.LogWarning("[AttemptTracker] No SendToGoogle bound; skipping send.");
            return;
        }

        // Fire-and-forget POST to Google Forms
        StartCoroutine(sender.PostRunEndRow(
            tsVal: IsoNowUtc(),
            sessionIdVal: _sessionId,
            levelVal: levelName,
            resultVal: result,
            attemptIndexVal: idx,
            secondsElapsedVal: secondsElapsed
        ));
    }
}
