using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartToLevel2 : MonoBehaviour
{
    [Header("Choose ONE setup")]
    [Tooltip("If true, the Level 2 UI and gameplay live in separate scenes.")]
    public bool useTwoScenes = false;

    [Header("Single-scene setup")]
    [Tooltip("The single Level 2 ENTRY scene that contains (or triggers) the UILevelPanel.")]
    public string level2EntryScene = "Level2_Main";

    [Header("Two-scene setup (additive)")]
    [Tooltip("Scene that contains the UILevelPanel (loaded first).")]
    public string level2UIScene = "Level2_UI";
    [Tooltip("Scene that contains the Level 2 gameplay (loaded additively).")]
    public string level2GameplayScene = "Level2_Gameplay";

    public void OnClickRestartLevel2()
    {
        // Make this look like a fresh Level 2 start so UILevelPanel.ShowIntro(2) appears
        if (LevelManager.I != null)
        {
            LevelManager.I.currentLevel    = 2;
            LevelManager.I.darkMazeCleared = false;

            var s = LevelManager.I.savedState;
            if (s != null)
            {
                s.lastScene         = "";            // not Corridor / not DarkMaze
                s.position          = Vector3.zero;  // no spawn restore
                s.coinsCollected    = 0;
                s.allCoinsCollected = false;
                s.exitUnlocked      = false;
            }
        }

        if (!useTwoScenes)
        {
            // Single-scene Level 2
            SceneManager.LoadScene(level2EntryScene, LoadSceneMode.Single);
        }
        else
        {
            // Two-scene Level 2 (UI first, then gameplay additive)
            SceneManager.LoadScene(level2UIScene, LoadSceneMode.Single);
            SceneManager.LoadScene(level2GameplayScene, LoadSceneMode.Additive);

            // Make gameplay the active scene (so spawns/FindObjectOfType work there)
            Scene activeGameplay = SceneManager.GetSceneByName(level2GameplayScene);
            if (activeGameplay.IsValid())
                SceneManager.SetActiveScene(activeGameplay);
        }
    }
}
