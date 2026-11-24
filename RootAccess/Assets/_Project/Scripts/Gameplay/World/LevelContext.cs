using RootAccess.Core;
using RootAccess.Systems;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>
    /// Per-scene entry logic. Start runs at order -200, i.e. after every Awake/OnEnable (spawn points registered) but
    /// before other Start methods, so the player is placed before its health caches the respawn point.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class LevelContext : MonoBehaviour
    {
        [SerializeField] private string levelId;
        [SerializeField] private Transform player;
        [SerializeField] private SpawnPoint defaultSpawn;

        public string LevelId => levelId;

        private void Start()
        {
            PlacePlayer();
            if (!string.IsNullOrEmpty(levelId))
                EventBus<ObjectiveReported>.Publish(new ObjectiveReported(ObjectiveType.Reach, levelId));
        }

        private void PlacePlayer()
        {
            if (player == null)
            {
                Debug.LogError($"[LevelContext] '{gameObject.scene.name}' has no player reference.", this);
                return;
            }

            string spawnId = ServiceLocator.TryGet(out GameManager gameManager) ? gameManager.ConsumePendingSpawnId() : null;
            SpawnPoint spawn = SpawnPoint.Find(spawnId);
            if (spawn != null)
            {
                Place(spawn.transform.position);
                return;
            }

            if (ServiceLocator.TryGet(out SessionService session) && session.Session.HasCheckpoint &&
                session.Session.SceneName == gameObject.scene.name)
            {
                Place(session.Session.CheckpointPosition);
                return;
            }

            if (defaultSpawn != null) Place(defaultSpawn.transform.position);
        }

        private void Place(Vector3 position)
        {
            player.position = position;
            if (player.TryGetComponent(out Rigidbody2D body)) body.position = position;
        }
    }
}
