using RootAccess.Core;
using UnityEngine;

namespace RootAccess.VFX
{
    /// <summary>Local co-op: the gameplay camera renders only the left half while the Operator console owns the right.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class SplitViewport : MonoBehaviour
    {
        private static readonly Rect Full = new(0f, 0f, 1f, 1f);
        private static readonly Rect LeftHalf = new(0f, 0f, 0.5f, 1f);

        private Camera _camera;

        private void Awake() => _camera = GetComponent<Camera>();

        private void OnEnable()
        {
            EventBus<CoopViewChanged>.Subscribe(OnCoopView);
            if (ServiceLocator.TryGet(out IOperatorConsole console) && console.Mode == CoopMode.LocalSplit) Apply(true);
        }

        private void OnDisable() => EventBus<CoopViewChanged>.Unsubscribe(OnCoopView);

        private void OnCoopView(CoopViewChanged evt) => Apply(evt.Split);

        private void Apply(bool split) => _camera.rect = split ? LeftHalf : Full;
    }
}
