namespace BusJam
{
    // Immutable snapshot: observers do not need to read the live RoundState.
    public readonly struct RoundFinishedResult
    {
        public bool Won { get; }
        public int LevelIndex { get; }
        public int NextLevelIndex { get; }
        public bool IsLastLevel { get; }
        public string FailureReason { get; }

        public RoundFinishedResult(bool won, int levelIndex, int levelCount, string failureReason)
        {
            Won = won;
            LevelIndex = levelIndex;
            IsLastLevel = levelIndex == levelCount - 1;
            NextLevelIndex = IsLastLevel ? levelIndex : levelIndex + 1;
            FailureReason = failureReason;
        }
    }
}
