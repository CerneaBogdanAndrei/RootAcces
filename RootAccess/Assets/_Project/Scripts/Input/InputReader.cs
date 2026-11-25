using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RootAccess.Input
{
    public enum InputMode
    {
        None,
        Gameplay,
        UI,
        Hacking
    }

    /// <summary>
    /// ScriptableObject facade over the generated <see cref="RootAccessInputActions"/> wrapper.
    /// Gameplay code subscribes to C# events instead of touching the Input System directly, which keeps
    /// systems testable and lets the active action map follow the game state.
    /// </summary>
    [CreateAssetMenu(menuName = "Root Access/Input Reader", fileName = "InputReader")]
    public sealed class InputReader : ScriptableObject,
        RootAccessInputActions.IPlayerActions,
        RootAccessInputActions.IUIActions,
        RootAccessInputActions.IHackingActions
    {
        private RootAccessInputActions _actions;

        public event Action<Vector2> MoveEvent;
        public event Action JumpStartedEvent;
        public event Action JumpCanceledEvent;
        public event Action DashEvent;
        public event Action<bool> CrouchEvent;
        public event Action InteractEvent;
        public event Action ThrowEvent;
        public event Action PauseEvent;
        public event Action InventoryEvent;
        public event Action DebugPanelEvent;

        public event Action UISubmitEvent;
        public event Action UICancelEvent;

        public event Action<Vector2> HackNavigateEvent;
        public event Action HackSubmitEvent;
        public event Action HackCancelEvent;
        public event Action HackClickEvent;

        public Vector2 Move { get; private set; }
        public Vector2 PointerPosition { get; private set; }
        public Vector2 AimStick { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool CrouchHeld { get; private set; }
        public InputMode Mode { get; private set; } = InputMode.None;

        public InputActionAsset Asset => EnsureActions().asset;

        private void OnEnable()
        {
            if (Application.isPlaying) EnsureActions();
        }

        private void OnDisable() => ReleaseActions();

        public void SetMode(InputMode mode)
        {
            RootAccessInputActions actions = EnsureActions();
            Mode = mode;
            ResetHeldState();

            SetMapEnabled(actions.Player.Get(), mode == InputMode.Gameplay);
            SetMapEnabled(actions.UI.Get(), mode == InputMode.UI);
            SetMapEnabled(actions.Hacking.Get(), mode == InputMode.Hacking);
        }

        public InputAction FindAction(string actionName) => Asset.FindAction(actionName, false);

        /// <summary>
        /// Local co-op: player 1 keeps keyboard + mouse and the gamepad is left to the Operator. False restores every
        /// device.
        /// </summary>
        public void RestrictToKeyboardMouse(bool restrict)
        {
            RootAccessInputActions actions = EnsureActions();
            if (!restrict)
            {
                actions.devices = null;
                return;
            }

            var devices = new System.Collections.Generic.List<InputDevice>(2);
            if (Keyboard.current != null) devices.Add(Keyboard.current);
            if (Mouse.current != null) devices.Add(Mouse.current);
            actions.devices = devices.ToArray();
        }

        public string SaveBindingOverrides() => Asset.SaveBindingOverridesAsJson();

        public void LoadBindingOverrides(string json)
        {
            Asset.RemoveAllBindingOverrides();
            if (!string.IsNullOrEmpty(json)) Asset.LoadBindingOverridesFromJson(json);
        }

        private RootAccessInputActions EnsureActions()
        {
            if (_actions != null) return _actions;
            _actions = new RootAccessInputActions();
            _actions.Player.SetCallbacks(this);
            _actions.UI.SetCallbacks(this);
            _actions.Hacking.SetCallbacks(this);
            return _actions;
        }

        private void ReleaseActions()
        {
            if (_actions == null) return;
            _actions.Disable();
            if (Application.isPlaying) _actions.Dispose();
            else DestroyImmediate(_actions.asset);
            _actions = null;
            Mode = InputMode.None;
        }

        private static void SetMapEnabled(InputActionMap map, bool enabled)
        {
            if (enabled && !map.enabled) map.Enable();
            else if (!enabled && map.enabled) map.Disable();
        }

        private void ResetHeldState()
        {
            Move = Vector2.zero;
            JumpHeld = false;
            CrouchHeld = false;
            MoveEvent?.Invoke(Vector2.zero);
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            Move = context.ReadValue<Vector2>();
            MoveEvent?.Invoke(Move);
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                JumpHeld = true;
                JumpStartedEvent?.Invoke();
            }
            else if (context.canceled)
            {
                JumpHeld = false;
                JumpCanceledEvent?.Invoke();
            }
        }

        public void OnDash(InputAction.CallbackContext context)
        {
            if (context.started) DashEvent?.Invoke();
        }

        public void OnCrouch(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                CrouchHeld = true;
                CrouchEvent?.Invoke(true);
            }
            else if (context.canceled)
            {
                CrouchHeld = false;
                CrouchEvent?.Invoke(false);
            }
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (context.started) InteractEvent?.Invoke();
        }

        public void OnThrow(InputAction.CallbackContext context)
        {
            if (context.started) ThrowEvent?.Invoke();
        }

        public void OnAimPointer(InputAction.CallbackContext context) => PointerPosition = context.ReadValue<Vector2>();

        public void OnAimStick(InputAction.CallbackContext context) => AimStick = context.ReadValue<Vector2>();

        public void OnPause(InputAction.CallbackContext context)
        {
            if (context.started) PauseEvent?.Invoke();
        }

        public void OnInventory(InputAction.CallbackContext context)
        {
            if (context.started) InventoryEvent?.Invoke();
        }

        void RootAccessInputActions.IPlayerActions.OnDebugPanel(InputAction.CallbackContext context)
        {
            if (context.started) DebugPanelEvent?.Invoke();
        }

        void RootAccessInputActions.IUIActions.OnDebugPanel(InputAction.CallbackContext context)
        {
            if (context.started) DebugPanelEvent?.Invoke();
        }

        void RootAccessInputActions.IUIActions.OnNavigate(InputAction.CallbackContext context) { }

        void RootAccessInputActions.IUIActions.OnSubmit(InputAction.CallbackContext context)
        {
            if (context.started) UISubmitEvent?.Invoke();
        }

        void RootAccessInputActions.IUIActions.OnCancel(InputAction.CallbackContext context)
        {
            if (context.started) UICancelEvent?.Invoke();
        }

        void RootAccessInputActions.IUIActions.OnPoint(InputAction.CallbackContext context) =>
            PointerPosition = context.ReadValue<Vector2>();

        void RootAccessInputActions.IUIActions.OnClick(InputAction.CallbackContext context) { }

        void RootAccessInputActions.IHackingActions.OnNavigate(InputAction.CallbackContext context)
        {
            if (context.performed) HackNavigateEvent?.Invoke(context.ReadValue<Vector2>());
        }

        void RootAccessInputActions.IHackingActions.OnSubmit(InputAction.CallbackContext context)
        {
            if (context.started) HackSubmitEvent?.Invoke();
        }

        void RootAccessInputActions.IHackingActions.OnCancel(InputAction.CallbackContext context)
        {
            if (context.started) HackCancelEvent?.Invoke();
        }

        void RootAccessInputActions.IHackingActions.OnPoint(InputAction.CallbackContext context) =>
            PointerPosition = context.ReadValue<Vector2>();

        void RootAccessInputActions.IHackingActions.OnClick(InputAction.CallbackContext context)
        {
            if (context.started) HackClickEvent?.Invoke();
        }
    }
}
