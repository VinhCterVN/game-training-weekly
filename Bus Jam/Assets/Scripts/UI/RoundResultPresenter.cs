using System;

namespace BusJam
{
    public sealed class RoundResultPresenter : IDisposable
    {
        private readonly BusJamGame _game;
        private readonly GameUI _ui;

        public RoundResultPresenter(BusJamGame game, GameUI ui)
        {
            _game = game;
            _ui = ui;
            _game.RoundFinished += OnRoundFinished;
        }

        private void OnRoundFinished(RoundFinishedResult result)
        {
            if (result.Won)
                _ui.Show(result.IsLastLevel ? "ALL LEVELS COMPLETE" : "LEVEL COMPLETE", "+100",
                    result.IsLastLevel ? "PLAY AGAIN" : "NEXT",
                    () => _ui.ClaimReward(() => _game.LoadLevel(result.IsLastLevel ? 0 : result.NextLevelIndex)));
            else
                _ui.Show("TRY AGAIN", result.FailureReason, "RETRY", () => _game.LoadLevel(result.LevelIndex));
        }

        public void Dispose() => _game.RoundFinished -= OnRoundFinished;
    }
}
