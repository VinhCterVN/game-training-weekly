using DG.Tweening;
using UnityEngine;

namespace DefaultNamespace
{
    public partial class GameManager : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip selectSound;
        [SerializeField] private AudioClip errorSound;
        [SerializeField] private AudioClip winningSound;

        private Tube _selectedTube;
        private bool _isBusy;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            CreateWinPopup();
        }

        public void OnTubeClicked(Tube tube)
        {
            if (_isBusy || tube == null || tube.IsComplete) return;

            if (_selectedTube == null)
            {
                if (tube.IsEmpty) return;
                _selectedTube = tube;
                _selectedTube.Select();
                if (audioSource != null && selectSound != null)
                    audioSource.PlayOneShot(selectSound);
                return;
            }

            if (_selectedTube == tube)
            {
                _selectedTube.Deselect();
                _selectedTube = null;
                return;
            }

            var sourceTube = _selectedTube;
            if (sourceTube.CanPourInto(tube))
            {
                _isBusy = true;
                var pourAmount = sourceTube.GetPourAmount(tube);
                if (pourAmount <= 0)
                {
                    sourceTube.Deselect();
                    _selectedTube = null;
                    _isBusy = false;
                    return;
                }

                var sequence = sourceTube.PourTo(tube, pourAmount);
                sequence.OnComplete(() =>
                {
                    _moveHistory.Push((sourceTube, tube, pourAmount));
                    _selectedTube = null;
                    if (HasWon())
                    {
                        _isBusy = true;
                        if (audioSource != null && winningSound != null)
                            audioSource.PlayOneShot(winningSound);
                        _winPopup.SetActive(true);
                    }
                    else
                    {
                        _isBusy = false;
                    }
                });
            }
            else if (!tube.IsEmpty)
            {
                if (audioSource != null && errorSound != null)
                    audioSource.PlayOneShot(errorSound);
                sourceTube.HandleTubeShake();
                sourceTube.Deselect();
                _selectedTube = null;
            }
        }

        private bool HasWon()
        {
            var tubes = FindObjectsOfType<Tube>();
            if (tubes.Length < 4)
            {
                Debug.LogWarning($"Win check expected at least 4 tubes but found {tubes.Length}.", this);
                return false;
            }

            foreach (var tube in tubes)
            {
                if (tube == null || (!tube.IsEmpty && !tube.IsComplete))
                {
                    if (tube == null)
                        Debug.LogWarning("Win check found a missing tube reference.", this);
                    else
                        Debug.Log(
                            $"Win check: {tube.name} is not solved (empty: {tube.IsEmpty}, complete: {tube.IsComplete}).",
                            tube);
                    return false;
                }
            }

            Debug.Log("Win condition met: all tubes are empty or contain a matched pair.", this);
            return true;
        }

    }
}