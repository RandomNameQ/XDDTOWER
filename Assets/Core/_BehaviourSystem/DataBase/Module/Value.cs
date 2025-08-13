using System;
using UnityEngine;

[Serializable]
public class Value
{
    [Serializable]
    public class Number
    {
        public int value;

    }
    [Serializable]

    public class Percent
    {
        public int percent;
        public Target target;
        public Effect effect;
        public Statistic statistic;

    }
    [Serializable]

    public class Random
    {
        public Vector2Int random;
    }
    public Number number;
    public Percent percent;
    public Random random;

}
