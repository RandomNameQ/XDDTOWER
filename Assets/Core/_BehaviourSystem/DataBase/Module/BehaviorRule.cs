using System;
using System.Collections.Generic;
using GeneratedEnums;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class BehaviorRule
{



    // [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, ShowIndexLabels = true)]
    // public List<Trigger> Triggers = new();




    public class EffectContainer
    {
        public AttackComp attackComp;
        public StatsId stats;
    }
    [Serializable]
    public class AttackComp
    {
        public EffectId effect = EffectId.Damage;
        public StatsId stats;
        public Count count;
        public Target target;

        [Serializable]
        public class Count
        {
            public int singleValue = 5;
            public int countCast = 1;
            public Vector2Int randomValue;
        }
        [Serializable]
        public class Target
        {
            public enum Priority
            {
                None,
                ClosePosition,
                FarPosition,
                MinimumHealth,
                MaximumHealth
            }
            public Priority priority = Priority.ClosePosition;
            public AttitudeId attitude = AttitudeId.Enemy;
            public TagId tag;
            public DirectionId position;
            public int countTarget = 1;
        }
    }

    [Serializable]
    [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, ShowIndexLabels = true)]

    public class Request
    {
        // если id одинаковый, то мы считаем это за or условие
        public bool isOverrideDefaultAttackComp;
        public AttitudeId attitude;
        public OperatinoId operation;
        public TagId tag;
        public EffectId effect;
        public DirectionId position;
        public AttackComp attackComp;

    }


    public List<Request> request = new();
    public AttackComp attackComp;
    public List<EffectContainer> effectContainer = new();
}
