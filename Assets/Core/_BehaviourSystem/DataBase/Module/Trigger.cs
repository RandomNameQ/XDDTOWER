using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.PackageManager;
using UnityEngine;

/// <summary>
/// Базовый триггер: сообщает, когда правило должно сработать (OR между триггерами правила).
/// </summary>
[Serializable]
public class Trigger
{

    public virtual void Initialize(BehaviorRule owner)
    {
        OwnerRule = owner;
    }
    [Flags]
    public enum Operation
    {
        None = 0,
        Apply = 1 << 0,
        Receive = 1 << 1,
        Remove = 1 << 2
    }
    public enum Operator
    {
        And,
        Or
    }
    public Operator op = Operator.And;
    public int groupLogic = 1;


    public BehaviorRule OwnerRule { get; private set; }
    public Creature Creature => OwnerRule?.client;

    [Serializable]
    public class TriggerRequest
    {
        public List<GeneratedEnums.RaceId> race = new();
        public List<GeneratedEnums.OperatinoId> operation = new();
        public Operation effectInteraction;
        public List<GeneratedEnums.EffectId> effect = new();
        public List<GeneratedEnums.StatsId> stats = new();
        public List<GeneratedEnums.DirectionId> neighbours = new();
        public Creature destiny;
        public Creature source;
    }
    public TriggerRequest request;
    public TriggerRequest responce;




    // Runtime state
    private bool _isSubscribed;

    // Lifecycle
    public virtual void Subscribe()
    {
        if (_isSubscribed) return;


        if ((request.effectInteraction & Operation.Apply) != 0) UnitEvent.OnUnitAppliedEffect += HandleUnitAppliedEffect;
        if ((request.effectInteraction & Operation.Receive) != 0) UnitEvent.OnUnitReceiveEffect += HandleUnitReceivedEffect;
        if (request.operation.Contains(GeneratedEnums.OperatinoId.Died)) UnitEvent.OnUnitDied += HandleUnitDied;

        _isSubscribed = true;
    }

    public virtual void Unsubscribe()
    {
        if (!_isSubscribed) return;

        UnitEvent.OnUnitAppliedEffect -= HandleUnitAppliedEffect;
        UnitEvent.OnUnitReceiveEffect -= HandleUnitReceivedEffect;
        UnitEvent.OnUnitDied -= HandleUnitDied;

        _isSubscribed = false;
    }

    // Default handlers (override in concrete triggers)
    protected virtual void HandleUnitAppliedEffect(GeneratedEnums.EffectId effectId, Creature destiny, Creature source)
    {
        responce.effect.Add(effectId);
        responce.destiny = destiny;
        responce.source = source;
    }
    protected virtual void HandleUnitReceivedEffect(GeneratedEnums.EffectId effectId, Creature destiny, Creature source)
    {
        responce.effect.Add(effectId);
        responce.destiny = destiny;
        responce.source = source;

        DecideTrigger();
    }
    protected virtual void HandleUnitDied(Creature creature, Creature killer)
    {
        responce.destiny = creature;
        responce.source = killer;
    }


    // тут решаем и исполнились ли все события для тригера
    public void DecideTrigger()
    {

        ResetData();

        // if (!HasAnyElement(request.race, responce.race)) return;
        // if (!HasAnyElement(request.operation, responce.operation)) return;
        // if (!HasAnyElement(request.effect, responce.effect)) return;
        // if (!HasAnyElement(request.stats, responce.stats)) return;

        // if(Creature.behaviorRunner.neighbors)

        // тригер: когда получаю урон, то вызывюа эффект
        // баг - эффект вызывает у цели HandleUnitReceivedEffect, что вызывает этот же тригер
        if (request.effectInteraction == Operation.Receive && responce.source != Creature)
            ApplyEffect();
    }
    public void CollectDataFromEvent()
    {
        responce.race.AddRange(responce.source.behaviorProfile.races);
    }


    public void ResetData()
    {

    }

    public void ApplyEffect()
    {
        Creature.PrepareUseEffect();
    }

    private bool HasIntersection<T>(IEnumerable<T> collection1, IEnumerable<T> collection2)
    {
        foreach (var item1 in collection1)
        {
            foreach (var item2 in collection2)
            {
                if (item1.Equals(item2))
                    return true;
            }
        }
        return false;
    }

    private bool HasAllElements<T>(IEnumerable<T> collection1, IEnumerable<T> collection2)
    {
        foreach (var item1 in collection1)
        {
            bool found = false;
            foreach (var item2 in collection2)
            {
                if (item1.Equals(item2))
                {
                    found = true;
                    break;
                }
            }
            if (!found)
                return false;
        }
        return true;
    }

    private bool HasAnyElement<T>(IEnumerable<T> collection1, IEnumerable<T> collection2)
    {
        foreach (var item1 in collection1)
        {
            foreach (var item2 in collection2)
            {
                if (item1.Equals(item2))
                    return true;
            }
        }
        return false;
    }

}



