using System;
using System.Collections.Generic;
using System.Text;

namespace BusJam
{
    public enum VehiclePhase { Grid, Arriving, Waiting, Departing, Delivered }
    public enum DispatchResult { Accepted, Blocked, BaysFull, Unavailable }

    // Authoritative rules. Animation only acknowledges transitions; it never determines occupancy.
    public sealed class RoundState
    {
        public readonly LevelDataSO Level;
        public readonly VehiclePhase[] Phases;
        public readonly int[] Loads;
        public readonly int[] Bays;
        public int PassengerIndex { get; private set; }
        public int Delivered { get; private set; }
        public bool Won => Delivered == Phases.Length && PassengerIndex == Level.passengers.Count;
        public int Remaining => Level.passengers.Count - PassengerIndex;

        public RoundState(LevelDataSO level)
        {
            Level = level;
            Phases = new VehiclePhase[level.vehicles.Count];
            Loads = new int[Phases.Length];
            Bays = new int[level.bayCount];
            for (int i = 0; i < Bays.Length; i++) Bays[i] = -1;
        }

        private RoundState(RoundState other)
        {
            Level = other.Level;
            Phases = (VehiclePhase[])other.Phases.Clone();
            Loads = (int[])other.Loads.Clone();
            Bays = (int[])other.Bays.Clone();
            PassengerIndex = other.PassengerIndex;
            Delivered = other.Delivered;
        }

        public RoundState Copy() => new RoundState(this);

        public int[,] Occupancy()
        {
            var cells = new int[Level.columns, Level.rows];
            for (int x = 0; x < Level.columns; x++) for (int y = 0; y < Level.rows; y++) cells[x, y] = -1;
            for (int i = 0; i < Phases.Length; i++) if (Phases[i] == VehiclePhase.Grid)
            {
                var v = Level.vehicles[i];
                for (int x = v.x; x < v.x + v.Width; x++) for (int y = v.y; y < v.y + v.Height; y++) cells[x, y] = i;
            }
            return cells;
        }

        public bool CanEscape(int id)
        {
            if (id < 0 || id >= Phases.Length || Phases[id] != VehiclePhase.Grid) return false;
            var cells = Occupancy();
            var v = Level.vehicles[id];
            var step = v.Direction;
            int px = v.x, py = v.y;
            while (true)
            {
                px += step.x; py += step.y;
                if (px + v.Width <= 0 || py + v.Height <= 0 || px >= Level.columns || py >= Level.rows) return true;
                for (int x = px; x < px + v.Width; x++) for (int y = py; y < py + v.Height; y++)
                    if (x >= 0 && x < Level.columns && y >= 0 && y < Level.rows && cells[x, y] >= 0 && cells[x, y] != id) return false;
            }
        }

        public int BayOf(int id) => Array.IndexOf(Bays, id);

        public DispatchResult Dispatch(int id)
        {
            if (id < 0 || id >= Phases.Length || Phases[id] != VehiclePhase.Grid) return DispatchResult.Unavailable;
            if (!CanEscape(id)) return DispatchResult.Blocked;
            int slot = Array.IndexOf(Bays, -1);
            if (slot < 0) return DispatchResult.BaysFull;
            Bays[slot] = id; // Reserve before the first animation frame.
            Phases[id] = VehiclePhase.Arriving;
            return DispatchResult.Accepted;
        }

        public void Arrive(int id)
        {
            if (Phases[id] != VehiclePhase.Arriving) throw new InvalidOperationException("Only an arriving vehicle can park.");
            Phases[id] = VehiclePhase.Waiting;
        }

        public int BoardingTarget()
        {
            if (Remaining == 0) return -1;
            foreach (int id in Bays)
                if (id >= 0 && Phases[id] == VehiclePhase.Waiting && Level.vehicles[id].color == Level.passengers[PassengerIndex] && Loads[id] < Level.vehicles[id].Capacity) return id;
            return -1;
        }

        public void Board(int id)
        {
            if (BoardingTarget() != id) throw new InvalidOperationException("Only the first passenger may board a matching waiting vehicle.");
            Loads[id]++;
            PassengerIndex++;
            if (Loads[id] == Level.vehicles[id].Capacity) Phases[id] = VehiclePhase.Departing;
        }

        public void Depart(int id)
        {
            if (Phases[id] != VehiclePhase.Departing) throw new InvalidOperationException("Vehicle must be full before departure.");
            Bays[BayOf(id)] = -1;
            Phases[id] = VehiclePhase.Delivered;
            Delivered++;
        }

        public bool Deadlocked
        {
            get
            {
                if (Won || BoardingTarget() >= 0) return false;
                foreach (int id in Bays) if (id < 0 || Phases[id] != VehiclePhase.Waiting) return false;
                return true;
            }
        }

        public bool GridJammed
        {
            get
            {
                if (Won || BoardingTarget() >= 0) return false;
                for (int i = 0; i < Phases.Length; i++)
                    if (Phases[i] == VehiclePhase.Arriving || Phases[i] == VehiclePhase.Departing || CanEscape(i)) return false;
                return true;
            }
        }

        public void Settle()
        {
            int id;
            while ((id = BoardingTarget()) >= 0)
            {
                Board(id);
                if (Phases[id] == VehiclePhase.Departing) Depart(id);
            }
        }

        internal string Key()
        {
            var s = new StringBuilder().Append(PassengerIndex).Append(':');
            for (int i = 0; i < Phases.Length; i++) s.Append((int)Phases[i]).Append(',').Append(Loads[i]).Append(';');
            foreach (int id in Bays) s.Append(id).Append(',');
            return s.ToString();
        }
    }

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
