using UnityEngine;

public class Patroller : WoodEnemy
{
    enum State { Patrol, Chase, Attack, Return }


    State state;
    float home;
    float waypoint;
    float strikeTime;

    void Start()
    {
        home = transform.position.x;
        waypoint = home - stats.patrolDistance;
    }

    void FixedUpdate()
    {
        if (player == null || MysticGame.Instance == null || MysticGame.Instance.RoundEnded)
            return;

        Vector2 offset = (Vector2)player.position - body.position;
        bool seen = CanSee(stats.detectionRange) &&
            Vector2.Angle(Vector2.right * facing, offset) <= stats.viewAngle * 0.5f;

        switch (state)
        {
            case State.Patrol:
                if (seen)
                    state = State.Chase;
                else if (MoveTowards(waypoint, stats.movementSpeed))
                    waypoint = waypoint < home ? home + stats.patrolDistance : home - stats.patrolDistance;
                break;

            case State.Chase:
                if (!seen)
                    state = State.Return;
                else if (offset.magnitude > stats.attackRange)
                    MoveTowards(player.position.x, stats.chaseSpeed);
                else if (Time.time >= readyTime)
                {
                    Warn(true);
                    strikeTime = Time.time + stats.windUpTime;
                    state = State.Attack;
                }
                break;

            case State.Attack:
                if (seen && Time.time < strikeTime)
                    break;

                if (seen && offset.magnitude <= stats.attackRange)
                    MysticGame.Instance.Damage(stats.attackDamage);

                Warn(false);
                readyTime = Time.time + stats.cooldownTime;
                state = seen ? State.Chase : State.Return;
                break;

            case State.Return:
                if (seen)
                    state = State.Chase;
                else if (MoveTowards(home, stats.movementSpeed))
                    state = State.Patrol;
                break;
        }
    }
}
