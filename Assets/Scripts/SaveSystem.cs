using System;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    static SaveData data;

    public static string FilePath =>
        Path.Combine(Application.persistentDataPath, "save.json");

    public static string LastError { get; private set; }
    public static string LoadNotice { get; private set; }
    public static int SelectedLevel { get; set; }

    public static SaveData Data
    {
        get
        {
            if (data == null)
                data = Load();

            return data;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Initialize()
    {
        data = null;
        SelectedLevel = 0;
        LastError = "";
        LoadNotice = "";
    }

    public static SaveData Defaults()
    {
        return new SaveData
        {
            musicVolume = GameData.Instance.defaultMusicVolume,
            effectsVolume = GameData.Instance.defaultEffectsVolume
        };
    }

    public static SaveData Load()
    {
        LoadNotice = "";
        if (!File.Exists(FilePath))
            return Defaults();

        try
        {
            string json = File.ReadAllText(FilePath).Trim();
            if (!json.StartsWith("{") || !json.EndsWith("}"))
                throw new InvalidDataException("Incomplete save.");

            SaveData loaded = new SaveData
            {
                version = 0,
                currentLevel = 0,
                unlockedLevels = null,
                completedLevels = null,
                musicVolume = float.NaN,
                effectsVolume = float.NaN
            };
            JsonUtility.FromJsonOverwrite(json, loaded);
            if (!Valid(loaded))
                throw new InvalidDataException("Invalid save values.");

            return loaded;
        }
        catch (Exception error)
        {
            LoadNotice = "Save could not be loaded. Default progress is active.";
            Debug.LogWarning("Mystic Woods: Using default save. " + error.Message);
            return Defaults();
        }
    }

    static bool Valid(SaveData value)
    {
        if (value == null || value.version != 1 || value.bestScore < 0 ||
            value.unlockedLevels == null || value.completedLevels == null ||
            !value.unlockedLevels.Contains(1) ||
            !value.unlockedLevels.Contains(value.currentLevel) ||
            !ValidVolume(value.musicVolume) || !ValidVolume(value.effectsVolume))
            return false;

        int count = GameData.Instance.levels.Length;
        foreach (int level in value.unlockedLevels)
        {
            if (level < 1 || level > count)
                return false;
        }

        foreach (int level in value.completedLevels)
        {
            if (!value.unlockedLevels.Contains(level))
                return false;
        }

        return true;
    }

    static bool ValidVolume(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value <= 1f;
    }

    public static bool Save()
    {
        return Write(Data);
    }

    static bool Write(SaveData value)
    {
        string temporary = Path.Combine(Application.persistentDataPath, "save.tmp");
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(temporary, JsonUtility.ToJson(value, true));
            if (File.Exists(FilePath))
                File.Replace(temporary, FilePath, null);
            else
                File.Move(temporary, FilePath);

            LastError = "";
            return true;
        }
        catch (Exception error)
        {
            LastError = "Could not save progress. Please try again.";
            Debug.LogWarning("Mystic Woods: Save failed. " + error.Message);
            return false;
        }
    }

    public static void LevelCompleted(int level, int score)
    {
        if (!Data.completedLevels.Contains(level))
            Data.completedLevels.Add(level);

        Data.bestScore = Mathf.Max(Data.bestScore, score);
        int next = Mathf.Min(level + 1, GameData.Instance.levels.Length);
        if (!Data.unlockedLevels.Contains(next))
            Data.unlockedLevels.Add(next);

        Data.currentLevel = next;
        Save();
    }

    // Volumes are stored as linear 0..1 slider values; MysticAudio converts them to decibels.
    // save = false is used while a slider is being dragged; the menu writes the file shortly after.
    public static bool ChangeVolume(bool music, float volume, bool save = true)
    {
        if (music)
            Data.musicVolume = Mathf.Clamp01(volume);
        else
            Data.effectsVolume = Mathf.Clamp01(volume);

        if (MysticAudio.Instance != null)
            MysticAudio.Instance.ApplySettings();

        return !save || Save();
    }

    public static bool ResetProgress()
    {
        SaveData defaults = Defaults();
        if (!Write(defaults))
            return false;

        data = defaults;
        SelectedLevel = 0;
        LoadNotice = "";
        if (MysticAudio.Instance != null)
            MysticAudio.Instance.ApplySettings();

        return true;
    }
}
