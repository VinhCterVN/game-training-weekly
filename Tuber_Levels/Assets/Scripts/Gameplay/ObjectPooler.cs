using System;
using System.Collections.Generic;
using UnityEngine;

namespace TuberLevels
{
    public sealed class ObjectPooler : MonoBehaviour
    {
        [SerializeField] private BottleController _prefab;
        [SerializeField] private RectTransform _board;
        [SerializeField] private int _capacity = 10;
        private readonly Stack<BottleController> _available = new Stack<BottleController>(10);
        public int CreatedCount { get; private set; }

        public void Configure(BottleController prefab, RectTransform board)
        {
            _prefab = prefab;
            _board = board;
        }

        public void Prewarm()
        {
            if (CreatedCount > 0) return;
            for (int i = 0; i < _capacity; i++)
            {
                var bottle = Instantiate(_prefab, _board);
                bottle.name = "Pooled bottle " + (i + 1);
                bottle.gameObject.SetActive(false);
                _available.Push(bottle);
                CreatedCount++;
            }
        }

        public BottleController Get()
        {
            if (_available.Count == 0)
                throw new InvalidOperationException("Level exceeds the prewarmed pool capacity.");
            var bottle = _available.Pop();
            bottle.gameObject.SetActive(true);
            return bottle;
        }

        public void Release(BottleController bottle)
        {
            bottle.Release();
            _available.Push(bottle);
        }
    }
}