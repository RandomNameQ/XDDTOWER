using Core.Data;
using System;

namespace Core.Card
{
    [Serializable]
    public class OnApplyEffect : LogicBase
    {
        public Enums.Effects effect;
        public int count;
        public Enums.Target target;
        public Enums.TargetModifier targetModifier;
    }
}