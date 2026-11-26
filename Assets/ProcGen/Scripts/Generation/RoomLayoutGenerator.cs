
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

    [Header("Generation Settings")]
    [SerializeField, Min(1)] private int roomCount = 15;
    [SerializeField] private Vector3 roomSpacing = new Vector3(10f, 10f, 10f);

    [Header("References")]
    [SerializeField] private GameObject roomPrefab;
    [SerializeField] private Transform generatedParent;
    [SerializeField] private RoomTransitionManager transitionManager;

    [Header("Random Settings")]
    [SerializeField] private bool useFixedSeed = true;
    [SerializeField] private int fixedSeed = 12345;

    private readonly Dictionary<Vector3Int, RoomNode> rooms =
        new Dictionary<Vector3Int, RoomNode>();

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

        if (roomPrefab.GetComponent<RoomInstance>() == null)
        {
            Debug.LogError("Room Prefab requires RoomInstance.", this);
            return;
        }

        if (transitionManager == null)
        {
            Debug.LogError("Room Transition Manager is not assigned.", this);
            return;
        }

        if (roomSpacing.x <= 0f ||
            roomSpacing.y <= 0f ||
            roomSpacing.z <= 0f)
        {
            Debug.LogError("Room spacing must be greater than zero.", this);
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
        ConfigureRooms();

        transitionManager.Initialize(rooms[startPosition]);

        Debug.Log($"Generated {rooms.Count} rooms. Seed: {generationSeed}", this);
    }

    private Vector3Int GetNextPosition()
    {
        var candidatePositions = new HashSet<Vector3Int>();

        foreach (var node in rooms.Values)
        {
            foreach (var direction in Directions)
            {
                var candidatePosition =
                    node.GridPosition + RoomDirectionUtility.GetOffset(direction);

                if (!rooms.ContainsKey(candidatePosition))
                    candidatePositions.Add(candidatePosition);
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
                var neighbourPosition =
                    node.GridPosition + RoomDirectionUtility.GetOffset(direction);

                if (rooms.TryGetValue(neighbourPosition, out var neighbour))
                    node.Connections[direction] = neighbour;
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
                transform.rotation,
                GetGeneratedParent()
            );

            roomObject.name =
                $"Room_{node.GridPosition.x}_{node.GridPosition.y}_{node.GridPosition.z}";

            node.Instance = roomObject.GetComponent<RoomInstance>();
        }
    }

    private void ConfigureRooms()
    {
        foreach (var node in rooms.Values)
        {
            node.Instance.Configure(node, transitionManager);
            node.Instance.SetRoomActive(false);
        }
    }

    private Vector3 GetWorldPosition(Vector3Int gridPosition)
    {
        var localPosition = Vector3.Scale(
            new Vector3(
                gridPosition.x,
                gridPosition.y,
                gridPosition.z
            ),
            roomSpacing
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
            {
                child.SetActive(false);
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }
}
