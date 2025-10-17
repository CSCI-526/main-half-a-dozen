using UnityEngine;

public class CorridorArrowManager : MonoBehaviour
{
    [Header("Arrow Instances (Scene Objects)")]
    public DirectionArrow rightArrow;
    public DirectionArrow leftArrow;

    [Header("Optional Auto-Hide")]
    public bool hideAfterSeconds = false;
    public float visibleSeconds = 6f;

    private bool darkRoomDone = false;

    void Start()
    {
        ShowRight();   
        if (hideAfterSeconds)
            Invoke(nameof(HideAll), visibleSeconds);
    }

    public void OnDarkRoomComplete()
    {
        darkRoomDone = true;
        ShowLeft();
    }

    void ShowRight()
    {
        if (rightArrow != null)
        {
            rightArrow.PointRight();
            rightArrow.Show(true);
        }
        if (leftArrow != null)
            leftArrow.Show(false);

        Debug.Log("➡️ Showing RIGHT arrow (go to Dark Room)");
    }

    void ShowLeft()
    {
        if (leftArrow != null)
        {
            leftArrow.PointLeft();
            leftArrow.Show(true);
        }
        if (rightArrow != null)
            rightArrow.Show(false);

        Debug.Log("⬅️ Showing LEFT arrow (return to Maze)");
    }

    void HideAll()
    {
        if (rightArrow != null) rightArrow.Show(false);
        if (leftArrow != null)  leftArrow.Show(false);
    }
}
