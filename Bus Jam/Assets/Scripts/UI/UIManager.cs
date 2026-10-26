using System;
using UnityEngine;

namespace BusJam
{
    public sealed class UIManager : MonoBehaviour
    {
        [SerializeField] private PopupView _home;
        [SerializeField] private PopupView _result;
        [SerializeField] private PopupView _settings;
        public bool ModalVisible => _home.Visible || _result.Visible || _settings.Visible;
        public void ShowHome() => _home.Open();
        public void CloseHome(Action completed) => _home.Close(completed);
        public void ShowResult() => _result.Open();
        public void CloseResult(Action completed = null) => _result.Close(completed);
        public void ShowSettings() => _settings.Open();
        public void CloseSettings(Action completed) => _settings.Close(completed);
    }
}
