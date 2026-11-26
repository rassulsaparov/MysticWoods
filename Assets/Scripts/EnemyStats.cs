using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStats", menuName = "Mystic Woods/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    [Min(1)] public int maxHP;
    [Min(1)] public int attackDamage;
    [Min(0)] public float movementSpeed;
    [Min(0)] public float chaseSpeed;
    [Min(0)] public float detectionRange;
    [Min(0)] public float attackRange;
    [Range(0, 360)] public float viewAngle;
    [Min(0)] public float patrolDistance;
    [Min(0)] public float patrolFrequency;
    [Range(0.3f, 0.8f)] public float windUpTime;
    [Min(0)] public float cooldownTime;
    [Min(0)] public float projectileSpeed;
    [Min(0)] public float projectileLifetime;
    [Min(0)] public float chargeSpeed;
    [Min(0)] public float chargeDistance;
    [Min(0)] public float laneHeight;
}
