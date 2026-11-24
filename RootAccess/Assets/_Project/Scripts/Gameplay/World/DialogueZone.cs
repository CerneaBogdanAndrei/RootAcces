using RootAccess.Core;
using RootAccess.Systems;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>Starts a dialogue the first time the player walks in (radio calls, tutorial hints).</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class DialogueZone : MonoBehaviour
    {
        [SerializeField] private DialogueDefinition dialogue;
        [SerializeField] private bool once = true;

        private bool _played;

        private void Awake() => GetComponent<Collider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (dialogue == null || (once && _played) || other.GetComponentInParent<IPlayerTarget>() == null) return;
            _played = true;
            EventBus<DialogueRequested>.Publish(new DialogueRequested(dialogue));
        }
    }
}
