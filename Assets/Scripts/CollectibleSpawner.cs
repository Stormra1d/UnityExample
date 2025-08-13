using UnityEngine;

public class CollectibleSpawner : MonoBehaviour
{
    public GameObject[] collectiblePrefabs;
    public int spawnCount = 10;
    public float collectibleSpawnRadius = 15f;

    public LayerMask groundMask;
    public LayerMask blockingMask;
    public float minSeparation = 0.5f;

    private void Start()
    {
        SpawnCollectibles();
    }

    void SpawnCollectibles()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPoint;
            if (NavMeshHelper.GetRandomNavMeshPosition(transform.position, collectibleSpawnRadius, out spawnPoint))
            {
                bool placed = false;

                for (int a = 0; a < 50; a++)
                {
                    RaycastHit hit;
                    Vector3 rayStart = spawnPoint + Vector3.up * 5f;
                    if (Physics.Raycast(rayStart, Vector3.down, out hit, 10f, groundMask, QueryTriggerInteraction.Collide))
                    {
                        Vector3 candidate = hit.point + Vector3.up * 0.3f;
                        Collider[] overlaps = Physics.OverlapSphere(candidate, minSeparation, blockingMask, QueryTriggerInteraction.Collide);

                        bool blocked = false;
                        foreach (var col in overlaps)
                        {
                            if (!col.CompareTag("Ground"))
                            {
                                blocked = true;
                                break;
                            }
                        }
                        if (!blocked)
                        {
                            var prefab = collectiblePrefabs[Random.Range(0, collectiblePrefabs.Length)];
                            Instantiate(prefab, candidate, Quaternion.identity);
                            placed = true;
                            break;
                        }
                    }

                    NavMeshHelper.GetRandomNavMeshPosition(transform.position, collectibleSpawnRadius * 1.2f, out spawnPoint);
                }
                if (!placed)
                {
                    Debug.LogWarning("Failed to place collectible after several attempts");
                }
            }
        }
    }
}
