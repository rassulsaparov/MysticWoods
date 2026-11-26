using UnityEngine;

[CreateAssetMenu(fileName = "GameData", menuName = "Mystic Woods/Game Data")]
public class GameData : ScriptableObject
{
    public PlayerStats playerStats;
    public ItemValues itemValues;
    public LevelData[] levels;
    [Range(0, 1)] public float defaultMusicVolume;
    [Range(0, 1)] public float defaultEffectsVolume;

    static GameData instance;

    public static GameData Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<GameData>("GameData");

            return instance;
        }
    }
}
