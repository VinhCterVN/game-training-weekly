using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BusJam
{
    public sealed class ShopController : MonoBehaviour
    {
        [Serializable]
        public struct OfferBinding
        {
            public string offerId;
            public Button buyButton;
        }

        [SerializeField] private OfferBinding[] offers;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private string comingSoonMessage = "Coming Soon!";
        [SerializeField] private float noticeDuration = 1.6f;

        public event Action<string> OfferSelected;

        private Sequence _noticeSeq;

        private void Awake()
        {
            if (statusLabel != null) statusLabel.alpha = 0f;
            if (offers == null) return;
            for (int i = 0; i < offers.Length; i++)
            {
                var offer = offers[i];
                if (offer.buyButton == null) continue;
                string id = offer.offerId;
                Button btn = offer.buyButton;
                btn.onClick.AddListener(() => OnOfferClicked(id, btn));
            }
        }

        private void OnDisable()
        {
            _noticeSeq?.Kill();
            if (statusLabel != null) statusLabel.alpha = 0f;
        }

        private void OnOfferClicked(string offerId, Button button)
        {
            var ui = GetComponentInParent<GameUI>();
            if (ui != null) ui.ClickFeedback();

            if (button != null)
            {
                button.transform.DOKill();
                button.transform.localScale = Vector3.one;
                button.transform.DOPunchScale(Vector3.one * 0.08f, 0.18f, 1).SetUpdate(true).SetLink(button.gameObject);
            }

            OfferSelected?.Invoke(offerId);
            ShowNotice(comingSoonMessage);
        }

        public void ShowNotice(string message)
        {
            if (statusLabel == null) return;
            _noticeSeq?.Kill();
            statusLabel.text = message;
            statusLabel.alpha = 1f;
            _noticeSeq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _noticeSeq.AppendInterval(noticeDuration);
            _noticeSeq.Append(statusLabel.DOFade(0f, 0.25f));
        }

        private void OnDestroy() => _noticeSeq?.Kill();
    }
}
