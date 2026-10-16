using System.Collections.Generic;

namespace TuberLevels
{
    public static class WaterSortRules
    {
        public const int Capacity = 4;

        /// <summary>
        /// Calculates how many units of water can be poured from the source bottle to the target bottle.
        /// </summary>
        /// <param name="source">
        /// The source bottle's colors, represented as a read-only list of WaterColor.
        /// </param>
        /// <param name="target">
        /// The target bottle's colors, represented as a read-only list of WaterColor.
        /// </param>
        /// <returns>
        /// The number of units that can be poured from the source to the target.
        /// </returns>
        public static int PourCount(IReadOnlyList<WaterColor> source, IReadOnlyList<WaterColor> target)
        {
            if (ReferenceEquals(source, target) || source.Count == 0 || target.Count >= Capacity) return 0;
            WaterColor color = source[source.Count - 1];
            if (target.Count > 0 && target[target.Count - 1] != color) return 0;
            int run = 1;
            for (int i = source.Count - 2; i >= 0 && source[i] == color; i--) run++;
            return System.Math.Min(run, Capacity - target.Count);
        }

        /// <summary>
        /// Determines whether a bottle is complete (i.e., contains only one color and is at full capacity).
        /// </summary>
        /// <param name="colors">
        /// The bottle's colors, represented as a read-only list of WaterColor.
        /// </param>
        /// <returns>
        /// True if the bottle is complete; otherwise, false.
        /// </returns>
        public static bool IsComplete(IReadOnlyList<WaterColor> colors)
        {
            if (colors.Count != Capacity) return false;
            for (int i = 1; i < colors.Count; i++)
                if (colors[i] != colors[0])
                    return false;
            return true;
        }

        public static bool IsWon(List<List<WaterColor>> state)
        {
            foreach (var bottle in state)
                if (bottle.Count > 0 && !IsComplete(bottle))
                    return false;
            return state.Count > 0;
        }

        public static void Apply(List<WaterColor> source, List<WaterColor> target, int count)
        {
            WaterColor color = source[source.Count - 1];
            source.RemoveRange(source.Count - count, count);
            for (int i = 0; i < count; i++) target.Add(color);
        }
    }
}