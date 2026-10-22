using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BusJam
{
    public class BusJamGame : MonoBehaviour
    {
        public GameCatalog catalog;
        public Camera gameCamera;
        public RoundState State { get; private set; }
        public bool IsBusy { get; private set; }
        public int LevelIndex { get; private set; }
        public GameUI UI { get; private set; }
        public GamePools Pools { get; private set; }
        private GameAudio audioFx;
        private readonly List<BusController> buses = new List<BusController>();
        private readonly List<PassengerController> passengers = new List<PassengerController>();
        private readonly List<PassengerController> seated = new List<PassengerController>();
        private readonly List<Transform> slots = new List<Transform>();
        private float cell;
        private Vector2 origin;
        private bool paused, roundOver;
        private int width, height;
        private float nextExhaust;
        private Coroutine flow;
        private Transform world;
        private const int VisiblePassengers = 20;

        private void Start()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            DOTween.SetTweensCapacity(400, 100);
            if (catalog == null) { Debug.LogError("Bus Jam catalog is missing. Run Bus Jam > Setup Project."); return; }
            int maxVehicles = 1;
            foreach (var level in catalog.levels) maxVehicles = Mathf.Max(maxVehicles, level.vehicles.Count);
            world = new GameObject("Pooled gameplay").transform;
            Pools = new GamePools(catalog, world, maxVehicles);
            audioFx = gameObject.AddComponent<GameAudio>(); audioFx.Initialize();
            UI = new GameObject("Game UI", typeof(RectTransform)).AddComponent<GameUI>();
            UI.Initialize(catalog, TogglePause, ConfirmRetry, () => { audioFx.Toggle(); UI.SetSound(audioFx.Muted); });
            UI.SetSound(audioFx.Muted);
            var bg = new GameObject("Mart and parking background").AddComponent<SpriteRenderer>();
            bg.sprite = catalog.background; bg.sortingOrder = -20;
            bg.transform.localScale = new Vector3(10.8f / bg.sprite.bounds.size.x, 19.2f / bg.sprite.bounds.size.y, 1);
            for (int i = 0; i < 3; i++)
            {
                var slot = new GameObject("Bay " + (i + 1)).transform;
                slot.position = BayPosition(i, 3);
                var white = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), Vector2.one * .5f, Texture2D.whiteTexture.width);
                for (int d = 0; d < 8; d++)
                {
                    Dash(slot, white, new Vector2(-.62f, -.88f + d * .25f), new Vector2(.035f, .14f));
                    Dash(slot, white, new Vector2(.62f, -.88f + d * .25f), new Vector2(.035f, .14f));
                }
                for (int d = 0; d < 5; d++)
                {
                    Dash(slot, white, new Vector2(-.5f + d * .25f, -.99f), new Vector2(.14f, .035f));
                    Dash(slot, white, new Vector2(-.5f + d * .25f, .99f), new Vector2(.14f, .035f));
                }
                slots.Add(slot);
            }
            FitCamera();
            int saved = Mathf.Clamp(PlayerPrefs.GetInt("BusJam.Level", 0), 0, catalog.levels.Length - 1);
            LoadLevel(saved);
        }

        private void Update()
        {
            if (Pools == null) return;
            Pools.Tick();
            if (State != null && Time.time >= nextExhaust)
            {
                nextExhaust = Time.time + .13f;
                for (int i = 0; i < buses.Count; i++)
                    if (State.Phases[i] == VehiclePhase.Arriving || State.Phases[i] == VehiclePhase.Departing)
                        Pools.Puff(buses[i].tail.position, new Color(.94f, .96f, 1, .6f), 3);
            }
            if (Screen.width != width || Screen.height != height) FitCamera();
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            if (IsBusy || roundOver || paused || UI.ModalVisible) return;
            Vector2 screen;
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase != TouchPhase.Began || EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return;
                screen = touch.position;
            }
            else
            {
                if (!Input.GetMouseButtonDown(0) || EventSystem.current.IsPointerOverGameObject()) return;
                screen = Input.mousePosition;
            }
            if (!gameCamera.pixelRect.Contains(screen)) return;
            var hit = Physics2D.OverlapPoint(gameCamera.ScreenToWorldPoint(screen));
            if (hit != null && hit.TryGetComponent<BusController>(out var vehicle)) TryDispatch(vehicle.Id);
        }

        private void FitCamera()
        {
            width = Screen.width; height = Screen.height;
            float aspect = width / (float)height, target = 1080f / 1920;
            gameCamera.rect = aspect > target ? new Rect((1 - target / aspect) * .5f, 0, target / aspect, 1) : new Rect(0, (1 - aspect / target) * .5f, 1, aspect / target);
            gameCamera.orthographicSize = 9.6f;
        }

        public void LoadLevel(int index)
        {
            IsBusy = true; Time.timeScale = 1; paused = false;
            if (flow != null) StopCoroutine(flow);
            flow = null;
            gameCamera.transform.DOKill(); gameCamera.transform.position = new Vector3(0, 0, -10);
            foreach (var p in seated) { p.transform.SetParent(world, true); Pools.Passengers.Release(p); } seated.Clear();
            foreach (var p in passengers) Pools.Passengers.Release(p); passengers.Clear();
            foreach (var b in buses) if (b != null && b.gameObject.activeSelf) Pools.Vehicles.Release(b); buses.Clear();
            Pools.ClearEffects();
            LevelIndex = Mathf.Clamp(index, 0, catalog.levels.Length - 1);
            var level = catalog.levels[LevelIndex];
            var errors = level.Validate();
            if (errors.Count > 0) { UI.Show("LEVEL ERROR", string.Join("\n", errors), "RETRY", () => LoadLevel(index)); Debug.LogError(string.Join("\n", errors)); return; }
            PlayerPrefs.SetInt("BusJam.Level", LevelIndex); PlayerPrefs.Save();
            State = new RoundState(level); roundOver = false;
            cell = Mathf.Min(8.5f / level.columns, 7.2f / level.rows);
            origin = new Vector2(-level.columns * cell * .5f, -2.75f - level.rows * cell * .5f);
            for (int i = 0; i < level.vehicles.Count; i++)
            {
                var v = level.vehicles[i]; var bus = Pools.Vehicles.Get();
                bus.Configure(i, v, catalog, cell, origin + new Vector2(v.x + v.Width * .5f, v.y + v.Height * .5f) * cell);
                buses.Add(bus);
            }
            for (int i = 0; i < slots.Count; i++) { slots[i].gameObject.SetActive(i < level.bayCount); slots[i].transform.position = BayPosition(i, level.bayCount); }
            RefreshPassengers(false);
            UI.Hide(); UI.Refresh(LevelIndex, State); IsBusy = false;
        }

        public DispatchResult TryDispatch(int id)
        {
            if (IsBusy || paused || roundOver || UI.ModalVisible) return DispatchResult.Unavailable;
            var result = State.Dispatch(id);
            if (result == DispatchResult.Accepted) { IsBusy = true; flow = StartCoroutine(DispatchFlow(id)); }
            else if (result == DispatchResult.Blocked || result == DispatchResult.BaysFull)
            {
                audioFx.Play(Cue.Bump);
                var v = buses[id].Placement;
                buses[id].transform.DOKill(); buses[id].transform.localScale = Vector3.one;
                buses[id].transform.position = origin + new Vector2(v.x + v.Width * .5f, v.y + v.Height * .5f) * cell;
                buses[id].transform.DOPunchPosition((Vector3)(Vector2)v.Direction * .13f, .3f, 5, .5f);
                Shake(.035f); Pools.Puff(buses[id].head.position, new Color(1, .8f, .25f), 6);
            }
            return result;
        }

        private IEnumerator DispatchFlow(int id)
        {
            var bus = buses[id]; var v = bus.Placement; bus.hitbox.enabled = false; bus.transform.DOKill();
            bus.transform.position = origin + new Vector2(v.x + v.Width * .5f, v.y + v.Height * .5f) * cell;
            bus.transform.localScale = Vector3.one;
            audioFx.Play(Cue.Tap); audioFx.Play(Cue.Engine);
            bus.transform.DOPunchScale(new Vector3(.07f, -.05f, 0), .25f, 1);
            Vector3 start = bus.transform.position;
            float left = origin.x - v.Length * cell * .55f - .15f, right = -origin.x + v.Length * cell * .55f + .15f;
            float bottom = origin.y - v.Length * cell * .55f - .2f, top = origin.y + State.Level.rows * cell + v.Length * cell * .55f + .15f;
            Vector3 exit = start;
            if (v.facing == Facing.Up) exit.y = top; else if (v.facing == Facing.Down) exit.y = bottom; else if (v.facing == Facing.Left) exit.x = left; else exit.x = right;
            Pools.Puff(bus.tail.position, new Color(.94f, .96f, 1, .7f));
            yield return bus.transform.DOMove(exit, .45f).SetEase(Ease.InQuad).WaitForCompletion();
            // Travel on the outer perimeter, never diagonally across parked vehicles.
            float side = v.facing == Facing.Left ? left : right;
            if (v.facing == Facing.Down)
            {
                bus.SetArt(side < 0 ? Facing.Left : Facing.Right, false);
                yield return bus.transform.DOMove(new Vector3(side, bottom), .25f).SetEase(Ease.Linear).WaitForCompletion();
            }
            if (v.facing != Facing.Up)
            {
                bus.SetArt(Facing.Up, false);
                yield return bus.transform.DOMove(new Vector3(v.facing == Facing.Down ? side : exit.x, top), .35f).SetEase(Ease.Linear).WaitForCompletion();
            }
            Vector3 bay = BayPosition(State.BayOf(id), State.Bays.Length);
            bus.SetArt(bus.transform.position.x < bay.x ? Facing.Right : Facing.Left, false);
            yield return bus.transform.DOMove(new Vector3(bay.x, top), .25f).SetEase(Ease.Linear).WaitForCompletion();
            bus.SetArt(Facing.Up, false);
            yield return bus.transform.DOMove(bay, .25f).SetEase(Ease.OutSine).WaitForCompletion();
            bus.Park(); State.Arrive(id); UI.Refresh(LevelIndex, State);
            int target;
            while ((target = State.BoardingTarget()) >= 0)
            {
                var passenger = passengers[0]; passengers.RemoveAt(0); seated.Add(passenger);
                var vehicle = buses[target]; int seat = State.Loads[target];
                audioFx.Play(Cue.Step);
                yield return passenger.transform.DOJump(vehicle.seats[seat].position, .35f, 1, .22f).SetEase(Ease.OutQuad).WaitForCompletion();
                passenger.transform.SetParent(vehicle.transform, true); passenger.transform.localScale = Vector3.one * .6f;
                State.Board(target); vehicle.SetCount(State.Loads[target]);
                RefreshPassengers(true); UI.Refresh(LevelIndex, State);
                if (State.Phases[target] == VehiclePhase.Departing)
                {
                    for (int i = seated.Count - 1; i >= 0; i--)
                        if (seated[i].transform.parent == vehicle.transform)
                        {
                            var p = seated[i]; p.transform.SetParent(world, true); Pools.Passengers.Release(p); seated.RemoveAt(i);
                        }
                    vehicle.CloseRoof(); audioFx.Play(Cue.Honk); audioFx.Play(Cue.Engine); Shake(.06f);
                    vehicle.transform.DOPunchScale(new Vector3(.12f, -.06f, 0), .3f, 1);
                    Pools.Puff(vehicle.tail.position, new Color(.94f, .96f, 1, .85f), 14);
                    yield return vehicle.transform.DOMoveX(7.5f, .5f).SetEase(Ease.InCubic).WaitForCompletion();
                    State.Depart(target); Pools.Vehicles.Release(vehicle); UI.Refresh(LevelIndex, State);
                }
            }
            IsBusy = false; flow = null;
            if (State.Won) Finish(true);
            else if (State.Deadlocked || State.GridJammed) Finish(false);
        }

        private void RefreshPassengers(bool animate)
        {
            int desired = Mathf.Min(VisiblePassengers, State.Remaining);
            while (passengers.Count < desired) passengers.Add(Pools.Passengers.Get());
            for (int i = 0; i < passengers.Count; i++)
            {
                var p = passengers[i]; var end = PassengerPosition(i);
                p.body.color = GameCatalog.Tint(State.Level.passengers[State.PassengerIndex + i]);
                if (!animate || i == passengers.Count - 1) p.Configure(State.Level.passengers[State.PassengerIndex + i], end);
                else { p.transform.DOKill(); p.transform.DOMove(end, .15f).SetEase(Ease.OutSine); }
                p.body.sortingOrder = 50 - i;
            }
        }

        private static Vector3 PassengerPosition(int i) => i < 12 ? new Vector3(-1.65f + i * .36f, 5.8f, 0) : new Vector3(2.31f, 5.8f + (i - 11) * .25f, 0);
        private static Vector3 BayPosition(int slot, int count) => new Vector3((slot - (count - 1) * .5f) * 2.15f, 3.85f, 0);
        private static void Dash(Transform parent, Sprite sprite, Vector2 position, Vector2 size)
        {
            var dash = new GameObject("Parking line").AddComponent<SpriteRenderer>();
            dash.transform.SetParent(parent, false); dash.transform.localPosition = position; dash.transform.localScale = new Vector3(size.x, size.y, 1);
            dash.sprite = sprite; dash.color = new Color(1, 1, 1, .85f); dash.sortingOrder = -5;
        }
        private void Shake(float strength)
        {
            gameCamera.transform.DOKill(); gameCamera.transform.position = new Vector3(0, 0, -10);
            gameCamera.transform.DOShakePosition(.22f, strength, 12, 90, false, true);
        }

        private void Finish(bool won)
        {
            roundOver = true; audioFx.Play(won ? Cue.Win : Cue.Lose);
            if (won)
            {
                int next = Mathf.Min(LevelIndex + 1, catalog.levels.Length - 1);
                PlayerPrefs.SetInt("BusJam.Level", next); PlayerPrefs.Save();
                for (int i = 0; i < 8; i++) Pools.Puff(new Vector3(-3.5f + i, 5, 0), GameCatalog.Tint((ColorID)(i % 4)), 22, true);
                bool last = LevelIndex == catalog.levels.Length - 1;
                UI.Show(last ? "GAME COMPLETED!" : "LEVEL CLEARED!", null, last ? "PLAY AGAIN" : "NEXT LEVEL", () => LoadLevel(last ? 0 : LevelIndex + 1), "REPLAY", () => LoadLevel(LevelIndex));
            }
            else UI.Show("GAME OVER", State.Deadlocked ? "Bays are full!" : "No valid moves left!", "RETRY", () => LoadLevel(LevelIndex));
        }

        public void TogglePause()
        {
            if (roundOver || (UI.ModalVisible && !paused)) return;
            paused = !paused; Time.timeScale = paused ? 0 : 1;
            if (paused) UI.Show("PAUSED", null, "RESUME", TogglePause, "RETRY", () => LoadLevel(LevelIndex));
            else UI.Hide();
        }

        private void ConfirmRetry()
        {
            if (UI.ModalVisible) return;
            paused = true; Time.timeScale = 0;
            UI.Show("RETRY LEVEL?", null, "RETRY", () => LoadLevel(LevelIndex), "CANCEL", () => { paused = false; Time.timeScale = 1; UI.Hide(); });
        }

        private void OnDestroy() { Time.timeScale = 1; DOTween.KillAll(); }
    }
}
