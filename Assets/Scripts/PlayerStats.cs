using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "Mystic Woods/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Min(1)] public int maxHP;
    [Min(0)] public float moveSpeed;
    [Min(0)] public float jumpForce;
    [Min(1)] public int attackDamage;
    [Min(0)] public float shotCooldown;
    [Min(0)] public float projectileSpeed;
    [Min(0)] public float projectileLifetime;
    [Min(0)] public float invulnerabilityTime;
    public Vector2 shotOffset;
    public Vector2 projectileSize;
}
