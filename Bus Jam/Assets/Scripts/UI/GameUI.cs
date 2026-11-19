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
        [SerializeField] private TMP_Text homeCoins, shopCoins, homeLevelLabel;
        [SerializeField] private Button play, homeSettings, settings, restart, home, closePopup;
        [SerializeField] private Button primary, secondary, closeSettings;
        [SerializeField] private Button homeTab, shopTab, shopClose;

        [Header("Layout Settings")]
        [SerializeField] private float primaryYWithSecondary = -55f;
        [SerializeField] private float primaryYAlone = -120f;

        private Action _clickVfx;
        private Action _primaryAction, _secondaryAction;
        private GameAudio _audio;
        private bool _actionPending;

        public void Initialize(Action onPlay, Action onHomeSettings, Action onSettings, Action retry, Action onHome,
            Action onClose, GameAudio gAudio, Action clickVfx)
        {
            _audio = gAudio;
            _clickVfx = clickVfx;
            Bind(play, () => manager.ShowGameplay(onPlay));
            Bind(homeSettings, onHomeSettings);
            Bind(settings, onSettings);
            Bind(restart, retry);
            Bind(home, onHome);
            Bind(closePopup, onClose);
            Bind(closeSettings, onClose);
            Bind(homeTab, () => manager.ShowHome(instant: true));
            Bind(shopTab, () => manager.ShowShop(instant: true));
            Bind(shopClose, () => manager.ShowHome(instant: true));
            Bind(primary, () => InvokeResult(_primaryAction));
            Bind(secondary, () => InvokeResult(_secondaryAction));
        }

        private void Bind(Button button, Action action)
        {
            if (button == null) return;
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
            manager.CloseResult(() =>
            {
                _actionPending = false;
                action?.Invoke();
            });
        }

        public void Refresh(int level, RoundState round)
        {
            hud.Refresh(level, round.Remaining);
            SetHomeLevel(level);
        }

        public void SetHomeLevel(int level)
        {
            if (homeLevelLabel != null) homeLevelLabel.text = $"LEVEL {level + 1}";
        }

        public void ShowHome(bool instant = false) => manager.ShowHome(instant);
        public void ShowShop(bool instant = false) => manager.ShowShop(instant);
        public void ShowSettings(bool withOptions = false) => manager.ShowSettings(withOptions);
        public void CloseSettings(Action completed) => manager.CloseSettings(completed);
        public void Hide() => manager.CloseResult();

        public void SetCoins(int coins)
        {
            hud.SetCoins(coins);
            string formatted = coins.ToString("N0");
            if (homeCoins != null) homeCoins.text = formatted;
            if (shopCoins != null) shopCoins.text = formatted;
        }

        // Display only: the game already credited `reward`; this animates the shown balance from `from` to from + reward.
        public void PlayRewardFlight(int from, int reward, Action completed)
        {
            coinEffect.Play(hud.CoinTarget, (landed, total) =>
            {
                int shown = from + Mathf.RoundToInt(reward * (landed / (float)total));
                hud.SetCoins(shown, true);
                string formatted = shown.ToString("N0");
                if (homeCoins != null) homeCoins.text = formatted;
                if (shopCoins != null) shopCoins.text = formatted;
                _audio.Play(Cue.Coin);
            }, completed);
        }

        public void Show(string xTitle, string xBody, string action, Action onPrimary, string second = null,
            Action onSecondary = null, bool isWin = true)
        {
            title.text = xTitle;
            body.text = xBody ?? "";
            body.gameObject.SetActive(!string.IsNullOrEmpty(xBody));
            primaryLabel.text = action;
            secondaryLabel.text = second ?? "";
            bool hasSecond = !string.IsNullOrEmpty(second);
            secondary.gameObject.SetActive(hasSecond);
            ((RectTransform)primary.transform).anchoredPosition = new Vector2(0, hasSecond ? primaryYWithSecondary : primaryYAlone);
            _primaryAction = onPrimary;
            _secondaryAction = onSecondary;
            manager.ShowResult(isWin);
        }
    }
}