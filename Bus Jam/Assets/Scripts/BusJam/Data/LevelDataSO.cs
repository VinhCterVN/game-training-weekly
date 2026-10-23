using System;
using System.Collections.Generic;
using UnityEngine;

namespace BusJam
{
    [CreateAssetMenu(menuName = "Bus Jam/Level", fileName = "Level")]
    public class LevelDataSO : ScriptableObject
    {
        public string title = "A fresh start";
        [TextArea] public string hint = "Tap a vehicle with a clear path. Match the first passenger's color.";
        [Range(4, 12)] public int columns = 6;
        [Range(4, 12)] public int rows = 6;
        [Range(2, 3)] public int bayCount = 2;
        public List<VehiclePlacement> vehicles = new List<VehiclePlacement>();
        public List<ColorID> passengers = new List<ColorID>();

        public List<string> Validate(bool requirePassengerBalance = true)
        {
            var issues = new List<string>();
            if (columns < 4 || columns > 12 || rows < 4 || rows > 12)
                issues.Add("Board dimensions must be 4–12 cells.");
            if (bayCount < 2 || bayCount > 3) issues.Add("Choose 2 or 3 waiting bays.");
            if (vehicles == null || vehicles.Count == 0)
            {
                issues.Add("Place at least one vehicle.");
                return issues;
            }

            if (vehicles.Count > 40) issues.Add("Maximum 40 vehicles per level.");
            var occupied = new HashSet<Vector2Int>();
            var seats = new int[4];
            for (int i = 0; i < vehicles.Count; i++)
            {
                var v = vehicles[i];
                if (v == null)
                {
                    issues.Add($"Vehicle {i + 1} is missing.");
                    continue;
                }

                if (!Enum.IsDefined(typeof(ColorID), v.color) || !Enum.IsDefined(typeof(Facing), v.facing) ||
                    !Enum.IsDefined(typeof(VehicleKind), v.kind))
                {
                    issues.Add($"Vehicle {i + 1} has an invalid enum.");
                    continue;
                }

                seats[(int)v.color] += v.Capacity;
                if (v.x < 0 || v.y < 0 || v.x + v.Width > columns || v.y + v.Height > rows)
                    issues.Add($"Vehicle {i + 1} extends outside the board.");
                for (int x = v.x; x < v.x + v.Width; x++)
                for (int y = v.y; y < v.y + v.Height; y++)
                    if (!occupied.Add(new Vector2Int(x, y)))
                        issues.Add($"Vehicle {i + 1} overlaps cell ({x}, {y}).");
            }

            if (!requirePassengerBalance) return issues;
            var counts = new int[4];
            if (passengers != null)
                foreach (var p in passengers)
                {
                    if (Enum.IsDefined(typeof(ColorID), p)) counts[(int)p]++;
                    else issues.Add("Passenger has an invalid color.");
                }

            for (int i = 0; i < 4; i++)
                if (counts[i] != seats[i])
                    issues.Add($"{(ColorID)i}: {counts[i]} passengers for {seats[i]} seats. These must match.");
            return issues;
        }
    }
}