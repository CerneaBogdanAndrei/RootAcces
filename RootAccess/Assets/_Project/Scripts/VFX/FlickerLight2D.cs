using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RootAccess.VFX
{
    /// <summary>Broken-neon flicker: smooth Perlin noise plus occasional hard dropouts.</summary>
    [RequireComponent(typeof(Light2D))]
    public sealed class FlickerLight2D : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float baseIntensity = 1.2f;
        [SerializeField, Range(0f, 1f)] private float noiseAmount = 0.25f;
        [SerializeField, Min(0f)] private float noiseSpeed = 6f;
        [SerializeField, Range(0f, 1f)] private float dropoutChancePerSecond = 0.35f;
        [SerializeField] private Vector2 dropoutDuration = new(0.04f, 0.18f);
        [SerializeField] private SpriteRenderer[] neonSprites = new SpriteRenderer[0];

        private Light2D _light;
        private float _seed;
        private float _dropoutUntil;
        private Color[] _spriteColors;

        private void Awake()
        {
            _light = GetComponent<Light2D>();
            _seed = Random.value * 100f;
            _spriteColors = new Color[neonSprites.Length];
            for (int i = 0; i < neonSprites.Length; i++)
                if (neonSprites[i] != null) _spriteColors[i] = neonSprites[i].color;
        }

        private void Update()
        {
            float time = Time.time;
            if (time >= _dropoutUntil && Random.value < dropoutChancePerSecond * Time.deltaTime)
                _dropoutUntil = time + Random.Range(dropoutDuration.x, dropoutDuration.y);

            bool dropped = time < _dropoutUntil;
            float noise = Mathf.PerlinNoise(_seed, time * noiseSpeed) * 2f - 1f;
            float factor = dropped ? 0.08f : 1f + noise * noiseAmount;
            _light.intensity = baseIntensity * factor;

            for (int i = 0; i < neonSprites.Length; i++)
            {
                if (neonSprites[i] == null) continue;
                Color c = _spriteColors[i];
                neonSprites[i].color = new Color(c.r * factor, c.g * factor, c.b * factor, c.a);
            }
        }
    }
}
