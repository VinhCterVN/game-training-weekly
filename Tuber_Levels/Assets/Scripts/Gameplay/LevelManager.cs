using System.Collections.Generic;
using UnityEngine;

namespace TuberLevels
{
    public sealed class LevelManager : MonoBehaviour
    {
        [SerializeField] private LevelDatabaseSO _database;
        [SerializeField] private ObjectPooler _pool;
        private readonly List<BottleController> _bottles = new List<BottleController>(10);
        private WaterSortController _sort;
        private UIManager _ui;
        private string _progressKey;
        public IReadOnlyList<BottleController> Bottles => _bottles;
        public LevelDatabaseSO Database => _database;
        public ObjectPooler Pool => _pool;
        public int CurrentLevelIndex { get; private set; }
        public int UnlockedLevel { get; private set; }

        public void Configure(LevelDatabaseSO database, ObjectPooler pool)
        {
            _database = database;
            _pool = pool;
        }

        public void Initialize(WaterSortController sort, UIManager ui)
        {
            _sort = sort;
            _ui = ui;
            bool smoke = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-lab-smoke") >= 0;
            _progressKey = smoke ? "TubeLevels.Test." : "TubeLevels.";
            _pool.Prewarm();
            UnlockedLevel = Mathf.Clamp(PlayerPrefs.GetInt(_progressKey + "Unlocked", 0), 0,
                _database.levels.Count - 1);
            Load(Mathf.Clamp(PlayerPrefs.GetInt(_progressKey + "Current", 0), 0, UnlockedLevel));
        }

        public void Load(int index)
        {
            if (index < 0 || index >= _database.levels.Count) return;
            var data = _database.levels[index];
            if (!data.Validate(out string error))
            {
                Debug.LogError(error);
                return;
            }

            _sort.Cancel();
            _ui.CloseOverlays();
            foreach (var bottle in _bottles) _pool.Release(bottle);
            _bottles.Clear();
            CurrentLevelIndex = index;
            for (int i = 0; i < data.bottles.Count; i++)
            {
                var bottle = _pool.Get();
                bottle.Init(data.bottles[i], i, _sort);
                _bottles.Add(bottle);
            }

            PlayerPrefs.SetInt(_progressKey + "Current", index);
            PlayerPrefs.Save();
            _ui.ShowLevel(index + 1, _database.levels.Count, data.displayName);
        }

        public void Complete()
        {
            PlayerPrefs.SetInt(_progressKey + "Complete." + CurrentLevelIndex, 1);
            UnlockedLevel = Mathf.Max(UnlockedLevel, Mathf.Min(CurrentLevelIndex + 1, _database.levels.Count - 1));
            PlayerPrefs.SetInt(_progressKey + "Unlocked", UnlockedLevel);
            PlayerPrefs.Save();
        }

        public bool IsCompleted(int index)
        {
            return PlayerPrefs.GetInt(_progressKey + "Complete." + index, 0) == 1;
        }

        public void Next()
        {
            Load((CurrentLevelIndex + 1) % _database.levels.Count);
        }

        public void Replay()
        {
            Load(CurrentLevelIndex);
        }

        public List<List<WaterColor>> Snapshot()
        {
            var state = new List<List<WaterColor>>();
            foreach (var bottle in _bottles) state.Add(new List<WaterColor>(bottle.Colors));
            return state;
        }
    }
}