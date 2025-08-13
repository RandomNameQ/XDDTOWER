using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using GeneratedEnums;

/// <summary>
/// Оркестратор исполнения правил поведения: триггеры → условия → цели → доставка → эффект.
/// Разбит на небольшие методы для читаемости.
/// </summary>
public partial class BehaviorRunner : MonoBehaviour
{
    private Creature _self;
    private List<BehaviorRule> _rules;
    private ICreatureBehaviorProvider _provider;

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
        if (_rules == null) return;
        foreach (var rule in _rules)
        {
            if (rule == null || rule.Triggers == null) continue;
            foreach (var trig in rule.Triggers)
            {
                trig?.Initialize(_self);
            }
        }
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
        if (!TriggerModule.ShouldFire(rule, deltaTime, _self, _conditionEngine, this)) return;

        // Gate by self-level condition semantics (requires explicit subject chain)
        if (!_conditionEngine.EvaluateForSelf(rule, _self, this)) return;

        var targets = TargetingModule.CollectAndFilterTargets(rule, _self, this, _conditionEngine);
        if (targets == null || targets.Count == 0) return;

        DeliveryModule.Deliver(rule, _self, targets, this, _valueResolver);
    }

    // Modules
    private readonly ConditionModule _conditionEngine = new ConditionModule();
    private readonly ValueResolver _valueResolver = new ValueResolver();

    // Deprecated direct condition checks are replaced by ConditionModule

    // Deprecated targeting is replaced by TargetingModule

    // Deprecated

    // Deprecated

    // Deprecated

    // Deprecated

    // Deprecated

    private static void TryApplyEffectViaExecutor(Creature self, Creature target, EffectSO effect)
    {
        var t = Type.GetType("EffectExecutor");
        if (t == null) return;
        var m = t.GetMethod("Apply", BindingFlags.Public | BindingFlags.Static);
        if (m == null) return;
        m.Invoke(null, new object[] { self, target, effect });
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

//
// Modules inside BehaviorRunner for readability and extensibility
//
public partial class BehaviorRunner
{
    /// <summary>
    /// Converts triggers to a simple fire decision.
    /// Special case: OnPassive means "fire when conditions are met".
    /// </summary>
    private static class TriggerModule
    {
        public static bool ShouldFire(BehaviorRule rule, float deltaTime, Creature self, ConditionModule conditionEngine, BehaviorRunner runner)
        {
            var triggers = rule.Triggers;
            if (triggers == null || triggers.Count == 0) return false;

            bool hasPassive = false;

            for (int i = 0; i < triggers.Count; i++)
            {
                var trig = triggers[i];
                if (trig == null) continue;
                if (trig is OnPassive)
                {
                    hasPassive = true;
                    continue;
                }
                if (trig.TryFire(deltaTime, self)) return true;
            }

            if (hasPassive)
            {
                // Fire only if conditions are satisfied
                return conditionEngine.EvaluateForSelf(rule, self, runner);
            }
            return false;
        }
    }

    /// <summary>
    /// Resolves and evaluates complex conditions.
    /// Supports subject selection via TargetCondition and NeighbourCondition.
    /// </summary>
    private class ConditionModule
    {
        private static readonly List<Creature> SubjectBuffer = new List<Creature>();

        public bool EvaluateForSelf(BehaviorRule rule, Creature self, BehaviorRunner runner)
        {
            return Evaluate(rule.Conditions, self, candidate: null, runner);
        }

        public bool EvaluateForCandidate(BehaviorRule rule, Creature self, Creature candidate, BehaviorRunner runner)
        {
            return Evaluate(rule.Conditions, self, candidate, runner);
        }

        private bool Evaluate(List<Condition> conditions, Creature self, Creature candidate, BehaviorRunner runner)
        {
            if (conditions == null || conditions.Count == 0) return true;

            // Build subject set if there are subject-scoped conditions
            bool requiresSubjects = ContainsSubjectConditions(conditions);
            if (!requiresSubjects)
            {
                // Only self-level conditions (not defined yet) → trivially true
                return true;
            }

            SubjectBuffer.Clear();
            BuildSubjects(conditions, self, candidate, runner, SubjectBuffer);

            if (SubjectBuffer.Count == 0)
            {
                // Ambiguous or no subjects → fail the condition set
                return false;
            }

            // Evaluate all conditions against the subject set
            for (int i = 0; i < conditions.Count; i++)
            {
                if (!EvaluateCondition(conditions[i], self, runner, SubjectBuffer, conditions)) return false;
            }
            return true;
        }

        private static bool ContainsSubjectConditions(List<Condition> conditions)
        {
            for (int i = 0; i < conditions.Count; i++)
            {
                var c = conditions[i];
                if (c == null) continue;
                if (c is ConditionGroup group)
                {
                    if (ContainsSubjectConditions(group.Children)) return true;
                }
                else if (c is RaceCondition || c is NeighbourCondition || c is EffectCondition || c is StatisticCondition || c is TargetCondition || c is OperationCondition)
                {
                    return true;
                }
            }
            return false;
        }

        private static void BuildSubjects(List<Condition> conditions, Creature self, Creature candidate, BehaviorRunner runner, List<Creature> destination)
        {
            // 1) If explicit TargetCondition present → start from it
            List<Creature> fromTarget = null;
            var targetCond = FindFirst<TargetCondition>(conditions);
            if (targetCond != null && targetCond.Target != null)
            {
                var enumerable = targetCond.Target.Select(self);
                if (enumerable != null)
                {
                    if (fromTarget == null) fromTarget = new List<Creature>();
                    foreach (var c in enumerable) if (c != null) fromTarget.Add(c);
                }
            }

            // 2) If NeighbourCondition present → derive neighbors relative to self
            List<Creature> fromNeighbor = null;
            var neighborCond = FindFirst<NeighbourCondition>(conditions);
            if (neighborCond != null)
            {
                fromNeighbor = new List<Creature>();
                AppendNeighborList(runner, neighborCond.direction, fromNeighbor);
            }

            // 3) If neither provided, but candidate was given → use candidate
            if (fromTarget == null && fromNeighbor == null && candidate != null)
            {
                destination.Add(candidate);
                return;
            }

            // 4) Combine sets (intersect if both provided, else take whichever exists)
            if (fromTarget != null && fromNeighbor != null)
            {
                for (int i = 0; i < fromTarget.Count; i++)
                {
                    var c = fromTarget[i];
                    if (fromNeighbor.Contains(c)) destination.Add(c);
                }
                return;
            }
            if (fromTarget != null)
            {
                destination.AddRange(fromTarget);
                return;
            }
            if (fromNeighbor != null)
            {
                destination.AddRange(fromNeighbor);
                return;
            }

            // 5) Nothing explicit → ambiguous
        }

        private static void AppendNeighborList(BehaviorRunner runner, DirectionId dir, List<Creature> list)
        {
            if (runner == null || runner.neighbors == null) return;
            switch (dir)
            {
                case DirectionId.Left: list.AddRange(runner.neighbors.left); break;
                case DirectionId.Right: list.AddRange(runner.neighbors.right); break;
                case DirectionId.Front: list.AddRange(runner.neighbors.front); break;
                case DirectionId.Back: list.AddRange(runner.neighbors.back); break;
                case DirectionId.FrontLeft: list.AddRange(runner.neighbors.frontLeft); break;
                case DirectionId.FrontRight: list.AddRange(runner.neighbors.frontRight); break;
                case DirectionId.BackLeft: list.AddRange(runner.neighbors.backLeft); break;
                case DirectionId.BackRight: list.AddRange(runner.neighbors.backRight); break;
                default: break;
            }
        }

        private static bool EvaluateCondition(Condition cond, Creature self, BehaviorRunner runner, List<Creature> subjects, List<Condition> rootConditions)
        {
            if (cond == null) return true;

            if (cond is ConditionGroup group)
            {
                if (group.Children == null || group.Children.Count == 0) return true;
                if (group.Op == ConditionGroup.Operator.And)
                {
                    for (int i = 0; i < group.Children.Count; i++)
                    {
                        if (!EvaluateCondition(group.Children[i], self, runner, subjects, rootConditions)) return false;
                    }
                    return true;
                }
                else
                {
                    for (int i = 0; i < group.Children.Count; i++)
                    {
                        if (EvaluateCondition(group.Children[i], self, runner, subjects, rootConditions)) return true;
                    }
                    return false;
                }
            }

            if (cond is NeighbourCondition) return true; // Already handled as subject filter
            if (cond is TargetCondition) return true;    // Already handled as subject source

            if (cond is RaceCondition race)
            {
                for (int i = 0; i < subjects.Count; i++)
                {
                    var subj = subjects[i];
                    var raceId = subj != null && subj.BehaviorProfile != null ? subj.BehaviorProfile.race : RaceId.None;
                    if (raceId != RaceId.None && (raceId & race.race) != 0) return true;
                }
                return false;
            }

            if (cond is EffectCondition effectCond)
            {
                var opCond = FindFirst<OperationCondition>(rootConditions);
                OperationCondition.Operation? expectedOp = opCond != null ? opCond.operation : (OperationCondition.Operation?)null;
                return EventBus.AnyEffectEventFor(subjects, effectCond.effect, expectedOp);
            }

            if (cond is StatisticCondition statCond)
            {
                var opCond = FindFirst<OperationCondition>(rootConditions);
                OperationCondition.Operation? expectedOp = opCond != null ? opCond.operation : (OperationCondition.Operation?)null;
                return EventBus.AnyStatEventFor(subjects, statCond.statistic, expectedOp);
            }

            // Unknown condition → treat as true to not block rules
            return true;
        }

        private static T FindFirst<T>(List<Condition> conditions) where T : Condition
        {
            if (conditions == null) return null;
            for (int i = 0; i < conditions.Count; i++)
            {
                var c = conditions[i];
                if (c == null) continue;
                if (c is T matched) return matched;
                if (c is ConditionGroup g)
                {
                    var nested = FindFirst<T>(g.Children);
                    if (nested != null) return nested;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Targeting and filtering candidates by conditions against each candidate.
    /// </summary>
    private static class TargetingModule
    {
        public static List<Creature> CollectAndFilterTargets(BehaviorRule rule, Creature self, BehaviorRunner runner, ConditionModule conditionEngine)
        {
            TargetsBuffer.Clear();
            var selected = rule.Target != null ? rule.Target.Select(self) : null;
            if (selected == null) return TargetsBuffer;

            foreach (var candidate in selected)
            {
                if (candidate == null) continue;
                if (!conditionEngine.EvaluateForCandidate(rule, self, candidate, runner)) continue;
                TargetsBuffer.Add(candidate);
            }
            return TargetsBuffer;
        }
    }

    /// <summary>
    /// Applies effects (via projectile or instant) and posts events to the bus.
    /// </summary>
    private static class DeliveryModule
    {
        public static void Deliver(BehaviorRule rule, Creature self, List<Creature> targets, BehaviorRunner runner, ValueResolver valueResolver)
        {
            if (rule == null || rule.effect == EffectId.None) return;

            var prefab = self.BehaviorProfile != null ? self.BehaviorProfile.spellPrefab : null;
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target == null) continue;

                int amount = valueResolver.ResolveAmount(rule.value, self, target);
                var effectSo = CreateTransientEffectSO(rule.effect, amount);

                if (prefab == null)
                {
                    ApplyInstant(self, target, effectSo);
                }
                else
                {
                    DeliverByProjectile(self, target, effectSo, prefab);
                }

                // Two perspective events: actor(Add) and target(Get)
                EventBus.PostEffectApplied(self, target, rule.effect, amount, OperationCondition.Operation.Add);
                EventBus.PostEffectApplied(self, target, rule.effect, amount, OperationCondition.Operation.Get);

                // Post stat delta for CurrentHealth for known effects
                if (rule.effect == EffectId.Damage)
                {
                    EventBus.PostStatChanged(self, target, StatsId.CurrentHealth, -Mathf.Abs(amount), OperationCondition.Operation.Get);
                }
                else if (rule.effect == EffectId.Heal)
                {
                    EventBus.PostStatChanged(self, target, StatsId.CurrentHealth, Mathf.Abs(amount), OperationCondition.Operation.Get);
                }
            }
        }

        private static EffectSO CreateTransientEffectSO(EffectId id, int amount)
        {
            var so = ScriptableObject.CreateInstance<EffectSO>();
            so.name = id.ToString();
            so.amount = amount;
            return so;
        }

        private static void ApplyInstant(Creature self, Creature target, EffectSO effect)
        {
            TryApplyEffectViaExecutor(self, target, effect);
        }

        private static void DeliverByProjectile(Creature self, Creature target, EffectSO effect, GameObject projectilePrefab)
        {
            var go = UnityEngine.Object.Instantiate(projectilePrefab, self.transform.position, Quaternion.identity);
            var proj = go.GetComponent<ProjectileBase>();
            if (proj == null)
            {
                TryApplyEffectViaExecutor(self, target, effect);
                UnityEngine.Object.Destroy(go);
                return;
            }
            proj.Init(target.gameObject, () =>
            {
                TryApplyEffectViaExecutor(self, target, effect);
            });
        }
    }

    /// <summary>
    /// Resolves numbers from Value (number/percent/random). Percent can use stats of selected targets.
    /// </summary>
    private class ValueResolver
    {
        public int ResolveAmount(Value value, Creature self, Creature target)
        {
            if (value == null) return 0;
            if (value.number != null) return Math.Max(0, value.number.value);
            if (value.random != null)
            {
                int min = Math.Min(value.random.random.x, value.random.random.y);
                int max = Math.Max(value.random.random.x, value.random.random.y) + 1;
                return UnityEngine.Random.Range(min, max);
            }
            if (value.percent != null)
            {
                float baseSum = 0f;
                var percent = value.percent;
                IEnumerable<Creature> who = null;
                if (percent.target != null)
                {
                    who = percent.target.Select(self);
                }
                else
                {
                    who = target != null ? new[] { target } : new[] { self };
                }

                if (percent.statistic != null && percent.statistic.stat != null)
                {
                    foreach (var c in who)
                    {
                        baseSum += ResolveStatValue(c, percent.statistic.stat.name);
                    }
                }
                else
                {
                    foreach (var c in who)
                    {
                        baseSum += ResolveStatValue(c, StatsId.CurrentHealth.ToString());
                    }
                }
                int result = Mathf.RoundToInt(baseSum * (percent.percent / 100f));
                return Math.Max(0, result);
            }
            return 0;
        }

        private static int ResolveStatValue(Creature c, string statKey)
        {
            if (c == null) return 0;
            // Minimal mapping for demo purposes
            if (string.Equals(statKey, StatsId.CurrentHealth.ToString(), StringComparison.Ordinal))
                return c.currentHealth;
            if (string.Equals(statKey, StatsId.MaxHealth.ToString(), StringComparison.Ordinal))
            {
                var profile = c.BehaviorProfile;
                if (profile != null && profile.rangs != null && profile.rangs.Count > 0)
                {
                    int idx = 0;
                    int max = profile.rangs[Mathf.Clamp(idx, 0, profile.rangs.Count - 1)].maxHealth;
                    return max;
                }
            }
            return 0;
        }
    }

    /// <summary>
    /// Simple global bus for cross-runner effect/stat events used by Operation conditions.
    /// </summary>
    private static class EventBus
    {
        private struct EffectEvent
        {
            public Creature actor;
            public Creature target;
            public EffectId effectId;
            public int amount;
            public float time;
            public OperationCondition.Operation operation;
        }

        private struct StatEvent
        {
            public Creature actor;
            public Creature target;
            public StatsId statId;
            public int amount;
            public float time;
            public OperationCondition.Operation operation;
        }

        private static readonly List<EffectEvent> Effects = new List<EffectEvent>();
        private static readonly List<StatEvent> Stats = new List<StatEvent>();
        private const float Lifetime = 0.5f; // seconds to keep events

        public static void PostEffectApplied(Creature actor, Creature target, EffectId id, int amount, OperationCondition.Operation perspective)
        {
            if (perspective == OperationCondition.Operation.Add)
            {
                Effects.Add(new EffectEvent { actor = actor, target = target, effectId = id, amount = amount, time = Time.time, operation = OperationCondition.Operation.Add });
            }
            else if (perspective == OperationCondition.Operation.Get)
            {
                Effects.Add(new EffectEvent { actor = target, target = actor, effectId = id, amount = amount, time = Time.time, operation = OperationCondition.Operation.Get });
            }
            Cull();
        }

        public static void PostStatChanged(Creature actor, Creature target, StatsId id, int amount, OperationCondition.Operation perspective)
        {
            if (perspective == OperationCondition.Operation.Add)
            {
                Stats.Add(new StatEvent { actor = actor, target = target, statId = id, amount = amount, time = Time.time, operation = OperationCondition.Operation.Add });
            }
            else if (perspective == OperationCondition.Operation.Get)
            {
                Stats.Add(new StatEvent { actor = target, target = actor, statId = id, amount = amount, time = Time.time, operation = OperationCondition.Operation.Get });
            }
            Cull();
        }

        public static bool AnyEffectEventFor(List<Creature> subjects, EffectId id, OperationCondition.Operation? expected)
        {
            Cull();
            for (int i = Effects.Count - 1; i >= 0; i--)
            {
                var e = Effects[i];
                if (e.effectId != id) continue;
                if (expected.HasValue && e.operation != expected.Value) continue;
                if (subjects.Contains(e.actor) || subjects.Contains(e.target)) return true;
            }
            return false;
        }

        public static bool AnyStatEventFor(List<Creature> subjects, StatsId id, OperationCondition.Operation? expected)
        {
            Cull();
            for (int i = Stats.Count - 1; i >= 0; i--)
            {
                var e = Stats[i];
                if (e.statId != id) continue;
                if (expected.HasValue && e.operation != expected.Value) continue;
                if (subjects.Contains(e.actor) || subjects.Contains(e.target)) return true;
            }
            return false;
        }

        private static void Cull()
        {
            float now = Time.time;
            for (int i = Effects.Count - 1; i >= 0; i--)
            {
                if (now - Effects[i].time > Lifetime) Effects.RemoveAt(i);
            }
            for (int i = Stats.Count - 1; i >= 0; i--)
            {
                if (now - Stats[i].time > Lifetime) Stats.RemoveAt(i);
            }
        }
    }
}
