using DG.Tweening;
using UnityEngine;

namespace BusJam
{
    public class BusController : MonoBehaviour
    {
        public SpriteRenderer body;
        public BoxCollider2D hitbox;
        public Transform head, tail;
        public Transform[] seats;
        public TextMesh count;
        public int Id { get; private set; }
        public VehiclePlacement Placement { get; private set; }
        public Facing Direction { get; private set; }
        [Header("Parking Settings")]
        [SerializeField] public float parkCarCell = 0.85f;
        [SerializeField] public float parkBusCell = 0.68f;

        [Header("Geometry & Layout")]
        [SerializeField] private float spriteMarginScale = 0.9f;
        [SerializeField] private float hitboxScale = 0.94f;
        [SerializeField] private float headOffsetFactor = 0.45f;
        [SerializeField] private float seatXOffset = 0.19f;
        [SerializeField] private float seatBaseY = 0.25f;
        [SerializeField] private float seatRowSpacing = 0.43f;
        [SerializeField] private float countYOffset = 0.14f;

        private GameCatalog _catalog;
        private float _cell;
        private Material _defaultMaterial;

        public void Configure(int id, VehiclePlacement placement, GameCatalog art, float cellSize, Vector3 position)
        {
            Id = id;
            Placement = placement;
            _catalog = art;
            _cell = cellSize;
            if (_defaultMaterial == null) _defaultMaterial = body.sharedMaterial;
            body.sharedMaterial = placement.color == ColorID.Green ? _catalog.greenVehicleMaterial : _defaultMaterial;
            transform.position = position;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
            body.transform.localPosition = Vector3.zero;
            count.gameObject.SetActive(false);
            SetArt(placement.facing, false);
            hitbox.enabled = true;
        }

        public void SetArt(Facing facing, bool open)
        {
            Direction = facing;
            body.sprite = _catalog.Vehicle(Placement.kind, Placement.color, facing, open);
            bool horizontal = facing == Facing.Left || facing == Facing.Right;
            float width = horizontal ? Placement.Length * _cell : _cell;
            float height = horizontal ? _cell : Placement.Length * _cell;
            // Sprite vertices exclude transparent margins in the supplied illustrations.
            var bounds = VisibleBounds(body.sprite);
            var scale = new Vector3(width * spriteMarginScale / bounds.size.x, height * spriteMarginScale / bounds.size.y, 1);
            body.transform.localScale = scale;
            body.transform.localPosition = -Vector3.Scale(bounds.center, scale);
            hitbox.size = new Vector2(width * hitboxScale, height * hitboxScale);
            var direction = facing == Facing.Up ? Vector3.up :
                facing == Facing.Down ? Vector3.down :
                facing == Facing.Left ? Vector3.left : Vector3.right;
            head.localPosition = direction * Placement.Length * _cell * headOffsetFactor;
            tail.localPosition = -head.localPosition;
            for (int i = 0; i < seats.Length; i++)
                seats[i].localPosition =
                    new Vector3((i % 2 == 0 ? -seatXOffset : seatXOffset) * _cell, (seatBaseY + (i / 2) * seatRowSpacing) * _cell, 0);
            count.transform.localPosition = new Vector3(0, -height * .5f - countYOffset, -.1f);
        }

        public void Park(float tiltAngle = 0f)
        {
            _cell = Placement.kind == VehicleKind.Car ? parkCarCell : parkBusCell;
            transform.rotation = Quaternion.Euler(0f, 0f, tiltAngle);
            SetArt(Facing.Up, true);
            count.gameObject.SetActive(true);
            count.transform.rotation = Quaternion.identity;
            SetCount(0);
        }

        public void SetCount(int loaded) => count.text = $"{loaded}/{Placement.Capacity}";

        public void CloseRoof()
        {
            transform.rotation = Quaternion.identity;
            SetArt(Facing.Right, false);
        }

        public void ResetView()
        {
            transform.DOKill();
            body.transform.DOKill();
            hitbox.enabled = false;
            gameObject.SetActive(false);
        }

        public static Bounds VisibleBounds(Sprite sprite)
        {
            if (sprite == null) return new Bounds(Vector3.zero, Vector3.one);
            var vertices = sprite.vertices;
            var b = new Bounds(vertices[0], Vector3.zero);
            for (int i = 1; i < vertices.Length; i++) b.Encapsulate(vertices[i]);
            return b;
        }
    }
}