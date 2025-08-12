using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PickupType { Health, Ammo }

public class MiscSpawner : MonoBehaviour
{
    public List<PickupConfig> pickups;
    public float spawnRadius = 15f;
    private readonly List<Coroutine> running = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (pickups == null) return;

        foreach (var config in pickups)
        {
            if (config != null)
            {
                running.Add(StartCoroutine(SpawnLoop(config)));
            }
        }
    }

    private void OnDisable()
    {
        foreach (var c in running) if (c != null) StopCoroutine(c);
        running.Clear();
    }

    private IEnumerator SpawnLoop(PickupConfig config)
    {
        while (isActiveAndEnabled)
        {
            if (config == null || config.prefab == null)
                yield break;

            yield return new WaitForSeconds(config.spawnInterval);

            if (!isActiveAndEnabled || config.prefab == null)
                yield break;

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
