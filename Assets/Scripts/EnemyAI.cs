using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public enum State { Patrol, Wait, Chase }

public class EnemyAI : BaseEnemyAI
{
    public State currentState;

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
    public float forgetTimer = 0f;
    public float forgetDuration = 3f;
    Vector3 lastKnownPosition;
    public bool isGoingToLastKnownPosition = false;

    protected override void Start()
    {
        base.Start();
        SwitchState(State.Patrol);
    }

    private void Update()
    {
        if (PlayerInSight())
        {
            if (currentState != State.Chase)
            {
                SwitchState(State.Chase);
            }
            forgetTimer = 0f;
            return;
        }

        if (currentState == State.Chase && !isGoingToLastKnownPosition)
        {
            forgetTimer += Time.deltaTime;
            if (forgetTimer >= forgetDuration)
            {
                isGoingToLastKnownPosition = true;
                agent.SetDestination(lastKnownPosition);
            }
        }
        else if (forgetTimer != 0f)
        {
            forgetTimer = 0f;
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
                Chase();
                break;
        }
    }

    void SwitchState(State newState, float delay = 0f)
    {
        bool wasChasing = currentState == State.Chase;
        currentState = newState;
        timer = delay;

        if (newState == State.Patrol)
        {
            GenerateNewPatrolPoint();
        }

        if (newState != State.Chase && !wasChasing)
        {
            forgetTimer = 0f;
            isGoingToLastKnownPosition = false;
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

    void Chase()
    {
        if (isGoingToLastKnownPosition)
        {
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                isGoingToLastKnownPosition = false;
                SwitchState(State.Wait, 2f);
            }
            return;
        }

        if (PlayerInSight())
        {
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);
        }
    }

    public bool PlayerInSight()
    {
        Vector3 dirToPlayer = (player.position - transform.position);
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
                SetLastKnownPosition(player.position);
                return true;
            }
        }
        return false;
    }

    public void SetLastKnownPosition(Vector3 position)
    {
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
        {
            lastKnownPosition = hit.position;
        }
        else
        {
            NavMesh.SamplePosition(position, out hit, 5.0f, NavMesh.AllAreas);
            lastKnownPosition = hit.position;
        }
    }

    void MoveTowards(Vector3 target, float speed)
    {
        agent.speed = speed;
        agent.SetDestination(target);
    }

    public void OnPlayerShot(Vector3 shotPosition)
    {
        if (currentState != State.Chase)
        {
            lastKnownPosition = shotPosition;
            isGoingToLastKnownPosition = true;
            forgetTimer = Mathf.Infinity;
            SwitchState(State.Chase);
        }
    }
}
