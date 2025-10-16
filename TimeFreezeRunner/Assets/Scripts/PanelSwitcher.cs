using UnityEngine;

public class PanelSwitcher : MonoBehaviour
{
    public GameObject howToPanel;
    public GameObject playerPerksPanel;

    public void ShowPlayerPerks()
    {
        Debug.Log("ShowPlayerPerks called!");
        howToPanel.SetActive(false);
        playerPerksPanel.SetActive(true);
        Debug.Log($"howToPanel active: {howToPanel.activeInHierarchy}, playerPerksPanel active: {playerPerksPanel.activeInHierarchy}");
    }

    public void StartGameFromPerks()
    {
        Debug.Log("StartGameFromPerks called!");
        playerPerksPanel.SetActive(false);
        // Let GameManager handle the actual game start
        if (GameManager.I != null)
            GameManager.I.StartGame();
        Debug.Log("Game started from perks panel!");
    }
}
