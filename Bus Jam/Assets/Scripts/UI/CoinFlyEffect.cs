using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace BusJam
{
    public sealed class CoinFlyEffect : MonoBehaviour
    {
        // All ten icons are authored in the prefab. No runtime icon allocation.
        [SerializeField] private RectTransform[] _icons;
        private readonly Queue<RectTransform> _pool = new();
        private readonly List<Sequence> _flights = new();
        public bool IsPlaying { get; private set; }
        private void Awake()
        {
            foreach (var icon in _icons) { icon.gameObject.SetActive(false); _pool.Enqueue(icon); }
        }
        public void Play(RectTransform target, Action<int> landed, Action completed)
        {
            if (IsPlaying) return;
            IsPlaying = true;
            int count = _pool.Count;
            int remaining = count;
            if (count == 0) { IsPlaying = false; completed?.Invoke(); return; }
            _flights.Clear();
            for (int i = 0; i < count; i++)
            {
                var icon = _pool.Dequeue();
                icon.gameObject.SetActive(true);
                icon.anchoredPosition = Vector2.zero;
                icon.localScale = Vector3.one;
                Vector3 origin = icon.position;
                Vector3 burst = origin + (Vector3)(UnityEngine.Random.insideUnitCircle * 110);
                Vector3 control = (burst + target.position) * .5f + Vector3.left * 180;
                var flight = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
                flight.AppendInterval(i * .065f);
                flight.Append(icon.DOMove(burst, .18f).SetEase(Ease.OutQuad).SetLink(gameObject));
                flight.Append(DOTween.To(() => 0f, t =>
                {
                    // Re-evaluate the destination so safe-area/viewport changes do not leave coins behind.
                    icon.position = (1 - t) * (1 - t) * burst + 2 * (1 - t) * t * control + t * t * target.position;
                }, 1, .55f).SetEase(Ease.InQuad).SetLink(gameObject));
                flight.OnComplete(() =>
                {
                    icon.gameObject.SetActive(false);
                    _pool.Enqueue(icon);
                    landed?.Invoke(10);
                    if (--remaining != 0) return;
                    IsPlaying = false;
                    completed?.Invoke();
                });
                _flights.Add(flight);
            }
        }
        private void OnDestroy() { foreach (var flight in _flights) flight.Kill(); }
    }
}
