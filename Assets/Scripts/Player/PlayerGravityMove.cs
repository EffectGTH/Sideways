using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerGravityFlip : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float gravityMagnitude = 3f;
    public KeyCode flipKey = KeyCode.Space;

    private Rigidbody2D rb;
    private float moveInput;
    private bool isUpsideDown;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityMagnitude;
    }

    private void Update()
    {
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
        var velocity = rb.linearVelocity;
        velocity.x = moveInput * moveSpeed;
        rb.linearVelocity = velocity;
    }
}