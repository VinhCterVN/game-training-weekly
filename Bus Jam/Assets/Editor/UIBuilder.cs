using System;
using System.IO;
using BusJam;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Authoring happens in the Editor; runtime only instantiates this saved prefab.
public static class UIBuilder
{
    private const string _prefabPath = "Assets/Resources/BusJamUI.prefab";
    private static TMP_FontAsset _font;
    private static readonly Color _navy = new Color(.07f, .12f, .22f);
    private static readonly Color _blue = new Color(.16f, .45f, .86f);
    private static readonly Color _gold = new Color(1, .77f, .22f);

    [InitializeOnLoadMethod]
    private static void EnsurePrefab()
    {
        if (!File.Exists(_prefabPath)) EditorApplication.delayCall += Generate;
    }

    [MenuItem("Bus Jam/Tools/Generate UI Prefab")]
    public static void Generate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        const string fontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        if (_font == null) throw new InvalidOperationException("TMP essential font could not be imported.");
        Directory.CreateDirectory("Assets/Resources");
        AssetDatabase.Refresh();
        var root = new GameObject("Bus Jam UI", typeof(RectTransform));
        try
        {
            var ui = root.AddComponent<GameUI>();
            var manager = root.AddComponent<UIManager>();
            var hudCanvas = Canvas("HUDCanvas", root.transform, 10);
            var popupCanvas = Canvas("PopupCanvas", root.transform, 20);
            var vfxCanvas = Canvas("VFXCanvas", root.transform, 30);
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetParent(root.transform, false);
            var hudSafe = Safe(hudCanvas);
            var hudRoot = Rect("Portrait HUD", hudSafe, Vector2.zero, new Vector2(1080, 1920));
            hudRoot.gameObject.AddComponent<PortraitLayoutFitter>();
            var hud = hudRoot.gameObject.AddComponent<HUDController>();
            var header = Panel("Header", hudRoot, new Vector2(0, -100), new Vector2(1010, 130), _navy, new Vector2(.5f, 1));
            var level = Label("Level", header, "LEVEL 1", new Vector2(-290, 0), new Vector2(330, 70), 40);
            var coinIcon = Coin("Wallet star", header, new Vector2(120, 0), 66);
            var coins = Label("Coins", header, "0", new Vector2(290, 0), new Vector2(250, 80), 40, _gold);
            var waiting = Label("Waiting", hudRoot, "0\nWAITING", new Vector2(-350, -370), new Vector2(220, 120), 32, Color.white, new Vector2(.5f, 1));
            var settings = Button("Settings", hudRoot, "SETTINGS", new Vector2(-320, 105), new Vector2(290, 90), _navy, new Vector2(.5f, 0));
            var restart = Button("Restart", hudRoot, "RESTART", new Vector2(0, 105), new Vector2(290, 90), _navy, new Vector2(.5f, 0));
            var homeButton = Button("Home", hudRoot, "HOME", new Vector2(320, 105), new Vector2(290, 90), _navy, new Vector2(.5f, 0));
            Set(hud, "_level", level, "_waiting", waiting, "_coins", coins, "_coinIcon", coinIcon);

            var home = Popup("Home", popupCanvas, new Vector2(900, 700), out var homeCard);
            Label("Title", homeCard, "BUS JAM", new Vector2(0, 180), new Vector2(800, 140), 84);
            var play = Button("Play", homeCard, "PLAY", new Vector2(0, -10), new Vector2(700, 120), _blue);
            var homeSettings = Button("Settings", homeCard, "SETTINGS", new Vector2(0, -170), new Vector2(700, 100), new Color(.18f, .25f, .36f));

            var result = Popup("Result", popupCanvas, new Vector2(900, 680), out var resultCard);
            var title = Label("Title", resultCard, "LEVEL COMPLETE", new Vector2(0, 190), new Vector2(820, 100), 52);
            var body = Label("Description", resultCard, "+100", new Vector2(0, 80), new Vector2(780, 100), 32, _gold);
            var primary = Button("Primary", resultCard, "NEXT", new Vector2(0, -55), new Vector2(740, 110), _blue);
            var secondary = Button("Secondary", resultCard, "CANCEL", new Vector2(0, -200), new Vector2(740, 95), new Color(.18f, .25f, .36f));

            var settingsPopup = Popup("Settings", popupCanvas, new Vector2(900, 850), out var settingsCard);
            Label("Title", settingsCard, "SETTINGS", new Vector2(0, 300), new Vector2(820, 100), 48);
            var sound = Toggle("Sound", settingsCard, "SOUND", 130);
            var music = Toggle("Music", settingsCard, "MUSIC", -10);
            var haptic = Toggle("Haptic", settingsCard, "VIBRATION", -150);
            var close = Button("Close", settingsCard, "DONE", new Vector2(0, -325), new Vector2(740, 100), _blue);
            var settingsController = settingsCard.gameObject.AddComponent<SettingsController>();
            Set(settingsController, "_sound", sound, "_music", music, "_haptic", haptic);
            Set(manager, "_home", home, "_result", result, "_settings", settingsPopup);

            var effect = vfxCanvas.gameObject.AddComponent<CoinFlyEffect>();
            var pool = new RectTransform[10];
            for (int i = 0; i < pool.Length; i++)
            {
                pool[i] = Coin("Pooled reward " + i, vfxCanvas, Vector2.zero, 76);
                pool[i].gameObject.SetActive(false);
            }
            var serializedEffect = new SerializedObject(effect);
            var icons = serializedEffect.FindProperty("_icons");
            icons.arraySize = pool.Length;
            for (int i = 0; i < pool.Length; i++) icons.GetArrayElementAtIndex(i).objectReferenceValue = pool[i];
            serializedEffect.ApplyModifiedPropertiesWithoutUndo();
            Set(ui, "_manager", manager, "_hud", hud, "_coinEffect", effect,
                "_title", title, "_body", body, "_primaryLabel", primary.GetComponentInChildren<TMP_Text>(),
                "_secondaryLabel", secondary.GetComponentInChildren<TMP_Text>(), "_play", play,
                "_homeSettings", homeSettings, "_settings", settings, "_restart", restart, "_home", homeButton,
                "_primary", primary, "_secondary", secondary, "_closeSettings", close);
            PrefabUtility.SaveAsPrefabAsset(root, _prefabPath);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            AssetDatabase.SaveAssets();
            Debug.Log("BUSJAM_WEEK6_UI_OK: prefab, TMP, safe areas, layered canvases and ten pooled icons saved.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size, Vector2? anchor = null)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor ?? new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private static RectTransform Canvas(string name, Transform parent, int order)
    {
        var rect = Rect(name, parent, Vector2.zero, Vector2.zero);
        var canvas = rect.gameObject.AddComponent<UnityEngine.Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        var scaler = rect.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = .5f;
        rect.gameObject.AddComponent<GraphicRaycaster>();
        return rect;
    }
    private static RectTransform Safe(Transform parent)
    {
        var safe = Rect("Safe Area", parent, Vector2.zero, Vector2.zero);
        Stretch(safe);
        safe.gameObject.AddComponent<SafeAreaFitter>();
        return safe;
    }
    private static RectTransform Panel(string name, Transform parent, Vector2 position, Vector2 size, Color color, Vector2? anchor = null)
    {
        var rect = Rect(name, parent, position, size, anchor);
        rect.gameObject.AddComponent<Image>().color = color;
        return rect;
    }
    private static TMP_Text Label(string name, Transform parent, string value, Vector2 position, Vector2 size, int fontSize, Color? color = null, Vector2? anchor = null)
    {
        var rect = Rect(name, parent, position, size, anchor);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = _font;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color ?? Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.raycastTarget = false;
        return text;
    }
    private static Button Button(string name, Transform parent, string label, Vector2 position, Vector2 size, Color color, Vector2? anchor = null)
    {
        var rect = Panel(name, parent, position, size, color, anchor);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        var text = Label("Label", rect, label, Vector2.zero, size - new Vector2(30, 12), 32);
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(15, 6);
        text.rectTransform.offsetMax = new Vector2(-15, -6);
        return button;
    }
    private static UnityEngine.UI.Toggle Toggle(string name, Transform parent, string label, float y)
    {
        var row = Panel(name, parent, new Vector2(0, y), new Vector2(750, 125), new Color(.14f, .21f, .32f));
        Label("Label", row, label, new Vector2(-85, 0), new Vector2(540, 100), 29);
        var box = Panel("Switch", row, new Vector2(275, 0), new Vector2(90, 90), new Color(.3f, .38f, .5f));
        var check = Panel("On", box, Vector2.zero, new Vector2(66, 66), _gold);
        check.GetComponent<Image>().raycastTarget = false;
        var toggle = row.gameObject.AddComponent<UnityEngine.UI.Toggle>();
        toggle.targetGraphic = box.GetComponent<Image>();
        toggle.graphic = check.GetComponent<Image>();
        return toggle;
    }
    private static RectTransform Coin(string name, Transform parent, Vector2 position, int size)
    {
        var rect = Rect(name, parent, position, new Vector2(size, size));
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/star_full.png");
        image.color = _gold;
        image.raycastTarget = false;
        return rect;
    }
    private static PopupView Popup(string name, Transform parent, Vector2 size, out RectTransform card)
    {
        var root = Rect(name + " Popup", parent, Vector2.zero, Vector2.zero);
        Stretch(root);
        var group = root.gameObject.AddComponent<CanvasGroup>();
        var backdrop = Panel("Backdrop", root, Vector2.zero, Vector2.zero, Color.black);
        Stretch(backdrop);
        var backdropGroup = backdrop.gameObject.AddComponent<CanvasGroup>();
        backdropGroup.alpha = .7f;
        var safe = Safe(root);
        var layout = Rect("Portrait layout", safe, Vector2.zero, Vector2.zero);
        Stretch(layout);
        layout.gameObject.AddComponent<PortraitLayoutFitter>();
        card = Panel("Card", layout, Vector2.zero, size, _navy);
        var popup = root.gameObject.AddComponent<PopupView>();
        Set(popup, "_group", group, "_backdrop", backdropGroup, "_card", card);
        root.gameObject.SetActive(false);
        return popup;
    }
    private static void Set(UnityEngine.Object target, params object[] pairs)
    {
        var serialized = new SerializedObject(target);
        for (int i = 0; i < pairs.Length; i += 2)
            serialized.FindProperty((string)pairs[i]).objectReferenceValue = (UnityEngine.Object)pairs[i + 1];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("Bus Jam/Build Android APK")]
    public static void BuildAndroid()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            throw new InvalidOperationException("Install Android Build Support for Unity 2022.3.62f2 in Unity Hub first.");
        if (!File.Exists(_prefabPath)) Generate();
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.busjamstudio.busjam");
        EditorUserBuildSettings.buildAppBundle = false;
        Directory.CreateDirectory("Builds/Android");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/MainScene.unity" }, locationPathName = "Builds/Android/BusJam.apk",
            target = BuildTarget.Android, options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("APK build failed: " + report.summary.result);
    }
}
