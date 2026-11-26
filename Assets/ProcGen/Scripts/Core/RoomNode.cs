
using System.Collections.Generic;
using UnityEngine;

public class RoomNode
{
    public Vector3Int GridPosition { get; }

    public Dictionary<RoomDirection, RoomNode> Connections { get; } =
        new Dictionary<RoomDirection, RoomNode>();

    public RoomInstance Instance { get; set; }

    public RoomNode(Vector3Int gridPosition)
    {
        GridPosition = gridPosition;
    }
}
