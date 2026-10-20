using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace BusJam
{
    public sealed class GamePools
    {
        public readonly ObjectPool<BusController> Vehicles;
        public readonly ObjectPool<PassengerController> Passengers;
        public readonly ObjectPool<ParticleSystem> Effects;
        private readonly List<ParticleSystem> activeEffects = new List<ParticleSystem>();

        public GamePools(GameCatalog catalog, Transform root, int vehicleCount)
        {
            Vehicles = new ObjectPool<BusController>(() => Object.Instantiate(catalog.busPrefab, root), v => v.gameObject.SetActive(true), v => v.ResetView(), null, true, vehicleCount, vehicleCount);
            Passengers = new ObjectPool<PassengerController>(() => Object.Instantiate(catalog.passengerPrefab, root), p => p.gameObject.SetActive(true), p => p.ResetView(), null, true, 40, 40);
            Effects = new ObjectPool<ParticleSystem>(() => Object.Instantiate(catalog.particlePrefab, root), p => p.gameObject.SetActive(true), p => { p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); p.gameObject.SetActive(false); }, null, true, 24, 24);
            Warm(Vehicles, vehicleCount); Warm(Passengers, 40); Warm(Effects, 24);
        }

        private static void Warm<T>(ObjectPool<T> pool, int count) where T : class
        {
            var items = new List<T>(count);
            for (int i = 0; i < count; i++) items.Add(pool.Get());
            foreach (var item in items) pool.Release(item);
        }

        public void Puff(Vector3 position, Color color, int count = 7, bool confetti = false)
        {
            if (Effects.CountInactive == 0) return;
            var ps = Effects.Get();
            ps.transform.position = position;
            var main = ps.main;
            main.startColor = color;
            main.startSize = confetti ? .13f : .22f;
            main.startSpeed = confetti ? 3.2f : .65f;
            main.gravityModifier = confetti ? .4f : -.04f;
            main.startLifetime = confetti ? 2.1f : .55f;
            ps.Emit(count);
            activeEffects.Add(ps);
        }

        public void Tick()
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
                if (!activeEffects[i].IsAlive()) { Effects.Release(activeEffects[i]); activeEffects.RemoveAt(i); }
        }

        public void ClearEffects()
        {
            foreach (var ps in activeEffects) Effects.Release(ps);
            activeEffects.Clear();
        }
    }
}
