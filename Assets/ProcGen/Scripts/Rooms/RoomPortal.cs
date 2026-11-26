using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class RoomPortal : MonoBehaviour
{
    [SerializeField] private RoomDirection direction;
    [SerializeField] private BoxCollider2D trigger;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject openVisual;
    [SerializeField] private GameObject closedVisual;
    [SerializeField] private Collider2D closedCollider;

    private RoomNode sourceRoom;
    private RoomNode targetRoom;
    private RoomTransitionManager transitionManager;
    private bool isRoomActive;

    public RoomDirection Direction => direction;
    public Transform SpawnPoint => spawnPoint;

    private void Reset()
    {
        trigger = GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
    }

    private void Awake()
    {
        if (trigger == null)
            trigger = GetComponent<BoxCollider2D>();

        trigger.isTrigger = true;
    }

    public void Configure(
        RoomNode source,
        RoomNode target,
        RoomTransitionManager manager)
    {
        sourceRoom = source;
        targetRoom = target;
        transitionManager = manager;

        if (openVisual != null)
            openVisual.SetActive(targetRoom != null);

        if (closedVisual != null)
            closedVisual.SetActive(targetRoom == null);

        UpdateState();
    }

    public void SetRoomActive(bool active)
    {
        isRoomActive = active;
        UpdateState();
    }

    private void UpdateState()
    {
        var hasConnection = targetRoom != null;

        if (trigger != null)
            trigger.enabled = isRoomActive && hasConnection;

        if (closedCollider != null)
            closedCollider.enabled = isRoomActive && !hasConnection;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (targetRoom == null || transitionManager == null)
            return;

        transitionManager.TryTransition(
            sourceRoom,
            targetRoom,
            direction
        );
    }
}
