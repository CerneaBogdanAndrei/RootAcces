using System;
using RootAccess.Core;
using UnityEngine;

namespace RootAccess.VFX
{
    /// <summary>
    /// Drives the <c>_DissolveAmount</c> property of the RootAccess dissolve shader on a set of renderers via
    /// MaterialPropertyBlock (no material instances). Used for teleports, hacked objects and death.
    /// </summary>
    public sealed class DissolveController : MonoBehaviour
    {
        [SerializeField] private Renderer[] renderers = new Renderer[0];
        [SerializeField, Min(0.01f)] private float defaultDuration = 0.45f;
        [SerializeField, ColorUsage(true, true)] private Color edgeColor = new(0.2f, 2.5f, 3f, 1f);

        private MaterialPropertyBlock _block;
        private float _from;
        private float _to;
        private float _duration;
        private float _elapsed;
        private bool _running;
        private Action _onComplete;

        public float Amount { get; private set; }
        public bool IsRunning => _running;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            if (renderers.Length == 0) renderers = GetComponentsInChildren<Renderer>(true);
            Apply(0f);
        }

        public void DissolveOut(Action onComplete = null, float duration = -1f) => Play(Amount, 1f, duration, onComplete);

        public void DissolveIn(Action onComplete = null, float duration = -1f) => Play(Amount, 0f, duration, onComplete);

        public void SetImmediate(float amount)
        {
            _running = false;
            Apply(Mathf.Clamp01(amount));
        }

        private void Play(float from, float to, float duration, Action onComplete)
        {
            _from = from;
            _to = to;
            _duration = duration > 0f ? duration : defaultDuration;
            _elapsed = 0f;
            _onComplete = onComplete;
            _running = true;
        }

        private void Update()
        {
            if (!_running) return;
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            Apply(Mathf.Lerp(_from, _to, t * t * (3f - 2f * t)));
            if (t < 1f) return;

            _running = false;
            Action callback = _onComplete;
            _onComplete = null;
            callback?.Invoke();
        }

        private void Apply(float amount)
        {
            Amount = amount;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer target = renderers[i];
                if (target == null) continue;
                target.GetPropertyBlock(_block);
                _block.SetFloat(ShaderIds.DissolveAmount, amount);
                _block.SetColor(ShaderIds.EdgeColor, edgeColor);
                target.SetPropertyBlock(_block);
            }
        }
    }
}
