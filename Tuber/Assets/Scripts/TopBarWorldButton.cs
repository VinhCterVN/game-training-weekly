using UnityEngine;

namespace DefaultNamespace
{
    public sealed class TopBarWorldButton : MonoBehaviour
    {
        private GameManager _gameManager;
        private bool _undo;

        public void Initialize(GameManager gameManager, bool undo)
        {
            _gameManager = gameManager;
            _undo = undo;
        }

        private void OnMouseUpAsButton()
        {
            if (_gameManager == null)
                return;

            if (_undo)
                _gameManager.UndoLastMove();
            else
                _gameManager.PlayAgain();
        }
    }
}
