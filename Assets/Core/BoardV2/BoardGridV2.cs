using System.Collections.Generic;
using UnityEngine;

namespace Core.BoardV2
{
    [DefaultExecutionOrder(-200)]
    public class BoardGridV2 : MonoBehaviour
    {
        [SerializeField] private BoardTypeV2 boardType = BoardTypeV2.Battle;
        [SerializeField] private int columns = 5;
        [SerializeField] private int rows = 3;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float cellGap = 0.1f;
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private Transform cellsParent;
        [SerializeField] private Transform objectsParent;
        [SerializeField] private BoardShapeType shapeType = BoardShapeType.Rectangle;
        [SerializeField] private List<Vector2Int> customMask = new List<Vector2Int>();
        [SerializeField, Range(0,1)] private float lockedPercent = 0f;
        [SerializeField] private bool lockFromEnd = false;

        public BoardTypeV2 BoardType => boardType;
        public int Columns => columns;
        public int Rows => rows;
        public float PlaneY { get; private set; }
        public float CellSize => cellSize;
        public float CellGap => cellGap;

        private readonly Dictionary<Vector2Int, BoardCell> coordToCell = new Dictionary<Vector2Int, BoardCell>();
        private readonly HashSet<Vector2Int> allowedCells = new HashSet<Vector2Int>();
        private PlaceableObject[,] occupant;
        private CellState[,] states;

        public delegate void ObjectPlacedHandler(PlaceableObject obj, Vector2Int origin);
        public event ObjectPlacedHandler OnObjectPlaced;
        public event ObjectPlacedHandler OnObjectRemoved;

        private void Awake()
        {
            Init();
        }

        private void Init()
        {
            PlaneY = transform.position.y;
            if (cellsParent == null) cellsParent = transform;
            if (objectsParent == null) objectsParent = transform;
            occupant = new PlaceableObject[columns, rows];
            states = new CellState[columns, rows];
            BuildAllowedMask();
            CreateCells();
            ApplyLocksByPercent();
            BoardRegistryV2.Register(this);
        }

        private void OnDestroy()
        {
            BoardRegistryV2.Unregister(this);
        }

        private void BuildAllowedMask()
        {
            allowedCells.Clear();
            if (shapeType == BoardShapeType.CustomMask)
            {
                for (int i = 0; i < customMask.Count; i++) allowedCells.Add(customMask[i]);
                return;
            }

            switch (shapeType)
            {
                case BoardShapeType.Rectangle:
                case BoardShapeType.Square:
                    for (int x = 0; x < columns; x++)
                        for (int z = 0; z < rows; z++)
                            allowedCells.Add(new Vector2Int(x, z));
                    break;
                case BoardShapeType.RightTriangle:
                    for (int z = 0; z < rows; z++)
                        for (int x = 0; x <= Mathf.Min(columns - 1, z); x++)
                            allowedCells.Add(new Vector2Int(x, z));
                    break;
                case BoardShapeType.IsoscelesTriangle:
                    for (int z = 0; z < rows; z++)
                    {
                        int width = Mathf.RoundToInt((float)columns * (z + 1) / rows);
                        int start = (columns - width) / 2;
                        for (int x = 0; x < width; x++)
                            allowedCells.Add(new Vector2Int(start + x, z));
                    }
                    break;
            }
        }

        private void CreateCells()
        {
            coordToCell.Clear();
            for (int x = 0; x < columns; x++)
            {
                for (int z = 0; z < rows; z++)
                {
                    var c = new Vector2Int(x, z);
                    if (!allowedCells.Contains(c)) continue;
                    if (cellPrefab == null) continue;
                    var cellGo = Instantiate(cellPrefab, GetCellCenterWorld(c), Quaternion.identity, cellsParent);
                    var cell = cellGo.GetComponent<BoardCell>();
                    if (cell == null) cell = cellGo.AddComponent<BoardCell>();
                    coordToCell[c] = cell;
                    states[x, z] = CellState.Allowed;
                    cell.Init(c, cellSize);
                }
            }
        }

        private void ApplyLocksByPercent()
        {
            if (lockedPercent <= 0f) return;
            int total = allowedCells.Count;
            int toLock = Mathf.FloorToInt(total * lockedPercent);
            if (toLock <= 0) return;

            var list = new List<Vector2Int>(allowedCells);
            list.Sort((a, b) =>
            {
                if (lockFromEnd) return (b.y * columns + b.x).CompareTo(a.y * columns + a.x);
                return (a.y * columns + a.x).CompareTo(b.y * columns + b.x);
            });

            for (int i = 0; i < toLock && i < list.Count; i++)
            {
                var c = list[i];
                states[c.x, c.y] |= CellState.Locked;
                if (coordToCell.TryGetValue(c, out var cell)) cell.SetState(states[c.x, c.y]);
            }
        }

        public bool ContainsWorldPosition(Vector3 worldPos)
        {
            var cell = WorldToCell(worldPos);
            return IsInside(cell) && allowedCells.Contains(cell);
        }

        public Vector2Int WorldToCell(Vector3 worldPos)
        {
            Vector3 origin = transform.position;
            float step = cellSize + cellGap;
            int x = Mathf.FloorToInt((worldPos.x - origin.x) / step);
            int z = Mathf.FloorToInt((worldPos.z - origin.z) / step);
            return new Vector2Int(x, z);
        }

        public Vector3 GetCellCenterWorld(Vector2Int cell)
        {
            Vector3 origin = transform.position;
            float step = cellSize + cellGap;
            float x = origin.x + cell.x * step + cellSize * 0.5f;
            float z = origin.z + cell.y * step + cellSize * 0.5f;
            return new Vector3(x, PlaneY, z);
        }

        public Vector3 GetAreaCenterWorld(Vector2Int originCell, int sizeX, int sizeZ)
        {
            var min = GetCellCenterWorld(originCell);
            var max = GetCellCenterWorld(new Vector2Int(originCell.x + sizeX - 1, originCell.y + sizeZ - 1));
            return (min + max) * 0.5f;
        }

        public bool IsAreaFree(Vector2Int originCell, int sizeX, int sizeZ)
        {
            for (int x = originCell.x; x < originCell.x + sizeX; x++)
            {
                for (int z = originCell.y; z < originCell.y + sizeZ; z++)
                {
                    if (!IsInside(x, z)) return false;
                    if (!allowedCells.Contains(new Vector2Int(x, z))) return false;
                    var s = states[x, z];
                    if ((s & (CellState.Locked | CellState.Destroyed)) != 0) return false;
                    if (occupant[x, z] != null) return false;
                }
            }
            return true;
        }

        public bool TryPlace(PlaceableObject obj, Vector2Int originCell)
        {
            if (!IsAreaFree(originCell, obj.SizeX, obj.SizeZ)) return false;
            for (int x = originCell.x; x < originCell.x + obj.SizeX; x++)
            {
                for (int z = originCell.y; z < originCell.y + obj.SizeZ; z++)
                {
                    occupant[x, z] = obj;
                    states[x, z] |= CellState.Occupied;
                    if (coordToCell.TryGetValue(new Vector2Int(x, z), out var cell)) cell.SetState(states[x, z]);
                }
            }
            obj.transform.position = GetAreaCenterWorld(originCell, obj.SizeX, obj.SizeZ);
            if (objectsParent == null) objectsParent = transform;
            obj.transform.SetParent(objectsParent, true);
            obj.CurrentBoard = this;
            obj.gameObject.BroadcastMessage("OnBoardChanged", boardType, SendMessageOptions.DontRequireReceiver);
            obj.OriginCell = originCell;
            OnObjectPlaced?.Invoke(obj, originCell);
            NotifyNeighborsChanged(obj);
            return true;
        }

        public void Remove(PlaceableObject obj)
        {
            if (obj == null) return;
            if (obj.CurrentBoard != this) return;
            var o = obj.OriginCell;
            if (o.x < 0) return;
            for (int x = o.x; x < o.x + obj.SizeX; x++)
            {
                for (int z = o.y; z < o.y + obj.SizeZ; z++)
                {
                    if (!IsInside(x, z)) continue;
                    if (occupant[x, z] == obj)
                    {
                        occupant[x, z] = null;
                        states[x, z] &= ~CellState.Occupied;
                        if (coordToCell.TryGetValue(new Vector2Int(x, z), out var cell)) cell.SetState(states[x, z]);
                    }
                }
            }
            OnObjectRemoved?.Invoke(obj, o);
            NotifyNeighborsChanged(obj);
            obj.OriginCell = new Vector2Int(-1, -1);
            obj.CurrentBoard = null;
        }

        private void NotifyNeighborsChanged(PlaceableObject obj)
        {
            if (obj == null) return;
            var notified = new HashSet<PlaceableObject>();
            void Notify(PlaceableObject p)
            {
                if (p == null) return;
                if (!notified.Add(p)) return;
                p.gameObject.BroadcastMessage("OnGridNeighborsChanged", SendMessageOptions.DontRequireReceiver);
            }

            Notify(obj);
            var tmp = new List<PlaceableObject>();
            GetNeighbors(obj, Direction.Left, tmp); foreach (var n in tmp) Notify(n); tmp.Clear();
            GetNeighbors(obj, Direction.Right, tmp); foreach (var n in tmp) Notify(n); tmp.Clear();
            GetNeighbors(obj, Direction.Up, tmp); foreach (var n in tmp) Notify(n); tmp.Clear();
            GetNeighbors(obj, Direction.Down, tmp); foreach (var n in tmp) Notify(n); tmp.Clear();
            GetNeighbors(obj, Direction.UpLeft, tmp); foreach (var n in tmp) Notify(n); tmp.Clear();
            GetNeighbors(obj, Direction.UpRight, tmp); foreach (var n in tmp) Notify(n); tmp.Clear();
            GetNeighbors(obj, Direction.DownLeft, tmp); foreach (var n in tmp) Notify(n); tmp.Clear();
            GetNeighbors(obj, Direction.DownRight, tmp); foreach (var n in tmp) Notify(n); tmp.Clear();
        }

        public bool TryMoveToBoard(PlaceableObject obj, BoardGridV2 targetBoard, Vector2Int targetOrigin)
        {
            if (obj == null || targetBoard == null) return false;
            if (!targetBoard.IsAreaFree(targetOrigin, obj.SizeX, obj.SizeZ)) return false;
            Remove(obj);
            return targetBoard.TryPlace(obj, targetOrigin);
        }

        public Vector2Int ClampOrigin(Vector2Int originCell, int sizeX, int sizeZ)
        {
            int x = Mathf.Clamp(originCell.x, 0, columns - sizeX);
            int z = Mathf.Clamp(originCell.y, 0, rows - sizeZ);
            return new Vector2Int(x, z);
        }

        public Vector2Int FindFirstFreePosition(int sizeX, int sizeZ)
        {
            for (int z = 0; z < rows; z++)
            {
                for (int x = 0; x < columns; x++)
                {
                    var c = new Vector2Int(x, z);
                    if (IsAreaFree(c, sizeX, sizeZ)) return c;
                }
            }
            return new Vector2Int(-1, -1);
        }

        public PlaceableObject GetNeighbor(PlaceableObject obj, Direction dir)
        {
            if (obj == null || obj.CurrentBoard != this) return null;
            var o = obj.OriginCell;
            if (o.x < 0) return null;

            if (dir == Direction.Right)
            {
                int x = o.x + obj.SizeX;
                if (x >= 0 && x < columns)
                {
                    for (int z = o.y; z < o.y + obj.SizeZ && z < rows; z++)
                    {
                        if (z < 0) continue;
                        var occ = occupant[x, z];
                        if (occ != null && occ != obj) return occ;
                    }
                }
            }
            else if (dir == Direction.Left)
            {
                int x = o.x - 1;
                if (x >= 0 && x < columns)
                {
                    for (int z = o.y; z < o.y + obj.SizeZ && z < rows; z++)
                    {
                        if (z < 0) continue;
                        var occ = occupant[x, z];
                        if (occ != null && occ != obj) return occ;
                    }
                }
            }
            else if (dir == Direction.Up)
            {
                int z = o.y + obj.SizeZ;
                if (z >= 0 && z < rows)
                {
                    for (int x = o.x; x < o.x + obj.SizeX && x < columns; x++)
                    {
                        if (x < 0) continue;
                        var occ = occupant[x, z];
                        if (occ != null && occ != obj) return occ;
                    }
                }
            }
            else if (dir == Direction.Down)
            {
                int z = o.y - 1;
                if (z >= 0 && z < rows)
                {
                    for (int x = o.x; x < o.x + obj.SizeX && x < columns; x++)
                    {
                        if (x < 0) continue;
                        var occ = occupant[x, z];
                        if (occ != null && occ != obj) return occ;
                    }
                }
            }
            else if (dir == Direction.UpLeft)
            {
                int x = o.x - 1;
                int z = o.y + obj.SizeZ;
                if (x >= 0 && x < columns && z >= 0 && z < rows)
                {
                    var occ = occupant[x, z];
                    if (occ != null && occ != obj && IsCornerTouchOnly(obj, occ, Direction.UpLeft)) return occ;
                }
            }
            else if (dir == Direction.UpRight)
            {
                int x = o.x + obj.SizeX;
                int z = o.y + obj.SizeZ;
                if (x >= 0 && x < columns && z >= 0 && z < rows)
                {
                    var occ = occupant[x, z];
                    if (occ != null && occ != obj && IsCornerTouchOnly(obj, occ, Direction.UpRight)) return occ;
                }
            }
            else if (dir == Direction.DownLeft)
            {
                int x = o.x - 1;
                int z = o.y - 1;
                if (x >= 0 && x < columns && z >= 0 && z < rows)
                {
                    var occ = occupant[x, z];
                    if (occ != null && occ != obj && IsCornerTouchOnly(obj, occ, Direction.DownLeft)) return occ;
                }
            }
            else if (dir == Direction.DownRight)
            {
                int x = o.x + obj.SizeX;
                int z = o.y - 1;
                if (x >= 0 && x < columns && z >= 0 && z < rows)
                {
                    var occ = occupant[x, z];
                    if (occ != null && occ != obj && IsCornerTouchOnly(obj, occ, Direction.DownRight)) return occ;
                }
            }

            return null;
        }

        public List<PlaceableObject> GetNeighbors(PlaceableObject obj, Direction dir, List<PlaceableObject> buffer)
        {
            buffer ??= new List<PlaceableObject>();
            buffer.Clear();
            if (obj == null || obj.CurrentBoard != this) return buffer;
            var seen = new HashSet<PlaceableObject>();
            var o = obj.OriginCell;
            if (o.x < 0) return buffer;

            void TryAdd(PlaceableObject occ)
            {
                if (occ == null || occ == obj) return;
                if (seen.Add(occ)) buffer.Add(occ);
            }

            if (dir == Direction.Right)
            {
                int x = o.x + obj.SizeX;
                if (x >= 0 && x < columns)
                {
                    for (int z = o.y; z < o.y + obj.SizeZ && z < rows; z++)
                    {
                        if (z < 0) continue;
                        TryAdd(occupant[x, z]);
                    }
                }
            }
            else if (dir == Direction.Left)
            {
                int x = o.x - 1;
                if (x >= 0 && x < columns)
                {
                    for (int z = o.y; z < o.y + obj.SizeZ && z < rows; z++)
                    {
                        if (z < 0) continue;
                        TryAdd(occupant[x, z]);
                    }
                }
            }
            else if (dir == Direction.Up)
            {
                int z = o.y + obj.SizeZ;
                if (z >= 0 && z < rows)
                {
                    for (int x = o.x; x < o.x + obj.SizeX && x < columns; x++)
                    {
                        if (x < 0) continue;
                        TryAdd(occupant[x, z]);
                    }
                }
            }
            else if (dir == Direction.Down)
            {
                int z = o.y - 1;
                if (z >= 0 && z < rows)
                {
                    for (int x = o.x; x < o.x + obj.SizeX && x < columns; x++)
                    {
                        if (x < 0) continue;
                        TryAdd(occupant[x, z]);
                    }
                }
            }
            else if (dir == Direction.UpLeft)
            {
                int x = o.x - 1;
                int z = o.y + obj.SizeZ;
                if (x >= 0 && x < columns && z >= 0 && z < rows)
                {
                    var occ = occupant[x, z];
                    if (occ != null && occ != obj && IsCornerTouchOnly(obj, occ, Direction.UpLeft)) TryAdd(occ);
                }
            }
            else if (dir == Direction.UpRight)
            {
                int x = o.x + obj.SizeX;
                int z = o.y + obj.SizeZ;
                if (x >= 0 && x < columns && z >= 0 && z < rows)
                {
                    var occ = occupant[x, z];
                    if (occ != null && occ != obj && IsCornerTouchOnly(obj, occ, Direction.UpRight)) TryAdd(occ);
                }
            }
            else if (dir == Direction.DownLeft)
            {
                int x = o.x - 1;
                int z = o.y - 1;
                if (x >= 0 && x < columns && z >= 0 && z < rows)
                {
                    var occ = occupant[x, z];
                    if (occ != null && occ != obj && IsCornerTouchOnly(obj, occ, Direction.DownLeft)) TryAdd(occ);
                }
            }
            else if (dir == Direction.DownRight)
            {
                int x = o.x + obj.SizeX;
                int z = o.y - 1;
                if (x >= 0 && x < columns && z >= 0 && z < rows)
                {
                    var occ = occupant[x, z];
                    if (occ != null && occ != obj && IsCornerTouchOnly(obj, occ, Direction.DownRight)) TryAdd(occ);
                }
            }

            return buffer;
        }

        private static bool IsCornerTouchOnly(PlaceableObject a, PlaceableObject b, Direction dir)
        {
            var aMinX = a.OriginCell.x;
            var aMaxX = a.OriginCell.x + a.SizeX - 1;
            var aMinZ = a.OriginCell.y;
            var aMaxZ = a.OriginCell.y + a.SizeZ - 1;

            var bMinX = b.OriginCell.x;
            var bMaxX = b.OriginCell.x + b.SizeX - 1;
            var bMinZ = b.OriginCell.y;
            var bMaxZ = b.OriginCell.y + b.SizeZ - 1;

            switch (dir)
            {
                case Direction.UpLeft:
                    return bMaxX == aMinX - 1 && bMinZ == aMaxZ + 1;
                case Direction.UpRight:
                    return bMinX == aMaxX + 1 && bMinZ == aMaxZ + 1;
                case Direction.DownLeft:
                    return bMaxX == aMinX - 1 && bMaxZ == aMinZ - 1;
                case Direction.DownRight:
                    return bMinX == aMaxX + 1 && bMaxZ == aMinZ - 1;
                default:
                    return false;
            }
        }

        public void SetCellState(Vector2Int coord, CellState state)
        {
            if (!IsInside(coord)) return;
            states[coord.x, coord.y] = state;
            if (coordToCell.TryGetValue(coord, out var cell)) cell.SetState(state);
        }

        public CellState GetCellState(Vector2Int coord)
        {
            if (!IsInside(coord)) return CellState.None;
            return states[coord.x, coord.y];
        }

        public void PaintCell(Vector2Int coord, bool allow)
        {
            if (!IsInside(coord)) return;
            if (allow) allowedCells.Add(coord); else allowedCells.Remove(coord);
            if (allow && !coordToCell.ContainsKey(coord) && cellPrefab != null)
            {
                var go = Instantiate(cellPrefab, GetCellCenterWorld(coord), Quaternion.identity, cellsParent);
                var cell = go.GetComponent<BoardCell>();
                if (cell == null) cell = go.AddComponent<BoardCell>();
                coordToCell[coord] = cell;
                states[coord.x, coord.y] |= CellState.Allowed;
                cell.Init(coord, cellSize);
            }
            else if (!allow && coordToCell.TryGetValue(coord, out var existing))
            {
                Destroy(existing.gameObject);
                coordToCell.Remove(coord);
                states[coord.x, coord.y] &= ~CellState.Allowed;
            }
        }

        public void ClearHighlights()
        {
            for (int x = 0; x < columns; x++)
            for (int z = 0; z < rows; z++)
            {
                var c = new Vector2Int(x, z);
                var s = GetCellState(c);
                if ((s & (CellState.HighlightValid | CellState.HighlightInvalid)) != 0)
                {
                    s &= ~(CellState.HighlightValid | CellState.HighlightInvalid);
                    SetCellState(c, s);
                }
            }
        }

        public Vector2Int SnapWorldToOrigin(Vector3 worldPos, int sizeX, int sizeZ)
        {
            var c = WorldToCell(worldPos);
            c = new Vector2Int(c.x - Mathf.FloorToInt(sizeX * 0.5f), c.y - Mathf.FloorToInt(sizeZ * 0.5f));
            return ClampOrigin(c, sizeX, sizeZ);
        }

        private bool IsInside(Vector2Int c) => IsInside(c.x, c.y);
        private bool IsInside(int x, int z) => x >= 0 && x < columns && z >= 0 && z < rows;

        // Генерация по кнопке в инспекторе: удаляет старые клетки и создаёт новые по текущим настройкам
        public void Regenerate()
        {
            PlaneY = transform.position.y;
            if (cellsParent == null) cellsParent = transform;
            if (objectsParent == null) objectsParent = transform;

            ClearAllCells();

            occupant = new PlaceableObject[columns, rows];
            states = new CellState[columns, rows];
            BuildAllowedMask();
            CreateCells();
            ApplyLocksByPercent();
        }

        private void ClearAllCells()
        {
            // Удаляем ячейки, созданные ранее и очищаем словари
            if (cellsParent == null) cellsParent = transform;

            foreach (var kv in coordToCell)
            {
                if (kv.Value != null)
                {
                    DestroyAppropriate(kv.Value.gameObject);
                }
            }
            coordToCell.Clear();

            // На всякий случай удаляем всех детей под cellsParent
            var toDelete = new List<Transform>();
            foreach (Transform child in cellsParent)
            {
                toDelete.Add(child);
            }
            for (int i = 0; i < toDelete.Count; i++)
            {
                DestroyAppropriate(toDelete[i].gameObject);
            }
        }

        private void DestroyAppropriate(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }

        // Размеры через публичный API
        public void SetDimensions(int newColumns, int newRows)
        {
            columns = Mathf.Max(1, newColumns);
            rows = Mathf.Max(1, newRows);
            Regenerate();
        }
    }
}


