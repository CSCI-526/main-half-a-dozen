using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathEventTracker : MonoBehaviour
{
    public static DeathEventTracker I;

    [Header("Wire this to the same GameObject")]
    public SendToGoogle_Death sender;

    string _sessionId;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        _sessionId = Guid.NewGuid().ToString();
    }

    public void LogDeathAt(Vector3 worldPos)
    {
        if (sender == null) { Debug.LogWarning("[DeathTracker] Sender not assigned."); return; }

        string tsIso = DateTime.UtcNow.ToString("o");
        string levelLabel = (LevelManager.I != null)
            ? $"Level{LevelManager.I.currentLevel}"
            : SceneManager.GetActiveScene().name;

        Vector2 pos2 = new Vector2(worldPos.x, worldPos.y);
        StartCoroutine(sender.PostDeathRow(tsIso, _sessionId, levelLabel, pos2));
    }
}
