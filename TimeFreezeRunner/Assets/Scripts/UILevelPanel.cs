// using UnityEngine;
// using TMPro;
// using UnityEngine.SceneManagement;

// [DefaultExecutionOrder(-10)]
// public class UILevelPanel : MonoBehaviour
// {
//     public static UILevelPanel I;

//     [Header("Texts")]
//     public TMP_Text levelTitle;
//     public TMP_Text subtitle;
//     public TMP_Text continueText;

//     [Header("Settings")]
//     public float fadeInTime = 0.5f;

//     private CanvasGroup cg;
//     public static bool IsIntroVisible { get; private set; } = false;

//     void Awake()
//     {
//         I = this;
//         cg = GetComponent<CanvasGroup>();
//         if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
//         cg.alpha = 0f;
//         gameObject.SetActive(false);
//     }

// public static void ShowIntro(int level)
// {
//     Debug.Log($"[UILevelPanel.ShowIntro] Called with level: {level}");
//     Debug.Log($"[UILevelPanel.ShowIntro] UILevelPanel.I is null: {I == null}");
    
//     if (I == null) 
//     {
//         Debug.LogWarning("[UILevelPanel.ShowIntro] UILevelPanel.I is null - cannot show intro!");
//         return;
//     }

//     // Prefer the active scene to decide the level (prevents stale LevelManager state)
//     string sceneName = SceneManager.GetActiveScene().name;
//     int lvl = level;

//     // Heuristics: if this is your Level 1 scene, force lvl = 1
//     if (!string.IsNullOrEmpty(sceneName) &&
//         (sceneName == "Main" || sceneName.Contains("Level1") || sceneName == "MainForLevel1"))
//     {
//         lvl = 1;
//     }
//     else if (LevelManager.I != null)
//     {
//         // Otherwise prefer LevelManager if present
//         lvl = Mathf.Max(1, LevelManager.I.currentLevel);
//     }

//     if (!I.gameObject.activeInHierarchy)
//         I.gameObject.SetActive(true);

//     I.StopAllCoroutines();                // ensure no stale coroutine
//     I.StartCoroutine(I.ShowIntroRoutine(lvl));
// }


//     // 🔸 Show Level Complete Panel
//     public static void ShowComplete(int level)
//     {
//         if (I != null)
//             I.StartCoroutine(I.ShowCompleteRoutine(level));
//     }

//     // ---------------------------------------------------------
//     // Intro Screen (LEVEL 1 / LEVEL 2 intro)
//     // ---------------------------------------------------------
//         private System.Collections.IEnumerator ShowIntroRoutine(int level)
//     {
//         Debug.Log($"[UILevelPanel.ShowIntroRoutine] Starting for Level {level}");
//         Debug.Log($"[UILevelPanel.ShowIntroRoutine] Scene name: {SceneManager.GetActiveScene().name}");
//         IsIntroVisible = true;
//         gameObject.SetActive(true);
//         Debug.Log($"[UILevelPanel.ShowIntroRoutine] GameObject activated, activeInHierarchy: {gameObject.activeInHierarchy}");

//         // Hide "How Not To Lose" panel if it's showing
//         if (GameManager.I != null && GameManager.I.ui != null)
//             GameManager.I.ui.HideHowTo();
//         // set text content
//         levelTitle.text = $"<color=#6FA8DC><b>LEVEL</b></color> <color=#6FA8DC><b>{level}</b></color>";

//         if (level == 1)
//         {
//             subtitle.text =
//                 "Collect <color=yellow>all Coins</color>\n" +
//                 "Reach <color=red>EXIT</color>\n" +
//                 "Press <color=yellow>Space</color> to <color=yellow>Teleport</color>\n" +
//                 "Press keys - <color=orange>1</color> or <color=orange>2</color> to pick teleport spot\n" +
//                 "<color=yellow>Limited to 2, choose smart.</color>";
//         }
//         else if (level == 2)
//         {
//             // Check if we're in the dark maze scene
//             string sceneName = SceneManager.GetActiveScene().name;
//             if (sceneName == "Level2_DarkMaze")
//             {
//                 levelTitle.text = $"<color=red><b>DARK MAZE</b></color>";
//                 subtitle.text = 
                    
//                     "Light up all the <color=yellow>bulbs</color>\n" +
//                     "Search for the <color=green>Key</color>";
//             }
//             else
//             {
//                 subtitle.text =
//                     "Gather every <color=yellow>coin</color> and,\nMake your way to the <color=orange>Blue Door</color>\n" +
//                     "\nTeleport works in this maze too!";
//             }
//         }
//         else if (level == 3)
//         {
//             subtitle.text = 
//                 "<color=red>Enemy Wipe Activated!</color> You have 2 uses, spend them wisely.\n" +
//                 "Hit <color=orange>Shift</color> to <color=orange>Clear</color> enemies for 5s\n" +
//                 "But beware, they'll <color=yellow>Multiply</color> after!";
//         }

//         continueText.text = "Press <color=orange>ENTER</color> to start";

//         // fade in
//         Debug.Log($"[UILevelPanel.ShowIntroRoutine] Starting fade in, CanvasGroup alpha: {cg.alpha}");
//         yield return Fade(1f);
//         Debug.Log($"[UILevelPanel.ShowIntroRoutine] Fade in complete, CanvasGroup alpha: {cg.alpha}");

//         // wait for input
//         while (!Input.GetKeyDown(KeyCode.Space) && !Input.GetKeyDown(KeyCode.Return))
//             yield return null;

//         // fade out intro panel
//         yield return Fade(0f);
//         gameObject.SetActive(false);
//         IsIntroVisible = false;

//         // show "How Not To Lose" after level intro ends (except for Main scene Level 1)
//         if (GameManager.I != null && GameManager.I.ui != null)
//         {
//             string sceneName = SceneManager.GetActiveScene().name;
//             if (sceneName != "Main" || level != 1)
//             {
//                 GameManager.I.ui.ShowHowTo(true);
//             }
//             else
//             {
//                 // For Main scene Level 1, start the game directly without showing howToPanel
//                 GameManager.I.StartGame();
//             }
//         }
//     }

//     // ---------------------------------------------------------
//     // Level Complete Screen
//     // ---------------------------------------------------------
//     private System.Collections.IEnumerator ShowCompleteRoutine(int level)
//     {
//         gameObject.SetActive(true);
//         levelTitle.text = $"LEVEL {level} COMPLETE!";
//         subtitle.text = "Well done!";
//         continueText.text = "Press ENTER to continue";

//         yield return Fade(1f);

//         while (!Input.GetKeyDown(KeyCode.Space) && !Input.GetKeyDown(KeyCode.Return))
//             yield return null;

//         yield return Fade(0f);
//         gameObject.SetActive(false);

//         // move to next level (call LevelManager’s transition)
//         if (LevelManager.I != null)
//             LevelManager.I.StartCoroutine(LevelManager.I.LoadNextLevelAfterDelay(0f));
//     }

//     // ---------------------------------------------------------
//     // Smooth Fade Animation
//     // ---------------------------------------------------------
//     private System.Collections.IEnumerator Fade(float target)
//     {
//         Debug.Log($"[UILevelPanel.Fade] Starting fade to target: {target}, current alpha: {cg.alpha}");
//         float t = 0f;
//         float start = cg.alpha;
//         while (t < fadeInTime)
//         {
//             cg.alpha = Mathf.Lerp(start, target, t / fadeInTime);
//             t += Time.deltaTime;
//             yield return null;
//         }
//         cg.alpha = target;
//         Debug.Log($"[UILevelPanel.Fade] Fade complete, final alpha: {cg.alpha}");
//     }
// }


using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

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

    // 🎥 NEW: Perk Tutorial UI Elements
    [Header("Perk Tutorial")]
    public GameObject perkTutorialPanel;
    public RawImage videoDisplay;
    public VideoPlayer videoPlayer;
    public Button replayButton;

    void Awake()
    {
        I = this;
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        gameObject.SetActive(false);

        // Hide tutorial panel on load
        if (perkTutorialPanel != null)
            perkTutorialPanel.SetActive(false);
            Debug.Log("🎥 Perk Tutorial Panel initialized and hidden.");
    }

    // ---------------------------------------------------------
    // LEVEL INTRO
    // ---------------------------------------------------------
    public static void ShowIntro(int level)
    {

    if (level == 1 && LevelManager.I != null && LevelManager.I.hasSeenLevel1Tutorial)
    {
        // ✅ Player already saw the tutorial once — do not auto-play again
        Debug.Log("🎥 Skipping auto tutorial replay on retry (already seen)");
        if (I != null && I.perkTutorialPanel != null)
            I.perkTutorialPanel.SetActive(false);
    }



        Debug.Log($"[UILevelPanel.ShowIntro] Called with level: {level}");
        if (I == null)
        {
            Debug.LogWarning("[UILevelPanel.ShowIntro] UILevelPanel.I is null - cannot show intro!");
            return;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        int lvl = level;

        if (!string.IsNullOrEmpty(sceneName) &&
            (sceneName == "Main" || sceneName.Contains("Level1") || sceneName == "MainForLevel1"))
        {
            lvl = 1;
        }
        else if (LevelManager.I != null)
        {
            lvl = Mathf.Max(1, LevelManager.I.currentLevel);
        }

        if (!I.gameObject.activeInHierarchy)
            I.gameObject.SetActive(true);

        I.StopAllCoroutines();
        I.StartCoroutine(I.ShowIntroRoutine(lvl));
    }

    // ---------------------------------------------------------
    // LEVEL COMPLETE PANEL
    // ---------------------------------------------------------
    public static void ShowComplete(int level)
    {
        if (I != null)
            I.StartCoroutine(I.ShowCompleteRoutine(level));
    }

    // ---------------------------------------------------------
    // INTRO SCREEN CONTENT
    // ---------------------------------------------------------
    private System.Collections.IEnumerator ShowIntroRoutine(int level)
    {
        IsIntroVisible = true;
        gameObject.SetActive(true);

        if (GameManager.I != null && GameManager.I.ui != null)
            GameManager.I.ui.HideHowTo();

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
                    "Gather every <color=yellow>coin</color> and,\nMake your way to the <color=orange>Blue Door</color>\n" +
                    "\nTeleport works in this maze too!";
            }
        }
        else if (level == 3)
        {
            subtitle.text =
                "<color=red>Enemy Wipe Activated!</color> You have 2 uses, spend them wisely.\n" +
                "Hit <color=orange>Shift</color> to <color=orange>Clear</color> enemies for 5s\n" +
                "But beware, they'll <color=yellow>Multiply</color> after!";
        }

        continueText.text = "Press <color=orange>ENTER</color> to start";

        yield return Fade(1f);

        while (!Input.GetKeyDown(KeyCode.Space) && !Input.GetKeyDown(KeyCode.Return))
            yield return null;

        yield return Fade(0f);
        gameObject.SetActive(false);
        IsIntroVisible = false;

        if (GameManager.I != null && GameManager.I.ui != null)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName != "Main" || level != 1)
                GameManager.I.ui.ShowHowTo(true);
            else
                GameManager.I.StartGame();
        }
    }

    // ---------------------------------------------------------
    // LEVEL COMPLETE SCREEN
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

        if (LevelManager.I != null)
            LevelManager.I.StartCoroutine(LevelManager.I.LoadNextLevelAfterDelay(0f));
    }

    // ---------------------------------------------------------
    // SMOOTH FADE ANIMATION
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

    // ---------------------------------------------------------
    // 🎥 PLAYER PERK TUTORIAL HANDLER
    // ---------------------------------------------------------
    public void ShowPerkTutorialForLevel(int level)
    {
        if (level == 1 && LevelManager.I != null)
        {
            LevelManager.I.hasSeenLevel1Tutorial = true;
        }
        // 🛑 prevent re-showing tutorial if panel is already open
    if (perkTutorialPanel != null && perkTutorialPanel.activeSelf)
    {
        Debug.Log("🎥 Tutorial already visible — skipping reopen.");
        return;
    }

        if (perkTutorialPanel == null || videoPlayer == null)
        {
            Debug.LogWarning("🎥 Perk Tutorial Panel or VideoPlayer not assigned!");
            return;
        }

        perkTutorialPanel.SetActive(true);

        string videoPath = "";

        // Each video in StreamingAssets folder
        if (level == 1)
            videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, "PositionSwitch_tutorial.mp4");
        else if (level == 3)
            videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, "EnemyWipe_tutorial.mp4");

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = videoPath;
        videoPlayer.isLooping = false;
        videoPlayer.Play();

        Debug.Log($"▶️ Playing Perk Tutorial for Level {level}: {videoPath}");

        // Replay button logic
        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(() =>
            {
                videoPlayer.Stop();
                videoPlayer.Play();
            });
        }

        // Handle auto-hide after playback
        videoPlayer.loopPointReached += OnTutorialEnd;
    }

    private void OnTutorialEnd(VideoPlayer vp)
    {
        continueText.text = "Press ENTER to start";
        Debug.Log("🎬 Tutorial finished — waiting for ENTER to continue.");
    }
}