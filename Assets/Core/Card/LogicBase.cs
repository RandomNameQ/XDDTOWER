using Core.Data;
using System;

namespace Core.Card

{
    [Serializable]
    public abstract class LogicBase
    {
        [Serializable]
        public class ContidionExecution
        {
            public bool isCooldownCondition;
            public Enums.Target target;
            public Enums.TargetModifier targetModifier;
            public Enums.Team team;
            public Enums.RangData rangData;
            public Enums.Creature creature;
            public Enums.Position position;
        }
        public ContidionExecution contidionExecution;
    }


}