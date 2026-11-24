using System.Collections.Generic;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>Named arrival point (scene entry, fast travel, rubric "Go to" targets).</summary>
    public sealed class SpawnPoint : MonoBehaviour
    {
        private static readonly List<SpawnPoint> Active = new();

        [SerializeField] private string spawnId = "default";

        public string SpawnId => spawnId;

        private void OnEnable() => Active.Add(this);

        private void OnDisable() => Active.Remove(this);

        public static SpawnPoint Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < Active.Count; i++)
                if (Active[i].spawnId == id) return Active[i];
            return null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 1f, 0.6f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.35f);
        }
    }
}
