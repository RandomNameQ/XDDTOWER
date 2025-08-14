using System;
using Sirenix.OdinInspector;
using UnityEngine;

public static class UnitEvent
{
    public static event Action<Creature> OnUnitDied;
    [Button]
    public static void OnUpdateNeighborsEvent(Creature creature)
    {
        OnUnitDied?.Invoke(creature);
    }

}
