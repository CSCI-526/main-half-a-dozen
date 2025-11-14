using UnityEngine;
using TMPro;

public class BulbCounter : MonoBehaviour
{
    public TextMeshProUGUI counterText;

    private int totalBulbs;
    private int litBulbs = 0;

    void Start()
    {
        totalBulbs = FindObjectsOfType<BeaconLightController>().Length;
        UpdateCounter();
    }

    public void IncrementBulbCount()
    {
        litBulbs++;
        UpdateCounter();
    }

    void UpdateCounter()
    {
        if (counterText != null)
            counterText.text = $"Bulbs Lit: {litBulbs}/{totalBulbs}";
    }
}