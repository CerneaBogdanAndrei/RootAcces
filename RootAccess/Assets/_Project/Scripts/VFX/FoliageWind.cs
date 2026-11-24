using RootAccess.Core;
using UnityEngine;

namespace RootAccess.VFX
{
    /// <summary>
    /// Script-driven wind. Publishes global shader parameters consumed by the grass wind shader
    /// (<c>_RA_Wind</c>: strength, frequency, gust, time) and the player position used to bend grass,
    /// and sways tree bones / pivots with layered sine waves.
    /// </summary>
    public sealed class FoliageWind : MonoBehaviour
    {
        [SerializeField, Range(0f, 2f)] private float strength = 0.6f;
        [SerializeField, Min(0.01f)] private float frequency = 1.3f;
        [SerializeField, Range(0f, 1f)] private float gustiness = 0.35f;
        [SerializeField] private Transform[] swayBones = new Transform[0];
        [SerializeField] private float boneMaxAngle = 4f;

        private Quaternion[] _restRotations;
        private float _externalBoost;

        public float Strength
        {
            get => strength;
            set => strength = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            _restRotations = new Quaternion[swayBones.Length];
            for (int i = 0; i < swayBones.Length; i++)
                if (swayBones[i] != null) _restRotations[i] = swayBones[i].localRotation;
        }

        public void AddGust(float amount) => _externalBoost = Mathf.Max(_externalBoost, amount);

        private void Update()
        {
            float time = Time.time;
            float gust = 1f + gustiness * Mathf.PerlinNoise(time * 0.25f, 3.7f) + _externalBoost;
            _externalBoost = Mathf.MoveTowards(_externalBoost, 0f, Time.deltaTime);
            Shader.SetGlobalVector(ShaderIds.GlobalWind, new Vector4(strength * gust, frequency, gustiness, time));

            if (ServiceLocator.TryGet(out IPlayerTarget player))
            {
                Vector2 p = player.Position;
                Shader.SetGlobalVector(ShaderIds.GlobalPlayerPosition, new Vector4(p.x, p.y, Mathf.Abs(player.Velocity.x), 1f));
            }

            for (int i = 0; i < swayBones.Length; i++)
            {
                Transform bone = swayBones[i];
                if (bone == null) continue;
                float phase = i * 0.7f + bone.position.x * 0.15f;
                float angle = Mathf.Sin(time * frequency + phase) * boneMaxAngle * strength * gust
                              + Mathf.Sin(time * frequency * 2.3f + phase) * boneMaxAngle * 0.3f * strength;
                bone.localRotation = _restRotations[i] * Quaternion.Euler(0f, 0f, angle);
            }
        }
    }
}
