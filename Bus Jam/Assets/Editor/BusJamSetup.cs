using System;
using System.Collections.Generic;
using System.IO;
using BusJam;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BusJamSetup
{
    private const string Root = "Assets/BusJam";
    [MenuItem("Bus Jam/Setup Project")]
    public static void Setup()
    {
        Directory.CreateDirectory(Root + "/Levels"); Directory.CreateDirectory(Root + "/Prefabs");
        AssetDatabase.Refresh();
        var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(Root + "/GameCatalog.asset");
        if (catalog == null) { catalog = ScriptableObject.CreateInstance<GameCatalog>(); AssetDatabase.CreateAsset(catalog, Root + "/GameCatalog.asset"); }
        catalog.background = Sprite("Assets/Sprites/UI/background.png");
        catalog.passenger = Sprite("Assets/Sprites/Passengers/passenger_white.png");
        catalog.slot = Sprite("Assets/Sprites/Park/park_slot_unlock.png");
        catalog.exhaust = Sprite("Assets/Sprites/Vfx/SoftCircle.png");
        catalog.star = Sprite("Assets/Sprites/UI/star_full.png");
        catalog.winRays = Sprite("Assets/Sprites/UI/win_rays.png");
        catalog.button = Sprite("Assets/Sprites/UI/btn_replay.png");
        catalog.primaryButton = Sprite("Assets/Sprites/UI/btn_next.png");
        catalog.dialogFrame = Sprite("Assets/Sprites/UI/win_frame.png");
        catalog.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var arts = new List<VehicleArt>();
        foreach (VehicleKind kind in Enum.GetValues(typeof(VehicleKind)))
            foreach (ColorID color in Enum.GetValues(typeof(ColorID)))
            {
                var art = new VehicleArt { kind = kind, color = color };
                string type = kind == VehicleKind.Car ? "car" : "bus";
                foreach (Facing direction in Enum.GetValues(typeof(Facing)))
                {
                    string suffix = (color == ColorID.Green ? ColorID.Blue : color).ToString().ToLowerInvariant() + "_" + direction.ToString().ToLowerInvariant() + ".png";
                    art.closed[(int)direction] = Sprite($"Assets/Sprites/Vehicles/{type}_arrow/{type}_arrow_{suffix}");
                    art.seats[(int)direction] = Sprite($"Assets/Sprites/Vehicles/{type}_seat/{type}_seat_{suffix}");
                }
                arts.Add(art);
            }
        catalog.vehicles = arts.ToArray();
        catalog.greenVehicleMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/GreenVehicle.mat");
        if (catalog.greenVehicleMaterial == null)
        {
            catalog.greenVehicleMaterial = new Material(Shader.Find("BusJam/GreenVehicle"));
            AssetDatabase.CreateAsset(catalog.greenVehicleMaterial, Root + "/GreenVehicle.mat");
        }
        catalog.levels = new LevelDataSO[5];
        string[] names = { "First departures", "Make some room", "Color connection", "Rush hour", "The grand jam" };
        for (int i = 0; i < 5; i++)
        {
            string path = $"{Root}/Levels/Level{i + 1:00}.asset";
            var level = AssetDatabase.LoadAssetAtPath<LevelDataSO>(path);
            if (level == null)
            {
                level = ScriptableObject.CreateInstance<LevelDataSO>(); level.title = names[i];
                level.columns = 5 + i; level.rows = 5 + i; level.bayCount = i < 2 ? 2 : 3;
                level.hint = i == 0 ? "Tap a car with a clear arrow. Match the first passenger's color." : "Plan your bays. Only the first passenger can board.";
                GenerateLayout(level, new[] { 4, 8, 12, 18, 24 }[i], 193 + i * 57);
                if (i == 0) SetTutorialLayout(level);
                GenerateQueue(level, i == 0 ? 1 : i < 3 ? 2 : 3);
                AssetDatabase.CreateAsset(level, path);
            }
            catalog.levels[i] = level;
        }
        MakePrefabs(catalog);
        EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
        ConfigurePlayer();
        // Preserve the original hand-authored scene; the new main scene replaces its missing-script setup.
        if (!File.Exists("Docs/MainScene.before-busjam.unity.txt"))
        {
            Directory.CreateDirectory("Docs");
            if (File.Exists("Assets/Scenes/MainScene.unity")) File.Copy("Assets/Scenes/MainScene.unity", "Docs/MainScene.before-busjam.unity.txt");
        }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 9.6f;
        camera.transform.position = new Vector3(0, 0, -10); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f, .18f, .27f);
        var game = new GameObject("Bus Jam").AddComponent<BusJamGame>(); game.catalog = catalog; game.gameCamera = camera;
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainScene.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/MainScene.unity", true) };
        AssetDatabase.SaveAssets(); Debug.Log("BUSJAM_SETUP_OK: scene, five levels, catalog and pooled prefabs saved.");
    }

    private static Sprite Sprite(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new Exception("Missing sprite: " + path);
        return sprite;
    }

    public static void GenerateLayout(LevelDataSO level, int count, int seed)
    {
        for (int attempt = 0; attempt < 64; attempt++)
        {
            var random = new System.Random(seed + attempt * 13); level.vehicles.Clear();
            for (int tries = 0; tries < 5000 && level.vehicles.Count < count; tries++)
            {
                var v = new VehiclePlacement { kind = random.Next(3) == 0 ? VehicleKind.Bus : VehicleKind.Car, color = (ColorID)random.Next(level.columns >= 8 ? 4 : 3), facing = (Facing)random.Next(4), x = random.Next(level.columns), y = random.Next(level.rows) };
                if (v.x + v.Width > level.columns || v.y + v.Height > level.rows) continue;
                bool overlap = false;
                foreach (var old in level.vehicles)
                    if (v.x < old.x + old.Width && v.x + v.Width > old.x && v.y < old.y + old.Height && v.y + v.Height > old.y) overlap = true;
                if (overlap) continue;
                level.vehicles.Add(v);
                if (!new RoundState(level).CanEscape(level.vehicles.Count - 1)) level.vehicles.RemoveAt(level.vehicles.Count - 1);
            }
            if (level.vehicles.Count == count) return;
        }
        throw new Exception($"Could not place {count} vehicles on {level.columns}x{level.rows}.");
    }

    public static void SetTutorialLayout(LevelDataSO level)
    {
        level.vehicles = new List<VehiclePlacement> {
            new VehiclePlacement { kind = VehicleKind.Car, color = ColorID.Blue, facing = Facing.Up, x = 0, y = 0 },
            new VehiclePlacement { kind = VehicleKind.Bus, color = ColorID.Red, facing = Facing.Right, x = 0, y = 3 },
            new VehiclePlacement { kind = VehicleKind.Car, color = ColorID.Yellow, facing = Facing.Left, x = 3, y = 0 },
            new VehiclePlacement { kind = VehicleKind.Bus, color = ColorID.Blue, facing = Facing.Up, x = 4, y = 1 }
        };
        level.hint = "The blue bus can leave first. Follow its arrow to clear a path.";
    }

    public static bool GenerateQueue(LevelDataSO level, int groupSize)
    {
        if (level.Validate(false).Count > 0 || groupSize < 1 || groupSize > level.bayCount) return false;
        var state = new RoundState(level); var order = new List<int>();
        while (order.Count < level.vehicles.Count)
        {
            bool found = false;
            for (int i = 0; i < level.vehicles.Count; i++) if (state.CanEscape(i))
            {
                state.Phases[i] = VehiclePhase.Delivered; order.Add(i); found = true; break;
            }
            if (!found) return false;
        }
        level.passengers.Clear();
        for (int start = 0; start < order.Count; start += groupSize)
        {
            int end = Mathf.Min(start + groupSize, order.Count);
            for (int i = start; i < end; i++) level.passengers.Add(level.vehicles[order[i]].color);
            for (int i = start; i < end; i++)
            {
                var v = level.vehicles[order[i]];
                for (int p = 1; p < v.Capacity; p++) level.passengers.Add(v.color);
            }
        }
        return true;
    }

    private static void MakePrefabs(GameCatalog catalog)
    {
        var root = new GameObject("Vehicle"); var bus = root.AddComponent<BusController>();
        bus.hitbox = root.AddComponent<BoxCollider2D>();
        bus.body = new GameObject("Body").AddComponent<SpriteRenderer>(); bus.body.transform.SetParent(root.transform); bus.body.sortingOrder = 5;
        bus.head = new GameObject("Head").transform; bus.head.SetParent(root.transform);
        bus.tail = new GameObject("Exhaust point").transform; bus.tail.SetParent(root.transform);
        bus.seats = new Transform[6];
        for (int i = 0; i < 6; i++) { bus.seats[i] = new GameObject("Seat " + (i + 1)).transform; bus.seats[i].SetParent(root.transform); }
        bus.count = new GameObject("Capacity").AddComponent<TextMesh>(); bus.count.transform.SetParent(root.transform);
        bus.count.font = catalog.font; bus.count.fontSize = 48; bus.count.characterSize = .065f; bus.count.anchor = TextAnchor.MiddleCenter; bus.count.color = new Color(.1f, .18f, .29f);
        var textRenderer = bus.count.GetComponent<MeshRenderer>(); textRenderer.sharedMaterial = catalog.font.material; textRenderer.sortingOrder = 70;
        catalog.busPrefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/Vehicle.prefab").GetComponent<BusController>(); UnityEngine.Object.DestroyImmediate(root);

        root = new GameObject("Passenger"); var passenger = root.AddComponent<PassengerController>();
        passenger.body = new GameObject("Body").AddComponent<SpriteRenderer>(); passenger.body.transform.SetParent(root.transform);
        passenger.body.sprite = catalog.passenger; passenger.body.sortingOrder = 25;
        var bounds = BusController.VisibleBounds(catalog.passenger);
        passenger.body.transform.localScale = Vector3.one * (.54f / bounds.size.y);
        passenger.body.transform.localPosition = -bounds.center * (.54f / bounds.size.y);
        catalog.passengerPrefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/Passenger.prefab").GetComponent<PassengerController>(); UnityEngine.Object.DestroyImmediate(root);

        root = new GameObject("Exhaust and celebration"); var ps = root.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = false; main.playOnAwake = false; main.duration = 3; main.maxParticles = 80; main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission; emission.enabled = false;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = .08f;
        var color = ps.colorOverLifetime; color.enabled = true;
        var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) }); color.color = gradient;
        var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, .5f, 1, 1.5f));
        var renderer = ps.GetComponent<ParticleSystemRenderer>(); renderer.sortingOrder = 90;
        string matPath = Root + "/Exhaust.mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (material == null) { material = new Material(Shader.Find("Particles/Standard Unlit")); AssetDatabase.CreateAsset(material, matPath); }
        material.mainTexture = catalog.exhaust.texture;
        material.SetFloat("_Mode", 2); material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); material.SetInt("_ZWrite", 0); material.EnableKeyword("_ALPHABLEND_ON"); material.renderQueue = 3000;
        renderer.sharedMaterial = material;
        catalog.particlePrefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/Exhaust.prefab").GetComponent<ParticleSystem>(); UnityEngine.Object.DestroyImmediate(root);
    }

    private static void ConfigurePlayer()
    {
        PlayerSettings.companyName = "Bus Jam Studio"; PlayerSettings.productName = "Bus Jam";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false; PlayerSettings.allowedAutorotateToLandscapeRight = false; PlayerSettings.allowedAutorotateToPortraitUpsideDown = false; PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.defaultScreenWidth = 1080; PlayerSettings.defaultScreenHeight = 1920;
        PlayerSettings.defaultWebScreenWidth = 1080; PlayerSettings.defaultWebScreenHeight = 1920;
        PlayerSettings.runInBackground = true; PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.template = "PROJECT:BusJam";
    }

    [MenuItem("Bus Jam/Open Game")]
    public static void OpenGame()
    {
        if (!EditorApplication.isPlaying && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
    }

    [MenuItem("Bus Jam/Build WebGL")]
    public static void BuildWebGL()
    {
        ConfigurePlayer();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { "Assets/Scenes/MainScene.unity" }, locationPathName = "Builds/WebGL", target = BuildTarget.WebGL, options = BuildOptions.None });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("WebGL build failed: " + report.summary.result);
        Debug.Log("BUSJAM_BUILD_OK: " + report.summary.totalSize + " bytes");
    }
}
