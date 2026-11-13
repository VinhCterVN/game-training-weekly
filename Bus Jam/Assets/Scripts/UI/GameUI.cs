using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BusJam
{
    // Composition and input binding only; game rules stay in BusJamGame/RoundState.
    public sealed class GameUI : MonoBehaviour
    {
        [SerializeField] private UIManager manager;
        [SerializeField] private HUDController hud;
        [SerializeField] private CoinFlyEffect coinEffect;
        [SerializeField] private TMP_Text title, body, primaryLabel, secondaryLabel;
        [SerializeField] private Button play, homeSettings, settings, restart, home, closePopup;
        [SerializeField] private Button primary, secondary, closeSettings;
        private Action _clickVfx;
        private Action _primaryAction, _secondaryAction;
        private GameAudio _audio;
        private bool _actionPending;
        public bool ModalVisible => manager.ModalVisible || _actionPending || coinEffect.IsPlaying;

        public void Initialize(Action onPlay, Action onSettings, Action retry, Action onHome, Action onClose, Action onCloseSettings, GameAudio gAudio, Action clickVfx)
        {
            _audio = gAudio;
            _clickVfx = clickVfx;
            Bind(play, () => manager.CloseHome(onPlay));
            Bind(homeSettings, onSettings);
            Bind(settings, onSettings);
            Bind(restart, retry);
            Bind(home, onHome);
            Bind(closePopup, onClose);
            Bind(closeSettings, onCloseSettings);
            Bind(primary, () => InvokeResult(_primaryAction));
            Bind(secondary, () => InvokeResult(_secondaryAction));
            hud.SetCoins(PlayerPrefs.GetInt("BusJam.Coins", 0));
        }
        private void Bind(Button button, Action action)
        {
            button.onClick.AddListener(() =>
            {
                if (_actionPending || coinEffect.IsPlaying) return;
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
            manager.CloseResult(() => { _actionPending = false; action?.Invoke(); });
        }
        public void Refresh(int level, RoundState round) => hud.Refresh(level, round.Remaining);
        public void ShowHome() => manager.ShowHome();
        public void ShowSettings() => manager.ShowSettings();
        public void CloseSettings(Action completed) => manager.CloseSettings(completed);
        public void Hide() => manager.CloseResult();
        public void ClaimReward(Action completed)
        {
            coinEffect.Play(hud.CoinTarget, amount =>
            {
                int coins = PlayerPrefs.GetInt("BusJam.Coins", 0) + amount;
                PlayerPrefs.SetInt("BusJam.Coins", coins);
                PlayerPrefs.Save();
                hud.SetCoins(coins, true);
                _audio.Play(Cue.Coin);
            }, completed);
        }
        public void Show(string xTitle, string xBody, string action, Action onPrimary, string second = null, Action onSecondary = null)
        {
            this.title.text = xTitle;
            this.body.text = xBody ?? "";
            this.body.gameObject.SetActive(!string.IsNullOrEmpty(xBody));
            primaryLabel.text = action;
            secondaryLabel.text = second ?? "";
            bool hasSecond = !string.IsNullOrEmpty(second);
            secondary.gameObject.SetActive(hasSecond);
            ((RectTransform)primary.transform).anchoredPosition = new Vector2(0, hasSecond ? -55 : -120);
            _primaryAction = onPrimary;
            _secondaryAction = onSecondary;
            manager.ShowResult();
        }
    }
}
