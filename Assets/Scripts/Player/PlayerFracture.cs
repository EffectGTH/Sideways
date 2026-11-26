using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerFracture : MonoBehaviour
{
    public int pieceCount = 12;
    public float minPieceScale = 0.15f;
    public float maxPieceScale = 0.35f;
    public float explosionForce = 7f;
    public float torqueForce = 18f;
    public float pieceLifetime = 3f;
    public float reloadDelay = 1.5f;

    private bool isDead;

    public void Shatter()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        var spriteRenderer = GetComponent<SpriteRenderer>();
        var pieceColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        var pieceSprite = spriteRenderer != null ? spriteRenderer.sprite : null;
        var gravityScale = rb != null ? rb.gravityScale : 3f;

        for (var i = 0; i < pieceCount; i++)
        {
            var piece = new GameObject("PlayerPiece");
            piece.transform.position = transform.position + (Vector3)(Random.insideUnitCircle * 0.15f);
            piece.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            var size = Random.Range(minPieceScale, maxPieceScale);
            piece.transform.localScale = new Vector3(size, size, 1f);

            var sr = piece.AddComponent<SpriteRenderer>();
            sr.sprite = pieceSprite;
            sr.color = pieceColor;

            var collider2d = piece.AddComponent<BoxCollider2D>();
            collider2d.sharedMaterial = GetComponent<Collider2D>()?.sharedMaterial;

            var pieceRb = piece.AddComponent<Rigidbody2D>();
            pieceRb.gravityScale = gravityScale;
            pieceRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var dir = Random.insideUnitCircle.normalized;
            dir.y += 0.35f * Mathf.Sign(gravityScale == 0f ? 1f : gravityScale);
            dir.Normalize();

            pieceRb.AddForce(dir * Random.Range(explosionForce * 0.6f, explosionForce * 1.3f), ForceMode2D.Impulse);
            pieceRb.AddTorque(Random.Range(-torqueForce, torqueForce), ForceMode2D.Impulse);

            Destroy(piece, pieceLifetime);
        }

        foreach (var col in GetComponents<Collider2D>())
        {
            col.enabled = false;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        var movement = GetComponent<PlayerGravityFlip>();
        if (movement != null)
        {
            movement.enabled = false;
        }

        StartCoroutine(ReloadAfterDelay());
    }

    private IEnumerator ReloadAfterDelay()
    {
        yield return new WaitForSeconds(reloadDelay);
        var activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }
}