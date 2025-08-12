using System;

namespace Core.BoardV2
{
    public enum BoardTypeV2
    {
        Battle,
        Inventory,
        Trash,
        Shop,
        Custom1,
        Custom2
    }

    public enum BoardShapeType
    {
        Rectangle,
        Square,
        RightTriangle,
        IsoscelesTriangle,
        CustomMask
    }

    [Flags]
    public enum CellState
    {
        None = 0,
        Allowed = 1 << 0,
        Occupied = 1 << 1,
        Locked = 1 << 2,
        Destroyed = 1 << 3,
        HighlightValid = 1 << 4,
        HighlightInvalid = 1 << 5
    }

    public enum Direction
    {
        Up,
        Down,
        Left,
        Right,
        UpLeft,
        UpRight,
        DownLeft,
        DownRight
    }
}


