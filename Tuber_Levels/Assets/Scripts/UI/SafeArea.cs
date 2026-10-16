using UnityEngine;

namespace TuberLevels
{
    public sealed class SafeArea : MonoBehaviour
    {
        private Rect _last;
        private Vector2Int _screen;

        private void Update()
        {
            Rect safe = Screen.safeArea;
            if (safe == _last && _screen.x == Screen.width && _screen.y == Screen.height) return;
            _last = safe;
            _screen = new Vector2Int(Screen.width, Screen.height);
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}