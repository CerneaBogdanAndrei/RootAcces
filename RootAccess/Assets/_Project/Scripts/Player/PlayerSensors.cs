using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Player
{
    /// <summary>Raycast/boxcast probes for ground, walls and ceiling. Allocation-free (struct-returning queries).</summary>
    public sealed class PlayerSensors
    {
        private const float SkinWidth = 0.02f;
        private const float MinGroundNormalY = 0.6f;
        private const float MinWallNormalX = 0.85f;

        private readonly CapsuleCollider2D _capsule;
        private readonly PlayerMovementConfig _config;

        public PlayerSensors(CapsuleCollider2D capsule, PlayerMovementConfig config)
        {
            _capsule = capsule;
            _config = config;
        }

        public bool IsGrounded { get; private set; }
        public bool WasGrounded { get; private set; }
        public Collider2D GroundCollider { get; private set; }
        public Vector2 GroundNormal { get; private set; } = Vector2.up;
        public int WallDirection { get; private set; }

        public bool JustLanded => IsGrounded && !WasGrounded;
        public bool JustLeftGround => !IsGrounded && WasGrounded;

        public void Refresh(float verticalVelocity)
        {
            WasGrounded = IsGrounded;
            Bounds bounds = _capsule.bounds;

            Vector2 groundOrigin = new(bounds.center.x, bounds.min.y + SkinWidth * 4f);
            Vector2 groundSize = new(bounds.size.x * 0.85f, SkinWidth);
            RaycastHit2D groundHit = Physics2D.BoxCast(groundOrigin, groundSize, 0f, Vector2.down,
                _config.GroundCheckDistance + SkinWidth * 4f, Layers.SolidMask);
            bool hasGround = groundHit.collider != null && groundHit.normal.y >= MinGroundNormalY;
            IsGrounded = hasGround && verticalVelocity <= 0.05f;
            GroundCollider = hasGround ? groundHit.collider : null;
            GroundNormal = hasGround ? groundHit.normal : Vector2.up;

            WallDirection = 0;
            if (ProbeWall(bounds, 1)) WallDirection = 1;
            else if (ProbeWall(bounds, -1)) WallDirection = -1;
        }

        public bool CanStandUp(float standingHeight)
        {
            Bounds bounds = _capsule.bounds;
            float extra = standingHeight - bounds.size.y;
            if (extra <= 0f) return true;
            Vector2 origin = new(bounds.center.x, bounds.max.y + extra * 0.5f);
            Vector2 size = new(bounds.size.x * 0.8f, extra);
            return Physics2D.OverlapBox(origin, size, 0f, Layers.SolidMask) == null;
        }

        private bool ProbeWall(Bounds bounds, int direction)
        {
            float distance = bounds.extents.x + _config.WallCheckDistance;
            Vector2 dir = new(direction, 0f);
            float upper = bounds.center.y + bounds.extents.y * 0.45f;
            float lower = bounds.center.y - bounds.extents.y * 0.35f;
            return IsWall(Physics2D.Raycast(new Vector2(bounds.center.x, upper), dir, distance, Layers.SolidMask)) &&
                   IsWall(Physics2D.Raycast(new Vector2(bounds.center.x, lower), dir, distance, Layers.SolidMask));
        }

        private static bool IsWall(RaycastHit2D hit) => hit.collider != null && Mathf.Abs(hit.normal.x) >= MinWallNormalX;
    }
}
