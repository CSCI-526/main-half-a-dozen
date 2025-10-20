using UnityEngine;

public class DustManager : MonoBehaviour
{
    public GameObject dustPrefab;
    public int dustCount = 25;
    public Vector2 spawnAreaMin = new Vector2(-10f, -3f);
    public Vector2 spawnAreaMax = new Vector2(10f, 3f);

    void Start()
    {
        for (int i = 0; i < dustCount; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                Random.Range(spawnAreaMin.y, spawnAreaMax.y),
                0
            );
            Instantiate(dustPrefab, pos, Quaternion.identity, transform);
        }
    }
}