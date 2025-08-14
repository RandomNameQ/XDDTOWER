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



}
