using RootAccess.Core;
using RootAccess.Systems;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>
    /// Hidden data log: touching it records the page in the session's <see cref="LoreArchive"/> (toast, small reward, the
    /// archivist contract's progress). Logs found in an earlier run stay hidden, so a level only shows what is left.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class LoreLog : MonoBehaviour
    {
        [SerializeField] private LoreDefinition log;
        [SerializeField] private Transform visual;
        [SerializeField] private float bobAmplitude = 0.1f;
        [SerializeField] private float bobSpeed = 2.2f;

        private Vector3 _visualRest;
        private bool _collected;

        public LoreDefinition Log => log;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (visual != null) _visualRest = visual.localPosition;
        }

        private void Start()
        {
            if (log == null)
            {
                Debug.LogWarning($"[LoreLog] {name} has no log assigned.");
                gameObject.SetActive(false);
                return;
            }

            if (ServiceLocator.TryGet(out SessionService session) && session.Session != null && session.Session.Lore.Has(log.LoreId))
                gameObject.SetActive(false);
        }

        private void Update()
        {
            if (visual == null) return;
            visual.localPosition = _visualRest + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + transform.position.x) * bobAmplitude);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_collected || log == null || other.GetComponentInParent<IPlayerTarget>() == null) return;
            if (!ServiceLocator.TryGet(out SessionService session) || !session.CollectLore(log)) return;
            _collected = true;
            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.Checkpoint, transform.position));
            EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.PickupGlow, transform.position));
            gameObject.SetActive(false);
        }
    }
}
