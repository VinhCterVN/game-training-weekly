using UnityEngine;

namespace BusJam
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class PortraitLayoutFitter : MonoBehaviour
    {
        [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 1920f);
        private RectTransform _rect;
        private void OnEnable() { _rect = (RectTransform)transform; Fit(); }
        private void LateUpdate() => Fit();
        private void Fit()
        {
            var parent = (RectTransform)_rect.parent;
            _rect.anchorMin = _rect.anchorMax = _rect.pivot = Vector2.one * .5f;
            _rect.anchoredPosition = Vector2.zero;
            _rect.sizeDelta = referenceResolution;
            float scale = Mathf.Min(parent.rect.width / referenceResolution.x, parent.rect.height / referenceResolution.y);
            _rect.localScale = Vector3.one * Mathf.Max(.001f, scale);
        }
    }
}
