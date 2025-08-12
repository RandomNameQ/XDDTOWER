using UnityEngine;

namespace Core.BoardV2
{
    public class PlaceableObject : MonoBehaviour
    {
        [SerializeField] private int sizeX = 1;
        [SerializeField] private int sizeZ = 1;

        public int SizeX => sizeX;
        public int SizeZ => sizeZ;

        public BoardGridV2 CurrentBoard { get; internal set; }
        public Vector2Int OriginCell { get; internal set; } = new Vector2Int(-1, -1);

        public void SetSize(int x, int z)
        {
            sizeX = Mathf.Max(1, x);
            sizeZ = Mathf.Max(1, z);
            transform.localScale = new Vector3(sizeX, transform.localScale.y, sizeZ);
        }

        public PlaceableObject GetNeighborRight()
        {
            if (CurrentBoard == null) return null;
            return CurrentBoard.GetNeighbor(this, Direction.Right);
        }

        public PlaceableObject GetNeighborLeft()
        {
            if (CurrentBoard == null) return null;
            return CurrentBoard.GetNeighbor(this, Direction.Left);
        }

        public PlaceableObject GetNeighborUp()
        {
            if (CurrentBoard == null) return null;
            return CurrentBoard.GetNeighbor(this, Direction.Up);
        }

        public PlaceableObject GetNeighborDown()
        {
            if (CurrentBoard == null) return null;
            return CurrentBoard.GetNeighbor(this, Direction.Down);
        }

        public System.Collections.Generic.List<PlaceableObject> GetNeighbors(Direction dir, System.Collections.Generic.List<PlaceableObject> buffer = null)
        {
            if (CurrentBoard == null)
            {
                if (buffer != null) buffer.Clear();
                return buffer ?? new System.Collections.Generic.List<PlaceableObject>();
            }
            buffer ??= new System.Collections.Generic.List<PlaceableObject>();
            CurrentBoard.GetNeighbors(this, dir, buffer);
            return buffer;
        }

        public void Rotate90()
        {
            int newX = sizeZ;
            int newZ = sizeX;
            sizeX = Mathf.Max(1, newX);
            sizeZ = Mathf.Max(1, newZ);
            transform.localScale = new Vector3(sizeX, transform.localScale.y, sizeZ);
        }

        public PlaceableObject GetNeighborUpLeft()
        {
            if (CurrentBoard == null) return null;
            return CurrentBoard.GetNeighbor(this, Direction.UpLeft);
        }

        public PlaceableObject GetNeighborUpRight()
        {
            if (CurrentBoard == null) return null;
            return CurrentBoard.GetNeighbor(this, Direction.UpRight);
        }

        public PlaceableObject GetNeighborDownLeft()
        {
            if (CurrentBoard == null) return null;
            return CurrentBoard.GetNeighbor(this, Direction.DownLeft);
        }

        public PlaceableObject GetNeighborDownRight()
        {
            if (CurrentBoard == null) return null;
            return CurrentBoard.GetNeighbor(this, Direction.DownRight);
        }
    }
}


