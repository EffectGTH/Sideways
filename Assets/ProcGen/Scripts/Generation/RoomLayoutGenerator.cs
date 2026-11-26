
using System;
using System.Collections.Generic;
using UnityEngine;

public class RoomLayoutGenerator : MonoBehaviour
{
    private static readonly RoomDirection[] Directions =
    {
        RoomDirection.Left,
        RoomDirection.Right,
        RoomDirection.Up,
        RoomDirection.Down,
        RoomDirection.Forward,
        RoomDirection.Back
    };

    private static readonly RoomDirection[] PositiveDirections =
    {
        RoomDirection.Right,
        RoomDirection.Up,
        RoomDirection.Forward
    };

    [Header("Generation Settings")]
    [SerializeField, Min(1)] private int roomCount = 15;
    [SerializeField, Min(1f)] private float roomSpacing = 10f;
    [SerializeField, Min(0.1f)] private float roomMarkerSize = 4f;
    [SerializeField, Min(0.1f)] private float connectionWidth = 0.5f;

    [Header("References")]
    [SerializeField] private GameObject roomPrefab;
    [SerializeField] private Transform generatedParent;

    [Header("Random Settings")]
    [SerializeField] private bool useFixedSeed = true;
    [SerializeField] private int fixedSeed = 12345;

    private readonly Dictionary<Vector3Int, RoomNode> rooms = new Dictionary<Vector3Int, RoomNode>();

    private System.Random random;

    private void Start()
    {
        Generate();
    }

    public void Generate()
    {
        if (roomPrefab == null)
        {
            Debug.LogError("Room Prefab is not assigned.", this);
            return;
        }

        if (roomSpacing <= roomMarkerSize)
        {
            Debug.LogError("Room Spacing must be greater than Room Marker Size.", this);
            return;
        }

        ClearGeneratedObjects();
        rooms.Clear();

        var generationSeed = useFixedSeed ? fixedSeed : Environment.TickCount;

        random = new System.Random(generationSeed);

        var startPosition = Vector3Int.zero;

        rooms.Add(startPosition, new RoomNode(startPosition));

        while (rooms.Count < roomCount)
        {
            var nextPosition = GetNextPosition();

            rooms.Add(nextPosition, new RoomNode(nextPosition));
        }

        BuildConnections();
        SpawnRooms();
        SpawnConnections();

        Debug.Log($"Generated {rooms.Count} rooms. Seed: {generationSeed}", this);
    }

    private Vector3Int GetNextPosition()
    {
        var candidatePositions = new HashSet<Vector3Int>();

        foreach (var node in rooms.Values)
        {
            foreach (var direction in Directions)
            {
                var candidatePosition = node.GridPosition + RoomDirectionUtility.GetOffset(direction);

                if (!rooms.ContainsKey(candidatePosition))
                {
                    candidatePositions.Add(candidatePosition);
                }
            }
        }

        var candidates = new List<Vector3Int>(candidatePositions);

        candidates.Sort(CompareGridPositions);

        return candidates[random.Next(candidates.Count)];
    }

    private static int CompareGridPositions(Vector3Int first, Vector3Int second)
    {
        var xComparison = first.x.CompareTo(second.x);

        if (xComparison != 0)
            return xComparison;

        var yComparison = first.y.CompareTo(second.y);

        if (yComparison != 0)
            return yComparison;

        return first.z.CompareTo(second.z);
    }

    private void BuildConnections()
    {
        foreach (var node in rooms.Values)
        {
            foreach (var direction in Directions)
            {
                var neighbourPosition = node.GridPosition + RoomDirectionUtility.GetOffset(direction);

                if (rooms.TryGetValue(neighbourPosition, out var neighbour))
                {
                    node.Connections[direction] = neighbour;
                }
            }
        }
    }

    private void SpawnRooms()
    {
        foreach (var node in rooms.Values)
        {
            var roomPosition = GetWorldPosition(node.GridPosition);

            var roomObject = Instantiate(
                roomPrefab,
                roomPosition,
                Quaternion.identity,
                GetGeneratedParent()
            );

            roomObject.name = $"Room_{node.GridPosition.x}_{node.GridPosition.y}_{node.GridPosition.z}";
        }
    }

    private void SpawnConnections()
    {
        foreach (var node in rooms.Values)
        {
            foreach (var direction in PositiveDirections)
            {
                if (node.Connections.TryGetValue(direction, out var neighbour))
                {
                    CreateConnection(node, neighbour);
                }
            }
        }
    }

    private void CreateConnection(RoomNode first, RoomNode second)
    {
        var firstPosition = GetWorldPosition(first.GridPosition);
        var secondPosition = GetWorldPosition(second.GridPosition);

        var gridDirection = second.GridPosition - first.GridPosition;

        var connectorLength = roomSpacing - roomMarkerSize;

        var connectorScale = new Vector3(
            connectionWidth,
            connectionWidth,
            connectionWidth
        );

        if (gridDirection.x != 0)
            connectorScale.x = connectorLength;
        else if (gridDirection.y != 0)
            connectorScale.y = connectorLength;
        else
            connectorScale.z = connectorLength;

        var connector = GameObject.CreatePrimitive(PrimitiveType.Cube);

        connector.name = "RoomConnection";
        connector.transform.SetParent(GetGeneratedParent(), true);
        connector.transform.position = (firstPosition + secondPosition) * 0.5f;
        connector.transform.localScale = connectorScale;

        var collider = connector.GetComponent<Collider>();

        if (collider != null)
        {
            if (Application.isPlaying)
                Destroy(collider);
            else
                DestroyImmediate(collider);
        }
    }

    private Vector3 GetWorldPosition(Vector3Int gridPosition)
    {
        var localPosition = new Vector3(
            gridPosition.x * roomSpacing,
            gridPosition.y * roomSpacing,
            gridPosition.z * roomSpacing
        );

        return transform.TransformPoint(localPosition);
    }

    private Transform GetGeneratedParent()
    {
        return generatedParent != null ? generatedParent : transform;
    }

    private void ClearGeneratedObjects()
    {
        var parent = GetGeneratedParent();

        for (var index = parent.childCount - 1; index >= 0; index--)
        {
            var child = parent.GetChild(index).gameObject;

            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }
}
