using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDash : MonoBehaviour
{
    public float dashSpeed = 22f;
    public float dashDuration = 0.14f;
    public float dashCooldown = 1.2f;
    public float postDashDrag = 0.45f;
    public Key dashKey = Key.LeftShift;
    public Image cooldownFillImage;

    private Rigidbody2D rb;
    private MonoBehaviour moveScript;
    private Vector2 facingDirection = Vector2.right;
    private float cooldownTimer;
    private bool isDashing;

    public bool IsDashing => isDashing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        moveScript = GetComponent("PlayerGravityMove") as MonoBehaviour;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        var moveInput = ReadMoveInput(keyboard);
        if (!isDashing && moveInput.sqrMagnitude > 0.001f)
        {
            facingDirection = moveInput.normalized;
        }

        if (cooldownTimer > 0f)
        {
            cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);
        }

        UpdateCooldownUi();

        if (!isDashing && cooldownTimer <= 0f && keyboard[dashKey].wasPressedThisFrame)
        {
            var dashDir = moveInput.sqrMagnitude > 0.001f ? moveInput.normalized : facingDirection;
            StartCoroutine(DashRoutine(dashDir));
        }
    }

    private Vector2 ReadMoveInput(Keyboard keyboard)
    {
        var x = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            x -= 1f;
        }
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            x += 1f;
        }

        var y = 0f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            y -= 1f;
        }
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            y += 1f;
        }

        return new Vector2(x, y);
    }

    private IEnumerator DashRoutine(Vector2 direction)
    {
        isDashing = true;
        cooldownTimer = dashCooldown;

        if (moveScript != null)
        {
            moveScript.enabled = false;
        }

        var originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        var elapsed = 0f;
        while (elapsed < dashDuration)
        {
            rb.linearVelocity = direction * dashSpeed;
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.gravityScale = originalGravity;
        rb.linearVelocity *= postDashDrag;

        if (moveScript != null)
        {
            moveScript.enabled = true;
        }

        isDashing = false;
    }

    private void UpdateCooldownUi()
    {
        if (cooldownFillImage == null)
        {
            return;
        }

        var ratio = dashCooldown > 0f ? 1f - cooldownTimer / dashCooldown : 1f;
        cooldownFillImage.fillAmount = Mathf.Clamp01(ratio);
    }
}