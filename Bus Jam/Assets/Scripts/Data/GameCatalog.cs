using UnityEngine;

namespace BusJam
{
    public class GameCatalog : ScriptableObject
    {
        public LevelDataSO[] levels;
        public VehicleArt[] vehicles;
        // public Sprite background, passenger, exhaust;
        // public Font font;
        public Material greenVehicleMaterial;
        public BusController busPrefab;
        public PassengerController passengerPrefab;
        public ParticleSystem particlePrefab;

        [Header("Color Tints")]
        [SerializeField] private Color redColor = new Color(1f, .24f, .28f);
        [SerializeField] private Color yellowColor = new Color(1f, .8f, .13f);
        [SerializeField] private Color greenColor = new Color(.2f, .83f, .4f);
        [SerializeField] private Color blueColor = new Color(.16f, .55f, 1f);

        private static GameCatalog _active;
        private void OnEnable() => _active = this;

        public Sprite Vehicle(VehicleKind kind, ColorID color, Facing facing, bool open)
        {
            foreach (var art in vehicles)
                if (art.kind == kind && art.color == color)
                    return (open ? art.seats : art.closed)[(int)facing];
            return null;
        }

        public Color GetTint(ColorID color) => color switch
        {
            ColorID.Red => redColor,
            ColorID.Yellow => yellowColor,
            ColorID.Green => greenColor,
            _ => blueColor
        };

        public static Color Tint(ColorID color)
        {
            if (_active != null) return _active.GetTint(color);
            return color switch
            {
                ColorID.Red => new Color(1f, .24f, .28f),
                ColorID.Yellow => new Color(1f, .8f, .13f),
                ColorID.Green => new Color(.2f, .83f, .4f),
                _ => new Color(.16f, .55f, 1f)
            };
        }
    }
}