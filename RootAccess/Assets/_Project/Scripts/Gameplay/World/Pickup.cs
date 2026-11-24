using RootAccess.Core;
using RootAccess.Systems;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>Collectible (item and/or credits). Collected on trigger contact; stays if the inventory is full.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class Pickup : MonoBehaviour
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int quantity = 1;
        [SerializeField, Min(0)] private int credits;
        [SerializeField] private Transform visual;
        [SerializeField] private float bobAmplitude = 0.12f;
        [SerializeField] private float bobSpeed = 2.5f;

        private Vector3 _visualRest;
        private bool _collected;

        public ItemDefinition Item => item;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (visual != null) _visualRest = visual.localPosition;
        }

        private void Update()
        {
            if (visual == null) return;
            visual.localPosition = _visualRest + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + transform.position.x) * bobAmplitude);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_collected || other.GetComponentInParent<IPlayerTarget>() == null) return;
            if (!ServiceLocator.TryGet(out SessionService session)) return;
            if (item != null && !session.TryAddItem(item, quantity)) return;
            if (credits > 0) session.Session.Wallet.Add(credits);

            _collected = true;
            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.Pickup, transform.position));
            EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.PickupGlow, transform.position));
            gameObject.SetActive(false);
        }
    }
}
