using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyContextAtDeathTracker : MonoBehaviour
{
    public static EnemyContextAtDeathTracker I;

    [Header("Wire this to the same GameObject")]
    public SendToGoogle_EnemyContext sender;

    string _sessionId;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        _sessionId = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Call this when the player is caught/dies.
    /// </summary>
    public void LogAtDeath(Vector3 playerWorldPos, string levelLabel)
    {
        if (sender == null)
        {
            Debug.LogWarning("[EnemyContextTracker] Sender not assigned.");
            return;
        }

        Vector2 playerPos2 = new Vector2(playerWorldPos.x, playerWorldPos.y);

        bool hasE1 = false, hasE2 = false, hasE3 = false;
        Vector2 e1Pos = Vector2.zero, e2Pos = Vector2.zero, e3Pos = Vector2.zero;
        float e1Dist = 0f, e2Dist = 0f, e3Dist = 0f;

        EnemyChaser[] enemies = GameObject.FindObjectsOfType<EnemyChaser>();
        if (enemies != null && enemies.Length > 0)
        {
            var list = new List<(EnemyChaser e, float dist)>(enemies.Length);

            for (int i = 0; i < enemies.Length; i++)
            {
                var e = enemies[i];
                if (e == null) continue;

                Vector2 ep = e.transform.position;
                float d = Vector2.Distance(playerPos2, ep); // >= 0
                list.Add((e, d));
            }

            list.Sort((a, b) => a.dist.CompareTo(b.dist));

            if (list.Count > 0)
            {
                hasE1 = true;
                e1Pos = list[0].e.transform.position;
                e1Dist = list[0].dist;
            }
            if (list.Count > 1)
            {
                hasE2 = true;
                e2Pos = list[1].e.transform.position;
                e2Dist = list[1].dist;
            }
            if (list.Count > 2)
            {
                hasE3 = true;
                e3Pos = list[2].e.transform.position;
                e3Dist = list[2].dist;
            }
        }

        Debug.Log(
            $"[EnemyContextTracker] Player=({playerPos2.x:0.###},{playerPos2.y:0.###}) " +
            $"E1={(hasE1 ? $"({e1Pos.x:0.###},{e1Pos.y:0.###},d={e1Dist:0.###})" : "NONE")} " +
            $"E2={(hasE2 ? $"({e2Pos.x:0.###},{e2Pos.y:0.###},d={e2Dist:0.###})" : "NONE")} " +
            $"E3={(hasE3 ? $"({e3Pos.x:0.###},{e3Pos.y:0.###},d={e3Dist:0.###})" : "NONE")}"
        );

        string tsIso = DateTime.UtcNow.ToString("o");
        string scene = SceneManager.GetActiveScene().name;
        string eventTypeVal = "Death";   // or "Caught" if you prefer

        StartCoroutine(sender.PostEnemyContextRow(
            tsIso,
            _sessionId,
            levelLabel,
            playerPos2,
            eventTypeVal,
            scene,
            hasE1, e1Pos, e1Dist,
            hasE2, e2Pos, e2Dist,
            hasE3, e3Pos, e3Dist
        ));
    }
}
