using System;
using UnityEngine;

namespace BusJam
{
    [Serializable]
    public class VehiclePlacement
    {
        public VehicleKind kind;
        public ColorID color;
        public Facing facing;
        public int x, y;
        public int Capacity => kind == VehicleKind.Car ? 4 : 6;
        public int Length => kind == VehicleKind.Car ? 2 : 3;
        public int Width => facing == Facing.Left || facing == Facing.Right ? Length : 1;
        public int Height => facing == Facing.Left || facing == Facing.Right ? 1 : Length;

        public Vector2Int Direction => facing == Facing.Up ? Vector2Int.up :
            facing == Facing.Down ? Vector2Int.down :
            facing == Facing.Right ? Vector2Int.right : Vector2Int.left;

        public VehiclePlacement Copy() => (VehiclePlacement)MemberwiseClone();
    }
}