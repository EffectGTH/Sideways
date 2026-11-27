
using System.Collections.Generic;
using UnityEngine;

public class MiniMapController : MonoBehaviour
{
    private static readonly RoomDirection[] ConnectionDirections =
    {
        RoomDirection.Right,
        RoomDirection.Up,
        RoomDirection.Forward
    };

    [SerializeField] private Camera mapCamera;
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private Transform mapContent;

    [SerializeField] private KeyCode toggleKey = KeyCode.M;
    [SerializeField] private float mapSpacing = 3.2f;
    [SerializeField] private float depthSpacingMultiplier = 1.5f;
    [SerializeField] private float roomMarkerSize = 0.55f;
    [SerializeField] private float connectionThickness = 0.12f;

    [SerializeField] private float rotationSensitivity = 4f;
    [SerializeField] private float minCameraPitch = 10f;
    [SerializeField] private float maxCameraPitch = 80f;
    [SerializeField] private float zoomSensitivity = 5f;
    [SerializeField] private float minMapZoom = 3f;
    [SerializeField] private float maxMapZoom = 150f;

    [SerializeField] private Rigidbody2D playerBody;
    [SerializeField] private Behaviour[] behavioursToDisable;
    [SerializeField] private bool pauseWholeGameWhenOpen;

    private bool[] previousBehaviourStates;
    private bool previousPlayerSimulation;
    private float previousTimeScale = 1f;

    private Vector3 mapCenter;
    private float cameraDistance;
    private float orbitYaw = 135f;
    private float orbitPitch = 35f;


    [SerializeField] private Color visitedRoomColor = new Color(0.12f, 0.3f, 0.42f);
    [SerializeField] private Color currentRoomColor = new Color(0.1f, 0.7f, 0.85f);
    [SerializeField] private Color connectionColor = new Color(0.12f, 0.2f, 0.28f);
    [SerializeField] private Color indicatorColor = new Color(0.3f, 0.95f, 1f);
    [SerializeField] private Color mapBackgroundColor = new Color(0.015f, 0.025f, 0.045f);
    [SerializeField] private Color fartherRoomColor = new Color(0.55f, 0.25f, 0.85f);
    [SerializeField] private Color closerRoomColor = new Color(0.95f, 0.55f, 0.2f);
    [SerializeField] private Color depthConnectionColor = new Color(0.4f, 0.25f, 0.6f);

    private readonly Dictionary<RoomNode, GameObject> roomMarkers =
        new Dictionary<RoomNode, GameObject>();

    private readonly Dictionary<RoomNode, GameObject> currentIndicators =
        new Dictionary<RoomNode, GameObject>();

    private readonly List<ConnectionVisual> connections =
        new List<ConnectionVisual>();

    private Material visitedMaterial;
    private Material currentMaterial;
    private Material connectionMaterial;
    private Material indicatorMaterial;
    private Material fartherMaterial;
    private Material closerMaterial;
    private Material depthConnectionMaterial;

    private RoomNode currentRoom;
    private int minimapLayer;
    private bool isOpen;

    private sealed class ConnectionVisual
    {
        public RoomNode First;
        public RoomNode Second;
        public GameObject Visual;
    }

    private void Awake()
    {
        minimapLayer = LayerMask.NameToLayer("Minimap");

        if (minimapLayer == -1)
        {
            Debug.LogError("Minimap layer does not exist.", this);
            enabled = false;
            return;
        }

        CreateMaterials();

        if (mapPanel != null)
            mapPanel.SetActive(false);

        if (mapCamera != null)
        {
            mapCamera.enabled = false;
            mapCamera.orthographic = true;
            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = mapBackgroundColor;
            mapCamera.cullingMask = 1 << minimapLayer;
        }
    }


    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            SetMapOpen(!isOpen);
        }

        if (isOpen)
        {
            HandleMapInput();
        }
    }


    public void Initialize(IEnumerable<RoomNode> nodes)
    {
        if (mapCamera == null || mapPanel == null || mapContent == null)
        {
            Debug.LogError("Minimap references are missing.", this);
            return;
        }

        ClearMap();

        var roomList = new List<RoomNode>(nodes);

        foreach (var node in roomList)
        {
            CreateRoomMarker(node);
        }

        BuildConnections(roomList);

        currentRoom = null;

        RefreshMap();
    }

    public void SetCurrentRoom(RoomNode room)
    {
        if (room == null)
            return;

        if (!roomMarkers.ContainsKey(room))
        {
            Debug.LogError("Room does not exist on the minimap.", this);
            return;
        }

        room.IsVisited = true;
        currentRoom = room;

        RefreshMap();
    }

    private void CreateRoomMarker(RoomNode node)
    {
        var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);

        marker.name = $"MapRoom_{node.GridPosition}";

        marker.transform.SetParent(mapContent, false);
        marker.transform.localPosition = GetMapPosition(node.GridPosition);
        marker.transform.localScale = Vector3.one * roomMarkerSize;

        var markerCollider = marker.GetComponent<Collider>();

        if (markerCollider != null)
            Destroy(markerCollider);

        marker.GetComponent<Renderer>().sharedMaterial = visitedMaterial;

        var indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        indicator.name = "CurrentRoomIndicator";
        indicator.transform.SetParent(marker.transform, false);
        indicator.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        indicator.transform.localScale = Vector3.one * 0.32f;

        var indicatorCollider = indicator.GetComponent<Collider>();

        if (indicatorCollider != null)
            Destroy(indicatorCollider);

        indicator.GetComponent<Renderer>().sharedMaterial = indicatorMaterial;
        indicator.SetActive(false);

        SetLayerRecursively(marker, minimapLayer);

        roomMarkers.Add(node, marker);
        currentIndicators.Add(node, indicator);
    }

    private void BuildConnections(List<RoomNode> nodes)
    {
        foreach (var node in nodes)
        {
            foreach (var direction in ConnectionDirections)
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
        var firstPosition = GetMapPosition(first.GridPosition);
        var secondPosition = GetMapPosition(second.GridPosition);

        var direction = secondPosition - firstPosition;
        var connectorLength = Mathf.Max(
            0.05f,
            (direction.x != 0 || direction.y != 0
                ? mapSpacing
                : mapSpacing * depthSpacingMultiplier) - roomMarkerSize
        );

        var connectorScale = Vector3.one * connectionThickness;

        if (direction.x != 0)
            connectorScale.x = connectorLength;
        else if (direction.y != 0)
            connectorScale.y = connectorLength;
        else
            connectorScale.z = connectorLength;

        var connector = GameObject.CreatePrimitive(PrimitiveType.Cube);

        connector.name = "MapConnection";
        connector.transform.SetParent(mapContent, false);
        connector.transform.localPosition = (firstPosition + secondPosition) * 0.5f;
        connector.transform.localScale = connectorScale;

        var connectorCollider = connector.GetComponent<Collider>();

        if (connectorCollider != null)
            Destroy(connectorCollider);

        var isDepthConnection = first.GridPosition.z != second.GridPosition.z;

        var connectionRenderer = connector.GetComponent<Renderer>();

        connectionRenderer.sharedMaterial = isDepthConnection
            ? depthConnectionMaterial
            : connectionMaterial;

        SetLayerRecursively(connector, minimapLayer);

        connections.Add(new ConnectionVisual
        {
            First = first,
            Second = second,
            Visual = connector
        });
    }

    private void RefreshMap()
    {
        foreach (var pair in roomMarkers)
        {
            var node = pair.Key;
            var marker = pair.Value;

            marker.SetActive(node.IsVisited);

            if (!node.IsVisited)
                continue;

            var depthDifference = node.GridPosition.z - currentRoom.GridPosition.z;

            var material = currentRoom == node
                ? currentMaterial
                : depthDifference > 0
                    ? fartherMaterial
                    : depthDifference < 0
                        ? closerMaterial
                        : visitedMaterial;

            marker.GetComponent<Renderer>().sharedMaterial = material;

            currentIndicators[node].SetActive(node == currentRoom);
        }

        foreach (var connection in connections)
        {
            var visible = connection.First.IsVisited &&
                          connection.Second.IsVisited;

            connection.Visual.SetActive(visible);
        }

        FrameCameraToVisitedRooms();
    }


    private void SetMapOpen(bool open)
    {
        if (isOpen == open)
            return;

        isOpen = open;

        mapPanel.SetActive(isOpen);
        mapCamera.enabled = isOpen;

        if (isOpen)
        {
            if (playerBody != null)
            {
                previousPlayerSimulation = playerBody.simulated;
                playerBody.simulated = false;
            }

            DisableGameplayBehaviours();

            if (pauseWholeGameWhenOpen)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            FrameCameraToVisitedRooms();
        }
        else
        {
            if (pauseWholeGameWhenOpen)
                Time.timeScale = previousTimeScale;

            if (playerBody != null)
                playerBody.simulated = previousPlayerSimulation;

            RestoreGameplayBehaviours();
        }
    }

    private void FrameCameraToVisitedRooms()
    {
        var hasVisitedRoom = false;
        var minimum = Vector3.zero;
        var maximum = Vector3.zero;

        foreach (var node in roomMarkers.Keys)
        {
            if (!node.IsVisited)
                continue;

            var position = mapContent.TransformPoint(
                GetMapPosition(node.GridPosition)
            );

            if (!hasVisitedRoom)
            {
                minimum = position;
                maximum = position;
                hasVisitedRoom = true;
            }
            else
            {
                minimum = Vector3.Min(minimum, position);
                maximum = Vector3.Max(maximum, position);
            }
        }

        if (!hasVisitedRoom)
            return;

        mapCenter = (minimum + maximum) * 0.5f;

        var radius = (maximum - minimum).magnitude * 0.5f;

        cameraDistance = Mathf.Max(20f, radius * 3f + 10f);

        mapCamera.orthographicSize = Mathf.Clamp(
            radius * 1.35f + 1.5f,
            minMapZoom,
            maxMapZoom
        );

        mapCamera.nearClipPlane = 0.1f;
        mapCamera.farClipPlane = 1000f;

        UpdateCameraPose();
    }

    private Vector3 GetMapPosition(Vector3Int gridPosition)
    {
        return new Vector3(
            gridPosition.x * mapSpacing,
            gridPosition.y * mapSpacing,
            gridPosition.z * mapSpacing * depthSpacingMultiplier
        );
    }

    private void CreateMaterials()
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader == null)
            shader = Shader.Find("Standard");

        if (shader == null)
        {
            Debug.LogError("Could not find a suitable map shader.", this);
            enabled = false;
            return;
        }

        visitedMaterial = new Material(shader);
        visitedMaterial.color = visitedRoomColor;

        currentMaterial = new Material(shader);
        currentMaterial.color = currentRoomColor;

        connectionMaterial = new Material(shader);
        connectionMaterial.color = connectionColor;

        indicatorMaterial = new Material(shader);
        indicatorMaterial.color = indicatorColor;

        fartherMaterial = new Material(shader);
        fartherMaterial.color = fartherRoomColor;

        closerMaterial = new Material(shader);
        closerMaterial.color = closerRoomColor;

        depthConnectionMaterial = new Material(shader);
        depthConnectionMaterial.color = depthConnectionColor;
    }

    private void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;

        foreach (Transform child in target.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void ClearMap()
    {
        for (var index = mapContent.childCount - 1; index >= 0; index--)
        {
            Destroy(mapContent.GetChild(index).gameObject);
        }

        roomMarkers.Clear();
        currentIndicators.Clear();
        connections.Clear();
    }


    private void HandleMapInput()
    {
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            var mouseX = Input.GetAxisRaw("Mouse X");
            var mouseY = Input.GetAxisRaw("Mouse Y");

            orbitYaw += mouseX * rotationSensitivity;
            orbitPitch -= mouseY * rotationSensitivity;

            orbitPitch = Mathf.Clamp(
                orbitPitch,
                minCameraPitch,
                maxCameraPitch
            );

            UpdateCameraPose();
        }

        var scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.001f)
        {
            mapCamera.orthographicSize = Mathf.Clamp(
                mapCamera.orthographicSize - scroll * zoomSensitivity,
                minMapZoom,
                maxMapZoom
            );
        }
    }

    private void UpdateCameraPose()
    {
        var pitchRadians = orbitPitch * Mathf.Deg2Rad;
        var yawRadians = orbitYaw * Mathf.Deg2Rad;

        var direction = new Vector3(
            Mathf.Cos(pitchRadians) * Mathf.Sin(yawRadians),
            Mathf.Sin(pitchRadians),
            Mathf.Cos(pitchRadians) * Mathf.Cos(yawRadians)
        );

        mapCamera.transform.position = mapCenter + direction * cameraDistance;
        mapCamera.transform.LookAt(mapCenter, Vector3.up);
    }

    private void DisableGameplayBehaviours()
    {
        if (behavioursToDisable == null)
            return;

        previousBehaviourStates = new bool[behavioursToDisable.Length];

        for (var index = 0; index < behavioursToDisable.Length; index++)
        {
            var behaviour = behavioursToDisable[index];

            if (behaviour == null)
                continue;

            previousBehaviourStates[index] = behaviour.enabled;
            behaviour.enabled = false;
        }
    }

    private void RestoreGameplayBehaviours()
    {
        if (previousBehaviourStates == null)
            return;

        for (var index = 0; index < behavioursToDisable.Length; index++)
        {
            var behaviour = behavioursToDisable[index];

            if (behaviour != null)
                behaviour.enabled = previousBehaviourStates[index];
        }

        previousBehaviourStates = null;
    }

}
