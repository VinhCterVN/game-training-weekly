using System.Collections.Generic;

namespace BusJam
{
    public static class LevelSolver
    {
        public static List<int> Solve(RoundState start, int maxNodes = 150000)
        {
            var visited = new HashSet<string>();
            var path = new List<int>();
            int budget = maxNodes;
            return Search(start, visited, path, ref budget) ? path : null;
        }

        private static bool Search(RoundState state, HashSet<string> visited, List<int> path, ref int budget)
        {
            if (state.Won) return true;
            if (--budget < 0 || state.Deadlocked || state.GridJammed || !visited.Add(state.Key())) return false;
            for (int id = 0; id < state.Phases.Length; id++)
            {
                if (!state.CanEscape(id)) continue;
                var next = state.Copy();
                if (next.Dispatch(id) != DispatchResult.Accepted) continue;
                next.Arrive(id);
                next.Settle();
                path.Add(id);
                if (Search(next, visited, path, ref budget)) return true;
                path.RemoveAt(path.Count - 1);
            }

            return false;
        }
    }
}