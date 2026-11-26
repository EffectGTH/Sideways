using System.Collections;
using UnityEngine;

public class RoomTransitionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera roomCamera;

    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 0.8f;
    [SerializeField] private AnimationCurve transitionCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private RoomInstance currentRoom;
    private Rigidbody2D playerBody;
    private Vector3 cameraOffset;
    private bool isTransitioning;

    public void Initialize(RoomNode startNode)
    {
        if (startNode == null || startNode.Instance == null)
        {
            Debug.LogError("Start room is missing.");
            return;
        }

        if (player == null || roomCamera == null)
        {
            Debug.LogError("Player or Camera is not assigned.");
            return;
        }

        playerBody = player.GetComponent<Rigidbody2D>();

        if (playerBody == null)
        {
            Debug.LogError("Player requires Rigidbody2D.");
            return;
        }

        currentRoom = startNode.Instance;

        cameraOffset = roomCamera.transform.position -
                       currentRoom.transform.position;

        currentRoom.SetRoomActive(true);
    }

    public void TryTransition(
        RoomNode sourceRoom,
        RoomNode targetRoom,
        RoomDirection exitDirection)
    {
        if (isTransitioning || currentRoom == null)
            return;

        if (sourceRoom == null || sourceRoom.Instance != currentRoom)
            return;

        if (targetRoom == null || targetRoom.Instance == null)
            return;

        isTransitioning = true;

        StartCoroutine(TransitionRoutine(targetRoom, exitDirection));
    }

    private IEnumerator TransitionRoutine(
        RoomNode targetRoom,
        RoomDirection exitDirection)
    {
        var nextRoom = targetRoom.Instance;

        var entryDirection =
            RoomDirectionUtility.GetOpposite(exitDirection);

        var targetSpawn = nextRoom.GetSpawnPoint(entryDirection);

        if (targetSpawn == null)
        {
            Debug.LogError($"SpawnPoint not found: {entryDirection}");
            isTransitioning = false;
            yield break;
        }

        var previousRoom = currentRoom;
        var wasSimulated = playerBody.simulated;

        playerBody.linearVelocity = Vector2.zero;
        playerBody.simulated = false;

        previousRoom.SetRoomActive(false);

        var startPlayerPosition = player.position;
        var targetPlayerPosition = targetSpawn.position;

        var startCameraPosition = roomCamera.transform.position;

        var targetCameraPosition =
            nextRoom.transform.position + cameraOffset;

        var elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            var progress = Mathf.Clamp01(elapsed / transitionDuration);
            var easedProgress = transitionCurve.Evaluate(progress);

            player.position = Vector3.Lerp(
                startPlayerPosition,
                targetPlayerPosition,
                easedProgress
            );

            roomCamera.transform.position = Vector3.Lerp(
                startCameraPosition,
                targetCameraPosition,
                easedProgress
            );

            elapsed += Time.deltaTime;

            yield return null;
        }

        player.position = targetPlayerPosition;

        playerBody.position = new Vector2(
            targetPlayerPosition.x,
            targetPlayerPosition.y
        );

        currentRoom = nextRoom;

        currentRoom.SetRoomActive(true);

        Physics2D.SyncTransforms();

        playerBody.linearVelocity = Vector2.zero;
        playerBody.simulated = wasSimulated;

        isTransitioning = false;
    }
}
