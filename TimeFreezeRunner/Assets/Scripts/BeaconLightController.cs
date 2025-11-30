using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BeaconLightController : MonoBehaviour
{
    public Light2D beaconLight;
    private bool activated = false;

    [Header("SFX")]
    public AudioClip activationSFX;  // assign in Inspector

    public bool IsActivated() => activated;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!activated && other.CompareTag("Player"))
        {
            Debug.Log("Beacon activated!:" + name);
            activated = true;

            // 🔊 Play sound: use player's AudioSource
            var audio = other.GetComponent<AudioSource>();
            if (audio != null && activationSFX != null)
            {
                audio.PlayOneShot(activationSFX);
            }

            FindObjectOfType<BulbCounter>()?.IncrementBulbCount();
            StartCoroutine(FadeInLight());
        }
    }

    System.Collections.IEnumerator FadeInLight()
    {
        float target = 2f;
        while (beaconLight.intensity < target)
        {
            beaconLight.intensity += Time.deltaTime * 2f;
            yield return null;
        }
    }
}
