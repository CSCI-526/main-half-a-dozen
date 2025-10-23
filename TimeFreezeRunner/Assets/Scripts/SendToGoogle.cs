using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Fire-and-forget poster to a Google Form (/formResponse).
/// Safe: if URL/IDs missing or network fails, it no-ops (gameplay unaffected).
/// </summary>
public class SendToGoogle : MonoBehaviour
{
    [Header("Google Form")]
    [Tooltip("The EXACT action URL ending with /formResponse")]
    [SerializeField] private string formActionUrl = ""; // <- paste your /formResponse URL here

    [Header("Entry IDs (from your screenshot)")]
    [SerializeField] private string ts            = "entry.176765317";
    [SerializeField] private string sessionId     = "entry.326046602";
    [SerializeField] private string level         = "entry.909627180";
    [SerializeField] private string eventType     = "entry.148349950";
    [SerializeField] private string result        = "entry.1217396183";
    [SerializeField] private string attemptIndex  = "entry.223776661";
    [SerializeField] private string secondsElapsed= "entry.1541212500";

    [Header("Settings")]
    [Tooltip("If false, disables sending entirely (keeps logs).")]
    public bool enabledSending = true;
    [Tooltip("Log success/failure to Console.")]
    public bool verbose = true;
    [Tooltip("Tiny throttle between requests.")]
    public float minIntervalSeconds = 0.05f;

    float _lastSend = -999f;

    public void ConfigureFormUrl(string url) => formActionUrl = url;

    public IEnumerator PostRunEndRow(string tsVal, string sessionIdVal, string levelVal,
                                     string resultVal, int attemptIndexVal, float secondsElapsedVal)
    {
        if (!enabledSending || string.IsNullOrEmpty(formActionUrl))
        {
            if (verbose) Debug.Log("[Forms] Disabled or URL not set; skipping send.");
            yield break;
        }

        // simple rate limit
        var wait = (_lastSend + minIntervalSeconds) - Time.unscaledTime;
        if (wait > 0) yield return new WaitForSecondsRealtime(wait);

        var form = new WWWForm();

        void Add(string entryId, string value)
        {
            if (!string.IsNullOrEmpty(entryId)) form.AddField(entryId, value ?? "");
        }

        Add(ts,             tsVal);
        Add(sessionId,      sessionIdVal);
        Add(level,          levelVal);
        Add(eventType,      "RunEnd");
        Add(result,         resultVal);
        Add(attemptIndex,   attemptIndexVal.ToString());
        Add(secondsElapsed, secondsElapsedVal.ToString("0.###"));

        using var www = UnityWebRequest.Post(formActionUrl, form);
        yield return www.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
        bool ok = www.result == UnityWebRequest.Result.Success;
#else
        bool ok = !www.isHttpError && !www.isNetworkError;
#endif
        if (!ok)
        {
            if (verbose) Debug.LogWarning($"[Forms] Error: {www.error}");
        }
        else if (verbose)
        {
            Debug.Log("[Forms] Row uploaded.");
        }
        _lastSend = Time.unscaledTime;
    }
}
