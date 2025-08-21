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

        [Serializable]
        [TableList(AlwaysExpanded = true, DrawScrollView = false)]
        [InlineProperty]
        public class BoolContainer<T>
        {
            public bool isActivated;
            [EnumToggleButtons]
            [HideLabel]
            [GUIColor(0.85f, 1f, 0.85f)]

            public T value;
        }
        [HideLabel]
        [BoxGroup("Attitude", centerLabel: true)]
        public BoolContainer<AttitudeId> attitude;

        [BoxGroup("Operation", centerLabel: true)]
        [HideLabel]

        public BoolContainer<OperatinoId> operation;
        [BoxGroup("Tag", centerLabel: true)]
        [HideLabel]

        public BoolContainer<TagId> tag;
        [BoxGroup("Tag Target", centerLabel: true)]
        [HideLabel]

        public BoolContainer<TagId> tagTarget;
        [BoxGroup("Effect", centerLabel: true)]
        [HideLabel]

        public BoolContainer<EffectId> effect;
        [HideLabel]
        [BoxGroup("Position", centerLabel: true)]
        public BoolContainer<DirectionId> side;
        [HideLabel]
        [BoxGroup("Attack Comp", centerLabel: true)]
        public BoolContainer<AttackComp> attackComp;

    }


    public List<Request> request = new();
    public AttackComp attackComp;
    public List<EffectContainer> effectContainer = new();
}
