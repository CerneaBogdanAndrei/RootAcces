using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Cinematics
{
    /// <summary>
    /// Target for Timeline Signal Emitters (wired through a SignalReceiver's UnityEvents): camera shake, sound
    /// cues and VFX bursts placed on the cutscene timeline.
    /// </summary>
    public sealed class CutsceneSignalReceiver : MonoBehaviour
    {
        [SerializeField] private Transform effectAnchor;

        public void Shake(float force) => EventBus<ScreenShakeRequested>.Publish(new ScreenShakeRequested(force));

        public void PlayAlarm() => EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.Alarm, AnchorPosition));

        public void PlayTeleport() => EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.TeleportIn, AnchorPosition));

        public void PlayDataStream() => EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.DataStream, AnchorPosition));

        private Vector3 AnchorPosition => effectAnchor != null ? effectAnchor.position : transform.position;
    }
}
