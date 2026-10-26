using UnityEngine;

namespace BusJam
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _lastArea;
        private Vector2Int _lastSize;
        private void OnEnable() { _rect = (RectTransform)transform; Apply(); }
        private void Update()
        {
            if (_lastArea != Screen.safeArea || _lastSize != new Vector2Int(Screen.width, Screen.height)) Apply();
        }
        private void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            _lastArea = Screen.safeArea;
            _lastSize = new Vector2Int(Screen.width, Screen.height);
            _rect.anchorMin = _lastArea.min / (Vector2)_lastSize;
            _rect.anchorMax = _lastArea.max / (Vector2)_lastSize;
            _rect.offsetMin = _rect.offsetMax = Vector2.zero;
        }
    }
}
