using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Coin : MonoBehaviour
{
    [Header("SFX")]
    public AudioClip coinSFX;   // assign in Inspector

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // is this the player?
        var player = other.attachedRigidbody ? 
                     other.attachedRigidbody.GetComponent<PlayerController>() : 
                     null;

        if (player)
        {
            // 🔊 try to get AudioSource from player
            var audio = player.GetComponent<AudioSource>();
            if (audio != null && coinSFX != null)
            {
                audio.PlayOneShot(coinSFX);
            }

            // your existing coin logic
            GameManager.I?.OnCoinCollected();
            Destroy(gameObject);
        }
    }
}
