using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CreatureSO", menuName = "CreatureSO", order = 0)]
public class CreatureSO : ScriptableObject
{
    public Sprite image;
    public Vector2Int size;

    public enum Rangs
    {
        S,
        A,
        B, C
    }

    [Serializable]
    public class Rang
    {
        // Храним ссылки на ScriptableObject, чтобы слой Core не зависел от BehaviourSystem.
        public List<ScriptableObject> rules;
        public int maxHealth = 100;
    }

    public List<Rang> rangs = new();
    public GameObject spellPrefab;
}


