using UnityEngine;
using UnityEngine.UI;

namespace TuberLevels
{
    public sealed class UIManager : MonoBehaviour
    {
        [SerializeField] private Text _level;
        [SerializeField] private Text _subtitle;
        [SerializeField] private Text _moves;
        [SerializeField] private Text _message;
        [SerializeField] private Button _undo;
        [SerializeField] private Button _restart;
        [SerializeField] private Button _home;
        [SerializeField] private Button _sound;
        [SerializeField] private Text _soundLabel;
        [SerializeField] private WinPopupUI _win;
        [SerializeField] private GameObject _levelPanel;
        [SerializeField] private Button _closeLevels;
        [SerializeField] private Button[] _levelButtons;
        [SerializeField] private Text[] _levelLabels;
        [SerializeField] private Text[] _levelStates;
        [SerializeField] private Image[] _progressDots;
        private LevelManager _levels;
        private WaterSortController _sort;
        private AudioManager _audio;
        public bool IsOverlayOpen => _win.gameObject.activeSelf || _levelPanel.activeSelf;
        public WinPopupUI Win => _win;

        public void Configure(Text level, Text subtitle, Text moves, Text message, Button undo, Button restart,
            Button home, Button sound, Text soundLabel, WinPopupUI win, GameObject levelPanel, Button close,
            Button[] levelButtons, Text[] labels, Text[] states, Image[] dots)
        {
            _level = level;
            _subtitle = subtitle;
            _moves = moves;
            _message = message;
            _undo = undo;
            _restart = restart;
            _home = home;
            _sound = sound;
            _soundLabel = soundLabel;
            _win = win;
            _levelPanel = levelPanel;
            _closeLevels = close;
            _levelButtons = levelButtons;
            _levelLabels = labels;
            _levelStates = states;
            _progressDots = dots;
        }

        public void Initialize(LevelManager levels, WaterSortController sort, AudioManager audio)
        {
            _levels = levels;
            _sort = sort;
            _audio = audio;
            _undo.onClick.AddListener(sort.Undo);
            _restart.onClick.AddListener(() =>
            {
                _audio.Play(Sound.Click);
                _levels.Replay();
            });
            _home.onClick.AddListener(OpenLevels);
            _sound.onClick.AddListener(() =>
            {
                _audio.ToggleMute();
                UpdateSound();
                _audio.Play(Sound.Click);
            });
            _closeLevels.onClick.AddListener(() =>
            {
                _audio.Play(Sound.Click);
                _levelPanel.SetActive(false);
            });
            _win.Initialize();
            _win.NextRequested += Next;
            _win.ReplayRequested += Replay;
            for (int i = 0; i < _levelButtons.Length; i++)
            {
                int index = i;
                _levelButtons[i].onClick.AddListener(() =>
                {
                    if (index <= _levels.UnlockedLevel)
                    {
                        _audio.Play(Sound.Click);
                        _levels.Load(index);
                    }
                });
            }

            UpdateSound();
        }

        private void Next()
        {
            _audio.Play(Sound.Click);
            _levels.Next();
        }

        private void Replay()
        {
            _audio.Play(Sound.Click);
            _levels.Replay();
        }

        private void UpdateSound()
        {
            _soundLabel.text = _audio.Muted ? "SOUND OFF" : "SOUND ON";
        }

        public void ShowLevel(int number, int total, string title)
        {
            _level.text = "LEVEL " + number.ToString("00");
            _subtitle.text = title;
            UpdateMoves(0, false);
            SetBusy(false);
            _undo.interactable = false;
            _message.text = "Tap a tube, then tap where you want to pour.";
            for (int i = 0; i < _progressDots.Length; i++)
                _progressDots[i].color = i < number ? new Color(.1f, .55f, .52f) : new Color(.1f, .55f, .52f, .18f);
        }

        public void UpdateMoves(int moves, bool canUndo)
        {
            _moves.text = moves.ToString("00") + "  MOVES";
            _undo.interactable = canUndo;
        }

        public void SetMessage(string message)
        {
            _message.text = message;
        }

        public void SetBusy(bool busy)
        {
            _restart.interactable = !busy;
            _home.interactable = !busy;
            _undo.interactable = !busy && _sort.Moves > 0;
        }

        public void ShowWin(int moves, bool final)
        {
            _win.Show(moves, final);
        }

        public void CloseOverlays()
        {
            _win.Hide();
            _levelPanel.SetActive(false);
        }

        public void OpenLevels()
        {
            if (_sort.IsBusy) return;
            _audio.Play(Sound.Click);
            for (int i = 0; i < _levelButtons.Length; i++)
            {
                bool unlocked = i <= _levels.UnlockedLevel;
                _levelButtons[i].interactable = unlocked;
                _levelLabels[i].text = (i + 1).ToString("00");
                _levelStates[i].text = !unlocked ? "LOCKED" : _levels.IsCompleted(i) ? "COMPLETE" : "PLAY";
            }

            _levelPanel.SetActive(true);
        }

        private void OnDestroy()
        {
            if (_win != null)
            {
                _win.NextRequested -= Next;
                _win.ReplayRequested -= Replay;
            }
        }
    }
}