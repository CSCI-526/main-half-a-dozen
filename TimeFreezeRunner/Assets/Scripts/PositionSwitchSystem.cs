// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;
// using TMPro;

// public class PositionSwitchSystem : MonoBehaviour
// {
//     public static bool IsTargetingGlobal = false;

//     [Header("References")]
//     public Transform player;
//     public LayerMask obstacleMask;
//     public GameObject markerPrefab;
//     public GameObject ringCalm;
//     public GameObject ringUrgent;

//     [Header("Level Bounds")]
//     [Tooltip("Collider outlining the playable maze area (Box/Polygon/Composite Collider 2D).")]
//     public Collider2D levelBounds;
//     [Tooltip("How far inside the maze we force final points (world units).")]
//     public float boundsPadding = 0.25f;

//     [Header("Availability (not used for gating)")]
//     public float triggerRadius = 3.8f;
//     public float dangerRadius = 2.6f;
//     public float cooldownSeconds = 0f; // free-trigger mode

//     [Header("Sampling")]
//     public float[] sampleRadii = new float[] { 3.5f, 4.75f, 6.0f, 7.25f };
//     public int sampleDirections = 24;
//     public int jitterPerDirection = 2;
//     public float radialJitter = 0.35f;
//     public float angularJitterDeg = 6f;

//     [Header("Safety")]
//     public float minEnemyDistance = 1.25f;
//     public float spotCheckRadius = 0.38f;
//     public bool requireLineOfSight = true;
//     public float minCoinDistance = 1.3f;

//     [Header("Pair Rules")]
//     public float minSpotSeparation = 5.0f;
//     public float pairSeparationWeight = 4.0f;
//     public int pairPoolSize = 48;

//     [Header("Density Bias")]
//     public float enemyDensityRadius = 4.0f;
//     public float enemyDensityWeight = 1.5f;

//     [Header("Search Expansion")]
//     public int expandAttempts = 3;
//     public float expandRadiusStep = 1.1f;
//     public int expandDirectionsStep = 4;
//     public float relaxEnemyDistanceStep = 0.1f;

//     [Header("Input")]
//     public KeyCode activateKey = KeyCode.Space;

//     [Header("UI")]
//     public string noSpotMsg = "No safe switch!";
//     public string noChargesMsg = "Maxed out position switches.";

//     [Tooltip("TMP text in top-right corner. Format uses switchesFormat: {0} = used, {1} = total.")]
//     public TMP_Text switchesText;

//     [Tooltip("UI format string. {0} = used, {1} = total.")]
//     public string switchesFormat = "Teleport: {0}/{1}";

//     [Header("Charges")]
//     public int maxSwitches = 2;

//     [Header("Targeting UX")]
//     public float pairCacheTTL = 0.50f; // brief grace while choosing

//     // ===== internals =====
//     bool _targeting = false;
//     float _lastUsedAt = -999f;
//     readonly List<GameObject> _markers = new();
//     Vector3[] _spots = new Vector3[0];
//     int _sel = 0;
//     int _switchesUsed = 0;

//     Transform[] _coinTransforms;

//     Vector3[] _pairCache = null;
//     float _pairCacheValidUntil = -999f;

//     void Awake()
//     {
//         SetRing(false, false);
//     }

//     void OnEnable()
//     {
//         SetRing(false, false);
//         _pairCache = null;
//         _pairCacheValidUntil = -999f;
//         IsTargetingGlobal = false;
//     }

//     void Start()
//     {
//         var coins = FindObjectsOfType<Coin>();
//         _coinTransforms = new Transform[coins.Length];
//         for (int i = 0; i < coins.Length; i++) _coinTransforms[i] = coins[i].transform;

//         // Initialize UI to 0 used / max
//         UpdateSwitchesUI();
//     }

//     void Reset()
//     {
//         if (!player) player = FindObjectOfType<PlayerController>()?.transform;
//     }

//     // === FREE-TRIGGER MODE: Space always enters targeting and spawns EXACTLY TWO bounded spots ===
//     void Update()
//     {
//         if (!player || GameManager.I == null)
//         {
//             SetRing(false, false);
//             return;
//         }
//         if (!GameManager.I.IsPlaying)
//         {
//             SetRing(false, false);
//             return;
//         }

//         if (_targeting)
//         {
//             UpdateTargetingInput();
//             return;
//         }

//         SetRing(false, false);

//         if (Input.GetKeyDown(activateKey))
//         {
//             if (_switchesUsed >= maxSwitches)
//             {
//                 GameManager.I.ui?.ShowIdleToast(noChargesMsg, 0.9f);
//                 return;
//             }

//             // Build two spots NOW (guaranteed and bounded).
//             var two = BuildExactlyTwoSpotsGuaranteed();
//             _pairCache = two;
//             _pairCacheValidUntil = Time.unscaledTime + pairCacheTTL;

//             BeginTargetingWithCachedPair();
//         }
//     }

//     // === ALWAYS return exactly two valid, inside-maze spots ===
//     Vector3[] BuildExactlyTwoSpotsGuaranteed()
//     {
//         const int MAX_RETRIES = 3;
//         for (int attempt = 0; attempt < MAX_RETRIES; attempt++)
//         {
//             var pair = TryBuildPairGuaranteed(); // may return 0/1/2
//             var two = EnsureTwoSpots(pair);
//             if (two.Length == 2) return two;
//         }
//         // Absolute final fallback if rare failure persists
//         return EnsureTwoSpots(new Vector3[0]);
//     }

//     // Post-process to ensure exactly two spots, clamped inside bounds
//     Vector3[] EnsureTwoSpots(Vector3[] input)
//     {
//         List<Vector3> valid = new();

//         // 1) Keep only truly open points and clamp inside (so nothing slips outside).
//         if (input != null)
//         {
//             for (int i = 0; i < input.Length; i++)
//             {
//                 Vector3 p = ClampInsideBounds(input[i], boundsPadding);
//                 if (IsSpotOpen(p)) valid.Add(p);
//             }
//         }

//         // 2) Deduplicate near-identical points
//         float dupEps = 0.15f;
//         for (int i = valid.Count - 1; i >= 0; i--)
//         {
//             for (int j = 0; j < i; j++)
//             {
//                 if (Vector2.Distance(valid[i], valid[j]) <= dupEps)
//                 {
//                     valid.RemoveAt(i);
//                     break;
//                 }
//             }
//         }

//         // 3) If 2+ remain: choose the farthest-apart two
//         if (valid.Count >= 2)
//         {
//             float best = -1f;
//             Vector3 A = valid[0], B = valid[1];
//             for (int i = 0; i < valid.Count; i++)
//             {
//                 for (int j = i + 1; j < valid.Count; j++)
//                 {
//                     float d = Vector2.Distance(valid[i], valid[j]);
//                     if (d > best)
//                     {
//                         best = d; A = valid[i]; B = valid[j];
//                     }
//                 }
//             }
//             A = ClampInsideBounds(A, boundsPadding);
//             B = ClampInsideBounds(B, boundsPadding);
//             return new[] { A, B };
//         }

//         // 4) If exactly 1 remains: synthesize a second around it
//         if (valid.Count == 1)
//         {
//             Vector3 a = ClampInsideBounds(valid[0], boundsPadding);
//             Vector3 b = SynthesizeSecondFrom(a);
//             b = ClampInsideBounds(b, boundsPadding);
//             if (!IsSpotOpen(b)) b = FindNearestOpenInside(b);
//             if (!IsSpotOpen(b)) b = FindNearestOpenInside(a);
//             a = ClampInsideBounds(a, boundsPadding);
//             b = ClampInsideBounds(b, boundsPadding);
//             return new[] { a, b };
//         }

//         // 5) If none remain: build a deterministic pair near the player
//         var forced = ForceOpenPairNear(player.position, Mathf.Max(1.0f, spotCheckRadius * 2f), 0.5f, 12f);
//         if (forced.Length != 2)
//         {
//             // Last resort: left/right nudged inside
//             Vector3 left  = ClampInsideBounds(player.position + Vector3.left  * Mathf.Max(1.2f, spotCheckRadius * 3f), boundsPadding);
//             Vector3 right = ClampInsideBounds(player.position + Vector3.right * Mathf.Max(1.2f, spotCheckRadius * 3f), boundsPadding);
//             left  = IsSpotOpen(left)  ? left  : FindNearestOpenInside(left);
//             right = IsSpotOpen(right) ? right : FindNearestOpenInside(right);
//             left  = ClampInsideBounds(left, boundsPadding);
//             right = ClampInsideBounds(right, boundsPadding);
//             return new[] { left, right };
//         }
//         forced[0] = ClampInsideBounds(forced[0], boundsPadding);
//         forced[1] = ClampInsideBounds(forced[1], boundsPadding);
//         return forced;
//     }

//     // --- Pair construction with progressive relaxation + bounded fallbacks ---
//     Vector3[] TryBuildPairGuaranteed()
//     {
//         Vector3 origin = player.position;

//         // 1) Original constraints
//         var p1 = FindPairWithParams(sampleRadii.ToList(), sampleDirections, jitterPerDirection,
//                                     radialJitter, angularJitterDeg,
//                                     minEnemyDistance, minCoinDistance, requireLineOfSight,
//                                     expandAttempts, expandRadiusStep, expandDirectionsStep, relaxEnemyDistanceStep);
//         if (p1.Length == 2) return p1;

//         // 2) Relax coins and LoS
//         var p2 = FindPairWithParams(sampleRadii.ToList(), sampleDirections + 8, jitterPerDirection + 1,
//                                     radialJitter * 1.15f, angularJitterDeg * 1.2f,
//                                     Mathf.Max(0.8f, minEnemyDistance * 0.8f), 0f, false,
//                                     expandAttempts + 2, expandRadiusStep * 1.1f, expandDirectionsStep + 4, relaxEnemyDistanceStep * 1.2f);
//         if (p2.Length == 2) return p2;

//         // 3) Wider search
//         var wide = new List<float>(sampleRadii);
//         float last = wide.Count > 0 ? wide[wide.Count - 1] : 4f;
//         wide.Add(last + 2f);
//         wide.Add(last + 4f);
//         wide.Add(last + 6f);
//         var p3 = FindPairWithParams(wide, 48, jitterPerDirection + 2,
//                                     radialJitter * 1.3f, angularJitterDeg * 1.3f,
//                                     0.7f, 0f, false,
//                                     expandAttempts + 4, expandRadiusStep * 1.2f, expandDirectionsStep + 8, relaxEnemyDistanceStep * 1.3f);
//         if (p3.Length == 2) return p3;

//         // 4) Brute-force (bounded)
//         var p4 = BruteForcePair(origin, 600, Mathf.Max(2.0f, minSpotSeparation * 0.6f));
//         if (p4.Length == 2) return p4;

//         // 5) Deterministic bounded ring-walk
//         return ForceOpenPairNear(origin, Mathf.Max(1.0f, spotCheckRadius * 2f), 0.5f, 12f);
//     }

//     Vector3[] FindPairWithParams(
//         List<float> radii, int directions, int jitters,
//         float radialJit, float angularJit,
//         float enemyGap, float coinGap, bool requireLoSNow,
//         int expand, float expandRadStep, int expandDirStep, float relaxEnemyStep)
//     {
//         for (int attempt = 0; attempt <= expand; attempt++)
//         {
//             var pool = BuildScoredPool(radii, directions, jitters, radialJit, angularJit, enemyGap, coinGap, requireLoSNow);
//             if (pool.Count >= 2)
//             {
//                 var pair = PickBestSeparatedPair(pool, Mathf.Max(1.0f, minSpotSeparation * (requireLoSNow ? 1f : 0.8f)));
//                 if (pair.Length == 2) return pair;
//             }

//             float last = radii.Count > 0 ? radii[radii.Count - 1] : 4f;
//             radii.Add(last + expandRadStep);
//             directions += expandDirStep;
//             enemyGap = Mathf.Max(0.45f, enemyGap - relaxEnemyStep);
//         }
//         return new Vector3[0];
//     }

//     Vector3[] BruteForcePair(Vector3 origin, int tries, float minSep)
//     {
//         List<Vector3> good = new();
//         for (int i = 0; i < tries; i++)
//         {
//             float r = 1.0f + i * 0.02f + Random.Range(0f, 0.75f);
//             float ang = Random.Range(0f, Mathf.PI * 2f);
//             Vector3 cand = origin + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * r;

//             cand = ClampInsideBounds(cand, boundsPadding);
//             if (IsSpotOpen(cand) && IsOK_EnemyCoinLoS(cand, 0.5f, 0f, false)) good.Add(cand);
//             if (good.Count >= 100) break;
//         }
//         if (good.Count < 2) return new Vector3[0];

//         // farthest-apart two
//         Vector3 a = good[0], b = good[1];
//         float best = -1f;
//         for (int i = 0; i < good.Count; i++)
//         {
//             for (int j = i + 1; j < good.Count; j++)
//             {
//                 float d = Vector2.Distance(good[i], good[j]);
//                 if (d >= minSep && d > best) { best = d; a = good[i]; b = good[j]; }
//             }
//         }
//         if (best <= 0f) return new Vector3[0];
//         return new[] { a, b };
//     }

//     Vector3[] ForceOpenPairNear(Vector3 origin, float startRadius, float stepRadius, float maxRadius)
//     {
//         for (float r = startRadius; r <= maxRadius; r += stepRadius)
//         {
//             const int K = 64;
//             for (int i = 0; i < K; i++)
//             {
//                 float ang = (Mathf.PI * 2f) * (i / (float)K);
//                 Vector3 a = origin + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
//                 a = ClampInsideBounds(a, boundsPadding);
//                 if (!IsSpotOpen(a)) continue;

//                 float ang2 = ang + Mathf.PI;
//                 Vector3 b = origin + new Vector3(Mathf.Cos(ang2), Mathf.Sin(ang2)) * r;
//                 b = ClampInsideBounds(b, boundsPadding);
//                 if (!IsSpotOpen(b))
//                 {
//                     bool found = false;
//                     for (int j = -6; j <= 6 && !found; j++)
//                     {
//                         float aJ = ang2 + (j * Mathf.Deg2Rad * 5f);
//                         Vector3 b2 = origin + new Vector3(Mathf.Cos(aJ), Mathf.Sin(aJ)) * r;
//                         b2 = ClampInsideBounds(b2, boundsPadding);
//                         if (IsSpotOpen(b2)) { b = b2; found = true; }
//                     }
//                     if (!found) continue;
//                 }

//                 if (!IsOK_EnemyCoinLoS(a, 0.4f, 0f, false)) continue;
//                 if (!IsOK_EnemyCoinLoS(b, 0.4f, 0f, false)) continue;
//                 if (Vector2.Distance(a, b) < Mathf.Max(1.5f, minSpotSeparation * 0.5f)) continue;

//                 return new[] { a, b };
//             }
//         }

//         // Last resort: left/right nudged inside
//         Vector3 left  = ClampInsideBounds(player.position + Vector3.left  * Mathf.Max(1.2f, spotCheckRadius * 3f), boundsPadding);
//         Vector3 right = ClampInsideBounds(player.position + Vector3.right * Mathf.Max(1.2f, spotCheckRadius * 3f), boundsPadding);
//         left  = IsSpotOpen(left)  ? left  : FindNearestOpenInside(left);
//         right = IsSpotOpen(right) ? right : FindNearestOpenInside(right);
//         left  = ClampInsideBounds(left, boundsPadding);
//         right = ClampInsideBounds(right, boundsPadding);
//         return new[] { left, right };
//     }

//     // Make a second point near 'a' with decent separation, staying valid & inside bounds.
//     Vector3 SynthesizeSecondFrom(Vector3 a)
//     {
//         float minSep = Mathf.Max(1.5f, minSpotSeparation * 0.5f);
//         float r0 = Mathf.Max(1.1f, minSep * 0.6f);
//         for (float r = r0; r <= r0 + 4.0f; r += 0.25f)
//         {
//             for (int i = 0; i < 24; i++)
//             {
//                 float ang = (Mathf.PI * 2f) * (i / 24f);
//                 Vector3 b = a + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
//                 b = ClampInsideBounds(b, boundsPadding);
//                 if (Vector2.Distance(a, b) < minSep) continue;
//                 if (!IsSpotOpen(b)) continue;
//                 if (!IsOK_EnemyCoinLoS(b, 0.4f, 0f, false)) continue;
//                 return b;
//             }
//         }
//         // fallback: opposite direction nudged/clamped inside
//         Vector3 oppDir = player ? (a - player.position).normalized : Vector3.right;
//         Vector3 opp = a + oppDir * (minSep + 0.5f);
//         return ClampInsideBounds(opp, boundsPadding);
//     }

//     // ---- Targeting flow ----
//     void BeginTargetingWithCachedPair()
//     {
//         if (_pairCache == null || _pairCache.Length != 2)
//         {
//             GameManager.I.ui?.ShowIdleToast(noSpotMsg, 0.8f);
//             return;
//         }

//         _spots = OrderSpotsForInput(_pairCache);

//         ClearMarkers();
//         for (int i = 0; i < _spots.Length; i++)
//         {
//             var go = Instantiate(markerPrefab, _spots[i], Quaternion.identity);
//             _markers.Add(go);
//             AttachMarkerLabel(go, (i + 1).ToString()); // NUMBER the markers (1/2)
//         }

//         _sel = 0;
//         _targeting = true;
//         IsTargetingGlobal = true;

//         SetRing(true, false); // glow only while targeting
//     }

//     Vector3[] OrderSpotsForInput(Vector3[] inSpots)
//     {
//         if (inSpots == null || inSpots.Length != 2) return inSpots;
//         var cam = Camera.main; if (!cam) return inSpots;
//         Vector3 a = cam.WorldToScreenPoint(inSpots[0]);
//         Vector3 b = cam.WorldToScreenPoint(inSpots[1]);
//         if (a.x <= b.x) return inSpots;
//         return new Vector3[] { inSpots[1], inSpots[0] };
//     }

//     void UpdateTargetingInput()
//     {
//         // Cancel on movement or Escape/Space
//         if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0f) { ExitTargeting(false); return; }
//         if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(activateKey)) { ExitTargeting(false); return; }

//         if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) _sel = (_sel - 1 + _spots.Length) % _spots.Length;
//         if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) _sel = (_sel + 1) % _spots.Length;

//         if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) { TryTeleport(0); return; }
//         if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) { TryTeleport(1); return; }

//         if (Input.GetMouseButtonDown(0))
//         {
//             int idx = NearestMarkerToMouse(0.8f);
//             if (idx != -1) { TryTeleport(idx); return; }
//         }

//         if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) { TryTeleport(_sel); }

//         for (int i = 0; i < _markers.Count; i++)
//         {
//             if (!_markers[i]) continue;
//             float t = (i == _sel) ? 1.15f : 1.0f;
//             _markers[i].transform.localScale = Vector3.Lerp(_markers[i].transform.localScale, new Vector3(t, t, 1f), 0.3f);
//         }
//     }

//     void TryTeleport(int idx)
//     {
//         if (idx < 0 || idx >= _spots.Length) return;
//         Vector3 p = _spots[idx];

//         // Re-validate and clamp (belt and suspenders)
//         p = ClampInsideBounds(p, boundsPadding);
//         if (!IsSpotOpen(p))
//         {
//             GameManager.I.ui?.ShowIdleToast(noSpotMsg, 0.8f);
//             return;
//         }

//         var pos = player.position; pos.x = p.x; pos.y = p.y; player.position = pos;

//         _lastUsedAt = Time.unscaledTime;
//         _switchesUsed = Mathf.Min(_switchesUsed + 1, maxSwitches);
//         UpdateSwitchesUI();
//         // Removed: GameManager.I.ui?.ShowIdleToast(_switchesUsed + "/" + maxSwitches + " position switches used", 0.9f);
//         // We now only rely on the top-right indicator.

//         // BETA METRIC4 CHANGES === Analytics: log successful Position Switch ===
//         {
//             // If you ever want to *explicitly* skip Dark Maze, uncomment the guard below:
//             // if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Level2_DarkMaze") { /* do nothing */ }
//             string levelName = (LevelManager.I != null)
//                 ? $"Level{LevelManager.I.currentLevel}"
//                 : UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

//             float t = (LevelTimer.IsRunning ? LevelTimer.Elapsed : Time.timeSinceLevelLoad);
//             AnalyticsLogger.I?.LogPowerUpUse(levelName, "PositionSwitch", t);
//         }
//         // ================================================

//         ExitTargeting(true);

//         _pairCache = null;
//         _pairCacheValidUntil = -999f;

//         if (_switchesUsed >= maxSwitches)
//             SetRing(false, false);
//     }

//     void ExitTargeting(bool used)
//     {
//         ClearMarkers();
//         _targeting = false;
//         IsTargetingGlobal = false;
//         SetRing(false, false);
//     }

//     void SetRing(bool show, bool urgent)
//     {
//         if (ringCalm) ringCalm.SetActive(show && !urgent);
//         if (ringUrgent) ringUrgent.SetActive(show && urgent);
//     }

//     void UpdateSwitchesUI()
//     {
//         if (!switchesText) return;

//         int used = _switchesUsed;
//         int total = maxSwitches;

//         switchesText.text = string.Format(switchesFormat, used, total);
//     }

//     // Reset position switch chances to maximum (for try again functionality)
//     public void ResetPositionSwitchChances()
//     {
//         _switchesUsed = 0;
//         UpdateSwitchesUI();
//     }

//     // ===== LABELS =====
//     // Create/update a centered TextMeshPro label on a marker (shows "1" or "2")
//     void AttachMarkerLabel(GameObject marker, string text)
//     {
//         // topmost sprite renderer (highest order) on this marker hierarchy
//         SpriteRenderer[] srs = marker.GetComponentsInChildren<SpriteRenderer>(true);
//         SpriteRenderer topSr = null;
//         foreach (var sr in srs) if (!topSr || sr.sortingOrder >= topSr.sortingOrder) topSr = sr;

//         TextMeshPro tmp = marker.GetComponentInChildren<TextMeshPro>(true);
//         if (tmp == null)
//         {
//             var child = new GameObject("Label");
//             child.layer = marker.layer;
//             child.transform.SetParent(marker.transform);
//             child.transform.localPosition = new Vector3(0f, 0f, -0.02f); // toward camera to avoid z-fighting
//             child.transform.localRotation = Quaternion.identity;
//             child.transform.localScale = Vector3.one;

//             tmp = child.AddComponent<TextMeshPro>();
//             tmp.alignment = TextAlignmentOptions.Center;
//             tmp.enableWordWrapping = false;
//             tmp.fontStyle = FontStyles.Bold;
//         }

//         // Solid black with white outline
//         tmp.color = Color.black;
//         tmp.outlineColor = Color.white;
//         tmp.outlineWidth = 0.4f;
//         var lc = tmp.color; lc.a = 1f; tmp.color = lc;

//         tmp.text = text;

//         // size based on marker sprite height
//         float spriteH = 1.0f;
//         if (topSr && topSr.sprite) spriteH = Mathf.Max(0.25f, topSr.sprite.bounds.size.y);
//         tmp.fontSize = Mathf.Clamp(spriteH * 8f, 3f, 18f);

//         // render above the marker
//         var mr = tmp.renderer;
//         if (topSr && mr != null)
//         {
//             mr.sortingLayerID = topSr.sortingLayerID;
//             mr.sortingLayerName = topSr.sortingLayerName;
//             mr.sortingOrder = topSr.sortingOrder + 50; // big gap to be safe
//         }
//     }

//     // ===== Scoring & validation helpers (bounded) =====

//     struct ScoredPoint { public Vector3 pos; public float score; }

//     List<ScoredPoint> BuildScoredPool(
//         List<float> radii, int directions, int jitters,
//         float radialJit, float angularJit, float enemyGap, float coinGap, bool requireLoSNow)
//     {
//         List<Vector3> candidates = new();
//         Vector3 origin = player.position;

//         for (int r = 0; r < radii.Count; r++)
//         {
//             for (int i = 0; i < directions; i++)
//             {
//                 float baseAng = (Mathf.PI * 2f) * (i / (float)directions);
//                 for (int j = 0; j < jitters; j++)
//                 {
//                     float ang = baseAng + Mathf.Deg2Rad * Random.Range(-angularJit, angularJit);
//                     float rad = radii[r] + Random.Range(-radialJit, radialJit);
//                     Vector3 cand = origin + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * rad;

//                     cand = ClampInsideBounds(cand, boundsPadding);
//                     if (IsSpotOpen(cand) && IsOK_EnemyCoinLoS(cand, enemyGap, coinGap, requireLoSNow))
//                         candidates.Add(cand);
//                 }
//             }
//         }
//         if (candidates.Count == 0) return new List<ScoredPoint>();

//         var enemies = FindObjectsOfType<EnemyChaser>();
//         int[] density = new int[candidates.Count];
//         int maxD = 0;
//         for (int i = 0; i < candidates.Count; i++)
//         {
//             int d = 0;
//             for (int e = 0; e < enemies.Length; e++)
//             {
//                 if (!enemies[e]) continue;
//                 if (Vector2.Distance(candidates[i], enemies[e].transform.position) <= enemyDensityRadius) d++;
//             }
//             density[i] = d;
//             if (d > maxD) maxD = d;
//         }

//         List<ScoredPoint> pool = new(candidates.Count);
//         for (int i = 0; i < candidates.Count; i++)
//         {
//             float distEnemy = DistToNearestEnemy(candidates[i]);
//             float losBonus = (requireLoSNow && HasLineOfSight(origin, candidates[i])) ? 0.6f : 0f;
//             float coinBonus = Mathf.Clamp01(DistToNearestCoin(candidates[i], 8f)) * 0.4f;
//             float densPenalty = enemyDensityWeight * (maxD > 0 ? (density[i] / (float)maxD) : 0f);
//             float s = distEnemy + losBonus + coinBonus - densPenalty;
//             pool.Add(new ScoredPoint { pos = candidates[i], score = s });
//         }

//         pool = pool.OrderByDescending(p => p.score).Take(Mathf.Min(pairPoolSize, pool.Count)).ToList();
//         return pool;
//     }

//     Vector3[] PickBestSeparatedPair(List<ScoredPoint> pool, float minSepOverride)
//     {
//         float best = float.NegativeInfinity;
//         Vector3 aBest = Vector3.zero, bBest = Vector3.zero;

//         for (int i = 0; i < pool.Count; i++)
//         {
//             for (int j = i + 1; j < pool.Count; j++)
//             {
//                 float sep = Vector2.Distance(pool[i].pos, pool[j].pos);
//                 if (sep < minSepOverride) continue;
//                 float pairScore = pool[i].score + pool[j].score + sep * pairSeparationWeight;
//                 if (pairScore > best)
//                 {
//                     best = pairScore;
//                     aBest = pool[i].pos;
//                     bBest = pool[j].pos;
//                 }
//             }
//         }

//         if (best == float.NegativeInfinity) return new Vector3[0];
//         return new Vector3[] { aBest, bBest };
//     }

//     bool IsSpotOpen(Vector3 p)
//     {
//         // 1) Must be inside the level bounds
//         if (!IsInsideLevel(p)) return false;

//         // 2) Must not overlap walls
//         if (Physics2D.OverlapCircle(p, spotCheckRadius, obstacleMask)) return false;

//         return true;
//     }

//     bool IsOK_EnemyCoinLoS(Vector3 p, float enemyGap, float coinGap, bool requireLoSNow)
//     {
//         if (enemyGap > 0f && DistToNearestEnemy(p) < enemyGap) return false;
//         if (coinGap > 0f && DistToNearestCoin(p, 10f) < coinGap) return false;
//         if (requireLoSNow && !HasLineOfSight(player.position, p)) return false;
//         return true;
//     }

//     // Robust inside test for ANY 2D collider (rotated or not):
//     // if inside => levelBounds.ClosestPoint(p) == p (within epsilon).
//     bool IsInsideLevel(Vector3 worldPoint)
//     {
//         if (!levelBounds) return true;
//         Vector2 p = worldPoint;
//         Vector2 cp = levelBounds.ClosestPoint(p);
//         return (Vector2.SqrMagnitude(cp - p) <= 1e-6f);
//     }

//     // Clamp an arbitrary point to lie just inside the bounds by at least "padding".
//     Vector3 ClampInsideBounds(Vector3 p, float padding)
//     {
//         if (!levelBounds) return p;

//         Vector2 point = p;

//         // Already inside?
//         if (IsInsideLevel(point))
//         {
//             // If overlapping walls due to thickness, nudge inward
//             if (Physics2D.OverlapCircle(point, spotCheckRadius * 0.95f, obstacleMask))
//             {
//                 Vector2 cpEdge = levelBounds.ClosestPoint(point + Vector2.one * 0.001f); // tiny bias
//                 Vector2 dirIn = (((Vector2)levelBounds.bounds.center) - cpEdge).normalized;
//                 if (dirIn.sqrMagnitude < 1e-6f) dirIn = Vector2.up;
//                 return (Vector2)point + dirIn * Mathf.Max(padding, spotCheckRadius);
//             }
//             return point;
//         }

//         // Outside → move to boundary and then nudge inward
//         Vector2 cp = levelBounds.ClosestPoint(point);
//         Vector2 dir = (((Vector2)levelBounds.bounds.center) - cp).normalized;
//         if (dir.sqrMagnitude < 1e-6f) dir = Vector2.up;
//         return cp + dir * Mathf.Max(padding, spotCheckRadius * 0.75f);
//     }

//     Vector3 FindNearestOpenInside(Vector3 from)
//     {
//         // Spiral search constrained by bounds, clamping each probe inside first
//         for (float r = 0f; r <= 3.0f; r += 0.2f)
//         {
//             for (int i = 0; i < 28; i++)
//             {
//                 float ang = (Mathf.PI * 2f) * (i / 28f);
//                 Vector3 p = from + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
//                 p = ClampInsideBounds(p, boundsPadding);
//                 if (IsSpotOpen(p)) return p;
//             }
//         }
//         // Fallback to player's current position clamped inside
//         return ClampInsideBounds(player ? player.position : from, boundsPadding);
//     }

//     float DistToNearestEnemy(Vector3 p)
//     {
//         float best = float.PositiveInfinity;
//         var enemies = FindObjectsOfType<EnemyChaser>();
//         for (int i = 0; i < enemies.Length; i++)
//         {
//             var e = enemies[i]; if (!e) continue;
//             float d = Vector2.Distance(p, e.transform.position);
//             if (d < best) best = d;
//         }
//         return float.IsInfinity(best) ? 999f : best;
//     }

//     float DistToNearestCoin(Vector3 p, float searchLimit)
//     {
//         if (_coinTransforms == null || _coinTransforms.Length == 0) return 999f;
//         float best = 999f;
//         for (int i = 0; i < _coinTransforms.Length; i++)
//         {
//             var t = _coinTransforms[i]; if (!t) continue;
//             float d = Vector2.Distance(p, t.position);
//             if (d < best) best = d;
//         }
//         return best;
//     }

//     bool HasLineOfSight(Vector3 a, Vector3 b)
//     {
//         Vector2 dir = (b - a);
//         float dist = dir.magnitude;
//         if (dist <= 0.01f) return true;
//         dir /= dist;
//         var hit = Physics2D.Raycast(a, dir, dist, obstacleMask);
//         return hit.collider == null;
//     }

//     int NearestMarkerToMouse(float maxPickDist)
//     {
//         if (_markers.Count == 0) return -1;
//         var cam = Camera.main; if (!cam) return -1;
//         Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition); world.z = 0f;
//         float best = maxPickDist; int idx = -1;
//         for (int i = 0; i < _markers.Count; i++)
//         {
//             if (!_markers[i]) continue;
//             float d = Vector2.Distance(world, _markers[i].transform.position);
//             if (d < best) { best = d; idx = i; }
//         }
//         return idx;
//     }

//     void ClearMarkers()
//     {
//         for (int i = 0; i < _markers.Count; i++) if (_markers[i]) Destroy(_markers[i]);
//         _markers.Clear();
//     }

//     void OnDisable()
//     {
//         ClearMarkers();
//         SetRing(false, false);
//         IsTargetingGlobal = false;
//     }
// }


using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class PositionSwitchSystem : MonoBehaviour
{
    public static bool IsTargetingGlobal = false;

    [Header("References")]
    public Transform player;
    public LayerMask obstacleMask;
    public GameObject markerPrefab;
    public GameObject ringCalm;
    public GameObject ringUrgent;

    [Header("Level Bounds")]
    [Tooltip("Collider outlining the playable maze area (Box/Polygon/Composite Collider 2D).")]
    public Collider2D levelBounds;
    [Tooltip("How far inside the maze we force final points (world units).")]
    public float boundsPadding = 0.25f;

    [Header("Availability (not used for gating)")]
    public float triggerRadius = 3.8f;
    public float dangerRadius = 2.6f;
    public float cooldownSeconds = 0f; // free-trigger mode

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
    public string noChargesMsg = "Maxed out position switches.";

    [Tooltip("TMP text in top-right corner. Format uses switchesFormat: {0} = used, {1} = total.")]
    public TMP_Text switchesText;

    [Tooltip("UI format string. {0} = used, {1} = total.")]
    public string switchesFormat = "Teleport: {0}/{1}";

    [Header("Charges")]
    public int maxSwitches = 2;

    [Header("Targeting UX")]
    public float pairCacheTTL = 0.50f; // brief grace while choosing

    // ===== internals =====
    bool _targeting = false;
    float _lastUsedAt = -999f;
    readonly List<GameObject> _markers = new();
    Vector3[] _spots = new Vector3[0];
    int _sel = 0;
    int _switchesUsed = 0;

    Transform[] _coinTransforms;

    Vector3[] _pairCache = null;
    float _pairCacheValidUntil = -999f;
    

    void Awake()
    {
        SetRing(false, false);
    }

    void OnEnable()
    {
        SetRing(false, false);
        _pairCache = null;
        _pairCacheValidUntil = -999f;
        IsTargetingGlobal = false;
    }

    void Start()
    {
        var coins = FindObjectsOfType<Coin>();
        _coinTransforms = new Transform[coins.Length];
        for (int i = 0; i < coins.Length; i++) _coinTransforms[i] = coins[i].transform;

        // Initialize UI to 0 used / max
        UpdateSwitchesUI();
    }

    void Reset()
    {
        if (!player) player = FindObjectOfType<PlayerController>()?.transform;
    }

    // === FREE-TRIGGER MODE: Space always enters targeting and spawns EXACTLY TWO bounded spots ===
    void Update()
    {
        if (!player || GameManager.I == null)
        {
            SetRing(false, false);
            return;
        }
        if (!GameManager.I.IsPlaying)
        {
            SetRing(false, false);
            return;
        }

        if (_targeting)
        {
            UpdateTargetingInput();
            return;
        }

        SetRing(false, false);

        if (Input.GetKeyDown(activateKey))
        {
            if (_switchesUsed >= maxSwitches)
            {
                GameManager.I.ui?.ShowIdleToast(noChargesMsg, 0.9f);
                return;
            }

            // Build two spots NOW (guaranteed and bounded).
            var two = BuildExactlyTwoSpotsGuaranteed();
            _pairCache = two;
            _pairCacheValidUntil = Time.unscaledTime + pairCacheTTL;

            BeginTargetingWithCachedPair();
        }
    }

    // === ALWAYS return exactly two valid, inside-maze spots ===
    Vector3[] BuildExactlyTwoSpotsGuaranteed()
    {
        const int MAX_RETRIES = 3;
        for (int attempt = 0; attempt < MAX_RETRIES; attempt++)
        {
            var pair = TryBuildPairGuaranteed(); // may return 0/1/2
            var two = EnsureTwoSpots(pair);
            if (two.Length == 2) return two;
        }
        // Absolute final fallback if rare failure persists
        return EnsureTwoSpots(new Vector3[0]);
    }

    // Post-process to ensure exactly two spots, clamped inside bounds
    Vector3[] EnsureTwoSpots(Vector3[] input)
    {
        List<Vector3> valid = new();

        // 1) Keep only truly open points and clamp inside (so nothing slips outside).
        if (input != null)
        {
            for (int i = 0; i < input.Length; i++)
            {
                Vector3 p = ClampInsideBounds(input[i], boundsPadding);
                if (IsSpotOpen(p)) valid.Add(p);
            }
        }

        // 2) Deduplicate near-identical points
        float dupEps = 0.15f;
        for (int i = valid.Count - 1; i >= 0; i--)
        {
            for (int j = 0; j < i; j++)
            {
                if (Vector2.Distance(valid[i], valid[j]) <= dupEps)
                {
                    valid.RemoveAt(i);
                    break;
                }
            }
        }

        // 3) If 2+ remain: choose the farthest-apart two
        if (valid.Count >= 2)
        {
            float best = -1f;
            Vector3 A = valid[0], B = valid[1];
            for (int i = 0; i < valid.Count; i++)
            {
                for (int j = i + 1; j < valid.Count; j++)
                {
                    float d = Vector2.Distance(valid[i], valid[j]);
                    if (d > best)
                    {
                        best = d; A = valid[i]; B = valid[j];
                    }
                }
            }
            A = ClampInsideBounds(A, boundsPadding);
            B = ClampInsideBounds(B, boundsPadding);
            return new[] { A, B };
        }

        // 4) If exactly 1 remains: synthesize a second around it
        if (valid.Count == 1)
        {
            Vector3 a = ClampInsideBounds(valid[0], boundsPadding);
            Vector3 b = SynthesizeSecondFrom(a);
            b = ClampInsideBounds(b, boundsPadding);
            if (!IsSpotOpen(b)) b = FindNearestOpenInside(b);
            if (!IsSpotOpen(b)) b = FindNearestOpenInside(a);
            a = ClampInsideBounds(a, boundsPadding);
            b = ClampInsideBounds(b, boundsPadding);
            return new[] { a, b };
        }

        // 5) If none remain: build a deterministic pair near the player
        var forced = ForceOpenPairNear(player.position, Mathf.Max(1.0f, spotCheckRadius * 2f), 0.5f, 12f);
        if (forced.Length != 2)
        {
            // Last resort: left/right nudged inside
            Vector3 left  = ClampInsideBounds(player.position + Vector3.left  * Mathf.Max(1.2f, spotCheckRadius * 3f), boundsPadding);
            Vector3 right = ClampInsideBounds(player.position + Vector3.right * Mathf.Max(1.2f, spotCheckRadius * 3f), boundsPadding);
            left  = IsSpotOpen(left)  ? left  : FindNearestOpenInside(left);
            right = IsSpotOpen(right) ? right : FindNearestOpenInside(right);
            left  = ClampInsideBounds(left, boundsPadding);
            right = ClampInsideBounds(right, boundsPadding);
            return new[] { left, right };
        }
        forced[0] = ClampInsideBounds(forced[0], boundsPadding);
        forced[1] = ClampInsideBounds(forced[1], boundsPadding);
        return forced;
    }

    // --- Pair construction with progressive relaxation + bounded fallbacks ---
    Vector3[] TryBuildPairGuaranteed()
    {
        Vector3 origin = player.position;

        // 1) Original constraints
        var p1 = FindPairWithParams(sampleRadii.ToList(), sampleDirections, jitterPerDirection,
                                    radialJitter, angularJitterDeg,
                                    minEnemyDistance, minCoinDistance, requireLineOfSight,
                                    expandAttempts, expandRadiusStep, expandDirectionsStep, relaxEnemyDistanceStep);
        if (p1.Length == 2) return p1;

        // 2) Relax coins and LoS
        var p2 = FindPairWithParams(sampleRadii.ToList(), sampleDirections + 8, jitterPerDirection + 1,
                                    radialJitter * 1.15f, angularJitterDeg * 1.2f,
                                    Mathf.Max(0.8f, minEnemyDistance * 0.8f), 0f, false,
                                    expandAttempts + 2, expandRadiusStep * 1.1f, expandDirectionsStep + 4, relaxEnemyDistanceStep * 1.2f);
        if (p2.Length == 2) return p2;

        // 3) Wider search
        var wide = new List<float>(sampleRadii);
        float last = wide.Count > 0 ? wide[wide.Count - 1] : 4f;
        wide.Add(last + 2f);
        wide.Add(last + 4f);
        wide.Add(last + 6f);
        var p3 = FindPairWithParams(wide, 48, jitterPerDirection + 2,
                                    radialJitter * 1.3f, angularJitterDeg * 1.3f,
                                    0.7f, 0f, false,
                                    expandAttempts + 4, expandRadiusStep * 1.2f, expandDirectionsStep + 8, relaxEnemyDistanceStep * 1.3f);
        if (p3.Length == 2) return p3;

        // 4) Brute-force (bounded)
        var p4 = BruteForcePair(origin, 600, Mathf.Max(2.0f, minSpotSeparation * 0.6f));
        if (p4.Length == 2) return p4;

        // 5) Deterministic bounded ring-walk
        return ForceOpenPairNear(origin, Mathf.Max(1.0f, spotCheckRadius * 2f), 0.5f, 12f);
    }

    Vector3[] FindPairWithParams(
        List<float> radii, int directions, int jitters,
        float radialJit, float angularJit,
        float enemyGap, float coinGap, bool requireLoSNow,
        int expand, float expandRadStep, int expandDirStep, float relaxEnemyStep)
    {
        for (int attempt = 0; attempt <= expand; attempt++)
        {
            var pool = BuildScoredPool(radii, directions, jitters, radialJit, angularJit, enemyGap, coinGap, requireLoSNow);
            if (pool.Count >= 2)
            {
                var pair = PickBestSeparatedPair(pool, Mathf.Max(1.0f, minSpotSeparation * (requireLoSNow ? 1f : 0.8f)));
                if (pair.Length == 2) return pair;
            }

            float last = radii.Count > 0 ? radii[radii.Count - 1] : 4f;
            radii.Add(last + expandRadStep);
            directions += expandDirStep;
            enemyGap = Mathf.Max(0.45f, enemyGap - relaxEnemyStep);
        }
        return new Vector3[0];
    }

    Vector3[] BruteForcePair(Vector3 origin, int tries, float minSep)
    {
        List<Vector3> good = new();
        for (int i = 0; i < tries; i++)
        {
            float r = 1.0f + i * 0.02f + Random.Range(0f, 0.75f);
            float ang = Random.Range(0f, Mathf.PI * 2f);
            Vector3 cand = origin + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * r;

            cand = ClampInsideBounds(cand, boundsPadding);
            if (IsSpotOpen(cand) && IsOK_EnemyCoinLoS(cand, 0.5f, 0f, false)) good.Add(cand);
            if (good.Count >= 100) break;
        }
        if (good.Count < 2) return new Vector3[0];

        // farthest-apart two
        Vector3 a = good[0], b = good[1];
        float best = -1f;
        for (int i = 0; i < good.Count; i++)
        {
            for (int j = i + 1; j < good.Count; j++)
            {
                float d = Vector2.Distance(good[i], good[j]);
                if (d >= minSep && d > best) { best = d; a = good[i]; b = good[j]; }
            }
        }
        if (best <= 0f) return new Vector3[0];
        return new[] { a, b };
    }

    Vector3[] ForceOpenPairNear(Vector3 origin, float startRadius, float stepRadius, float maxRadius)
    {
        for (float r = startRadius; r <= maxRadius; r += stepRadius)
        {
            const int K = 64;
            for (int i = 0; i < K; i++)
            {
                float ang = (Mathf.PI * 2f) * (i / (float)K);
                Vector3 a = origin + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
                a = ClampInsideBounds(a, boundsPadding);
                if (!IsSpotOpen(a)) continue;

                float ang2 = ang + Mathf.PI;
                Vector3 b = origin + new Vector3(Mathf.Cos(ang2), Mathf.Sin(ang2)) * r;
                b = ClampInsideBounds(b, boundsPadding);
                if (!IsSpotOpen(b))
                {
                    bool found = false;
                    for (int j = -6; j <= 6 && !found; j++)
                    {
                        float aJ = ang2 + (j * Mathf.Deg2Rad * 5f);
                        Vector3 b2 = origin + new Vector3(Mathf.Cos(aJ), Mathf.Sin(aJ)) * r;
                        b2 = ClampInsideBounds(b2, boundsPadding);
                        if (IsSpotOpen(b2)) { b = b2; found = true; }
                    }
                    if (!found) continue;
                }

                if (!IsOK_EnemyCoinLoS(a, 0.4f, 0f, false)) continue;
                if (!IsOK_EnemyCoinLoS(b, 0.4f, 0f, false)) continue;
                if (Vector2.Distance(a, b) < Mathf.Max(1.5f, minSpotSeparation * 0.5f)) continue;

                return new[] { a, b };
            }
        }

        // Last resort: left/right nudged inside
        Vector3 left  = ClampInsideBounds(player.position + Vector3.left  * Mathf.Max(1.2f, spotCheckRadius * 3f), boundsPadding);
        Vector3 right = ClampInsideBounds(player.position + Vector3.right * Mathf.Max(1.2f, spotCheckRadius * 3f), boundsPadding);
        left  = IsSpotOpen(left)  ? left  : FindNearestOpenInside(left);
        right = IsSpotOpen(right) ? right : FindNearestOpenInside(right);
        left  = ClampInsideBounds(left, boundsPadding);
        right = ClampInsideBounds(right, boundsPadding);
        return new[] { left, right };
    }

    // Make a second point near 'a' with decent separation, staying valid & inside bounds.
    Vector3 SynthesizeSecondFrom(Vector3 a)
    {
        float minSep = Mathf.Max(1.5f, minSpotSeparation * 0.5f);
        float r0 = Mathf.Max(1.1f, minSep * 0.6f);
        for (float r = r0; r <= r0 + 4.0f; r += 0.25f)
        {
            for (int i = 0; i < 24; i++)
            {
                float ang = (Mathf.PI * 2f) * (i / 24f);
                Vector3 b = a + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
                b = ClampInsideBounds(b, boundsPadding);
                if (Vector2.Distance(a, b) < minSep) continue;
                if (!IsSpotOpen(b)) continue;
                if (!IsOK_EnemyCoinLoS(b, 0.4f, 0f, false)) continue;
                return b;
            }
        }
        // fallback: opposite direction nudged/clamped inside
        Vector3 oppDir = player ? (a - player.position).normalized : Vector3.right;
        Vector3 opp = a + oppDir * (minSep + 0.5f);
        return ClampInsideBounds(opp, boundsPadding);
    }

    // ---- Targeting flow ----
    void BeginTargetingWithCachedPair()
    {
        if (_pairCache == null || _pairCache.Length != 2)
        {
            GameManager.I.ui?.ShowIdleToast(noSpotMsg, 0.8f);
            return;
        }

        _spots = OrderSpotsForInput(_pairCache);

        ClearMarkers();
        for (int i = 0; i < _spots.Length; i++)
        {
            var go = Instantiate(markerPrefab, _spots[i], Quaternion.identity);
            _markers.Add(go);
            AttachMarkerLabel(go, (i + 1).ToString()); // NUMBER the markers (1/2)
        }

        _sel = 0;
        _targeting = true;
        IsTargetingGlobal = true;

        SetRing(true, false); // glow only while targeting
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
        // Cancel on movement or Escape/Space
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

        // Re-validate and clamp (belt and suspenders)
        p = ClampInsideBounds(p, boundsPadding);
        if (!IsSpotOpen(p))
        {
            GameManager.I.ui?.ShowIdleToast(noSpotMsg, 0.8f);
            return;
        }

        var pos = player.position; pos.x = p.x; pos.y = p.y; player.position = pos;

        _lastUsedAt = Time.unscaledTime;

        // ⭐ DO NOT CONSUME TELEPORT DURING TUTORIAL ⭐
        if (!GameManager.I.ignoreTeleportUse)
        {
            _switchesUsed = Mathf.Min(_switchesUsed + 1, maxSwitches);
            UpdateSwitchesUI();
        }
        // Removed: GameManager.I.ui?.ShowIdleToast(_switchesUsed + "/" + maxSwitches + " position switches used", 0.9f);
        // We now only rely on the top-right indicator.

        // BETA METRIC4 CHANGES === Analytics: log successful Position Switch ===
        {
            // If you ever want to *explicitly* skip Dark Maze, uncomment the guard below:
            // if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Level2_DarkMaze") { /* do nothing */ }
            string levelName = (LevelManager.I != null)
                ? $"Level{LevelManager.I.currentLevel}"
                : UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            float t = (LevelTimer.IsRunning ? LevelTimer.Elapsed : Time.timeSinceLevelLoad);
            AnalyticsLogger.I?.LogPowerUpUse(levelName, "PositionSwitch", t);
        }
        // ================================================

        ExitTargeting(true);

        _pairCache = null;
        _pairCacheValidUntil = -999f;

        if (_switchesUsed >= maxSwitches)
            SetRing(false, false);
    }

    void ExitTargeting(bool used)
    {
        ClearMarkers();
        _targeting = false;
        IsTargetingGlobal = false;
        SetRing(false, false);
    }

    void SetRing(bool show, bool urgent)
    {
        if (ringCalm) ringCalm.SetActive(show && !urgent);
        if (ringUrgent) ringUrgent.SetActive(show && urgent);
    }

    void UpdateSwitchesUI()
    {
        if (!switchesText) return;

        int used = _switchesUsed;
        int total = maxSwitches;

        switchesText.text = string.Format(switchesFormat, used, total);
    }

    // Reset position switch chances to maximum (for try again functionality)
    public void ResetPositionSwitchChances()
    {
        _switchesUsed = 0;
        UpdateSwitchesUI();
    }

    // ===== LABELS =====
    // Create/update a centered TextMeshPro label on a marker (shows "1" or "2")
    void AttachMarkerLabel(GameObject marker, string text)
    {
        // topmost sprite renderer (highest order) on this marker hierarchy
        SpriteRenderer[] srs = marker.GetComponentsInChildren<SpriteRenderer>(true);
        SpriteRenderer topSr = null;
        foreach (var sr in srs) if (!topSr || sr.sortingOrder >= topSr.sortingOrder) topSr = sr;

        TextMeshPro tmp = marker.GetComponentInChildren<TextMeshPro>(true);
        if (tmp == null)
        {
            var child = new GameObject("Label");
            child.layer = marker.layer;
            child.transform.SetParent(marker.transform);
            child.transform.localPosition = new Vector3(0f, 0f, -0.02f); // toward camera to avoid z-fighting
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;

            tmp = child.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.fontStyle = FontStyles.Bold;
        }

        // Solid black with white outline
        tmp.color = Color.black;
        tmp.outlineColor = Color.white;
        tmp.outlineWidth = 0.4f;
        var lc = tmp.color; lc.a = 1f; tmp.color = lc;

        tmp.text = text;

        // size based on marker sprite height
        float spriteH = 1.0f;
        if (topSr && topSr.sprite) spriteH = Mathf.Max(0.25f, topSr.sprite.bounds.size.y);
        tmp.fontSize = Mathf.Clamp(spriteH * 8f, 3f, 18f);

        // render above the marker
        var mr = tmp.renderer;
        if (topSr && mr != null)
        {
            mr.sortingLayerID = topSr.sortingLayerID;
            mr.sortingLayerName = topSr.sortingLayerName;
            mr.sortingOrder = topSr.sortingOrder + 50; // big gap to be safe
        }
    }

    // ===== Scoring & validation helpers (bounded) =====

    struct ScoredPoint { public Vector3 pos; public float score; }

    List<ScoredPoint> BuildScoredPool(
        List<float> radii, int directions, int jitters,
        float radialJit, float angularJit, float enemyGap, float coinGap, bool requireLoSNow)
    {
        List<Vector3> candidates = new();
        Vector3 origin = player.position;

        for (int r = 0; r < radii.Count; r++)
        {
            for (int i = 0; i < directions; i++)
            {
                float baseAng = (Mathf.PI * 2f) * (i / (float)directions);
                for (int j = 0; j < jitters; j++)
                {
                    float ang = baseAng + Mathf.Deg2Rad * Random.Range(-angularJit, angularJit);
                    float rad = radii[r] + Random.Range(-radialJit, radialJit);
                    Vector3 cand = origin + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * rad;

                    cand = ClampInsideBounds(cand, boundsPadding);
                    if (IsSpotOpen(cand) && IsOK_EnemyCoinLoS(cand, enemyGap, coinGap, requireLoSNow))
                        candidates.Add(cand);
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

        List<ScoredPoint> pool = new(candidates.Count);
        for (int i = 0; i < candidates.Count; i++)
        {
            float distEnemy = DistToNearestEnemy(candidates[i]);
            float losBonus = (requireLoSNow && HasLineOfSight(origin, candidates[i])) ? 0.6f : 0f;
            float coinBonus = Mathf.Clamp01(DistToNearestCoin(candidates[i], 8f)) * 0.4f;
            float densPenalty = enemyDensityWeight * (maxD > 0 ? (density[i] / (float)maxD) : 0f);
            float s = distEnemy + losBonus + coinBonus - densPenalty;
            pool.Add(new ScoredPoint { pos = candidates[i], score = s });
        }

        pool = pool.OrderByDescending(p => p.score).Take(Mathf.Min(pairPoolSize, pool.Count)).ToList();
        return pool;
    }

    Vector3[] PickBestSeparatedPair(List<ScoredPoint> pool, float minSepOverride)
    {
        float best = float.NegativeInfinity;
        Vector3 aBest = Vector3.zero, bBest = Vector3.zero;

        for (int i = 0; i < pool.Count; i++)
        {
            for (int j = i + 1; j < pool.Count; j++)
            {
                float sep = Vector2.Distance(pool[i].pos, pool[j].pos);
                if (sep < minSepOverride) continue;
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

    bool IsSpotOpen(Vector3 p)
    {
        // 1) Must be inside the level bounds
        if (!IsInsideLevel(p)) return false;

        // 2) Must not overlap walls
        if (Physics2D.OverlapCircle(p, spotCheckRadius, obstacleMask)) return false;

        return true;
    }

    bool IsOK_EnemyCoinLoS(Vector3 p, float enemyGap, float coinGap, bool requireLoSNow)
    {
        if (enemyGap > 0f && DistToNearestEnemy(p) < enemyGap) return false;
        if (coinGap > 0f && DistToNearestCoin(p, 10f) < coinGap) return false;
        if (requireLoSNow && !HasLineOfSight(player.position, p)) return false;
        return true;
    }

    // Robust inside test for ANY 2D collider (rotated or not):
    // if inside => levelBounds.ClosestPoint(p) == p (within epsilon).
    bool IsInsideLevel(Vector3 worldPoint)
    {
        if (!levelBounds) return true;
        Vector2 p = worldPoint;
        Vector2 cp = levelBounds.ClosestPoint(p);
        return (Vector2.SqrMagnitude(cp - p) <= 1e-6f);
    }

    // Clamp an arbitrary point to lie just inside the bounds by at least "padding".
    Vector3 ClampInsideBounds(Vector3 p, float padding)
    {
        if (!levelBounds) return p;

        Vector2 point = p;

        // Already inside?
        if (IsInsideLevel(point))
        {
            // If overlapping walls due to thickness, nudge inward
            if (Physics2D.OverlapCircle(point, spotCheckRadius * 0.95f, obstacleMask))
            {
                Vector2 cpEdge = levelBounds.ClosestPoint(point + Vector2.one * 0.001f); // tiny bias
                Vector2 dirIn = (((Vector2)levelBounds.bounds.center) - cpEdge).normalized;
                if (dirIn.sqrMagnitude < 1e-6f) dirIn = Vector2.up;
                return (Vector2)point + dirIn * Mathf.Max(padding, spotCheckRadius);
            }
            return point;
        }

        // Outside → move to boundary and then nudge inward
        Vector2 cp = levelBounds.ClosestPoint(point);
        Vector2 dir = (((Vector2)levelBounds.bounds.center) - cp).normalized;
        if (dir.sqrMagnitude < 1e-6f) dir = Vector2.up;
        return cp + dir * Mathf.Max(padding, spotCheckRadius * 0.75f);
    }

    Vector3 FindNearestOpenInside(Vector3 from)
    {
        // Spiral search constrained by bounds, clamping each probe inside first
        for (float r = 0f; r <= 3.0f; r += 0.2f)
        {
            for (int i = 0; i < 28; i++)
            {
                float ang = (Mathf.PI * 2f) * (i / 28f);
                Vector3 p = from + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
                p = ClampInsideBounds(p, boundsPadding);
                if (IsSpotOpen(p)) return p;
            }
        }
        // Fallback to player's current position clamped inside
        return ClampInsideBounds(player ? player.position : from, boundsPadding);
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
        var cam = Camera.main; if (!cam) return -1;
        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition); world.z = 0f;
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
        IsTargetingGlobal = false;
    }
}
