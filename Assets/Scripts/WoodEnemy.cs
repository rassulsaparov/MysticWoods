using UnityEngine;

public abstract class WoodEnemy : MonoBehaviour
{
    [SerializeField] protected EnemyStats stats;
    int hp;

    protected Rigidbody2D body;
    protected SpriteRenderer visual;
    protected Transform player;
    protected Camera gameCamera;
    protected float readyTime;
    protected float facing = -1f;

    Color normalColor;
    bool warning;
    ContactFilter2D solids;
    RaycastHit2D[] hits = new RaycastHit2D[8];
    BoxCollider2D box;

    public void SetPlayer(Transform target)
    {
        if (stats == null)
        {
            Debug.LogError("Mystic Woods: Enemy stats are missing.");
            enabled = false;
            return;
        }

        hp = stats.maxHP;
        player = target;
        gameCamera = Camera.main;
        body = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
        visual = GetComponent<SpriteRenderer>();
        normalColor = visual.color;
        solids.useTriggers = false;
    }

    public void TakeHit(int amount)
    {
        if (hp <= 0)
            return;

        hp = Mathf.Max(0, hp - amount);
        if (hp == 0)
        {
            MysticAudio.Play(Sfx.EnemyDeath);
            MysticGame.Instance.AddScore(GameData.Instance.itemValues.enemyScore);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    public static bool OnScreen(Camera camera, Vector3 position)
    {
        if (camera == null)
            return false;

        Vector3 point = camera.WorldToViewportPoint(position);
        return point.z > 0f && point.x >= 0f && point.x <= 1f &&
            point.y >= 0f && point.y <= 1f;
    }

    protected bool CanSee(float range)
    {
        if (player == null || MysticGame.Instance == null ||
            MysticGame.Instance.RoundEnded || !OnScreen(gameCamera, player.position) ||
            !OnScreen(gameCamera, transform.position))
            return false;

        Vector2 offset = (Vector2)player.position - body.position;
        if (offset.magnitude > range)
            return false;

        int count = Physics2D.Raycast(body.position, offset.normalized,
            solids, hits, offset.magnitude);
        return count == 0 || hits[0].transform == player;
    }

    protected void Face(float direction)
    {
        if (direction == 0f)
            return;

        facing = Mathf.Sign(direction);
        visual.flipX = facing > 0f;
    }

    protected bool MoveTowards(float targetX, float speed)
    {
        float distance = targetX - body.position.x;
        Face(distance);
        float step = Mathf.Min(Mathf.Abs(distance), speed * Time.fixedDeltaTime);
        Vector2 direction = Vector2.right * facing;
        int count = Physics2D.BoxCast(body.position, box.bounds.size, 0f,
            direction, solids, hits, step);
        for (int i = 0; i < count; i++)
        {
            if (hits[i].transform != player)
                step = Mathf.Min(step, hits[i].distance);
        }

        Vector2 next = body.position + direction * step;
        Vector2 front = next + direction * box.bounds.extents.x;
        if (!OnScreen(gameCamera, front))
            return true;

        body.MovePosition(next);
        return step >= Mathf.Abs(distance) || step <= Mathf.Epsilon;
    }

    // Called with true at the first frame of every wind-up (Patroller, Shooter, Charger)
    // and with false when the attack lands or is cancelled.
    protected void Warn(bool active)
    {
        if (active && !warning)
            MysticAudio.Play(Sfx.EnemyWindUp);

        warning = active;
        visual.color = active ? Color.yellow : normalColor;
    }
}
