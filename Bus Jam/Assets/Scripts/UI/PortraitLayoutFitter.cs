using UnityEngine;

namespace BusJam
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class PortraitLayoutFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private void OnEnable() { _rect = (RectTransform)transform; Fit(); }
        private void LateUpdate() => Fit();
        private void Fit()
        {
            var parent = (RectTransform)_rect.parent;
            _rect.anchorMin = _rect.anchorMax = _rect.pivot = Vector2.one * .5f;
            _rect.anchoredPosition = Vector2.zero;
            _rect.sizeDelta = new Vector2(1080, 1920);
            float scale = Mathf.Min(parent.rect.width / 1080, parent.rect.height / 1920);
            _rect.localScale = Vector3.one * Mathf.Max(.001f, scale);
        }
    }
}
