using UnityEngine;
using System; // for Action
using System.Collections.Generic; // for Dictionary

// Перечисление типов досок
public enum BoardType
{
    GameBoard,
    PlayerInventory
}

// Статический менеджер для управления всеми досками
public static class BoardManager
{
    private static Dictionary<BoardType, BoardGrid> boards = new Dictionary<BoardType, BoardGrid>();
    
    public static void RegisterBoard(BoardType type, BoardGrid board)
    {
        boards[type] = board;
    }
    
    public static BoardGrid GetBoard(BoardType type)
    {
        if (boards.TryGetValue(type, out BoardGrid board))
            return board;
        return null;
    }
    
    /// <summary>
    /// Получает доску, над которой находится указанная мировая позиция
    /// </summary>
    public static BoardGrid GetBoardAtPosition(Vector3 worldPosition)
    {
        foreach (var board in boards.Values)
        {
            if (board.IsPositionOverBoard(worldPosition))
                return board;
        }
        return null;
    }
}

[RequireComponent(typeof(Grid))]
[DefaultExecutionOrder(-100)]
public class BoardGrid : MonoBehaviour
{
    public BoardType boardType; // Тип доски (игровое поле или инвентарь)
    public int columns = 5;
    public int rows = 3;
    public float cellGap = 0.1f; // Новое поле для контроля зазора между клетками

    private Grid grid;                    // ссылка на компонент Grid
    private bool[,] occupied;             // занятость клеток [x,z]

    // Событие вызывается при любом изменении доски
    public event Action OnBoardChanged;

    private void Awake()
    {
        grid = GetComponent<Grid>();
        if (grid == null)
        {
            Debug.LogError("BoardGrid: Grid component not found on GameObject!");
            enabled = false;
            return;
        }
        occupied = new bool[columns, rows];
        
        // Регистрация в BoardManager
        BoardManager.RegisterBoard(boardType, this);
    }

    // Центр клетки в мировых координатах с учетом зазора
    public Vector3 GetCellCenterWorld(Vector3Int cell)
    {
        Vector3 center = grid.GetCellCenterWorld(cell);
        // Добавляем смещение для учета зазора
        center.x += cell.x * cellGap;
        center.z += cell.z * cellGap;
        return center;
    }

    // Конвертация позиции мира в координаты клетки
    public Vector3Int WorldToCell(Vector3 worldPos) => grid.WorldToCell(worldPos);

    // Проверить, занята ли одна клетка
    public bool IsOccupied(Vector3Int cell)
    {
        if (cell.x < 0 || cell.x >= columns || cell.z < 0 || cell.z >= rows) return false;
        return occupied[cell.x, cell.z];
    }
    
    /// <summary>
    /// Проверяет, находится ли указанная мировая позиция над этой доской
    /// </summary>
    public bool IsPositionOverBoard(Vector3 worldPosition)
    {
        Vector3Int cell = WorldToCell(worldPosition);
        return cell.x >= 0 && cell.x < columns && cell.z >= 0 && cell.z < rows;
    }

    // Проверить, можно ли разместить фигуру sizeX×sizeY c левым-нижним углом в cell
    private bool CanPlace(Vector3Int cell, int sizeX, int sizeY)
    {
        if (cell.x < 0 || cell.z < 0 || cell.x + sizeX > columns || cell.z + sizeY > rows) return false;

        for (int x = cell.x; x < cell.x + sizeX; x++)
            for (int z = cell.z; z < cell.z + sizeY; z++)
                if (occupied[x, z]) return false;

        return true;
    }

    /// <summary>
    /// Проверить, свободна ли область sizeX×sizeY с левым-нижним углом в cell.
    /// </summary>
    public bool IsAreaFree(Vector3Int cell, int sizeX = 1, int sizeY = 1)
    {
        if (cell.x < 0 || cell.z < 0 || cell.x + sizeX > columns || cell.z + sizeY > rows) return false;

        for (int x = cell.x; x < cell.x + sizeX; x++)
            for (int z = cell.z; z < cell.z + sizeY; z++)
                if (occupied[x, z]) return false;

        return true;
    }

    // Попытаться поставить карту любого размера; возвращает true, если успешно
    public bool TryPlaceCard(GameObject card, Vector3Int cell, int sizeX = 1, int sizeY = 1)
    {
        if (!CanPlace(cell, sizeX, sizeY)) return false;

        for (int x = cell.x; x < cell.x + sizeX; x++)
            for (int z = cell.z; z < cell.z + sizeY; z++)
                occupied[x, z] = true;

        card.transform.position = GetAreaCenterWorld(cell, sizeX, sizeY);
        OnBoardChanged?.Invoke();
        return true;
    }

    // Снять карту с клетки (учитывает размер фигуры)
    public void RemoveCard(Vector3Int cell, int sizeX = 1, int sizeY = 1)
    {
        if (cell.x < 0 || cell.z < 0 || cell.x + sizeX > columns || cell.z + sizeY > rows) return;

        for (int x = cell.x; x < cell.x + sizeX; x++)
            for (int z = cell.z; z < cell.z + sizeY; z++)
                occupied[x, z] = false;

        OnBoardChanged?.Invoke();
    }

    /// <summary>
    /// Находит свободную позицию на доске для размещения объекта заданного размера
    /// </summary>
    /// <param name="sizeX">Ширина объекта</param>
    /// <param name="sizeY">Высота объекта</param>
    /// <returns>Координаты левого нижнего угла свободной области или Vector3Int.one * -1, если свободного места нет</returns>
    public Vector3Int FindFreePosition(int sizeX, int sizeY)
    {
        // Перебираем все возможные позиции на доске
        for (int x = 0; x < columns; x++)
        {
            for (int z = 0; z < rows; z++)
            {
                Vector3Int cell = new Vector3Int(x, 0, z);
                
                // Проверяем, свободна ли область нужного размера
                if (IsAreaFree(cell, sizeX, sizeY))
                {
                    return cell; // Возвращаем найденную свободную позицию
                }
            }
        }
        
        // Если свободного места нет, возвращаем специальное значение
        return Vector3Int.one * -1;
    }

    /// <summary>
    /// Находит свободные позиции на доске для размещения объекта заданного размера
    /// </summary>
    /// <param name="sizeX">Ширина объекта</param>
    /// <param name="sizeY">Высота объекта</param>
    /// <returns>Список всех клеток, которые будет занимать объект, или пустой список, если свободного места нет</returns>
    public List<Vector3Int> FindFreePositions(int sizeX, int sizeY)
    {
        // Перебираем все возможные позиции на доске
        for (int x = 0; x < columns; x++)
        {
            for (int z = 0; z < rows; z++)
            {
                Vector3Int cell = new Vector3Int(x, 0, z);
                
                // Проверяем, свободна ли область нужного размера
                if (IsAreaFree(cell, sizeX, sizeY))
                {
                    // Создаем список всех клеток, которые будет занимать карта
                    List<Vector3Int> positions = new List<Vector3Int>();
                    for (int dx = 0; dx < sizeX; dx++)
                    {
                        for (int dz = 0; dz < sizeY; dz++)
                        {
                            positions.Add(new Vector3Int(cell.x + dx, 0, cell.z + dz));
                        }
                    }
                    return positions; // Возвращаем найденные позиции
                }
            }
        }
        
        // Если свободного места нет, возвращаем пустой список
        return new List<Vector3Int>();
    }

    /// <summary>
    /// Попытаться поставить карту на указанные позиции
    /// </summary>
    /// <param name="card">Игровой объект карты</param>
    /// <param name="positions">Список позиций, которые должна занимать карта</param>
    /// <returns>true, если размещение успешно</returns>
    public bool TryPlaceCardAtPositions(GameObject card, List<Vector3Int> positions)
    {
        if (positions == null || positions.Count == 0) return false;
        
        // Проверяем, что все позиции свободны и в пределах доски
        foreach (Vector3Int pos in positions)
        {
            if (pos.x < 0 || pos.x >= columns || pos.z < 0 || pos.z >= rows || occupied[pos.x, pos.z])
                return false;
        }
        
        // Помечаем все позиции как занятые
        foreach (Vector3Int pos in positions)
        {
            occupied[pos.x, pos.z] = true;
        }
        
        // Вычисляем центр всех клеток
        Vector3 center = CalculateCenterOfPositions(positions);
        
        // Устанавливаем позицию объекта карты
        card.transform.position = center;

        if (card.TryGetComponent<GridPlacement>(out var gp))
        {
            int minX = positions[0].x;
            int minZ = positions[0].z;
            foreach (var p in positions)
            {
                if (p.x < minX) minX = p.x;
                if (p.z < minZ) minZ = p.z;
            }
            Vector3Int origin = new Vector3Int(minX, 0, minZ);
            gp.ForcePlace(this, origin);
        }

        // Вызываем событие об изменении доски
        OnBoardChanged?.Invoke();
        
        return true;
    }

    /// <summary>
    /// Вычисляет центр группы клеток в мировых координатах с учетом зазоров
    /// </summary>
    /// <param name="positions">Список позиций клеток</param>
    /// <returns>Центр группы клеток в мировых координатах</returns>
    private Vector3 CalculateCenterOfPositions(List<Vector3Int> positions)
    {
        if (positions == null || positions.Count == 0)
            return Vector3.zero;
        
        // Находим минимальные и максимальные координаты
        int minX = int.MaxValue, minZ = int.MaxValue;
        int maxX = int.MinValue, maxZ = int.MinValue;
        
        foreach (Vector3Int pos in positions)
        {
            minX = Mathf.Min(minX, pos.x);
            minZ = Mathf.Min(minZ, pos.z);
            maxX = Mathf.Max(maxX, pos.x);
            maxZ = Mathf.Max(maxZ, pos.z);
        }
        
        // Вычисляем центр как среднее между минимальными и максимальными координатами
        Vector3 min = GetCellCenterWorld(new Vector3Int(minX, 0, minZ));
        Vector3 max = GetCellCenterWorld(new Vector3Int(maxX, 0, maxZ));
        
        return (min + max) * 0.5f;
    }

    // Центр прямоугольной области sizeX×sizeY в мировых координатах с учетом зазоров
    public Vector3 GetAreaCenterWorld(Vector3Int origin, int sizeX, int sizeY)
    {
        Vector3 min = GetCellCenterWorld(origin);
        Vector3 max = GetCellCenterWorld(new Vector3Int(origin.x + sizeX - 1, 0, origin.z + sizeY - 1));
        // Учитываем зазоры при расчете центра области
        return (min + max) * 0.5f;
    }
    
    /// <summary>
    /// Попытаться переместить карту с текущей доски на другую доску
    /// </summary>
    /// <param name="card">Игровой объект карты</param>
    /// <param name="sourceCell">Клетка на текущей доске</param>
    /// <param name="sizeX">Ширина карты</param>
    /// <param name="sizeY">Высота карты</param>
    /// <param name="targetBoardType">Тип целевой доски</param>
    /// <param name="targetCell">Клетка на целевой доске</param>
    /// <returns>true, если перемещение успешно</returns>
    public bool TryMoveCardToBoard(GameObject card, Vector3Int sourceCell, int sizeX, int sizeY, 
                                 BoardType targetBoardType, Vector3Int targetCell)
    {
        // Получаем целевую доску
        BoardGrid targetBoard = BoardManager.GetBoard(targetBoardType);
        if (targetBoard == null || targetBoard == this) return false;
        
        // Проверяем, можно ли разместить на целевой доске
        if (!targetBoard.IsAreaFree(targetCell, sizeX, sizeY)) return false;
        
        // Удаляем с текущей доски
        RemoveCard(sourceCell, sizeX, sizeY);
        
        // Добавляем на целевую доску
        return targetBoard.TryPlaceCard(card, targetCell, sizeX, sizeY);
    }
}
