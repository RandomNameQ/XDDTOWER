using System.Collections.Generic;
using UnityEngine;

namespace Core.BoardV2
{
    public static class BoardRegistryV2
    {
        private static readonly Dictionary<BoardTypeV2, BoardGridV2> typeToBoard = new Dictionary<BoardTypeV2, BoardGridV2>();
        private static readonly List<BoardGridV2> boards = new List<BoardGridV2>();

        public static void Register(BoardGridV2 board)
        {
            if (!boards.Contains(board)) boards.Add(board);
            typeToBoard[board.BoardType] = board;
        }

        public static void Unregister(BoardGridV2 board)
        {
            boards.Remove(board);
            if (typeToBoard.TryGetValue(board.BoardType, out var existing) && existing == board)
                typeToBoard.Remove(board.BoardType);
        }

        public static BoardGridV2 Get(BoardTypeV2 type)
        {
            typeToBoard.TryGetValue(type, out var board);
            return board;
        }

        public static BoardGridV2 GetBoardAtPosition(Vector3 worldPosition)
        {
            for (int i = 0; i < boards.Count; i++)
            {
                var b = boards[i];
                if (b != null && b.ContainsWorldPosition(worldPosition)) return b;
            }
            return null;
        }

        public static BoardGridV2 GetTrashBoard()
        {
            return Get(BoardTypeV2.Trash);
        }
    }
}


