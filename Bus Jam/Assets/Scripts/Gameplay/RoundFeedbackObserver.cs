using System;
using UnityEngine;

namespace BusJam
{
    public sealed class RoundFeedbackObserver : IDisposable
    {
        private readonly BusJamGame _game;
        private readonly GameAudio _audio;
        private readonly GamePools _pools;

        public RoundFeedbackObserver(BusJamGame game, GameAudio audio, GamePools pools)
        {
            _game = game;
            _audio = audio;
            _pools = pools;
            _game.RoundFinished += OnRoundFinished;
        }

        private void OnRoundFinished(RoundFinishedResult result)
        {
            _audio.Play(result.Won ? Cue.Win : Cue.Lose);
            HapticFeedback.Play(result.Won ? HapticCue.Success : HapticCue.Warning);
            if (!result.Won) return;
            for (int i = 0; i < 8; i++)
                _pools.Puff(new Vector3(-3.5f + i, 5, 0), GameCatalog.Tint((ColorID)(i % 4)), 22, true);
        }

        public void Dispose() => _game.RoundFinished -= OnRoundFinished;
    }
}
