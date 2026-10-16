using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TuberLevels
{
    public sealed class WaterSortController : MonoBehaviour
    {
        [SerializeField] private Image _stream;
        [SerializeField] private CompletionParticles _particles;
        private LevelManager _levels;
        private UIManager _ui;
        private AudioManager _audio;
        private BottleController _selected;
        private bool _isBusy;
        private bool _hasWon;
        private int _moves;
        private readonly List<PourMove> _history = new List<PourMove>(256);
        public bool IsBusy => _isBusy;
        public bool HasWon => _hasWon;
        public int Moves => _moves;

        public void Configure(Image stream, CompletionParticles particles)
        {
            _stream = stream;
            _particles = particles;
        }

        public void Initialize(LevelManager levels, UIManager ui, AudioManager audio)
        {
            _levels = levels;
            _ui = ui;
            _audio = audio;
        }

        public void Select(BottleController bottle)
        {
            if (_isBusy || _hasWon || _ui.IsOverlayOpen) return;
            _audio.Play(Sound.Click);
            if (_selected == bottle)
            {
                _selected.SetSelected(false);
                _selected = null;
                return;
            }

            if (_selected == null)
            {
                if (bottle.Colors.Count == 0)
                {
                    _ui.SetMessage("Choose a tube with water first.");
                    return;
                }

                _selected = bottle;
                bottle.SetSelected(true);
                return;
            }

            int amount = WaterSortRules.PourCount(_selected.Colors, bottle.Colors);
            if (amount == 0)
            {
                _audio.Play(Sound.Error);
                _ui.SetMessage("Pour onto the same color or into an empty tube.");
                return;
            }

            var source = _selected;
            _selected = null;
            _isBusy = true;
            _ui.SetBusy(true);
            var color = source.Colors[source.Colors.Count - 1];
            _audio.Play(Sound.Pour);
            source.AnimatePour(bottle, () =>
            {
                WaterSortRules.Apply(source.Colors, bottle.Colors, amount);
                source.Refresh();
                bottle.Refresh();
                _history.Add(new PourMove(source.Index, bottle.Index, amount));
                _moves++;
            }, () =>
            {
                _isBusy = false;
                _ui.SetBusy(false);
                _ui.UpdateMoves(_moves, _history.Count > 0);
                if (WaterSortRules.IsComplete(bottle.Colors))
                {
                    _audio.Play(Sound.Complete);
                    _particles.Burst(bottle.MouthPoint.position);
                }

                CheckWin();
            }, _stream, Palette(color));
        }

        private void CheckWin()
        {
            foreach (var bottle in _levels.Bottles)
                if (bottle.Colors.Count > 0 && !WaterSortRules.IsComplete(bottle.Colors))
                {
                    _ui.SetMessage(HasLegalMove()
                        ? "One color in every tube. You've got this."
                        : "No moves left. Undo a move or restart to try again.");
                    return;
                }

            _hasWon = true;
            _levels.Complete();
            _audio.Play(Sound.Win);
            _ui.ShowWin(_moves, _levels.CurrentLevelIndex == _levels.Database.levels.Count - 1);
        }

        private bool HasLegalMove()
        {
            foreach (var a in _levels.Bottles)
            foreach (var b in _levels.Bottles)
                if (a != b && WaterSortRules.PourCount(a.Colors, b.Colors) > 0)
                    return true;
            return false;
        }

        public void Undo()
        {
            if (_isBusy || _hasWon || _history.Count == 0) return;
            if (_selected != null) _selected.SetSelected(false);
            _selected = null;
            var move = _history[_history.Count - 1];
            _history.RemoveAt(_history.Count - 1);
            var source = _levels.Bottles[move.from];
            var target = _levels.Bottles[move.to];
            WaterSortRules.Apply(target.Colors, source.Colors, move.count);
            source.Refresh();
            target.Refresh();
            _particles.Clear();
            _moves--;
            _audio.Play(Sound.Click);
            _ui.UpdateMoves(_moves, _history.Count > 0);
            _ui.SetMessage("Move undone. Try a different route.");
        }

        public void Cancel()
        {
            if (_levels != null)
                foreach (var bottle in _levels.Bottles)
                    if (bottle != null)
                        bottle.CancelMotion();
            if (_stream != null) _stream.gameObject.SetActive(false);
            if (_particles != null) _particles.Clear();
            _selected = null;
            _isBusy = false;
            _hasWon = false;
            _moves = 0;
            _history.Clear();
        }

        public static Color Palette(WaterColor color)
        {
            switch (color)
            {
                case WaterColor.Blue: return new Color(.08f, .35f, 1);
                case WaterColor.Cyan: return Color.cyan;
                case WaterColor.Green: return new Color(.2f, .85f, .3f);
                case WaterColor.Orange: return new Color(1, .5f, .1f);
                case WaterColor.Pink: return new Color(1, .35f, .65f);
                case WaterColor.Purple: return new Color(.7f, .25f, .95f);
                case WaterColor.Red: return new Color(1, .15f, .2f);
                default: return Color.yellow;
            }
        }

        private void OnDestroy()
        {
            Cancel();
        }
    }
}