using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BeaconLightController : MonoBehaviour
{
    public Light2D beaconLight;
    private bool activated = false;

    public bool IsActivated() => activated;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!activated && other.CompareTag("Player"))
        {
            Debug.Log("Beacon activated!:" + name);
            activated = true;
            StartCoroutine(FadeInLight());
        }
    }

    System.Collections.IEnumerator FadeInLight()
    {
        activated = true;
        float target = 2f;
        while (beaconLight.intensity < target)
        {
            beaconLight.intensity += Time.deltaTime * 2f;
            yield return null;
        }
    }
}