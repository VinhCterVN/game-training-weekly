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
        private Sequence _transition;
        public bool Visible { get; private set; }
        public void Open()
        {
            _transition?.Kill();
            Visible = true;
            gameObject.SetActive(true);
            _group.alpha = 1;
            _group.blocksRaycasts = _group.interactable = true;
            _card.localScale = Vector3.one * .5f;
            _backdrop.alpha = 0;
            _transition = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _transition.Join(_card.DOScale(1, .35f).SetEase(Ease.OutBack).SetLink(gameObject));
            _transition.Join(_backdrop.DOFade(.7f, .25f).SetLink(gameObject));
        }
        public void Close(Action completed = null)
        {
            _transition?.Kill();
            if (!Visible) { completed?.Invoke(); return; }
            _group.interactable = false;
            _transition = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _transition.Join(_card.DOScale(0, .18f).SetEase(Ease.InBack).SetLink(gameObject));
            _transition.Join(_group.DOFade(0, .18f).SetLink(gameObject));
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
