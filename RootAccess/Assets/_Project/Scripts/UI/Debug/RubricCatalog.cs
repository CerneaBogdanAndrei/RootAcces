using System;
using RootAccess.Core;
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
        [SerializeField] private string titleEn;
        [SerializeField, TextArea] private string summaryEn;

        public RubricEntry(string entryTitle, string entrySummary, string scene, string spawn, string entryTitleEn = null, string entrySummaryEn = null)
        {
            title = entryTitle;
            summary = entrySummary;
            sceneName = scene;
            spawnId = spawn;
            titleEn = entryTitleEn;
            summaryEn = entrySummaryEn;
        }

        public string Title => Loc.T(title, titleEn);
        public string Summary => Loc.T(summary, summaryEn);
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
