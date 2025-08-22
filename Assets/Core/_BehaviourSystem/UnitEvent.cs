using System;
using GeneratedEnums;
using Sirenix.OdinInspector;
using UnityEngine;

public static class UnitEvent
{
    public static event Action<Creature, Creature> OnUnitDied;
    public static void OnUnitDiedsEvent(Creature destiny, Creature source)
    {
        OnUnitDied?.Invoke(destiny, source);
    }

    public static event Action<EffectId, Creature, Creature> OnUnitReceiveEffect;
    public static void OnUnitRecieveEffectEvent(EffectId effect, Creature destiny, Creature source) => OnUnitReceiveEffect?.Invoke(effect, destiny, source);

    public static event Action<EffectId, Creature, Creature> OnUnitAppliedEffect;
    public static void OnUnitAppliedEffectEvent(EffectId effect, Creature destiny, Creature source) => OnUnitAppliedEffect?.Invoke(effect, destiny, source);

    public static event Action<Creature> OnUnitSpawnedOnBattlleBoard;
    public static void OnUnitSpawnedOnBattleBoardEvent(Creature unit) => OnUnitSpawnedOnBattlleBoard?.Invoke(unit);

    public static event Action<Creature> OnUnitRemovedFromBattleBoard;
    public static void OnUnitRemovedFromBattleBoardEvent(Creature unit) => OnUnitRemovedFromBattleBoard?.Invoke(unit);

    public static event Action OnUnitChangePositionOnBoard;
    public static void OnUnitChangePositionOnBoardEvent() => OnUnitChangePositionOnBoard?.Invoke();


}
