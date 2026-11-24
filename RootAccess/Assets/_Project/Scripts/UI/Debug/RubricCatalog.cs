using System;
using UnityEngine;

namespace RootAccess.UI
{
    [Serializable]
    public sealed class RubricEntry
    {
        [SerializeField] private string title;
        [SerializeField, TextArea] private string summary;
        [SerializeField] private string sceneName;
        [SerializeField] private string spawnId;

        public RubricEntry(string entryTitle, string entrySummary, string scene, string spawn)
        {
            title = entryTitle;
            summary = entrySummary;
            sceneName = scene;
            spawnId = spawn;
        }

        public string Title => title;
        public string Summary => summary;
        public string SceneName => sceneName;
        public string SpawnId => spawnId;
    }

    /// <summary>Data behind the F1 rubric panel: where each graded feature can be demonstrated.</summary>
    [CreateAssetMenu(menuName = "Root Access/Rubric Catalog", fileName = "RubricCatalog")]
    public sealed class RubricCatalog : ScriptableObject
    {
        [SerializeField] private RubricEntry[] entries = new RubricEntry[0];

        public RubricEntry[] Entries => entries;

        public void SetEntries(RubricEntry[] values) => entries = values ?? new RubricEntry[0];
    }
}
