using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BusJam
{
    public class BusJamGame : MonoBehaviour
    {
        [Header("Configuration & References")]
        [SerializeField] private GameCatalog catalog;
        [SerializeField] private Camera gameCamera;
        [SerializeField] private BackgroundFitter backgroundFitter;
        [SerializeField] private GameObject gameplaySceneryRoot;
        [SerializeField] private Transform pooledGameplayRoot;
        [SerializeField] private GameUI uiInScene;
        [SerializeField] private Transform[] parkingBays = new Transform[3];

        [Header("Performance & Systems")]
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private int tweenCapacity = 400;
        [SerializeField] private int sequenceCapacity = 100;

        [Header("Gameplay Timing")]
        [Min(.02f)] public float selectionDelay = .15f;
        [Min(.02f)] public float boardingInterval = .07f;
        [SerializeField] private float exhaustInterval = 0.13f;

        [Header("Camera & Viewport")]
        [SerializeField] private float cameraOrthographicSize = 9.6f;
        [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 1920f);
        [SerializeField] private float shakeDuration = 0.22f;
        [SerializeField] private int shakeVibrato = 12;

        [Header("Grid & Board Layout")]
        [SerializeField] private float maxGridWidth = 8.5f;
        [SerializeField] private float maxGridHeight = 7.2f;
        [Min(0.1f)] [SerializeField] private float maxCellSize = 1.0f;
        [SerializeField] private float gridCenterY = -2.75f;
        [SerializeField] private float baySpacing = 2.15f;
        [SerializeField] private float bayPosY = 4.35f;

        [Header("Background-Aligned Bays & Queue")]
        [SerializeField] private bool alignToBackground = true;
        [SerializeField] private float bayCurbNormalizedY = 0.716f;
        [SerializeField] private float bayCurbUsableWidthRatio = 0.62f;
        [SerializeField] private float bayTiltAngle = 20f;
        [SerializeField] private Vector2 bayVisualScale = new Vector2(0.52f, 0.48f);
        [SerializeField] private float gridNormalizedY = 0.36f;
        [SerializeField] private float queueLeftNormalizedX = 0.305f;
        [SerializeField] private float queueRightNormalizedX = 0.762f;
        [SerializeField] private float queueFrontNormalizedY = 0.798f;
        [SerializeField] private float queueBarNormalizedY = 0.850f;
        [SerializeField] private float queueBackNormalizedY = 0.808f;
        [SerializeField] private int queueVerticalCount = 4;
        [SerializeField] private int queueHorizontalCount = 13;

        [Header("Passenger Queue")]
        [SerializeField] private int visiblePassengers = 20;
        [SerializeField] private float queueFrontStartX = -1.65f;
        [SerializeField] private float queueFrontSpacing = 0.36f;
        [SerializeField] private float queueFrontY = 5.8f;
        [SerializeField] private float queueTurnX = 2.31f;
        [SerializeField] private float queueTurnSpacingY = 0.25f;
        [SerializeField] private int queueTurnThreshold = 12;
        [SerializeField] private float passengerQueueMoveDuration = 0.15f;
        [SerializeField] private float passengerJumpPower = 0.35f;
        [SerializeField] private float passengerJumpDuration = 0.22f;
        [SerializeField] private float passengerSeatedScale = 0.6f;

        [Header("Vehicle Entrance")]
        [SerializeField] private float vehicleEntranceDuration = 0.3f;
        [SerializeField] private float vehicleEntranceStagger = 0.04f;
        [SerializeField] private float vehicleEntranceOffset = 1.6f;
        [SerializeField] private float vehicleLandPunch = 0.12f;
        [SerializeField] private float vehicleLandPunchDuration = 0.18f;
        [SerializeField] private Color landSpotlightColor = new Color(1f, 0.95f, 0.7f, 0.65f);
        [SerializeField] private float landSpotlightDuration = 0.6f;

        [Header("Movement & Feedback")]
        [SerializeField] private float busExitDuration = 0.45f;
        [SerializeField] private float busPerimeterDuration = 0.25f;
        [SerializeField] private float busAscentDuration = 0.35f;
        [SerializeField] private float busAlignDuration = 0.25f;
        [SerializeField] private float busParkDuration = 0.25f;
        [SerializeField] private float departShakeStrength = 0.06f;
        [SerializeField] private float departExitX = 7.5f;
        [SerializeField] private float departDuration = 0.5f;
        [SerializeField] private float bumpShakeStrength = 0.035f;
        [SerializeField] private float bumpPunchDistance = 0.13f;
        [SerializeField] private float bumpPunchDuration = 0.3f;
        [SerializeField] private float jamApproachSpeed = 8f;
        [SerializeField] private float jamReturnDuration = 0.2f;

        [Header("Visual Effects & Particle Cues")]
        [SerializeField] private Color exhaustPuffColor = new Color(0.94f, 0.96f, 1f, 0.6f);
        [SerializeField] private int exhaustPuffCount = 3;
        [SerializeField] private Color bumpPuffColor = new Color(1f, 0.8f, 0.25f);
        [SerializeField] private int bumpPuffCount = 6;
        [SerializeField] private Color departPuffColor = new Color(0.94f, 0.96f, 1f, 0.85f);
        [SerializeField] private int departPuffCount = 14;
        [SerializeField] private Color clickPuffColor = new Color(1f, 0.8f, 0.25f);
        [SerializeField] private int clickPuffCount = 8;

        [Header("Rewards")]
        [SerializeField] private int winReward = 100;

        private RoundState State { get; set; }
        private bool IsBusy => _loading || _activeMotions > 0 || (State != null && State.BoardingTarget() >= 0);
        private int LevelIndex { get; set; }
        private GameUI UI { get; set; }
        private GamePools Pools { get; set; }
        public event System.Action<RoundFinishedResult> RoundFinished;
        private GameAudio _audioFx;
        private RoundProgressObserver _roundProgress;
        private RoundFeedbackObserver _roundFeedback;
        private RoundResultPresenter _roundPresenter;
        private readonly List<BusController> _buses = new();
        private readonly List<PassengerController> _passengers = new();
        private readonly List<PassengerController> _seated = new();
        private float _cell;
        private Vector2 _origin;
        private bool _paused, _roundOver, _loading, _homeOpen;
        public int Coins => SaveData.Coins;
        private int _width, _height;
        private float _nextExhaust, _nextSelection, _nextBoarding;
        private int _activeMotions;
        private int[] _boardingInFlight, _boardedCounts;
        private Transform _world;

        private void Start()
        {
            Application.targetFrameRate = targetFrameRate;
            Screen.orientation = ScreenOrientation.Portrait;
            DOTween.SetTweensCapacity(tweenCapacity, sequenceCapacity);
            if (catalog == null)
            {
                Debug.LogError("Bus Jam catalog is missing. Run Bus Jam > Setup Project.");
                return;
            }

            if (parkingBays == null || parkingBays.Length != 3 || System.Array.Exists(parkingBays, bay => bay == null))
            {
                Debug.LogError("Bus Jam parking scenery is missing. Run Bus Jam > Setup Project.");
                return;
            }

            int maxVehicles = 1;
            foreach (var level in catalog.levels) maxVehicles = Mathf.Max(maxVehicles, level.vehicles.Count);
            if (pooledGameplayRoot != null)
            {
                _world = pooledGameplayRoot;
            }
            else
            {
                _world = new GameObject("Pooled gameplay").transform;
                _world.SetParent(transform, false);
            }
            Pools = new GamePools(catalog, _world, maxVehicles);
            _audioFx = gameObject.AddComponent<GameAudio>();
            _audioFx.Initialize();

            // Validate if uiInScene is null
            if (uiInScene == null)
            {
                Debug.LogError("Bus Jam UI is not assigned. Drag the scene's BusJamUI object into Ui In Scene.");
                enabled = false;
                return;
            }

            UI = uiInScene;
            UI.Initialize(
                onPlay: StartPlaying,
                onHomeSettings: OpenSettingsFromHome,
                onSettings: () => TogglePause(),
                retry: ConfirmRetry,
                onHome: () =>
                {
                    CloseSettings();
                    ShowHome();
                },
                onClose: CloseSettings,
                gAudio: _audioFx,
                clickVfx: () =>
                {
                    if (_homeOpen) return;
                    Vector3 point = gameCamera.ScreenToWorldPoint(Input.mousePosition);
                    point.z = 0;
                    Pools.Puff(point, clickPuffColor, clickPuffCount);
                });
            _roundProgress = new RoundProgressObserver(this);
            _roundFeedback = new RoundFeedbackObserver(this, _audioFx, Pools);
            _roundPresenter = new RoundResultPresenter(this, UI);
            FitCamera();
            UI.SetCoins(SaveData.Coins);
            ShowHome(instant: true);
        }

        private void Update()
        {
            if (Pools == null) return;
            Pools.Tick();
            if (!_homeOpen && State != null && Time.time >= _nextExhaust)
            {
                _nextExhaust = Time.time + exhaustInterval;
                for (var i = 0; i < _buses.Count; i++)
                    if (State.Phases[i] == VehiclePhase.Arriving || State.Phases[i] == VehiclePhase.Departing)
                        Pools.Puff(_buses[i].tail.position, exhaustPuffColor, exhaustPuffCount);
            }

            if (Screen.width != _width || Screen.height != _height) FitCamera();
            if (Input.GetKeyDown(KeyCode.Escape) && !_homeOpen) TogglePause();
            if (_homeOpen || _loading || State == null || _roundOver || _paused) return;
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
            if (_width <= 0 || _height <= 0) return;
            float aspect = _width / (float)_height, target = referenceResolution.x / referenceResolution.y;
            gameCamera.rect = new Rect(0f, 0f, 1f, 1f);
            gameCamera.orthographicSize = aspect < target
                ? cameraOrthographicSize * (target / aspect)
                : cameraOrthographicSize;
            if (backgroundFitter != null) backgroundFitter.Fit();
            RelayoutActiveWorld();
        }

        private float CurrentGridCenterY() =>
            alignToBackground && backgroundFitter != null
                ? backgroundFitter.NormalizedToWorld(0.5f, gridNormalizedY).y
                : gridCenterY;

        private void RelayoutActiveWorld()
        {
            if (_homeOpen || _loading || State == null) return;
            _origin = new Vector2(-State.Level.columns * _cell * .5f, CurrentGridCenterY() - State.Level.rows * _cell * .5f);
            for (var i = 0; i < _buses.Count; i++)
            {
                if (State.Phases[i] == VehiclePhase.Grid && _buses[i].hitbox.enabled)
                {
                    var v = _buses[i].Placement;
                    _buses[i].transform.position = _origin + new Vector2(v.x + v.Width * .5f, v.y + v.Height * .5f) * _cell;
                }
            }

            int bayCount = State.Bays.Length;
            Quaternion bayRotation = Quaternion.Euler(0f, 0f, bayTiltAngle);
            Vector3 bayScale = new Vector3(bayVisualScale.x, bayVisualScale.y, 1f);
            for (var i = 0; i < parkingBays.Length; i++)
            {
                bool active = i < bayCount;
                parkingBays[i].gameObject.SetActive(active);
                if (!active) continue;
                parkingBays[i].position = BayPosition(i, bayCount);
                parkingBays[i].rotation = bayRotation;
                parkingBays[i].localScale = bayScale;
            }

            for (var i = 0; i < State.Bays.Length; i++)
            {
                int vehicleId = State.Bays[i];
                if (vehicleId >= 0 && vehicleId < _buses.Count && State.Phases[vehicleId] == VehiclePhase.Waiting)
                {
                    _buses[vehicleId].transform.position = BayPosition(i, bayCount);
                    _buses[vehicleId].transform.rotation = bayRotation;
                }
            }

            RefreshPassengers(false);
        }

        private void ReleaseGameplayObjects()
        {
            StopAllCoroutines();
            _activeMotions = 0;
            if (gameCamera != null)
            {
                gameCamera.transform.DOKill();
                gameCamera.transform.position = new Vector3(0, 0, -10);
            }
            if (Pools == null) return;
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
        }

        public void LoadLevel(int index)
        {
            _loading = true;
            Time.timeScale = 1;
            _paused = false;
            _nextSelection = _nextBoarding = _nextExhaust = Time.time;
            ReleaseGameplayObjects();
            if (gameplaySceneryRoot != null) gameplaySceneryRoot.SetActive(true);
            LevelIndex = Mathf.Clamp(index, 0, catalog.levels.Length - 1);
            var level = catalog.levels[LevelIndex];
            var errors = level.Validate();
            if (errors.Count > 0)
            {
                UI.Show("LEVEL ERROR", string.Join("\n", errors), "RETRY", () => LoadLevel(index));
                Debug.LogError(string.Join("\n", errors));
                return;
            }

            SaveData.Level = LevelIndex;
            State = new RoundState(level);
            _boardingInFlight = new int[level.vehicles.Count];
            _boardedCounts = new int[level.vehicles.Count];
            _roundOver = false;
            _cell = Mathf.Min(maxCellSize, maxGridWidth / level.columns, maxGridHeight / level.rows);
            _origin = new Vector2(-level.columns * _cell * .5f, CurrentGridCenterY() - level.rows * _cell * .5f);
            for (int i = 0; i < level.vehicles.Count; i++)
            {
                var v = level.vehicles[i];
                var bus = Pools.Vehicles.Get();
                Vector3 targetPosition = _origin + new Vector2(v.x + v.Width * .5f, v.y + v.Height * .5f) * _cell;
                bus.Configure(i, v, catalog, _cell, targetPosition);
                _buses.Add(bus);
                PlayVehicleEntrance(bus, targetPosition, i * vehicleEntranceStagger);
            }

            Quaternion bayRotation = Quaternion.Euler(0f, 0f, bayTiltAngle);
            Vector3 bayScale = new Vector3(bayVisualScale.x, bayVisualScale.y, 1f);
            for (var i = 0; i < parkingBays.Length; i++)
            {
                bool active = i < level.bayCount;
                parkingBays[i].gameObject.SetActive(active);
                if (!active) continue;
                parkingBays[i].position = BayPosition(i, level.bayCount);
                parkingBays[i].rotation = bayRotation;
                parkingBays[i].localScale = bayScale;
            }

            RefreshPassengers(false);
            UI.Hide();
            UI.Refresh(LevelIndex, State);
            _loading = false;
        }

        private static Sprite _spotlightSprite;

        private void PlayVehicleEntrance(BusController bus, Vector3 targetPosition, float delay)
        {
            _activeMotions++;
            bus.hitbox.enabled = false;
            bus.body.DOFade(0, 0);
            bus.transform.position = targetPosition + Vector3.up * vehicleEntranceOffset;
            // Accelerating drop (sharp impact), quick fade-in during the first third, then a punch on landing.
            DOTween.Sequence()
                .SetDelay(delay)
                .Append(bus.transform.DOMove(targetPosition, vehicleEntranceDuration).SetEase(Ease.InQuad))
                .Join(bus.body.DOFade(1, vehicleEntranceDuration * .35f))
                .AppendCallback(() =>
                {
                    if (bus == null || !bus.gameObject.activeInHierarchy) return;
                    bus.hitbox.enabled = true;
                    _activeMotions = Mathf.Max(0, _activeMotions - 1);
                    bus.transform.DOPunchScale(Vector3.one * vehicleLandPunch, vehicleLandPunchDuration, 1, 0);
                    PlayLandSpotlight(bus);
                })
                .SetLink(bus.gameObject);
        }

        private void PlayLandSpotlight(BusController bus)
        {
            if (_spotlightSprite == null)
            {
                const int size = 64;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float d = Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), Vector2.one * (size - 1) * .5f) / (size * .5f));
                        tex.SetPixel(x, y, new Color(1, 1, 1, (1 - d) * (1 - d)));
                    }
                tex.Apply();
                _spotlightSprite = Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * .5f, size);
            }

            var bounds = bus.body.bounds;
            var go = new GameObject("LandSpotlight");
            go.transform.SetParent(bus.transform.parent, true);
            go.transform.position = new Vector3(bounds.center.x, bounds.min.y, bus.transform.position.z);
            go.transform.localScale = new Vector3(bounds.size.x * 1.4f, bounds.size.x * .45f, 1);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _spotlightSprite;
            sr.color = landSpotlightColor;
            sr.sortingLayerID = bus.body.sortingLayerID;
            sr.sortingOrder = bus.body.sortingOrder - 1;
            sr.DOFade(0, landSpotlightDuration).SetEase(Ease.OutQuad).OnComplete(() => Destroy(go)).SetLink(go);
        }

        private void TryDispatch(int id)
        {
            if (_loading || State == null || _paused || _roundOver || Time.time < _nextSelection) return;
            var result = State.Dispatch(id);
            if (result == DispatchResult.Accepted)
            {
                _nextSelection = Time.time + Mathf.Max(.02f, selectionDelay);
                HapticFeedback.Play(HapticCue.Light);
                StartCoroutine(DispatchFlow(id));
            }
            else if (result == DispatchResult.Blocked || result == DispatchResult.BaysFull)
            {
                if (result == DispatchResult.Blocked)
                {
                    _nextSelection = Time.time + Mathf.Max(.02f, selectionDelay);
                    StartCoroutine(RunIntoJam(id));
                }
                else
                {
                    PlayJamFeedback(_buses[id]);
                    _buses[id].transform.DOPunchPosition(
                        (Vector3)(Vector2)_buses[id].Placement.Direction * bumpPunchDistance,
                        bumpPunchDuration, 5, .5f);
                    Shake(bumpShakeStrength);
                }
            }
        }

        private IEnumerator RunIntoJam(int id)
        {
            _activeMotions++;
            var bus = _buses[id];
            var placement = bus.Placement;
            Vector3 start = _origin + new Vector2(placement.x + placement.Width * .5f,
                placement.y + placement.Height * .5f) * _cell;
            Vector3 direction = (Vector2)placement.Direction;
            float travelDistance = BlockedTravelDistance(id);

            bus.hitbox.enabled = false;
            bus.transform.DOKill();
            bus.transform.position = start;
            bus.transform.localScale = Vector3.one;
            _audioFx.Play(Cue.Tap);
            _audioFx.Play(Cue.Engine);
            if (travelDistance > 0)
            {
                float approachDuration = travelDistance / Mathf.Max(.1f, jamApproachSpeed);
                Pools.Puff(bus.tail.position, exhaustPuffColor, exhaustPuffCount);
                yield return bus.transform.DOMove(start + direction * travelDistance, approachDuration)
                    .SetEase(Ease.InQuad).WaitForCompletion();
            }

            PlayJamFeedback(bus);
            Shake(bumpShakeStrength);
            yield return bus.transform.DOMove(start, jamReturnDuration).SetEase(Ease.OutSine).WaitForCompletion();
            bus.hitbox.enabled = true;
            _activeMotions--;
        }

        private float BlockedTravelDistance(int id)
        {
            var placement = _buses[id].Placement;
            var occupancy = State.Occupancy();
            Vector2Int step = placement.Direction;

            for (int distance = 1; ; distance++)
            {
                int x = placement.x + step.x * distance;
                int y = placement.y + step.y * distance;
                bool reachedExit = x + placement.Width <= 0 || y + placement.Height <= 0 ||
                                   x >= State.Level.columns || y >= State.Level.rows;
                if (reachedExit) return 0;

                for (int cellX = x; cellX < x + placement.Width; cellX++)
                for (int cellY = y; cellY < y + placement.Height; cellY++)
                    if (cellX >= 0 && cellX < State.Level.columns && cellY >= 0 && cellY < State.Level.rows &&
                        occupancy[cellX, cellY] >= 0 && occupancy[cellX, cellY] != id)
                        return (distance - 1) * _cell;
            }
        }

        private void PlayJamFeedback(BusController bus)
        {
            _audioFx.Play(Cue.Bump);
            HapticFeedback.Play(HapticCue.Warning);
            Pools.Puff(bus.head.position, bumpPuffColor, bumpPuffCount);
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
            Pools.Puff(bus.tail.position, exhaustPuffColor, exhaustPuffCount);
            yield return bus.transform.DOMove(exit, busExitDuration).SetEase(Ease.InQuad).WaitForCompletion();
            // Travel on the outer perimeter, never diagonally across parked vehicles.
            float side = v.facing == Facing.Left ? left : right;
            if (v.facing == Facing.Down)
            {
                bus.SetArt(side < 0 ? Facing.Left : Facing.Right, false);
                yield return bus.transform.DOMove(new Vector3(side, bottom), busPerimeterDuration).SetEase(Ease.Linear)
                    .WaitForCompletion();
            }

            if (v.facing != Facing.Up)
            {
                bus.SetArt(Facing.Up, false);
                yield return bus.transform.DOMove(new Vector3(v.facing == Facing.Down ? side : exit.x, top), busAscentDuration)
                    .SetEase(Ease.Linear).WaitForCompletion();
            }

            Vector3 bay = BayPosition(State.BayOf(id), State.Bays.Length);
            bus.SetArt(bus.transform.position.x < bay.x ? Facing.Right : Facing.Left, false);
            yield return bus.transform.DOMove(new Vector3(bay.x, top), busAlignDuration).SetEase(Ease.Linear).WaitForCompletion();
            bus.SetArt(Facing.Up, false);
            bus.transform.DORotate(new Vector3(0f, 0f, bayTiltAngle), busParkDuration).SetEase(Ease.OutSine);
            yield return bus.transform.DOMove(bay, busParkDuration).SetEase(Ease.OutSine).WaitForCompletion();
            bus.Park(bayTiltAngle);
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
            yield return passenger.transform.DOJump(vehicle.seats[seat].position, passengerJumpPower, 1, passengerJumpDuration)
                .SetEase(Ease.OutQuad).WaitForCompletion();
            passenger.transform.SetParent(vehicle.transform, true);
            passenger.transform.localScale = Vector3.one * passengerSeatedScale;
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
            HapticFeedback.Play(HapticCue.Success);
            _audioFx.Play(Cue.Engine);
            Shake(departShakeStrength);
            vehicle.transform.DOPunchScale(new Vector3(.12f, -.06f, 0), .3f, 1);
            Pools.Puff(vehicle.tail.position, departPuffColor, departPuffCount);
            float exitX = departExitX;
            if (gameCamera != null) exitX = Mathf.Max(exitX, gameCamera.orthographicSize * gameCamera.aspect + 2.5f);
            yield return vehicle.transform.DOMoveX(exitX, departDuration).SetEase(Ease.InCubic).WaitForCompletion();
            State.Depart(target);
            Pools.Vehicles.Release(vehicle);
            UI.Refresh(LevelIndex, State);
            _activeMotions--;
        }

        private void RefreshPassengers(bool animate)
        {
            int desired = Mathf.Min(visiblePassengers, State.Remaining);
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
                    p.transform.DOMove(end, passengerQueueMoveDuration).SetEase(Ease.OutSine);
                }

                p.body.sortingOrder = 50 - i;
            }
        }

        private Vector3 PassengerPosition(int i)
        {
            if (alignToBackground && backgroundFitter != null)
            {
                int verticalCount = Mathf.Max(2, queueVerticalCount);
                int horizontalCount = Mathf.Max(2, queueHorizontalCount);
                if (i < verticalCount)
                {
                    float t = i / (float)(verticalCount - 1);
                    float v = Mathf.Lerp(queueFrontNormalizedY, queueBarNormalizedY, t);
                    return backgroundFitter.NormalizedToWorld(queueLeftNormalizedX, v);
                }

                int horizIndex = i - verticalCount + 1;
                if (horizIndex <= horizontalCount)
                {
                    float t = horizIndex / (float)horizontalCount;
                    float u = Mathf.Lerp(queueLeftNormalizedX, queueRightNormalizedX, t);
                    return backgroundFitter.NormalizedToWorld(u, queueBarNormalizedY);
                }

                int backIndex = horizIndex - horizontalCount;
                int backTotal = Mathf.Max(1, visiblePassengers - verticalCount - horizontalCount);
                float bt = Mathf.Clamp01(backIndex / (float)backTotal);
                float bv = Mathf.Lerp(queueBarNormalizedY, queueBackNormalizedY, bt);
                return backgroundFitter.NormalizedToWorld(queueRightNormalizedX, bv);
            }

            return i < queueTurnThreshold
                ? new Vector3(queueFrontStartX + i * queueFrontSpacing, queueFrontY, 0)
                : new Vector3(queueTurnX, queueFrontY + (i - (queueTurnThreshold - 1)) * queueTurnSpacingY, 0);
        }

        private Vector3 BayPosition(int slot, int count)
        {
            if (alignToBackground && backgroundFitter != null)
            {
                Vector3 curbCenter = backgroundFitter.NormalizedToWorld(0.5f, bayCurbNormalizedY);
                float usableWidth = backgroundFitter.WorldSize.x * bayCurbUsableWidthRatio;
                float effectiveSpacing = count > 1 ? Mathf.Min(baySpacing, usableWidth / count) : 0f;
                return new Vector3(curbCenter.x + (slot - (count - 1) * .5f) * effectiveSpacing, curbCenter.y, 0f);
            }
            return new Vector3((slot - (count - 1) * .5f) * baySpacing, bayPosY, 0f);
        }

        private void Shake(float strength)
        {
            gameCamera.transform.DOKill();
            gameCamera.transform.position = new Vector3(0, 0, -10);
            gameCamera.transform.DOShakePosition(shakeDuration, strength, shakeVibrato);
        }

        private void Finish(bool won)
        {
            if (_roundOver) return;
            _roundOver = true;
            int reward = won ? winReward : 0;
            if (won) SaveData.Coins += reward;
            var result = new RoundFinishedResult(won, LevelIndex, catalog.levels.Length,
                won ? null : State.Deadlocked ? "No free bays" : "No moves left", reward);
            RoundFinished?.Invoke(result);
        }

        // Decides what follows a win: next level, or back to level 0 after the last one.
        public void AdvanceLevel() => LoadLevel(LevelIndex >= catalog.levels.Length - 1 ? 0 : LevelIndex + 1);
        public void RetryLevel() => LoadLevel(LevelIndex);

        private void StartPlaying()
        {
            _homeOpen = false;
            _paused = false;
            Time.timeScale = 1;
            if (gameplaySceneryRoot != null) gameplaySceneryRoot.SetActive(true);
            FitCamera();
            LoadLevel(Mathf.Clamp(SaveData.Level, 0, catalog.levels.Length - 1));
        }

        private void ShowHome(bool instant = false)
        {
            _homeOpen = true;
            _paused = true;
            _roundOver = false;
            Time.timeScale = 0;
            ReleaseGameplayObjects();
            State = null;
            if (gameplaySceneryRoot != null) gameplaySceneryRoot.SetActive(false);
            UI.SetCoins(SaveData.Coins);
            UI.SetHomeLevel(Mathf.Clamp(SaveData.Level, 0, catalog.levels.Length - 1));
            UI.ShowHome(instant);
        }

        private void OpenSettingsFromHome()
        {
            _paused = true;
            UI.ShowSettings(withOptions: false);
        }

        private void TogglePause(bool inGame = true)
        {
            if (_roundOver) return;
            _paused = true;
            Time.timeScale = 0;
            UI.ShowSettings(withOptions: inGame);
        }

        private void CloseSettings()
        {
            UI.CloseSettings(() =>
            {
                _paused = _homeOpen;
                Time.timeScale = _paused ? 0 : 1;
            });
        }

        private void ConfirmRetry()
        {
            CloseSettings();
            LoadLevel(LevelIndex);
        }

        private void OnDestroy()
        {
            _roundPresenter?.Dispose();
            _roundFeedback?.Dispose();
            _roundProgress?.Dispose();
            RoundFinished = null;
            Time.timeScale = 1;
            // if (UI != null) Destroy(UI.gameObject);
            if (gameCamera != null) gameCamera.transform.DOKill();
        }
    }
}
