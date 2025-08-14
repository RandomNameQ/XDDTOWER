using System;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;

/// <summary>
/// Базовый триггер: сообщает, когда правило должно сработать (OR между триггерами правила).
/// </summary>
[Serializable]
public class Trigger
{

    public int groupLogic = 1;
    public enum Operator
    {
        And,
        Or
    }
    public Operator op = Operator.And;

    // Back-reference to the owning rule and convenience access to its client
    public BehaviorRule OwnerRule { get; private set; }
    public Creature Creature => OwnerRule?.client;


    public virtual void Initialize(BehaviorRule owner)
    {
        OwnerRule = owner;
    }
    [Flags]
    public enum Operation
    {
        None = 0,
        Apply = 1 << 0,
        Recevi = 1 << 1,
        Remove = 1 << 2
    }
    public List<GeneratedEnums.RaceId> race = new();

    public List<GeneratedEnums.OperatinoId> operation = new();
    public Operation effectInteraction;
    public List<GeneratedEnums.EffectId> effect = new();
    public List<GeneratedEnums.StatsId> stats = new();
    public List<GeneratedEnums.DirectionId> neighbours = new();

    // Runtime state
    private bool _isSubscribed;

    // Lifecycle
    public virtual void Subscribe()
    {
        if (_isSubscribed) return;

        if ((effectInteraction & Operation.Apply) != 0) UnitEvent.OnUnitAppliedEffect += HandleUnitAppliedEffect;
        if ((effectInteraction & Operation.Recevi) != 0) UnitEvent.OnUnitReceviEffect += HandleUnitReceivedEffect;
        if (operation.Contains(GeneratedEnums.OperatinoId.Died)) UnitEvent.OnUnitDied += HandleUnitDied;

        _isSubscribed = true;
    }

    public virtual void Unsubscribe()
    {
        if (!_isSubscribed) return;

        UnitEvent.OnUnitAppliedEffect -= HandleUnitAppliedEffect;
        UnitEvent.OnUnitReceviEffect -= HandleUnitReceivedEffect;
        UnitEvent.OnUnitDied -= HandleUnitDied;

        _isSubscribed = false;
    }

    // Default handlers (override in concrete triggers)
    protected virtual void HandleUnitAppliedEffect(GeneratedEnums.EffectId effectId, Creature destiny, Creature source) { }
    protected virtual void HandleUnitReceivedEffect(GeneratedEnums.EffectId effectId, Creature destiny, Creature source) { }
    protected virtual void HandleUnitDied(Creature creature, Creature killer) { }

}



