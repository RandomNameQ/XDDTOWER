using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class BehaviorRule
{

    [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = false, DraggableItems = true, ShowIndexLabels = true)]
    [SerializeReference]
    public List<Trigger> Triggers = new();

    [SerializeReference]
    public List<Condition> Conditions = new();

    public Target Target;
    [SerializeReference]


    public EffectSO effect;
    public Statistic statistic;
}
