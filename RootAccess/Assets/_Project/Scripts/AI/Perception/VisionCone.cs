using RootAccess.Core;
using UnityEngine;

namespace RootAccess.AI
{
    /// <summary>
    /// Raycast-based field of view along the local +X axis. <see cref="CanSee"/> is the gameplay query; the cone
    /// mesh (rebuilt from occlusion rays every frame, allocation-free) is the player-facing telegraph.
    /// </summary>
    public sealed class VisionCone : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float viewDistance = 7f;
        [SerializeField, Range(5f, 180f)] private float viewAngle = 60f;
        [SerializeField, Range(4, 64)] private int rayCount = 24;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private Color calmColor = new(1f, 0.95f, 0.4f, 0.18f);
        [SerializeField] private Color alertColor = new(1f, 0.2f, 0.25f, 0.32f);
        [SerializeField] private Vector2 targetOffset = new(0f, 0.35f);

        private Mesh _mesh;
        private Vector3[] _vertices;
        private Color[] _colors;
        private int[] _triangles;
        private float _alert;
        private bool _active = true;

        public float ViewDistance => viewDistance;
        public float ViewAngle => viewAngle;
        public Vector2 Forward => transform.right;
        public bool Active => _active;

        private void Awake()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            _vertices = new Vector3[rayCount + 2];
            _colors = new Color[rayCount + 2];
            _triangles = new int[rayCount * 3];
            for (int i = 0; i < rayCount; i++)
            {
                _triangles[i * 3] = 0;
                _triangles[i * 3 + 1] = i + 1;
                _triangles[i * 3 + 2] = i + 2;
            }

            if (meshFilter == null) return;
            _mesh = new Mesh { name = "VisionCone" };
            _mesh.MarkDynamic();
            meshFilter.sharedMesh = _mesh;
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
        }

        public void Configure(float distance, float angle)
        {
            viewDistance = Mathf.Max(0.5f, distance);
            viewAngle = Mathf.Clamp(angle, 5f, 180f);
        }

        public void SetActive(bool active)
        {
            _active = active;
            if (meshFilter != null && meshFilter.TryGetComponent(out MeshRenderer meshRenderer)) meshRenderer.enabled = active;
        }

        /// <summary>0 = calm colour, 1 = alert colour.</summary>
        public void SetAlert(float alert01) => _alert = Mathf.Clamp01(alert01);

        public bool CanSee(IPlayerTarget target, out float normalizedDistance)
        {
            normalizedDistance = 1f;
            if (!_active || target == null || !target.IsAlive || target.IsHidden) return false;

            Vector2 origin = transform.position;
            Vector2 toTarget = target.Position + targetOffset - origin;
            float distance = toTarget.magnitude;
            if (distance > viewDistance || distance < 0.001f) return false;
            if (Vector2.Angle(Forward, toTarget) > viewAngle * 0.5f) return false;
            if (Physics2D.Linecast(origin, origin + toTarget, Layers.ObstacleMask).collider != null) return false;

            normalizedDistance = distance / viewDistance;
            return true;
        }

        private void LateUpdate()
        {
            if (_mesh == null || !_active) return;
            Vector2 origin = transform.position;
            float halfAngle = viewAngle * 0.5f;
            float baseAngle = Mathf.Atan2(Forward.y, Forward.x) * Mathf.Rad2Deg;
            Color color = Color.Lerp(calmColor, alertColor, _alert);
            Color edge = new(color.r, color.g, color.b, 0f);

            _vertices[0] = Vector3.zero;
            _colors[0] = color;
            for (int i = 0; i <= rayCount; i++)
            {
                float angle = (baseAngle - halfAngle + viewAngle * i / rayCount) * Mathf.Deg2Rad;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                RaycastHit2D hit = Physics2D.Raycast(origin, direction, viewDistance, Layers.ObstacleMask);
                Vector2 end = hit.collider != null ? hit.point : origin + direction * viewDistance;
                _vertices[i + 1] = transform.InverseTransformPoint(end);
                _colors[i + 1] = Color.Lerp(color, edge, hit.collider != null ? hit.fraction : 1f);
            }

            _mesh.Clear();
            _mesh.vertices = _vertices;
            _mesh.colors = _colors;
            _mesh.triangles = _triangles;
            _mesh.RecalculateBounds();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 origin = transform.position;
            float baseAngle = Mathf.Atan2(transform.right.y, transform.right.x) * Mathf.Rad2Deg;
            for (int sign = -1; sign <= 1; sign += 2)
            {
                float angle = (baseAngle + sign * viewAngle * 0.5f) * Mathf.Deg2Rad;
                Gizmos.DrawLine(origin, origin + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * viewDistance);
            }
        }
    }
}
