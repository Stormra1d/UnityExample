using UnityEngine;

public class DumbEnemyAI : BaseEnemyAI
{
    public float chaseSpeed = 5f;

    protected override void Start()
    {
        base.Start();
        agent.speed = chaseSpeed;
    }

    private void Update()
    {
        if (player != null)
        {
            agent.SetDestination(player.position);
        }
    }
}
