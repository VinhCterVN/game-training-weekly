using UnityEngine;

namespace TuberLevels
{
    [ExecuteAlways]
    public sealed class PortraitLayout : MonoBehaviour
    {
        private void LateUpdate()
        {
            var parent = transform.parent as RectTransform;
            if (parent == null) return;
            float scale = Mathf.Min(parent.rect.width / 720, parent.rect.height / 1280);
            transform.localScale = Vector3.one * scale;
        }
    }
}