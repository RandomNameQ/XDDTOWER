using System;
using Sirenix.OdinInspector;
using UnityEngine;

public static class GlobalEvent
{
    public static event Action OnInteractShop;
    [Button]
    public static void OnInteractShopEvent()
    {
        OnInteractShop?.Invoke();
    }

}
