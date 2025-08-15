using System;
using Sirenix.OdinInspector;
using UnityEngine;

public static class UnitEvent
{
    public static event Action<Creature, Creature> OnUnitDied;
    public static void OnUnitDiedsEvent(Creature creature, Creature killer)
    {
        OnUnitDied?.Invoke(creature, killer);
    }

    public static event Action<GeneratedEnums.EffectId, Creature, Creature> OnUnitReceiveEffect;
    public static void OnUnitRecieveEffectEvent(GeneratedEnums.EffectId effect, Creature destiny, Creature source)
    {
        OnUnitReceiveEffect?.Invoke(effect, destiny, source);
    }

    public static event Action<GeneratedEnums.EffectId, Creature, Creature> OnUnitAppliedEffect;
    public static void OnUnitAppliedEffectEvent(GeneratedEnums.EffectId effect, Creature destiny, Creature source)
    {
        var handler = OnUnitAppliedEffect;
        handler?.Invoke(effect, destiny, source);
    }

}
