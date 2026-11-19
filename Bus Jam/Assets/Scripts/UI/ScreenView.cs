using System;
using DG.Tweening;
using UnityEngine;

namespace BusJam
{
    public sealed class ScreenView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private float fadeDuration = 0.18f;

        private Tween _transition;
        public bool Visible { get; private set; }

        public void Show(bool instant = false)
        {
            _transition?.Kill();
            Visible = true;
            gameObject.SetActive(true);
            if (group == null) return;
            group.blocksRaycasts = group.interactable = true;
            if (instant || fadeDuration <= 0f)
            {
                group.alpha = 1f;
                return;
            }
            group.alpha = 0f;
            _transition = group.DOFade(1f, fadeDuration).SetUpdate(true).SetLink(gameObject);
        }

        public void Hide(Action completed = null, bool instant = false)
        {
            _transition?.Kill();
            if (!Visible && !gameObject.activeSelf)
            {
                completed?.Invoke();
                return;
            }
            Visible = false;
            if (group != null) group.interactable = false;
            if (instant || group == null || fadeDuration <= 0f)
            {
                if (group != null)
                {
                    group.alpha = 0f;
                    group.blocksRaycasts = false;
                }
                gameObject.SetActive(false);
                completed?.Invoke();
                return;
            }
            _transition = group.DOFade(0f, fadeDuration).SetUpdate(true).SetLink(gameObject).OnComplete(() =>
            {
                group.blocksRaycasts = false;
                gameObject.SetActive(false);
                completed?.Invoke();
            });
        }

        private void OnDestroy() => _transition?.Kill();
    }
}
