# MysticWoods
2D mobile adventure game made with Unity

2D platformer made with Unity 6000.6.2f1. Start from `Assets/Scenes/MainMenu.unity`. Gameplay runs in `SampleScene`.

## Sound Pass (Assignment 12)

| Part | Where |
| --- | --- |
| AudioMixer | `Assets/Resources/Audio/MysticMixer.mixer`: Master → Music → MusicTracks, Master → SFX. Snapshots: **Normal** and **Paused** |
| Exposed parameters | `MasterVolume`, `MusicVolume` (Music group), `SFXVolume` (SFX group) |
| AudioManager | `Assets/Scripts/MysticAudio.cs`: sound effects, mixer volumes in dB, Paused snapshot, UI clicks |
| MusicManager | `Assets/Scripts/MusicManager.cs`: menu and level tracks, 1 s equal-power crossfade |
| Settings | Main menu → SETTINGS: Music and Effects sliders, saved in `save.json` by `SaveSystem` |

Sound events: jump, player hit, coin pickup, enemy wind-up (Patroller, Shooter, Charger and the boss), enemy death, UI click, level win, lose, player shot.

## Sound and music sources and licenses

I made every sound and both music tracks for this project. The script `Tools/generate_audio.py` synthesizes them using only the Python 3 standard library. It uses a fixed random seed, so running it again recreates the same files. The project uses no third-party recordings or samples.

Regenerate:

```
py -I Tools/generate_audio.py
```

| File (`Assets/Resources/Audio/`) | Used for | Source | License |
| --- | --- | --- | --- |
| `jump_1.wav`, `jump_2.wav` | Player jump | Original, `generate_audio.py` (`jump`) | CC0 1.0 |
| `hit_1.wav`, `hit_2.wav` | Player takes damage | Original, `generate_audio.py` (`hit`) | CC0 1.0 |
| `pickup_1.wav`, `pickup_2.wav` | Coin pickup, effects slider preview | Original, `generate_audio.py` (`pickup`) | CC0 1.0 |
| `enemy_windup_1.wav`, `enemy_windup_2.wav` | Start of every enemy wind-up | Original, `generate_audio.py` (`windup`) | CC0 1.0 |
| `enemy_death_1.wav`, `enemy_death_2.wav` | Enemy or boss defeated | Original, `generate_audio.py` (`enemy_death`) | CC0 1.0 |
| `ui_click.wav` | Every menu, pause and end-panel button | Original, `generate_audio.py` (`ui_click`) | CC0 1.0 |
| `win.wav` | Level completed / game won | Original, `generate_audio.py` (`win`) | CC0 1.0 |
| `lose.wav` | Game over / time up | Original, `generate_audio.py` (`lose`) | CC0 1.0 |
| `shoot_1.wav`, `shoot_2.wav` | Player shot | Original, `generate_audio.py` (`shoot`) | CC0 1.0 |
| `music_menu.wav` | Main menu music, 27.4 s loop, A minor, 70 BPM | Original, `generate_audio.py` (`menu_theme`) | CC0 1.0 |
| `music_level.wav` | Level music, 36.9 s loop, D dorian, 104 BPM | Original, `generate_audio.py` (`level_theme`) | CC0 1.0 |

CC0 1.0: https://creativecommons.org/publicdomain/zero/1.0/
