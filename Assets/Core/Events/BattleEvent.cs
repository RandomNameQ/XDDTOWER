using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class BattleEvent : MonoBehaviour
{

    public static event Action OnStartFight;
    [Button]
    public static void OnStartFightEvent()
    {
        OnStartFight?.Invoke();
    }
}
