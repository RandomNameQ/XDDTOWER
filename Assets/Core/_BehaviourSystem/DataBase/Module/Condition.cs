using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using GeneratedEnums;

/// <summary>
/// Базовое условие. Может проверять как самого себя (self), так и конкретную цель.
/// </summary>
[Serializable]
public abstract class Condition
{
    public bool isActive = true;
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
        if (race == GeneratedEnums.RaceId.None) return false;
        var profile = target.BehaviorProfile;
        if (profile == null || profile.races == null || profile.races.Count == 0) return false;
        return profile.races.Contains(race);
    }
}


[Serializable]
public class NeighbourCondition : Condition
{
    public DirectionId direction;
    /// <summary>
    /// Проверка: является ли цель соседом в указанном направлении относительно self.
    /// Основано на соседях, собранных раннером (BehaviorRunner.neighbors).
    /// </summary>
    public override bool EvaluateForTarget(Creature self, Creature target)
    {
        if (self == null || target == null) return false;
        var runner = self.GetComponent<BehaviorRunner>();
        if (runner == null || runner.neighbors == null) return false;

        List<Creature> list = null;
        switch (direction)
        {
            case DirectionId.Left: list = runner.neighbors.left; break;
            case DirectionId.Right: list = runner.neighbors.right; break;
            case DirectionId.Front: list = runner.neighbors.front; break;
            case DirectionId.Back: list = runner.neighbors.back; break;
            case DirectionId.FrontLeft: list = runner.neighbors.frontLeft; break;
            case DirectionId.FrontRight: list = runner.neighbors.frontRight; break;
            case DirectionId.BackLeft: list = runner.neighbors.backLeft; break;
            case DirectionId.BackRight: list = runner.neighbors.backRight; break;
            default: return false;
        }
        if (list == null) return false;
        return list.Contains(target);
    }
}

[Serializable]
public class EffectCondition : Condition
{
    public EffectId effect;
}

[Serializable]
public class StatisticCondition : Condition
{
    public StatsId statistic;
}

// если условия выполняются, то это перехытвает контроль над обычным поведением и заменяет.
[Serializable]
public class ChangeBehaviorCondition : Condition
{
    public Target Target;
    public EffectId effect;
    public StatsId statistic;
    public Value value;
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

/// <summary>
/// Специальное условие смены поведения: если присутствует в списке условий правила и
/// все предыдущие (обычные) условия выполнены, то вместо "стандартной" логики правила
/// применяется альтернативная: берётся указанный Target/Effect/Value и исполняется он.
/// Это позволяет переопределять действие правила в конкретных ситуациях.
/// </summary>
// Удалён дублирующийся вариант ChangeBehaviorCondition (см. реализацию выше по файлу)

