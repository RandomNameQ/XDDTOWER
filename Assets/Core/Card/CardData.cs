using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
namespace Core.Card
{
    [CreateAssetMenu(fileName = "CardData", menuName = "ScriptableObjects/Card/CardCreature")]
    public partial class CardData : ScriptableObject
    {
        public new string name;
        public string description;
        public string descriptio1n;
        public int sizeX = 1;
        public int sizeZ = 1;

        [HideLabel]
        [PreviewField(Alignment = ObjectFieldAlignment.Left, Height = 128)]
        public Sprite image;
        // [TableList(ShowIndexLabels = true)]
        public List<CardRang> cardRang = new();
        public List<PassiveData> passiveData = new();



        [Serializable]
        public class CardRang
        {

            // от D До SS+ ранга
            // тут храним данные типа статистики существа и ее возможности
            [SerializeReference]
            public List<LogicBase> logic = new();
            // public List<AbilityData> abilityData = new();
            [InlineProperty]
            public OffensiveData offensiveData;
            [InlineProperty]
            public DefensiveData defensiveData;
            [InlineProperty]
            public Enums.RangData rangData;

        }
        [Serializable]
        public class OffensiveData
        {
            public float cooldown;
            public float remainingCooldown;
        }

        [Serializable]
        public class DefensiveData
        {
            public int health;
            public int healthRegen;
            public int dodgeChance;
            public int blockChance;
        }

        [Serializable]
        public class AbilityData
        {
            [SerializeReference]
            public LogicBase logic;
        }
        [Serializable]
        public class PassiveData
        {
            // пассивка, котора меняет поведение или добавляет цифры или еще чего. Должна состоять из обьектов для совместимости
            public AbilityData abilityData;
        }

    }



}