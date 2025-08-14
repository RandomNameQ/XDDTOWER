using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

[CreateAssetMenu(fileName = "CreatureBehaviorProfile", menuName = "Behaviour/Creature Behavior Profile", order = 20)]
public class CreatureBehaviorProfileSO : RegistryItemSO
{
    // Полный отказ от CreatureSO: профиль самодостаточен
    public Sprite image;
    public Vector2Int size;
    public List<GeneratedEnums.RaceId> races = new();
    public GameObject spellPrefab;
    public new string name;


    [Serializable]
    public class RangRules
    {
        public List<BehaviorRule> rules = new();
        public Deffence deffence;
        public Deffence offence;
        public bool isInstantActivation;
        public bool isActiveRang;

        public void OnEnable()
        {
            Debug.Log(11);
        }
    }
    public void OnEnable()
    {
        ActiveRang();
    } 
    [Button]
    public void ActiveRang()
    {
        rangs.ForEach(rang => rang.isActiveRang = false);
        rangs[0].isActiveRang = true;
    }
    [Serializable]
    public class Data
    {
        public GeneratedEnums.StatsId stat;
        public float baseValue;
    }

    [Serializable]
    public class Offence
    {
        public Data criticalChance = new Data { stat = GeneratedEnums.StatsId.MaxHealth, baseValue = 0f };
        public Data criticalDamage = new Data { stat = GeneratedEnums.StatsId.MaxHealth, baseValue = 1.5f };
        public Data cooldown = new Data { stat = GeneratedEnums.StatsId.MaxHealth, baseValue = 3f };
    }
    [Serializable]
    public class Deffence
    {

        public Data maxHealh = new Data { stat = GeneratedEnums.StatsId.MaxHealth, baseValue = 100f };
        public Data regeneration = new Data { stat = GeneratedEnums.StatsId.Regeneration, baseValue = 0f };
        public Data blockChance = new Data { stat = GeneratedEnums.StatsId.BlockChance, baseValue = 0f };
        public Data dodgeChance = new Data { stat = GeneratedEnums.StatsId.DodgeChance, baseValue = 0f };

    }





    public List<RangRules> rangs = new();
}


