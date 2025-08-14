using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class BehaviorRule
{

    [HideInInspector]
    public Creature client;

    [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, ShowIndexLabels = true)]
    public List<Trigger> Triggers = new();

    public Target Target;


    [SerializeReference]
    public GeneratedEnums.EffectId effect;
    public GeneratedEnums.StatsId statistic;
    public Value value;

    public List<Action> eventsChain = new();


    public void Initialize(Creature ownerCreature)
    {
        client = ownerCreature;
        if (Triggers != null)
        {
            foreach (var t in Triggers)
            {
                t?.Initialize(this);
            }
        }
    }

}
