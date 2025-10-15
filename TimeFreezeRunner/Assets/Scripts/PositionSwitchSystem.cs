using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class PositionSwitchSystem : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public LayerMask obstacleMask;
    public GameObject markerPrefab;
    public GameObject ringCalm;
    public GameObject ringUrgent;

    [Header("Availability")]
    public float triggerRadius = 3.8f;
    public float dangerRadius = 2.6f;
    public float cooldownSeconds = 6f;

    [Header("Sampling")]
    public float[] sampleRadii = new float[] { 3.5f, 4.75f, 6.0f, 7.25f };
    public int sampleDirections = 24;
    public int jitterPerDirection = 2;
    public float radialJitter = 0.35f;
    public float angularJitterDeg = 6f;

    [Header("Safety")]
    public float minEnemyDistance = 1.25f;
    public float spotCheckRadius = 0.38f;
    public bool requireLineOfSight = true;
    public float minCoinDistance = 1.3f;

    [Header("Pair Rules")]
    public float minSpotSeparation = 5.0f;
    public float pairSeparationWeight = 4.0f;
    public int pairPoolSize = 48;

    [Header("Density Bias")]
    public float enemyDensityRadius = 4.0f;
    public float enemyDensityWeight = 1.5f;

    [Header("Search Expansion")]
    public int expandAttempts = 3;
    public float expandRadiusStep = 1.1f;
    public int expandDirectionsStep = 4;
    public float relaxEnemyDistanceStep = 0.1f;

    [Header("Input")]
    public KeyCode activateKey = KeyCode.Space;

    [Header("UI")]
    public string noSpotMsg = "No safe switch!";
    public string notAvailableMsg = "No position switch available!";
    public string noChargesMsg = "No switches remaining!";
    public TMP_Text switchesText;
    public string switchesFormat = "Switches: {0}/{1}";

    [Header("Charges")]
    public int maxSwitches = 2;

    [Header("Glow/Probe Timing")]
    public float probeInterval = 0.08f;
    public float pairCacheTTL = 0.30f;
    public float ringHoldSeconds = 0.25f;

    bool _targeting = false;
    float _lastUsedAt = -999f;
    readonly List<GameObject> _markers = new();
    Vector3[] _spots = new Vector3[0];
    int _sel = 0;
    int _switchesUsed = 0;

    Transform[] _coinTransforms;

    float _nextProbeAt = 0f;
    Vector3[] _pairCache = null;
    float _pairCacheValidUntil = -999f;
    float _lastUsableAt = -999f;

    void Awake() { SetRing(false, false); }
    void OnEnable() { SetRing(false, false); _pairCache = null; _pairCacheValidUntil = -999f; _lastUsableAt = -999f; _nextProbeAt = 0f; }

    void Start()
    {
        var coins = FindObjectsOfType<Coin>();
        _coinTransforms = new Transform[coins.Length];
        for (int i = 0; i < coins.Length; i++) _coinTransforms[i] = coins[i].transform;
        UpdateSwitchesUI();
    }

    void Reset()
    {
        if (!player) player = FindObjectOfType<PlayerController>()?.transform;
    }

    void Update()
    {
        if (!player || GameManager.I == null) { SetRing(false, false); return; }
        if (!GameManager.I.IsPlaying) { SetRing(false, false); return; }
        if (_targeting) { UpdateTargetingInput(); return; }

        bool hasCharges = _switchesUsed < maxSwitches;
        bool offCd = IsOffCooldown();
        float nearest = DistToNearestEnemy(player.position);
        bool enemyInRange = nearest <= triggerRadius;
        bool urgent = nearest <= dangerRadius;

        bool wantProbe = hasCharges && offCd && enemyInRange;
        if (wantProbe && Time.unscaledTime >= _nextProbeAt)
        {
            var pair = FindPairWithExpansion();
            if (pair != null && pair.Length == 2)
            {
                _pairCache = pair;
                _pairCacheValidUntil = Time.unscaledTime + pairCacheTTL;
                _lastUsableAt = Time.unscaledTime;
            }
            _nextProbeAt = Time.unscaledTime + probeInterval;
        }

        bool pairAlive = (_pairCache != null && _pairCache.Length == 2 && Time.unscaledTime <= _pairCacheValidUntil);
        bool usableNow = hasCharges && offCd && enemyInRange && pairAlive;

        bool showRing = hasCharges && (usableNow || (Time.unscaledTime - _lastUsableAt <= ringHoldSeconds && pairAlive));
        SetRing(showRing, urgent);

        if (Input.GetKeyDown(activateKey))
        {
            if (!hasCharges) { GameManager.I.ui?.ShowIdleToast(noChargesMsg, 0.8f); return; }

            if (!usableNow)
            {
                if (hasCharges && (Time.unscaledTime - _lastUsableAt <= ringHoldSeconds && pairAlive))
                {
                    BeginTargetingWithCachedPair();
                }
                else
                {
                    GameManager.I.ui?.ShowIdleToast(notAvailableMsg, 0.8f);
                }
                return;
            }

            BeginTargetingWithCachedPair();
        }
    }

    void BeginTargetingWithCachedPair()
    {
        if (_pairCache == null || _pairCache.Length != 2) { GameManager.I.ui?.ShowIdleToast(noSpotMsg, 0.8f); return; }

        _spots = OrderSpotsForInput(_pairCache);

        ClearMarkers();
        for (int i = 0; i < _spots.Length; i++)
            _markers.Add(Instantiate(markerPrefab, _spots[i], Quaternion.identity));

        _sel = 0;
        _targeting = true;
        SetRing(false, false);
    }

    Vector3[] OrderSpotsForInput(Vector3[] inSpots)
    {
        if (inSpots == null || inSpots.Length != 2) return inSpots;
        var cam = Camera.main; if (!cam) return inSpots;
        Vector3 a = cam.WorldToScreenPoint(inSpots[0]);
        Vector3 b = cam.WorldToScreenPoint(inSpots[1]);
        if (a.x <= b.x) return inSpots;
        return new Vector3[] { inSpots[1], inSpots[0] };
    }

    void UpdateTargetingInput()
    {
        if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0f) { ExitTargeting(false); return; }
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(activateKey)) { ExitTargeting(false); return; }

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) _sel = (_sel - 1 + _spots.Length) % _spots.Length;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) _sel = (_sel + 1) % _spots.Length;

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) { TryTeleport(0); return; }
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) { TryTeleport(1); return; }

        if (Input.GetMouseButtonDown(0))
        {
            int idx = NearestMarkerToMouse(0.8f);
            if (idx != -1) { TryTeleport(idx); return; }
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) { TryTeleport(_sel); }

        for (int i = 0; i < _markers.Count; i++)
        {
            if (!_markers[i]) continue;
            float t = (i == _sel) ? 1.15f : 1.0f;
            _markers[i].transform.localScale = Vector3.Lerp(_markers[i].transform.localScale, new Vector3(t, t, 1f), 0.3f);
        }
    }

    void TryTeleport(int idx)
    {
        if (idx < 0 || idx >= _spots.Length) return;
        Vector3 p = _spots[idx];

        if (Physics2D.OverlapCircle(p, spotCheckRadius, obstacleMask))
        {
            GameManager.I.ui?.ShowIdleToast(noSpotMsg, 0.8f);
            return;
        }

        var pos = player.position; pos.x = p.x; pos.y = p.y; player.position = pos;

        _lastUsedAt = Time.unscaledTime;
        _switchesUsed = Mathf.Min(_switchesUsed + 1, maxSwitches);
        UpdateSwitchesUI();
        GameManager.I.ui?.ShowIdleToast(_switchesUsed + "/" + maxSwitches + " position switches used", 0.9f);

        ExitTargeting(true);

        if (_switchesUsed >= maxSwitches)
        {
            _pairCache = null;
            _pairCacheValidUntil = -999f;
            _lastUsableAt = -999f;
            SetRing(false, false);
        }
        else
        {
            _pairCache = null;
            _pairCacheValidUntil = -999f;
        }
    }

    void ExitTargeting(bool used)
    {
        ClearMarkers();
        _targeting = false;
    }

    void SetRing(bool show, bool urgent)
    {
        if (ringCalm) ringCalm.SetActive(show && !urgent);
        if (ringUrgent) ringUrgent.SetActive(show && urgent);
    }

    void UpdateSwitchesUI()
    {
        if (switchesText) switchesText.text = string.Format(switchesFormat, maxSwitches - _switchesUsed, maxSwitches);
    }

    bool IsOffCooldown() => cooldownSeconds <= 0f || (Time.unscaledTime - _lastUsedAt) >= cooldownSeconds;

    Vector3[] FindPairWithExpansion()
    {
        float enemyGap = minEnemyDistance;
        List<float> radii = new List<float>(sampleRadii);
        int dirs = sampleDirections;

        for (int attempt = 0; attempt <= expandAttempts; attempt++)
        {
            var pool = BuildScoredPool(radii, dirs, enemyGap, minCoinDistance);
            if (pool.Count >= 2)
            {
                var pair = PickBestSeparatedPair(pool);
                if (pair.Length == 2) return pair;
            }

            float last = radii.Count > 0 ? radii[radii.Count - 1] : 4f;
            radii.Add(last + expandRadiusStep);
            dirs += expandDirectionsStep;
            enemyGap = Mathf.Max(0.6f, enemyGap - relaxEnemyDistanceStep);
        }
        return new Vector3[0];
    }

    struct ScoredPoint { public Vector3 pos; public float score; }

    List<ScoredPoint> BuildScoredPool(List<float> radii, int directions, float enemyGap, float coinGap)
    {
        List<Vector3> candidates = new List<Vector3>();
        Vector3 origin = player.position;
        for (int r = 0; r < radii.Count; r++)
        {
            for (int i = 0; i < directions; i++)
            {
                float baseAng = (Mathf.PI * 2f) * (i / (float)directions);
                for (int j = 0; j < jitterPerDirection; j++)
                {
                    float ang = baseAng + Mathf.Deg2Rad * Random.Range(-angularJitterDeg, angularJitterDeg);
                    float rad = radii[r] + Random.Range(-radialJitter, radialJitter);
                    Vector3 cand = origin + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * rad;
                    if (IsSpotValid(cand, enemyGap, coinGap)) candidates.Add(cand);
                }
            }
        }
        if (candidates.Count == 0) return new List<ScoredPoint>();

        var enemies = FindObjectsOfType<EnemyChaser>();
        int[] density = new int[candidates.Count];
        int maxD = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            int d = 0;
            for (int e = 0; e < enemies.Length; e++)
            {
                if (!enemies[e]) continue;
                if (Vector2.Distance(candidates[i], enemies[e].transform.position) <= enemyDensityRadius) d++;
            }
            density[i] = d;
            if (d > maxD) maxD = d;
        }

        List<ScoredPoint> pool = new List<ScoredPoint>(candidates.Count);
        for (int i = 0; i < candidates.Count; i++)
        {
            float distEnemy = DistToNearestEnemy(candidates[i]);
            float losBonus = requireLineOfSight && HasLineOfSight(origin, candidates[i]) ? 0.6f : 0f;
            float coinBonus = Mathf.Clamp01(DistToNearestCoin(candidates[i], 8f)) * 0.4f;
            float densPenalty = enemyDensityWeight * (maxD > 0 ? (density[i] / (float)maxD) : 0f);
            float s = distEnemy + losBonus + coinBonus - densPenalty;
            pool.Add(new ScoredPoint { pos = candidates[i], score = s });
        }

        pool = pool.OrderByDescending(p => p.score).Take(Mathf.Min(pairPoolSize, pool.Count)).ToList();
        return pool;
    }

    Vector3[] PickBestSeparatedPair(List<ScoredPoint> pool)
    {
        float best = float.NegativeInfinity;
        Vector3 aBest = Vector3.zero, bBest = Vector3.zero;

        for (int i = 0; i < pool.Count; i++)
        {
            for (int j = i + 1; j < pool.Count; j++)
            {
                float sep = Vector2.Distance(pool[i].pos, pool[j].pos);
                if (sep < minSpotSeparation) continue;
                float pairScore = pool[i].score + pool[j].score + sep * pairSeparationWeight;
                if (pairScore > best)
                {
                    best = pairScore;
                    aBest = pool[i].pos;
                    bBest = pool[j].pos;
                }
            }
        }

        if (best == float.NegativeInfinity) return new Vector3[0];
        return new Vector3[] { aBest, bBest };
    }

    bool IsSpotValid(Vector3 p, float enemyGap, float coinGap)
    {
        if (Physics2D.OverlapCircle(p, spotCheckRadius, obstacleMask)) return false;
        if (DistToNearestEnemy(p) < enemyGap) return false;
        if (coinGap > 0f && DistToNearestCoin(p, 10f) < coinGap) return false;
        if (requireLineOfSight && !HasLineOfSight(player.position, p)) return false;
        return true;
    }

    float DistToNearestEnemy(Vector3 p)
    {
        float best = float.PositiveInfinity;
        var enemies = FindObjectsOfType<EnemyChaser>();
        for (int i = 0; i < enemies.Length; i++)
        {
            var e = enemies[i]; if (!e) continue;
            float d = Vector2.Distance(p, e.transform.position);
            if (d < best) best = d;
        }
        return float.IsInfinity(best) ? 999f : best;
    }

    float DistToNearestCoin(Vector3 p, float searchLimit)
    {
        if (_coinTransforms == null || _coinTransforms.Length == 0) return 999f;
        float best = 999f;
        for (int i = 0; i < _coinTransforms.Length; i++)
        {
            var t = _coinTransforms[i]; if (!t) continue;
            float d = Vector2.Distance(p, t.position);
            if (d < best) best = d;
        }
        return best;
    }

    bool HasLineOfSight(Vector3 a, Vector3 b)
    {
        Vector2 dir = (b - a);
        float dist = dir.magnitude;
        if (dist <= 0.01f) return true;
        dir /= dist;
        var hit = Physics2D.Raycast(a, dir, dist, obstacleMask);
        return hit.collider == null;
    }

    int NearestMarkerToMouse(float maxPickDist)
    {
        if (_markers.Count == 0) return -1;
        Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition); world.z = 0f;
        float best = maxPickDist; int idx = -1;
        for (int i = 0; i < _markers.Count; i++)
        {
            if (!_markers[i]) continue;
            float d = Vector2.Distance(world, _markers[i].transform.position);
            if (d < best) { best = d; idx = i; }
        }
        return idx;
    }

    void ClearMarkers()
    {
        for (int i = 0; i < _markers.Count; i++) if (_markers[i]) Destroy(_markers[i]);
        _markers.Clear();
    }

    void OnDisable()
    {
        ClearMarkers();
        SetRing(false, false);
    }
}
