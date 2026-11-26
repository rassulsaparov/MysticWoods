using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public int version = 1;
    public int bestScore;
    public int currentLevel = 1;
    public List<int> unlockedLevels = new List<int> { 1 };
    public List<int> completedLevels = new List<int>();
    public float musicVolume;
    public float effectsVolume;
}
