using DG.Tweening;
using TMPro;
using UnityEngine;

namespace BusJam
{
    public sealed class HUDController : MonoBehaviour
    {
        [SerializeField] private TMP_Text _level;
        [SerializeField] private TMP_Text _waiting;
        [SerializeField] private TMP_Text _coins;
        [SerializeField] private RectTransform _coinIcon;

        [Header("Animation Settings")]
        [SerializeField] private float iconPunchScale = 0.22f;
        [SerializeField] private float textPunchScale = 0.12f;
        [SerializeField] private float punchDuration = 0.2f;
        [SerializeField] private int punchVibrato = 2;

        public RectTransform CoinTarget => _coinIcon;
        public void Refresh(int level, int waiting)
        {
            _level.text = $"LEVEL {level + 1}";
            _waiting.text = $"{waiting}\nWAITING";
        }
        public void SetCoins(int coins, bool animate = false)
        {
            _coins.text = coins.ToString("N0");
            if (!animate) return;
            _coinIcon.DOKill();
            _coinIcon.localScale = Vector3.one;
            _coinIcon.DOPunchScale(Vector3.one * iconPunchScale, punchDuration, punchVibrato).SetUpdate(true).SetLink(gameObject);
            _coins.transform.DOKill();
            _coins.transform.localScale = Vector3.one;
            _coins.transform.DOPunchScale(Vector3.one * textPunchScale, punchDuration, punchVibrato).SetUpdate(true).SetLink(gameObject);
        }
    }
}
