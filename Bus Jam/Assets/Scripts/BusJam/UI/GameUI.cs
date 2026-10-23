using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BusJam
{
    public class GameUI : MonoBehaviour
    {
        private Font _font;
        private RectTransform _canvasRoot, _safeRoot;
        private GameObject _modal;
        private Text _levelText, _remainingText, _modalTitle, _modalBody, _primaryText, _soundText;
        private Button _primary, _secondary;
        private Action _primaryAction, _secondaryAction;
        private Rect _lastSafeArea;
        private int _lastWidth, _lastHeight;
        private readonly Color _ink = new Color(.12f, .19f, .32f);

        public void Initialize(GameCatalog catalog, Action pause, Action retry, Action sound)
        {
            _font = catalog.font != null ? catalog.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = .5f;
            gameObject.AddComponent<GraphicRaycaster>();
            _canvasRoot = GetComponent<RectTransform>();
            _safeRoot = Rect("Safe area", _canvasRoot, Vector2.zero, new Vector2(1080, 1920));
            var portrait = Rect("Portrait HUD", _safeRoot, Vector2.zero, new Vector2(1080, 1920));
            var fitter = portrait.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1080f / 1920;
            var top = Panel("Header", portrait, new Vector2(0, 869), new Vector2(1010, 114),
                new Color(.1f, .17f, .29f, .94f));
            Text("Brand", top, "BUS JAM", new Vector2(-340, 0), new Vector2(260, 80), 43, Color.white);
            _levelText = Text("Level", top, "LEVEL 1", Vector2.zero, new Vector2(330, 80), 39, Color.white);
            _remainingText = Text("Remaining count", portrait, "0", new Vector2(-350, 647), new Vector2(170, 75), 52,
                Color.white);
            Text("Remaining label", portrait, "WAITING", new Vector2(-350, 600), new Vector2(160, 35), 23, _ink);

            var controlColor = new Color(.12f, .19f, .30f, .94f);
            var controlSize = new Vector2(240, 84);
            FlatButton("Pause", portrait, "PAUSE", new Vector2(-260, -850), controlSize, controlColor, pause, 28);
            FlatButton("Retry", portrait, "RETRY", new Vector2(0, -850), controlSize, controlColor, retry, 28);
            var soundButton = FlatButton("Sound", portrait, "SOUND ON", new Vector2(260, -850), controlSize,
                controlColor, sound, 28);
            _soundText = soundButton.GetComponentInChildren<Text>();

            _modal = Panel("Modal overlay", _canvasRoot, Vector2.zero, Vector2.zero, new Color(.02f, .05f, .1f, .75f))
                .gameObject;
            var m = (RectTransform)_modal.transform;
            m.anchorMin = Vector2.zero;
            m.anchorMax = Vector2.one;
            m.sizeDelta = Vector2.zero;
            var cardBorder = Panel("Dialog border", m, Vector2.zero, new Vector2(606, 366),
                new Color(.28f, .38f, .52f, 1f));
            var card = Panel("Dialog", cardBorder, Vector2.zero, new Vector2(600, 360),
                new Color(.12f, .16f, .24f, .98f));
            _modalTitle = Text("Title", card, "BUS JAM", new Vector2(0, 85), new Vector2(560, 70), 44, Color.white);
            _modalBody = Text("Description", card, "", new Vector2(0, 25), new Vector2(540, 50), 24,
                new Color(.82f, .87f, .94f));
            _modalBody.fontStyle = FontStyle.Normal;
            _primary = FlatButton("Primary", card, "PLAY", new Vector2(0, -25), new Vector2(440, 75),
                new Color(.2f, .5f, .88f), () => _primaryAction?.Invoke(), 30);
            _primaryText = _primary.GetComponentInChildren<Text>();
            _secondary = FlatButton("Secondary", card, "RETRY", new Vector2(0, -105), new Vector2(440, 65),
                new Color(.32f, .38f, .48f), () => _secondaryAction?.Invoke(), 26);
            _modal.SetActive(false);
            if (EventSystem.current == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            UpdateSafeArea();
        }

        private void Update()
        {
            if (_lastSafeArea != Screen.safeArea || _lastWidth != Screen.width || _lastHeight != Screen.height)
                UpdateSafeArea();
        }

        private void UpdateSafeArea()
        {
            if (_safeRoot == null) return;
            _lastSafeArea = Screen.safeArea;
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
            _safeRoot.anchorMin = _lastSafeArea.min / new Vector2(Screen.width, Screen.height);
            _safeRoot.anchorMax = _lastSafeArea.max / new Vector2(Screen.width, Screen.height);
            _safeRoot.offsetMin = _safeRoot.offsetMax = Vector2.zero;
        }

        public void Refresh(int level, RoundState round)
        {
            _levelText.text = $"LEVEL {level + 1}";
            _remainingText.text = round.Remaining.ToString();
        }

        public void Hint(string message)
        {
        }

        public void SetSound(bool muted) => _soundText.text = muted ? "SOUND OFF" : "SOUND ON";
        public void Hide() => _modal.SetActive(false);
        public bool ModalVisible => _modal.activeSelf;

        public void Show(string title, string body, string action, Action onPrimary, string second = null,
            Action onSecondary = null)
        {
            _modalTitle.text = title;
            _primaryText.text = action;
            _primaryAction = onPrimary;
            _secondaryAction = onSecondary;

            bool hasBody = !string.IsNullOrWhiteSpace(body);
            _modalBody.gameObject.SetActive(hasBody);
            if (hasBody) _modalBody.text = body;

            bool hasSecond = !string.IsNullOrWhiteSpace(second);
            _secondary.gameObject.SetActive(hasSecond);
            if (hasSecond) _secondary.GetComponentInChildren<Text>().text = second;

            var titleRect = _modalTitle.rectTransform;
            var primaryRect = _primary.GetComponent<RectTransform>();
            var secondRect = _secondary.GetComponent<RectTransform>();

            if (!hasBody)
            {
                titleRect.anchoredPosition = new Vector2(0, 75);
                if (hasSecond)
                {
                    primaryRect.anchoredPosition = new Vector2(0, -20);
                    secondRect.anchoredPosition = new Vector2(0, -100);
                }
                else
                {
                    primaryRect.anchoredPosition = new Vector2(0, -50);
                }
            }
            else
            {
                titleRect.anchoredPosition = new Vector2(0, 95);
                _modalBody.rectTransform.anchoredPosition = new Vector2(0, 35);
                if (hasSecond)
                {
                    primaryRect.anchoredPosition = new Vector2(0, -30);
                    secondRect.anchoredPosition = new Vector2(0, -105);
                }
                else
                {
                    primaryRect.anchoredPosition = new Vector2(0, -60);
                }
            }

            _modal.SetActive(true);
        }

        private RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var r = (RectTransform)go.transform;
            r.SetParent(parent, false);
            r.sizeDelta = size;
            r.anchoredPosition = position;
            return r;
        }

        private RectTransform Panel(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var r = Rect(name, parent, position, size);
            r.gameObject.AddComponent<Image>().color = color;
            return r;
        }

        private Text Text(string name, Transform parent, string value, Vector2 position, Vector2 size, int fontSize,
            Color color)
        {
            var r = Rect(name, parent, position, size);
            var t = r.gameObject.AddComponent<Text>();
            t.font = _font;
            t.text = value;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontStyle = FontStyle.Bold;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            return t;
        }

        private Button FlatButton(string name, Transform parent, string label, Vector2 position, Vector2 size,
            Color bgColor, Action action, int fontSize = 30)
        {
            var r = Panel(name, parent, position, size, bgColor);
            var b = r.gameObject.AddComponent<Button>();
            b.targetGraphic = r.GetComponent<Image>();
            var colors = b.colors;
            colors.highlightedColor = bgColor * 1.15f;
            colors.pressedColor = bgColor * 0.8f;
            b.colors = colors;
            Text("Label", r, label, Vector2.zero, size - new Vector2(10, 6), fontSize, Color.white);
            b.onClick.AddListener(() => action());
            return b;
        }
    }
}