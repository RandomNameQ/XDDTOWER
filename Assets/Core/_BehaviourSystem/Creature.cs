using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.UI;
 
using Sirenix.OdinInspector;
using JetBrains.Annotations;

/// <summary>
/// Носитель поведения на сцене. Хранит базовые данные юнита и реестр всех существ.
/// </summary>
public class Creature : MonoBehaviour, ICreatureComponent, IInitFromSO
{
    public static readonly System.Collections.Generic.List<Creature> All = new System.Collections.Generic.List<Creature>();
    public CreatureBehaviorProfileSO behaviorProfile;
    ScriptableObject ICreatureComponent.CreatureData { get => behaviorProfile; set => behaviorProfile = value as CreatureBehaviorProfileSO; }
    public Image image;
    
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

    private void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
        ApplyBoardSizeFromSO();
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

    public class Behaviour { }

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

    // Полный отказ от CreatureSO — сеттера под него больше нет

    // Полный отказ от CreatureSO: вспомогательных методов не требуется

    private void InitVisualFromProfile()
    {
        if (image == null) return;
        if (behaviorProfile != null && behaviorProfile.image != null)
        {
            image.sprite = behaviorProfile.image;
            return;
        }
        // Профиль обязателен — фолбэков нет
    }

    private void InitStatsFromProfile()
    {
        if (behaviorProfile == null || behaviorProfile.rangs == null || behaviorProfile.rangs.Count == 0) return;
        var runner = GetComponent<BehaviorRunner>();
        int idx = runner != null ? Mathf.Clamp(runner.rangIndex, 0, behaviorProfile.rangs.Count - 1) : 0;
        int maxHp = behaviorProfile.rangs[idx].maxHealth;
        if (maxHp > 0)
        {
            currentHealth = maxHp;
        }
    }

}
