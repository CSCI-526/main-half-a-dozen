using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class SendToGoogle_EnemyContext : MonoBehaviour
{
    [Header("Google Form URL (Enemy Context Metric)")]
    [Tooltip("Paste the EXACT /formResponse URL (ends with /formResponse)")]
    [SerializeField] private string formActionUrl = "";

    [Header("Entry IDs from NEW EnemyContext form")]
    [SerializeField] private string ts        = "";   // ts
    [SerializeField] private string sessionId = "";   // sessionID
    [SerializeField] private string level     = "";   // level
    [SerializeField] private string posX      = "";   // posX
    [SerializeField] private string posY      = "";   // posY
    [SerializeField] private string eventType = "";   // eventType
    [SerializeField] private string sceneName = "";   // sceneName

    [Header("Nearest enemies at death (up to 3)")]
    [SerializeField] private string enemy1X        = ""; // enemy1X
    [SerializeField] private string enemy1Y        = ""; // enemy1Y
    [SerializeField] private string enemy1Distance = ""; // enemy1Distance

    [SerializeField] private string enemy2X        = ""; // enemy2X
    [SerializeField] private string enemy2Y        = ""; // enemy2Y
    [SerializeField] private string enemy2Distance = ""; // enemy2Distance

    [SerializeField] private string enemy3X        = ""; // enemy3X
    [SerializeField] private string enemy3Y        = ""; // enemy3Y
    [SerializeField] private string enemy3Distance = ""; // enemy3Distance

    [Header("Settings")]
    public bool enabledSending = true;
    public bool verbose = true;
    public float minIntervalSeconds = 0.05f;

    float _lastSend = -999f;

    public IEnumerator PostEnemyContextRow(
        string tsVal,
        string sessionIdVal,
        string levelVal,
        Vector2 playerPos,
        string eventTypeVal,
        string sceneNameVal,
        bool hasE1, Vector2 e1Pos, float e1Dist,
        bool hasE2, Vector2 e2Pos, float e2Dist,
        bool hasE3, Vector2 e3Pos, float e3Dist
    )
    {
        if (!enabledSending || string.IsNullOrEmpty(formActionUrl))
        {
            if (verbose) Debug.Log("[EnemyContextForms] Disabled or URL not set; skipping.");
            yield break;
        }

        float wait = (_lastSend + minIntervalSeconds) - Time.unscaledTime;
        if (wait > 0)
            yield return new WaitForSecondsRealtime(wait);

        var form = new WWWForm();
        var debugPayload = new StringBuilder();

        void Add(string entryId, string value)
        {
            if (string.IsNullOrEmpty(entryId)) return;
            string safe = value ?? "";
            form.AddField(entryId, safe);
            debugPayload.AppendLine($"{entryId} = {safe}");
        }

        // ---- core fields (match your form: ts, sessionID, level, posX, posY, eventType, sceneName) ----
        Add(ts,        tsVal);
        Add(sessionId, sessionIdVal);
        Add(level,     levelVal);

        Add(posX,      playerPos.x.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
        Add(posY,      playerPos.y.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));

        Add(eventType, eventTypeVal);
        Add(sceneName, sceneNameVal);

        // ---- Enemy 1 ----
        if (hasE1)
        {
            Add(enemy1X, e1Pos.x.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
            Add(enemy1Y, e1Pos.y.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
            Add(enemy1Distance, Mathf.Abs(e1Dist).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
        }
        else
        {
            Add(enemy1X, "");
            Add(enemy1Y, "");
            Add(enemy1Distance, "");
        }

        // ---- Enemy 2 ----
        if (hasE2)
        {
            Add(enemy2X, e2Pos.x.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
            Add(enemy2Y, e2Pos.y.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
            Add(enemy2Distance, Mathf.Abs(e2Dist).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
        }
        else
        {
            Add(enemy2X, "");
            Add(enemy2Y, "");
            Add(enemy2Distance, "");
        }

        // ---- Enemy 3 ----
        if (hasE3)
        {
            Add(enemy3X, e3Pos.x.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
            Add(enemy3Y, e3Pos.y.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
            Add(enemy3Distance, Mathf.Abs(e3Dist).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
        }
        else
        {
            Add(enemy3X, "");
            Add(enemy3Y, "");
            Add(enemy3Distance, "");
        }

        if (verbose)
            Debug.Log("[EnemyContextForms] Sending row:\n" + debugPayload.ToString());

        using var www = UnityWebRequest.Post(formActionUrl, form);
        yield return www.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
        bool ok = www.result == UnityWebRequest.Result.Success;
#else
        bool ok = !www.isHttpError && !www.isNetworkError;
#endif

        if (!ok)
            Debug.LogWarning($"[EnemyContextForms] Error: {www.error}");
        else if (verbose)
            Debug.Log("[EnemyContextForms] Enemy-context row uploaded.");

        _lastSend = Time.unscaledTime;
    }
}
