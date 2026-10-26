using System;
using UnityEngine;

namespace BusJam
{
    [Serializable]
    public class VehicleArt
    {
        public VehicleKind kind;
        public ColorID color;
        public Sprite[] closed = new Sprite[4];
        public Sprite[] seats = new Sprite[4];
    }
}