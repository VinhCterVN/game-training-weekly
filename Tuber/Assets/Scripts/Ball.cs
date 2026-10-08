using UnityEngine;

namespace DefaultNamespace
{
    public class Ball : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        public TubeColor Color { get; private set; }
        public int SortingOrder
        {
            get => spriteRenderer.sortingOrder;
            set => spriteRenderer.sortingOrder = value;
        }

        public void Setup(TubeColor color, Sprite sprite)
        {
            Color = color;
            spriteRenderer.sprite = sprite;
        }

        public void SetSprite(Sprite sprite)
        {
            spriteRenderer.sprite = sprite;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}