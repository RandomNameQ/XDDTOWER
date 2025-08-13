using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using GeneratedEnums;

/// <summary>
/// Оркестратор исполнения правил поведения: триггеры → условия → цели → доставка → эффект.
/// Разбит на небольшие подсистемы (вложенные классы), чтобы чтение логики было максимально простым.
/// </summary>
public class BehaviorRunner : MonoBehaviour
{
    /// <summary>
    /// Ссылка на самого владельца-существо.
    /// </summary>
    private Creature _self;
    /// <summary>
    /// Активный список правил поведения, выбранный по рангу.
    /// </summary>
    private List<BehaviorRule> _rules;
    /// <summary>
    /// Провайдер профилей поведения (на будущее, сейчас не используется для загрузки).
    /// </summary>
    private ICreatureBehaviorProvider _provider;

    // ------------------------
    // Вложенные подсистемы для читаемости
    // ------------------------
    /// <summary>
    /// Подсистема триггеров: инициализация и проверка срабатывания.
    /// </summary>
    private static class TriggerLogic
    {
        /// <summary>
        /// Инициализирует все триггеры всех правил для конкретного существа.
        /// </summary>
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

        /// <summary>
        /// Проверяет, сработал ли хотя бы один триггер правила в этот кадр.
        /// </summary>
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

    /// <summary>
    /// Подсистема условий: проверка условий на self и на кандидатов-целей.
    /// </summary>
    private static class ConditionLogic
    {
        /// <summary>
        /// Возвращает ссылку на найденное условие-перехватчик (ChangeBehaviorCondition), если оно есть и
        /// при этом все прочие условия (кроме него) выполнены.
        /// Если перехватчика нет — возвращает null, а AreSelfConditionsMet продолжит обычную проверку.
        /// </summary>
        public static ChangeBehaviorCondition TryGetChangeBehaviorCondition(BehaviorRule rule, Creature self)
        {
            var conditions = rule.Conditions;
            if (conditions == null || conditions.Count == 0) return null;

            ChangeBehaviorCondition interceptor = null;
            for (int i = 0; i < conditions.Count; i++)
            {
                var cond = conditions[i];
                if (cond == null) continue;
                if (cond is ChangeBehaviorCondition cbc)
                {
                    interceptor = cbc;
                    continue; // не проверяем его здесь; он сработает, если остальные true
                }
                if (!cond.Evaluate(self)) return null;
            }
            return interceptor; // если добрались сюда — все прочие true
        }

        /// <summary>
        /// Проверяет условия, относящиеся к самому себе (self).
        /// </summary>
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

        /// <summary>
        /// Проверяет условия на конкретного кандидата (target).
        /// </summary>
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

    /// <summary>
    /// Подсистема целей: выбор кандидатов и фильтрация по условиям.
    /// </summary>
    private static class TargetLogic
    {
        private static readonly List<Creature> Buffer = new();

        /// <summary>
        /// Выбирает цели из правила и фильтрует по условиям на цель.
        /// </summary>
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

    /// <summary>
    /// Подсистема эффектов: доставка (снаряд/мгновенно) и применение.
    /// </summary>
    private static class EffectLogic
    {
        /// <summary>
        /// Доставляет эффект до цели: через снаряд (если есть префаб) или мгновенно.
        /// </summary>
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

        /// <summary>
        /// Мгновенное применение эффекта (без визуальной доставки).
        /// </summary>
        private static void ApplyInstant(Creature self, Creature target, BehaviorRule rule)
        {
            var so = CreateRuntimeEffectAsset(rule.effect, rule.value);
            if (so == null) return;
            EffectExecutor.Apply(self, target, so);
        }

        /// <summary>
        /// Создаёт временный EffectSO по идентификатору енама и значению.
        /// Имя ассета совпадает с названием енама, чтобы корректно отработал EffectExecutor.
        /// </summary>
        private static EffectSO CreateRuntimeEffectAsset(EffectId effectId, Value value)
        {
            if (effectId == EffectId.None) return null;
            var so = ScriptableObject.CreateInstance<EffectSO>();
            so.name = effectId.ToString();
            so.amount = EvaluateAmount(value);
            return so;
        }

        /// <summary>
        /// Оценивает числовую величину эффекта из структуры Value (number/random/percent).
        /// </summary>
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

    /// <summary>
    /// Подсистема соседей/позиционирования: отслеживание изменения клетки/доски и сбор соседей в 8 направлениях.
    /// Вся логика, касающаяся получения соседей из грид-системы, вынесена сюда.
    /// </summary>
    private class NeighborsLogic
    {
        private readonly Creature _self;
        private readonly BehaviorRunner _owner;
        private object _lastBoard;
        private Vector2Int _lastOrigin;

        /// <summary>
        /// Создание менеджера соседей.
        /// </summary>
        public NeighborsLogic(Creature self, BehaviorRunner owner)
        {
            _self = self;
            _owner = owner;
        }

        /// <summary>
        /// Подписка на глобальное событие обновления соседей.
        /// </summary>
        public void Subscribe()
        {
            GlobalEvent.OnUpdateNeighbors += HandleGlobalUpdateNeighbors;
        }

        /// <summary>
        /// Отписка от глобальных событий.
        /// </summary>
        public void Unsubscribe()
        {
            GlobalEvent.OnUpdateNeighbors -= HandleGlobalUpdateNeighbors;
        }

        /// <summary>
        /// Публичный метод: полное обновление соседей (например, по внешнему вызову).
        /// </summary>
        public void UpdateNeighbors()
        {
            var placeable = GetPlaceableComponent(_owner.gameObject);
            if (placeable == null) return;
            RebuildNeighbors(placeable);
        }

        /// <summary>
        /// Проверка «нужно ли» обновлять: если изменилась доска или клетка — перестраиваем список соседей.
        /// Вызывать каждый кадр.
        /// </summary>
        public void UpdateIfNeeded()
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

        /// <summary>
        /// Обработчик глобального события «обновить соседей».
        /// </summary>
        private void HandleGlobalUpdateNeighbors()
        {
            UpdateNeighbors();
        }

        /// <summary>
        /// Полная перестройка списков соседей во всех 8 направлениях.
        /// </summary>
    private void RebuildNeighbors(Component placeable)
    {
            if (_owner.neighbors == null) _owner.neighbors = new Neighbor();
            _owner.neighbors.left.Clear();
            _owner.neighbors.right.Clear();
            _owner.neighbors.front.Clear();
            _owner.neighbors.back.Clear();
            _owner.neighbors.frontLeft.Clear();
            _owner.neighbors.frontRight.Clear();
            _owner.neighbors.backLeft.Clear();
            _owner.neighbors.backRight.Clear();

        if (placeable == null) return;

            AppendNeighbors(_owner.neighbors.left, placeable, "Left");
            AppendNeighbors(_owner.neighbors.right, placeable, "Right");
            AppendNeighbors(_owner.neighbors.front, placeable, "Up");
            AppendNeighbors(_owner.neighbors.back, placeable, "Down");
            AppendNeighbors(_owner.neighbors.frontLeft, placeable, "UpLeft");
            AppendNeighbors(_owner.neighbors.frontRight, placeable, "UpRight");
            AppendNeighbors(_owner.neighbors.backLeft, placeable, "DownLeft");
            AppendNeighbors(_owner.neighbors.backRight, placeable, "DownRight");
        }

        /// <summary>
        /// Добавляет всех соседей в указанном направлении в список существ.
        /// </summary>
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

        /// <summary>
        /// Добавляет одного соседнего компонента, если он содержит существо.
        /// </summary>
    private static void AppendNeighbor(List<Creature> list, Component neighbor)
    {
        if (neighbor == null) return;
        var c = neighbor.GetComponent<Creature>();
        if (c != null) list.Add(c);
    }

        /// <summary>
        /// Возвращает компонент размещаемого объекта из грид-системы через тип по имени.
        /// </summary>
    private static Component GetPlaceableComponent(GameObject go)
    {
        var t = TryResolveType("Core.BoardV2.PlaceableObject", "ZXC");
        if (t == null) return null;
        return go.GetComponent(t);
    }

        /// <summary>
        /// Получает свойство через рефлексию с защитой от ошибок типов.
        /// </summary>
    private static T GetProperty<T>(Component comp, string propertyName)
    {
        if (comp == null) return default;
        var pi = comp.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (pi == null) return default;
        var v = pi.GetValue(comp);
        if (v is T tv) return tv;
        return default;
    }

        /// <summary>
        /// Вызывает метод получения соседа по имени (например, GetNeighborLeft) через рефлексию.
        /// </summary>
    private static Component CallNeighbor(Component comp, string methodName)
    {
        if (comp == null) return null;
        var mi = comp.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (mi == null) return null;
        var v = mi.Invoke(comp, null) as Component;
        return v;
    }

        /// <summary>
        /// Пытается получить тип по полному имени из загруженных сборок.
        /// </summary>
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

    /// <summary>
    /// DTO-структура списков соседей по 8 направлениям.
    /// </summary>
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
    /// <summary>
    /// Публичный доступ к соседям для инспектора/других систем.
    /// </summary>
    public Neighbor neighbors;

    /// <summary>
    /// Менеджер соседей, инкапсулирующий всю позиционную логику.
    /// </summary>
    private NeighborsLogic _neighborsLogic;
    /// <summary>
    /// Рантайм-копия профиля поведения, чтобы экземпляры не делили состояние.
    /// </summary>
    private CreatureBehaviorProfileSO _runtimeProfile;

    /// <summary>
    /// Индекс ранга из профиля поведения, по которому берутся правила.
    /// </summary>
    [Tooltip("Какой ранг использовать из CreatureBehaviorProfileSO.rangs")] public int rangIndex = 0;

    /// <summary>
    /// Временный буфер целей (оставлен для обратной совместимости, текущая логика использует TargetLogic.Buffer).
    /// </summary>
    private static readonly List<Creature> TargetsBuffer = new();

    /// <summary>
    /// Точка входа: инициализация self/правил/провайдера, триггеров и подписка на обновление соседей.
    /// </summary>
    private void Start()
    {
        InitializeSelfAndRules();
        InitializeProvider();
        LoadRulesFromProvider();
        InitializeTriggersForAllRules();
        // Инициализируем менеджер соседей и подписываемся на события
        _neighborsLogic = new NeighborsLogic(_self, this);
        _neighborsLogic.Subscribe();
    }

    /// <summary>
    /// Ежекадровый цикл: при необходимости обновляем соседей и запускаем обработку правил.
    /// </summary>
    private void Update()
    {
        _neighborsLogic?.UpdateIfNeeded();
        if (!IsReady()) return;
        float deltaTime = Time.deltaTime;
        ProcessAllRules(deltaTime);
    }

    /// <summary>
    /// Внешний хук (если используется системой грида): перестроить соседей немедленно.
    /// </summary>
    private void OnGridNeighborsChanged()
    {
        _neighborsLogic?.UpdateNeighbors();
    }

    /// <summary>
    /// Публичный метод для внешнего ручного запроса обновления соседей.
    /// </summary>
    public void UpdateNeighbors()
    {
        _neighborsLogic?.UpdateNeighbors();
    }

    /// <summary>
    /// Отписка от событий при уничтожении.
    /// </summary>
    private void OnDestroy()
    {
        _neighborsLogic?.Unsubscribe();
    }

    /// <summary>
    /// Инициализация ссылки на self и базовая проверка наличия профиля.
    /// </summary>
    private void InitializeSelfAndRules()
    {
        _self = GetComponent<Creature>();
        if (_self == null || _self.BehaviorProfile == null) return;
    }

    /// <summary>
    /// Инициализация провайдера профилей (при отсутствии локального компонента выполняется глобальный поиск).
    /// </summary>
    private void InitializeProvider()
    {
        _provider = GetComponent<ICreatureBehaviorProvider>();
        if (_provider == null)
        {
            _provider = FindFirstObjectByType<CreatureBehaviorProvider>();
        }
    }

    /// <summary>
    /// Загрузка правил из профиля существа по индексу ранга.
    /// </summary>
    private void LoadRulesFromProvider()
    {
        if (_self == null) return;
        // 1) Если профиль задан на самом юните — создаём рантайм-копию ScriptableObject, чтобы не делить состояние
        if (_self.BehaviorProfile != null &&
            _self.BehaviorProfile.rangs != null &&
            _self.BehaviorProfile.rangs.Count > 0)
        {
            int idx = Mathf.Clamp(rangIndex, 0, _self.BehaviorProfile.rangs.Count - 1);
            _runtimeProfile = ScriptableObject.Instantiate(_self.BehaviorProfile);
            _runtimeProfile.name = _self.BehaviorProfile.name; // убрать (Clone) в имени
            _rules = _runtimeProfile.rangs[idx].rules;
            return;
        }

        // Профиль обязателен: внешний провайдер по прежнему CreatureSO больше не используется
    }

    /// <summary>
    /// Инициализация триггеров всех правил.
    /// </summary>
    private void InitializeTriggersForAllRules()
    {
        TriggerLogic.InitializeAll(_rules, _self);
    }

    /// <summary>
    /// Готовность раннера к работе: должны быть self и список правил.
    /// </summary>
    private bool IsReady()
    {
        return _self != null && _rules != null;
    }

    /// <summary>
    /// Обходит все правила и запускает их обработку.
    /// </summary>
    private void ProcessAllRules(float deltaTime)
    {
        for (int i = 0; i < _rules.Count; i++)
        {
            var rule = _rules[i];
            if (rule == null) continue;
            ProcessSingleRule(rule, deltaTime);
        }
    }

    /// <summary>
    /// Обработка одного правила: триггеры → условия(self) → цели → применение эффекта.
    /// </summary>
    private void ProcessSingleRule(BehaviorRule rule, float deltaTime)
    {
        if (!CheckTrigger(rule, deltaTime)) return;
        // 1) Проверяем, есть ли условие-перехватчик и выполнены ли все остальные условия
        var changeBehavior = ConditionLogic.TryGetChangeBehaviorCondition(rule, _self);
        if (changeBehavior == null)
        {
            if (!ConditionLogic.AreSelfConditionsMet(rule, _self)) return;
        }

        // 2) Выбираем цель: либо стандартную из правила, либо переопределённую
        var effectiveRule = rule;
        if (changeBehavior != null)
        {
            // Если перехватчик есть, подставляем его целевые поля поверх rule
            effectiveRule = new BehaviorRule
            {
                Triggers = rule.Triggers, // триггеры не меняем
                Conditions = rule.Conditions, // условия оставляем для EvaluateForTarget
                Target = changeBehavior.Target != null ? changeBehavior.Target : rule.Target,
                effect = changeBehavior.effect != EffectId.None ? changeBehavior.effect : rule.effect,
                statistic = rule.statistic,
                value = changeBehavior.value != null ? changeBehavior.value : rule.value
            };
        }

        var targets = TargetLogic.CollectAndFilterTargets(effectiveRule, _self);
        // Обновляем debug-поле текущей цели сразу после выбора целей
        _self.currentTarget = (targets != null && targets.Count > 0) ? targets[0] : null;
        if (targets == null || targets.Count == 0) return;

        DeliverEffectToTargets(effectiveRule, targets);
    }

    /// <summary>
    /// Обёртка проверки срабатывания триггеров для правила.
    /// </summary>
    private bool CheckTrigger(BehaviorRule rule, float deltaTime)
    {
        return TriggerLogic.ShouldFire(rule, deltaTime, _self);
    }

    /// <summary>
    /// Метод оставлен для обратной совместимости. Сейчас логика делегирована в TargetLogic.
    /// </summary>
    private List<Creature> CollectAndFilterTargets(BehaviorRule rule)
    {
        return TargetLogic.CollectAndFilterTargets(rule, _self);
    }

    /// <summary>
    /// Применяет эффект правила по всем выбранным целям.
    /// </summary>
    private void DeliverEffectToTargets(BehaviorRule rule, List<Creature> targets)
    {
        for (int k = 0; k < targets.Count; k++)
        {
            var target = targets[k];
            DeliverEffect(_self, target, rule);
        }
    }

    /// <summary>
    /// Доставка эффекта до конкретной цели (делегируется в EffectLogic).
    /// </summary>
    private void DeliverEffect(Creature self, Creature target, BehaviorRule rule)
    {
        if (rule == null || target == null) return;
        var prefab = _runtimeProfile != null ? _runtimeProfile.spellPrefab : (self.BehaviorProfile != null ? self.BehaviorProfile.spellPrefab : null);
        EffectLogic.Deliver(self, target, rule, prefab);
    }
}
