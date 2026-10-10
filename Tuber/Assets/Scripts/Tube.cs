using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;

namespace DefaultNamespace
{
    public partial class Tube : MonoBehaviour
    {
        [Header("Balls")] [SerializeField] private Ball ballPrefab;
        [SerializeField] private Transform[] ballSlots;
        [SerializeField] private Sprite redSprite;
        [SerializeField] private Sprite blueSprite;
        [SerializeField] private Sprite greenSprite;

        [Header("Bottom Sprites")] [SerializeField]
        private Sprite redBottomSprite;

        [SerializeField] private Sprite blueBottomSprite;
        [SerializeField] private Sprite greenBottomSprite;

        [SerializeField] private int tubeIndex;
        [SerializeField] private float selectedOffset = 0.25f;
        [SerializeField] private float selectedScale = 1.05f;
        [SerializeField] private float pourUpOffset = 1.5f;
        [SerializeField] private float moveDuration = 0.2f;

        private Vector3 _originalPosition;
        private Vector3 _originalScale;
        private Quaternion _originalRotation;
        private bool _isSelected;

        private Stack<TubeColor> _colorsStack;
        private List<Ball> _slotBalls = new();
        private List<Ball> _balls = new();

        private int Count => _balls.Count;
        public bool IsEmpty => _balls.Count == 0;

        public bool IsComplete
        {
            get
            {
                if (_balls.Count != 2)
                    return false;

                var color = _balls[0].Color;
                return _balls.All(ball => ball.Color == color);
            }
        }

        private bool IsFull => _balls.Count >= ballSlots.Length;
        private TubeColor TopColor => _balls.Last().Color;

        private void Start() => SpawnBalls();

        private void Awake()
        {
            _originalPosition = transform.position;
            _originalScale = transform.localScale;
            _originalRotation = transform.rotation;
            _colorsStack = new Stack<TubeColor>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();

            tubeIndex = transform.GetSiblingIndex();
            switch (tubeIndex)
            {
                case 0:
                    _colorsStack.Push(TubeColor.Red);
                    _colorsStack.Push(TubeColor.Green);
                    _colorsStack.Push(TubeColor.Blue);
                    break;
                case 1:
                    _colorsStack.Push(TubeColor.Green);
                    _colorsStack.Push(TubeColor.Blue);
                    _colorsStack.Push(TubeColor.Red);
                    break;
            }
        }

        public void Select()
        {
            if (_isSelected) return;
            _isSelected = true;

            transform.DOKill();
            Sequence sequence = DOTween.Sequence();
            sequence.Join(transform.DOMoveY(_originalPosition.y + selectedOffset, moveDuration).SetEase(Ease.OutQuad));
            sequence.Join(transform.DOScale(_originalScale * selectedScale, moveDuration).SetEase(Ease.OutQuad));
        }

        public void Deselect()
        {
            if (!_isSelected) return;
            _isSelected = false;

            transform.DOKill();
            Sequence sequence = DOTween.Sequence();
            sequence.Join(transform.DOMoveY(_originalPosition.y, moveDuration).SetEase(Ease.OutQuad));
            sequence.Join(transform.DOScale(_originalScale, moveDuration).SetEase(Ease.OutQuad));
        }

        private int GetTopSameColorCount()
        {
            if (IsEmpty) return 0;

            var top = TopColor;
            var count = 0;
            for (var i = _balls.Count - 1; i >= 0; --i)
            {
                if (_balls[i].Color != top) break;
                count++;
            }

            return count;
        }

        public int GetPourAmount(Tube target)
        {
            if (target == this || IsEmpty || target.IsFull) return 0;

            var freeSpace = target.ballSlots.Length - target.Count;
            return Mathf.Min(GetTopSameColorCount(), freeSpace);
        }

        public void HandleTubeShake(Action onComplete = null)
        {
            transform.DOKill();
            Sequence sequence = DOTween.Sequence();
            sequence.Append(transform.DOShakeRotation(0.45f, new Vector3(5f, 5f, 5f), 20))
                .AppendCallback(() => transform.position = _originalPosition)
                .OnComplete(() => onComplete?.Invoke());
        }

        private void SpawnBalls()
        {
            var colors = _colorsStack.ToArray();
            Array.Reverse(colors);

            for (var i = 0; i < ballSlots.Length; ++i)
            {
                var ball = Instantiate(ballPrefab, ballSlots[i].position, Quaternion.identity, ballSlots[i]);
                _slotBalls.Add(ball);
                ball.SetVisible(false);

                if (i >= colors.Length)
                    continue;

                var isBottom = (i == 0);
                ball.Setup(colors[i], GetSprite(colors[i], isBottom));
                ball.SetVisible(true);
                _balls.Add(ball);
            }
        }

        private Sprite GetSprite(TubeColor color, bool isBottom)
        {
            return color switch
            {
                TubeColor.Red => isBottom ? redBottomSprite : redSprite,
                TubeColor.Blue => isBottom ? blueBottomSprite : blueSprite,
                TubeColor.Green => isBottom ? greenBottomSprite : greenSprite,
                _ => null
            };
        }

        public bool CanPourInto(Tube tube) =>
            tube != null && tube != this && !IsEmpty && !tube.IsFull && (tube.IsEmpty || tube.TopColor == TopColor);

        private void OnMouseDown() => FindObjectOfType<GameManager>().OnTubeClicked(this);
    }
}

public enum TubeColor
{
    Red,
    Blue,
    Green
}