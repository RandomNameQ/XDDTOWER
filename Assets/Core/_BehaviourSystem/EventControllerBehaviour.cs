using System;
using System.Collections.Generic;
using System.Linq;
using GeneratedEnums;
using LazyHelper;
using PlasticPipe.PlasticProtocol.Messages;
using Sirenix.OdinInspector;
using UnityEditor.PackageManager;
using UnityEngine;

public class EventControllerBehaviour : Singleton<EventControllerBehaviour>
{
    // сюда приходят запросы от карт "что им надо для срабатывания"
    // сюда прихрдят инвенты от всей обьектов

    // этот обьект проверяет "сработал ли чей-то запрос" и отправляет ответ

    // как хранить запросы?


    [Serializable]
    public class RequestBehaviour
    {
        public Creature client;
        public List<BehaviorRule> behaviour = new();
    }

    public class Response
    {
        public Creature destiny;
        public Creature source;
        public EffectId effect;
        public OperatinoId operation;
    }
    [Serializable]

    public class StoredEnums
    {
        public AttitudeId attitude;
        public OperatinoId operation;
        public TagId tag;
        public EffectId effect;
        public DirectionId position;
    }
    // public StoredEnums andEnums;
    // public StoredEnums orEnums;

    public List<RequestBehaviour> requests = new();
    public List<Response> responses = new();
    public void AddRequest(Creature client, List<BehaviorRule> rule)
    {
        foreach (var req in requests)
        {
            var c = req.client;
            if (c == client)
            {
                return;
            }
        }

        requests.Add(new RequestBehaviour()
        {
            client = client,
            behaviour = rule
        });
    }


    public void OnEnable()
    {
        UnitEvent.OnUnitAppliedEffect += UnitApplyEffect;
        UnitEvent.OnUnitReceiveEffect += UnitRecieveEffect;
        UnitEvent.OnUnitDied += UnitDie;
    }

    public void OnDisable()
    {
        UnitEvent.OnUnitAppliedEffect -= UnitApplyEffect;
        UnitEvent.OnUnitReceiveEffect -= UnitRecieveEffect;
        UnitEvent.OnUnitDied -= UnitDie;
    }

    public void UnitRecieveEffect(EffectId effect, Creature destiny, Creature source)
    {
        Response responce = null;
        responce.operation = OperatinoId.ReceiveEffect;
        responce.destiny = destiny;
        responce.effect = effect;
        responce.source = source;

        responses.Add(responce);

        Check();
    }

    public void UnitApplyEffect(EffectId effect, Creature destiny, Creature source)
    {
        Response responce = null;
        responce.operation = OperatinoId.ApplyEffect;
        responce.destiny = destiny;
        responce.effect = effect;
        responce.source = source;

        responses.Add(responce);

        Check();

    }

    public void UnitDie(Creature destiny, Creature source)
    {
        Response responce = null;
        responce.operation = OperatinoId.Died;
        responce.destiny = destiny;
        responce.source = source;

        responses.Add(responce);

        Check();
    }

    public void AddResponce()
    {

    }
    [Button]
    public void Check()
    {
        // у меня есть запросы и ответы. мне надо узнать 

        foreach (RequestBehaviour request in requests)
        {
            foreach (BehaviorRule behaviour in request.behaviour)
            {
                foreach (var req in behaviour.request)
                {
                    FullFillEnums(behaviour.request);
                    RequestVerification(request.client, req);
                }
            }
        }

        AddResponce();
        ClearData();
    }

    public bool RequestVerification(Creature client, BehaviorRule.Request request)
    {
        // все енамы во флаге это AND
        // все реквесты это OR
        foreach (Response response in responses)
        {
            // если false то значит например request damage, а в responce heal
            if (!CheckEffectAndOperation(request, response)) continue;

            // дальше нам необхожимо найти цель, которая описана в request
            // это требуется, чтобы найти race, attitude и прочие
            // чтобы найти цель требуется сначала узенать attitude и operation
            // attitude указывает на то искать цель у врагов или союзников
            // operation указывает на то что будет является целью soruce или destiny


            Creature target = FindTarget(client, response, request);
            if (target == null) return false;

            if (!target.behavior.tag.HasFlag(request.tag.value)) return false;

        }

        return true;
    }




    public Creature FindTarget(Creature client, Response response, BehaviorRule.Request request)
    {
        var clientTeam = client.teamNumber;
        var destintyTeam = response.destiny.teamNumber;
        var sourceTeam = response.source.teamNumber;

        // для союзников нужно еще сделать проверку на позицию слева справ и етк

        List<Creature> targets = new();

        if (request.attitude.value.ContainsFlag(AttitudeId.Enemy) && request.operation.value.ContainsFlag(OperatinoId.ApplyEffect))
            if (sourceTeam != clientTeam) return response.source;

        if (request.attitude.value.ContainsFlag(AttitudeId.Enemy) && request.operation.value.ContainsFlag(OperatinoId.ReceiveEffect))
            if (destintyTeam != clientTeam) return response.destiny;


        if (request.attitude.value.ContainsFlag(AttitudeId.Ally) && request.operation.value.ContainsFlag(OperatinoId.ApplyEffect))
            if (sourceTeam == clientTeam && !IsHaveTargetInSide(client, request, out targets, response.source))
                if (targets.Contains(response.source) && Helper.ContainsFlag(response.source.behavior.tag, request.tag.value)) return response.source;


        if (request.attitude.value.ContainsFlag(AttitudeId.Ally) && request.operation.value.ContainsFlag(OperatinoId.ReceiveEffect))
            if (destintyTeam == clientTeam && !IsHaveTargetInSide(client, request, out targets, response.destiny))
                if (targets.Contains(response.destiny) && Helper.ContainsFlag(response.destiny.behavior.tag, request.tag.value)) return response.destiny;

        return null;
    }

    private bool IsHaveTargetInSide(Creature client, BehaviorRule.Request request, out List<Creature> found, Creature target)
    {
        found = new List<Creature>();
        if (client == null || client.behaviorRunner == null)
            return false;

        var targets = client.behaviorRunner.TryGetCreaturesInDirections(request.position.value, out found);

        return targets;
    }


    public bool CheckEffectAndOperation(BehaviorRule.Request request, Response response)
    {
        if (!Helper.ContainsAllFlags(request.effect.value, response.effect))
            return false;

        if (!Helper.ContainsAllFlags(request.operation.value, response.operation))
            return false;

        // если выбрано 1+ енам, то значит истинно, если все енамы совпали
        // if (Helper.GetActiveFlagsCount(request.effect) > 1)
        // {

        // }
        // else
        // {
        //     if (!Helper.ContainsMyFlags(request.effect, response.effect))
        //         return false;
        // }


        // if (Helper.GetActiveFlagsCount(request.operation) > 1)
        // {

        // }
        // else
        // {
        //     if (!Helper.ContainsMyFlags(request.operation, response.operation))
        //         return false;
        // }

        return true;
    }
    // public bool CheckOrEnums(Response response)
    // {
    //     if (!Helper.HasAnyFlag(response.effect, orEnums.effect)) return false;
    //     if (!Helper.HasAnyFlag(response.operation, orEnums.operation)) return false;


    //     return true;
    // }
    // public bool CheckAndEnums(Response response)
    // {
    //     if (!Helper.HasAllFlags(response.effect, andEnums.effect)) return false;
    //     if (!Helper.HasAllFlags(response.operation, andEnums.operation)) return false;


    //     return true;
    // }

    public void FullFillEnums(List<BehaviorRule.Request> allRulesRequests)
    {
        // // Все записи сгруппированные по ID (одинаковые ID вместе)
        // List<BehaviorRule.Request> andReq = allRulesRequests
        //     .GroupBy(rule => rule.id)
        //     .Where(group => group.Count() > 1) // только группы с одинаковыми ID
        //     .SelectMany(group => group)
        //     .ToList();

        // // Все записи с уникальными ID (не повторяющиеся)
        // List<BehaviorRule.Request> orReq = allRulesRequests
        //     .GroupBy(rule => rule.id)
        //     .Where(group => group.Count() == 1) // только уникальные ID
        //     .SelectMany(group => group)
        //     .ToList();

        // Debug.Log(orReq.Count);
        // andEnums = new StoredEnums();
        // orEnums = new StoredEnums();

        // foreach (var r in andReq)
        // {
        //     andEnums.attitude |= r.attitude;
        //     andEnums.operation |= r.operation;
        //     andEnums.tag |= r.tag;
        //     andEnums.effect |= r.effect;
        //     andEnums.position |= r.position;
        // }

        // foreach (var r in orReq)
        // {
        //     orEnums.attitude |= r.attitude;
        //     orEnums.operation |= r.operation;
        //     orEnums.tag |= r.tag;
        //     orEnums.effect |= r.effect;
        //     orEnums.position |= r.position;
        // }
    }


    public void AddEffectInContainer()
    {

    }
    public void ClearData()
    {
        responses.Clear();
    }
    // через foreach опрашиваем клиентов и смотрим что им надо
    // через foreach опрашиваем respcone и видим что пришло
    // задача найти данные для клиента
}
