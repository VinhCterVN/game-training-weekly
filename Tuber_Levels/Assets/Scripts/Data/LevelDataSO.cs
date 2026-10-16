using System;
using System.Collections.Generic;
using UnityEngine;

namespace TuberLevels
{
    public enum WaterColor
    {
        Blue,
        Cyan,
        Green,
        Orange,
        Pink,
        Purple,
        Red,
        Yellow
    }

    [Serializable]
    public sealed class BottleData
    {
        [Tooltip("Bottom to top. Capacity is four.")]
        public List<WaterColor> colors = new List<WaterColor>();

        public Vector2 position;
    }

    [CreateAssetMenu(menuName = "Tube Levels/Level")]
    public sealed class LevelDataSO : ScriptableObject
    {
        public int levelIndex = 1;
        public string displayName = "First drops";
        [Range(3, 8)] public int colorCount = 3;
        [Range(1, 2)] public int emptyBottles = 1;
        public int seed = 101;
        public List<BottleData> bottles = new List<BottleData>();

        public List<List<WaterColor>> CreateState()
        {
            var state = new List<List<WaterColor>>(bottles.Count);
            foreach (var bottle in bottles) state.Add(new List<WaterColor>(bottle.colors));
            return state;
        }

        public bool Validate(out string message)
        {
            var counts = new int[8];
            int empty = 0, complete = 0;
            if (bottles == null || bottles.Count < 3 || bottles.Count > 10)
            {
                message = "Use 3 to 10 bottles.";
                return false;
            }

            foreach (var bottle in bottles)
            {
                if (bottle == null || bottle.colors == null || bottle.colors.Count > 4)
                {
                    message = "Every bottle needs a color list with at most four units.";
                    return false;
                }

                if (bottle.colors.Count == 0) empty++;
                if (WaterSortRules.IsComplete(bottle.colors)) complete++;
                foreach (var color in bottle.colors)
                {
                    if ((int)color < 0 || (int)color >= counts.Length)
                    {
                        message = "Unknown color.";
                        return false;
                    }

                    counts[(int)color]++;
                }
            }

            int used = 0;
            foreach (int count in counts)
            {
                if (count > 0) used++;
                if (count != 0 && count != 4)
                {
                    message = "Each used color must have exactly four units.";
                    return false;
                }
            }

            if (used < 2 || empty < 1 || empty > 2)
            {
                message = "Use at least two colors and one or two empty bottles.";
                return false;
            }

            message = complete > 0
                ? "Valid. Warning: some bottles start completed."
                : "Valid color counts and empty bottles.";
            return true;
        }
    }
}