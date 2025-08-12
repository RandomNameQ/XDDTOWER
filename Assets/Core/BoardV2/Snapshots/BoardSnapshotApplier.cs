using System.Collections.Generic;
using UnityEngine;
 

namespace Core.BoardV2.Snapshots
{
    public class BoardSnapshotApplier : MonoBehaviour
    {
        [SerializeField] private BoardSnapshotSO snapshot;
        [SerializeField] private BoardGridV2 targetGrid;

        [Header("Spawn")]
        [SerializeField] private bool respawnUnits;
        [SerializeField] private bool callInitOnCreature = true;

        public void Apply()
        {
            if (snapshot == null)
            {
                Debug.LogError("BoardSnapshotApplier: snapshot is null");
                return;
            }
            if (targetGrid == null) targetGrid = GetComponent<BoardGridV2>();
            if (targetGrid == null)
            {
                Debug.LogError("BoardSnapshotApplier: targetGrid is null");
                return;
            }

            // 1) Подогнать размеры
            targetGrid.SetDimensions(snapshot.columns, snapshot.rows);

            // 2) Сбросить все флаги до Allowed для существующих разрешённых клеток
            for (int x = 0; x < snapshot.columns; x++)
            {
                for (int z = 0; z < snapshot.rows; z++)
                {
                    var coord = new Vector2Int(x, z);
                    var state = targetGrid.GetCellState(coord);
                    if (state == CellState.None) continue;
                    targetGrid.SetCellState(coord, CellState.Allowed);
                }
            }

            // 3) Применить Locked
            if (snapshot.lockedCells != null)
            {
                foreach (var c in snapshot.lockedCells)
                {
                    var s = targetGrid.GetCellState(c);
                    if (s == CellState.None) continue;
                    targetGrid.SetCellState(c, s | CellState.Locked);
                }
            }

            // 4) Применить Occupied только через размещение юнитов, а не напрямую флагом

            // 5) Респаун юнитов
            if (respawnUnits)
            {
                // Удаляем старые объекты на гриде
                var toDestroy = new List<GameObject>();
                foreach (Transform child in targetGrid.transform)
                {
                    if (child == null) continue;
                    if (child.GetComponentInParent<BoardGridV2>() != targetGrid) continue;
                    if (child.GetComponent<PlaceableObject>() != null) toDestroy.Add(child.gameObject);
                }
                for (int i = 0; i < toDestroy.Count; i++)
                {
                    if (Application.isPlaying) Destroy(toDestroy[i]); else DestroyImmediate(toDestroy[i]);
                }

                if (snapshot.units != null)
                {
                    foreach (var u in snapshot.units)
                    {
                        if (u == null || u.creature == null) continue;
                        GameObject go;
                        if (snapshot.unitPrefab != null)
                        {
                            go = Instantiate(snapshot.unitPrefab);
                        }
                        else
                        {
                            go = new GameObject("Unit");
                        }
                        var p = go.GetComponent<PlaceableObject>();
                        if (p == null) p = go.AddComponent<PlaceableObject>();
                        p.SetSize(u.sizeX, u.sizeZ);

                        var link = go.GetComponent<ICreatureComponent>();
                        if (link == null) link = go.AddComponent<CreatureLink>();
                        link.CreatureData = u.creature;
                        if (callInitOnCreature)
                        {
                            var init = go.GetComponent<IInitFromSO>();
                            init?.InitDataSO();
                        }
                        targetGrid.TryPlace(p, u.origin);
                    }
                }
            }
        }
    }
}


