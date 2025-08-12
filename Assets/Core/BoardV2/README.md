Board V2 — гриды, столы, перетаскивание карт и сохранения

Новая независимая система грид-столов и перетаскивания для 3D-объектов.

Быстрый старт
- Создай на сцене столы: `BoardGridV2` для `Battle`, `Inventory`, `Trash` и `ShopBoardV2` для `Shop`.
- На столах укажи: `columns/rows`, `cellSize`, `cellGap=0.1`, `shapeType`, `cellPrefab`, `cellsParent`.
- На префаб карты добавь: `BoxCollider`, `PlaceableObject` (SizeX/SizeZ), `Draggable3DV2`.
- Включи новый Input System в Project Settings.
- Запусти игру и перетаскивай карты между столами.

Функции
- Формы столов: прямоугольник/квадрат/треугольники/кастом-маска.
- Подсветка валидности при драге, перенос между столами, падение в `Trash` вне столов.
- Блокировки/разрушения ячеек, эффекты на клетках, соседи у карты.
- Сохранение/загрузка конфигурации стола и юнитов через `BoardSaveSO` + `BoardSaveLoader`.

Сохранения
 - Снимок борда: `BoardSnapshotSO` (Create → BoardV2 → Board Snapshot).
 - Чтобы сохранить: открой `BoardSnapshotSO`, перетащи объект грида из сцены в поле редактора и нажми кнопку сохранения.
 - Чтобы загрузить: добавь `BoardSnapshotApplier` на объект со `BoardGridV2`, укажи `snapshot`, нажми `Apply()` (через кнопку в вызывающем коде или сделаю кнопку в инспекторе по запросу). При включённом `respawnUnits` юниты будут пересозданы по снимку.

Магазин
- Переопредели `ShopBoardV2.CanPickUp` для проверки покупки.

Авто-расстановка
```csharp
AutoPlacementService.TryPlaceInOrder(placeable, new[] { BoardTypeV2.Inventory, BoardTypeV2.Battle, BoardTypeV2.Trash });
```

Полезные классы
- `BoardGridV2`, `BoardCell`, `PlaceableObject`, `Draggable3DV2`, `BoardRegistryV2`, `ShopBoardV2`, `BoardPainterV2`, `CellEffectsMapV2`.
- Сохранения: `BoardSaveSO`, `BoardSaveLoader`.
- Фабрика юнитов: `CreatureFactory`.

Частые проблемы
- Нет `MainCamera` — назначь `targetCamera` у `Draggable3DV2`.
- Нет `cellPrefab` — ячейки не появятся.
- На карте нет коллайдера — драг не начнётся.
- Отключи старые `GridPlacement/Drag3dObject/BoardGrid`.


