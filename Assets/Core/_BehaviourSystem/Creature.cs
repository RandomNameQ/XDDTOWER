using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Reflection;
using ScrutableObjects;
using Sirenix.OdinInspector;
using System.Linq;
using Core.BoardV2;
using ZLinq;
using LazyHelper;

[SelectionBase]
public class Creature : MonoBehaviour
{
    [ShowProperties]
    public CreatureBehaviorProfileSO behavior;
    public DecideBehavior decideBehavior;
    public BoardTypeV2 board;


    public CreatureLife creatureLife;

    public BehaviorRunner behaviorRunner;
    public Image cooldownBar;
    public Image image;

    public enum TeamNumber
    {
        Zero,
        One,
        Two,
        Three
    }
    public TeamNumber teamNumber;

    public Creature currentTarget;

    public float cooldownTimer;


    private void Awake()
    {
        InitSelfComp();
    }

    public void OnBoardChanged(BoardTypeV2 newBoard)
    {
        board = newBoard;

        if (board == BoardTypeV2.Battle) UnitEvent.OnUnitSpawnedOnBattleBoardEvent(this);
        else UnitEvent.OnUnitRemovedFromBattleBoardEvent(this);

        UpdateRequestState();
    }

    public void InitSelfComp()
    {
        CreatCloneFromBeh();

        decideBehavior = new(this);
        ApplyBoardSizeFromSO();
        InitVisualFromProfile();

        InitCooldownTimer();

        SendDataToEventBehaviour();
    }

    public CreatureBehaviorProfileSO.RangRules FindActiveRang()
    {
        return behavior.rangs.Where(rule => rule.isActiveRang).FirstOrDefault();
    }
    [Button]
    public void SendDataToEventBehaviour()
    {
        var rang = FindActiveRang();
        EventControllerBehaviour.Instance.AddRequest(this, rang.rules);
    }

    public bool isNeedStopRequestStateUpdate;
    [Button]
    public void UpdateRequestState()
    {
        if (isNeedStopRequestStateUpdate) return;
        // надо узнать есть ли в стороне обьекты и если есть, то какой tag - все это надо сравнивать
        foreach (var rule in FindActiveRang().rules)
        {
            foreach (BehaviorRule.Request request in rule.request)
            {
                // выясняем где надо узнать "есть ли сосед"
                // получаем лист сторон где надо узнать
                List<GeneratedEnums.DirectionId> sides = Helper.GetFlagsList(request.side.value);

                int numberCreatureWithSide = 0;
                List<Creature> neighbor = new();
                // получаем обьекты, которые размещены в сторонах
                foreach (GeneratedEnums.DirectionId side in sides)
                {
                    neighbor = behaviorRunner.GetCreaturesInDirection(side);
                    if (neighbor.Count == 0) continue; // соседей с этой стороны нет
                    numberCreatureWithSide++;
                }
                // если количество количество request сторон совпадает с кол-во найденных обьектов из сторон, то значит мы искали 3 стороны и нашли 3 обьекта
                // если ищем 3 стороны, а нашли 2 обьекта = false
                // если ищем 1 сторону, а нашли 2 обьекта, то странненько
                if (numberCreatureWithSide > sides.Count) Debug.Log("странное поведение");

                // и совершаем проверку на race если надо
                if (sides.Count == numberCreatureWithSide)
                {
                    if (Helper.ContainsFlag(GeneratedEnums.TagId.None, request.tag))
                    {

                    }
                }
            }
        }
    }

    private void InitCooldownTimer()
    {
        if (behavior == null || behavior.currentRang == null)
        {
            cooldownTimer = 0f;
            return;
        }
        bool isInstant = behavior.currentRang.isInstantActivation;
        float cooldownSeconds = GetAttackCooldownSeconds();
        cooldownTimer = isInstant ? 0f : cooldownSeconds;
    }

    private void CreatCloneFromBeh()
    {
        var clone = Instantiate(behavior);
        behavior = clone;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Обновляем визуал и размер в редакторе при изменении профиля
        InitVisualFromProfile();
        ApplyBoardSizeFromSO();
    }
#endif




    private void ApplyBoardSizeFromSO()
    {
        Vector2Int size = behavior != null ? behavior.size : Vector2Int.zero;
        if (size == Vector2Int.zero) return;
        var placeable = GetComponent<Core.BoardV2.PlaceableObject>();
        if (placeable != null)
        {
            var sx = Mathf.Max(1, size.x);
            var sz = Mathf.Max(1, size.y);
            placeable.SetSize(sx, sz);
        }
        else
        {
            transform.localScale = new Vector3(size.x, 1, size.y);
        }
    }

    public CreatureBehaviorProfileSO BehaviorProfile => behavior;



    private void InitVisualFromProfile()
    {
        // Пытаемся найти UI Image автоматически, если не назначен в инспекторе
        if (image == null)
        {
            image = GetComponentInChildren<Image>(true);
        }

        var sprite = behavior != null ? behavior.image : null;
        if (sprite == null) return;

        // Применяем к UI-изображению, если оно есть
        if (image != null)
        {
            image.sprite = sprite;
        }

        // Применяем к SpriteRenderer (для 2D/мировых объектов)
        var sr = GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null)
        {
            sr.sprite = sprite;
        }

        // Если нет ни UI Image, ни SpriteRenderer — оставляем как есть
    }




    [Serializable]
    public class DecideBehavior
    {
        public Creature creature;

        public DecideBehavior(Creature creature)
        {
            this.creature = creature;
        }

        public void ApplyEffect(Creature sources)
        {
            // var effect = sources.behavior.currentRang.rules[0].effect;
            // creature.creatureLife.HandleEffect(effect, sources);
        }


    }

    private void Update()
    {
        if (behavior == null || behavior.currentRang == null) return;
        if (behavior.currentRang.isPasive) return;
        if (creatureLife != null && creatureLife.isDead) return;
        UpdateCombatLoop();
    }

    private void UpdateCombatLoop()
    {
        if (!behavior.currentRang.isPasive)
        {
            AttackCooldownTick();
            UpdateCooldownUI();
        }
    }

    private void UpdateCooldownUI()
    {
        if (cooldownBar == null) return;

        float cooldownSeconds = GetAttackCooldownSeconds();
        if (cooldownSeconds <= 0f)
        {
            cooldownBar.fillAmount = 0f;
            return;
        }

        float fillAmount = cooldownTimer / cooldownSeconds;
        cooldownBar.fillAmount = Mathf.Clamp01(fillAmount);
    }

    private void AttackCooldownTick()
    {
        if (behavior == null || behavior.currentRang == null) return;
        if (creatureLife != null && creatureLife.isDead) return;

        float cooldownSeconds = GetAttackCooldownSeconds();
        if (cooldownSeconds <= 0f)
        {
            cooldownTimer = 0f;
            return;
        }

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        PrepareUseEffect();

    }

    [Button]
    public void PrepareUseEffect(Creature target = null)
    {
        // if (creatureLife != null && creatureLife.isDead) return;

        // var resolvedTarget = target ?? FindTarget();
        // if (resolvedTarget == null)
        // {
        //     return;
        // }

        // currentTarget = resolvedTarget;
        // SpawnProjectile(resolvedTarget);
        // cooldownTimer = GetAttackCooldownSeconds();
    }

    private float GetAttackCooldownSeconds()
    {
        var offence = behavior.currentRang.offence;
        float value = offence != null ? offence.cooldown.baseValue : 0f;
        if (value <= 0f) value = 1f;
        return value;
    }

    private void SpawnProjectile(Creature target)
    {
        if (behavior == null || behavior.spellPrefab == null) return;
        if (creatureLife != null && creatureLife.isDead) return;

        var projGo = Instantiate(behavior.spellPrefab, transform.position, Quaternion.identity);
        var projectile = projGo.GetComponent<ProjectileBase>();
        if (projectile == null)
        {
            projectile = projGo.AddComponent<ProjectileBase>();
        }
        projectile.Init(target.gameObject, this, target);
    }

    public void OnDeath(Creature source = null)
    {
        if (behaviorRunner != null)
        {
            behaviorRunner.enabled = false;
        }

        if (decideBehavior != null)
        {
            decideBehavior = null;
        }

        currentTarget = null;
        cooldownTimer = 0f;
    }
    [Button]
    public void ChargeCooldown(float time)
    {
        float cooldownSeconds = GetAttackCooldownSeconds();
        if (cooldownSeconds <= 0f) return;

        cooldownTimer = Mathf.Max(0f, cooldownTimer - time);

        if (cooldownTimer <= 0f)
        {
            PrepareUseEffect();
            cooldownTimer = cooldownSeconds - time;
        }

        UpdateCooldownUI();
    }

}
