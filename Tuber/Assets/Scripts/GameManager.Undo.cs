using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    public partial class GameManager
    {
        private readonly Stack<(Tube source, Tube target, int amount)> _moveHistory = new();

        public void UndoLastMove()
        {
            if (_isBusy || _moveHistory.Count == 0)
                return;

            ClearSelection();
            var move = _moveHistory.Pop();
            move.source.ResetTransformForUndo();
            move.target.ResetTransformForUndo();
            move.source.UndoPourTo(move.target, move.amount);

            if (_winPopup != null)
                _winPopup.SetActive(false);

            _isBusy = false;
        }

        private void ClearSelection()
        {
            if (_selectedTube != null)
                _selectedTube.Deselect();
            _selectedTube = null;
        }
    }
}
