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
        public Transform[] parkingBays = new Transform[3];
        [Min(.02f)] public float selectionDelay = .15f;
        [Min(.02f)] public float boardingInterval = .07f;
        public RoundState State { get; private set; }
        public bool IsBusy => _loading || _activeMotions > 0 || (State != null && State.BoardingTarget() >= 0);
        public int LevelIndex { get; private set; }
        public GameUI UI { get; private set; }
        public GamePools Pools { get; private set; }
        private GameAudio _audioFx;
        private readonly List<BusController> _buses = new();
        private readonly List<PassengerController> _passengers = new();
        private readonly List<PassengerController> _seated = new();
        private float _cell;
        private Vector2 _origin;
        private bool _paused, _roundOver, _loading;
        private int _width, _height;
        private float _nextExhaust, _nextSelection, _nextBoarding;
        private int _activeMotions;
        private int[] _boardingInFlight, _boardedCounts;
        private Transform _world;
        private const int VisiblePassengers = 20;

        private void Start()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            DOTween.SetTweensCapacity(400, 100);
            if (catalog == null)
            {
                Debug.LogError("Bus Jam catalog is missing. Run Bus Jam > Setup Project.");
                return;
            }

            if (parkingBays == null || parkingBays.Length != 3 ||
                System.Array.Exists(parkingBays, bay => bay == null))
            {
                Debug.LogError("Bus Jam parking scenery is missing. Run Bus Jam > Setup Project.");
                return;
            }

            int maxVehicles = 1;
            foreach (var level in catalog.levels) maxVehicles = Mathf.Max(maxVehicles, level.vehicles.Count);
            _world = new GameObject("Pooled gameplay").transform;
            _world.SetParent(transform, false);
            Pools = new GamePools(catalog, _world, maxVehicles);
            _audioFx = gameObject.AddComponent<GameAudio>();
            _audioFx.Initialize();
            UI = new GameObject("Game UI", typeof(RectTransform)).AddComponent<GameUI>();
            UI.Initialize(catalog, TogglePause, ConfirmRetry, () =>
            {
                _audioFx.Toggle();
                UI.SetSound(_audioFx.Muted);
            });
            UI.SetSound(_audioFx.Muted);
            FitCamera();
            int saved = Mathf.Clamp(PlayerPrefs.GetInt("BusJam.Level", 0), 0, catalog.levels.Length - 1);
            LoadLevel(saved);
        }

        private void Update()
        {
            if (Pools == null) return;
            Pools.Tick();
            if (State != null && Time.time >= _nextExhaust)
            {
                _nextExhaust = Time.time + .13f;
                for (int i = 0; i < _buses.Count; i++)
                    if (State.Phases[i] == VehiclePhase.Arriving || State.Phases[i] == VehiclePhase.Departing)
                        Pools.Puff(_buses[i].tail.position, new Color(.94f, .96f, 1, .6f), 3);
            }

            if (Screen.width != _width || Screen.height != _height) FitCamera();
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            if (_loading || State == null || _roundOver || _paused || UI.ModalVisible) return;
            LaunchNextPassenger();
            if (!IsBusy)
            {
                if (State.Won) Finish(true);
                else if (State.Deadlocked || State.GridJammed) Finish(false);
            }

            if (_roundOver || Time.time < _nextSelection) return;
            Vector2 screen;
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase != TouchPhase.Began ||
                    EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return;
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
            _width = Screen.width;
            _height = Screen.height;
            float aspect = _width / (float)_height, target = 1080f / 1920;
            gameCamera.rect = aspect > target
                ? new Rect((1 - target / aspect) * .5f, 0, target / aspect, 1)
                : new Rect(0, (1 - aspect / target) * .5f, 1, aspect / target);
            gameCamera.orthographicSize = 9.6f;
        }

        public void LoadLevel(int index)
        {
            _loading = true;
            Time.timeScale = 1;
            _paused = false;
            StopAllCoroutines();
            _activeMotions = 0;
            _nextSelection = _nextBoarding = _nextExhaust = Time.time;
            gameCamera.transform.DOKill();
            gameCamera.transform.position = new Vector3(0, 0, -10);
            foreach (var p in _seated)
            {
                p.transform.SetParent(_world, true);
                Pools.Passengers.Release(p);
            }

            _seated.Clear();
            foreach (var p in _passengers) Pools.Passengers.Release(p);
            _passengers.Clear();
            foreach (var b in _buses)
                if (b != null && b.gameObject.activeSelf)
                    Pools.Vehicles.Release(b);
            _buses.Clear();
            Pools.ClearEffects();
            LevelIndex = Mathf.Clamp(index, 0, catalog.levels.Length - 1);
            var level = catalog.levels[LevelIndex];
            var errors = level.Validate();
            if (errors.Count > 0)
            {
                UI.Show("LEVEL ERROR", string.Join("\n", errors), "RETRY", () => LoadLevel(index));
                Debug.LogError(string.Join("\n", errors));
                return;
            }

            PlayerPrefs.SetInt("BusJam.Level", LevelIndex);
            PlayerPrefs.Save();
            State = new RoundState(level);
            _boardingInFlight = new int[level.vehicles.Count];
            _boardedCounts = new int[level.vehicles.Count];
            _roundOver = false;
            _cell = Mathf.Min(8.5f / level.columns, 7.2f / level.rows);
            _origin = new Vector2(-level.columns * _cell * .5f, -2.75f - level.rows * _cell * .5f);
            for (int i = 0; i < level.vehicles.Count; i++)
            {
                var v = level.vehicles[i];
                var bus = Pools.Vehicles.Get();
                bus.Configure(i, v, catalog, _cell,
                    _origin + new Vector2(v.x + v.Width * .5f, v.y + v.Height * .5f) * _cell);
                _buses.Add(bus);
            }

            for (int i = 0; i < parkingBays.Length; i++)
            {
                parkingBays[i].gameObject.SetActive(i < level.bayCount);
                parkingBays[i].position = BayPosition(i, level.bayCount);
            }

            RefreshPassengers(false);
            UI.Hide();
            UI.Refresh(LevelIndex, State);
            _loading = false;
        }

        public DispatchResult TryDispatch(int id)
        {
            if (_loading || State == null || _paused || _roundOver || UI.ModalVisible || Time.time < _nextSelection)
                return DispatchResult.Unavailable;
            var result = State.Dispatch(id);
            if (result == DispatchResult.Accepted)
            {
                _nextSelection = Time.time + Mathf.Max(.02f, selectionDelay);
                StartCoroutine(DispatchFlow(id));
            }
            else if (result == DispatchResult.Blocked || result == DispatchResult.BaysFull)
            {
                _audioFx.Play(Cue.Bump);
                var v = _buses[id].Placement;
                _buses[id].transform.DOKill();
                _buses[id].transform.localScale = Vector3.one;
                _buses[id].transform.position =
                    _origin + new Vector2(v.x + v.Width * .5f, v.y + v.Height * .5f) * _cell;
                _buses[id].transform.DOPunchPosition((Vector3)(Vector2)v.Direction * .13f, .3f, 5, .5f);
                Shake(.035f);
                Pools.Puff(_buses[id].head.position, new Color(1, .8f, .25f), 6);
            }

            return result;
        }

        private IEnumerator DispatchFlow(int id)
        {
            _activeMotions++;
            var bus = _buses[id];
            var v = bus.Placement;
            bus.hitbox.enabled = false;
            bus.transform.DOKill();
            bus.transform.position = _origin + new Vector2(v.x + v.Width * .5f, v.y + v.Height * .5f) * _cell;
            bus.transform.localScale = Vector3.one;
            _audioFx.Play(Cue.Tap);
            _audioFx.Play(Cue.Engine);
            bus.transform.DOPunchScale(new Vector3(.07f, -.05f, 0), .25f, 1);
            Vector3 start = bus.transform.position;
            float left = _origin.x - v.Length * _cell * .55f - .15f,
                right = -_origin.x + v.Length * _cell * .55f + .15f;
            float bottom = _origin.y - v.Length * _cell * .55f - .2f,
                top = _origin.y + State.Level.rows * _cell + v.Length * _cell * .55f + .15f;
            Vector3 exit = start;
            if (v.facing == Facing.Up) exit.y = top;
            else if (v.facing == Facing.Down) exit.y = bottom;
            else if (v.facing == Facing.Left) exit.x = left;
            else exit.x = right;
            Pools.Puff(bus.tail.position, new Color(.94f, .96f, 1, .7f));
            yield return bus.transform.DOMove(exit, .45f).SetEase(Ease.InQuad).WaitForCompletion();
            // Travel on the outer perimeter, never diagonally across parked vehicles.
            float side = v.facing == Facing.Left ? left : right;
            if (v.facing == Facing.Down)
            {
                bus.SetArt(side < 0 ? Facing.Left : Facing.Right, false);
                yield return bus.transform.DOMove(new Vector3(side, bottom), .25f).SetEase(Ease.Linear)
                    .WaitForCompletion();
            }

            if (v.facing != Facing.Up)
            {
                bus.SetArt(Facing.Up, false);
                yield return bus.transform.DOMove(new Vector3(v.facing == Facing.Down ? side : exit.x, top), .35f)
                    .SetEase(Ease.Linear).WaitForCompletion();
            }

            Vector3 bay = BayPosition(State.BayOf(id), State.Bays.Length);
            bus.SetArt(bus.transform.position.x < bay.x ? Facing.Right : Facing.Left, false);
            yield return bus.transform.DOMove(new Vector3(bay.x, top), .25f).SetEase(Ease.Linear).WaitForCompletion();
            bus.SetArt(Facing.Up, false);
            yield return bus.transform.DOMove(bay, .25f).SetEase(Ease.OutSine).WaitForCompletion();
            bus.Park();
            State.Arrive(id);
            UI.Refresh(LevelIndex, State);
            _activeMotions--;
        }

        private void LaunchNextPassenger()
        {
            if (Time.time < _nextBoarding) return;
            int target = State.BoardingTarget();
            if (target < 0) return;

            var passenger = _passengers[0];
            _passengers.RemoveAt(0);
            _seated.Add(passenger);
            int seat = State.Loads[target];
            // Reserve the queue entry and seat before another passenger can launch.
            State.Board(target);
            _boardingInFlight[target]++;
            _nextBoarding = Time.time + Mathf.Max(.02f, boardingInterval);
            passenger.transform.DOKill();
            StartCoroutine(BoardPassenger(passenger, target, seat));
            RefreshPassengers(true);
            UI.Refresh(LevelIndex, State);
        }

        private IEnumerator BoardPassenger(PassengerController passenger, int target, int seat)
        {
            _activeMotions++;
            var vehicle = _buses[target];
            _audioFx.Play(Cue.Step);
            yield return passenger.transform.DOJump(vehicle.seats[seat].position, .35f, 1, .22f)
                .SetEase(Ease.OutQuad).WaitForCompletion();
            passenger.transform.SetParent(vehicle.transform, true);
            passenger.transform.localScale = Vector3.one * .6f;
            vehicle.SetCount(++_boardedCounts[target]);
            _boardingInFlight[target]--;
            _activeMotions--;
            // A full reservation does not mean every passenger has landed yet.
            if (State.Phases[target] == VehiclePhase.Departing && _boardingInFlight[target] == 0)
                StartCoroutine(DepartVehicle(target));
        }

        private IEnumerator DepartVehicle(int target)
        {
            _activeMotions++;
            var vehicle = _buses[target];
            for (int i = _seated.Count - 1; i >= 0; i--)
                if (_seated[i].transform.parent == vehicle.transform)
                {
                    var p = _seated[i];
                    p.transform.SetParent(_world, true);
                    Pools.Passengers.Release(p);
                    _seated.RemoveAt(i);
                }

            vehicle.CloseRoof();
            _audioFx.Play(Cue.Honk);
            _audioFx.Play(Cue.Engine);
            Shake(.06f);
            vehicle.transform.DOPunchScale(new Vector3(.12f, -.06f, 0), .3f, 1);
            Pools.Puff(vehicle.tail.position, new Color(.94f, .96f, 1, .85f), 14);
            yield return vehicle.transform.DOMoveX(7.5f, .5f).SetEase(Ease.InCubic).WaitForCompletion();
            State.Depart(target);
            Pools.Vehicles.Release(vehicle);
            UI.Refresh(LevelIndex, State);
            _activeMotions--;
        }

        private void RefreshPassengers(bool animate)
        {
            int desired = Mathf.Min(VisiblePassengers, State.Remaining);
            while (_passengers.Count < desired) _passengers.Add(Pools.Passengers.Get());
            for (int i = 0; i < _passengers.Count; i++)
            {
                var p = _passengers[i];
                var end = PassengerPosition(i);
                p.body.color = GameCatalog.Tint(State.Level.passengers[State.PassengerIndex + i]);
                if (!animate || i == _passengers.Count - 1)
                    p.Configure(State.Level.passengers[State.PassengerIndex + i], end);
                else
                {
                    p.transform.DOKill();
                    p.transform.DOMove(end, .15f).SetEase(Ease.OutSine);
                }

                p.body.sortingOrder = 50 - i;
            }
        }

        private static Vector3 PassengerPosition(int i) => i < 12
            ? new Vector3(-1.65f + i * .36f, 5.8f, 0)
            : new Vector3(2.31f, 5.8f + (i - 11) * .25f, 0);

        private static Vector3 BayPosition(int slot, int count) =>
            new Vector3((slot - (count - 1) * .5f) * 2.15f, 3.85f, 0);

        private void Shake(float strength)
        {
            gameCamera.transform.DOKill();
            gameCamera.transform.position = new Vector3(0, 0, -10);
            gameCamera.transform.DOShakePosition(.22f, strength, 12, 90, false, true);
        }

        private void Finish(bool won)
        {
            _roundOver = true;
            _audioFx.Play(won ? Cue.Win : Cue.Lose);
            if (won)
            {
                int next = Mathf.Min(LevelIndex + 1, catalog.levels.Length - 1);
                PlayerPrefs.SetInt("BusJam.Level", next);
                PlayerPrefs.Save();
                for (int i = 0; i < 8; i++)
                    Pools.Puff(new Vector3(-3.5f + i, 5, 0), GameCatalog.Tint((ColorID)(i % 4)), 22, true);
                bool last = LevelIndex == catalog.levels.Length - 1;
                UI.Show(last ? "GAME COMPLETED!" : "LEVEL CLEARED!", null, last ? "PLAY AGAIN" : "NEXT LEVEL",
                    () => LoadLevel(last ? 0 : LevelIndex + 1), "REPLAY", () => LoadLevel(LevelIndex));
            }
            else
                UI.Show("GAME OVER", State.Deadlocked ? "Bays are full!" : "No valid moves left!", "RETRY",
                    () => LoadLevel(LevelIndex));
        }

        public void TogglePause()
        {
            if (_roundOver || (UI.ModalVisible && !_paused)) return;
            _paused = !_paused;
            Time.timeScale = _paused ? 0 : 1;
            if (_paused) UI.Show("PAUSED", null, "RESUME", TogglePause, "RETRY", () => LoadLevel(LevelIndex));
            else UI.Hide();
        }

        private void ConfirmRetry()
        {
            if (UI.ModalVisible) return;
            _paused = true;
            Time.timeScale = 0;
            UI.Show("RETRY LEVEL?", null, "RETRY", () => LoadLevel(LevelIndex), "CANCEL", () =>
            {
                _paused = false;
                Time.timeScale = 1;
                UI.Hide();
            });
        }

        private void OnDestroy()
        {
            Time.timeScale = 1;
            DOTween.KillAll();
        }
    }
}