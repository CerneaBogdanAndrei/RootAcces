using RootAccess.Core;
using UnityEngine;

namespace RootAccess.VFX
{
    /// <summary>Showcase helper: cycles through VFX ids at a fixed interval so every effect can be seen in one spot.</summary>
    public sealed class VfxDemoEmitter : MonoBehaviour
    {
        [SerializeField] private VfxId[] effects = { VfxId.Sparks, VfxId.EmpExplosion, VfxId.DataStream, VfxId.LandingDust, VfxId.Splash, VfxId.Death };
        [SerializeField, Min(0.2f)] private float interval = 1.2f;

        private float _next;
        private int _index;

        private void Update()
        {
            if (effects.Length == 0 || Time.time < _next) return;
            _next = Time.time + interval;
            EventBus<VfxRequested>.Publish(new VfxRequested(effects[_index], transform.position));
            _index = (_index + 1) % effects.Length;
        }
    }
}
