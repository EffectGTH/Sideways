using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDash : MonoBehaviour
{
    public float dashSpeed = 16f;
    public float dashDuration = 0.16f;
    public float dashCooldown = 1.5f;
    public Key dashKey = Key.LeftShift;
    public Image cooldownFillImage;

    private Rigidbody2D rb;
    private float facingDirection = 1f;
    private float cooldownTimer;
    private bool isDashing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        var left = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? -1f : 0f;
        var right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f;
        var horizontal = left + right;

        if (!isDashing && Mathf.Abs(horizontal) > 0.01f)
        {
            facingDirection = Mathf.Sign(horizontal);
        }

        if (cooldownTimer > 0f)
        {
            cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);
        }

        UpdateCooldownUi();

        if (!isDashing && cooldownTimer <= 0f && keyboard[dashKey].wasPressedThisFrame)
        {
            StartCoroutine(DashRoutine());
        }
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        cooldownTimer = dashCooldown;

        var originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        var elapsed = 0f;
        while (elapsed < dashDuration)
        {
            var velocity = rb.linearVelocity;
            velocity.x = facingDirection * dashSpeed;
            velocity.y = 0f;
            rb.linearVelocity = velocity;

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.gravityScale = originalGravity;
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