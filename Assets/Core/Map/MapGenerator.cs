using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Основные настройки")]
    public Config config;
    public Transform startPoint;

    [Header("Префабы")]
    public GameObject cellPrefab;
    public GameObject pathPrefab;

    private GameObject allPathsParent; // Общий родитель для всех путей

    [ShowInInspector]
    private Dictionary<int, List<GameObject>> pathCellsDictionary = new();
    public List<GameObject> allFirstCells = new();

    [System.Serializable]
    public class Config
    {
        public int pathsCount = 1;
        public int pathLength = 10;
        public float zOffsetRange = 0.5f;
        public float distanceBetweenCells = 2f;
        public float pathZSpacing = 3f;
    }

        private void Start()
    {
        if (!ValidateSettings()) return;

        allPathsParent = new GameObject("All_Generated_Paths");
        GenerateMap();
    }

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
            float randomZ = RandomManager.Q.Range(-config.zOffsetRange, config.zOffsetRange);
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
        cell.name = $"Cell_{cellIndex}";
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

        foreach (var cell in allFirstCells)
        {
            ConnectCells(cell, startCell, allPathsParent);
        }

    }
}