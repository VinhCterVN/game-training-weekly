using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

namespace DefaultNamespace
{
    public partial class Tube
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip pourSound;

        [Header("Pour")] [SerializeField] private float exitOffset = 0.8f;
        [SerializeField] private float ballMoveDuration = 0.15f;
        [SerializeField] private int pouringSortingOrderOffset = 100;

        public Sequence PourTo(Tube target, int amount)
        {
            _isSelected = false;
            var sequence = DOTween.Sequence();
            var transfers = new List<(Ball sourceBall, Ball targetBall, Vector3 targetScale)>();
            var originalBallSortingOrders = _balls
                .Select(ball => (ball, sortingOrder: ball.SortingOrder))
                .ToList();

            var tubeRenderer = GetComponent<SpriteRenderer>();
            var originalTubeSortingOrder = tubeRenderer.sortingOrder;
            tubeRenderer.sortingOrder += pouringSortingOrderOffset;
            foreach (var (ball, _) in originalBallSortingOrders)
                ball.SortingOrder += pouringSortingOrderOffset + 1;

            for (var i = 0; i < amount; ++i)
            {
                var ball = _balls.Last();
                _balls.RemoveAt(_balls.Count - 1);

                var targetSlotIndex = target._balls.Count;
                var targetBall = target._slotBalls[targetSlotIndex];
                var targetScale = targetBall.transform.localScale;
                targetBall.Setup(ball.Color, target.GetSprite(ball.Color, targetSlotIndex == 0));
                target._balls.Add(targetBall);
                transfers.Add((ball, targetBall, targetScale));
            }

            var targetPosition = target.transform.position;
            var distance = _originalPosition.x - targetPosition.x;
            distance += distance > 0 ? -exitOffset : exitOffset;

            sequence
                .Append(transform.DOMoveY(_originalPosition.y + pourUpOffset, 0.15f))
                .Join(transform.DOScale(_originalScale, 0.15f))
                .Append(transform.DOMoveX(_originalPosition.x - distance, 0.5f))
                .Append(transform.DORotate(new Vector3(0, 0, distance < 0 ? -25 : 25), 0.5f))
                .AppendCallback(() =>
                {
                    if (audioSource != null && pourSound != null)
                        audioSource.PlayOneShot(pourSound);

                    foreach (var transfer in transfers)
                    {
                        transfer.sourceBall.SetVisible(false);
                        transfer.targetBall.transform.localScale = Vector3.zero;
                        transfer.targetBall.SetVisible(true);
                        transfer.targetBall.transform.DOScale(transfer.targetScale, ballMoveDuration)
                            .SetEase(Ease.OutQuad);
                    }
                })
                .Append(transform.DORotate(Vector3.zero, 0.3f))
                .Append(transform.DOMove(_originalPosition, 0.3f))
                .AppendCallback(() =>
                {
                    tubeRenderer.sortingOrder = originalTubeSortingOrder;
                    foreach (var (ball, sortingOrder) in originalBallSortingOrders)
                        ball.SortingOrder = sortingOrder;
                });

            return sequence;
        }

    }
}
