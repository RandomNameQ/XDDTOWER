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
    public StoredEnums andEnums;
    public StoredEnums orEnums;

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

    public void RequestVerification(Creature client, BehaviorRule.Request request)
    {
        foreach (Response responce in responses)
        {

            if (CheckOrEnums(responce) || CheckAndEnums(responce))
            {
                FindTarget(client, responce);
            }
        }

        // у нас есить клиент и вперую очередь нам необходимо найти обьекты, которые соответствуют position, attitude
    }

    public Creature FindTarget(Creature client, Response response)
    {
        var team = client.teamNumber;


        Creature target = null;

        // if (andEnums.attitude.HasFlag(AttitudeId.Ally))
        // {
        // }
        // допустим 5 врагов выстрелило с эффектом и операуцией
        // теперь надо отсортировать по остальным условиям

        if (orEnums.operation.HasFlag(OperatinoId.ApplyEffect))
        {
            target = response.source;
        }
        if (andEnums.operation.HasFlag(OperatinoId.ApplyEffect))
        {

        }
        if (andEnums.operation.HasFlag(OperatinoId.ReceiveEffect))
        {
            target = response.destiny;
        }
        if (andEnums.attitude.HasFlag(AttitudeId.Enemy))
        {
            target = response.source;
        }

        return target;
    }

    public bool CheckOrEnums(Response response)
    {
        if (!Helper.HasAnyFlag(response.effect, orEnums.effect)) return false;
        if (!Helper.HasAnyFlag(response.operation, orEnums.operation)) return false;


        return true;
    }
    public bool CheckAndEnums(Response response)
    {
        if (!Helper.HasAllFlags(response.effect, andEnums.effect)) return false;
        if (!Helper.HasAllFlags(response.operation, andEnums.operation)) return false;


        return true;
    }

    public void FullFillEnums(List<BehaviorRule.Request> allRulesRequests)
    {
        // Все записи сгруппированные по ID (одинаковые ID вместе)
        List<BehaviorRule.Request> andReq = allRulesRequests
            .GroupBy(rule => rule.id)
            .Where(group => group.Count() > 1) // только группы с одинаковыми ID
            .SelectMany(group => group)
            .ToList();

        // Все записи с уникальными ID (не повторяющиеся)
        List<BehaviorRule.Request> orReq = allRulesRequests
            .GroupBy(rule => rule.id)
            .Where(group => group.Count() == 1) // только уникальные ID
            .SelectMany(group => group)
            .ToList();

        Debug.Log(orReq.Count);
        andEnums = new StoredEnums();
        orEnums = new StoredEnums();

        foreach (var r in andReq)
        {
            andEnums.attitude |= r.attitude;
            andEnums.operation |= r.operation;
            andEnums.tag |= r.tag;
            andEnums.effect |= r.effect;
            andEnums.position |= r.position;
        }

        foreach (var r in orReq)
        {
            orEnums.attitude |= r.attitude;
            orEnums.operation |= r.operation;
            orEnums.tag |= r.tag;
            orEnums.effect |= r.effect;
            orEnums.position |= r.position;
        }
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
