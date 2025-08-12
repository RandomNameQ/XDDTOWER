using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Target
{
    public GeneratedEnums.AttitudeId attitude = GeneratedEnums.AttitudeId.Enemy;
    public GeneratedEnums.ModifierId distance = GeneratedEnums.ModifierId.Least;
    public int maxTargets = 1;

    private static readonly List<Creature> Scratch = new();
    private static readonly List<Creature> Candidates = new();

    public virtual IEnumerable<Creature> Select(Creature self)
    {
        Scratch.Clear();
        var candidates = GetCandidates(self, attitude);
        SortCandidatesByDistance(self, candidates);
        TakeUpToMaxTargets(candidates, Scratch);
        return Scratch;
    }

    protected virtual List<Creature> GetCandidates(Creature self, GeneratedEnums.AttitudeId att)
    {
        // Используем реестр существ для эффективности
        Candidates.Clear();
        var all = Creature.All;
        for (int i = 0; i < all.Count; i++)
        {
            var c = all[i];
            if (c == null || c == self) continue;
            bool isEnemy = c.teamNumber != self.teamNumber;
            if ((att == GeneratedEnums.AttitudeId.Enemy && isEnemy) ||
                (att == GeneratedEnums.AttitudeId.Ally && !isEnemy))
            {
                Candidates.Add(c);
            }
        }
        return Candidates;
    }

    private void SortCandidatesByDistance(Creature self, List<Creature> candidates)
    {
        if (distance == GeneratedEnums.ModifierId.Least)
        {
            candidates.Sort((a, b) => CompareByDistanceAsc(self, a, b));
        }
        else if (distance == GeneratedEnums.ModifierId.Most)
        {
            candidates.Sort((a, b) => -CompareByDistanceAsc(self, a, b));
        }
    }

    private static int CompareByDistanceAsc(Creature self, Creature a, Creature b)
    {
        float da = Vector3.Distance(self.transform.position, a.transform.position);
        float db = Vector3.Distance(self.transform.position, b.transform.position);
        return da.CompareTo(db);
    }

    private void TakeUpToMaxTargets(List<Creature> candidates, List<Creature> destination)
    {
        int max = Mathf.Max(1, maxTargets);
        for (int i = 0; i < candidates.Count && i < max; i++)
        {
            destination.Add(candidates[i]);
        }
    }
}
