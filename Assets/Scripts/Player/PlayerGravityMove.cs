
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
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

    private Rigidbody2D rb;
    private float moveInput;
    private bool isUpsideDown;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityMagnitude;
    }

    private void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        isGrounded = groundCheck != null &&
                     Physics2D.OverlapCircle(
                         groundCheck.position,
                         groundCheckRadius,
                         groundLayer
                     );

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

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
