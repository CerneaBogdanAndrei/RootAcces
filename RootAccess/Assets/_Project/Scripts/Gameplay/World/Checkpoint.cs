using RootAccess.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RootAccess.Gameplay
{
    /// <summary>Activates on first player contact: sets the respawn point and triggers an autosave.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class Checkpoint : MonoBehaviour
    {
        [SerializeField] private string checkpointId = "checkpoint";
        [SerializeField] private Transform respawnPoint;
        [SerializeField] private Animator animator;
        [SerializeField] private Light2D beacon;
        [SerializeField] private Color inactiveColor = new(1f, 0.4f, 0.2f);
        [SerializeField] private Color activeColor = new(0.2f, 1f, 0.5f);

        public bool IsActivated { get; private set; }
        public string CheckpointId => checkpointId;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (beacon != null) beacon.color = inactiveColor;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsActivated || other.GetComponentInParent<IPlayerTarget>() == null) return;
            IsActivated = true;
            Vector3 position = respawnPoint != null ? respawnPoint.position : transform.position;
            if (animator != null) animator.SetBool(AnimatorParams.Active, true);
            if (beacon != null) beacon.color = activeColor;
            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.Checkpoint, transform.position));
            EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.DataStream, transform.position));
            EventBus<CheckpointReached>.Publish(new CheckpointReached(checkpointId, position));
        }
    }
}
