using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnManager : MonoBehaviour
{
    public GameObject smartEnemyPrefab;
    public GameObject dumbEnemyPrefab;

    public Vector3 patrolOrigin = new Vector3(3, 0, 12);
    public float patrolRadius = 15f;

    public int maxDumbEnemies = 4;
    private int phase = 1;
    private int smartKillCount = 0;

    private GameObject currentSmartEnemy;
    private List<GameObject> dumbEnemies = new List<GameObject>();
    private int phase2Spawned = 0;

    private void Start()
    {
        SpawnSmartEnemy();
        StartCoroutine(DumbEnemySpawner());
    }

    void SpawnSmartEnemy()
    {
        Vector3 pos = GetRandomNavMeshPoint();
        currentSmartEnemy = Instantiate(smartEnemyPrefab, pos, Quaternion.identity);
        Health aiHealth = currentSmartEnemy.GetComponent<Health>();
        aiHealth.OnDeath += OnSmartEnemyDeath;
    }

    void SpawnDumbEnemy()
    {
        Vector3 pos = GetRandomNavMeshPoint();
        GameObject dumb = Instantiate(dumbEnemyPrefab, pos, Quaternion.identity);
        dumbEnemies.Add(dumb);

        Health aiHealth = dumb.GetComponent<Health>();
        aiHealth.OnDeath += () =>
        {
            dumbEnemies.Remove(dumb);
            if (phase == 2)
            {
                phase2Spawned--;
                if (phase2Spawned <= 0 && dumbEnemies.Count == 0)
                    phase = 3;
            }
        };
    }

    void OnSmartEnemyDeath()
    {
        smartKillCount++;
        currentSmartEnemy = null;

        if (phase == 1 && smartKillCount < 3)
        {
            SpawnSmartEnemy();
        }

        if (phase == 1 && smartKillCount >= 3)
        {
            phase = 2;
            StartCoroutine(Phase2Routine());
        }
    }

    IEnumerator DumbEnemySpawner()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f);

            if (phase == 1)
            {
                if (dumbEnemies.Count < maxDumbEnemies)
                    SpawnDumbEnemy();
            }
            else if (phase == 3)
            {
                if (Random.value < 0.5f)
                    SpawnDumbEnemy();
                else
                    SpawnSmartEnemy();
            }
        }
    }

    IEnumerator Phase2Routine()
    {
        for (int i = 0; i < 5; i++)
        {
            SpawnDumbEnemy();
            phase2Spawned++;
        }

        yield return new WaitForSeconds(10f);

        for (int i = 0; i < 5; i++)
        {
            SpawnDumbEnemy();
            phase2Spawned++;
        }
    }

    Vector3 GetRandomNavMeshPoint()
    {
        if (NavMeshHelper.GetRandomNavMeshPosition(patrolOrigin, patrolRadius, out Vector3 result))
            return result;

        return patrolOrigin;
    }
}
