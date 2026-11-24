using System;
using RootAccess.Core;
using RootAccess.Input;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RootAccess.UI
{
    /// <summary>Settings row that rebinds one action for one control scheme via the Input System rebinding API.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class RebindActionButton : MonoBehaviour
    {
        [SerializeField] private string actionName = InputActionNames.Jump;
        [SerializeField] private string bindingGroup = ControlSchemes.KeyboardMouse;
        [SerializeField] private TMP_Text actionLabel;
        [SerializeField] private TMP_Text bindingLabel;
        [SerializeField] private string displayName;

        private InputReader _input;
        private Button _button;

        public event Action Rebound;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(StartRebind);
            if (actionLabel != null && !string.IsNullOrEmpty(displayName)) actionLabel.text = displayName;
        }

        public void Configure(string action, string group, string label)
        {
            actionName = action;
            bindingGroup = group;
            displayName = label;
            if (actionLabel != null) actionLabel.text = label;
        }

        public void Bind(InputReader input)
        {
            _input = input;
            RefreshLabel();
        }

        public void RefreshLabel()
        {
            if (bindingLabel == null) return;
            InputAction action = _input != null ? _input.FindAction(actionName) : null;
            bindingLabel.text = RebindService.GetDisplayString(action, RebindService.FindBindingIndex(action, bindingGroup));
        }

        private void StartRebind()
        {
            InputAction action = _input != null ? _input.FindAction(actionName) : null;
            int index = RebindService.FindBindingIndex(action, bindingGroup);
            if (action == null || index < 0)
            {
                Debug.LogWarning($"[RebindActionButton] No '{bindingGroup}' binding for action '{actionName}'.");
                return;
            }

            if (bindingLabel != null) bindingLabel.text = Strings.Settings.Rebind;
            _button.interactable = false;
            RebindService.StartRebind(action, index, OnFinished, OnFinished);
        }

        private void OnFinished()
        {
            _button.interactable = true;
            RefreshLabel();
            Rebound?.Invoke();
            EventBus<SfxRequested>.Publish(SfxRequested.Ui(SfxId.UiClick));
        }
    }
}
