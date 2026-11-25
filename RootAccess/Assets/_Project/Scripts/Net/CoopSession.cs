using System;
using RootAccess.Core;
using RootAccess.Input;
using RootAccess.Systems;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace RootAccess.Net
{
    /// <summary>
    /// Asymmetric co-op (Infiltrator + Operator) for both setups, with one shared Operator code path:
    /// <list type="bullet">
    /// <item><b>LocalSplit</b>: same PC. The gameplay camera takes the left half, the Operator console the right half;
    /// player 1 keeps keyboard + mouse, the Operator uses a gamepad. Link: <see cref="LocalOperatorLink"/>.</item>
    /// <item><b>LanHost / LanClient</b>: two PCs. The host plays the level and is authoritative; the client runs only the
    /// Operator console and talks to the host through Netcode named messages (<see cref="NetworkOperatorLink"/>).</item>
    /// </list>
    /// The infiltrator side (local or host) publishes snapshots ~10×/s and applies commands within the
    /// <see cref="OperatorModel"/> energy budget; the operator side (local or client) stores what it receives for the UI.
    /// </summary>
    public sealed class CoopSession : MonoBehaviour, IOperatorConsole
    {
        public const ushort DefaultPort = 7777;
        private const int MaxMapCells = 64 * 1024;

        [SerializeField] private InputReader input;
        [SerializeField] private ushort port = DefaultPort;
        [SerializeField, Min(0.02f)] private float snapshotInterval = 0.1f;

        private readonly OperatorModel _model = new();
        private readonly OperatorSnapshot _outgoing = new();
        private IOperatorLink _link;
        private NetworkManager _network;
        private float _nextSnapshot;
        private bool _inGameplay;
        private bool _split;

        public CoopMode Mode { get; private set; }
        public bool IsLinked => _link != null && _link.IsConnected;
        public string Status { get; private set; } = string.Empty;
        public OperatorSnapshot LatestSnapshot { get; private set; }
        public OperatorLevelMap LatestMap { get; private set; }
        public OperatorModel Model => _model;
        public ushort Port => port;

        public event Action Changed;

        private bool IsInfiltratorSide => Mode is CoopMode.LocalSplit or CoopMode.LanHost;

        private void Awake()
        {
            ServiceLocator.Register(this);
            ServiceLocator.Register<IOperatorConsole>(this);
        }

        private void OnEnable()
        {
            EventBus<SceneLoadStarted>.Subscribe(OnSceneLoadStarted);
            EventBus<SceneLoadCompleted>.Subscribe(OnSceneLoaded);
        }

        private void OnDisable()
        {
            EventBus<SceneLoadStarted>.Unsubscribe(OnSceneLoadStarted);
            EventBus<SceneLoadCompleted>.Unsubscribe(OnSceneLoaded);
        }

        private void OnDestroy()
        {
            Stop();
            ServiceLocator.Unregister(this);
            ServiceLocator.Unregister<IOperatorConsole>(this);
        }

        // ---------------------------------------------------------------- modes

        public void StartLocal()
        {
            Stop();
            Mode = CoopMode.LocalSplit;
            Attach(new LocalOperatorLink());
            _model.Refill();
            SetStatus(Strings.Coop.LocalStatus);
            RefreshView();
        }

        public bool Host()
        {
            Stop();
            _network = CreateNetworkManager(out UnityTransport transport);
            transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
            if (!_network.StartHost())
            {
                Fail(Strings.Coop.HostFailed);
                return false;
            }

            Mode = CoopMode.LanHost;
            Attach(new NetworkOperatorLink(_network));
            _network.OnClientConnectedCallback += OnPeerConnected;
            _network.OnClientDisconnectCallback += OnPeerDisconnected;
            _model.Refill();
            SetStatus(string.Format(Strings.Coop.HostWaiting, port));
            Debug.Log($"[CoopSession] LAN host listening on port {port}.");
            return true;
        }

        public bool Join(string address)
        {
            Stop();
            if (string.IsNullOrWhiteSpace(address)) address = "127.0.0.1";
            _network = CreateNetworkManager(out UnityTransport transport);
            transport.SetConnectionData(address.Trim(), port);
            if (!_network.StartClient())
            {
                Fail(Strings.Coop.JoinFailed);
                return false;
            }

            Mode = CoopMode.LanClient;
            Attach(new NetworkOperatorLink(_network));
            _network.OnClientConnectedCallback += OnPeerConnected;
            _network.OnClientDisconnectCallback += OnPeerDisconnected;
            SetStatus(string.Format(Strings.Coop.Connecting, address, port));
            Debug.Log($"[CoopSession] Joining {address}:{port} as Operator.");
            return true;
        }

        public void Stop()
        {
            if (Mode == CoopMode.None && _link == null && _network == null) return;
            if (_link != null)
            {
                _link.SnapshotReceived -= OnSnapshotReceived;
                _link.LevelMapReceived -= OnLevelMapReceived;
                _link.CommandReceived -= OnCommandReceived;
                _link.Dispose();
                _link = null;
            }

            if (_network != null)
            {
                _network.OnClientConnectedCallback -= OnPeerConnected;
                _network.OnClientDisconnectCallback -= OnPeerDisconnected;
                if (_network.IsListening) _network.Shutdown();
                Destroy(_network.gameObject);
                _network = null;
            }

            Mode = CoopMode.None;
            LatestSnapshot = null;
            LatestMap = null;
            SetStatus(string.Empty);
            RefreshView();
        }

        /// <summary>Operator side: ask the infiltrator's game to act on a target (goes through the link either way).</summary>
        public void SendCommand(string targetId)
        {
            if (_link == null || string.IsNullOrEmpty(targetId)) return;
            _link.SendCommand(new OperatorCommand(targetId));
        }

        // ---------------------------------------------------------------- infiltrator side

        private void Update()
        {
            if (!IsInfiltratorSide || !_inGameplay || _link == null) return;
            _model.Tick(Time.deltaTime);
            if (Time.unscaledTime < _nextSnapshot) return;
            _nextSnapshot = Time.unscaledTime + snapshotInterval;
            if (!ServiceLocator.TryGet(out IPlayerTarget player)) return;
            BuildSnapshot(player);
            _link.SendSnapshot(_outgoing);
        }

        private void BuildSnapshot(IPlayerTarget player)
        {
            _outgoing.PlayerPosition = player.Position;
            _outgoing.Energy = _model.Energy;
            _outgoing.MaxEnergy = _model.MaxEnergy;
            _outgoing.Entities.Clear();
            foreach (IOperatorTarget target in OperatorRegistry.All)
                _outgoing.Entities.Add(new OperatorEntity(target.OperatorId, target.OperatorKind, target.OperatorPosition, target.OperatorActive,
                    target.CanOperate && _model.CanAfford(target.OperatorKind)));
        }

        private void OnCommandReceived(OperatorCommand command)
        {
            if (!IsInfiltratorSide) return;
            IOperatorTarget target = OperatorRegistry.Find(command.TargetId);
            if (target == null || !target.CanOperate) return;
            if (!_model.TrySpend(target.OperatorKind))
            {
                EventBus<ToastRequested>.Publish(new ToastRequested(Strings.Coop.NoEnergy));
                return;
            }

            target.Operate();
            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.HackSuccess, target.OperatorPosition));
            EventBus<ToastRequested>.Publish(new ToastRequested(string.Format(Strings.Coop.OperatorActed, KindLabel(target.OperatorKind))));
            Debug.Log($"[CoopSession] Operator acted on {target.OperatorKind} {target.OperatorId} (energy {_model.Energy:0}).");
            _nextSnapshot = 0f;
        }

        /// <summary>1×1 occupancy grid of walls and floors inside the level bounds, for the Operator's map.</summary>
        private static OperatorLevelMap BuildLevelMap(string sceneName)
        {
            Rect bounds = ServiceLocator.TryGet(out ILevelInfo level) ? level.Bounds : new Rect(-10f, -5f, 20f, 15f);
            int width = Mathf.Clamp(Mathf.CeilToInt(bounds.width), 1, 1024);
            int height = Mathf.Clamp(Mathf.CeilToInt(bounds.height), 1, MaxMapCells / width);
            var solid = new bool[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                solid[y * width + x] = Physics2D.OverlapPoint(new Vector2(bounds.xMin + x + 0.5f, bounds.yMin + y + 0.5f), Layers.ObstacleMask) != null;
            return new OperatorLevelMap(sceneName, new Rect(bounds.position, new Vector2(width, height)), width, height, solid);
        }

        // ---------------------------------------------------------------- operator side

        private void OnSnapshotReceived(OperatorSnapshot snapshot)
        {
            LatestSnapshot = snapshot;
            Changed?.Invoke();
        }

        private void OnLevelMapReceived(OperatorLevelMap map)
        {
            LatestMap = map;
            if (Mode == CoopMode.LanClient) SetStatus(string.Format(Strings.Coop.Connected, map.LevelName));
            Changed?.Invoke();
        }

        // ---------------------------------------------------------------- plumbing

        private void Attach(IOperatorLink link)
        {
            _link = link;
            _link.SnapshotReceived += OnSnapshotReceived;
            _link.LevelMapReceived += OnLevelMapReceived;
            _link.CommandReceived += OnCommandReceived;
        }

        private static NetworkManager CreateNetworkManager(out UnityTransport transport)
        {
            // NetworkManager must live on a root object; it survives scene loads like the rest of the systems.
            var go = new GameObject("NetworkManager");
            DontDestroyOnLoad(go);
            transport = go.AddComponent<UnityTransport>();
            var manager = go.AddComponent<NetworkManager>();
            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                EnableSceneManagement = false,
                ConnectionApproval = false
            };
            return manager;
        }

        private void OnSceneLoadStarted(SceneLoadStarted evt)
        {
            // Leaving for the main menu ends the co-op session (the menu starts a new one if asked).
            if (evt.SceneName == SceneNames.MainMenu) Stop();
        }

        private void OnSceneLoaded(SceneLoadCompleted evt)
        {
            _inGameplay = SceneNames.IsGameplayScene(evt.SceneName);
            if (IsInfiltratorSide && _inGameplay)
            {
                OperatorLevelMap map = BuildLevelMap(evt.SceneName);
                _link?.SendLevelMap(map);
                _nextSnapshot = 0f;
            }

            RefreshView();
        }

        private void OnPeerConnected(ulong clientId)
        {
            if (Mode == CoopMode.LanHost && clientId != NetworkManager.ServerClientId) SetStatus(Strings.Coop.OperatorJoined);
            if (Mode == CoopMode.LanClient) SetStatus(Strings.Coop.WaitingForMap);
            Debug.Log($"[CoopSession] Peer connected: {clientId}.");
        }

        private void OnPeerDisconnected(ulong clientId)
        {
            Debug.Log($"[CoopSession] Peer disconnected: {clientId}.");
            if (Mode == CoopMode.LanHost) SetStatus(string.Format(Strings.Coop.HostWaiting, port));
            if (Mode == CoopMode.LanClient) SetStatus(Strings.Coop.Disconnected);
        }

        /// <summary>Split-screen only while a level is on screen in local mode; player 1 then keeps keyboard + mouse only.</summary>
        private void RefreshView()
        {
            bool split = Mode == CoopMode.LocalSplit && _inGameplay;
            if (input != null) input.RestrictToKeyboardMouse(Mode == CoopMode.LocalSplit);
            if (split == _split) return;
            _split = split;
            EventBus<CoopViewChanged>.Publish(new CoopViewChanged(split));
        }

        private void Fail(string message)
        {
            Debug.LogWarning($"[CoopSession] {message}");
            if (_network != null) Destroy(_network.gameObject);
            _network = null;
            Mode = CoopMode.None;
            SetStatus(message);
        }

        private void SetStatus(string status)
        {
            Status = status;
            Changed?.Invoke();
        }

        private static string KindLabel(OperatorTargetKind kind) => kind switch
        {
            OperatorTargetKind.Door => Strings.Coop.Door,
            OperatorTargetKind.Camera => Strings.Coop.Camera,
            _ => Strings.Coop.Guard
        };
    }
}
