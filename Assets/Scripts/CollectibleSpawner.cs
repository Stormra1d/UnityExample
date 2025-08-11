using UnityEngine;

public class CollectibleSpawner : MonoBehaviour
{
    public GameObject[] collectiblePrefabs;
    public int spawnCount = 10;
    public float collectibleSpawnRadius = 15f;

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
                    if (Physics.Raycast(rayStart, Vector3.down, out hit, 10f, LayerMask.GetMask("Movable")))
                    {
                        Vector3 candidate = hit.point + Vector3.up * 0.3f;
                        float checkRadius = 0.3f;
                        Collider[] overlaps = Physics.OverlapSphere(candidate, checkRadius, LayerMask.GetMask("Movable"));
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
                            Instantiate(collectiblePrefabs[Random.Range(0, collectiblePrefabs.Length)], candidate, Quaternion.identity);
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
