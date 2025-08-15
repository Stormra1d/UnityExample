using UnityEngine;
using UnityEngine.AI;

public enum State { Patrol, Wait, Chase }

public class EnemyAI : BaseEnemyAI
{
    public State currentState;
    public Transform testPlayer;

    public Vector3 patrolOrigin = new Vector3(3, 0, 12);
    public float patrolRadius = 15f;
    public float moveSpeed = 3f;
    public float chaseSpeed = 5f;
    public float waitTime = 4f;
    public float retryTime = 2f;
    public float sightRange = 15f;
    public float fovAngle = 120f;
    public LayerMask playerLayer;
    public LayerMask obstacleMask;

    Vector3 targetPosition;
    float timer;

    public float forgetDuration = 3f;

    float lastSeenTime = float.NegativeInfinity;
    public Vector3 lastKnownPosition;
    public bool isGoingToLastKnownPosition = false;
    bool hasEverSeenPlayer = false;

    protected override void Start()
    {
        base.Start();
        if (testPlayer == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                testPlayer = playerObj.transform;
        }
        agent.stoppingDistance = Mathf.Max(agent.stoppingDistance, 0.4f);
        SwitchState(State.Patrol);
    }

    private void Update()
    {
        bool seen = PlayerInSight();

        if (seen)
        {
            if (currentState != State.Chase)
            {
                SwitchState(State.Chase);
            }

            if (isGoingToLastKnownPosition)
            {
                isGoingToLastKnownPosition = false;
            }
        }

        float timeSinceSeen = Time.time - lastSeenTime;
        if (!seen && !isGoingToLastKnownPosition && hasEverSeenPlayer && timeSinceSeen >= forgetDuration)
        {
            float distanceToLKP = Vector3.Distance(transform.position, lastKnownPosition);
            if (distanceToLKP > agent.stoppingDistance + 0.5f)
            {
                isGoingToLastKnownPosition = true;
                bool ok = agent.SetDestination(lastKnownPosition);
            }
        }

        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                break;
            case State.Wait:
                Wait();
                break;
            case State.Chase:
                Chase(seen);
                break;
        }
    }

    void SwitchState(State newState, float delay = 0f)
    {
        State prev = currentState;
        currentState = newState;
        timer = delay;

        if (newState == State.Patrol)
        {
            GenerateNewPatrolPoint();
        }
    }

    void Patrol()
    {
        if (PlayerInSight())
        {
            SwitchState(State.Chase);
            return;
        }

        if (Vector3.Distance(transform.position, targetPosition) < 1f)
        {
            SwitchState(State.Wait, waitTime);
            return;
        }

        MoveTowards(targetPosition, moveSpeed);

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            SwitchState(State.Wait, retryTime);
        }
    }

    void GenerateNewPatrolPoint()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDir = Random.insideUnitSphere * patrolRadius;
            randomDir.y = 0;
            Vector3 point = patrolOrigin + randomDir;

            if (NavMesh.SamplePosition(point, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                targetPosition = hit.position;
                return;
            }
        }

        SwitchState(State.Wait, retryTime);
    }

    void Wait()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            if (PlayerInSight())
            {
                SwitchState(State.Chase);
            }
            else
            {
                SwitchState(State.Patrol);
            }
        }
    }

    void Chase(bool seenNow)
    {
        if (isGoingToLastKnownPosition)
        {
            float arriveEps = agent.stoppingDistance + 0.05f;
            if (!agent.pathPending && agent.remainingDistance <= arriveEps)
            {
                isGoingToLastKnownPosition = false;
                SwitchState(State.Wait, 2f);
            }
            else
            {
                agent.speed = chaseSpeed;
                bool ok = agent.SetDestination(lastKnownPosition);
            }
            return;
        }

        if (seenNow)
        {
            agent.speed = chaseSpeed;
            bool ok = agent.SetDestination(testPlayer.position);
        }
    }

    public bool PlayerInSight()
    {
        if (testPlayer == null)
        {
            return false;
        }

        Vector3 dirToPlayer = (testPlayer.position - transform.position);
        if (dirToPlayer.magnitude > sightRange)
        {
            return false;
        }

        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        if (angle > fovAngle / 2f)
        {
            return false;
        }

        if (Physics.Raycast(transform.position + Vector3.up, dirToPlayer.normalized, out RaycastHit hit, sightRange, obstacleMask | playerLayer))
        {
            if (hit.collider.CompareTag("Player"))
            {
                SetLastKnownPosition(testPlayer.position);
                lastSeenTime = Time.time;
                hasEverSeenPlayer = true;
                return true;
            }
        }
        return false;
    }

    public void SetLastKnownPosition(Vector3 position)
    {
        bool ok = NavMesh.SamplePosition(position, out NavMeshHit hit, 1.0f, NavMesh.AllAreas);
        if (!ok) ok = NavMesh.SamplePosition(position, out hit, 5.0f, NavMesh.AllAreas);
        lastKnownPosition = hit.position;
    }

    void MoveTowards(Vector3 target, float speed)
    {
        agent.speed = speed;
        bool ok = agent.SetDestination(target);
    }

    public void OnPlayerShot(Vector3 shotPosition)
    {
        if (currentState != State.Chase)
        {
            bool ok = NavMesh.SamplePosition(shotPosition, out NavMeshHit hit, 5.0f, NavMesh.AllAreas);
            lastKnownPosition = ok ? hit.position : shotPosition;
            isGoingToLastKnownPosition = true;
            lastSeenTime = float.NegativeInfinity;
            hasEverSeenPlayer = true;
            SwitchState(State.Chase);
            agent.SetDestination(lastKnownPosition);
        }
    }
}
