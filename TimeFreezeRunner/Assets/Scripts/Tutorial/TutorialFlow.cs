// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class NewBehaviourScript : MonoBehaviour
// {
//     // Start is called before the first frame update
//     void Start()
//     {
        
//     }

//     // Update is called once per frame
//     void Update()
//     {
        
//     }
// }


using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class Level1TutorialFlow : MonoBehaviour
{
    public PlayerController player;
    public TMP_Text stepText;
    public GameObject arrowMove;
    public GameObject arrowCoin;
    public GameObject arrowTeleport;

    private bool moved = false;
    private bool collectedCoin = false;
    private bool usedTeleport = false;

    void Start()
    {
        stepText.text = "Use <b>WASD</b> or Arrow Keys to Move";
        player.moveSpeed = 4f;
    }

    void Update()
    {
        if (!moved && player.isMoving)
        {
            moved = true;
            arrowMove.SetActive(false);
            stepText.text = "Great! Now Collect the Coin";
            arrowCoin.SetActive(true);
        }

        if (moved && !collectedCoin && GameManager.I.coinsCollected > 0)
        {
            collectedCoin = true;
            arrowCoin.SetActive(false);
            stepText.text = "Nice! Press <b>Space</b> to open teleport";
            arrowTeleport.SetActive(true);
        }

        if (collectedCoin && !usedTeleport && PositionSwitchSystem.IsTargetingGlobal)
        {
            usedTeleport = true;
            stepText.text = "Pick a spot (1 or 2) to teleport!";
        }

        if (usedTeleport && !PositionSwitchSystem.IsTargetingGlobal)
        {
            StartCoroutine(TutorialComplete());
        }
    }

    private System.Collections.IEnumerator TutorialComplete()
    {
        stepText.text = "Tutorial Complete! Returning to Level 1...";
        yield return new WaitForSeconds(2f);

        // Mark tutorial as completed
        LevelManager.I.hasSeenLevel1Tutorial = true;

        SceneManager.LoadScene("Main");   // Your Level 1 scene
    }
}
