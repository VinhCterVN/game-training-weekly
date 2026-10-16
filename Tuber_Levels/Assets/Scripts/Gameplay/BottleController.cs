using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TuberLevels
{
    public sealed class BottleController : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private RectTransform _mouthPoint;
        [SerializeField] private Image _glass;
        [SerializeField] private Image _selection;
        [SerializeField] private Image[] _layers;
        [SerializeField] private Sprite[] _water;
        [SerializeField] private Sprite[] _bottomWater;
        [SerializeField] private Text _number;
        private readonly List<WaterColor> _colors = new List<WaterColor>(4);
        private WaterSortController _controller;
        private Sequence _sequence;
        private Vector3 _restPosition;
        private int _restOrder;
        public List<WaterColor> Colors => _colors;
        public RectTransform MouthPoint => _mouthPoint;
        public int Index { get; private set; }
        public Vector3 RestPosition => _restPosition;

        public void Configure(RectTransform mouth, Image glass, Image selection, Image[] layers, Sprite[] water,
            Sprite[] bottom, Text number)
        {
            _mouthPoint = mouth;
            _glass = glass;
            _selection = selection;
            _layers = layers;
            _water = water;
            _bottomWater = bottom;
            _number = number;
        }

        public void Init(BottleData data, int index, WaterSortController controller)
        {
            CancelMotion();
            Index = index;
            _controller = controller;
            _colors.Clear();
            _colors.AddRange(data.colors);
            ((RectTransform)transform).anchoredPosition = data.position;
            transform.localRotation = Quaternion.identity;
            _restPosition = transform.localPosition;
            _restOrder = transform.GetSiblingIndex();
            _number.text = (index + 1).ToString("00");
            _selection.enabled = false;
            Refresh();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _controller.Select(this);
        }

        public void Refresh()
        {
            for (int i = 0; i < 4; i++)
            {
                _layers[i].enabled = i < _colors.Count;
                if (i < _colors.Count) _layers[i].sprite = (i == 0 ? _bottomWater : _water)[(int)_colors[i]];
            }

            _number.color = WaterSortRules.IsComplete(_colors)
                ? new Color(.1f, .55f, .38f)
                : new Color(.2f, .38f, .43f);
        }

        public void SetSelected(bool selected)
        {
            transform.DOKill();
            _selection.enabled = selected;
            transform.DOLocalMove(_restPosition + (selected ? Vector3.up * 24 : Vector3.zero), .16f);
        }

        public void AnimatePour(BottleController target, Action transfer, Action completed, Image stream, Color color)
        {
            transform.DOKill();
            _selection.enabled = false;
            transform.SetAsLastSibling();
            float angle = target.transform.localPosition.x >= _restPosition.x ? -65 : 65;
            Quaternion rotation = Quaternion.Euler(0, 0, angle);
            
            Vector3 destinationMouth = transform.parent.InverseTransformPoint(target.MouthPoint.position);
            Vector3 streamEnd = destinationMouth;
            Vector3 streamStart = destinationMouth + Vector3.up * 65;
            Vector3 position = streamStart - rotation * _mouthPoint.localPosition;
            _sequence = DOTween.Sequence();
            _sequence.Append(transform.DOLocalMove(position, .3f).SetEase(Ease.InOutSine));
            _sequence.Join(transform.DOLocalRotate(new Vector3(0, 0, angle), .3f));
            _sequence.AppendCallback(() =>
            {
                stream.color = color;
                stream.transform.SetAsLastSibling();
                stream.rectTransform.localPosition = (streamStart + streamEnd) * .5f;
                stream.rectTransform.sizeDelta = new Vector2(9, Vector3.Distance(streamStart, streamEnd));
                stream.gameObject.SetActive(true);
            });
            _sequence.AppendInterval(.3f);
            _sequence.AppendCallback(transfer.Invoke);
            _sequence.AppendInterval(.12f);
            _sequence.AppendCallback(() => stream.gameObject.SetActive(false));
            _sequence.Append(transform.DOLocalMove(_restPosition, .28f).SetEase(Ease.InOutSine));
            _sequence.Join(transform.DOLocalRotate(Vector3.zero, .28f));
            _sequence.OnComplete(() =>
            {
                transform.SetSiblingIndex(_restOrder);
                completed();
            });
        }

        public void CancelMotion()
        {
            _sequence?.Kill();
            _sequence = null;
            transform.DOKill();
        }

        public void Release()
        {
            CancelMotion();
            _colors.Clear();
            _controller = null;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            CancelMotion();
        }
    }
}