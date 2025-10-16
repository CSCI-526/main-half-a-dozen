using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10)]
public class UILevelPanel : MonoBehaviour
{
    public static UILevelPanel I;

    [Header("Texts")]
    public TMP_Text levelTitle;
    public TMP_Text subtitle;
    public TMP_Text continueText;

    [Header("Settings")]
    public float fadeInTime = 0.5f;

    private CanvasGroup cg;
    public static bool IsIntroVisible { get; private set; } = false;

    void Awake()
    {
        I = this;
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        gameObject.SetActive(false);
    }

    // 🔸 Show Intro Panel
public static void ShowIntro(int level)
{
    if (I == null) return;
    if (!I.gameObject.activeInHierarchy)
        I.gameObject.SetActive(true);  // 🟢 ensures it's active
    I.StartCoroutine(I.ShowIntroRoutine(level));
}

    // 🔸 Show Level Complete Panel
    public static void ShowComplete(int level)
    {
        if (I != null)
            I.StartCoroutine(I.ShowCompleteRoutine(level));
    }

    // ---------------------------------------------------------
    // Intro Screen (LEVEL 1 / LEVEL 2 intro)
    // ---------------------------------------------------------
        private System.Collections.IEnumerator ShowIntroRoutine(int level)
    {
        Debug.Log($"[UILevelPanel] Showing Level Intro for Level {level}");
        IsIntroVisible = true;
        gameObject.SetActive(true);

        // Hide "How Not To Lose" panel if it's showing
        if (GameManager.I != null && GameManager.I.ui != null)
            GameManager.I.ui.HideHowTo();

        // set text content
        levelTitle.text = $"LEVEL {level}";
        subtitle.text = level == 1
            ? "Collect all coins and reach the exit"
            : "Explore the maze, light all beacons and find the key";
        continueText.text = "Press SPACE to start";

        // fade in
        yield return Fade(1f);

        // wait for input
        while (!Input.GetKeyDown(KeyCode.Space))
            yield return null;

        // fade out intro panel
        yield return Fade(0f);
        gameObject.SetActive(false);
        IsIntroVisible = false;

        // show "How Not To Lose" after level intro ends
        if (GameManager.I != null && GameManager.I.ui != null)
        {
            GameManager.I.ui.ShowHowTo(true);
        }
    }

    // ---------------------------------------------------------
    // Level Complete Screen
    // ---------------------------------------------------------
    private System.Collections.IEnumerator ShowCompleteRoutine(int level)
    {
        gameObject.SetActive(true);
        levelTitle.text = $"LEVEL {level} COMPLETE!";
        subtitle.text = "Well done!";
        continueText.text = "Press SPACE to continue";

        yield return Fade(1f);

        while (!Input.GetKeyDown(KeyCode.Space))
            yield return null;

        yield return Fade(0f);
        gameObject.SetActive(false);

        // move to next level (call LevelManager’s transition)
        if (LevelManager.I != null)
            LevelManager.I.StartCoroutine(LevelManager.I.LoadNextLevelAfterDelay(0f));
    }

    // ---------------------------------------------------------
    // Smooth Fade Animation
    // ---------------------------------------------------------
    private System.Collections.IEnumerator Fade(float target)
    {
        float t = 0f;
        float start = cg.alpha;
        while (t < fadeInTime)
        {
            cg.alpha = Mathf.Lerp(start, target, t / fadeInTime);
            t += Time.deltaTime;
            yield return null;
        }
        cg.alpha = target;
    }
}