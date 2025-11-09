using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using static IdleFailTracker;


public class SendToGoogleIdle : MonoBehaviour
{
    [SerializeField] bool verboseLogging = true;
    public static SendToGoogleIdle Instance { get; private set; }

    [Header("Google Form")]
    [Tooltip("The form action URL ending in /formResponse")]
    // ✅ Correct live form action endpoint
    private string formActionUrl = "https://docs.google.com/forms/d/e/1FAIpQLSc5vgLSUuvk8NXrL5mlGWVWBoNUFzqfkVnJrffffv2K0BAmpw/formResponse";

    [Header("Entry IDs (auto-mapped from your Google Form)")]
    private string e_ts                        = "entry.2034717883";
    private string e_sessionId                 = "entry.2105670350";
    private string e_sceneName                 = "entry.1536079719";
    private string e_levelIndex                = "entry.1593729329";
    private string e_attemptIndex              = "entry.1760574110";
    private string e_eventType                 = "entry.1181994755";
    private string e_idleThresholdSec          = "entry.974616481";
    private string e_idleDeathCountThisLevel   = "entry.703989574";
    private string e_timeSinceLevelStartSec    = "entry.541874513";
    private string e_submitOnNth               = "entry.1952332081";
    private string e_secondStrike              = "entry.1141201131";

    // Optional extras (not in this form but kept for compatibility)
    private string e_buildVersion = "entry.placeholder_buildVersion";
    private string e_coinsCollected = "entry.placeholder_coinsCollected";
    private string e_powerupsUsed = "entry.placeholder_powerupsUsed";
    private string e_client = "entry.placeholder_client";

    private void Awake()
    {
        if (Instance != this && Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Post(IdlePayload p)
    {
        StartCoroutine(PostRoutine(p));
    }

    private IEnumerator PostRoutine(IdlePayload p)
    {
        WWWForm form = new WWWForm();
        form.AddField(e_ts, p.ts);
        form.AddField(e_sessionId, p.sessionId);
        form.AddField(e_sceneName, p.sceneName);
        form.AddField(e_levelIndex, p.levelIndex.ToString());
        form.AddField(e_attemptIndex, p.attemptIndex.ToString());
        form.AddField(e_eventType, p.eventType);
        form.AddField(e_idleThresholdSec, p.idleThresholdSec.ToString("0.###"));
        form.AddField(e_idleDeathCountThisLevel, p.idleDeathCountThisLevel.ToString());
        form.AddField(e_timeSinceLevelStartSec, p.timeSinceLevelStartSec.ToString("0.###"));
        form.AddField(e_submitOnNth, p.submitOnNth.ToString());
        form.AddField(e_secondStrike, p.secondStrike ? "true" : "false");

        // Optional fields (ignore if not present in your form)
        form.AddField(e_buildVersion, p.buildVersion ?? "alpha");
        form.AddField(e_coinsCollected, p.coinsCollected.ToString());
        form.AddField(e_powerupsUsed, p.powerupsUsed.ToString());
        form.AddField(e_client, p.client ?? "WebGL");

        using (UnityWebRequest req = UnityWebRequest.Post(formActionUrl, form))
        {
            req.SetRequestHeader("Referer", "https://docs.google.com"); // harmless hint
            yield return req.SendWebRequest();

            if (verboseLogging)
            {
                Debug.Log($"[IdleMetric] POST result={req.result} code={req.responseCode} err={req.error}");
                if (req.downloadHandler != null && !string.IsNullOrEmpty(req.downloadHandler.text))
                    Debug.Log($"[IdleMetric] Response body: {req.downloadHandler.text}");
            }
        }

    }
}
