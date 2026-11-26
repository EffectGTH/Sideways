using UnityEngine;

public class SpikesHazard : MonoBehaviour
{
    public string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        var fracture = other.GetComponent<PlayerFracture>();
        if (fracture != null)
        {
            fracture.Shatter();
        }
    }
}