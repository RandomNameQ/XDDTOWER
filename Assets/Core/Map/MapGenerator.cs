using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Основные настройки")]
    public MapConfig config;
    public Transform startPoint;

    [Header("Префабы")]
    public GameObject cellPrefab;
    public GameObject pathPrefab;

    private GameObject allPathsParent; // Общий родитель для всех путей

    [ShowInInspector]
    private Dictionary<int, List<GameObject>> pathCellsDictionary = new();
    public List<GameObject> allFirstCells = new();



    // private void Start()
    // {
    //     if (!ValidateSettings()) return;

    //     allPathsParent = new GameObject("All_Generated_Paths");
    //     GenerateMap();
    // }

    [Button]
    public void GenerateMap()
    {
        if (!ValidateSettings())
        {
            Debug.LogWarning("Map generation aborted: invalid settings");
            return;
        }

        ClearPreviousMap();
        InitializeMapParent();
        GenerateAllPaths();
        CreateStartCell();

        var spawnContent = GetComponent<SpawnContentInMap>();
        spawnContent.GenerateContent();

    }

    private void ClearPreviousMap()
    {
        allFirstCells.Clear();
        if (allPathsParent != null)
        {
            DestroyImmediate(allPathsParent);
        }
    }

    private void InitializeMapParent()
    {
        allPathsParent = new GameObject("All_Generated_Paths");
        allPathsParent.transform.SetParent(transform);
    }

    private bool ValidateSettings()
    {
        if (startPoint == null) Debug.LogError("Не назначен стартовый объект!");
        else if (cellPrefab == null) Debug.LogError("Не назначен префаб ячейки!");
        else if (config.pathLength < 2) Debug.LogError("Нужно минимум 2 ячейки!");
        else if (config.pathsCount < 1) Debug.LogError("Нужно минимум 1 путь!");
        else return true;

        return false;
    }

    private void GenerateAllPaths()
    {
        Vector3 basePosition = startPoint.position;

        for (int pathIndex = 0; pathIndex < config.pathsCount; pathIndex++)
        {
            // Создаём отдельный родитель для пути
            GameObject pathParent = new GameObject($"Path_{pathIndex}");
            pathParent.transform.SetParent(allPathsParent.transform);

            // Смещаем стартовую позицию пути по Z
            Vector3 pathStartPos = basePosition + new Vector3(
                0f,
                0f,
                pathIndex * config.pathZSpacing // Офсет между путями по Z
            );

            GenerateSinglePath(pathStartPos, pathIndex, pathParent);
        }
    }

    private void GenerateSinglePath(Vector3 startPos, int pathIndex, GameObject pathParent)
    {
        // Инициализируем список для этого пути
        pathCellsDictionary[pathIndex] = new List<GameObject>();

        // Первая клетка (без смещения)
        Vector3 basePos = startPos;
        Vector3 currentPos = basePos;
        GameObject prevCell = SpawnCell(currentPos, pathIndex, 0, pathParent);
        pathCellsDictionary[pathIndex].Add(prevCell);

        for (int cellIndex = 1; cellIndex < config.pathLength; cellIndex++)
        {
            // Вычисляем базовую позицию по прямой линии
            basePos += new Vector3(config.distanceBetweenCells, 0f, 0f);

            // Генерируем случайное смещение по Z для текущей ячейки
            float randomZ = RandomManager.Instance.Range(-config.zOffsetRange, config.zOffsetRange);
            currentPos = basePos + new Vector3(0f, 0f, randomZ);

            GameObject newCell = SpawnCell(currentPos, pathIndex, cellIndex, pathParent);
            pathCellsDictionary[pathIndex].Add(newCell);
            ConnectCells(prevCell, newCell, pathParent);
            prevCell = newCell;
        }
    }

    private GameObject SpawnCell(Vector3 position, int pathIndex, int cellIndex, GameObject parent)
    {
        GameObject cell = Instantiate(cellPrefab, position, Quaternion.identity, parent.transform);
        cell.GetComponent<CellData>().cellOrder = cellIndex + 1;
        cell.name = $"{cellIndex + 1}";

        if (config.monsterCells.Contains(cellIndex + 1))
        {
            cell.GetComponent<CellData>().cellVariant = CellData.CellVariant.Monster;
        }
        else
            cell.GetComponent<CellData>().cellVariant = CellData.CellVariant.Shop;

        return cell;
    }

    private void ConnectCells(GameObject from, GameObject to, GameObject parent)
    {
        if (pathPrefab == null) return;

        GameObject connection = Instantiate(pathPrefab, parent.transform);
        connection.name = $"Connection_{from.name}_to_{to.name}";

        LineRenderer line = connection.GetComponent<LineRenderer>();
        if (line == null) return;

        line.positionCount = 2;
        line.SetPosition(0, from.transform.position);
        line.SetPosition(1, to.transform.position);

        // Обновляем данные в CellData
        CellData fromCellData = from.GetComponent<CellData>();
        CellData toCellData = to.GetComponent<CellData>();

        if (fromCellData != null && toCellData != null)
        {
            // Добавляем путь в список путей обеих клеток
            fromCellData.paths.Add(connection);
            toCellData.paths.Add(connection);

            // Добавляем клетки в список соединенных клеток друг друга
            if (!fromCellData.connectedCells.Contains(toCellData))
                fromCellData.connectedCells.Add(toCellData);

            if (!toCellData.connectedCells.Contains(fromCellData))
                toCellData.connectedCells.Add(fromCellData);
        }
    }
    public void CreateStartCell()
    {
        allFirstCells.Clear();
        foreach (var cell in pathCellsDictionary)
        {
            allFirstCells.Add(cell.Value[0]);
            continue;
        }
        var centerZ = allFirstCells.Average(cell => cell.transform.position.z);
        var posForStartCell = new Vector3(allFirstCells[0].transform.position.x - 10, 0.5f, centerZ);
        var startCell = Instantiate(cellPrefab, posForStartCell, Quaternion.identity, allPathsParent.transform);
        startCell.GetComponent<CellData>().cellVariant = CellData.CellVariant.Start;
        
        foreach (var cell in allFirstCells)
        {
            ConnectCells(startCell, cell, allPathsParent);
        }
    }
}