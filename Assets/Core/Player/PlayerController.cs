using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    // надо засетапить чтобы персонаж мог ходить по карте

    public Sprite image;
    public CellData position;
    public CellData previousPosition;


    private void Start()
    {

    }

    public void ChangePositionOnMap(CellData newPosition)
    {
        previousPosition = position;
        if (previousPosition != null)
        {
            previousPosition.UpdatePreviousPosition();


        }


        position = newPosition;
        position.ChangePosition();
    }

}
