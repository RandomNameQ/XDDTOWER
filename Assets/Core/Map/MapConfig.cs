using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapConfig", menuName = "ScriptableObjects/MapConfig")]
public class MapConfig : ScriptableObject
{
    public int pathsCount = 3;
    public int pathLength = 10;
    public float zOffsetRange = 2;
    public float distanceBetweenCells = 5f;
    public float pathZSpacing = 6f;
    public int floorNumber;

    public List<int> monsterCells = new();
}
