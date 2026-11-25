using RootAccess.Core;
using RootAccess.Systems;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>
    /// Leaves the level on contact. A completing exit reports the "reach" objective and shows the Level Complete
    /// screen; otherwise it loads the target scene directly.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class LevelExit : MonoBehaviour
    {
        [SerializeField] private string exitId = "exit";
        [SerializeField] private string levelId;
        [SerializeField] private string targetScene = SceneNames.Hub;
        [SerializeField] private string targetSpawnId;
        [SerializeField] private bool completesLevel = true;
        [SerializeField] private LevelDefinition unlocksLevel;

        private bool _used;

        public string TargetScene => targetScene;

        private void Awake() => GetComponent<Collider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_used || other.GetComponentInParent<IPlayerTarget>() == null) return;
            _used = true;
            EventBus<ObjectiveReported>.Publish(new ObjectiveReported(ObjectiveType.Reach, exitId));

            if (ServiceLocator.TryGet(out SessionService session))
            {
                if (unlocksLevel != null) session.Session.UnlockLevel(unlocksLevel.LevelId);
                session.SaveGame();
            }

            if (completesLevel)
            {
                EventBus<LevelCompleted>.Publish(new LevelCompleted(levelId, targetScene));
                return;
            }

            if (ServiceLocator.TryGet(out GameManager gameManager)) gameManager.LoadScene(targetScene, targetSpawnId);
        }
    }
}
