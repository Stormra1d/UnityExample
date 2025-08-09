using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PickupType { Health, Ammo }

public class MiscSpawner : MonoBehaviour
{
    public List<PickupConfig> pickups;
    public float spawnRadius = 15f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (var config in pickups)
        {
            StartCoroutine(SpawnLoop(config));
        }
    }

    private IEnumerator SpawnLoop(PickupConfig config)
    {
        while (true)
        {
            yield return new WaitForSeconds(config.spawnInterval);
            if (NavMeshHelper.GetRandomNavMeshPosition(transform.position, spawnRadius, out Vector3 point))
            {
                point.y += 0.5f;
                Instantiate(config.prefab, point, Quaternion.identity);
            }
        }
    }
}

[System.Serializable]
public class PickupConfig
{
    public PickupType type;
    public GameObject prefab;
    public float spawnInterval = 60f;
}
