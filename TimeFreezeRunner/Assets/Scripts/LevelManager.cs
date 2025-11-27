using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour

{

    [HideInInspector] 
public bool hasSeenLevel1Tutorial = false;
    public static LevelManager I;

    [Header("Completion Flags")]
    public bool allLevelsCompleted = false;

    [Header("Progress Tracking")]
    public int currentLevel = 1;
    public bool darkMazeCleared = false;

    public int switchesUsed;

    [System.Serializable]
    public class PlayerState
    {
        public Vector3 position;
        public int coinsCollected;
        public bool exitUnlocked;
        public string lastScene;
        public string nextScene;
        public bool allCoinsCollected;

        public int switchesUsed;
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

    public void OnLevelComplete()
    {
        Debug.Log($"✅ Level {currentLevel} complete!");
        UILevelPanel.ShowComplete(currentLevel);
        StartCoroutine(LoadNextLevelAfterDelay(2f));
    }

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
            allLevelsCompleted = true; 
        }
        else
        {
            Debug.Log("🎉 All levels finished!");
            yield break;
        }

        if (currentLevel == 2 && savedState != null)
        {
            savedState.coinsCollected = 0;
            savedState.allCoinsCollected = false;
            savedState.exitUnlocked = false;
            savedState.lastScene = "";
            savedState.nextScene = "";
            
            if(savedState != null)
            {
                savedState.switchesUsed = 0;
            }
        }

        Debug.Log($"➡️ Loading next scene: {nextScene}");
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            currentLevel = nextLevel;
            Debug.Log($"✅ Scene '{scene.name}' loaded → Now Level {currentLevel}");
        };

        SceneManager.LoadScene(nextScene);
    }

    public bool canReturnToLevel2 = false;

public void MarkDarkMazeCleared()
{
    darkMazeCleared = true;
    canReturnToLevel2 = true;
    Debug.Log("🌟 Dark Maze cleared! Corridor updated.");
}

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

        if (savedState.exitUnlocked && GameManager.I.exitDoor != null)
            GameManager.I.exitDoor.ActivateExit(true);

        Debug.Log($"🔁 Restored player state → pos: {savedState.position}, coins: {savedState.coinsCollected}");
    }
    
    
}