using System;

namespace BusJam
{
    // Observer Class which shows Popup UI, triggered when the Game Round is Finished (Win/Lose)
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
            {
                // The game already credited the reward; the HUD still shows the previous balance until the flight.
                int shownBalance = _game.Coins - result.Reward;
                _ui.Show(
                    result.IsLastLevel ? "ALL LEVELS COMPLETE" : "LEVEL COMPLETE", $"+{result.Reward}",
                    result.IsLastLevel ? "PLAY AGAIN" : "NEXT",
                    () => _ui.PlayRewardFlight(shownBalance, result.Reward, _game.AdvanceLevel),
                    isWin: true);
            }
            else
                _ui.Show("TRY AGAIN", result.FailureReason, "RETRY", _game.RetryLevel, isWin: false);
        }

        public void Dispose() => _game.RoundFinished -= OnRoundFinished;
    }
}