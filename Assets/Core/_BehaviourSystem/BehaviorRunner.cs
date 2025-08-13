using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using GeneratedEnums;

/// <summary>
/// Оркестратор исполнения правил поведения: триггеры → условия → цели → доставка → эффект.
/// Разбит на небольшие методы для читаемости.
/// </summary>
public class BehaviorRunner : MonoBehaviour
{
    private Creature _self;
    private List<BehaviorRule> _rules;
    private ICreatureBehaviorProvider _provider;

    // ------------------------
    // Вложенные подсистемы для читаемости
    // ------------------------
    private static class TriggerLogic
    {
        public static void InitializeAll(List<BehaviorRule> rules, Creature self)
        {
            if (rules == null) return;
            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (rule == null || rule.Triggers == null) continue;
                for (int t = 0; t < rule.Triggers.Count; t++)
                {
                    rule.Triggers[t]?.Initialize(self);
                }
            }
        }

        public static bool ShouldFire(BehaviorRule rule, float deltaTime, Creature self)
        {
            var triggers = rule.Triggers;
            if (triggers == null || triggers.Count == 0) return false;
            for (int t = 0; t < triggers.Count; t++)
            {
                var trig = triggers[t];
                if (trig != null && trig.TryFire(deltaTime, self)) return true;
            }
            return false;
        }
    }

    private static class ConditionLogic
    {
        public static bool AreSelfConditionsMet(BehaviorRule rule, Creature self)
        {
            var conditions = rule.Conditions;
            if (conditions == null || conditions.Count == 0) return true;
            for (int i = 0; i < conditions.Count; i++)
            {
                var cond = conditions[i];
                if (cond != null && !cond.Evaluate(self)) return false;
            }
            return true;
        }

        public static bool AreTargetConditionsMet(List<Condition> conditions, Creature self, Creature candidate)
        {
            if (conditions == null || conditions.Count == 0) return true;
            for (int i = 0; i < conditions.Count; i++)
            {
                var cond = conditions[i];
                if (cond != null && !cond.EvaluateForTarget(self, candidate)) return false;
            }
            return true;
        }
    }

    private static class TargetLogic
    {
        private static readonly List<Creature> Buffer = new();

        public static List<Creature> CollectAndFilterTargets(BehaviorRule rule, Creature self)
        {
            Buffer.Clear();
            var selected = rule.Target != null ? rule.Target.Select(self) : null;
            if (selected == null) return Buffer;

            var conditions = rule.Conditions;
            foreach (var candidate in selected)
            {
                if (candidate == null) continue;
                if (!ConditionLogic.AreTargetConditionsMet(conditions, self, candidate)) continue;
                Buffer.Add(candidate);
            }
            return Buffer;
        }
    }

    private static class EffectLogic
    {
        public static void Deliver(Creature self, Creature target, BehaviorRule rule, GameObject projectilePrefab)
        {
            if (self == null || target == null || rule == null) return;

            if (projectilePrefab == null)
            {
                ApplyInstant(self, target, rule);
                return;
            }

            var go = UnityEngine.Object.Instantiate(projectilePrefab, self.transform.position, Quaternion.identity);
            var proj = go.GetComponent<ProjectileBase>();
            if (proj == null)
            {
                ApplyInstant(self, target, rule);
                UnityEngine.Object.Destroy(go);
                return;
            }

            proj.Init(target.gameObject, () => { ApplyInstant(self, target, rule); });
        }

        private static void ApplyInstant(Creature self, Creature target, BehaviorRule rule)
        {
            var so = CreateRuntimeEffectAsset(rule.effect, rule.value);
            if (so == null) return;
            EffectExecutor.Apply(self, target, so);
        }

        private static EffectSO CreateRuntimeEffectAsset(EffectId effectId, Value value)
        {
            if (effectId == EffectId.None) return null;
            var so = ScriptableObject.CreateInstance<EffectSO>();
            so.name = effectId.ToString();
            so.amount = EvaluateAmount(value);
            return so;
        }

        private static int EvaluateAmount(Value v)
        {
            if (v == null) return 0;
            if (v.number != null) return v.number.value;
            if (v.random != null)
            {
                var min = Math.Min(v.random.random.x, v.random.random.y);
                var max = Math.Max(v.random.random.x, v.random.random.y) + 1;
                return UnityEngine.Random.Range(min, max);
            }
            if (v.percent != null) return v.percent.percent; // упрощённая модель
            return 0;
        }
    }

    [Serializable]
    public class Neighbor
    {
        public List<Creature> left = new();
        public List<Creature> right = new();
        public List<Creature> front = new();
        public List<Creature> back = new();
        public List<Creature> frontLeft = new();
        public List<Creature> frontRight = new();
        public List<Creature> backLeft = new();
        public List<Creature> backRight = new();
    }
    public Neighbor neighbors;

    [Tooltip("Какой ранг использовать из CreatureBehaviorProfileSO.rangs")] public int rangIndex = 0;

    private static readonly List<Creature> TargetsBuffer = new();

    private void Start()
    {
        InitializeSelfAndRules();
        InitializeProvider();
        LoadRulesFromProvider();
        InitializeTriggersForAllRules();
        SubscribeToGlobalNeighborUpdates();
    }

    private object _lastBoard;
    private Vector2Int _lastOrigin;

    private void Update()
    {
        UpdateNeighborsIfNeeded();
        if (!IsReady()) return;
        float deltaTime = Time.deltaTime;
        ProcessAllRules(deltaTime);
    }

    private void OnGridNeighborsChanged()
    {
        if (_self == null) _self = GetComponent<Creature>();
        var placeable = GetPlaceableComponent(gameObject);
        if (placeable == null) return;
        RebuildNeighbors(placeable);
    }

    public void UpdateNeighbors()
    {
        var placeable = GetPlaceableComponent(gameObject);
        if (placeable == null) return;
        RebuildNeighbors(placeable);
    }

    private void SubscribeToGlobalNeighborUpdates()
    {
        GlobalEvent.OnUpdateNeighbors += HandleGlobalUpdateNeighbors;
    }

    private void OnDestroy()
    {
        GlobalEvent.OnUpdateNeighbors -= HandleGlobalUpdateNeighbors;
    }

    private void HandleGlobalUpdateNeighbors()
    {
        UpdateNeighbors();
    }

    private void InitializeSelfAndRules()
    {
        _self = GetComponent<Creature>();
        if (_self == null || _self.BehaviorProfile == null) return;
    }

    private void InitializeProvider()
    {
        _provider = GetComponent<ICreatureBehaviorProvider>();
        if (_provider == null)
        {
            _provider = FindFirstObjectByType<CreatureBehaviorProvider>();
        }
    }

    private void LoadRulesFromProvider()
    {
        if (_self == null) return;
        // 1) Если профиль задан на самом юните — берём правила напрямую
        if (_self.BehaviorProfile != null &&
            _self.BehaviorProfile.rangs != null &&
            _self.BehaviorProfile.rangs.Count > 0)
        {
            int idx = Mathf.Clamp(rangIndex, 0, _self.BehaviorProfile.rangs.Count - 1);
            _rules = _self.BehaviorProfile.rangs[idx].rules;
            return;
        }

        // Профиль обязателен: внешний провайдер по прежнему CreatureSO больше не используется
    }

    private void InitializeTriggersForAllRules()
    {
        TriggerLogic.InitializeAll(_rules, _self);
    }

    private bool IsReady()
    {
        return _self != null && _rules != null;
    }

    private void ProcessAllRules(float deltaTime)
    {
        for (int i = 0; i < _rules.Count; i++)
        {
            var rule = _rules[i];
            if (rule == null) continue;
            ProcessSingleRule(rule, deltaTime);
        }
    }

    private void ProcessSingleRule(BehaviorRule rule, float deltaTime)
    {
        if (!ПроверкаТригера(rule, deltaTime)) return;
        if (!ConditionLogic.AreSelfConditionsMet(rule, _self)) return;

        var targets = TargetLogic.CollectAndFilterTargets(rule, _self);
        if (targets == null || targets.Count == 0) return;

        DeliverEffectToTargets(rule, targets);
    }

    private bool ПроверкаТригера(BehaviorRule rule, float deltaTime)
    {
        return TriggerLogic.ShouldFire(rule, deltaTime, _self);
    }

    private List<Creature> CollectAndFilterTargets(BehaviorRule rule)
    {
        return TargetLogic.CollectAndFilterTargets(rule, _self);
    }

    private void DeliverEffectToTargets(BehaviorRule rule, List<Creature> targets)
    {
        for (int k = 0; k < targets.Count; k++)
        {
            var target = targets[k];
            DeliverEffect(_self, target, rule);
        }
    }

    private void DeliverEffect(Creature self, Creature target, BehaviorRule rule)
    {
        if (rule == null || target == null) return;
        var prefab = self.BehaviorProfile != null ? self.BehaviorProfile.spellPrefab : null;
        EffectLogic.Deliver(self, target, rule, prefab);
    }


    private void UpdateNeighborsIfNeeded()
    {
        if (_self == null) return;
        var placeable = GetPlaceableComponent(_self.gameObject);
        if (placeable == null) return;
        var currentBoard = GetProperty<object>(placeable, "CurrentBoard");
        var origin = GetProperty<Vector2Int>(placeable, "OriginCell");

        if (currentBoard != _lastBoard || origin != _lastOrigin)
        {
            _lastBoard = currentBoard;
            _lastOrigin = origin;
            RebuildNeighbors(placeable);
        }
    }

    private void RebuildNeighbors(Component placeable)
    {
        if (neighbors == null) neighbors = new Neighbor();
        neighbors.left.Clear();
        neighbors.right.Clear();
        neighbors.front.Clear();
        neighbors.back.Clear();
        neighbors.frontLeft.Clear();
        neighbors.frontRight.Clear();
        neighbors.backLeft.Clear();
        neighbors.backRight.Clear();

        if (placeable == null) return;

        AppendNeighbors(neighbors.left, placeable, "Left");
        AppendNeighbors(neighbors.right, placeable, "Right");
        AppendNeighbors(neighbors.front, placeable, "Up");
        AppendNeighbors(neighbors.back, placeable, "Down");
        AppendNeighbors(neighbors.frontLeft, placeable, "UpLeft");
        AppendNeighbors(neighbors.frontRight, placeable, "UpRight");
        AppendNeighbors(neighbors.backLeft, placeable, "DownLeft");
        AppendNeighbors(neighbors.backRight, placeable, "DownRight");
    }

    private static void AppendNeighbors(List<Creature> list, Component placeable, string dirSuffix)
    {
        var directionType = TryResolveType("Core.BoardV2.Direction", "ZXC");
        if (directionType == null)
        {
            AppendNeighbor(list, CallNeighbor(placeable, "GetNeighbor" + dirSuffix));
            return;
        }
        var dirValue = Enum.Parse(directionType, dirSuffix);

        var placeableType = TryResolveType("Core.BoardV2.PlaceableObject", "ZXC");
        if (placeableType == null)
        {
            AppendNeighbor(list, CallNeighbor(placeable, "GetNeighbor" + dirSuffix));
            return;
        }
        var listType = typeof(List<>).MakeGenericType(placeableType);
        var buffer = Activator.CreateInstance(listType);

        var board = GetProperty<Component>(placeable, "CurrentBoard");
        if (board == null)
        {
            AppendNeighbor(list, CallNeighbor(placeable, "GetNeighbor" + dirSuffix));
            return;
        }
        var getNeighbors = board.GetType().GetMethod("GetNeighbors", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (getNeighbors == null)
        {
            AppendNeighbor(list, CallNeighbor(placeable, "GetNeighbor" + dirSuffix));
            return;
        }
        getNeighbors.Invoke(board, new object[] { placeable, dirValue, buffer });

        var asIEnumerable = buffer as System.Collections.IEnumerable;
        if (asIEnumerable == null) return;
        foreach (var item in asIEnumerable)
        {
            if (item is Component comp)
            {
                var c = comp.GetComponent<Creature>();
                if (c != null) list.Add(c);
            }
        }
    }

    private static void AppendNeighbor(List<Creature> list, Component neighbor)
    {
        if (neighbor == null) return;
        var c = neighbor.GetComponent<Creature>();
        if (c != null) list.Add(c);
    }

    private static Component GetPlaceableComponent(GameObject go)
    {
        var t = TryResolveType("Core.BoardV2.PlaceableObject", "ZXC");
        if (t == null) return null;
        return go.GetComponent(t);
    }

    private static T GetProperty<T>(Component comp, string propertyName)
    {
        if (comp == null) return default;
        var pi = comp.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (pi == null) return default;
        var v = pi.GetValue(comp);
        if (v is T tv) return tv;
        return default;
    }

    private static Component CallNeighbor(Component comp, string methodName)
    {
        if (comp == null) return null;
        var mi = comp.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (mi == null) return null;
        var v = mi.Invoke(comp, null) as Component;
        return v;
    }

    private static Type TryResolveType(string fullName, string assemblyName)
    {
        var t = Type.GetType(fullName + ", " + assemblyName, false);
        if (t != null) return t;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                t = asm.GetType(fullName, false);
                if (t != null) return t;
            }
            catch { }
        }
        return null;
    }
}
