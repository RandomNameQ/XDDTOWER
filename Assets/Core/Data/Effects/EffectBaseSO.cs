using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Data
{
    [CreateAssetMenu(fileName = "New Effect", menuName = "ScriptableObjects/EffectBase")]
    [Serializable]
    public class EffectBaseSO : ScriptableObject
    {
        // тут сделай напиши зачем нужен скрипт и что он делает, чтобы помнить и праймиться
        public Enums.Effects enumEffect;
        private string saveEnumName;


        [Button]
        public void ChangeNameFromEnum()
        {
            ScriptableUtility.ChangeNameFromEnum(this, enumEffect);
        }

        [Button]
        public void ConvertStringToEnum()
        {
            enumEffect = ScriptableUtility.ConvertStringToEnum<Enums.Effects>(saveEnumName);
        }

        [Button]
        public void ConvertNameToEnum()
        {
            enumEffect = ScriptableUtility.ConvertStringToEnum<Enums.Effects>(name);
        }
    }
}
