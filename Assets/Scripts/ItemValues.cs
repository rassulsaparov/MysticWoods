using UnityEngine;

[CreateAssetMenu(fileName = "ItemValues", menuName = "Mystic Woods/Item Values")]
public class ItemValues : ScriptableObject
{
    [Min(1)] public int coinValue;
    [Min(0)] public int coinScore;
    [Min(0)] public int enemyScore;
    [Min(0)] public int bossScore;
    [Min(1)] public int hazardDamage;
    public Vector2 coinSize;
    public Vector2 hazardSize;
}
