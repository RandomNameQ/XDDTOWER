using System.Collections.Generic;

namespace Core.BoardV2
{
    public static class AutoPlacementService
    {
        public static bool TryPlaceInOrder(PlaceableObject obj, IList<BoardTypeV2> order)
        {
            for (int i = 0; i < order.Count; i++)
            {
                var board = BoardRegistryV2.Get(order[i]);
                if (board == null) continue;
                var pos = board.FindFirstFreePosition(obj.SizeX, obj.SizeZ);
                if (pos.x >= 0 && board.TryPlace(obj, pos)) return true;
            }
            return false;
        }
    }
}


