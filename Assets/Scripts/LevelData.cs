using UnityEngine;

public enum LevelGoal { CollectCoins, DefeatBoss }

[System.Serializable]
public class PlatformData
{
    public Vector2 position;
    public Vector2 size;
    public bool moving;
    [Min(0)] public float movementDistance;
    [Min(0)] public float movementFrequency;
}

[System.Serializable]
public class EnemySpawn
{
    public WoodEnemy prefab;
    public Vector2 position;
}

[CreateAssetMenu(fileName = "LevelData", menuName = "Mystic Woods/Level Data")]
public class LevelData : ScriptableObject
{
    public string title;
    public LevelGoal goal;
    [Min(0)] public int goalAmount;
    [Min(0)] public float timeLimit;
    [Min(0)] public int completionScore;
    public Vector2 playerSpawn;
    public float fallHeight;
    public Vector2 exitPosition;
    public Vector2 exitSize;
    public PlatformData[] platforms;
    public Vector2[] coins;
    public Vector2[] hazards;
    public EnemySpawn[] enemies;
    public EnemyStats bossStats;
    public Vector2 bossPosition;
    public Vector2 bossSize;
    public Vector2 bossShotOffset;
    public Vector2 bossProjectileSize;
}
