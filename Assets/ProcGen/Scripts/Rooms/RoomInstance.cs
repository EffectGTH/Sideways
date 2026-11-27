
using UnityEngine;

public class RoomInstance : MonoBehaviour
{
    [SerializeField] private Transform visualsRoot;
    [SerializeField] private Transform physicsRoot;
    [SerializeField] private Transform portalsRoot;

    private Collider2D[] physicsColliders;
    private RoomPortal[] portals;

    private void Awake()
    {
        physicsColliders = physicsRoot != null
            ? physicsRoot.GetComponentsInChildren<Collider2D>(true)
            : new Collider2D[0];

        portals = portalsRoot != null
            ? portalsRoot.GetComponentsInChildren<RoomPortal>(true)
            : new RoomPortal[0];
    }

    public void Configure(
        RoomNode node,
        RoomTransitionManager manager)
    {
        foreach (var portal in portals)
        {
            node.Connections.TryGetValue(
                portal.Direction,
                out var target
            );

            portal.Configure(node, target, manager);
        }
    }

    public Transform GetSpawnPoint(RoomDirection direction)
    {
        foreach (var portal in portals)
        {
            if (portal.Direction == direction)
                return portal.SpawnPoint;
        }

        return null;
    }

    public void SetRoomActive(bool active)
    {
        foreach (var roomCollider in physicsColliders)
        {
            roomCollider.enabled = active;
        }

        foreach (var portal in portals)
        {
            portal.SetRoomActive(active);
        }
    }

    public void SetRoomVisible(bool visible)
    {
        if (visualsRoot != null)
            visualsRoot.gameObject.SetActive(visible);

        if (portalsRoot != null)
            portalsRoot.gameObject.SetActive(visible);
    }
}
