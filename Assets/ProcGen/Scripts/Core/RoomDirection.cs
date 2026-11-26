
using UnityEngine;

public enum RoomDirection
{
    Left,
    Right,
    Up,
    Down,
    Forward,
    Back
}

public static class RoomDirectionUtility
{
    public static Vector3Int GetOffset(RoomDirection direction)
    {
        switch (direction)
        {
            case RoomDirection.Left:
                return Vector3Int.left;

            case RoomDirection.Right:
                return Vector3Int.right;

            case RoomDirection.Up:
                return Vector3Int.up;

            case RoomDirection.Down:
                return Vector3Int.down;

            case RoomDirection.Forward:
                return new Vector3Int(0, 0, 1);

            case RoomDirection.Back:
                return new Vector3Int(0, 0, -1);

            default:
                return Vector3Int.zero;
        }
    }
}
