using System;
using UnityEngine.InputSystem;

namespace RootAccess.Input
{
    public static class InputActionNames
    {
        public const string Jump = "Jump";
        public const string Interact = "Interact";
        public const string Dash = "Dash";
        public const string Crouch = "Crouch";
        public const string Pause = "Pause";
    }

    public static class ControlSchemes
    {
        public const string KeyboardMouse = "Keyboard&Mouse";
        public const string Gamepad = "Gamepad";
    }

    /// <summary>Interactive rebinding helpers built on Input System's RebindingOperation.</summary>
    public static class RebindService
    {
        private const string CancelPath = "<Keyboard>/escape";
        private static InputActionRebindingExtensions.RebindingOperation _current;

        public static bool IsRebinding => _current != null;

        public static int FindBindingIndex(InputAction action, string bindingGroup)
        {
            if (action == null) return -1;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                if (binding.isComposite || binding.isPartOfComposite) continue;
                if (!string.IsNullOrEmpty(binding.groups) && binding.groups.Contains(bindingGroup)) return i;
            }

            return -1;
        }

        public static string GetDisplayString(InputAction action, int bindingIndex)
        {
            if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count) return "-";
            return action.GetBindingDisplayString(bindingIndex);
        }

        public static void StartRebind(InputAction action, int bindingIndex, Action onComplete, Action onCancel = null)
        {
            if (action == null || bindingIndex < 0)
            {
                onCancel?.Invoke();
                return;
            }

            Cancel();
            bool wasEnabled = action.enabled;
            action.Disable();

            _current = action.PerformInteractiveRebinding(bindingIndex)
                .WithControlsExcluding("<Pointer>/position")
                .WithControlsExcluding("<Pointer>/delta")
                .WithControlsExcluding("<Mouse>/scroll")
                .WithCancelingThrough(CancelPath)
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(operation => Finish(operation, action, wasEnabled, onComplete))
                .OnCancel(operation => Finish(operation, action, wasEnabled, onCancel))
                .Start();
        }

        public static void Cancel() => _current?.Cancel();

        public static void ResetAll(InputActionAsset asset) => asset?.RemoveAllBindingOverrides();

        private static void Finish(InputActionRebindingExtensions.RebindingOperation operation, InputAction action, bool wasEnabled, Action callback)
        {
            operation.Dispose();
            if (ReferenceEquals(_current, operation)) _current = null;
            if (wasEnabled) action.Enable();
            callback?.Invoke();
        }
    }
}
