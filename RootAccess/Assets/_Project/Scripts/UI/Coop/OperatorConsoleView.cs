using System.Collections.Generic;
using RootAccess.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RootAccess.UI
{
    /// <summary>
    /// The Operator's screen, identical in both co-op setups: a schematic map of the level (walls from the level map,
    /// icons for the infiltrator, doors, cameras and guards), the targets nearest to the infiltrator with their action
    /// and cost, and the energy bar. Split-screen: right half, driven by a gamepad (or the numpad). LAN client: the
    /// whole screen, driven by mouse / keyboard / gamepad through the regular UI navigation.
    /// </summary>
    public sealed class OperatorConsoleView : UIPanel
    {
        private const int MaxListed = 8;

        [SerializeField] private RectTransform frame;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private Image energyFill;
        [SerializeField] private TMP_Text energyLabel;
        [SerializeField] private RawImage terrain;
        [SerializeField] private RectTransform iconLayer;
        [SerializeField] private Image iconTemplate;
        [SerializeField] private RectTransform listRoot;
        [SerializeField] private Button entryTemplate;
        [SerializeField] private TMP_Text emptyLabel;
        [SerializeField] private TMP_Text hintLabel;
        [SerializeField] private Button leaveButton;
        [SerializeField] private Vector2 mapArea = new(880f, 300f);
        [SerializeField] private Color wallColor = new(0.3f, 0.36f, 0.55f, 1f);
        [SerializeField] private Color playerColor = new(0.3f, 0.95f, 1f);
        [SerializeField] private Color doorColor = new(0.35f, 1f, 0.55f);
        [SerializeField] private Color cameraColor = new(1f, 0.85f, 0.3f);
        [SerializeField] private Color guardColor = new(1f, 0.3f, 0.35f);
        [SerializeField] private Color unavailableColor = new(0.5f, 0.52f, 0.62f);

        private readonly List<OperatorEntity> _sorted = new();
        private readonly List<Image> _icons = new();
        private readonly List<Button> _entries = new();
        private IOperatorConsole _console;
        private OperatorLevelMap _drawnMap;
        private Texture2D _terrainTexture;
        private Image _playerIcon;
        private string _selectedId;
        private bool _dirty;
        private bool _shownSplit;

        public int TargetCount => _sorted.Count;
        public string SelectedId => _selectedId;
        public IReadOnlyList<OperatorEntity> Targets => _sorted;

        protected override void Awake()
        {
            base.Awake();
            if (iconTemplate != null) iconTemplate.gameObject.SetActive(false);
            if (entryTemplate != null) entryTemplate.gameObject.SetActive(false);
            if (leaveButton != null) leaveButton.onClick.AddListener(Leave);
        }

        private void OnEnable()
        {
            CommandLineAutomation.CommandRequested += OnAutomationCommand;
            _dirty = true;
        }

        private void OnDisable()
        {
            CommandLineAutomation.CommandRequested -= OnAutomationCommand;
            if (_console != null) _console.Changed -= OnConsoleChanged;
            _console = null;
        }

        private void OnDestroy()
        {
            if (_terrainTexture != null) Destroy(_terrainTexture);
        }

        // ---------------------------------------------------------------- public API (UI, gamepad, tests)

        public void Select(int index)
        {
            if (index < 0 || index >= _sorted.Count) return;
            _selectedId = _sorted[index].Id;
            _dirty = true;
        }

        public void SelectNext(int step)
        {
            if (_sorted.Count == 0) return;
            int current = Mathf.Max(0, IndexOf(_selectedId));
            Select((current + step + _sorted.Count) % _sorted.Count);
        }

        public void ExecuteSelected()
        {
            if (_console == null || string.IsNullOrEmpty(_selectedId)) return;
            _console.SendCommand(_selectedId);
            EventBus<SfxRequested>.Publish(SfxRequested.Ui(SfxId.UiClick));
        }

        // ---------------------------------------------------------------- loop

        private void Update()
        {
            if (_console == null && ServiceLocator.TryGet(out _console)) _console.Changed += OnConsoleChanged;
            bool split = _console != null && _console.Mode == CoopMode.LocalSplit && IsGameplayOnScreen();
            bool full = _console != null && _console.Mode == CoopMode.LanClient;
            UpdateVisibility(split, full);
            if (!IsVisible) return;

            if (split && !IsTimeStopped()) PollLocalOperatorInput();
            if (!_dirty) return;
            _dirty = false;
            Render();
        }

        /// <summary>
        /// Pause and the personal terminal stop time: the console stays visible under the Infiltrator's menu (drawn above
        /// it), but the Operator cannot act until play resumes.
        /// </summary>
        private static bool IsTimeStopped() =>
            ServiceLocator.TryGet(out GameManager gameManager) && gameManager.State is GameState.Paused or GameState.Terminal;

        private static bool IsGameplayOnScreen() =>
            ServiceLocator.TryGet(out GameManager gameManager) && gameManager.State is GameState.Playing or GameState.Hacking or GameState.Dialogue
                or GameState.Terminal or GameState.Paused;

        private void UpdateVisibility(bool split, bool full)
        {
            bool shouldShow = split || full;
            if (shouldShow && (!IsVisible || _shownSplit != split))
            {
                _shownSplit = split;
                if (frame != null)
                {
                    frame.anchorMin = new Vector2(split ? 0.5f : 0f, 0f);
                    frame.anchorMax = Vector2.one;
                }

                if (leaveButton != null) leaveButton.gameObject.SetActive(full);
                if (hintLabel != null) hintLabel.text = split ? Strings.Coop.LocalHint : string.Empty;
                _dirty = true;
                Show();
            }
            else if (!shouldShow && IsVisible)
            {
                Hide();
            }
        }

        /// <summary>Split-screen: the Operator's gamepad (player 1 is restricted to keyboard + mouse) or the numpad.</summary>
        private void PollLocalOperatorInput()
        {
            Gamepad pad = Gamepad.current;
            Keyboard keys = Keyboard.current;
            bool up = (pad != null && pad.dpad.up.wasPressedThisFrame) || (keys != null && keys.numpad8Key.wasPressedThisFrame);
            bool down = (pad != null && pad.dpad.down.wasPressedThisFrame) || (keys != null && keys.numpad2Key.wasPressedThisFrame);
            bool act = (pad != null && pad.buttonSouth.wasPressedThisFrame) || (keys != null && keys.numpad5Key.wasPressedThisFrame);
            if (up) SelectNext(-1);
            if (down) SelectNext(1);
            if (act) ExecuteSelected();
        }

        private void OnConsoleChanged() => _dirty = true;

        private void OnAutomationCommand(string command)
        {
            if (command != AutomationCommands.OperatorAct) return;
            int door = _sorted.FindIndex(e => e.Kind == OperatorTargetKind.Door && e.Available);
            Select(door >= 0 ? door : 0);
            ExecuteSelected();
            Debug.Log($"[OperatorConsoleView] Automation: acted on {_selectedId ?? "nothing"} ({_sorted.Count} targets).");
        }

        private void Leave()
        {
            _console?.Stop();
            if (ServiceLocator.TryGet(out GameManager gameManager)) gameManager.ReturnToMainMenu();
        }

        // ---------------------------------------------------------------- rendering

        private void Render()
        {
            if (statusLabel != null) statusLabel.text = _console != null ? _console.Status : string.Empty;
            OperatorSnapshot snapshot = _console?.LatestSnapshot;
            OperatorLevelMap map = _console?.LatestMap;
            if (map != null && !ReferenceEquals(map, _drawnMap)) DrawTerrain(map);

            _sorted.Clear();
            if (snapshot != null)
            {
                _sorted.AddRange(snapshot.Entities);
                Vector2 player = snapshot.PlayerPosition;
                _sorted.Sort((a, b) => (a.Position - player).sqrMagnitude.CompareTo((b.Position - player).sqrMagnitude));
                if (energyFill != null) energyFill.fillAmount = snapshot.MaxEnergy > 0f ? snapshot.Energy / snapshot.MaxEnergy : 0f;
                if (energyLabel != null) energyLabel.text = string.Format(Strings.Coop.Energy, Mathf.FloorToInt(snapshot.Energy), Mathf.RoundToInt(snapshot.MaxEnergy));
            }

            if (_sorted.Count > 0 && IndexOf(_selectedId) < 0) _selectedId = _sorted[0].Id;
            if (emptyLabel != null)
                emptyLabel.text = snapshot == null ? Strings.Coop.NoSignal : _sorted.Count == 0 ? Strings.Coop.NoTargets : string.Empty;
            DrawIcons(snapshot, map);
            DrawList(snapshot);
        }

        private void DrawTerrain(OperatorLevelMap map)
        {
            _drawnMap = map;
            if (terrain == null) return;
            if (_terrainTexture != null) Destroy(_terrainTexture);
            _terrainTexture = new Texture2D(map.Width, map.Height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[map.Width * map.Height];
            Color32 wall = wallColor;
            Color32 air = new(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = map.Solid[i] ? wall : air;
            _terrainTexture.SetPixels32(pixels);
            _terrainTexture.Apply();
            terrain.texture = _terrainTexture;

            float scale = Mathf.Min(mapArea.x / map.Width, mapArea.y / map.Height);
            var size = new Vector2(map.Width * scale, map.Height * scale);
            terrain.rectTransform.sizeDelta = size;
            if (iconLayer != null) iconLayer.sizeDelta = size;
        }

        private void DrawIcons(OperatorSnapshot snapshot, OperatorLevelMap map)
        {
            int used = 0;
            if (snapshot != null && map != null && iconLayer != null && iconTemplate != null)
            {
                foreach (OperatorEntity entity in _sorted)
                {
                    Image icon = Icon(used++);
                    icon.color = entity.Available ? ColorOf(entity.Kind, entity.Active) : unavailableColor;
                    Place(icon.rectTransform, map, entity.Position, entity.Id == _selectedId ? 18f : 11f);
                }

                _playerIcon ??= Icon(-1);
                _playerIcon.gameObject.SetActive(true);
                _playerIcon.color = playerColor;
                Place(_playerIcon.rectTransform, map, snapshot.PlayerPosition, 14f);
                _playerIcon.transform.SetAsLastSibling();
            }
            else if (_playerIcon != null)
            {
                _playerIcon.gameObject.SetActive(false);
            }

            for (int i = used; i < _icons.Count; i++) _icons[i].gameObject.SetActive(false);
        }

        private void DrawList(OperatorSnapshot snapshot)
        {
            int count = Mathf.Min(MaxListed, _sorted.Count);
            for (int i = 0; i < count; i++)
            {
                Button entry = Entry(i);
                OperatorEntity entity = _sorted[i];
                TMP_Text label = entry.GetComponentInChildren<TMP_Text>(true);
                if (label == null) continue;
                float distance = snapshot != null ? Vector2.Distance(entity.Position, snapshot.PlayerPosition) : 0f;
                label.text = $"{(entity.Id == _selectedId ? "> " : string.Empty)}{KindName(entity.Kind)} · {StateName(entity)} · " +
                             $"{string.Format(Strings.Coop.Distance, distance)} · {ActionName(entity.Kind)}";
                label.color = entity.Available ? ColorOf(entity.Kind, false) : unavailableColor;
                entry.interactable = entity.Available;
            }

            for (int i = count; i < _entries.Count; i++) _entries[i].gameObject.SetActive(false);
        }

        private Image Icon(int index)
        {
            if (index >= 0 && index < _icons.Count)
            {
                _icons[index].gameObject.SetActive(true);
                return _icons[index];
            }

            Image icon = Instantiate(iconTemplate, iconLayer);
            icon.gameObject.SetActive(true);
            if (index >= 0) _icons.Add(icon);
            return icon;
        }

        private Button Entry(int index)
        {
            while (_entries.Count <= index)
            {
                Button entry = Instantiate(entryTemplate, listRoot);
                int slot = _entries.Count;
                entry.onClick.AddListener(() =>
                {
                    Select(slot);
                    ExecuteSelected();
                });
                _entries.Add(entry);
            }

            _entries[index].gameObject.SetActive(true);
            return _entries[index];
        }

        private void Place(RectTransform icon, OperatorLevelMap map, Vector2 world, float size)
        {
            Vector2 normalized = Rect.PointToNormalized(map.Bounds, world);
            Vector2 layer = iconLayer.sizeDelta;
            icon.anchorMin = icon.anchorMax = Vector2.zero;
            icon.anchoredPosition = new Vector2(normalized.x * layer.x, normalized.y * layer.y);
            icon.sizeDelta = new Vector2(size, size);
        }

        private int IndexOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return -1;
            for (int i = 0; i < _sorted.Count; i++)
                if (_sorted[i].Id == id) return i;
            return -1;
        }

        private Color ColorOf(OperatorTargetKind kind, bool active) => kind switch
        {
            OperatorTargetKind.Door => active ? doorColor : Color.Lerp(doorColor, guardColor, 0.6f),
            OperatorTargetKind.Camera => active ? unavailableColor : cameraColor,
            _ => active ? Color.Lerp(guardColor, cameraColor, 0.4f) : guardColor
        };

        private static string KindName(OperatorTargetKind kind) => kind switch
        {
            OperatorTargetKind.Door => Strings.Coop.TargetDoor,
            OperatorTargetKind.Camera => Strings.Coop.TargetCamera,
            _ => Strings.Coop.TargetGuard
        };

        private static string StateName(OperatorEntity entity) => entity.Kind switch
        {
            OperatorTargetKind.Door => entity.Active ? Strings.Coop.StateOpen : Strings.Coop.StateClosed,
            OperatorTargetKind.Camera => entity.Active ? Strings.Coop.StateLooped : Strings.Coop.StateWatching,
            _ => entity.Active ? Strings.Coop.StateAlert : Strings.Coop.StateCalm
        };

        private static string ActionName(OperatorTargetKind kind)
        {
            int cost = Mathf.RoundToInt(OperatorModel.CostOf(kind));
            return kind switch
            {
                OperatorTargetKind.Door => string.Format(Strings.Coop.ActionDoor, cost),
                OperatorTargetKind.Camera => string.Format(Strings.Coop.ActionCamera, cost),
                _ => string.Format(Strings.Coop.ActionGuard, cost)
            };
        }
    }
}
