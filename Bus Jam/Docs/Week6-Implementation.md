# Week 6 implementation

Open `Assets/Scenes/MainScene.unity` and press Play. The game starts at Home. Settings is available from Home and the gameplay HUD. Win rewards are collected with Next; ten pooled stars credit 100 coins before the next level loads. Level progress, wallet balance and independent Sound/Music/Haptic flags are saved in PlayerPrefs.

## Architecture

- `Assets/Resources/BusJamUI.prefab`: authored UI hierarchy; no runtime construction of UI objects. Includes TextMeshPro labels, an EventSystem, HUDCanvas (10), PopupCanvas (20), VFXCanvas (30), Home, Result and Settings panels, and ten preallocated reward icons.
- `GameUI` binds input and coordinates views. `UIManager` opens/closes panels. `HUDController` renders counters. `SettingsController` handles toggles; `SettingsManager` owns persisted settings.
- `SafeAreaFitter` tracks safe area and viewport changes. `PortraitLayoutFitter` uniformly fits the 1080 x 1920 design inside that area. CanvasScaler uses Scale With Screen Size, 1080 x 1920, match 0.5.
- `PopupView` uses linked DOTween transitions with unscaled time, including while gameplay is paused. Raycasts stay blocked through closing transitions.
- `CoinFlyEffect` bursts ten preauthored icons, follows quadratic curves to the current HUD target, returns each icon to its pool and invokes individual wallet updates. Claim input is guarded during closing and flight animations.
- `GameAudio` separates SFX and looping BGM sources and applies settings immediately. Button, gameplay, outcome and coin sounds are synthesized original audio. UI clicks and gameplay interactions emit pooled particles.
- `HapticFeedback` checks the saved flag and rate-limits pulses. Android uses vibration duration/amplitude where supported, with a manifest VIBRATE permission. iOS uses Handheld.Vibrate. WebGL/Editor do not vibrate.

## Editor commands

- Bus Jam > Tools > Generate UI Prefab: regenerate the prefab from the editor authoring script. This overwrites manual edits to that prefab.
- Bus Jam > Tools > Validate Project: validate references, canvas layers, TMP fonts, safe areas, icon pool, orientation, level solvability, settings and aspect-fit calculations.
- Bus Jam > Build WebGL: build to `Builds/WebGL`, using the supplied mobile portrait HTML template. The saved UI prefab is preserved; generation only runs if it is missing.
- Bus Jam > Build Android APK: build to `Builds/Android/BusJam.apk`. Requires Android Build Support, SDK, NDK and OpenJDK for Unity 2022.3.62f2.

Batch play-mode smoke test (run in an isolated project copy, without `-quit`): `-executeMethod GameplaySmokeTests.Begin`. It exits automatically with status 0/1 and restores the PlayerPrefs keys it changes. Reports are written under `Logs/Week6-*.txt`.

## Verification and remaining delivery work

Verified in Unity 2022.3.62f2: editor compilation; serialized prefab references; five valid solvable levels; fit calculations at 16:9, 19.5:9 and 20:9 with safe insets; persisted settings; real play-mode Home/Settings/pause/restart/cancel flow; animated level-one solution; win popup; double-click Claim protection; ten reward arrivals totaling exactly 100 coins; advancement to level two. The play-mode smoke test reported no runtime Console errors.

Physical-device haptic strength, real notch/Dynamic Island insets and mobile-browser performance still require device testing. The WebGL build succeeded (about 32 MB); output is in `Builds/WebGL`. No connected browser was available for a visual WebGL smoke test. Android Build Support is absent on this machine, so no APK was produced. Publishing a public WebGL URL requires a hosting destination.

## Directory layout

Game scripts live directly in `Assets/Scripts/{Audio,Data,Enums,Gameplay,Pooling,Settings,UI}`. ScriptableObject assets live in `Assets/Data`, levels in `Assets/Data/Levels`, materials in `Assets/Materials`, shaders in `Assets/Shaders`, and gameplay prefabs in `Assets/Prefabs`. The `BusJam` namespace remains to keep C# types grouped; it does not require a matching folder layer.

Dialogs use short functional labels. Home contains the title, Play and Settings. Settings contains Sound, Music, Vibration and Done. Results show the outcome, reward or failure reason, and the next action. Restart confirmation uses Restart / Cancel.
