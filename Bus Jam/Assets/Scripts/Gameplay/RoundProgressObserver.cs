using System;
using UnityEngine;

namespace BusJam
{
    // Observer Class used to save/store Game Progress, triggered when game round is Win.
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
            SaveData.Level = result.NextLevelIndex;
        }

        public void Dispose() => _game.RoundFinished -= OnRoundFinished;
    }
}
