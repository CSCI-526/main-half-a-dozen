using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public Transform player;
    public GameObject enemyPrefab;
    public int enemyCount = 8;
    public float safeRadiusFromPlayer = 4f;
    public float spawnRadius = 11f;
    public float angleJitterDeg = 10f;
    public float minEnemySeparation = 3f;
    public LayerMask obstacleMask;
    public float checkRadius = 0.5f;
    public int maxPlacementTries = 25;

    private readonly List<Vector2> placed = new();

    void Start()
    {
        SpawnEnemies();
    }

    void SpawnEnemies()
    {
        if (!player || !enemyPrefab) return;

        Vector2 p = player.position;
        float baseStep = 360f / Mathf.Max(1, enemyCount);

        for (int i = 0; i < enemyCount; i++)
        {
            bool placedOK = false;

            for (int t = 0; t < maxPlacementTries && !placedOK; t++)
            {
                float angle = (i * baseStep) + Random.Range(-angleJitterDeg, angleJitterDeg);
                float rad = angle * Mathf.Deg2Rad;
                float r = spawnRadius + Random.Range(-1.0f, 1.0f);
                Vector2 candidate = p + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * r;

                if (!PassesRules(candidate, p)) continue;

                bool farFromOthers = true;
                foreach (var pos in placed)
                    if (Vector2.Distance(pos, candidate) < minEnemySeparation)
                    { farFromOthers = false; break; }
                if (!farFromOthers) continue;

                var go = Instantiate(enemyPrefab, candidate, Quaternion.identity);
                var chaser = go.GetComponent<EnemyChaser>();
                if (chaser) chaser.obstacleMask = obstacleMask;

                placed.Add(candidate);
                placedOK = true;
            }
        }
    }

    bool PassesRules(Vector2 pos, Vector2 playerPos)
    {
        if (Vector2.Distance(pos, playerPos) < safeRadiusFromPlayer) return false;
        if (Physics2D.OverlapCircle(pos, checkRadius, obstacleMask)) return false;
        return true;
    }

    void OnDrawGizmosSelected()
    {
        if (!player) return;
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(player.position, safeRadiusFromPlayer);
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(player.position, spawnRadius);
    }

    // === Add below inside EnemySpawner class ===
    public List<Vector2> SpawnAtPositions(IEnumerable<Vector2> positions)
    {
        var created = new List<Vector2>();
        if (!enemyPrefab) return created;

        foreach (var pos in positions)
        {
            var go = Instantiate(enemyPrefab, pos, Quaternion.identity);
            var chaser = go.GetComponent<EnemyChaser>();
            if (chaser) chaser.obstacleMask = obstacleMask;

            placed.Add(pos);
            created.Add(pos);
        }
        return created;
    }

    // Spawns `count` additional enemies using the same radial/spacing rules.
    // Returns the exact positions used (so caller can remember them for future waves).
    //New function added
    public List<Vector2> SpawnExtra(int count)
    {
        var created = new List<Vector2>();
        if (!player || !enemyPrefab || count <= 0) return created;

        Vector2 p = player.position;
        float baseStep = 360f / Mathf.Max(1, count);

        for (int i = 0; i < count; i++)
        {
            bool placedOK = false;

            for (int t = 0; t < maxPlacementTries && !placedOK; t++)
            {
                float angle = (i * baseStep) + Random.Range(-angleJitterDeg, angleJitterDeg);
                float rad = angle * Mathf.Deg2Rad;
                float r = spawnRadius + Random.Range(-1.0f, 1.0f);
                Vector2 candidate = p + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * r;

                if (!PassesRules(candidate, p)) continue;

                bool farFromOthers = true;
                foreach (var pos in placed)
                    if (Vector2.Distance(pos, candidate) < minEnemySeparation)
                    { farFromOthers = false; break; }
                if (!farFromOthers) continue;

                var go = Instantiate(enemyPrefab, candidate, Quaternion.identity);
                var chaser = go.GetComponent<EnemyChaser>();
                if (chaser) chaser.obstacleMask = obstacleMask;

                placed.Add(candidate);
                created.Add(candidate);
                placedOK = true;
            }
        }

        return created;
    }

}
