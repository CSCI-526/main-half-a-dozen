using UnityEngine;
using TMPro;

public class CorridorUIController : MonoBehaviour
{
    public TMP_Text infoText;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            infoText.text = "Loading next area...";
        }
    }
}