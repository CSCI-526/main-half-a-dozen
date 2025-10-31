using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class SendToGoogle_Death : MonoBehaviour
{
    [Header("Google Form (Metric 2)")]
    [Tooltip("Paste the EXACT /formResponse URL for the Metric2 form")]
    [SerializeField] private string formActionUrl = "";

    [Header("Entry IDs from Metric2 form")]
    [SerializeField] private string ts        = ""; // entry.xxxxx
    [SerializeField] private string sessionId = "";
    [SerializeField] private string level     = "";
    [SerializeField] private string posX      = "";
    [SerializeField] private string posY      = "";
    [SerializeField] private string eventType = ""; 

    [Header("Settings")]
    public bool enabledSending = true;
    public bool verbose = true;
    public float minIntervalSeconds = 0.05f;

    float _lastSend = -999f;

    public IEnumerator PostDeathRow(string tsVal, string sessionIdVal, string levelVal, Vector2 posWorld)
    {
        if (!enabledSending || string.IsNullOrEmpty(formActionUrl))
        {
            if (verbose) Debug.Log("[DeathForms] Disabled or URL not set; skipping.");
            yield break;
        }

   
        var wait = (_lastSend + minIntervalSeconds) - Time.unscaledTime;
        if (wait > 0) yield return new WaitForSecondsRealtime(wait);

        var form = new WWWForm();
        void Add(string entryId, string value)
        {
            if (!string.IsNullOrEmpty(entryId)) form.AddField(entryId, value ?? "");
        }

        Add(ts,        tsVal);
        Add(sessionId, sessionIdVal);
        Add(level,     levelVal);
        Add(posX,      posWorld.x.ToString("0.###"));
        Add(posY,      posWorld.y.ToString("0.###"));
        Add(eventType, "Death");

        using var www = UnityWebRequest.Post(formActionUrl, form);
        yield return www.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
        bool ok = www.result == UnityWebRequest.Result.Success;
#else
        bool ok = !www.isHttpError && !www.isNetworkError;
#endif
        if (!ok)
        {
            if (verbose) Debug.LogWarning($"[DeathForms] Error: {www.error}");
        }
        else if (verbose)
        {
            Debug.Log("[DeathForms] Death row uploaded.");
        }

        _lastSend = Time.unscaledTime;
    }
}
