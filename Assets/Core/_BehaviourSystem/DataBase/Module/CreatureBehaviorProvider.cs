using System.Collections.Generic;
using UnityEngine;

public class CreatureBehaviorProvider : MonoBehaviour, ICreatureBehaviorProvider
{
    [SerializeField] private CreatureBehaviorDatabaseSO database;

    public List<BehaviorRule> GetRules(CreatureBehaviorProfileSO profile, int rangIndex)
    {
        if (profile == null) return null;
        if (profile == null || profile.rangs == null || profile.rangs.Count == 0) return null;
        int idx = Mathf.Clamp(rangIndex, 0, profile.rangs.Count - 1);
        return profile.rangs[idx].rules;
    }
}


