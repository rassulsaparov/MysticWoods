using UnityEngine;

public class Shooter : WoodEnemy
{
    enum State { Idle, Aim, Shoot, Cooldown }


    State state;
    Vector2 direction;

    void FixedUpdate()
    {
        if (player == null || MysticGame.Instance == null || MysticGame.Instance.RoundEnded)
            return;

        switch (state)
        {
            case State.Idle:
                if (CanSee(Mathf.Min(stats.detectionRange, stats.attackRange)))
                {
                    direction = ((Vector2)player.position - body.position).normalized;
                    Face(direction.x);
                    Warn(true);
                    readyTime = Time.time + stats.windUpTime;
                    state = State.Aim;
                }
                break;

            case State.Aim:
                if (!CanSee(stats.attackRange) || Time.time >= readyTime)
                {
                    Warn(false);
                    state = State.Shoot;
                }
                break;

            case State.Shoot:
                if (CanSee(stats.attackRange) && Time.time >= readyTime)
                    MysticGame.Instance.EnemyShot(body.position, direction, stats.projectileSpeed, stats.attackDamage, stats.projectileLifetime);

                readyTime = Time.time + stats.cooldownTime;
                state = State.Cooldown;
                break;

            case State.Cooldown:
                if (Time.time >= readyTime)
                    state = State.Idle;
                break;
        }
    }
}
