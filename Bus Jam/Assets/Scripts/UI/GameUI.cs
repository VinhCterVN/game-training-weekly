using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BusJam
{
    // Composition and input binding only; game rules stay in BusJamGame/RoundState.
    public sealed class GameUI : MonoBehaviour
    {
        [SerializeField] private UIManager _manager;
        [SerializeField] private HUDController _hud;
        [SerializeField] private CoinFlyEffect _coinEffect;
        [SerializeField] private TMP_Text _title, _body, _primaryLabel, _secondaryLabel;
        [SerializeField] private Button _play, _homeSettings, _settings, _restart, _home;
        [SerializeField] private Button _primary, _secondary, _closeSettings;
        private Action _clickVfx;
        private Action _primaryAction, _secondaryAction;
        private GameAudio _audio;
        private bool _actionPending;
        public bool ModalVisible => _manager.ModalVisible || _actionPending || _coinEffect.IsPlaying;

        public void Initialize(Action play, Action settings, Action retry, Action home, Action closeSettings, GameAudio audio, Action clickVfx)
        {
            _audio = audio;
            _clickVfx = clickVfx;
            Bind(_play, () => _manager.CloseHome(play));
            Bind(_homeSettings, settings);
            Bind(_settings, settings);
            Bind(_restart, retry);
            Bind(_home, home);
            Bind(_closeSettings, closeSettings);
            Bind(_primary, () => InvokeResult(_primaryAction));
            Bind(_secondary, () => InvokeResult(_secondaryAction));
            _hud.SetCoins(PlayerPrefs.GetInt("BusJam.Coins", 0));
        }
        private void Bind(Button button, Action action)
        {
            button.onClick.AddListener(() =>
            {
                if (_actionPending || _coinEffect.IsPlaying) return;
                ClickFeedback();
                action?.Invoke();
            });
        }
        public void ClickFeedback()
        {
            _audio.Play(Cue.Tap);
            HapticFeedback.Play(HapticCue.Light);
            _clickVfx?.Invoke();
        }
        private void InvokeResult(Action action)
        {
            _actionPending = true;
            _manager.CloseResult(() => { _actionPending = false; action?.Invoke(); });
        }
        public void Refresh(int level, RoundState round) => _hud.Refresh(level, round.Remaining);
        public void ShowHome() => _manager.ShowHome();
        public void ShowSettings() => _manager.ShowSettings();
        public void CloseSettings(Action completed) => _manager.CloseSettings(completed);
        public void Hide() => _manager.CloseResult();
        public void ClaimReward(Action completed)
        {
            _coinEffect.Play(_hud.CoinTarget, amount =>
            {
                int coins = PlayerPrefs.GetInt("BusJam.Coins", 0) + amount;
                PlayerPrefs.SetInt("BusJam.Coins", coins);
                PlayerPrefs.Save();
                _hud.SetCoins(coins, true);
                _audio.Play(Cue.Coin);
            }, completed);
        }
        public void Show(string title, string body, string action, Action onPrimary, string second = null, Action onSecondary = null)
        {
            _title.text = title;
            _body.text = body ?? "";
            _body.gameObject.SetActive(!string.IsNullOrEmpty(body));
            _primaryLabel.text = action;
            _secondaryLabel.text = second ?? "";
            bool hasSecond = !string.IsNullOrEmpty(second);
            _secondary.gameObject.SetActive(hasSecond);
            ((RectTransform)_primary.transform).anchoredPosition = new Vector2(0, hasSecond ? -55 : -120);
            _primaryAction = onPrimary;
            _secondaryAction = onSecondary;
            _manager.ShowResult();
        }
    }
}
