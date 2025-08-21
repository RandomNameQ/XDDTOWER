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
        
        // Проверяем все правила поведения для активного ранга
        foreach (var rule in FindActiveRang().rules)
        {
            foreach (BehaviorRule.Request request in rule.request)
            {
                // Проверяем условия для активации правила
                if (CheckRequestConditions(request))
                {
                    // Применяем эффект, если все условия выполнены
                    ApplyRuleEffect(rule, request);
                }
            }
        }
    }

    /// <summary>
    /// Проверяет все условия Request для активации правила
    /// </summary>
    private bool CheckRequestConditions(BehaviorRule.Request request)
    {
        // Проверяем количество соседей по требуемым сторонам
        if (!CheckNeighborCount(request)) return false;
        
        // Проверяем отношение к соседям (враг/союзник)
        if (!CheckAttitudeCondition(request)) return false;
        
        // Проверяем требуемые теги
        if (!CheckTagConditions(request)) return false;
        
        // Проверяем требуемые операции
        if (!CheckOperationCondition(request)) return false;
        
        return true;
    }

    /// <summary>
    /// Проверяет количество соседей по требуемым сторонам
    /// </summary>
    private bool CheckNeighborCount(BehaviorRule.Request request)
    {
        // Получаем список сторон, где нужно проверить наличие соседей
        List<GeneratedEnums.DirectionId> sides = Helper.GetFlagsList(request.side.value);
        
        int numberCreatureWithSide = 0;
        List<Creature> neighbor = new();
        
        // Подсчитываем количество сторон с соседями
        foreach (GeneratedEnums.DirectionId side in sides)
        {
            neighbor = behaviorRunner.GetCreaturesInDirection(side);
            if (neighbor.Count == 0) continue; // Соседей с этой стороны нет
            numberCreatureWithSide++;
        }
        
        // Проверяем корректность: количество найденных соседей не должно превышать количество требуемых сторон
        if (numberCreatureWithSide > sides.Count) 
        {
            Debug.LogWarning($"Странное поведение: найдено {numberCreatureWithSide} соседей для {sides.Count} требуемых сторон");
            return false;
        }
        
        // Условие выполнено, если количество сторон с соседями совпадает с требуемым
        return sides.Count == numberCreatureWithSide;
    }

    /// <summary>
    /// Проверяет условие отношения к соседям (враг/союзник)
    /// </summary>
    private bool CheckAttitudeCondition(BehaviorRule.Request request)
    {
        if (!request.attitude.isActivated) return true; // Условие не активировано
        
        // Получаем всех соседей по всем направлениям
        var allNeighbors = GetAllNeighbors();
        
        foreach (var neighbor in allNeighbors)
        {
            // Проверяем отношение к каждому соседу
            if (!CheckAttitudeToCreature(neighbor, request.attitude.value))
            {
                return false;
            }
        }
        
        return true;
    }

    /// <summary>
    /// Проверяет теговые условия
    /// </summary>
    private bool CheckTagConditions(BehaviorRule.Request request)
    {
        // Проверяем тег самого существа
        if (request.tag.isActivated)
        {
            if (!Helper.ContainsFlag(request.tag.value, behavior.tag))
            {
                return false;
            }
        }
        
        // Проверяем теги целей
        if (request.tagTarget.isActivated)
        {
            var allNeighbors = GetAllNeighbors();
            bool hasValidTarget = false;
            
            foreach (var neighbor in allNeighbors)
            {
                if (Helper.ContainsFlag(request.tagTarget.value, neighbor.behavior.tag))
                {
                    hasValidTarget = true;
                    break;
                }
            }
            
            if (!hasValidTarget) return false;
        }
        
        return true;
    }

    /// <summary>
    /// Проверяет условие операции
    /// </summary>
    private bool CheckOperationCondition(BehaviorRule.Request request)
    {
        if (!request.operation.isActivated) return true; // Условие не активировано
        
        // Здесь можно добавить логику проверки операций
        // Например, проверка состояния существа, эффектов и т.д.
        
        return true;
    }

    /// <summary>
    /// Получает всех соседей по всем направлениям
    /// </summary>
    private List<Creature> GetAllNeighbors()
    {
        var allNeighbors = new List<Creature>();
        var directions = System.Enum.GetValues(typeof(GeneratedEnums.DirectionId));
        
        foreach (GeneratedEnums.DirectionId direction in directions)
        {
            var neighbors = behaviorRunner.GetCreaturesInDirection(direction);
            allNeighbors.AddRange(neighbors);
        }
        
        return allNeighbors;
    }

    /// <summary>
    /// Проверяет отношение к конкретному существу
    /// </summary>
    private bool CheckAttitudeToCreature(Creature target, GeneratedEnums.AttitudeId requiredAttitude)
    {
        if (target == null) return false;
        
        switch (requiredAttitude)
        {
            case GeneratedEnums.AttitudeId.Enemy:
                return target.teamNumber != this.teamNumber;
            case GeneratedEnums.AttitudeId.Ally:
                return target.teamNumber == this.teamNumber;
            case GeneratedEnums.AttitudeId.Me:
                return target == this;
            case GeneratedEnums.AttitudeId.Any:
                return true;
            case GeneratedEnums.AttitudeId.None:
                return true;
            default:
                return true;
        }
    }

    /// <summary>
    /// Применяет эффект правила, если все условия выполнены
    /// </summary>
    private void ApplyRuleEffect(BehaviorRule rule, BehaviorRule.Request request)
    {
        // Проверяем, есть ли компонент атаки для применения эффекта
        if (rule.attackComp != null)
        {
            // Применяем эффект через существующую систему
            if (request.attackComp.isActivated && request.attackComp.value != null)
            {
                ApplyAttackEffect(request.attackComp.value);
            }
            else
            {
                ApplyAttackEffect(rule.attackComp);
            }
        }
    }

    /// <summary>
    /// Применяет эффект атаки к целям
    /// </summary>
    private void ApplyAttackEffect(BehaviorRule.AttackComp attackComp)
    {
        // Находим цели согласно настройкам
        var targets = FindTargetsForAttack(attackComp.target);
        
        foreach (var target in targets)
        {
            if (target != null && target.creatureLife != null)
            {
                // Применяем эффект к цели
                target.creatureLife.HandleEffect(attackComp.effect, this);
            }
        }
    }

    /// <summary>
    /// Находит цели для атаки согласно настройкам
    /// </summary>
    private List<Creature> FindTargetsForAttack(BehaviorRule.AttackComp.Target targetSettings)
    {
        var targets = new List<Creature>();
        var allNeighbors = GetAllNeighbors();
        
        // Фильтруем по отношению
        var filteredTargets = allNeighbors.Where(neighbor => 
            CheckAttitudeToCreature(neighbor, targetSettings.attitude)).ToList();
        
        // Фильтруем по тегу
        if (targetSettings.tag != GeneratedEnums.TagId.None)
        {
            filteredTargets = filteredTargets.Where(neighbor => 
                Helper.ContainsFlag(targetSettings.tag, neighbor.behavior.tag)).ToList();
        }
        
        // Применяем приоритет выбора
        filteredTargets = ApplyTargetPriority(filteredTargets, targetSettings.priority);
        
        // Ограничиваем количество целей
        int targetCount = Mathf.Min(targetSettings.countTarget, filteredTargets.Count);
        targets.AddRange(filteredTargets.Take(targetCount));
        
        return targets;
    }

    /// <summary>
    /// Применяет приоритет выбора целей
    /// </summary>
    private List<Creature> ApplyTargetPriority(List<Creature> targets, BehaviorRule.AttackComp.Target.Priority priority)
    {
        if (targets.Count <= 1) return targets;
        
        switch (priority)
        {
            case BehaviorRule.AttackComp.Target.Priority.ClosePosition:
                return targets.OrderBy(t => Vector3.Distance(transform.position, t.transform.position)).ToList();
            case BehaviorRule.AttackComp.Target.Priority.FarPosition:
                return targets.OrderByDescending(t => Vector3.Distance(transform.position, t.transform.position)).ToList();
            case BehaviorRule.AttackComp.Target.Priority.MinimumHealth:
                return targets.OrderBy(t => t.creatureLife?.currentLife ?? 0).ToList();
            case BehaviorRule.AttackComp.Target.Priority.MaximumHealth:
                return targets.OrderByDescending(t => t.creatureLife?.currentLife ?? 0).ToList();
            default:
                return targets;
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
