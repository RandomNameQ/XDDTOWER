## XDDTOWER — Core Systems Guide ⚙️🧩

Краткое руководство по трем подсистемам проекта: `@BoardV2`, `@_BehaviourSystem`, `@Creatures`.
Эмодзи помогают быстрее сканировать текст и найти нужные разделы.

---

## 🧩 BoardV2 — гриды, столы и перетаскивание

**Что это**: модуль для 3D-«столов» с ячейками, на которые можно класть и перетаскивать объекты (карты/юниты).

**Ключевые классы**
- `BoardGridV2` — сам стол: размер, форма, ячейки, размещение объектов
- `BoardCell` — визуал и состояние ячейки
- `PlaceableObject` — объект с размерами в ячейках
- `Draggable3DV2` — drag & drop по плоскости стола с превью валидности
- `BoardRegistryV2` — реестр столов и поиск стола по позиции
- `AutoPlacementService` — авто-расстановка по списку столов
- `ShopBoardV2` — наследник стола для магазина (логика «можно ли взять?»)
- `BoardPainterV2` — «пейнт» включенных ячеек прямо в рантайме
- `CellEffectsMapV2` — теги/эффекты на клетках (хуки при размещении)
- `CreatureFactory` — упрощённый инстанс юнитов из `CreatureBehaviorProfileSO`

**Быстрый старт 🚀**
1) На сцене создайте несколько столов `BoardGridV2` и выберите `BoardTypeV2` (например: `Battle`, `Inventory`, `Trash`, `Shop`).
2) В инспекторе стола задайте: `columns/rows`, `cellSize`, `cellGap`, `shapeType`, `cellPrefab`, `cellsParent/objectsParent`.
3) На префаб карты/юнита добавьте: `BoxCollider`, `PlaceableObject` (размер в клетках), `Draggable3DV2`.
4) Включите новый Unity Input System и убедитесь, что `Camera.main` доступна (или задайте `targetCamera`).
5) Запустите игру — перетаскивайте объекты между столами с подсветкой валидности.

**Drag & Drop как это работает 🧠**
- `Draggable3DV2` ловит нажатие на свой коллайдер, отсоединяет объект от исходного стола, двигает по плоскости.
- Пока курсор над столом — считается «снапнутая» ячейка-оригин, рисуется превью (валидно/не валидно).
- Отпускание: если цель валидна — `BoardGridV2.TryPlace(...)`, иначе возврат на исходный стол.

**Авто-расстановка 🔁**
```csharp
using Core.BoardV2;

AutoPlacementService.TryPlaceInOrder(
    placeable,
    new[] { BoardTypeV2.Inventory, BoardTypeV2.Battle, BoardTypeV2.Trash }
);
```

**Расширение 🛠️**
- Проверка покупки в магазине: наследуйте `ShopBoardV2` и переопределите `CanPickUp`.
```csharp
using Core.BoardV2;

public class MyShopBoard : ShopBoardV2
{
    public override bool CanPickUp(PlaceableObject obj)
    {
        // ваша логика списания монет/разрешений
        return PlayerCoins >= defaultPrice;
    }
}
```
- Теги/эффекты клетки: наследуйте `CellEffectsMapV2` и обработайте `ApplyTags(...)`.
- Подписка на размещение: `board.OnObjectPlaced += (obj, origin) => { ... };`
- Свои формы: `BoardShapeType.CustomMask` + инструмент `BoardPainterV2` для включения/выключения клеток.

**Полезные советы 💡**
- Если превью не светится/перетаскивание не идёт — проверьте `BoxCollider` и `targetCamera` на объекте.
- Без `cellPrefab` ячейки не создадутся.
- `BoardRegistryV2.GetBoardAtPosition(worldPos)` помогает понять, над каким столом сейчас курсор.

---

## 🧠 BehaviourSystem — правила поведения юнитов

**Что это**: рантайм-оркестратор «Триггеры → Условия → Цели → Доставка → Эффект».

**Ключевые классы**
- `BehaviorRunner` — компонент на юните, выполняет правила
- (Контракты) `ICreatureBehaviorProvider`, `BehaviorRule`, `Condition`, `Target`, `Effect` — поставщик и элементы правил
- Интеграция с снарядами: если у юнита в `CreatureBehaviorProfileSO` задан `spellPrefab` с `ProjectileBase`, эффект доставится снарядом; иначе — мгновенно

**Как подключить ⚙️**
1) На префаб юнита добавьте `Creature` и укажите его `CreatureBehaviorProfileSO`.
2) `Creature` автоматически добавит `BehaviorRunner` на `Awake()`.
3) На самом юните или на сцене может быть компонент, реализующий `ICreatureBehaviorProvider` (он вернёт список `BehaviorRule` для пары `(CreatureBehaviorProfileSO, rangIndex)`), но по умолчанию правила читаются прямо из профиля.
4) Опционально: в `CreatureBehaviorProfileSO.spellPrefab` задайте префаб с `ProjectileBase` — тогда эффект будет доставляться по попаданию.

**Пример провайдера правил 🔌**
```csharp
using System.Collections.Generic;

public class SimpleProvider : UnityEngine.MonoBehaviour, ICreatureBehaviorProvider
{
    public List<BehaviorRule> GetRules(CreatureBehaviorProfileSO profile, int rangIndex)
    {
        // верните набор правил под конкретного юнита и ранг
        return new List<BehaviorRule>();
    }
}
```

**Расширение 🛠️**
- Собственные триггеры (таймер, попадание, событие и т.д.).
- Условия для себя и для целей (например: «цель в радиусе», «меньше 50% HP»).
- Селекторы целей (ближайший враг, соседи по борду и т.д.).
- Эффекты (урон, баффы, дебаффы, спавн и пр.).

---

## 🐾 Creatures — данные и носители юнитов

**Что это**: данные `ScriptableObject` юнитов и компонент-носитель на сцене.

**Ключевые элементы**
- `CreatureBehaviorProfileSO` — иконка, размер в клетках `size`, `raceId`, список `rangs` (внутри — `rules` и `maxHealth`), `spellPrefab`.
- `Creature` — компонент на объекте сцены: хранит `behaviorProfile`, команду, здоровье, регистрируется в `Creature.All`, гарантирует наличие `BehaviorRunner`.
- `ICreatureComponent`/`CreatureLink` — лёгкая связка объекта и `CreatureBehaviorProfileSO`.
- `IInitFromSO` — контракт для инициализации из профиля (`Creature.InitDataSO()` масштабирует объект и задаёт спрайт).
- `CreatureFactory` — хелпер, чтобы инстанснуть объект из `CreatureBehaviorProfileSO` (если на префабе нет `ICreatureComponent`, добавит `CreatureLink`).

**Использование с BoardV2 🧩**
1) Префаб юнита содержит: `Creature` (+ его `CreatureBehaviorProfileSO`), `PlaceableObject`, `Draggable3DV2`, `BoxCollider`.
2) Тогда юнита можно перетаскивать по столам и он будет стрелять/кастовать по своим правилам.
3) Если инстансите через `CreatureFactory.InstantiateCreature(so)`, убедитесь, что на результате есть `Creature` для работы поведения (или добавьте его отдельно).

**Мини-пример создания юнита 📦**
```csharp
using Core.BoardV2;
using UnityEngine;

public class SpawnExample : MonoBehaviour
{
    public CreatureBehaviorProfileSO profile;
    public BoardGridV2 battle;

    void Start()
    {
        var go = CreatureFactory.InstantiateCreature(profile);
        go.AddComponent<BoxCollider>();
        go.AddComponent<PlaceableObject>();
        go.AddComponent<Draggable3DV2>();
        if (go.GetComponent<Creature>() == null) go.AddComponent<Creature>();

        var placeable = go.GetComponent<PlaceableObject>();
        var pos = battle.FindFirstFreePosition(placeable.SizeX, placeable.SizeZ);
        battle.TryPlace(placeable, pos);
    }
}
```

---

## ❓ Траблшутинг
- **Нет подсветки/драга**: проверьте `BoxCollider` на объекте и `targetCamera` в `Draggable3DV2`.
- **Ячейки не создаются**: проверьте `cellPrefab` у `BoardGridV2`.
- **Не находится стол под курсором**: используйте `BoardRegistryV2.GetBoardAtPosition(worldPos)` для отладки.
- **Эффект не летит снарядом**: убедитесь, что `CreatureBehaviorProfileSO.spellPrefab` содержит `ProjectileBase`.

---

## 📚 Полезные ссылки на файлы
- BoardV2: `Assets/Core/BoardV2/BoardGridV2.cs`, `PlaceableObject.cs`, `Draggable3DV2.cs`, `BoardRegistryV2.cs`, `ShopBoardV2.cs`, `AutoPlacementService.cs`, `CellEffectsV2.cs`, `BoardPainterV2.cs`
- BehaviourSystem: `Assets/Core/_BehaviourSystem/BehaviorRunner.cs`, `Assets/Core/_BehaviourSystem/Creature.cs`
- Creatures: `CreatureLink.cs`, `ICreatureComponent.cs`, `IInitFromSO.cs`, `Assets/Core/BoardV2/CreatureFactory.cs`

---

## 🧭 Принципы расширения
- Делите ответственность: визуал ячейки — в `BoardCell`, логика валидности — в `BoardGridV2`.
- Сигналы/события: используйте `OnObjectPlaced` и свои компоненты-слушатели.
- Поведение — конфигурируемо: провайдер правил может отдавать правила по `(CreatureBehaviorProfileSO, rangIndex)`, но обычно они читаются напрямую из профиля.

Готово. Если нужно — добавлю иллюстрации, примеры конкретных `Rule/Trigger/Effect` и кнопки в инспекторе для снапшотов борда.


