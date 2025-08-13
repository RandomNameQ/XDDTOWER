using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

/// <summary>
/// Базовое условие. Может проверять как самого себя (self), так и конкретную цель.
/// </summary>
[Serializable]
public abstract class Condition
{
    /// <summary>
    /// Проверка без привязки к цели (например, текущие ХП юнита и т.п.).
    /// </summary>
    public virtual bool Evaluate(Creature self) => true;

    /// <summary>
    /// Проверка на конкретного кандидата (цель).
    /// </summary>
    public virtual bool EvaluateForTarget(Creature self, Creature target) => true;

    public ConditionGroup.Operator Op = ConditionGroup.Operator.And;

}

/// <summary>
/// Группа условий (AND/OR) как для self, так и для цели.
/// </summary>
[Serializable]
public class ConditionGroup : Condition
{
    public enum Operator
    {
        And,
        Or
    }


    [SerializeReference]
    public List<Condition> Children = new();

    public override bool Evaluate(Creature self)
    {
        if (Children == null || Children.Count == 0) return true;
        if (Op == Operator.And)
        {
            foreach (var c in Children)
            {
                if (!c.Evaluate(self)) return false;
            }
            return true;
        }
        else
        {
            foreach (var c in Children)
            {
                if (c.Evaluate(self)) return true;
            }
            return false;
        }
    }

    public override bool EvaluateForTarget(Creature self, Creature target)
    {
        if (Children == null || Children.Count == 0) return true;
        if (Op == Operator.And)
        {
            foreach (var c in Children)
            {
                if (!c.EvaluateForTarget(self, target)) return false;
            }
            return true;
        }
        else
        {
            foreach (var c in Children)
            {
                if (c.EvaluateForTarget(self, target)) return true;
            }
            return false;
        }
    }
}


/// <summary>
/// Пример условия по расе цели (заглушка — доработайте сравнение с реальной расой).
/// </summary>
[Serializable]
public class RaceCondition : Condition
{
    public GeneratedEnums.RaceId race;

    public override bool EvaluateForTarget(Creature self, Creature target)
    {
        if (target == null) return false;
        var raceId = target.BehaviorProfile != null ? target.BehaviorProfile.race : GeneratedEnums.RaceId.None;
        if (raceId == GeneratedEnums.RaceId.None) return false;
        return true;
    }
}


[Serializable]
public class NeighbourCondition : Condition
{
    public GeneratedEnums.DirectionId direction;
}

[Serializable]
public class EffectCondition : Condition
{
    public GeneratedEnums.EffectId effect;
}

[Serializable]
public class StatisticCondition : Condition
{
    public GeneratedEnums.StatsId statistic;
}

[Serializable]
public class OperationCondition : Condition
{
    public enum Operation
    {
        // обьект накладывает что-то
        Add,
        // обьект получает что-то
        Get,
        // обьект удаляет что-то
        Remove
    }

    public Operation operation;
}

[Serializable]
public class TargetCondition : Condition
{
    public Target Target;
}

