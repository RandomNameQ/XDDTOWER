using System;
using UnityEngine;

[Serializable]

public class Statistic
{
    public StatsSO stat;
    public enum Operation
    {
        Add,
        Remove
    }
    public Operation operation;

}
