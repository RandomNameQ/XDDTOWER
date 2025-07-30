using UnityEngine;

[RequireComponent(typeof(BoardGrid))]
public class BoardVisualizer : MonoBehaviour
{
    private BoardGrid board;
    public GameObject indicatorPrefab;
    private GameObject[,] indicators;

    private void Start()
    {
        // Если не назначили вручную – пробуем найти на том же объекте
        if (board == null) board = GetComponent<BoardGrid>();
        if (board == null)
        {
            Debug.LogError("BoardVisualizer: BoardGrid component not found!");
            enabled = false;
            return;
        }

        // Создаём индикаторы
        indicators = new GameObject[board.columns, board.rows];

        for (int x = 0; x < board.columns; x++)
            for (int z = 0; z < board.rows; z++)
            {
                Vector3Int cell = new(x, 0, z);
                Vector3 pos = board.GetCellCenterWorld(cell);
                indicators[x, z] = Instantiate(indicatorPrefab, pos, Quaternion.identity, transform);
            }

        UpdateAllIndicators();
        board.OnBoardChanged += UpdateAllIndicators;
    }

    private void OnDestroy()
    {
        if (board != null) board.OnBoardChanged -= UpdateAllIndicators;
    }

    // Обновить все плашки по текущему состоянию доски
    private void UpdateAllIndicators()
    {
        for (int x = 0; x < board.columns; x++)
            for (int z = 0; z < board.rows; z++)
            {
                Vector3Int cell = new(x, 0, z);
                UpdateIndicator(cell, board.IsOccupied(cell));
            }
    }

    private void UpdateIndicator(Vector3Int cell, bool occupied)
    {
        var rend = indicators[cell.x, cell.z].GetComponent<Renderer>();
        if (rend != null)
            rend.material.color = occupied ? Color.red : Color.green;
    }
}
