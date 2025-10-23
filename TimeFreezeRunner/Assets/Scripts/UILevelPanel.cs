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

public static void ShowIntro(int level)
{
    Debug.Log($"[UILevelPanel.ShowIntro] Called with level: {level}");
    Debug.Log($"[UILevelPanel.ShowIntro] UILevelPanel.I is null: {I == null}");
    
    if (I == null) 
    {
        Debug.LogWarning("[UILevelPanel.ShowIntro] UILevelPanel.I is null - cannot show intro!");
        return;
    }

    // Prefer the active scene to decide the level (prevents stale LevelManager state)
    string sceneName = SceneManager.GetActiveScene().name;
    int lvl = level;

    // Heuristics: if this is your Level 1 scene, force lvl = 1
    if (!string.IsNullOrEmpty(sceneName) &&
        (sceneName == "Main" || sceneName.Contains("Level1") || sceneName == "MainForLevel1"))
    {
        lvl = 1;
    }
    else if (LevelManager.I != null)
    {
        // Otherwise prefer LevelManager if present
        lvl = Mathf.Max(1, LevelManager.I.currentLevel);
    }

    if (!I.gameObject.activeInHierarchy)
        I.gameObject.SetActive(true);

    I.StopAllCoroutines();                // ensure no stale coroutine
    I.StartCoroutine(I.ShowIntroRoutine(lvl));
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
        Debug.Log($"[UILevelPanel.ShowIntroRoutine] Starting for Level {level}");
        Debug.Log($"[UILevelPanel.ShowIntroRoutine] Scene name: {SceneManager.GetActiveScene().name}");
        IsIntroVisible = true;
        gameObject.SetActive(true);
        Debug.Log($"[UILevelPanel.ShowIntroRoutine] GameObject activated, activeInHierarchy: {gameObject.activeInHierarchy}");

        // Hide "How Not To Lose" panel if it's showing
        if (GameManager.I != null && GameManager.I.ui != null)
            GameManager.I.ui.HideHowTo();
        // set text content
        levelTitle.text = $"<color=#6FA8DC><b>LEVEL</b></color> <color=#6FA8DC><b>{level}</b></color>";

        if (level == 1)
        {
            subtitle.text =
                "Collect <color=yellow>all Coins</color>\n" +
                "Reach <color=red>EXIT</color>\n" +
                "Press <color=yellow>Space</color> to <color=yellow>Teleport</color>\n" +
                "Press keys - <color=orange>1</color> or <color=orange>2</color> to pick teleport spot\n" +
                "<color=yellow>Limited to 2, choose smart.</color>";
        }
        else if (level == 2)
        {
            // Check if we're in the dark maze scene
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "Level2_DarkMaze")
            {
                levelTitle.text = $"<color=red><b>DARK MAZE</b></color>";
                subtitle.text = 
                    
                    "Light up all the <color=yellow>bulbs</color>\n" +
                    "Search for the <color=green>Key</color>";
            }
            else
            {
                subtitle.text =
                    "Collect <color=yellow>all Coins</color>\n" +
                    "Enter the <color=orange>Blue Door</color>\n" +
                    "Telport Available!";
            }
        }
        else if (level == 3)
        {
            subtitle.text = 
                "<color=red>Enemy Wipe Activated!</color> You have 2 uses, spend them wisely.\n" +
                "Press <color=orange>K</color> to <color=orange>Clear</color> enemies for 5s\n" +
                "But beware, they'll <color=yellow>Multiply</color> after!";
        }

        continueText.text = "Press <color=orange>ENTER</color> to start";

        // fade in
        Debug.Log($"[UILevelPanel.ShowIntroRoutine] Starting fade in, CanvasGroup alpha: {cg.alpha}");
        yield return Fade(1f);
        Debug.Log($"[UILevelPanel.ShowIntroRoutine] Fade in complete, CanvasGroup alpha: {cg.alpha}");

        // wait for input
        while (!Input.GetKeyDown(KeyCode.Space) && !Input.GetKeyDown(KeyCode.Return))
            yield return null;

        // fade out intro panel
        yield return Fade(0f);
        gameObject.SetActive(false);
        IsIntroVisible = false;

        // show "How Not To Lose" after level intro ends (except for Main scene Level 1)
        if (GameManager.I != null && GameManager.I.ui != null)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName != "Main" || level != 1)
            {
                GameManager.I.ui.ShowHowTo(true);
            }
            else
            {
                // For Main scene Level 1, start the game directly without showing howToPanel
                GameManager.I.StartGame();
            }
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
        continueText.text = "Press ENTER to continue";

        yield return Fade(1f);

        while (!Input.GetKeyDown(KeyCode.Space) && !Input.GetKeyDown(KeyCode.Return))
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
        Debug.Log($"[UILevelPanel.Fade] Starting fade to target: {target}, current alpha: {cg.alpha}");
        float t = 0f;
        float start = cg.alpha;
        while (t < fadeInTime)
        {
            cg.alpha = Mathf.Lerp(start, target, t / fadeInTime);
            t += Time.deltaTime;
            yield return null;
        }
        cg.alpha = target;
        Debug.Log($"[UILevelPanel.Fade] Fade complete, final alpha: {cg.alpha}");
    }
}