
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerGravityFlip : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Gravity")]
    [SerializeField] private float gravityMagnitude = 3f;
    [SerializeField] private KeyCode flipKey = KeyCode.Space;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Step Assist")]
    [SerializeField] private float maxStepHeight = 0.2f;
    [SerializeField] private float stepProbeDistance = 0.15f;
    [SerializeField] private float stepSkin = 0.02f;
    [SerializeField] private float stepRayHeight = 0.05f;

    private Rigidbody2D rb;
    private BoxCollider2D playerCollider;

    private float moveInput;
    private bool isUpsideDown;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<BoxCollider2D>();

        rb.gravityScale = gravityMagnitude;
    }

    private void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        isGrounded = CheckGrounded();

        if (Input.GetKeyDown(flipKey) && isGrounded)
        {
            FlipGravity();
        }
    }

    private void FixedUpdate()
    {
        var velocity = rb.linearVelocity;

        velocity.x = moveInput * moveSpeed;

        rb.linearVelocity = velocity;

        TryStepUp();
    }

    private bool CheckGrounded()
    {
        if (groundCheck == null)
            return false;

        return Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

    private void FlipGravity()
    {
        isUpsideDown = !isUpsideDown;

        rb.gravityScale = isUpsideDown
            ? -gravityMagnitude
            : gravityMagnitude;

        var scale = transform.localScale;

        scale.y = Mathf.Abs(scale.y) * (isUpsideDown ? -1f : 1f);

        transform.localScale = scale;
    }

    private void TryStepUp()
    {
        if (!isGrounded || Mathf.Abs(moveInput) < 0.01f)
            return;

        var moveDirection = Vector2.right * Mathf.Sign(moveInput);

        var groundNormal = isUpsideDown
            ? Vector2.down
            : Vector2.up;

        var bounds = playerCollider.bounds;

        var edgeX = moveDirection.x > 0
            ? bounds.max.x + 0.02f
            : bounds.min.x - 0.02f;

        var footY = groundNormal.y > 0
            ? bounds.min.y + 0.04f
            : bounds.max.y - 0.04f;

        var horizontalOrigin = new Vector2(
            edgeX,
            footY + groundNormal.y * stepRayHeight
        );

        var obstacleHit = Physics2D.Raycast(
            horizontalOrigin,
            moveDirection,
            stepProbeDistance,
            groundLayer
        );

        if (!obstacleHit)
            return;

        var landingOrigin = new Vector2(
            obstacleHit.point.x + moveDirection.x * 0.03f,
            footY
        ) + groundNormal * (maxStepHeight + stepRayHeight);

        var landingHit = Physics2D.Raycast(
            landingOrigin,
            -groundNormal,
            maxStepHeight + stepRayHeight * 2f,
            groundLayer
        );

        if (!landingHit)
            return;

        var stepHeight = Vector2.Dot(
            landingHit.point - new Vector2(0f, footY),
            groundNormal
        );

        if (stepHeight <= 0.02f || stepHeight > maxStepHeight)
            return;

        if (Vector2.Dot(landingHit.normal, groundNormal) < 0.7f)
            return;

        var stepOffset = groundNormal * (stepHeight + stepSkin);

        var targetCenter = (Vector2)bounds.center + stepOffset;

        var targetSize = new Vector2(
            Mathf.Max(0.01f, bounds.size.x - 0.04f),
            Mathf.Max(0.01f, bounds.size.y - 0.04f)
        );

        var blocked = Physics2D.OverlapBox(
            targetCenter,
            targetSize,
            playerCollider.transform.eulerAngles.z,
            groundLayer
        );

        if (blocked != null)
            return;

        rb.position += stepOffset;

        var velocity = rb.linearVelocity;
        var velocityIntoGround = Vector2.Dot(velocity, groundNormal);

        if (velocityIntoGround < 0f)
        {
            velocity -= groundNormal * velocityIntoGround;
            rb.linearVelocity = velocity;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(
                groundCheck.position,
                groundCheckRadius
            );
        }

        if (playerCollider != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(
                playerCollider.bounds.center,
                playerCollider.bounds.size
            );
        }
    }
}
