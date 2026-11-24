using RootAccess.Core;
using RootAccess.Input;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RootAccess.UI
{
    /// <summary>F1 grading aid: lists rubric items with a "Go to" button that loads the scene section showing it.</summary>
    public sealed class DebugRubricPanel : UIPanel
    {
        [SerializeField] private InputReader input;
        [SerializeField] private RubricCatalog catalog;
        [SerializeField] private RectTransform list;
        [SerializeField] private GameObject rowTemplate;
        [SerializeField] private TMP_Text titleLabel;

        private bool _built;

        protected override void Awake()
        {
            base.Awake();
            if (rowTemplate != null) rowTemplate.SetActive(false);
            if (titleLabel != null) titleLabel.text = Strings.Rubric.PanelTitle;
        }

        private void OnEnable()
        {
            if (input != null) input.DebugPanelEvent += Toggle;
        }

        private void OnDisable()
        {
            if (input != null) input.DebugPanelEvent -= Toggle;
        }

        public void Toggle()
        {
            if (IsVisible)
            {
                Hide();
                return;
            }

            Build();
            Show();
        }

        private void Build()
        {
            if (_built || catalog == null || list == null || rowTemplate == null) return;
            _built = true;
            foreach (RubricEntry entry in catalog.Entries)
            {
                GameObject row = Instantiate(rowTemplate, list);
                row.SetActive(true);
                TMP_Text[] labels = row.GetComponentsInChildren<TMP_Text>(true);
                if (labels.Length > 0) labels[0].text = $"<b>{entry.Title}</b>  <size=80%>{entry.Summary}</size>";
                if (labels.Length > 1) labels[1].text = Strings.Rubric.GoTo;
                Button button = row.GetComponentInChildren<Button>(true);
                if (button == null) continue;
                RubricEntry captured = entry;
                button.onClick.AddListener(() => GoTo(captured));
            }
        }

        private void GoTo(RubricEntry entry)
        {
            Hide();
            if (ServiceLocator.TryGet(out GameManager gameManager)) gameManager.LoadScene(entry.SceneName, entry.SpawnId);
        }
    }
}
