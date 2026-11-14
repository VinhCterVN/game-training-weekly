using System;
using DG.Tweening;
using UnityEngine;

namespace BusJam
{
    public sealed class PopupView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private CanvasGroup _backdrop;
        [SerializeField] private RectTransform _card;
        [SerializeField] private RectTransform options = null;
        [SerializeField] private GameObject congrats = null;

        [Header("Animation Settings")]
        [SerializeField] private float cardInitialScale = 0.5f;
        [SerializeField] private float openDuration = 0.35f;
        [SerializeField] private Ease openEase = Ease.OutBack;
        [SerializeField] private float backdropTargetAlpha = 0.7f;
        [SerializeField] private float backdropFadeDuration = 0.25f;
        [SerializeField] private float closeDuration = 0.18f;
        [SerializeField] private Ease closeEase = Ease.InBack;

        private Sequence _transition;
        public bool Visible { get; private set; }
        public void Open(bool withOptions = false, bool isWin = true)
        {
            _transition?.Kill();
            Visible = true;
            gameObject.SetActive(true);
            if (options != null) options.gameObject.SetActive(withOptions);
            if (congrats != null) congrats.SetActive(isWin);
            _group.alpha = 1;
            _group.blocksRaycasts = _group.interactable = true;
            _card.localScale = Vector3.one * cardInitialScale;
            _backdrop.alpha = 0;
            _transition = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _transition.Join(_card.DOScale(1, openDuration).SetEase(openEase).SetLink(gameObject));
            _transition.Join(_backdrop.DOFade(backdropTargetAlpha, backdropFadeDuration).SetLink(gameObject));
        }
        public void Close(Action completed = null)
        {
            _transition?.Kill();
            if (!Visible) { completed?.Invoke(); return; }
            _group.interactable = false;
            _transition = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _transition.Join(_card.DOScale(0, closeDuration).SetEase(closeEase).SetLink(gameObject));
            _transition.Join(_group.DOFade(0, closeDuration).SetLink(gameObject));
            _transition.OnComplete(() =>
            {
                Visible = false;
                _group.blocksRaycasts = false;
                gameObject.SetActive(false);
                completed?.Invoke();
            });
        }
        private void OnDestroy() => _transition?.Kill();
    }
}
