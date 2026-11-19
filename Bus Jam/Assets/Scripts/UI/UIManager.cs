using System;
using UnityEngine;

namespace BusJam
{
    public sealed class UIManager : MonoBehaviour
    {
        [Header("Screens")]
        [SerializeField] private ScreenView _homeScreen;
        [SerializeField] private ScreenView _shopScreen;
        [SerializeField] private GameObject _gameplayHud;
        [SerializeField] private GameObject _bottomNavBar;
        [SerializeField] private RectTransform _navSelectionHighlight;
        [SerializeField] private RectTransform _homeTabAnchor;
        [SerializeField] private RectTransform _shopTabAnchor;

        [Header("Popups")]
        [SerializeField] private PopupView _result;
        [SerializeField] private PopupView _settings;

        public bool ModalVisible => (_result != null && _result.Visible) || (_settings != null && _settings.Visible);
        public bool MenuVisible => (_homeScreen != null && _homeScreen.Visible) || (_shopScreen != null && _shopScreen.Visible);

        public void ShowHome(bool instant = false)
        {
            if (_shopScreen != null) _shopScreen.Hide(instant: true);
            if (_gameplayHud != null) _gameplayHud.SetActive(false);
            if (_bottomNavBar != null) _bottomNavBar.SetActive(true);
            MoveNavHighlight(_homeTabAnchor);
            if (_homeScreen != null) _homeScreen.Show(instant);
        }

        public void ShowShop(bool instant = false)
        {
            if (_homeScreen != null) _homeScreen.Hide(instant: true);
            if (_gameplayHud != null) _gameplayHud.SetActive(false);
            if (_bottomNavBar != null) _bottomNavBar.SetActive(true);
            MoveNavHighlight(_shopTabAnchor);
            if (_shopScreen != null) _shopScreen.Show(instant);
        }

        public void ShowGameplay(Action completed = null)
        {
            if (_bottomNavBar != null) _bottomNavBar.SetActive(false);
            if (_shopScreen != null) _shopScreen.Hide(instant: true);
            if (_gameplayHud != null) _gameplayHud.SetActive(true);
            if (_homeScreen != null && _homeScreen.Visible)
                _homeScreen.Hide(completed);
            else
                completed?.Invoke();
        }

        public void ShowResult(bool isWin) => _result.Open(isWin: isWin);
        public void CloseResult(Action completed = null) => _result.Close(completed);
        public void ShowSettings(bool withOptions = false) => _settings.Open(withOptions);
        public void CloseSettings(Action completed = null) => _settings.Close(completed);

        private void MoveNavHighlight(RectTransform target)
        {
            if (_navSelectionHighlight == null || target == null) return;
            _navSelectionHighlight.anchoredPosition = new Vector2(target.anchoredPosition.x, _navSelectionHighlight.anchoredPosition.y);
        }
    }
}
