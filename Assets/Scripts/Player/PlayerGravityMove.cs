using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerGravityFlip : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float gravityMagnitude = 3f;
    public KeyCode flipKey = KeyCode.Space;

    private Rigidbody2D rb;
    private PlayerDash dash;
    private float moveInput;
    private bool isUpsideDown;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        dash = GetComponent<PlayerDash>();
        rb.gravityScale = gravityMagnitude;
    }

    private void Update()
    {
        if (dash != null && dash.IsDashing)
        {
            return;
        }

        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(flipKey))
        {
            isUpsideDown = !isUpsideDown;
            rb.gravityScale = isUpsideDown ? -gravityMagnitude : gravityMagnitude;
            var scale = transform.localScale;
            scale.y = Mathf.Abs(scale.y) * (isUpsideDown ? -1f : 1f);
            transform.localScale = scale;
        }
    }

    private void FixedUpdate()
    {
        if (dash != null && dash.IsDashing)
        {
            return;
        }

        var velocity = rb.linearVelocity;
        velocity.x = moveInput * moveSpeed;
        rb.linearVelocity = velocity;
    }
}