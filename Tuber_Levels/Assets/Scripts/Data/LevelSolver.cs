using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TuberLevels
{
    public struct PourMove
    {
        public int from, to, count;

        public PourMove(int source, int target, int amount)
        {
            from = source;
            to = target;
            count = amount;
        }
    }
    
    public static class LevelSolver
    {
        public static bool Solve(List<List<WaterColor>> input, out List<PourMove> solution, int budget = 150000)
        {
            var state = new List<List<WaterColor>>();
            foreach (var bottle in input) state.Add(new List<WaterColor>(bottle));
            solution = new List<PourMove>();
            return Search(state, solution, new HashSet<string>(), ref budget, 0);
        }

        private static bool Search(List<List<WaterColor>> state, List<PourMove> path, HashSet<string> visited,
            ref int budget, int depth)
        {
            if (WaterSortRules.IsWon(state)) return true;
            if (--budget <= 0 || depth >= 150) return false;
            var keys = new string[state.Count];
            for (int i = 0; i < state.Count; i++)
            {
                var key = new StringBuilder();
                foreach (var c in state[i]) key.Append((char)('A' + (int)c));
                keys[i] = key.ToString();
            }

            Array.Sort(keys, StringComparer.Ordinal);
            if (!visited.Add(string.Join("|", keys))) return false;
            for (int pass = 0; pass < 2; pass++)
            for (int from = 0; from < state.Count; from++)
            {
                if (state[from].Count == 0 || WaterSortRules.IsComplete(state[from])) continue;
                bool triedEmpty = false;
                for (int to = 0; to < state.Count; to++)
                {
                    if (from == to || (state[to].Count == 0) != (pass == 1)) continue;
                    if (state[to].Count == 0)
                    {
                        if (triedEmpty) continue;
                        triedEmpty = true;
                        bool uniform = true;
                        foreach (var c in state[from])
                            if (c != state[from][0])
                                uniform = false;
                        if (uniform) continue;
                    }

                    int amount = WaterSortRules.PourCount(state[from], state[to]);
                    if (amount == 0) continue;
                    WaterSortRules.Apply(state[from], state[to], amount);
                    path.Add(new PourMove(from, to, amount));
                    if (Search(state, path, visited, ref budget, depth + 1)) return true;
                    path.RemoveAt(path.Count - 1);
                    WaterSortRules.Apply(state[to], state[from], amount);
                }
            }

            return false;
        }

        public static void Generate(LevelDataSO level)
        {
            var random = new System.Random(level.seed);
            for (int attempt = 0; attempt < 200; attempt++)
            {
                var units = new List<WaterColor>();
                for (int c = 0; c < level.colorCount; c++)
                for (int n = 0; n < 4; n++)
                    units.Add((WaterColor)c);
                for (int i = units.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    var temp = units[i];
                    units[i] = units[j];
                    units[j] = temp;
                }

                var bottles = new List<BottleData>();
                for (int i = 0; i < level.colorCount + level.emptyBottles; i++)
                {
                    var bottle = new BottleData();
                    if (i < level.colorCount) bottle.colors.AddRange(units.GetRange(i * 4, 4));
                    bottles.Add(bottle);
                }

                bool completed = false;
                foreach (var b in bottles)
                    if (WaterSortRules.IsComplete(b.colors))
                        completed = true;
                if (completed) continue;
                var state = new List<List<WaterColor>>();
                foreach (var b in bottles) state.Add(b.colors);
                if (!Solve(state, out var solution, 30000) || solution.Count < level.colorCount + 2) continue;
                level.bottles = bottles;
                Layout(level);
                return;
            }

            throw new InvalidOperationException("No solvable layout found. Change the seed or add an empty bottle.");
        }

        public static void Layout(LevelDataSO level)
        {
            int count = level.bottles.Count;
            int columns = count <= 4 ? count : (count + 1) / 2;
            float spacing = count > 8 ? 128 : 145;
            for (int i = 0; i < count; i++)
            {
                int row = i / columns;
                int rowCount = Math.Min(columns, count - row * columns);
                level.bottles[i].position = new Vector2((i % columns - (rowCount - 1) * .5f) * spacing,
                    count <= 4 ? 0 : 165 - row * 330);
            }
        }
    }
}