using DG.Tweening;
using UnityEngine;

namespace TuberLevels
{
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private LevelManager _levels;
        [SerializeField] private WaterSortController _sort;
        [SerializeField] private UIManager _ui;
        [SerializeField] private AudioManager _audio;

        public void Configure(LevelManager levels, WaterSortController sort, UIManager ui, AudioManager audio)
        {
            _levels = levels;
            _sort = sort;
            _ui = ui;
            _audio = audio;
        }

        private void Start()
        {
            Application.targetFrameRate = 60;
            DOTween.Init(false, true).SetCapacity(100, 20);
            _audio.Initialize();
            _sort.Initialize(_levels, _ui, _audio);
            _ui.Initialize(_levels, _sort, _audio);
            _levels.Initialize(_sort, _ui);
        }
    }
}