using UnityEngine;

namespace BusJam
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class BackgroundFitter : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float shakeMargin = 1.02f;

        private SpriteRenderer _renderer;
        private int _lastWidth;
        private int _lastHeight;
        private float _lastOrtho;

        private void OnEnable()
        {
            _renderer = GetComponent<SpriteRenderer>();
            Fit();
        }

        private void LateUpdate()
        {
            var cam = ResolveCamera();
            if (cam == null) return;
            if (Screen.width != _lastWidth || Screen.height != _lastHeight || !Mathf.Approximately(cam.orthographicSize, _lastOrtho))
                Fit();
        }

        [ContextMenu("Fit Now")]
        public void Fit()
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            var cam = ResolveCamera();
            if (_renderer == null || _renderer.sprite == null || cam == null || !cam.orthographic) return;

            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
            _lastOrtho = cam.orthographicSize;

            float worldHeight = cam.orthographicSize * 2f;
            float worldWidth = worldHeight * cam.aspect;
            Vector2 spriteSize = _renderer.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f) return;

            float scaleX = (worldWidth / spriteSize.x) * shakeMargin;
            float scaleY = (worldHeight / spriteSize.y) * shakeMargin;

            // Uniform cover on portrait/narrow screens; stretch width only on wide screens so the bus stop curb stays aligned.
            transform.localScale = scaleY >= scaleX
                ? new Vector3(scaleY, scaleY, 1f)
                : new Vector3(scaleX, scaleY, 1f);
        }

        private Camera ResolveCamera() => targetCamera != null ? targetCamera : Camera.main;
    }
}
