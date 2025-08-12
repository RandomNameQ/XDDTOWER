using System;
using Sirenix.OdinInspector;
using UnityEngine;

public static class GlobalEvent
{
    public static event Action OnInteractShop;
    public static event Action OnUpdateNeighbors;
    [Button]
    public static void OnInteractShopEvent()
    {
        OnInteractShop?.Invoke();
    }

    [Button]
    public static void OnUpdateNeighborsEvent()
    {
        OnUpdateNeighbors?.Invoke();
    }

}
