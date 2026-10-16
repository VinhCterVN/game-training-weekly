using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TuberLevels.Editor
{
    public static class Lab2Builder
    {
        private static Font _font;
        private static Sprite _round;
        private static readonly Color _ink = new Color(.08f, .23f, .29f);
        private static readonly Color _teal = new Color(.04f, .45f, .46f);
        private static readonly Color _muted = new Color(.27f, .43f, .46f);

        [MenuItem("Tube Levels/Build or Repair Game Scene")]
        public static void BuildScene()
        {
            Directory.CreateDirectory("Assets/ScriptableObjects/Levels");
            Directory.CreateDirectory("Assets/Prefabs");
            Directory.CreateDirectory("Assets/Art");
            Directory.CreateDirectory("Assets/Audio");
            GenerateRound();
            GenerateAudio();
            AssetDatabase.Refresh();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Scenes", "Assets/Art" }))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                if (importer.assetPath.EndsWith("rounded.png")) importer.spriteBorder = new Vector4(20, 20, 20, 20);
                importer.SaveAndReimport();
            }

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _round = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/rounded.png");
            var database =
                AssetDatabase.LoadAssetAtPath<LevelDatabaseSO>("Assets/ScriptableObjects/LevelDatabase.asset");
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<LevelDatabaseSO>();
                AssetDatabase.CreateAsset(database, "Assets/ScriptableObjects/LevelDatabase.asset");
            }

            string[] titles =
                { "First drops", "Find your flow", "A little more color", "Color in motion", "The final blend" };
            database.levels.Clear();
            for (int i = 0; i < 5; i++)
            {
                string path = "Assets/ScriptableObjects/Levels/Level_" + (i + 1).ToString("00") + ".asset";
                var data = AssetDatabase.LoadAssetAtPath<LevelDataSO>(path);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<LevelDataSO>();
                    data.levelIndex = i + 1;
                    data.colorCount = i + 3;
                    data.emptyBottles = i < 2 ? 1 : 2;
                    data.seed = 101 + i * 73;
                    data.displayName = titles[i];
                    LevelSolver.Generate(data);
                    AssetDatabase.CreateAsset(data, path);
                }

                database.levels.Add(data);
            }

            EditorUtility.SetDirty(database);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.78f, .94f, .89f);
            camera.transform.position = Vector3.back * 10;
            var canvasObject = new GameObject("Game Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var canvas = (RectTransform)canvasObject.transform;
            var background = Picture("Background", canvas, Art("bg1"), Vector2.zero, new Vector2(720, 1280));
            Stretch(background.rectTransform);
            var safe = Rect("Safe Area", canvas, Vector2.zero, Vector2.zero);
            Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();
            var frame = Rect("Portrait Layout", safe, Vector2.zero, new Vector2(720, 1280));
            // Scale the complete layout inside the safe area without cropping on tablets or landscape windows.
            frame.gameObject.AddComponent<PortraitLayout>();
            Label("Eyebrow", frame, "T U B E   L E V E L S", new Vector2(0, 558), new Vector2(500, 32), 21, _teal,
                FontStyle.Bold);
            var levelText = Label("Level title", frame, "LEVEL 01", new Vector2(0, 494), new Vector2(500, 70), 57, _ink,
                FontStyle.Bold);
            var subtitle = Label("Level subtitle", frame, "First drops", new Vector2(0, 439), new Vector2(500, 38), 25,
                _muted);
            var dots = new Image[5];
            for (int i = 0; i < 5; i++)
                dots[i] = Panel("Progress " + i, frame, new Vector2((i - 2) * 26, 390), new Vector2(12, 12), _teal);
            var movesCard = Panel("Move counter", frame, new Vector2(0, 329), new Vector2(186, 48),
                new Color(1, 1, 1, .65f));
            var moves = Label("Moves", movesCard.transform, "00  MOVES", Vector2.zero, new Vector2(170, 40), 20, _muted,
                FontStyle.Bold);
            var board = Rect("Bottle Board", frame, new Vector2(0, -35), new Vector2(680, 670));
            var prefab = CreateBottle();
            var stream = Panel("Pour Stream", board, Vector2.zero, new Vector2(9, 65), Color.cyan);
            stream.gameObject.SetActive(false);
            var effectsRoot = Rect("Completion Particles", frame, Vector2.zero, new Vector2(720, 1280));
            var effects = effectsRoot.gameObject.AddComponent<CompletionParticles>();
            var pieces = new Image[24];
            for (int i = 0; i < pieces.Length; i++)
            {
                pieces[i] = Panel("Particle " + i, effectsRoot, Vector2.zero, new Vector2(7, 13), Color.white);
                pieces[i].gameObject.SetActive(false);
            }

            effects.Configure(pieces);
            var message = Label("Instructions", frame, "Tap a tube, then tap where you want to pour.",
                new Vector2(0, -405), new Vector2(610, 72), 24, _muted);
            var toolbar = Panel("Toolbar", frame, new Vector2(0, -520), new Vector2(540, 130),
                new Color(1, 1, 1, .55f));
            var home = IconButton("Levels", toolbar.transform, Art("buttons/home_normal"), new Vector2(-165, 12),
                "LEVELS");
            var undo = IconButton("Undo", toolbar.transform, Art("icon-tb-undo"), new Vector2(0, 12), "UNDO");
            var restart = IconButton("Restart", toolbar.transform, Art("buttons/reset_normal"), new Vector2(165, 12),
                "RESTART");
            SetButtonSprites(home, Art("buttons/home_pressed"), Art("buttons/home_disabled"));
            SetButtonSprites(restart, Art("buttons/reset_pressed"), Art("buttons/reset_disabled"));
            var sound = TextButton("Sound", frame, "SOUND ON", new Vector2(0, -608), new Vector2(180, 36),
                new Color(1, 1, 1, 0), _muted, 17);
            var win = CreateWin(frame);
            var levelPanel = CreateLevelPanel(frame, out Button close, out Button[] levelButtons, out Text[] labels,
                out Text[] states);
            var systems = new GameObject("Game Systems");
            var pool = systems.AddComponent<ObjectPooler>();
            pool.Configure(prefab, board);
            var audio = systems.AddComponent<AudioManager>();
            var source = systems.AddComponent<AudioSource>();
            source.playOnAwake = false;
            var clips = new AudioClip[5];
            string[] names = { "Click", "Pour", "Complete", "Win", "Error" };
            for (int i = 0; i < names.Length; i++)
                clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + names[i] + ".wav");
            audio.Configure(source, clips);
            var levels = systems.AddComponent<LevelManager>();
            levels.Configure(database, pool);
            var sort = systems.AddComponent<WaterSortController>();
            sort.Configure(stream, effects);
            var ui = systems.AddComponent<UIManager>();
            ui.Configure(levelText, subtitle, moves, message, undo, restart, home, sound,
                sound.GetComponentInChildren<Text>(), win,
                levelPanel, close, levelButtons, labels, states, dots);
            systems.AddComponent<GameManager>().Configure(levels, sort, ui, audio);
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            win.gameObject.SetActive(false);
            levelPanel.SetActive(false);
            PlayerSettings.productName = "Tube Levels";
            PlayerSettings.companyName = "Lab2";
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainScene.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/MainScene.unity", true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Tube Levels: scene, prefab, five levels and five audio clips ready.");
        }

        private static BottleController CreateBottle()
        {
            var root = Rect("Bottle", null, Vector2.zero, new Vector2(120, 280));
            var hit = root.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            var selected = Picture("Selection", root, Art("tubes/tube_selected"), Vector2.zero, new Vector2(124, 267));
            selected.enabled = false;
            var glass = Picture("Glass", root, Art("tubes/tube_clear"), Vector2.zero, new Vector2(110, 250));
            var layers = new Image[4];
            for (int i = 0; i < 4; i++)
                layers[i] = Picture("Water layer " + i, root, null, new Vector2(0, -82 + i * 47),
                    new Vector2(77, i == 0 ? 53 : 59));
            var shine = Picture("Glass highlights", root, Art("tubes/tube_clear"), Vector2.zero, new Vector2(110, 250));
            shine.color = new Color(1, 1, 1, .18f);
            var mouth = Rect("MouthPoint", root, new Vector2(0, 107), Vector2.zero);
            var number = Label("Tube number", root, "01", new Vector2(0, -153), new Vector2(70, 30), 20, _muted,
                FontStyle.Bold);
            var water = new Sprite[8];
            var bottom = new Sprite[8];
            for (int i = 0; i < 8; i++)
            {
                string color = ((WaterColor)i).ToString().ToLowerInvariant();
                water[i] = Art("water_balls/water_ball_" + color);
                bottom[i] = Art("water_balls/water_ball_bottom_" + color);
            }

            var controller = root.gameObject.AddComponent<BottleController>();
            controller.Configure(mouth, glass, selected, layers, water, bottom, number);
            var asset = PrefabUtility.SaveAsPrefabAsset(root.gameObject, "Assets/Prefabs/Bottle.prefab");
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            return asset.GetComponent<BottleController>();
        }

        private static WinPopupUI CreateWin(Transform parent)
        {
            var shade = Panel("Win Popup", parent, Vector2.zero, new Vector2(720, 1280),
                new Color(.03f, .12f, .18f, .7f));
            shade.raycastTarget = true;
            var card = Panel("Card", shade.transform, Vector2.zero, new Vector2(590, 530), new Color(.97f, 1, .96f));
            Picture("Completed ribbon", card.transform, Art("win_panel"), new Vector2(0, 192), new Vector2(530, 114));
            var title = Label("Title", card.transform, "You Win !!!", new Vector2(0, 78), new Vector2(550, 62),
                39, _ink, FontStyle.Bold);
            var next = TextButton("Next", card.transform, "NEXT LEVEL", new Vector2(0, -104), new Vector2(440, 76),
                _teal, Color.white, 25);
            var replay = TextButton("Replay", card.transform, "PLAY AGAIN", new Vector2(0, -196), new Vector2(440, 64),
                new Color(.84f, .94f, .89f), _teal, 22);
            var popup = shade.gameObject.AddComponent<WinPopupUI>();
            popup.Configure(card.rectTransform, title, next.GetComponentInChildren<Text>(), next, replay);
            return popup;
        }

        private static GameObject CreateLevelPanel(Transform parent, out Button close, out Button[] buttons,
            out Text[] labels, out Text[] states)
        {
            var shade = Panel("Level Selection", parent, Vector2.zero, new Vector2(720, 1280),
                new Color(.03f, .12f, .18f, .7f));
            shade.raycastTarget = true;
            var card = Panel("Card", shade.transform, Vector2.zero, new Vector2(640, 800), new Color(.96f, 1, .96f));
            Label("Title", card.transform, "Find your flow", new Vector2(0, 306), new Vector2(570, 70), 46, _ink,
                FontStyle.Bold);
            Label("Description", card.transform, "Five puzzles. One color at a time.", new Vector2(0, 245),
                new Vector2(590, 40), 24, _muted);
            buttons = new Button[5];
            labels = new Text[5];
            states = new Text[5];
            for (int i = 0; i < 5; i++)
            {
                float x = i < 3 ? (i - 1) * 184 : (i - 3.5f) * 184;
                float y = i < 3 ? 99 : -114;
                var badge = Picture("Level " + (i + 1), card.transform, Art("badges/badge_blank"), new Vector2(x, y),
                    new Vector2(132, 150));
                badge.raycastTarget = true;
                buttons[i] = badge.gameObject.AddComponent<Button>();
                var colors = buttons[i].colors;
                colors.disabledColor = new Color(.45f, .55f, .6f, .7f);
                buttons[i].colors = colors;
                labels[i] = Label("Number", badge.transform, (i + 1).ToString("00"), Vector2.zero, new Vector2(120, 70),
                    43, Color.white, FontStyle.Bold);
                states[i] = Label("State", badge.transform, "PLAY", new Vector2(0, -99), new Vector2(172, 40), 18,
                    _teal, FontStyle.Bold);
            }

            close = TextButton("Back", card.transform, "BACK TO GAME", new Vector2(0, -316), new Vector2(440, 68),
                _teal, Color.white, 24);
            return shade.gameObject;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Sprite Art(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Scenes/" + path + ".png");
        }

        private static Image Picture(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size)
        {
            var image = Rect(name, parent, position, size).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static Image Panel(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var image = Picture(name, parent, _round, position, size);
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }

        private static Text Label(string name, Transform parent, string value, Vector2 position, Vector2 size,
            int fontSize, Color color, FontStyle style = FontStyle.Normal)
        {
            var text = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
            text.font = _font;
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = style;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static Button TextButton(string name, Transform parent, string value, Vector2 position, Vector2 size,
            Color background, Color foreground, int fontSize)
        {
            var image = Panel(name, parent, position, size, background);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Label("Label", image.transform, value, Vector2.zero, size - new Vector2(16, 4), fontSize, foreground,
                FontStyle.Bold);
            return button;
        }

        private static Button IconButton(string name, Transform parent, Sprite sprite, Vector2 position, string label)
        {
            var image = Picture(name, parent, sprite, position, new Vector2(76, 76));
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Label("Label", image.transform, label, new Vector2(0, -53), new Vector2(140, 28), 17, _muted,
                FontStyle.Bold);
            return button;
        }

        private static void SetButtonSprites(Button button, Sprite pressed, Sprite disabled)
        {
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { pressedSprite = pressed, disabledSprite = disabled };
        }

        private static void GenerateRound()
        {
            if (File.Exists("Assets/Art/rounded.png")) return;
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float dx = Mathf.Max(18 - x, x - 45, 0);
                float dy = Mathf.Max(18 - y, y - 45, 0);
                texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(18.5f - Mathf.Sqrt(dx * dx + dy * dy))));
            }

            texture.Apply();
            File.WriteAllBytes("Assets/Art/rounded.png", texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void GenerateAudio()
        {
            string[] names = { "Click", "Pour", "Complete", "Win", "Error" };
            float[] durations = { .09f, .65f, .4f, .85f, .2f };
            for (int kind = 0; kind < names.Length; kind++)
            {
                string path = "Assets/Audio/" + names[kind] + ".wav";
                if (File.Exists(path)) continue;
                const int rate = 22050;
                int count = (int)(rate * durations[kind]);
                var random = new System.Random(42);
                using (var writer = new BinaryWriter(File.Create(path)))
                {
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                    writer.Write(36 + count * 2);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                    writer.Write(16);
                    writer.Write((short)1);
                    writer.Write((short)1);
                    writer.Write(rate);
                    writer.Write(rate * 2);
                    writer.Write((short)2);
                    writer.Write((short)16);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                    writer.Write(count * 2);
                    for (int i = 0; i < count; i++)
                    {
                        double time = (double)i / rate, progress = (double)i / count;
                        double frequency = kind == 0 ? 920 :
                            kind == 1 ? 320 + 100 * Math.Sin(time * 35) :
                            kind == 4 ? 180 - 60 * progress : 523.25 * Math.Pow(2, Math.Floor(progress * 4) * 4 / 12);
                        double sample = Math.Sin(2 * Math.PI * frequency * time);
                        if (kind == 1) sample = sample * .35 + (random.NextDouble() * 2 - 1) * .3;
                        double envelope = Math.Min(1, time * 90) * Math.Pow(1 - progress, 1.4);
                        writer.Write((short)(sample * envelope * 14000));
                    }
                }
            }
        }

        public static void BuildAndVerify()
        {
            BuildScene();
            Lab2Verification.Run();
            string output = Environment.GetEnvironmentVariable("TUBE_BUILD_OUTPUT");
            if (string.IsNullOrEmpty(output)) output = "Builds/Windows/TubeLevels.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/MainScene.unity" }, locationPathName = output,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Build failed: " + report.summary.result);
            Debug.Log("LAB2 BUILD AND VERIFICATION PASSED");
        }
    }
}