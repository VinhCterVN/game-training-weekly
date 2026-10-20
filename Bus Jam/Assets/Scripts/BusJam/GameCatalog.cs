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

    public class GameCatalog : ScriptableObject
    {
        public LevelDataSO[] levels;
        public VehicleArt[] vehicles;
        public Sprite background, passenger, slot, exhaust, star, winRays;
        public Sprite button, primaryButton, dialogFrame;
        public Font font;
        public Material greenVehicleMaterial;
        public BusController busPrefab;
        public PassengerController passengerPrefab;
        public ParticleSystem particlePrefab;

        public Sprite Vehicle(VehicleKind kind, ColorID color, Facing facing, bool open)
        {
            foreach (var art in vehicles) if (art.kind == kind && art.color == color)
                return (open ? art.seats : art.closed)[(int)facing];
            return null;
        }

        public static Color Tint(ColorID color)
        {
            switch (color)
            {
                case ColorID.Red: return new Color(1f, .24f, .28f);
                case ColorID.Yellow: return new Color(1f, .8f, .13f);
                case ColorID.Green: return new Color(.2f, .83f, .4f);
                default: return new Color(.16f, .55f, 1f);
            }
        }
    }
}
