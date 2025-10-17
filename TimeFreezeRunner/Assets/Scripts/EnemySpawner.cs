// WIP-2 LATEST WORKING VERSION


// using UnityEngine;
// using System.Collections.Generic;

// public class EnemySpawner : MonoBehaviour
// {
//     public Transform player;
//     public GameObject enemyPrefab;
//     public int enemyCount = 8;
//     public float safeRadiusFromPlayer = 4f;
//     public float spawnRadius = 11f;
//     public float angleJitterDeg = 10f;
//     public float minEnemySeparation = 3f;
//     public LayerMask obstacleMask;
//     public float checkRadius = 0.5f;
//     public int maxPlacementTries = 25;

//     private readonly List<Vector2> placed = new();

//     void Start()
//     {
//         SpawnEnemies();
//     }

//     void SpawnEnemies()
//     {
//         if (!player || !enemyPrefab) return;

//         Vector2 p = player.position;
//         float baseStep = 360f / Mathf.Max(1, enemyCount);

//         for (int i = 0; i < enemyCount; i++)
//         {
//             bool placedOK = false;

//             for (int t = 0; t < maxPlacementTries && !placedOK; t++)
//             {
//                 float angle = (i * baseStep) + Random.Range(-angleJitterDeg, angleJitterDeg);
//                 float rad = angle * Mathf.Deg2Rad;
//                 float r = spawnRadius + Random.Range(-1.0f, 1.0f);
//                 Vector2 candidate = p + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * r;

//                 if (!PassesRules(candidate, p)) continue;

//                 bool farFromOthers = true;
//                 foreach (var pos in placed)
//                     if (Vector2.Distance(pos, candidate) < minEnemySeparation)
//                     { farFromOthers = false; break; }
//                 if (!farFromOthers) continue;

//                 var go = Instantiate(enemyPrefab, candidate, Quaternion.identity);
//                 var chaser = go.GetComponent<EnemyChaser>();
//                 if (chaser) chaser.obstacleMask = obstacleMask;

//                 placed.Add(candidate);
//                 placedOK = true;
//             }
//         }
//     }

//     bool PassesRules(Vector2 pos, Vector2 playerPos)
//     {
//         if (Vector2.Distance(pos, playerPos) < safeRadiusFromPlayer) return false;
//         if (Physics2D.OverlapCircle(pos, checkRadius, obstacleMask)) return false;
//         return true;
//     }

//     void OnDrawGizmosSelected()
//     {
//         if (!player) return;
//         Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(player.position, safeRadiusFromPlayer);
//         Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(player.position, spawnRadius);
//     }
// }


// LEVEL 3 TESTING

// using UnityEngine;
// using System.Collections.Generic;

// public class EnemySpawner : MonoBehaviour
// {
//     public Transform player;
//     public GameObject enemyPrefab;
//     public int enemyCount = 8;
//     public float safeRadiusFromPlayer = 4f;
//     public float spawnRadius = 11f;
//     public float angleJitterDeg = 10f;
//     public float minEnemySeparation = 3f;
//     public LayerMask obstacleMask;
//     public float checkRadius = 0.5f;
//     public int maxPlacementTries = 25;


//     private readonly List<Vector2> placed = new();

//     void Start()
//     {
//         SpawnEnemies();
//     }

//     void SpawnEnemies()
//     {
//         if (!player || !enemyPrefab) return;

//         Vector2 p = player.position;
//         float baseStep = 360f / Mathf.Max(1, enemyCount);

//         for (int i = 0; i < enemyCount; i++)
//         {
//             bool placedOK = false;

//             for (int t = 0; t < maxPlacementTries && !placedOK; t++)
//             {
//                 float angle = (i * baseStep) + Random.Range(-angleJitterDeg, angleJitterDeg);
//                 float rad = angle * Mathf.Deg2Rad;
//                 float r = spawnRadius + Random.Range(-1.0f, 1.0f);
//                 Vector2 candidate = p + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * r;

//                 if (!PassesRules(candidate, p)) continue;

//                 bool farFromOthers = true;
//                 foreach (var pos in placed)
//                     if (Vector2.Distance(pos, candidate) < minEnemySeparation)
//                     { farFromOthers = false; break; }
//                 if (!farFromOthers) continue;

//                 var go = Instantiate(enemyPrefab, candidate, Quaternion.identity);
//                 var chaser = go.GetComponent<EnemyChaser>();
//                 if (chaser) chaser.obstacleMask = obstacleMask;

//                 placed.Add(candidate);
//                 placedOK = true;
//             }
//         }
//     }

//     // ---------- original rule checks ----------
//     bool PassesRules(Vector2 pos, Vector2 playerPos)
//     {
//         if (Vector2.Distance(pos, playerPos) < safeRadiusFromPlayer) return false;
//         if (Physics2D.OverlapCircle(pos, checkRadius, obstacleMask)) return false;
//         return true;
//     }

//     void OnDrawGizmosSelected()
//     {
//         if (!player) return;
//         Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(player.position, safeRadiusFromPlayer);
//         Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(player.position, spawnRadius);
//     }

//     // ================== Added public helpers ==================

//     // Respawn enemies at exact positions (used for baseline restore)
//     public List<Vector2> SpawnAtPositions(IEnumerable<Vector2> positions)
//     {
//         var created = new List<Vector2>();
//         if (!enemyPrefab) return created;

//         foreach (var pos in positions)
//         {
//             var go = Instantiate(enemyPrefab, pos, Quaternion.identity);
//             var chaser = go.GetComponent<EnemyChaser>();
//             if (chaser)
//             {
//                 chaser.obstacleMask = obstacleMask;
//                 if (!chaser.player && player) chaser.player = player;
//             }

//             placed.Add(pos);
//             created.Add(pos);
//         }
//         return created;
//     }

//     // Add exactly `count` more enemies using the same rules; retries with multiple strategies.
//     public List<Vector2> SpawnExtra(int count)
//     {
//         var created = new List<Vector2>();
//         if (!player || !enemyPrefab || count <= 0) return created;

//         Vector2 p = player.position;
//         int target = count;
//         int attempts = 0;
//         int hardCap = Mathf.Max(200, maxPlacementTries * count * 4);

//         while (created.Count < target && attempts < hardCap)
//         {
//             attempts++;

//             // Strategy 1: ring around player
//             float angle = Random.Range(0f, 360f) + Random.Range(-angleJitterDeg, angleJitterDeg);
//             float rad = angle * Mathf.Deg2Rad;
//             float r = spawnRadius + Random.Range(-1.0f, 1.0f);
//             Vector2 candidate = p + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * r;

//             if (TryPlace(candidate, p, out Vector2 final))
//             {
//                 created.Add(InstantiateEnemyAt(final));
//                 continue;
//             }

//             // Strategy 2: small local spiral around a random seed near the ring
//             Vector2 seed = p + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * (spawnRadius * 0.8f);
//             const int spiralSteps = 14;
//             const float stepRadius = 0.35f;
//             float a = Random.Range(0f, 360f) * Mathf.Deg2Rad;
//             bool placedOK = false;

//             for (int s = 0; s < spiralSteps; s++)
//             {
//                 Vector2 probe = seed + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (s * stepRadius);
//                 float sep = (s > spiralSteps * 0.6f) ? minEnemySeparation * 0.9f : minEnemySeparation;

//                 if (TryPlace(probe, p, out final, sep))
//                 {
//                     created.Add(InstantiateEnemyAt(final));
//                     placedOK = true;
//                     break;
//                 }
//                 a += 37f * Mathf.Deg2Rad; // golden-ish increment
//             }

//             if (placedOK) continue;

//             // Strategy 3: pure random near center (rarely needed, just in tight spaces)
//             Vector2 centerProbe = p + Random.insideUnitCircle * (spawnRadius * 0.6f);
//             if (TryPlace(centerProbe, p, out final))
//             {
//                 created.Add(InstantiateEnemyAt(final));
//             }
//         }

//         return created;
//     }

//     // Try to validate & accept a position with rule checks and separation
//     bool TryPlace(Vector2 candidate, Vector2 playerPos, out Vector2 final, float separationOverride = -1f)
//     {
//         final = candidate;
//         if (!PassesRules(candidate, playerPos)) return false;

//         float sep = (separationOverride > 0f) ? separationOverride : minEnemySeparation;
//         for (int i = 0; i < placed.Count; i++)
//             if (Vector2.Distance(placed[i], candidate) < sep) return false;

//         return true;
//     }

//     Vector2 InstantiateEnemyAt(Vector2 pos)
//     {
//         var go = Instantiate(enemyPrefab, pos, Quaternion.identity);
//         var chaser = go.GetComponent<EnemyChaser>();
//         if (chaser)
//         {
//             chaser.obstacleMask = obstacleMask;
//             if (!chaser.player && player) chaser.player = player;
//         }

//         placed.Add(pos);
//         return pos;
//     }


// }




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

    [Header("Arena Bounds")]
    public BoxCollider2D arenaCollider;   // drag ArenaBounds (BoxCollider2D) here
    public float arenaPadding = 0.75f;    // keeps spawns clearly inside the white frame

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

                // clamp into arena
                if (arenaCollider)
                {
                    var b = arenaCollider.bounds;
                    float minX = b.min.x + arenaPadding, maxX = b.max.x - arenaPadding;
                    float minY = b.min.y + arenaPadding, maxY = b.max.y - arenaPadding;
                    candidate.x = Mathf.Clamp(candidate.x, minX, maxX);
                    candidate.y = Mathf.Clamp(candidate.y, minY, maxY);
                }

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

    // ---------- rule checks ----------
    bool PassesRules(Vector2 pos, Vector2 playerPos)
    {
        if (!InsideArena(pos)) return false;                      // NEW
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

    // ================== public helpers ==================

    public List<Vector2> SpawnAtPositions(IEnumerable<Vector2> positions)
    {
        var created = new List<Vector2>();
        if (!enemyPrefab) return created;

        foreach (var pos in positions)
        {
            if (arenaCollider && !InsideArena(pos)) continue; // NEW safety

            var go = Instantiate(enemyPrefab, pos, Quaternion.identity);
            var chaser = go.GetComponent<EnemyChaser>();
            if (chaser)
            {
                chaser.obstacleMask = obstacleMask;
                if (!chaser.player && player) chaser.player = player;
            }

            placed.Add(pos);
            created.Add(pos);
        }
        return created;
    }

    public List<Vector2> SpawnExtra(int count)
    {
        var created = new List<Vector2>();
        if (!player || !enemyPrefab || count <= 0) return created;

        Vector2 p = player.position;
        int target = count;
        int attempts = 0;
        int hardCap = Mathf.Max(200, maxPlacementTries * count * 4);

        while (created.Count < target && attempts < hardCap)
        {
            attempts++;

            // Strategy 1: ring around player
            float angle = Random.Range(0f, 360f) + Random.Range(-angleJitterDeg, angleJitterDeg);
            float rad = angle * Mathf.Deg2Rad;
            float r = spawnRadius + Random.Range(-1.0f, 1.0f);
            Vector2 candidate = p + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * r;

            // clamp into arena
            if (arenaCollider)
            {
                var b = arenaCollider.bounds;
                float minX = b.min.x + arenaPadding, maxX = b.max.x - arenaPadding;
                float minY = b.min.y + arenaPadding, maxY = b.max.y - arenaPadding;
                candidate.x = Mathf.Clamp(candidate.x, minX, maxX);
                candidate.y = Mathf.Clamp(candidate.y, minY, maxY);
            }

            if (TryPlace(candidate, p, out Vector2 final))
            {
                created.Add(InstantiateEnemyAt(final));
                continue;
            }

            // Strategy 2: small spiral nudge near the ring
            Vector2 seed = p + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * (spawnRadius * 0.8f);
            const int spiralSteps = 14;
            const float stepRadius = 0.35f;
            float a = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            bool placedOK = false;

            for (int s = 0; s < spiralSteps; s++)
            {
                Vector2 probe = seed + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (s * stepRadius);
                float sep = (s > spiralSteps * 0.6f) ? minEnemySeparation * 0.9f : minEnemySeparation;

                if (TryPlace(probe, p, out final, sep))
                {
                    created.Add(InstantiateEnemyAt(final));
                    placedOK = true;
                    break;
                }
                a += 37f * Mathf.Deg2Rad;
            }

            if (placedOK) continue;

            // Strategy 3: random near center
            Vector2 centerProbe = p + Random.insideUnitCircle * (spawnRadius * 0.6f);
            if (TryPlace(centerProbe, p, out final))
            {
                created.Add(InstantiateEnemyAt(final));
            }
        }

        return created;
    }

    bool TryPlace(Vector2 candidate, Vector2 playerPos, out Vector2 final, float separationOverride = -1f)
    {
        final = candidate;
        if (!PassesRules(candidate, playerPos)) return false;

        float sep = (separationOverride > 0f) ? separationOverride : minEnemySeparation;
        for (int i = 0; i < placed.Count; i++)
            if (Vector2.Distance(placed[i], candidate) < sep) return false;

        return true;
    }

    Vector2 InstantiateEnemyAt(Vector2 pos)
    {
        var go = Instantiate(enemyPrefab, pos, Quaternion.identity);
        var chaser = go.GetComponent<EnemyChaser>();
        if (chaser)
        {
            chaser.obstacleMask = obstacleMask;
            if (!chaser.player && player) chaser.player = player;
        }

        placed.Add(pos);
        return pos;
    }

    bool InsideArena(Vector2 pos)
    {
        if (!arenaCollider) return true; // backwards-compatible if not set
        var b = arenaCollider.bounds;
        float minX = b.min.x + arenaPadding;
        float maxX = b.max.x - arenaPadding;
        float minY = b.min.y + arenaPadding;
        float maxY = b.max.y - arenaPadding;
        return (pos.x > minX && pos.x < maxX && pos.y > minY && pos.y < maxY);
    }
}

