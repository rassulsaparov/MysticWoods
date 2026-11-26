using UnityEngine;

public class Charger : WoodEnemy
{
    enum State { Idle, Prepare, Charge, Recover }


    State state;
    float targetX;

    void FixedUpdate()
    {
        if (player == null || MysticGame.Instance == null || MysticGame.Instance.RoundEnded)
            return;

        switch (state)
        {
            case State.Idle:
                if (CanSee(stats.detectionRange) && Mathf.Abs(player.position.y - body.position.y) <= stats.laneHeight)
                {
                    Face(player.position.x - body.position.x);
                    targetX = body.position.x + facing * stats.chargeDistance;
                    Warn(true);
                    readyTime = Time.time + stats.windUpTime;
                    state = State.Prepare;
                }
                break;

            case State.Prepare:
                if (!CanSee(stats.detectionRange))
                    Recover();
                else if (Time.time >= readyTime)
                {
                    Warn(false);
                    state = State.Charge;
                }
                break;

            case State.Charge:
                if (!OnScreen(gameCamera, player.position) || !OnScreen(gameCamera, transform.position))
                    Recover();
                else if ((player.position.x - body.position.x) * facing >= 0f && CanSee(stats.attackRange))
                {
                    MysticGame.Instance.Damage(stats.attackDamage);
                    Recover();
                }
                else if (MoveTowards(targetX, stats.chargeSpeed))
                    Recover();
                break;

            case State.Recover:
                if (Time.time >= readyTime)
                    state = State.Idle;
                break;
        }
    }

    void Recover()
    {
        Warn(false);
        readyTime = Time.time + stats.cooldownTime;
        state = State.Recover;
    }
}
