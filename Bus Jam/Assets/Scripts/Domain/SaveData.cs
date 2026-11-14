using UnityEngine;

namespace BusJam
{
    // Single owner of persisted player progress. UI never touches these; it is only handed values to display.
    public static class SaveData
    {
        private const string LevelKey = "BusJam.Level";
        private const string CoinsKey = "BusJam.Coins";

        public static int Level
        {
            get => PlayerPrefs.GetInt(LevelKey, 0);
            set { PlayerPrefs.SetInt(LevelKey, value); PlayerPrefs.Save(); }
        }

        public static int Coins
        {
            get => PlayerPrefs.GetInt(CoinsKey, 0);
            set { PlayerPrefs.SetInt(CoinsKey, value); PlayerPrefs.Save(); }
        }
    }
}
