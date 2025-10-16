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

//     // === Add below inside EnemySpawner class ===
//     public List<Vector2> SpawnAtPositions(IEnumerable<Vector2> positions)
//     {
//         var created = new List<Vector2>();
//         if (!enemyPrefab) return created;

//         foreach (var pos in positions)
//         {
//             var go = Instantiate(enemyPrefab, pos, Quaternion.identity);
//             var chaser = go.GetComponent<EnemyChaser>();
//             if (chaser) chaser.obstacleMask = obstacleMask;

//             placed.Add(pos);
//             created.Add(pos);
//         }
//         return created;
//     }

//     // Spawns `count` additional enemies using the same radial/spacing rules.
//     // Returns the exact positions used (so caller can remember them for future waves).
//     //New function added
//     public List<Vector2> SpawnExtra(int count)
//     {
//         var created = new List<Vector2>();
//         if (!player || !enemyPrefab || count <= 0) return created;

//         Vector2 p = player.position;
//         float baseStep = 360f / Mathf.Max(1, count);

//         for (int i = 0; i < count; i++)
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
//                 created.Add(candidate);
//                 placedOK = true;
//             }
//         }

//         return created;
//     }

// }


using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;
    public GameObject enemyPrefab;

    [Header("Counts")]
    public int enemyCount = 8;

    [Header("Placement Rules")]
    public float safeRadiusFromPlayer = 4f;   // keep some distance from player
    public float spawnRadius = 11f;           // base ring radius around player
    public float angleJitterDeg = 10f;        // random angular jitter per slice
    public float minEnemySeparation = 3f;     // avoid clumping
    public LayerMask obstacleMask;
    public float checkRadius = 0.5f;          // physics overlap radius
    public int maxPlacementTries = 25;

    [Header("Arena Bounds (NEW)")]
    // Assign the BoxCollider2D from your white wall frame (or a child “PlayArea” with the same size).
    // We’ll treat the INSIDE of that frame as the legal spawn region by shrinking the bounds by `arenaPadding`.
    public BoxCollider2D arenaCollider;
    public float arenaPadding = 0.75f;        // shrink so we’re clearly inside the walls

    private readonly List<Vector2> placed = new();

    void Awake()
    {
        if (!player)
        {
            var pc = FindObjectOfType<PlayerController>();
            if (pc) player = pc.transform;
        }
    }

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
                {
                    if (Vector2.Distance(pos, candidate) < minEnemySeparation)
                    { farFromOthers = false; break; }
                }
                if (!farFromOthers) continue;

                var go = Instantiate(enemyPrefab, candidate, Quaternion.identity);
                var chaser = go.GetComponent<EnemyChaser>();
                if (chaser)
                {
                    chaser.obstacleMask = obstacleMask;
                    if (!chaser.player && player) chaser.player = player;
                }

                placed.Add(candidate);
                placedOK = true;
            }
        }
    }

    // ---------- helpers ----------

    bool PassesRules(Vector2 pos, Vector2 playerPos)
    {
        if (!InsideArena(pos)) return false;                                       // NEW: keep inside maze
        if (Vector2.Distance(pos, playerPos) < safeRadiusFromPlayer) return false; // keep away from player
        if (Physics2D.OverlapCircle(pos, checkRadius, obstacleMask)) return false; // don’t spawn inside walls
        return true;
    }

    bool InsideArena(Vector2 pos)
    {
        if (!arenaCollider) return true; // if not set, don’t block spawns (backward compatible)

        var b = arenaCollider.bounds;
        // shrink by padding so we’re clearly within the inner rectangle of the frame
        float minX = b.min.x + arenaPadding;
        float maxX = b.max.x - arenaPadding;
        float minY = b.min.y + arenaPadding;
        float maxY = b.max.y - arenaPadding;

        return (pos.x > minX && pos.x < maxX && pos.y > minY && pos.y < maxY);
    }

    void OnDrawGizmosSelected()
    {
        if (!player) return;
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(player.position, safeRadiusFromPlayer);
        Gizmos.color = Color.cyan;   Gizmos.DrawWireSphere(player.position, spawnRadius);

        if (arenaCollider)
        {
            var b = arenaCollider.bounds;
            var min = new Vector3(b.min.x + arenaPadding, b.min.y + arenaPadding, 0);
            var max = new Vector3(b.max.x - arenaPadding, b.max.y - arenaPadding, 0);
            var size = max - min;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube((min + max) * 0.5f, new Vector3(size.x, size.y, 0.01f));
        }
    }

    // ================== Public API used by GameManager ==================

    public List<Vector2> SpawnAtPositions(IEnumerable<Vector2> positions)
    {
        var created = new List<Vector2>();
        if (!enemyPrefab) return created;

        foreach (var pos in positions)
        {
            // Safety: if recorded baseline ever drifts, refuse out-of-arena spawns
            if (!InsideArena(pos)) continue;

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

    /// <summary>
    /// Spawns exactly `count` more enemies using spacing/obstacle rules around the player.
    /// Guarantees it doesn’t exceed `count`. If placement fails too many times, it may return fewer.
    /// </summary>
    // public List<Vector2> SpawnExtra(int count)
    // {
    //     var created = new List<Vector2>();
    //     if (!player || !enemyPrefab || count <= 0) return created;

    //     Vector2 p = player.position;
    //     float baseStep = 360f / Mathf.Max(1, count);

    //     for (int i = 0; i < count; i++)
    //     {
    //         bool placedOK = false;

    //         for (int t = 0; t < maxPlacementTries && !placedOK; t++)
    //         {
    //             float angle = (i * baseStep) + Random.Range(-angleJitterDeg, angleJitterDeg);
    //             float rad = angle * Mathf.Deg2Rad;
    //             float r = spawnRadius + Random.Range(-1.0f, 1.0f);
    //             Vector2 candidate = p + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * r;

    //             if (!PassesRules(candidate, p)) continue;

    //             bool farFromOthers = true;
    //             for (int k = 0; k < placed.Count; k++)
    //             {
    //                 if (Vector2.Distance(placed[k], candidate) < minEnemySeparation)
    //                 { farFromOthers = false; break; }
    //             }
    //             if (!farFromOthers) continue;

    //             var go = Instantiate(enemyPrefab, candidate, Quaternion.identity);
    //             var chaser = go.GetComponent<EnemyChaser>();
    //             if (chaser)
    //             {
    //                 chaser.obstacleMask = obstacleMask;
    //                 if (!chaser.player && player) chaser.player = player;
    //             }

    //             placed.Add(candidate);
    //             created.Add(candidate);
    //             placedOK = true;
    //         }
    //     }

    //     return created;
    // }
    // REPLACE your SpawnExtra with this version
    public List<Vector2> SpawnExtra(int count)
    {
        var created = new List<Vector2>();
        if (!player || !enemyPrefab || count <= 0) return created;

        Vector2 p = player.position;

        int hardCap = Mathf.Max(200, maxPlacementTries * count * 4); // safety so we never loop forever
        int attempts = 0;
        int target = count;

        // We’ll try until we place exactly `count` enemies or hit a very high attempt cap.
        while (created.Count < target && attempts < hardCap)
        {
            attempts++;

            // 1) Try your original ring sampling around the player
            if (TryPickCandidateRing(p, out Vector2 cand1))
            {
                if (PassesRules(cand1, p) && IsFarFromOthers(cand1, minEnemySeparation))
                {
                    created.Add(InstantiateEnemyAt(cand1));
                    continue;
                }
            }

            // 2) Try uniform sample INSIDE arena bounds (not relying on ring radius)
            if (TryPickCandidateInsideArena(out Vector2 cand2))
            {
                if (PassesRules(cand2, p) && IsFarFromOthers(cand2, minEnemySeparation))
                {
                    created.Add(InstantiateEnemyAt(cand2));
                    continue;
                }
            }

            // 3) Last-resort: small local spiral around a uniform point to “nudge” into legality
            if (TryPickCandidateInsideArena(out Vector2 seed))
            {
                const int spiralSteps = 14;
                const float stepRadius = 0.35f;
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                bool placedOK = false;

                for (int s = 0; s < spiralSteps; s++)
                {
                    Vector2 probe = seed + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (s * stepRadius);

                    // (Optional) relax separation a tiny bit in the last few probes
                    float sep = (s > spiralSteps * 0.6f) ? minEnemySeparation * 0.85f : minEnemySeparation;

                    if (PassesRules(probe, p) && IsFarFromOthers(probe, sep))
                    {
                        created.Add(InstantiateEnemyAt(probe));
                        placedOK = true;
                        break;
                    }

                    angle += 37f * Mathf.Deg2Rad; // a “golden-ish” step to avoid repeating angles
                }

                if (placedOK) continue;
            }
        }

        return created;
    }

    // --- helpers ---

    // Uses your original “ring” strategy (angle + jitter + radius), but as a single-pick helper.
    bool TryPickCandidateRing(Vector2 playerPos, out Vector2 candidate)
    {
        float angle = Random.Range(0f, 360f) + Random.Range(-angleJitterDeg, angleJitterDeg);
        float rad = angle * Mathf.Deg2Rad;
        float r = spawnRadius + Random.Range(-1.0f, 1.0f);
        candidate = playerPos + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * r;

        // If you’re using ArenaBounds via BoxCollider2D:
        if (arenaCollider)
        {
            var b = arenaCollider.bounds;
            float minX = b.min.x + arenaPadding;
            float maxX = b.max.x - arenaPadding;
            float minY = b.min.y + arenaPadding;
            float maxY = b.max.y - arenaPadding;
            // clamp into arena so the first pass is already inside
            candidate.x = Mathf.Clamp(candidate.x, minX, maxX);
            candidate.y = Mathf.Clamp(candidate.y, minY, maxY);
        }
        return true;
    }

    // Uniform random inside the arena rectangle (works only if arenaCollider assigned).
    bool TryPickCandidateInsideArena(out Vector2 candidate)
    {
        candidate = Vector2.zero;

        if (!arenaCollider) return false;

        var b = arenaCollider.bounds;
        float minX = b.min.x + arenaPadding;
        float maxX = b.max.x - arenaPadding;
        float minY = b.min.y + arenaPadding;
        float maxY = b.max.y - arenaPadding;

        candidate = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
        return true;
    }

    // Distance check vs. already placed enemies (and those spawned this session)
    bool IsFarFromOthers(Vector2 pos, float required)
    {
        for (int i = 0; i < placed.Count; i++)
            if (Vector2.Distance(placed[i], pos) < required) return false;
        return true;
    }

    // Spawns and records
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

}
