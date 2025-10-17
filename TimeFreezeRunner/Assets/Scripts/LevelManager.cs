using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager I;

    [Header("Completion Flags")]
public bool allLevelsCompleted = false;

    [Header("Progress Tracking")]
    public int currentLevel = 1;
    public bool darkMazeCleared = false;

    [System.Serializable]
    public class PlayerState
    {
        public Vector3 position;
        public int coinsCollected;
        public bool exitUnlocked;
        public string lastScene;
        public string nextScene;
        public bool allCoinsCollected;
    }

    public PlayerState savedState = new PlayerState();

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    // ---------------------------------------------------------
    // ✅ Called when the player finishes a level (from GameManager)
    // ---------------------------------------------------------
    public void OnLevelComplete()
    {
        Debug.Log($"✅ Level {currentLevel} complete!");
        UILevelPanel.ShowComplete(currentLevel);
        StartCoroutine(LoadNextLevelAfterDelay(2f));
    }

    // ---------------------------------------------------------
    // 🚀 Transition to the next main level
    // ---------------------------------------------------------
public System.Collections.IEnumerator LoadNextLevelAfterDelay(float delay)
{
    yield return new WaitForSeconds(delay);

    string nextScene = "";
    int nextLevel = currentLevel + 1;

    if (currentLevel == 1)
        nextScene = "MainForLevel2";
    else if (currentLevel == 2)
    {
        nextScene = "MainForLevel3";
        allLevelsCompleted = true; // ✅ mark all levels done
    }
    else
    {
        Debug.Log("🎉 All levels finished!");
        yield break;
    }

    // ✅ HARD RULE: when advancing from Level 2 → Level 3,
    // start coins at 0 and make sure the door is initially LOCKED on the next level.
    if (currentLevel == 2 && savedState != null)
    {
        savedState.coinsCollected    = 0;
        savedState.allCoinsCollected = false;
        savedState.exitUnlocked      = false;   // ensures GameManager won't re-open it on restore
        savedState.lastScene         = "";      // not a side-scene return
        savedState.nextScene         = "";
    }

    Debug.Log($"➡️ Loading next scene: {nextScene}");
    SceneManager.sceneLoaded += (scene, mode) =>
    {
        currentLevel = nextLevel;
        Debug.Log($"✅ Scene '{scene.name}' loaded → Now Level {currentLevel}");
    };

    SceneManager.LoadScene(nextScene);
}

    // ---------------------------------------------------------
    // 🌟 Called from Dark Maze when the player finds the key
    // ---------------------------------------------------------
    public void MarkDarkMazeCleared()
    {
        darkMazeCleared = true;
        Debug.Log("🌟 Dark Maze cleared! Player can now exit.");
    }

    // ---------------------------------------------------------
    // 💾 Save & Restore Player State between scenes
    // ---------------------------------------------------------
    public void ResetProgress()
    {
        currentLevel = 1;
        darkMazeCleared = false;
        savedState = new PlayerState();
    }

    public void RestorePlayerState()
    {
        if (GameManager.I == null || GameManager.I.player == null)
            return;

        var p = GameManager.I.player;
        p.transform.position = savedState.position;
        GameManager.I.coinsCollected = savedState.coinsCollected;

        // Restore exit door status
        if (savedState.exitUnlocked && GameManager.I.exitDoor != null)
            GameManager.I.exitDoor.ActivateExit(true);

        Debug.Log($"🔁 Restored player state → pos: {savedState.position}, coins: {savedState.coinsCollected}");
    }
}