using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Every sound event the game can play. Gameplay scripts call MysticAudio.Play(Sfx.X).
public enum Sfx
{
    Jump,
    Hit,
    Pickup,
    EnemyWindUp,
    EnemyDeath,
    UIClick,
    Win,
    Lose,
    Shoot
}

// AudioManager: owns the AudioMixer, plays sound effects through the SFX group,
// applies saved volumes in decibels and switches the Paused snapshot.
// One instance is created before the first scene loads and survives scene changes.
public class MysticAudio : MonoBehaviour
{
    public const float MinDecibels = -80f;
    public const string MusicParameter = "MusicVolume";
    public const string EffectsParameter = "SFXVolume";
    public const string MasterParameter = "MasterVolume";

    const int VoiceCount = 10;
    const float PauseFadeTime = 0.35f;
    const float ResumeFadeTime = 0.5f;
    const float RepeatGuard = 0.04f;

    public static MysticAudio Instance { get; private set; }

    public AudioMixerGroup MusicGroup { get; private set; }

    AudioMixer mixer;
    AudioMixerGroup effectsGroup;
    AudioMixerSnapshot normalSnapshot;
    AudioMixerSnapshot pausedSnapshot;

    AudioSource[] voices;
    int nextVoice;

    readonly Dictionary<Sfx, AudioClip[]> clips = new Dictionary<Sfx, AudioClip[]>();
    readonly Dictionary<Sfx, int> lastClip = new Dictionary<Sfx, int>();
    readonly Dictionary<Sfx, float> lastPlayed = new Dictionary<Sfx, float>();
    readonly HashSet<Button> clickButtons = new HashSet<Button>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null)
            return;

        GameObject audioSystem = new GameObject("AudioSystem");
        audioSystem.AddComponent<MysticAudio>();
        audioSystem.AddComponent<MusicManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // An older scene copy of the AudioManager: the bootstrap instance already exists.
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        mixer = Resources.Load<AudioMixer>("Audio/MysticMixer");
        if (mixer == null)
        {
            Debug.LogError("Mystic Woods: Resources/Audio/MysticMixer is missing.");
        }
        else
        {
            MusicGroup = FindGroup("MusicTracks");
            effectsGroup = FindGroup("SFX");
            normalSnapshot = mixer.FindSnapshot("Normal");
            pausedSnapshot = mixer.FindSnapshot("Paused");
        }

        voices = new AudioSource[VoiceCount];
        for (int i = 0; i < VoiceCount; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = effectsGroup;
            voices[i] = source;
        }

        LoadClips(Sfx.Jump, "jump_1", "jump_2");
        LoadClips(Sfx.Hit, "hit_1", "hit_2");
        LoadClips(Sfx.Pickup, "pickup_1", "pickup_2");
        LoadClips(Sfx.EnemyWindUp, "enemy_windup_1", "enemy_windup_2");
        LoadClips(Sfx.EnemyDeath, "enemy_death_1", "enemy_death_2");
        LoadClips(Sfx.UIClick, "ui_click");
        LoadClips(Sfx.Win, "win");
        LoadClips(Sfx.Lose, "lose");
        LoadClips(Sfx.Shoot, "shoot_1", "shoot_2");

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        if (Instance != this)
            return;

        // AudioMixer.SetFloat is ignored when called in Awake, so volumes are applied here.
        ApplySettings();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    AudioMixerGroup FindGroup(string groupName)
    {
        foreach (AudioMixerGroup group in mixer.FindMatchingGroups(string.Empty))
        {
            if (group.name == groupName)
                return group;
        }

        Debug.LogError("Mystic Woods: Mixer group not found: " + groupName);
        return null;
    }

    void LoadClips(Sfx sound, params string[] names)
    {
        List<AudioClip> loaded = new List<AudioClip>();
        foreach (string clipName in names)
        {
            AudioClip clip = Resources.Load<AudioClip>("Audio/" + clipName);
            if (clip != null)
                loaded.Add(clip);
            else
                Debug.LogWarning("Mystic Woods: Sound not found: Audio/" + clipName);
        }

        clips[sound] = loaded.ToArray();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Leaving the pause menu by Restart or Main Menu reloads a scene:
        // always return to the normal mix.
        SetPaused(false);

        clickButtons.RemoveWhere(button => button == null);
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include))
            RegisterButton(button);
    }

    // ------------------------------------------------------------ playback

    public static void Play(Sfx sound)
    {
        if (Instance != null)
            Instance.PlaySound(sound);
    }

    void PlaySound(Sfx sound)
    {
        if (!clips.TryGetValue(sound, out AudioClip[] options) || options.Length == 0)
            return;

        // One game event must give one sound, even if two code paths report it in the same frame.
        float now = Time.unscaledTime;
        if (lastPlayed.TryGetValue(sound, out float previous) && now - previous < RepeatGuard)
            return;

        lastPlayed[sound] = now;

        int index = 0;
        if (options.Length > 1)
        {
            int last = lastClip.TryGetValue(sound, out int value) ? value : -1;
            index = Random.Range(0, options.Length);
            if (index == last)
                index = (index + 1) % options.Length;
        }

        lastClip[sound] = index;

        AudioSource voice = FreeVoice();
        voice.clip = options[index];
        voice.pitch = Varies(sound) ? Random.Range(0.92f, 1.08f) : 1f;
        voice.volume = Level(sound);
        voice.Play();
    }

    AudioSource FreeVoice()
    {
        for (int i = 0; i < voices.Length; i++)
        {
            AudioSource candidate = voices[(nextVoice + i) % voices.Length];
            if (!candidate.isPlaying)
            {
                nextVoice = (nextVoice + i + 1) % voices.Length;
                return candidate;
            }
        }

        // All voices busy: reuse the oldest one.
        AudioSource oldest = voices[nextVoice];
        nextVoice = (nextVoice + 1) % voices.Length;
        return oldest;
    }

    static bool Varies(Sfx sound)
    {
        // Jingles keep their tuning; everything that repeats during play gets pitch variation.
        return sound != Sfx.Win && sound != Sfx.Lose;
    }

    static float Level(Sfx sound)
    {
        switch (sound)
        {
            case Sfx.EnemyWindUp: return 1f;
            case Sfx.Hit: return 0.95f;
            case Sfx.Win:
            case Sfx.Lose: return 0.9f;
            case Sfx.EnemyDeath: return 0.85f;
            case Sfx.Pickup: return 0.75f;
            case Sfx.Jump: return 0.6f;
            case Sfx.UIClick: return 0.6f;
            case Sfx.Shoot: return 0.5f;
            default: return 1f;
        }
    }

    // ----------------------------------------------------------- UI clicks

    public static void RegisterButton(Button button)
    {
        if (Instance == null || button == null || button.GetComponent<MysticHoldButton>() != null)
            return;

        if (Instance.clickButtons.Add(button))
            button.onClick.AddListener(() => Play(Sfx.UIClick));
    }

    // ------------------------------------------------------ volume and mix

    public static float ToDecibels(float linear)
    {
        if (linear <= 0.0001f)
            return MinDecibels;

        return Mathf.Max(MinDecibels, Mathf.Log10(linear) * 20f);
    }

    public void ApplySettings()
    {
        if (mixer == null)
            return;

        mixer.SetFloat(MasterParameter, 0f);
        mixer.SetFloat(MusicParameter, ToDecibels(SaveSystem.Data.musicVolume));
        mixer.SetFloat(EffectsParameter, ToDecibels(SaveSystem.Data.effectsVolume));
    }

    public static void SetPaused(bool paused)
    {
        if (Instance == null)
            return;

        AudioMixerSnapshot target = paused ? Instance.pausedSnapshot : Instance.normalSnapshot;
        if (target != null)
            target.TransitionTo(paused ? PauseFadeTime : ResumeFadeTime);
    }
}
