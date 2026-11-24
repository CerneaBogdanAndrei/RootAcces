using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>Showcase helper: toggles hackable targets on a timer (doors opening/closing for the Animator demo).</summary>
    public sealed class PeriodicHackTrigger : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour[] targets = new MonoBehaviour[0];
        [SerializeField] private HackAction action = HackAction.Toggle;
        [SerializeField, Min(0.2f)] private float interval = 2.5f;

        private float _next;

        private void Update()
        {
            if (Time.time < _next) return;
            _next = Time.time + interval;
            foreach (MonoBehaviour target in targets)
                if (target is IHackable hackable) hackable.ApplyHack(action);
        }
    }
}
