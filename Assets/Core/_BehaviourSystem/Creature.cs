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
    public Behavior behavior;

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
    }

    private void Start()
    {
        behavior = new(this);
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
            gameObject.AddComponent<BehaviorRunner>();
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
    public class Behavior
    {
        public Creature creature;

        public Behavior(Creature creature)
        {
            this.creature = creature;
        }

        public void ApplyEffect(GeneratedEnums.EffectId effect)
        {

        }
        public void ReciveEffect()
        {

        }

    }
}
