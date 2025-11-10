// BETA METRIC4 CHANGES LevelTimer.cs
// Put this anywhere in your project. No GameObject needed.
using UnityEngine;

/// <summary>
/// Minimal per-attempt timer for single-scene, multi-level games.
/// Call LevelTimer.Begin() when an attempt starts (StartGame/Restart).
/// Read LevelTimer.Elapsed for "time since level start" in seconds.
/// </summary>
public static class LevelTimer
{
    private static float _startTime;
    private static bool _started;

    /// <summary>Start (or restart) the attempt timer.</summary>
    public static void Begin()
    {
        _startTime = Time.time;
        _started = true;
    }

    /// <summary>Seconds since the last Begin(). Returns 0 if not started yet.</summary>
    public static float Elapsed => _started ? (Time.time - _startTime) : 0f;

    /// <summary>Has Begin() been called at least once?</summary>
    public static bool IsRunning => _started;

    /// <summary>Force-set the start time offset (rarely needed).</summary>
    public static void SetElapsed(float seconds)
    {
        _startTime = Time.time - Mathf.Max(0f, seconds);
        _started = true;
    }
}