using System;
using UnityEngine;

namespace BusJam
{
    public sealed class RoundProgressObserver : IDisposable
    {
        private readonly BusJamGame _game;

        public RoundProgressObserver(BusJamGame game)
        {
            _game = game;
            _game.RoundFinished += OnRoundFinished;
        }

        private void OnRoundFinished(RoundFinishedResult result)
        {
            if (!result.Won) return;
            PlayerPrefs.SetInt("BusJam.Level", result.NextLevelIndex);
            PlayerPrefs.Save();
        }

        public void Dispose() => _game.RoundFinished -= OnRoundFinished;
    }
}
