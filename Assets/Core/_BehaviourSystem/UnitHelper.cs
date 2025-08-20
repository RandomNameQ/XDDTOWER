using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZLinq;

public class UnitHelper : Singleton<UnitHelper>
{

    public List<Creature> storedUnits = new();

    private void OnEnable()
    {
        UnitEvent.OnUnitSpawnedOnBattlleBoard += AddUnit;
        UnitEvent.OnUnitRemovedFromBattleBoard += RemoveUnit;
    }
    private void OnDisable()
    {
        UnitEvent.OnUnitSpawnedOnBattlleBoard -= AddUnit;
        UnitEvent.OnUnitRemovedFromBattleBoard -= RemoveUnit;
    }

    public void AddUnit(Creature unit)
    {
        if (storedUnits.Contains(unit))
        {
            Debug.Log("юнит есть");
        }
        else
        {
            Debug.Log("add unit");
            storedUnits.Add(unit);
        }
    }
    public void RemoveUnit(Creature unit)
    {
        if (storedUnits.Contains(unit))
        {
            storedUnits.Remove(unit);
            Debug.Log("remove unit");
        }
        else
        {
            Debug.Log("не могу убрать юнита");
        }
    }
    public List<Creature> GetAlly(Creature.TeamNumber teamNumber, bool incudeCreature = false)
    {
        return storedUnits.AsEnumerable().Where(unit => unit.teamNumber == teamNumber).ToList();
    }
    public List<Creature> GetEnemy(Creature.TeamNumber teamNumber, bool incudeCreature = false)
    {
        return storedUnits.AsEnumerable().Where(unit => unit.teamNumber != teamNumber).ToList();
    }
    public Creature GetEnemy(Creature.TeamNumber teamNumber)
    {
        return storedUnits.AsEnumerable().Where(unit => unit.teamNumber != teamNumber).FirstOrDefault();
    }

    public List<Creature> GetUnits(Creature client = null, GeneratedEnums.AttitudeId attitude = GeneratedEnums.AttitudeId.None, GeneratedEnums.TagId tag = GeneratedEnums.TagId.None, bool needCloseDintance = true, bool incudeCaller = false)
    {
        List<Creature> units = new();
        var team = client.teamNumber;

        if ((attitude & GeneratedEnums.AttitudeId.Any) != 0) units.AddRange(storedUnits);
        if ((attitude & GeneratedEnums.AttitudeId.Ally) != 0) units.AddRange(storedUnits.AsValueEnumerable().Where(unit => unit.teamNumber == team).ToList());
        if ((attitude & GeneratedEnums.AttitudeId.Enemy) != 0) units.AddRange(storedUnits.AsValueEnumerable().Where(unit => unit.teamNumber != team).ToList());

        // if ((attitude & GeneratedEnums.AttitudeId.Me) != 0 && client != null) units.Add(client);

        // if (!incudeCaller && client != null) units.RemoveAll(u => u == client);

        // units = units.Distinct().ToList();

        if (client != null && units.Count > 0)
        {
            var clientPosition = client.transform.position;
            if (needCloseDintance)
                units = units.OrderBy(u => (u.transform.position - clientPosition).sqrMagnitude).ToList();
            else
                units = units.OrderByDescending(u => (u.transform.position - clientPosition).sqrMagnitude).ToList();
        }

        return units;
    }
    public void GetUnit()
    {

    }
}

