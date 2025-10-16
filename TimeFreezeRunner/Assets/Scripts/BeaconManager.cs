using UnityEngine;

public class BeaconManager : MonoBehaviour
{
    public BeaconLightController[] beacons;
    public GameObject keyPrefab;
    public Transform keySpawnPoint;

    private bool keySpawned = false;

    void Update()
    {
        if (!keySpawned && AllBeaconsLit())
        {
            SpawnKey();
        }
    }

    bool AllBeaconsLit()
    {
        foreach (var b in beacons)
        {
            if (!b.IsActivated()) return false;
        }
        return true;
    }

    void SpawnKey()
    {
        Instantiate(keyPrefab, keySpawnPoint.position, Quaternion.identity);
        keySpawned = true;
    }
}