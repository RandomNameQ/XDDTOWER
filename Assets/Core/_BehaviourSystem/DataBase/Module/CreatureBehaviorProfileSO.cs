using System;
using System.Collections.Generic;
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
        [SerializeReference]
        public List<BehaviorRule> rules = new();
        public int maxHealth = 100;
    }

    public List<RangRules> rangs = new();
}


