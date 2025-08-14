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
    public enum Operation
    {
        None,
        Apply,
        Recevi,
        Remove
    }
    public List<GeneratedEnums.RaceId> race = new();

    public List<GeneratedEnums.OperatinoId> operation = new();
    public Operation effectInteraction;
    public List<GeneratedEnums.EffectId> effect = new();
    public List<GeneratedEnums.StatsId> stats = new();
    public List<GeneratedEnums.DirectionId> neighbours = new();

}



