using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Reflection;
using ScrutableObjects;
using Sirenix.OdinInspector;
using System.Linq;

/// <summary>
/// Носитель поведения на сцене. Хранит базовые данные юнита и реестр всех существ.
/// </summary>
public class Creature : MonoBehaviour, ICreatureComponent, IInitFromSO
{
    public static readonly List<Creature> All = new();
    [ShowProperties]
    public CreatureBehaviorProfileSO behaviorProfile;
    ScriptableObject ICreatureComponent.CreatureData { get => behaviorProfile; set => behaviorProfile = value as CreatureBehaviorProfileSO; }
    public Image image;
    public DecideBehavior decideBehavior;

    [HideInInspector]
    public CreatureLife creatureLife;

    public BehaviorRunner behaviorRunner;
    public Image cooldownPanel;
    public enum TeamNumber
    {
        Zero,
        One,
        Two,
        Three
    }
    public TeamNumber teamNumber;
    public int currentHealth;

    [Header("Debug/Runtime")]
    [Tooltip("Текущая выбранная цель (обновляется при каждом выстреле)")]
    public Creature currentTarget;

    public float cooldownTimer;

    private void Awake()
    {
        EnsureBehaviorRunnerAttached();
        creatureLife = GetComponent<CreatureLife>();
    }

    private void Start()
    {
        decideBehavior = new(this);
        InitCooldownTimer();

    }

    private void InitCooldownTimer()
    {
        if (behaviorProfile == null || behaviorProfile.currentRang == null)
        {
            cooldownTimer = 0f;
            return;
        }
        bool isInstant = behaviorProfile.currentRang.isInstantActivation;
        float cooldownSeconds = GetAttackCooldownSeconds();
        cooldownTimer = isInstant ? 0f : cooldownSeconds;
    }

    private void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
        // Автоматическая инициализация из профиля: визуал, статы, размер
        InitVisualFromProfile();
        InitStatsFromProfile();
        ApplyBoardSizeFromSO();
        BuildRuntimeRulesFromProfile();


    }

    private void OnDisable()
    {
        All.Remove(this);
        UnsubscribeAllRuntimeRules();
    }
    [Button]
    public void InitDataSO()
    {
        InitVisualFromProfile();
        InitStatsFromProfile();
        ApplyBoardSizeFromSO();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Обновляем визуал и размер в редакторе при изменении профиля
        InitVisualFromProfile();
        ApplyBoardSizeFromSO();
    }
#endif


    private void EnsureBehaviorRunnerAttached()
    {
        if (GetComponent<BehaviorRunner>() == null)
        {
            behaviorRunner = gameObject.AddComponent<BehaviorRunner>();

        }
    }

    private void ApplyBoardSizeFromSO()
    {
        Vector2Int size = behaviorProfile != null ? behaviorProfile.size : Vector2Int.zero;
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

    public CreatureBehaviorProfileSO BehaviorProfile => behaviorProfile;



    private void InitVisualFromProfile()
    {
        // Пытаемся найти UI Image автоматически, если не назначен в инспекторе
        if (image == null)
        {
            image = GetComponentInChildren<Image>(true);
        }

        var sprite = behaviorProfile != null ? behaviorProfile.image : null;
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

    private void InitStatsFromProfile()
    {
        if (behaviorProfile == null) return;
        int maxHp = behaviorProfile.rangs
            .Where(rang => rang != null && rang.isActiveRang)
            .Select(rang => Mathf.RoundToInt(rang.deffence.maxHealh.baseValue))
            .FirstOrDefault();
        if (maxHp > 0)
        {
            currentHealth = maxHp;
        }
    }

    // Runtime-экземпляры правил для данного существа (клонируются из профиля)
    public List<BehaviorRule> runtimeRules = new();

    private void BuildRuntimeRulesFromProfile()
    {
        UnsubscribeAllRuntimeRules();
        runtimeRules.Clear();
        if (behaviorProfile == null || behaviorProfile.rangs == null || behaviorProfile.rangs.Count == 0) return;
        var runner = GetComponent<BehaviorRunner>();
        int idx = runner != null ? Mathf.Clamp(runner.rangIndex, 0, behaviorProfile.rangs.Count - 1) : 0;
        var sourceRules = behaviorProfile.rangs[idx].rules;
        if (sourceRules == null || sourceRules.Count == 0) return;
        foreach (var src in sourceRules)
        {
            var cloned = DeepCloneBehaviorRule(src);
            cloned.Initialize(this);
            runtimeRules.Add(cloned);
        }
    }

    private void UnsubscribeAllRuntimeRules()
    {
        if (runtimeRules == null) return;
        foreach (var rule in runtimeRules)
        {
            rule?.Unsubscribe();
        }
    }

    private static BehaviorRule DeepCloneBehaviorRule(BehaviorRule source)
    {
        if (source == null) return null;
        return (BehaviorRule)DeepCloneObject(source);
    }

    private static object DeepCloneObject(object source)
    {
        if (source == null) return null;
        var t = source.GetType();
        // Примитивы и строки копируем как есть
        if (t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal)) return source;
        // UnityEngine.Object-ссылки оставляем как есть (не клонируем ассеты/компоненты)
        if (typeof(UnityEngine.Object).IsAssignableFrom(t)) return source;
        // Коллекции
        if (typeof(IList).IsAssignableFrom(t))
        {
            var list = (IList)source;
            var elementType = t.IsArray ? t.GetElementType() : t.IsGenericType ? t.GetGenericArguments()[0] : typeof(object);
            var clonedList = t.IsArray ? (IList)Array.CreateInstance(elementType, list.Count) : (IList)Activator.CreateInstance(t);
            int i = 0;
            foreach (var item in list)
            {
                var clonedItem = DeepCloneObject(item);
                if (t.IsArray)
                {
                    clonedList[i++] = clonedItem;
                }
                else
                {
                    clonedList.Add(clonedItem);
                }
            }
            return clonedList;
        }
        // Остальные ссылочные типы: создаём экземпляр и копируем поля
        var clone = Activator.CreateInstance(t);
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (var f in t.GetFields(flags))
        {
            // Пропускаем автосвойства-компиляторные (обычно имя содержит '>k__BackingField')
            if (f.Name.Contains("k__BackingField")) continue;
            var fv = f.GetValue(source);
            var cv = DeepCloneObject(fv);
            f.SetValue(clone, cv);
        }
        return clone;
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
            var effect = sources.behaviorProfile.currentRang.rules[0].effect;
            creature.creatureLife.HandleEffect(effect, sources);
        }


    }

    private void Update()
    {
        if (behaviorProfile == null || behaviorProfile.currentRang == null) return;
        if (behaviorProfile.currentRang.isPasive) return;
        if (creatureLife != null && creatureLife.isDead) return;
        UpdateCombatLoop();
    }

    private void UpdateCombatLoop()
    {
        if (!behaviorProfile.currentRang.isPasive)
        {
            AttackCooldownTick();
            UpdateCooldownUI();
        }
    }

    private void UpdateCooldownUI()
    {
        if (cooldownPanel == null) return;

        float cooldownSeconds = GetAttackCooldownSeconds();
        if (cooldownSeconds <= 0f)
        {
            cooldownPanel.fillAmount = 0f;
            return;
        }

        float fillAmount = cooldownTimer / cooldownSeconds;
        cooldownPanel.fillAmount = Mathf.Clamp01(fillAmount);
    }

    private void AttackCooldownTick()
    {
        if (behaviorProfile == null || behaviorProfile.currentRang == null) return;
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
        if (creatureLife != null && creatureLife.isDead) return;

        var resolvedTarget = target ?? FindTarget();
        if (resolvedTarget == null)
        {
            return;
        }

        currentTarget = resolvedTarget;
        SpawnProjectile(resolvedTarget);
        cooldownTimer = GetAttackCooldownSeconds();
    }

    private float GetAttackCooldownSeconds()
    {
        var offence = behaviorProfile.currentRang.offence;
        float value = offence != null ? offence.cooldown.baseValue : 0f;
        if (value <= 0f) value = 1f;
        return value;
    }
    private bool HasAnyElement<T>(IEnumerable<T> collection1, IEnumerable<T> collection2)
    {
        foreach (var item1 in collection1)
        {
            foreach (var item2 in collection2)
            {
                if (item1.Equals(item2))
                    return true;
            }
        }
        return false;
    }

    private Creature FindTarget()
    {
        if (creatureLife != null && creatureLife.isDead) return null;

        var candidates = new List<Creature>();
        Creature target = null;
        if (behaviorProfile.currentRang.rules[0].Target.direction != GeneratedEnums.DirectionId.None)
        {
            // если надо взять цель со стороны 
            var direction = behaviorProfile.currentRang.rules[0].Target.direction;
            target = behaviorRunner.GetCreaturesByDirection(direction).FirstOrDefault();

            bool needRace = behaviorProfile.currentRang.rules[0].Target.race != GeneratedEnums.RaceId.None;
        }

        // если нашли цель по условияем, то возвращем, иначе ищем по дистанции
        if (target != null) return target;

        switch (behaviorProfile.currentRang.rules[0].Target.attitude)
        {
            case GeneratedEnums.AttitudeId.Enemy:
                GetEnemyCandidates(candidates);
                break;
            case GeneratedEnums.AttitudeId.Ally:
                GetAllyCandidates(candidates);
                break;
            case GeneratedEnums.AttitudeId.Me:
                GetSelfCandidates(candidates);
                break;
            case GeneratedEnums.AttitudeId.Random:
                GetRandomCandidates(candidates);
                break;
            case GeneratedEnums.AttitudeId.None:
            default:
                return null;
        }

        if (candidates.Count == 0) return null;

        Creature nearest = candidates[0];
        float nearestSqrDist = float.MaxValue;

        foreach (var other in candidates)
        {
            if (other == null || other == this) continue;
            if (!other.isActiveAndEnabled) continue;
            if (other.creatureLife != null && other.creatureLife.isDead) continue;

            float sqr = (other.transform.position - transform.position).sqrMagnitude;
            if (sqr < nearestSqrDist)
            {
                nearestSqrDist = sqr;
                nearest = other;
            }
        }
        return nearest;
    }

    private void GetEnemyCandidates(List<Creature> candidates)
    {
        foreach (var other in All)
        {
            if (other == null || other == this) continue;
            if (other.teamNumber == teamNumber) continue;
            if (!other.isActiveAndEnabled) continue;
            if (other.creatureLife != null && other.creatureLife.isDead) continue;
            candidates.Add(other);
        }
    }

    private void GetAllyCandidates(List<Creature> candidates)
    {
        foreach (var other in All)
        {
            if (other == null || other == this) continue;
            if (other.teamNumber != teamNumber) continue;
            if (!other.isActiveAndEnabled) continue;
            if (other.creatureLife != null && other.creatureLife.isDead) continue;
            candidates.Add(other);
        }
    }

    private void GetSelfCandidates(List<Creature> candidates)
    {
        if (isActiveAndEnabled && (creatureLife == null || !creatureLife.isDead))
        {
            candidates.Add(this);
        }
    }

    private void GetRandomCandidates(List<Creature> candidates)
    {
        foreach (var other in All)
        {
            if (other == null || other == this) continue;
            if (!other.isActiveAndEnabled) continue;
            if (other.creatureLife != null && other.creatureLife.isDead) continue;
            candidates.Add(other);
        }
    }

    private void SpawnProjectile(Creature target)
    {
        if (behaviorProfile == null || behaviorProfile.spellPrefab == null) return;
        if (creatureLife != null && creatureLife.isDead) return;

        var projGo = Instantiate(behaviorProfile.spellPrefab, transform.position, Quaternion.identity);
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

        // UnitEvent.OnUnitDiedsEvent(this, source);
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
