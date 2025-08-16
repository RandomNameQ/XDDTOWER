using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEditor.PackageManager;
using UnityEngine;

/// <summary>
/// Базовый триггер: сообщает, когда правило должно сработать (OR между триггерами правила).
/// </summary>
[Serializable]
public class Trigger
{
    [Flags]
    public enum Request
    {
        None = 0,
        Reflect = 1 << 0,
        Died = 1 << 1,
    }

    public enum Target
    {
        None,
        Ally,
        Enemy,
        Any
    }
    public Request behaviour;
    public Target target;


    public virtual void Initialize(BehaviorRule owner)
    {
        this.owner = owner;
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


    public BehaviorRule owner { get; private set; }
    public Creature Creature => owner?.client;

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
        if (owner.Triggers.Count == 0) return;

        // Debug.Log($"HandleUnitAppliedEffect: {effectId} applied to {destiny?.name} by {source?.name}");
        responce.effect.Add(effectId);
        responce.destiny = destiny;
        responce.source = source;
    }
    protected virtual void HandleUnitReceivedEffect(GeneratedEnums.EffectId effectId, Creature destiny, Creature source)
    {
        if (owner.Triggers.Count == 0) return;

        // Debug.Log($"HandleUnitReceivedEffect: {effectId} received by {destiny?.name} from {source?.name}");
        responce.effect.Add(effectId);
        responce.destiny = destiny;
        responce.source = source;

        DecideTrigger();
        ResetData();
    }

    protected virtual void HandleUnitDied(Creature creature, Creature killer)
    {
        responce.destiny = creature;
        responce.source = killer;
    }


    // тут решаем и исполнились ли все события для тригера
    [Button]
    public void DecideTrigger()
    {

        if ((behaviour & Request.Reflect) != 0) Reflect();
        if ((behaviour & Request.Reflect) != 0 && target == Target.None) ReflectForUnit();



        // if (!HasAnyElement(request.race, responce.race)) return;
        // if (!HasAnyElement(request.operation, responce.operation)) return;

        // Debug.Log("Request effects: " + string.Join(", ", request.effect));
        // Debug.Log("Response effects: " + string.Join(", ", responce.effect));
        // if (!HasAnyElement(request.stats, responce.stats)) return;


        // Проверяем соседей только если указаны конкретные направления
        // if (request.neighbours != null && request.neighbours.Count > 0)
        //     if (!HasAnyElement(request.neighbours, Creature.behaviorRunner.neighbors.allSides)) return;

    }
    private bool IsEffect<T>(IEnumerable<T> collection1, IEnumerable<T> collection2)
    {
        return HasAnyElement(request.effect, responce.effect);
    }
    private bool IsNeighbours<T>(IEnumerable<T> collection1, IEnumerable<T> collection2)
    {
        if (request.neighbours == null || request.neighbours.Count == 0)
            return false;


        return HasAnyElement(collection1, collection2);
    }

    // реагируем когда раса или attitude(кроме меня) получает эффект
    public void ReflectForUnit()
    {

    }

    public void Reflect()
    {
        // если цель это я
        // if (target == Target.Me && responce.destiny == Creature)
        // проверяем тот ли тип полуичи

        if (!IsEffect(request.effect, responce.effect)) return;

        if (target != Target.None)
        {

            // есть баг? - если наш тригер "когда враг получает эффект-урон" и этот обьект наносит урон этому обьекту, то появлется бесконечный цикл
            var hittedTarget = responce.destiny;
            var teamNumber = Creature.teamNumber;

            // какая-то цель получила эффект - мы получили ее расу и сравнили с тем, что ищем
            var isAlly = teamNumber == hittedTarget.teamNumber;

            var isFindRace = request.race.Count != 0;
            var isRace = HasAnyElement(hittedTarget.behaviorProfile.races, request.race);

            if (target == Target.Ally && isAlly)
            {
                if (isFindRace)
                {
                    if (isRace)
                        ApplyEffect();
                }
                else
                {
                    ApplyEffect();
                }


            }
            if (target == Target.Enemy && !isAlly)
            {
                if (isFindRace)
                {
                    if (isRace)
                        ApplyEffect();
                }
                else
                {
                    ApplyEffect();
                }
            }
            if (target == Target.Any)
            {
                if (isFindRace)
                {
                    if (isRace)
                        ApplyEffect();
                }
                else
                {
                    ApplyEffect();
                }
            }

        }
        else
        {
            // когда владелец получает эффект и условие соседа
            // есть ли нужный сосед
            if (request.neighbours.Count != 0 && !IsNeighbours(request.neighbours, Creature.behaviorRunner.neighbors.allSides)) return;



            if (responce.destiny == Creature)
                ApplyEffect();
        }



        // как мне узнать получил ли союзник-враг-раса какой-то эффект

        // тригер: когда получаю урон, то вызывюа эффект
        // баг - эффект вызывает у цели HandleUnitReceivedEffect, что вызывает этот же тригер

        // отслеэивать источник мне пока не надо наврено
        // if (responce.source == Creature) return;







        // Это не надо потому что я уже отражаю урон
        // if (request.effectInteraction == Operation.Receive)

    }
    public void CollectDataFromEvent()
    {
        responce.race.AddRange(responce.source.behaviorProfile.races);
    }


    public void ResetData()
    {

        // Очищаем все списки
        responce.race.Clear();
        responce.operation.Clear();
        responce.effect.Clear();
        responce.stats.Clear();
        responce.neighbours.Clear();

        // Сбрасываем ссылочные типы
        responce.destiny = null;
        responce.source = null;

        // Сбрасываем enum в значение по умолчанию
        responce.effectInteraction = Operation.None;
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



