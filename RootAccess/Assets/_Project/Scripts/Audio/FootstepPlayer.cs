using RootAccess.Core;
using RootAccess.Systems;
using UnityEngine;

namespace RootAccess.Audio
{
    /// <summary>
    /// Lives next to an Animator; walk/run clips call <c>OnFootstep</c> through Animation Events.
    /// Probes the ground under the character so the footstep matches the surface.
    /// </summary>
    public sealed class FootstepPlayer : MonoBehaviour
    {
        [SerializeField] private Transform feet;
        [SerializeField] private float probeDistance = 0.4f;
        [SerializeField] private SurfaceType fallbackSurface = SurfaceType.Concrete;
        [SerializeField] private bool silent;
        [SerializeField, Tooltip("Player only: the 'silent steps' skill makes these footsteps much quieter.")]
        private bool silencedBySkill;

        private const float SilentStepsVolume = 0.3f;

        public bool Silent
        {
            get => silent;
            set => silent = value;
        }

        public void OnFootstep()
        {
            if (silent) return;
            Vector2 origin = feet != null ? feet.position : transform.position;
            RaycastHit2D hit = Physics2D.Raycast(origin + Vector2.up * 0.1f, Vector2.down, probeDistance, Layers.SolidMask);
            SurfaceType surface = hit.collider != null ? SurfaceTag.Resolve(hit.collider, fallbackSurface) : fallbackSurface;
            float volume = silencedBySkill && ServiceLocator.TryGet(out SessionService session) && session.HasSkillEffect(SkillEffect.SilentSteps)
                ? SilentStepsVolume
                : 1f;
            EventBus<FootstepRequested>.Publish(new FootstepRequested(surface, origin, volume));
        }
    }
}
