# Mystic Woods — Assignment 10

## Proposal and implementation

Keep the existing level builder, player, camera, menus and artwork. Add three enemy prefabs to the first stage of SampleScene so a tester can encounter all three on one run. The second stage uses the Patroller in its original enemy slot. The third stage retains the boss.

MysticGame spawns the prefabs and passes each one the existing player. WoodEnemy shares visibility checks, horizontal movement and the warning tint. Each prefab has its own script and state machine. All three use the Enemy sprite from MysticSprites. The Shooter fires the existing Bullet sprite along the direction chosen when it starts aiming. Its projectile checks the first solid collider along each movement step. Player shots can defeat all three enemies.

| Prefab | States | Warning | Cooldown |
| --- | --- | --- | --- |
| Patroller | Patrol → Chase → Attack → Return to patrol after losing sight | 0.45 s | 1.6 s |
| Shooter | Idle → Aim → Shoot → Cooldown | 0.6 s | 2.1 s |
| Charger | Idle → Prepare → Charge → Recover | 0.7 s | 2.2 s |

Edit each prefab in Assets/Prefabs before entering Play Mode to retain tuning changes. The Patroller checks distance, viewing angle and an unobstructed ray. A yellow tint marks each wind-up. The Charger commits to a horizontal direction, so jumping or retreating during preparation can avoid the dash. Enemy damage uses the existing shared health and invulnerability timer.

## Issues found and changes made

- The old game bootstrap checked the active scene only on application startup. Starting in MainMenu skipped setup, and the gameplay scene had no saved MysticGame component. A scene-owned MysticGame now runs on entry and restart and holds the prefab references.
- Pausing set the time scale to zero while player movement and fire still processed input in Update. Both gameplay input paths now skip paused frames.
- Player settings allowed both portrait orientations during autorotation. Only the two landscape directions are now enabled.
- Stage replacement deferred destruction until the end of the frame. Generated objects are now disabled before removal, and a coin must still belong to the current level before it can increase the counter.

## Verification

The project gameplay scripts compiled with the compiler and assembly references from Unity 6000.6.2f1. Static checks resolved both scenes' local object references, the six menu and pause button callbacks, and the three prefab script and sprite references. MainMenu and SampleScene are both enabled in build settings.

The batch Editor encountered Unity Licensing Client initialization errors. Play Mode, phone controls, timing as seen by a player, and level completion still require a live check.

Start from MainMenu. Run, jump, fire in both directions, then pause, resume, restart and return to the menu. In stage one, approach the Patroller from both sides and break sight behind a platform. Dodge a Shooter shot after the flash, jump over a charge and confirm each enemy waits between attacks. Check camera edges, pause during a warning, collect four coins and continue to the next stage. On a phone, test movement while jumping or firing and rotate the device.

## Phone playtests

Target from the assignment is completion in 2–4 tries. Record actual attempts, including failures.

| Tester | Device | Tries | Completed? | Observation |
| --- | --- | --- | --- | --- |
| 1 | | | | |
| 2 | | | | |

## Balance changes after playtesting

| Prefab and parameter | Old value | New value | Observation behind the change | Retest result |
| --- | --- | --- | --- | --- |
| | | | | |
| | | | | |

## Assignment 11 — 2026-11-19

- Asked for balance assets, three data-driven levels and persistent progress using the existing game. PlayerStats, EnemyStats, ItemValues and three LevelData assets now provide gameplay values. GameData in Resources connects them.
- MysticGame now builds all levels from those assets. Added a per-level score and time limits of 120, 150 and 180 seconds. Progress records completed and unlocked levels and resumes at the start of the selected level.
- SaveSystem uses JsonUtility and persistentDataPath/save.json, writes through save.tmp and validates loaded values. It saves after level completion, on settings changes, application pause and focus loss. Music and effects are stored in the same JSON file as required by the assignment.
- Play opens a level selector. Settings adjusts audio and opens a blocking confirmation before resetting progress and volumes. Existing enemy prefabs now reference individual stats assets, and enemy HP remains per instance.
- Gameplay scripts compiled successfully with Unity 6000.6.2f1 references. The Editor opened through Unity Hub after batch licensing failures. Computer Use was stopped with Escape before Play Mode verification completed. Phone restart, corrupt-file recovery, layout, level completion and reset still need live verification. Android Build Support is absent, so an APK has not been produced.


## Assignment 12 — Sound Pass — 2026-11-26

- Asked the agent for a full sound layer in the existing game. It replaced the runtime beeps in MysticAudio with an AudioMixer (Master, Music, SFX), WAV clips and a separate MusicManager. Both live on one AudioSystem object, which is created before the first scene and kept with DontDestroyOnLoad.
- The first plan put the pause volume drop on the Music group, which the slider also controls. Once a parameter is exposed and set from script, snapshots stop changing it, so the Paused snapshot would have done nothing. Music now goes to a child group, MusicTracks. The slider controls Music. Paused lowers MusicTracks by 10 dB and adds a 650 Hz lowpass.
- Snapshot transitions run on mixer time. With Time.timeScale = 0 they would never move, so the mixer update mode is Unscaled Time. The crossfade also uses unscaled time.
- AudioMixer.SetFloat is ignored in Awake, so saved volumes are applied in Start. The volume uses 20 × Log10(slider) with a floor of −80 dB. The old -/+ buttons now move the slider. The file is saved 0.4 s after the slider stops moving and when the panel closes.
- A fatal hit played the hit sound and then the lose sound. Now only lose plays. The LEFT/RIGHT/JUMP/FIRE hold buttons played a click on every release, so the click sound skips them. The boss had no warning, so it now plays the wind-up 0.5 s before each shot.
- Verified in a temporary Windows build with an automatic script, removed afterwards. Results: one MysticAudio and one MusicManager in every scene, a 1 s crossfade, the music continues after Restart, and volumes in dB are kept after closing and reopening the app. Not checked by ear yet: the pause filter, the mix levels and phone playback.