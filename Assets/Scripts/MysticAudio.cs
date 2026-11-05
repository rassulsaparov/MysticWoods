
using UnityEngine;

public class MysticAudio : MonoBehaviour
{
    public static MysticAudio Instance;

    private AudioSource musicSource;
    private AudioSource effectsSource;

    private AudioClip jumpSound;
    private AudioClip coinSound;
    private AudioClip shootSound;
    private AudioClip damageSound;
    private AudioClip backgroundMusic;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        effectsSource = gameObject.AddComponent<AudioSource>();

        musicSource.volume = 0.15f;
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        effectsSource.volume = 0.45f;
        effectsSource.playOnAwake = false;

        jumpSound = MakeTone(500f, 850f, 0.18f);
        coinSound = MakeTone(900f, 1400f, 0.20f);
        shootSound = MakeTone(750f, 300f, 0.12f);
        damageSound = MakeTone(300f, 100f, 0.35f);

        backgroundMusic = MakeMusic();

        musicSource.clip = backgroundMusic;
        musicSource.Play();
    }

    AudioClip MakeTone(
        float startFreq,
        float endFreq,
        float duration)
    {
        int sampleRate = 44100;
        int length = Mathf.RoundToInt(sampleRate * duration);

        float[] samples = new float[length];
        float phase = 0f;

        for (int i = 0; i < length; i++)
        {
            float t = (float)i / length;

            float frequency = Mathf.Lerp(
                startFreq,
                endFreq,
                t
            );

            phase += 2f * Mathf.PI * frequency / sampleRate;

            float envelope = Mathf.Pow(1f - t, 2f);

            samples[i] =
                Mathf.Sin(phase) * envelope * 0.5f;
        }

        AudioClip clip = AudioClip.Create(
            "GeneratedSound",
            length,
            1,
            sampleRate,
            false
        );

        clip.SetData(samples, 0);
        return clip;
    }

    AudioClip MakeMusic()
    {
        int sampleRate = 44100;
        float duration = 8f;

        int length = Mathf.RoundToInt(
            sampleRate * duration
        );

        float[] samples = new float[length];

        float[] notes =
        {
            261.63f,
            329.63f,
            392.00f,
            329.63f,
            293.66f,
            349.23f,
            440.00f,
            392.00f
        };

        for (int i = 0; i < length; i++)
        {
            float time = (float)i / sampleRate;

            int noteIndex =
                Mathf.FloorToInt(time) % notes.Length;

            float frequency = notes[noteIndex];

            float noteTime = time % 1f;

            float envelope =
                Mathf.Min(1f, noteTime * 12f) *
                Mathf.Min(1f, (1f - noteTime) * 5f);

            float melody = Mathf.Sin(
                2f * Mathf.PI * frequency * time
            );

            float bass = Mathf.Sin(
                2f * Mathf.PI * (frequency / 4f) * time
            );

            samples[i] =
                (melody * 0.14f + bass * 0.10f) *
                envelope;
        }

        AudioClip clip = AudioClip.Create(
            "MysticWoodsMusic",
            length,
            1,
            sampleRate,
            false
        );

        clip.SetData(samples, 0);
        return clip;
    }

    public void PlayJump()
    {
        if (effectsSource != null)
            effectsSource.PlayOneShot(jumpSound);
    }

    public void PlayCoin()
    {
        if (effectsSource != null)
            effectsSource.PlayOneShot(coinSound);
    }

    public void PlayShoot()
    {
        if (effectsSource != null)
            effectsSource.PlayOneShot(shootSound);
    }

    public void PlayDamage()
    {
        if (effectsSource != null)
            effectsSource.PlayOneShot(damageSound);
    }
}
