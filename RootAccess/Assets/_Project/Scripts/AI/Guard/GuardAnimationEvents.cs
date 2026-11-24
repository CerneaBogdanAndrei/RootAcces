using UnityEngine;

namespace RootAccess.AI
{
    /// <summary>Sits on the guard's Animator object and forwards Animation Events to the guard logic.</summary>
    public sealed class GuardAnimationEvents : MonoBehaviour
    {
        [SerializeField] private Guard guard;

        private void Awake()
        {
            if (guard == null) guard = GetComponentInParent<Guard>();
        }

        public void OnHitFrame()
        {
            if (guard != null) guard.OnHitFrame();
        }
    }
}
