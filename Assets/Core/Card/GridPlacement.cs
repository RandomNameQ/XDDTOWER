using UnityEngine;

/// <summary>
/// Централизует логику взаимодействия фигуры с игровой доской.
/// </summary>
public class GridPlacement : MonoBehaviour
{
    private int sizeX = 1;
    private int sizeZ = 1;

    [SerializeField] private BoardGrid _board;
    [SerializeField] private BoardType _boardType = BoardType.GameBoard;

    private Vector3 _originalPosition;
    private Vector3Int _placedCell = new(-1, 0, -1);
    [SerializeField, Tooltip("Текущая клетка (левая-нижняя), занимаемая объектом.")]
    private Vector3Int inspectorPlacedCell = new(-1, 0, -1); // отображается в инспекторе

    // Сохранение исходного состояния для восстановления при неудачном перемещении
    private BoardGrid _originalBoard;
    private Vector3Int _originalCell;

    // Логика по списку занятых клеток удалена по требованию

    private Vector3Int _currentCell = new(-1, 0, -1); // клетка под фигурой во время перетаскивания

    private float _boardPlaneY;

    public int SizeX => sizeX;
    public int SizeZ => sizeZ;
    public float BoardPlaneY => _boardPlaneY;
    /// <summary>
    /// Левая-нижняя клетка, в которой сейчас размещён объект. Если не размещён – x < 0.
    /// </summary>
    public Vector3Int PlacedCell => _placedCell;

    /// <summary>
    /// Текущая доска, с которой взаимодействует объект
    /// </summary>
    public BoardGrid CurrentBoard => _board;

    /// <summary>
    /// Позволяет задать размер фигуры из другого скрипта (например PawnCreature).
    /// </summary>
    public void SetSize(int x, int z)
    {
        sizeX = Mathf.Max(1, x);
        sizeZ = Mathf.Max(1, z);
        // Пропорционально меняем локальный масштаб, чтобы визуально соответствовать размеру клетки
        transform.localScale = new Vector3(sizeX, 1, sizeZ);
    }

    /// <summary>
    /// Устанавливает текущую доску для этого объекта
    /// </summary>
    public void SetBoard(BoardGrid board)
    {
        _board = board;
        _boardPlaneY = _board.transform.position.y;
    }

    #region UNITY

    /// <summary>
    /// Удаляет карту с доски и освобождает занимаемые клетки.
    /// </summary>
    public void RemoveCard()
    {
        if (_placedCell.x >= 0 && _board != null)
        {
            _board.RemoveCard(_placedCell, sizeX, sizeZ);
            _placedCell = new Vector3Int(-1, 0, -1);
            inspectorPlacedCell = new Vector3Int(-1, 0, -1);
        }
    }
    private void Awake()
    {
        // Сначала пытаемся использовать доску, указанную в инспекторе
        if (_board == null)
        {
            // Затем пытаемся получить доску из BoardManager по типу
            _board = BoardManager.GetBoard(_boardType);

            // Если всё еще нет, ищем любую доску в сцене
            if (_board == null)
                _board = FindAnyObjectByType<BoardGrid>();
        }

        if (_board == null)
        {
            Debug.LogError($"{nameof(GridPlacement)}: BoardGrid not found in scene!");
            enabled = false;
            return;
        }

        _boardPlaneY = _board.transform.position.y;

        // Если размер не задан явно, берём из масштаба
        if (sizeX <= 0) sizeX = Mathf.RoundToInt(transform.localScale.x);
        if (sizeZ <= 0) sizeZ = Mathf.RoundToInt(transform.localScale.z);

        // Подгоняем масштаб, чтобы точно соответствовать клеткам
        transform.localScale = new Vector3(sizeX, 1, sizeZ);
    }
    #endregion

    #region DRAG LIFECYCLE API
    public void StartDrag()
    {
        _originalPosition = transform.position;
        _originalBoard = _board;
        _originalCell = _placedCell;

        // Если уже стояли – освобождаем клетки
        if (_placedCell.x >= 0)
        {
            _board.RemoveCard(_placedCell, sizeX, sizeZ);
            inspectorPlacedCell = new Vector3Int(-1, 0, -1);
            _placedCell = new(-1, 0, -1);
        }
    }

    /// <summary>
    /// Возвращает позицию, привязанную к гриду с учётом границ.
    /// </summary>
    public Vector3 GetSnappedPosition(Vector3 targetWorldPos)
    {
        if (_board == null) return targetWorldPos;
        return GetSnappedPositionOnBoard(targetWorldPos, _board);
    }

    /// <summary>
    /// Возвращает позицию, привязанную к гриду указанной доски с учётом границ.
    /// </summary>
    public Vector3 GetSnappedPositionOnBoard(Vector3 targetWorldPos, BoardGrid targetBoard)
    {
        if (targetBoard == null) return targetWorldPos;

        Vector3Int cell = targetBoard.WorldToCell(targetWorldPos);

        cell.x -= Mathf.FloorToInt(sizeX * 0.5f);
        cell.z -= Mathf.FloorToInt(sizeZ * 0.5f);

        cell.x = Mathf.Clamp(cell.x, 0, targetBoard.columns - sizeX);
        cell.z = Mathf.Clamp(cell.z, 0, targetBoard.rows - sizeZ);

        // Если область свободна – используем её, иначе оставляем прежнюю позицию
        if (targetBoard.IsAreaFree(cell, sizeX, sizeZ))
        {
            // Запоминаем последнюю корректную клетку только если это текущая доска
            if (targetBoard == _board)
            {
                _currentCell = cell;
                inspectorPlacedCell = cell;
            }
            return targetBoard.GetAreaCenterWorld(cell, sizeX, sizeZ);
        }

        // Область занята – не меняем позицию (визуально объект «отталкивается»)
        return transform.position;
    }

    public void EndDrag()
    {
        EndDrag(transform.position);
    }

    /// <summary>
    /// Завершает перетаскивание с указанной позицией курсора
    /// </summary>
    public void EndDrag(Vector3 dropPosition)
    {
        if (_board == null) return;

        // Проверяем, находимся ли мы над другой доской
        BoardGrid targetBoard = BoardManager.GetBoardAtPosition(dropPosition);
        if (targetBoard != null && targetBoard != _board)
        {
            // Перемещаем на другую доску
            TryMoveToAnotherBoard(targetBoard, dropPosition);
            return;
        }

        // Стандартная логика размещения на текущей доске
        Vector3Int cell = _currentCell;
        if (cell.x < 0) // если по какой-то причине не был рассчитан
        {
            cell = _board.WorldToCell(transform.position);
            cell.x -= Mathf.FloorToInt(sizeX * 0.5f);
            cell.z -= Mathf.FloorToInt(sizeZ * 0.5f);
        }

        if (_board.TryPlaceCard(gameObject, cell, sizeX, sizeZ))
        {
            _placedCell = cell;
            inspectorPlacedCell = cell;
        }
        else
        {
            // Возврат в исходную точку, если разместить не удалось
            transform.position = _originalPosition;

            // Восстанавливаем занятость клеток на исходной доске, если они были заняты
            if (_originalCell.x >= 0 && _originalBoard != null)
            {
                _originalBoard.TryPlaceCard(gameObject, _originalCell, sizeX, sizeZ);
                _board = _originalBoard;
                _placedCell = _originalCell;
                inspectorPlacedCell = _originalCell;
            }
        }
    }

    /// <summary>
    /// Пытается переместить карту на другую доску
    /// </summary>
    private bool TryMoveToAnotherBoard(BoardGrid targetBoard, Vector3 dropPosition)
    {
        // Получаем клетку на целевой доске
        Vector3Int targetCell = targetBoard.WorldToCell(dropPosition);
        targetCell.x -= Mathf.FloorToInt(sizeX * 0.5f);
        targetCell.z -= Mathf.FloorToInt(sizeZ * 0.5f);

        // Ограничиваем в пределах целевой доски
        targetCell.x = Mathf.Clamp(targetCell.x, 0, targetBoard.columns - sizeX);
        targetCell.z = Mathf.Clamp(targetCell.z, 0, targetBoard.rows - sizeZ);

        // Проверяем, свободна ли целевая область
        if (!targetBoard.IsAreaFree(targetCell, sizeX, sizeZ))
        {
            // Возвращаем в исходное положение при неудаче
            transform.position = _originalPosition;

            // Восстанавливаем занятость клеток на исходной доске, если они были заняты
            if (_originalCell.x >= 0 && _originalBoard != null)
            {
                _originalBoard.TryPlaceCard(gameObject, _originalCell, sizeX, sizeZ);
                _board = _originalBoard;
                _placedCell = _originalCell;
                inspectorPlacedCell = _originalCell;
            }

            return false;
        }

        // Если у нас была позиция на предыдущей доске, используем TryMoveCardToBoard
        if (_originalCell.x >= 0 && _originalBoard != null)
        {
            if (_originalBoard.TryMoveCardToBoard(gameObject, _originalCell, sizeX, sizeZ, targetBoard.boardType, targetCell))
            {
                // Обновляем ссылку на текущую доску
                SetBoard(targetBoard);
                _placedCell = targetCell;
                inspectorPlacedCell = targetCell;
                return true;
            }
            else
            {
                // Восстанавливаем занятость клеток на исходной доске при неудаче
                transform.position = _originalPosition;
                _originalBoard.TryPlaceCard(gameObject, _originalCell, sizeX, sizeZ);
                _board = _originalBoard;
                _placedCell = _originalCell;
                inspectorPlacedCell = _originalCell;
                return false;
            }
        }
        else
        {
            // Если не было позиции на предыдущей доске, просто размещаем на новой
            if (targetBoard.TryPlaceCard(gameObject, targetCell, sizeX, sizeZ))
            {
                // Обновляем ссылку на текущую доску
                SetBoard(targetBoard);
                _placedCell = targetCell;
                inspectorPlacedCell = targetCell;
                return true;
            }
            else
            {
                // Возвращаем в исходное положение при неудаче
                transform.position = _originalPosition;
                return false;
            }
        }
    }
    #endregion

    public void ForcePlace(BoardGrid board, Vector3Int cell)
    {
        SetBoard(board);
        _placedCell = cell;
        inspectorPlacedCell = cell;
        _currentCell = cell;
    }
    // Логика по списку занятых клеток удалена по требованию
}