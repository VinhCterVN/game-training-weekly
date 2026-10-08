using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefaultNamespace
{
    public partial class GameManager
    {
        [SerializeField] private Sprite winPanelSprite;
        [SerializeField] private Sprite restartButtonSprite;

        private GameObject _winPopup;

        private void Start()
        {
            BindTopBarButtons();
        }

        public void PlayAgain()
        {
            DG.Tweening.DOTween.KillAll();
#if UNITY_EDITOR
            UnityEditor.Selection.activeObject = null;
#endif
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void BindTopBarButtons()
        {
            var topBar = FindObjectsOfType<Transform>(true)
                .FirstOrDefault(transform => NormalizeName(transform.name) == "topbar");
            if (topBar == null)
            {
                Debug.LogWarning("Could not find a GameObject named TopBar in the active scene.", this);
                return;
            }

            BindTopBarButton(topBar, "undo", UndoLastMove);
            BindTopBarButton(topBar, "playagain", PlayAgain);
        }

        private void BindTopBarButton(Transform topBar, string nameToken,
            UnityEngine.Events.UnityAction action)
        {
            var target = topBar.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => NormalizeName(transform.name).Contains(nameToken));
            if (target == null)
            {
                Debug.LogWarning($"Could not find a TopBar control named for '{nameToken}'.", topBar);
                return;
            }

            var button = target.GetComponent<Button>();
            var image = target.GetComponent<Image>() ?? target.GetComponentInChildren<Image>(true);
            if (button == null && image != null)
            {
                button = target.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
            }

            if (button != null)
            {
                button.onClick.AddListener(action);
                var canvas = button.GetComponentInParent<Canvas>();
                if (canvas != null)
                    EnsureGraphicRaycaster(canvas);
                else
                    Debug.LogWarning($"UI control '{target.name}' is not under a Canvas.", target);

                EnsureEventSystem();
            }

            var spriteRenderer = target.GetComponent<SpriteRenderer>() ??
                                 target.GetComponentInChildren<SpriteRenderer>(true);
            if (spriteRenderer != null)
            {
                var clickHandler = spriteRenderer.GetComponent<TopBarWorldButton>();
                if (clickHandler == null)
                    clickHandler = spriteRenderer.gameObject.AddComponent<TopBarWorldButton>();

                clickHandler.Initialize(this, nameToken == "undo");

                var collider = spriteRenderer.GetComponent<Collider2D>();
                if (collider == null)
                {
                    var boxCollider = spriteRenderer.gameObject.AddComponent<BoxCollider2D>();
                    boxCollider.size = spriteRenderer.sprite != null
                        ? spriteRenderer.sprite.bounds.size
                        : Vector2.one;
                    boxCollider.offset = spriteRenderer.sprite != null
                        ? (Vector2)spriteRenderer.sprite.bounds.center
                        : Vector2.zero;
                }
            }

            if (button == null && spriteRenderer == null)
                Debug.LogWarning($"TopBar control '{target.name}' needs a UI Image or a SpriteRenderer.", target);
        }

        private static string NormalizeName(string value)
        {
            return new string(value.Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
        }

        private static void EnsureGraphicRaycaster(Canvas canvas)
        {
            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                var eventSystemObject = new GameObject("EventSystem");
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }

            if (eventSystem.GetComponent<BaseInputModule>() == null)
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }

        private void CreateWinPopup()
        {
            var canvasObject = new GameObject("Win Popup Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var canvasScaler = canvasObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1080, 1920);
            canvasScaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            var overlay = CreateUIObject("Win Popup Overlay", canvas.transform);
            StretchToParent(overlay);
            var overlayImage = overlay.gameObject.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.72f);

            var panel = CreateUIObject("Win Panel", overlay);
            SetCenteredRect(panel, new Vector2(900f, 190f), new Vector2(0f, 55f));
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = winPanelSprite;
            panelImage.preserveAspect = true;
            panelImage.raycastTarget = false;

            var restart = CreateUIObject("Restart Button", overlay);
            SetCenteredRect(restart, new Vector2(128f, 128f), new Vector2(0f, -115f));
            var restartImage = restart.gameObject.AddComponent<Image>();
            restartImage.sprite = restartButtonSprite;
            restartImage.preserveAspect = true;
            var restartButton = restart.gameObject.AddComponent<Button>();
            restartButton.targetGraphic = restartImage;
            restartButton.onClick.AddListener(PlayAgain);

            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }

            _winPopup = overlay.gameObject;
            _winPopup.SetActive(false);
        }

        private static RectTransform CreateUIObject(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void SetCenteredRect(RectTransform rectTransform, Vector2 size, Vector2 position)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = position;
        }
    }
}
