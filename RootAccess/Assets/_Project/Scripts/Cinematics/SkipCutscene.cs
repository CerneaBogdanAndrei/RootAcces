using RootAccess.Input;
using UnityEngine;

namespace RootAccess.Cinematics
{
    /// <summary>Lets the player skip the running cutscene with Submit or Cancel (keyboard or gamepad).</summary>
    [RequireComponent(typeof(CutsceneController))]
    public sealed class SkipCutscene : MonoBehaviour
    {
        [SerializeField] private InputReader input;
        [SerializeField, Min(0f)] private float ignoreFirstSeconds = 0.5f;

        private CutsceneController _cutscene;
        private float _startedAt;

        private void Awake() => _cutscene = GetComponent<CutsceneController>();

        private void OnEnable()
        {
            _startedAt = Time.unscaledTime;
            if (input == null) return;
            input.UISubmitEvent += OnSkipRequested;
            input.UICancelEvent += OnSkipRequested;
        }

        private void OnDisable()
        {
            if (input == null) return;
            input.UISubmitEvent -= OnSkipRequested;
            input.UICancelEvent -= OnSkipRequested;
        }

        private void OnSkipRequested()
        {
            if (Time.unscaledTime - _startedAt < ignoreFirstSeconds) return;
            _cutscene.Skip();
        }
    }
}
