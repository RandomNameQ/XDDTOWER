using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor.TypeSearch;
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
        Any,
        Me
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

    [NonSerialized]
    public BehaviorRule owner;
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
        public Creature victimCreature;
        public Creature killerCreature;
    }
    public TriggerRequest request;
    public TriggerRequest responce;

    public Creature targetFromTrigger;
    public bool isGetTargetFromTrigger;




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

        DecideTrigger();
        ResetData();
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
        responce.victimCreature = creature;
        responce.killerCreature = killer;

        DecideTrigger();
        ResetData();
    }


    // тут решаем и исполнились ли все события для тригера
    [Button]
    public void DecideTrigger()
    {

        if ((behaviour & Request.Reflect) != 0) Reflect();
        if ((behaviour & Request.Died) != 0) DiedTrigger();
        if (request.effectInteraction == Operation.Apply) TargetApply();
        // if ((request.operation & GeneratedEnums.OperatinoId.Died) != 0 || trigg) DiedTrigger();



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
    [Button]
    private List<Creature> GetSideTargets(List<GeneratedEnums.DirectionId> sides)
    {
        List<Creature> targets = new();
        foreach (var side in sides)
        {
            var creaturesInDirection = Creature.behaviorRunner.GetCreaturesByDirection(side);
            if (creaturesInDirection != null)
            {
                targets.AddRange(creaturesInDirection);
            }
        }
        return targets;
    }
    public void DiedTrigger()
    {


        if (target != Target.None)
        {
            if (request.neighbours.Count != 0)
            {
                // допустим если справа от меня умирает юнит
                List<GeneratedEnums.DirectionId> reqsetSide = request.neighbours;
                var sideTargets = GetSideTargets(reqsetSide);

                // теперь надо узнать находятся ли эти юниты в убитых
                bool isKilledUnitFinded = false;
                foreach (var unit in sideTargets)
                {
                    // responce.killerCreature это убийца victimCreature
                    if (unit == responce.victimCreature)
                    {
                        isKilledUnitFinded = true;
                    }
                }
                if (!isKilledUnitFinded) return;
            }


            var victim = responce.victimCreature;
            var killer = responce.killerCreature;

            var teamNumber = Creature.teamNumber;
            var isAlly = teamNumber == victim.teamNumber;

            var isFindRace = request.race.Count != 0;
            var isRace = HasAnyElement(victim.behaviorProfile.races, request.race);


            if (target == Target.Ally && isAlly)
            {
                if (responce.victimCreature == Creature) return;

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
            if (target == Target.Me)
            {
                if (responce.victimCreature != Creature) return;
                ApplyEffect();
            }
        }
    }

    public void TargetApply()
    {
        // если имы ищем ANY эффекты то пропускаем поиск совпаднией, так как ищем все, а в TargetApply попадтю только те, кто наложиоли эффекты
        if (!request.effect.Contains(GeneratedEnums.EffectId.Any))
            if (!IsEffect(request.effect, responce.effect)) return;


        if (target != Target.None)
        {

            if (request.neighbours.Count != 0)
            {
                // допустим если справа от меня умирает юнит
                List<GeneratedEnums.DirectionId> reqsetSide = request.neighbours;
                var sideTargets = GetSideTargets(reqsetSide);

                // теперь надо узнать находятся ли эти юниты в убитых
                bool isUnitFinded = false;
                foreach (var unit in sideTargets)
                {
                    // responce.killerCreature это убийца victimCreature
                    if (unit == responce.source)
                    {
                        isUnitFinded = true;
                    }
                }
                if (!isUnitFinded) return;
            }

            var chekedCreature = responce.source;
            var myTeam = Creature.teamNumber;

            // какая-то цель получила эффект - мы получили ее расу и сравнили с тем, что ищем
            var isAlly = myTeam == chekedCreature.teamNumber;

            var isFindRace = request.race.Count != 0;
            var isRace = HasAnyElement(chekedCreature.behaviorProfile.races, request.race);

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
        responce.victimCreature = null;
        responce.killerCreature = null;

        targetFromTrigger = null;
        // Сбрасываем enum в значение по умолчанию
        responce.effectInteraction = Operation.None;
    }

    public void ApplyEffect()
    {
        Creature.PrepareUseEffect();
        targetFromTrigger = responce.killerCreature;
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



