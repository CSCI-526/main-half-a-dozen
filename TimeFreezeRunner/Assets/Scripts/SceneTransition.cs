using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public string targetScene;
    public bool autoLoad = false;
    public float delayBeforeLoad = 1f;

    [Header("Arrow Indicator")]
    public GameObject arrowPrefab;
    private GameObject arrowInstance;

    [Header("Visuals")]
    public SpriteRenderer doorRenderer;
    public Color inactiveColor = new Color(0f, 1f, 1f, 0.4f); // dim cyan
    public Color activeColor = new Color(0f, 1f, 1f, 1f);     // bright cyan
    private bool isUnlocked = false;

    private void Start()
    {
        if (doorRenderer != null)
            doorRenderer.color = inactiveColor;
    }

    private void Update()
    {
        // Once all coins are collected, unlock and animate door
        if (!isUnlocked && GameManager.I != null && GameManager.I.coinsCollected >= GameManager.I.totalCoins)
        {
            isUnlocked = true;
            StartCoroutine(PulseDoor());
            ShowArrow();
            
            if (arrowInstance != null)
        arrowInstance.transform.localPosition = 
            new Vector3(0f, 1.2f + Mathf.Sin(Time.time * 2f) * 0.1f, 0f);

            // 🔓 Notify player once
            if (GameManager.I.ui != null)
                GameManager.I.ui.ShowIdleToast("🔓 Cyan Door Unlocked — Proceed to the Corridor!", 2.5f);
        }
    }

    //     private void OnTriggerEnter2D(Collider2D other)
    // {
    //     if (other.CompareTag("Player"))
    //     {
    //         // ✅ Only enforce coin requirement in Level 2
    //         string sceneName = SceneManager.GetActiveScene().name;
    //         bool requireCoins = sceneName == "MainForLevel2";

    //         if (requireCoins && !isUnlocked)
    //         {
    //             Debug.Log("❌ Door locked — collect all coins first!");
    //             if (GameManager.I != null && GameManager.I.ui != null)
    //                 GameManager.I.ui.ShowIdleToast("Collect all coins to unlock the Cyan Door!", 2f);
    //             return;
    //         }

    //         Debug.Log($"✅ Transition triggered → Loading {targetScene}");
    //         StartCoroutine(LoadScene());
    //     }
    // }
private void OnTriggerEnter2D(Collider2D other)
{
    if (!other.CompareTag("Player")) return;

    string currentScene = SceneManager.GetActiveScene().name;
    string next = targetScene;

    if (currentScene == "Corridor")
    {
        // Block returning left until key collected
        if (next == "MainForLevel2" && !LevelManager.I.canReturnToLevel2)
        {
            Debug.Log("❌ Cannot return to Level 2 yet — key not collected!");
            GameManager.I?.ui?.ShowIdleToast("Find the key in the Dark Maze first!", 2f);
            return;
        }

        // Block re-entering Dark Maze after key collected
        if (next == "Level2_DarkMaze" && LevelManager.I.darkMazeCleared)
        {
            Debug.Log("🚫 Dark Maze sealed after key collection!");
            GameManager.I?.ui?.ShowIdleToast("Dark Maze is sealed after collecting the key!", 2f);
            return;
        }
    }

    if (currentScene == "MainForLevel2" && !isUnlocked)
    {
        Debug.Log("❌ Door locked — collect all coins first!");
        GameManager.I?.ui?.ShowIdleToast("Collect all coins to unlock the Cyan Door!", 2f);
        return;
    }

    Debug.Log($"✅ Transition triggered → Loading {targetScene}");
    StartCoroutine(LoadScene());
}

    private System.Collections.IEnumerator LoadScene()
    {
        if (LevelManager.I != null && GameManager.I != null)
        {
            var s = LevelManager.I.savedState;
            s.position = GameManager.I.player.transform.position;
            s.coinsCollected = GameManager.I.coinsCollected;
            s.exitUnlocked = GameManager.I.exitDoor != null && GameManager.I.exitDoor.isActiveAndEnabled;
            s.lastScene = SceneManager.GetActiveScene().name;
            s.nextScene = targetScene;

            Debug.Log($"💾 Saved state: {s.lastScene} → {s.nextScene}, position={s.position}");
        }

        yield return new WaitForSeconds(delayBeforeLoad);
        SceneManager.LoadScene(targetScene);
    }

    private System.Collections.IEnumerator PulseDoor()
    {
        float t = 0f;
        while (isUnlocked)
        {
            t += Time.deltaTime * 2f;
            if (doorRenderer != null)
                doorRenderer.color = Color.Lerp(activeColor, inactiveColor, Mathf.PingPong(t, 1f));
            yield return null;
        }
    }
    
    private void ShowArrow()
{
        if (arrowPrefab != null && arrowInstance == null)
        {
            arrowInstance = Instantiate(
                arrowPrefab,
                transform.position + new Vector3(0f, 1f, 0f),
                Quaternion.identity
            );
            arrowInstance.transform.SetParent(transform);
            arrowInstance.transform.localRotation = Quaternion.Euler(0, 0, 0);
    }
}
}