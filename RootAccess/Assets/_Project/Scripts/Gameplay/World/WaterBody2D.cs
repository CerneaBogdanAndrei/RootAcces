using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>
    /// Dynamic water: a row of damped springs forms the surface (waves propagate to neighbours), rendered as a
    /// vertex-coloured mesh. Works with a BuoyancyEffector2D on the same object for floating bodies and notifies
    /// <see cref="IWaterAffectable"/> swimmers on enter/exit.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D), typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class WaterBody2D : MonoBehaviour
    {
        [SerializeField] private float width = 10f;
        [SerializeField] private float depth = 4f;
        [SerializeField, Range(8, 256)] private int columns = 64;
        [SerializeField] private float springStiffness = 0.025f;
        [SerializeField] private float damping = 0.03f;
        [SerializeField] private float spread = 0.2f;
        [SerializeField] private float splashScale = 0.06f;
        [SerializeField] private float density = 1f;
        [SerializeField] private Color surfaceColor = new(0.35f, 0.85f, 1f, 0.75f);
        [SerializeField] private Color deepColor = new(0.02f, 0.12f, 0.25f, 0.9f);

        private float[] _heights;
        private float[] _velocities;
        private float[] _leftDeltas;
        private float[] _rightDeltas;
        private Vector3[] _vertices;
        private Mesh _mesh;
        private BoxCollider2D _volume;
        private BuoyancyEffector2D _buoyancy;
        private readonly System.Collections.Generic.HashSet<IWaterAffectable> _swimmers = new();

        public float SurfaceY => transform.position.y + depth * 0.5f;
        public float Depth => depth;
        public float BottomY => transform.position.y - depth * 0.5f;

        private void Awake()
        {
            _volume = GetComponent<BoxCollider2D>();
            _volume.isTrigger = true;
            _volume.size = new Vector2(width, depth);
            _volume.offset = Vector2.zero;
            TryGetComponent(out _buoyancy);

            _heights = new float[columns];
            _velocities = new float[columns];
            _leftDeltas = new float[columns];
            _rightDeltas = new float[columns];
            BuildMesh();
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
        }

        public void Splash(float worldX, float velocity)
        {
            float local = worldX - (transform.position.x - width * 0.5f);
            int index = Mathf.Clamp(Mathf.RoundToInt(local / width * (columns - 1)), 0, columns - 1);
            _velocities[index] += velocity * splashScale;
            if (index > 0) _velocities[index - 1] += velocity * splashScale * 0.5f;
            if (index < columns - 1) _velocities[index + 1] += velocity * splashScale * 0.5f;
        }

        private void FixedUpdate()
        {
            for (int i = 0; i < columns; i++)
            {
                float acceleration = -springStiffness * _heights[i] - damping * _velocities[i];
                _velocities[i] += acceleration;
                _heights[i] += _velocities[i];
            }

            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < columns; i++)
                {
                    if (i > 0)
                    {
                        _leftDeltas[i] = spread * (_heights[i] - _heights[i - 1]);
                        _velocities[i - 1] += _leftDeltas[i];
                    }

                    if (i < columns - 1)
                    {
                        _rightDeltas[i] = spread * (_heights[i] - _heights[i + 1]);
                        _velocities[i + 1] += _rightDeltas[i];
                    }
                }

                for (int i = 0; i < columns; i++)
                {
                    if (i > 0) _heights[i - 1] += _leftDeltas[i];
                    if (i < columns - 1) _heights[i + 1] += _rightDeltas[i];
                }
            }

            UpdateMesh();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Rigidbody2D body = other.attachedRigidbody;
            float impact = body != null ? body.linearVelocity.y : -5f;
            Splash(other.transform.position.x, impact);
            if (Mathf.Abs(impact) > 3f)
            {
                EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.Splash, new Vector3(other.transform.position.x, SurfaceY, 0f)));
                EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.Splash, other.transform.position));
            }

            IWaterAffectable swimmer = other.GetComponentInParent<IWaterAffectable>();
            if (swimmer == null) return;
            _swimmers.Add(swimmer);
            swimmer.EnterWater(new WaterVolumeInfo(SurfaceY, density, _volume));
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            Rigidbody2D body = other.attachedRigidbody;
            if (body != null) Splash(other.transform.position.x, body.linearVelocity.y * 0.5f);
            IWaterAffectable swimmer = other.GetComponentInParent<IWaterAffectable>();
            if (swimmer == null) return;
            _swimmers.Remove(swimmer);
            swimmer.ExitWater(_volume);
        }

        /// <summary>
        /// Raises or lowers the water with the bottom kept in place (sluice / flood puzzles): moves the volume, resizes the
        /// trigger and the buoyancy surface, rebuilds the mesh and tells every swimmer inside about the new surface.
        /// </summary>
        public void SetDepth(float newDepth)
        {
            newDepth = Mathf.Max(0.2f, newDepth);
            if (Mathf.Approximately(newDepth, depth)) return;
            float bottom = BottomY;
            depth = newDepth;
            transform.position = new Vector3(transform.position.x, bottom + depth * 0.5f, transform.position.z);
            if (_volume != null) _volume.size = new Vector2(width, depth);
            if (_buoyancy != null) _buoyancy.surfaceLevel = depth * 0.5f;
            if (_mesh != null) UpdateMesh();
            foreach (IWaterAffectable swimmer in _swimmers) swimmer.UpdateWaterSurface(SurfaceY);
        }

        private void BuildMesh()
        {
            _mesh = new Mesh { name = "WaterSurface" };
            _mesh.MarkDynamic();
            _vertices = new Vector3[columns * 2];
            var colors = new Color[columns * 2];
            var triangles = new int[(columns - 1) * 6];
            for (int i = 0; i < columns; i++)
            {
                colors[i * 2] = surfaceColor;
                colors[i * 2 + 1] = deepColor;
                if (i == columns - 1) continue;
                int t = i * 6;
                int v = i * 2;
                triangles[t] = v;
                triangles[t + 1] = v + 2;
                triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 1;
                triangles[t + 4] = v + 2;
                triangles[t + 5] = v + 3;
            }

            UpdateVertices();
            _mesh.vertices = _vertices;
            _mesh.colors = colors;
            _mesh.triangles = triangles;
            _mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }

        private void UpdateVertices()
        {
            float half = width * 0.5f;
            float top = depth * 0.5f;
            for (int i = 0; i < columns; i++)
            {
                float x = -half + width * i / (columns - 1);
                _vertices[i * 2] = new Vector3(x, top + _heights[i], 0f);
                _vertices[i * 2 + 1] = new Vector3(x, -top, 0f);
            }
        }

        private void UpdateMesh()
        {
            UpdateVertices();
            _mesh.vertices = _vertices;
            _mesh.RecalculateBounds();
        }
    }
}
