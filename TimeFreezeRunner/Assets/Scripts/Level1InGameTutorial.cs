// using UnityEngine;
// using TMPro;
// using System.Collections.Generic;
// using System.Collections;



// public class Level1InGameTutorial : MonoBehaviour
// {
//     public PlayerController player;
//     public GameObject floatingTextPrefab;
//     public GameObject arrowPrefab;
//     private GameObject arrowObj;
//     private List<GameObject> coinArrows = new List<GameObject>();
//     public TMP_Text screenMessageText;
//     public GameObject exitArrowPrefab;
//     public GameObject exitLabelPrefab;

//     private GameObject exitArrowObj;
//     private GameObject exitLabelObj;

//     bool allCoinsStepDone = false;

//     bool teleportInstructionShown = false;
//     bool teleportModeShown = false;
//     bool teleportUsed = false;
    



//     GameObject floatingTextObj;
//     TMP_Text text;

//     bool started = false;
//     bool moved = false;
//     bool firstCoinCollected = false;

//     Coin nearestCoin;

//     void Start()
//     {
//         Debug.Log("Tutorial START called.");

//         floatingTextObj = Instantiate(floatingTextPrefab);
//         Debug.Log("Spawned floatingTextObj: " + floatingTextObj);

//         text = floatingTextObj.GetComponentInChildren<TMP_Text>();
//         floatingTextObj.SetActive(false);

//         nearestCoin = FindClosestCoin();
//     }


//     void Update()
//     {
//         // Wait until the game starts
//         if (!started)
//         {
//             if (GameManager.I.IsPlaying)
//             {
//                 started = true;
//                 floatingTextObj.SetActive(true);
//                 text.text = "Use WASD / Arrow Keys to Move";
//             }
//             return;
//         }

//         // STEP 1 — Movement tutorial
//         if (!moved)
//         {
//             PositionTextAbove(player.transform.position);

//             if (player.isMoving)
//             {
//                 moved = true;
//                 text.text = "Collect a coin!";

//                 // Spawn arrows above ALL coins
//                 foreach (Coin coin in FindObjectsOfType<Coin>())
//                 {
//                     GameObject arrow = Instantiate(arrowPrefab);
//                     arrow.transform.position = coin.transform.position + new Vector3(0, 1.2f, 0);
//                     coinArrows.Add(arrow);
//                 }
//             }
//             return;
//         }

//         // STEP 2 — First Coin Tutorial
//         if (moved && !firstCoinCollected)
//         {
//             // Update coin arrows
//             foreach (Coin coin in FindObjectsOfType<Coin>())
//             {
//                 int i = FindCoinIndex(coin);
//                 if (i >= 0 && i < coinArrows.Count)
//                 {
//                     coinArrows[i].transform.position = coin.transform.position + new Vector3(0, 1.2f, 0);
//                 }
//             }

//             PositionTextAbove(player.transform.position);

//             // First coin collected
//             if (GameManager.I.coinsCollected > 0)
//             {
//                 firstCoinCollected = true;

//                 // Remove coin arrows
//                 foreach (var arrow in coinArrows)
//                     Destroy(arrow);
//                 coinArrows.Clear();

//                 floatingTextObj.SetActive(false);

//                 // BEGIN TELEPORT TUTORIAL
//                 teleportInstructionShown = true;
//                 GameManager.I.ignoreTeleportUse = true;   // <-- ADD THIS
//                 screenMessageText.gameObject.SetActive(true);
//                 screenMessageText.text = "Press SPACE to open teleport!";
//             }

//             return;
//         }

//         // STEP 3 — TELEPORT TUTORIAL
//         if (firstCoinCollected && !teleportUsed)
//         {
//             // Player pressed SPACE, teleport mode opens
//             if (teleportInstructionShown && PositionSwitchSystem.IsTargetingGlobal && !teleportModeShown)
//             {
//                 teleportModeShown = true;
//                 screenMessageText.text = "Press 1 or 2 to teleport!";
//             }

//             // Player teleported (exited targeting)
//             if (teleportModeShown && !PositionSwitchSystem.IsTargetingGlobal)
//             {
//                 teleportUsed = true;
//                 GameManager.I.ignoreTeleportUse = false;

//                 screenMessageText.text = "Nice! Collect all coins before reaching the EXIT!";
//                 StartCoroutine(HideScreenMessageAfter(3f));
//             }

//             return;
//         }

//         // STEP 4 — After ALL coins collected
//         if (!allCoinsStepDone && GameManager.I.coinsCollected >= GameManager.I.totalCoins)
//         {
//             allCoinsStepDone = true;

//             Transform exitPos = GameManager.I.exitDoor.transform;

//             // Arrow
//             exitArrowObj = Instantiate(exitArrowPrefab);
//             exitArrowObj.transform.position = exitPos.position + new Vector3(1.3f, 0f, 0f);

//             // Label
//             exitLabelObj = Instantiate(exitLabelPrefab);
//             exitLabelObj.transform.position = exitPos.position + new Vector3(2.8f, 0f, 0f);
//             TMP_Text lbl = exitLabelObj.GetComponentInChildren<TMP_Text>();
//             lbl.text = "Head to Exit!";

//             return;
//         }

//         // Cleanup at exit
//         if (allCoinsStepDone && PlayerReachedExit())
//         {
//             if (exitArrowObj != null) Destroy(exitArrowObj);
//             if (exitLabelObj != null) Destroy(exitLabelObj);
//         }
//     }


//     void PositionTextAbove(Vector3 pos)
//     {
//         floatingTextObj.transform.position = pos + new Vector3(0, 1.8f, 0);
//     }

//     void PositionTextNear(Vector3 pos)
//     {
//         floatingTextObj.transform.position = pos + new Vector3(0, 1.0f, 0);
//     }

//     bool PlayerReachedExit()
//     {
//         if (GameManager.I.exitDoor == null) return false;

//         return Vector3.Distance(
//             player.transform.position,
//             GameManager.I.exitDoor.transform.position
//         ) < 1.2f;
//     }


//     Coin FindClosestCoin()
//     {
//         var coins = FindObjectsOfType<Coin>();
//         float minDist = Mathf.Infinity;
//         Coin closest = null;

//         foreach (var c in coins)
//         {
//             float d = Vector3.Distance(player.transform.position, c.transform.position);
//             if (d < minDist)
//             {
//                 minDist = d;
//                 closest = c;
//             }
//         }
//         return closest;
//     }

//     System.Collections.IEnumerator HideTextAfter(float t)
//     {
//         yield return new WaitForSeconds(t);
//         floatingTextObj.SetActive(false);
//     }
//     int FindCoinIndex(Coin coin)
//     {
//         var coins = FindObjectsOfType<Coin>();
//         for (int i = 0; i < coins.Length; i++)
//         {
//             if (coins[i] == coin)
//                 return i;
//         }
//         return -1;
//     }
//     IEnumerator HideScreenMessageAfter(float t)
//     {
//         yield return new WaitForSeconds(t);
//         screenMessageText.gameObject.SetActive(false);
//     }
//     public void ResetTutorialState()
//     {
//         started = false;
//         moved = false;
//         firstCoinCollected = false;
//         teleportInstructionShown = false;
//         teleportModeShown = false;
//         teleportUsed = false;
//         allCoinsStepDone = false;

//         // Hide UI
//         if (floatingTextObj != null)
//             floatingTextObj.SetActive(false);

//         if (screenMessageText != null)
//             screenMessageText.gameObject.SetActive(false);

//         // Destroy arrows if any
//         if (exitArrowObj != null) Destroy(exitArrowObj);
//         if (exitLabelObj != null) Destroy(exitLabelObj);
//         foreach (var a in coinArrows) Destroy(a);
//         coinArrows.Clear();

//         // Reset teleport-ignore flag
//         GameManager.I.ignoreTeleportUse = false;
//     }


// }


using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class Level1InGameTutorial : MonoBehaviour
{
    public PlayerController player;
    public GameObject floatingTextPrefab;
    public GameObject arrowPrefab;
    public TMP_Text screenMessageText;
    public GameObject exitArrowPrefab;
    public GameObject exitLabelPrefab;

    private GameObject floatingTextObj;
    private TMP_Text text;
    private List<GameObject> coinArrows = new List<GameObject>();

    private GameObject exitArrowObj;
    private GameObject exitLabelObj;

    bool started = false;
    bool moved = false;
    bool firstCoinCollected = false;

    bool teleportInstructionShown = false;
    bool teleportModeShown = false;
    bool teleportUsed = false;

    bool allCoinsStepDone = false;


    // ============================================================
    // INITIALIZATION
    // ============================================================
    void Start()
    {
        floatingTextObj = Instantiate(floatingTextPrefab);
        text = floatingTextObj.GetComponentInChildren<TMP_Text>();
        floatingTextObj.SetActive(false);
    }


    // ============================================================
    // MAIN UPDATE LOOP
    // ============================================================
    void Update()
    {
        // STEP 0 — wait for level to actually begin
        if (!started)
        {
            if (GameManager.I.IsPlaying)
            {
                started = true;
                floatingTextObj.SetActive(true);
                text.text = "Use WASD / Arrow Keys to Move";
            }
            return;
        }

        // STEP 1 — first movement
        if (!moved)
        {
            PositionTextAbove(player.transform.position);

            if (player.isMoving)
            {
                moved = true;
                text.text = "Collect a coin!";

                foreach (Coin coin in FindObjectsOfType<Coin>())
                {
                    GameObject arrow = Instantiate(arrowPrefab);
                    arrow.transform.position = coin.transform.position + new Vector3(0, 1.2f, 0);
                    coinArrows.Add(arrow);
                }
            }
            return;
        }

        // STEP 2 — collect first coin
        if (moved && !firstCoinCollected)
        {
            // keep the arrows on top of coins
            foreach (Coin coin in FindObjectsOfType<Coin>())
            {
                int idx = FindCoinIndex(coin);
                if (idx >= 0 && idx < coinArrows.Count)
                {
                    coinArrows[idx].transform.position = coin.transform.position + new Vector3(0, 1.2f, 0);
                }
            }

            PositionTextAbove(player.transform.position);

            if (GameManager.I.coinsCollected > 0)
            {
                firstCoinCollected = true;

                foreach (var a in coinArrows) Destroy(a);
                coinArrows.Clear();
                floatingTextObj.SetActive(false);

                // BEGIN TELEPORT TUTORIAL
                teleportInstructionShown = true;
                GameManager.I.ignoreTeleportUse = true;

                FreezePlayer();
                FreezeEnemies(true);

                screenMessageText.gameObject.SetActive(true);
                screenMessageText.text = "Press SPACE to open teleport!";
            }

            return;
        }

        // STEP 3 — TELEPORT SECTION
        // STEP 3 — TELEPORT TUTORIAL
        if (firstCoinCollected && !teleportUsed)
        {
            // 3A — teleport mode opened (player pressed SPACE)
            if (teleportInstructionShown && PositionSwitchSystem.IsTargetingGlobal && !teleportModeShown)
            {
                teleportModeShown = true;
                screenMessageText.text = "Press 1 or 2 to teleport!";
            }

            // 3B — teleport completed (player pressed 1 or 2)
            if (teleportModeShown && !PositionSwitchSystem.IsTargetingGlobal)
            {
                teleportUsed = true;
                GameManager.I.ignoreTeleportUse = false;

                // ⭐ only unfreeze HERE ⭐
                UnfreezePlayer();
                FreezeEnemies(false);

                screenMessageText.text = "Nice! Collect all coins before reaching the EXIT!";
                StartCoroutine(HideScreenMessageAfter(3f));
            }

            return;
        }


        // STEP 4 — after ALL coins collected → show exit arrow & label
        if (!allCoinsStepDone && GameManager.I.coinsCollected >= GameManager.I.totalCoins)
        {
            allCoinsStepDone = true;

            Transform exitPos = GameManager.I.exitDoor.transform;

            exitArrowObj = Instantiate(exitArrowPrefab);
            exitArrowObj.transform.position = exitPos.position + new Vector3(1.3f, 0f, 0f);

            exitLabelObj = Instantiate(exitLabelPrefab);
            exitLabelObj.transform.position = exitPos.position + new Vector3(2.8f, 0f, 0f);
            exitLabelObj.GetComponentInChildren<TMP_Text>().text = "Head to Exit!";

            return;
        }

        // STEP 5 — cleanup when player reaches exit
        if (allCoinsStepDone && PlayerReachedExit())
        {
            if (exitArrowObj != null) Destroy(exitArrowObj);
            if (exitLabelObj != null) Destroy(exitLabelObj);
        }
    }


    // ============================================================
    // FREEZE / UNFREEZE HELPERS
    // ============================================================

    void FreezePlayer()
    {
        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            rb.velocity = Vector2.zero;
            player.enabled = false;   // ⭐ STOP PLAYER
        }
    }

    void UnfreezePlayer()
    {
        if (player != null)
        {
            player.enabled = true;    // ⭐ RESUME PLAYER
        }
    }

    // void FreezeEnemies(bool freeze)
    // {
    //     foreach (var e in FindObjectsOfType<EnemyChaser>())
    //     {
    //         e.enabled = !freeze;      // ⭐ STOP/START ENEMY MOVEMENT
    //     }
    // }
    void FreezeEnemies(bool freeze)
    {
        foreach (var e in FindObjectsOfType<EnemyChaser>())
        {
            Rigidbody2D rb = e.GetComponent<Rigidbody2D>();

            if (freeze)
            {
                // Stop movement script
                e.enabled = false;

                // Stop physics
                if (rb != null)
                {
                    rb.velocity = Vector2.zero;
                    rb.constraints = RigidbodyConstraints2D.FreezeAll;
                }
            }
            else
            {
                // Resume physics
                if (rb != null)
                {
                    rb.constraints = RigidbodyConstraints2D.FreezeRotation;  
                }

                // Resume movement script
                e.enabled = true;
            }
        }
    }


    // ============================================================
    // UTILITY FUNCTIONS
    // ============================================================

    void PositionTextAbove(Vector3 pos)
    {
        floatingTextObj.transform.position = pos + new Vector3(0, 1.8f, 0);
    }

    bool PlayerReachedExit()
    {
        if (GameManager.I.exitDoor == null) return false;
        return Vector3.Distance(player.transform.position, GameManager.I.exitDoor.transform.position) < 1.2f;
    }

    int FindCoinIndex(Coin c)
    {
        Coin[] coins = FindObjectsOfType<Coin>();
        for (int i = 0; i < coins.Length; i++)
            if (coins[i] == c) return i;
        return -1;
    }

    IEnumerator HideScreenMessageAfter(float t)
    {
        yield return new WaitForSeconds(t);
        screenMessageText.gameObject.SetActive(false);
    }


    // ============================================================
    // RESET TUTORIAL WHEN PLAYER DIES
    // ============================================================

    public void ResetTutorialState()
    {
        started = false;
        moved = false;
        firstCoinCollected = false;
        teleportInstructionShown = false;
        teleportModeShown = false;
        teleportUsed = false;
        allCoinsStepDone = false;

        if (floatingTextObj != null)
            floatingTextObj.SetActive(false);

        if (screenMessageText != null)
            screenMessageText.gameObject.SetActive(false);

        if (exitArrowObj != null) Destroy(exitArrowObj);
        if (exitLabelObj != null) Destroy(exitLabelObj);

        foreach (var a in coinArrows) Destroy(a);
        coinArrows.Clear();

        GameManager.I.ignoreTeleportUse = false;

        FreezeEnemies(false);
    }
}
