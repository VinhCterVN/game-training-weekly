using UnityEngine;
using UnityEngine.UI;

namespace TuberLevels
{
    // Canvas particles are preallocated, so bursts also work in screen-space UI.
    public sealed class CompletionParticles : MonoBehaviour
    {
        [SerializeField] private Image[] _particles;
        private Vector2[] _velocities;
        private float _remaining;

        public void Configure(Image[] particles)
        {
            _particles = particles;
        }

        private void Awake()
        {
            _velocities = new Vector2[_particles.Length];
            Clear();
        }

        public void Burst(Vector3 worldPosition)
        {
            _remaining = 1.1f;
            for (int i = 0; i < _particles.Length; i++)
            {
                var particle = _particles[i];
                particle.transform.position = worldPosition;
                particle.color = WaterSortController.Palette((WaterColor)(i % 8));
                particle.gameObject.SetActive(true);
                float angle = (25 + i * 137.5f % 130) * Mathf.Deg2Rad;
                _velocities[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (90 + i % 5 * 28);
            }
        }

        private void Update()
        {
            if (_remaining <= 0) return;
            float dt = Time.deltaTime;
            _remaining -= dt;
            for (int i = 0; i < _particles.Length; i++)
            {
                _velocities[i] += Vector2.down * (240 * dt);
                _particles[i].rectTransform.anchoredPosition += _velocities[i] * dt;
                _particles[i].transform.Rotate(0, 0, dt * (100 + i * 13));
                Color c = _particles[i].color;
                c.a = Mathf.Clamp01(_remaining * 2);
                _particles[i].color = c;
            }

            if (_remaining <= 0) Clear();
        }

        public void Clear()
        {
            _remaining = 0;
            if (_particles == null) return;
            foreach (var particle in _particles)
                if (particle != null)
                    particle.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            Clear();
        }
    }
}