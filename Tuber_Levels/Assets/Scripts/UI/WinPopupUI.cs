using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace TuberLevels
{
    public sealed class WinPopupUI : MonoBehaviour
    {
        [SerializeField] private RectTransform _card;
        [SerializeField] private Text _title;
        [SerializeField] private Text _nextLabel;
        [SerializeField] private Button _next;
        [SerializeField] private Button _replay;
        private Tween _tween;
        public event Action NextRequested;
        public event Action ReplayRequested;

        public void Configure(RectTransform card, Text title, Text nextLabel, Button next, Button replay)
        {
            _card = card;
            _title = title;
            _nextLabel = nextLabel;
            _next = next;
            _replay = replay;
        }

        public void Initialize()
        {
            _next.onClick.AddListener(() => NextRequested?.Invoke());
            _replay.onClick.AddListener(() => ReplayRequested?.Invoke());
        }

        public void Show(int moves, bool final)
        {
            gameObject.SetActive(true);
            _title.text = final ? "All levels complete!" : "You Win !!!";
            _nextLabel.text = final ? "PLAY FROM LEVEL 1" : "NEXT LEVEL";
            _tween?.Kill();
            _card.localScale = Vector3.one * .8f;
            _tween = _card.DOScale(1, .35f).SetEase(Ease.OutBack);
        }

        public void Hide()
        {
            _tween?.Kill();
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _tween?.Kill();
            if (_card != null) _card.DOKill();
            transform.DOKill();
        }
    }
}