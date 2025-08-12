using System;
using System.Collections.Generic;
using UnityEngine;
 

namespace Core.BoardV2.Snapshots
{
    [CreateAssetMenu(fileName = "BoardSnapshotSO", menuName = "BoardV2/Board Snapshot", order = 0)]
    public class BoardSnapshotSO : ScriptableObject
    {
        [Header("Префаб юнита для воссоздания")]
        public GameObject unitPrefab;

        [Serializable]
        public class UnitEntry
        {
            public ScriptableObject creature;
            public Vector2Int origin;
            public int sizeX = 1;
            public int sizeZ = 1;
        }

        [Header("Снимок борда")]
        public int columns;
        public int rows;
        public List<Vector2Int> lockedCells = new List<Vector2Int>();
        public List<Vector2Int> occupiedCells = new List<Vector2Int>();

        [Header("Снимок юнитов")]
        public List<UnitEntry> units = new List<UnitEntry>();

        public void CaptureFromGrid(BoardGridV2 source)
        {
            if (source == null) return;

            columns = source.Columns;
            rows = source.Rows;
            lockedCells.Clear();
            occupiedCells.Clear();

            for (int x = 0; x < columns; x++)
            {
                for (int z = 0; z < rows; z++)
                {
                    var coord = new Vector2Int(x, z);
                    var state = source.GetCellState(coord);
                    if ((state & CellState.Locked) != 0) lockedCells.Add(coord);
                    if ((state & CellState.Occupied) != 0) occupiedCells.Add(coord);
                }
            }

            units.Clear();
            var placeables = source.GetComponentsInChildren<PlaceableObject>(true);
            var unique = new HashSet<PlaceableObject>();
            foreach (var p in placeables)
            {
                if (p == null) continue;
                if (p.CurrentBoard != source) continue;
                if (!unique.Add(p)) continue;
                var link = p.GetComponent<ICreatureComponent>();
                var creatureSO = link != null ? link.CreatureData : null;
                units.Add(new UnitEntry
                {
                    creature = creatureSO,
                    origin = p.OriginCell,
                    sizeX = p.SizeX,
                    sizeZ = p.SizeZ
                });
            }
        }
    }
}


