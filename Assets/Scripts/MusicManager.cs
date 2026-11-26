using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Plays the menu and level tracks through the MusicTracks mixer group.
// It lives on the persistent AudioSystem object, so a scene reload or a new level
// keeps the current track playing; only a change of track starts a crossfade.
public class MusicManager : MonoBehaviour
{
    public const float CrossfadeTime = 1f;
    const string MenuScene = "MainMenu";

    public static MusicManager Instance { get; private set; }

    readonly AudioSource[] decks = new AudioSource[2];
    int activeDeck;
    AudioClip menuTrack;
    AudioClip levelTrack;
    Coroutine fade;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        menuTrack = Resources.Load<AudioClip>("Audio/music_menu");
        levelTrack = Resources.Load<AudioClip>("Audio/music_level");
        if (menuTrack == null || levelTrack == null)
            Debug.LogError("Mystic Woods: Music clips are missing from Resources/Audio.");

        for (int i = 0; i < decks.Length; i++)
        {
            AudioSource deck = gameObject.AddComponent<AudioSource>();
            deck.playOnAwake = false;
            deck.loop = true;
            deck.volume = 0f;
            deck.spatialBlend = 0f;
            deck.priority = 0;
            deck.outputAudioMixerGroup =
                MysticAudio.Instance != null ? MysticAudio.Instance.MusicGroup : null;
            decks[i] = deck;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        if (Instance == this)
            PlayFor(SceneManager.GetActiveScene());
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayFor(scene);
    }

    void PlayFor(Scene scene)
    {
        Play(scene.name == MenuScene ? menuTrack : levelTrack);
    }

    public void Play(AudioClip track)
    {
        if (track == null)
            return;

        AudioSource current = decks[activeDeck];
        if (current.clip == track && current.isPlaying)
            return; // Same track: keep playing, no restart.

        AudioSource incoming = decks[1 - activeDeck];
        if (incoming.clip != track || !incoming.isPlaying)
        {
            incoming.clip = track;
            incoming.volume = 0f;
            incoming.time = 0f;
            incoming.Play();
        }

        if (fade != null)
            StopCoroutine(fade);

        fade = StartCoroutine(Crossfade(current, incoming));
        activeDeck = 1 - activeDeck;
    }

    IEnumerator Crossfade(AudioSource outgoing, AudioSource incoming)
    {
        float outStart = outgoing.volume;
        float inStart = incoming.volume;
        float elapsed = 0f;

        // Unscaled time: the fade keeps running while the game is paused (timeScale = 0).
        while (elapsed < CrossfadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / CrossfadeTime);

            // Equal-power curves keep the loudness steady in the middle of the fade.
            incoming.volume = Mathf.Lerp(inStart, 1f, Mathf.Sin(t * Mathf.PI * 0.5f));
            outgoing.volume = Mathf.Lerp(outStart, 0f, 1f - Mathf.Cos(t * Mathf.PI * 0.5f));
            yield return null;
        }

        incoming.volume = 1f;
        outgoing.volume = 0f;
        outgoing.Stop();
        outgoing.clip = null;
        fade = null;
    }
}
