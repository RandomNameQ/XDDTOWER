using Sirenix.OdinInspector;
using UnityEngine;

public class QuickAccess : MonoBehaviour
{



    [Button]
    public void InteractShop()
    {
        GlobalEvent.OnInteractShopEvent();
    }
}