using System;
using System.Linq;
using DG.Tweening;

namespace DefaultNamespace
{
    public partial class Tube
    {
        public void UndoPourTo(Tube target, int amount)
        {
            for (var i = 0; i < amount; i++)
            {
                if (target._balls.Count == 0 || _balls.Count >= ballSlots.Length)
                    throw new InvalidOperationException("Cannot undo the recorded pour with the current tube contents.");

                var ballToRestore = target._balls.Last();
                target._balls.RemoveAt(target._balls.Count - 1);

                var sourceSlotIndex = _balls.Count;
                var restoredBall = _slotBalls[sourceSlotIndex];
                restoredBall.Setup(ballToRestore.Color, GetSprite(ballToRestore.Color, sourceSlotIndex == 0));
                restoredBall.SetVisible(true);
                _balls.Add(restoredBall);

                ballToRestore.SetVisible(false);
            }

            RefreshBallSprites();
            target.RefreshBallSprites();
        }

        public void ResetTransformForUndo()
        {
            transform.DOKill();
            _isSelected = false;
            transform.SetPositionAndRotation(_originalPosition, _originalRotation);
            transform.localScale = _originalScale;
        }

        private void RefreshBallSprites()
        {
            for (var i = 0; i < _balls.Count; i++)
                _balls[i].SetSprite(GetSprite(_balls[i].Color, i == 0));
        }
    }
}
