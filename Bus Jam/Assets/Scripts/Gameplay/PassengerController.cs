using DG.Tweening;
using UnityEngine;

namespace BusJam
{
    public class PassengerController : MonoBehaviour
    {
        public SpriteRenderer body;

        public void Configure(ColorID color, Vector3 position)
        {
            transform.position = position;
            transform.localScale = Vector3.one;
            body.color = GameCatalog.Tint(color);
        }

        public void ResetView()
        {
            transform.DOKill();
            gameObject.SetActive(false);
        }
    }
}