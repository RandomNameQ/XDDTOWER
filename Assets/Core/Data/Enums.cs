using System;
using Codice.Utils;
using UnityEngine;

public class Enums
{
    [Serializable]
    public enum RangData
    {
        None,
        E,
        D,
        C,
        B,
        A,
        S,
        SS

    }
    [Serializable]
    public enum Team
    {
        None,
        Me,
        Enemy,
        Ally,
        Neutral
    }

    [Serializable]
    public enum Creature
    {
        None,
        Slime,
        Goblin,
        Dragon,
        Dog
    }

    [Serializable]
    public enum Position
    {
        None,
        Front,
        Back,
        Left,
        Right
    }

    public enum Target
    {
        None,
        Self,
        Enemy,
        Ally
    }
    public enum TargetModifier
    {
        None,
        Farest,
        Nearest,
        Random,
        Strongest,
        Weakest,
        LowestHealth,
        HighestHealth,
        LowestMana,
        HighestCooldown,

        LowestCooldown,

    }


    public enum Effects
    {
        None,
        Damage,
        Armour,
        Heal,
        Stun,
        Slow,
        Burn,
        Freeze,
    }
}
