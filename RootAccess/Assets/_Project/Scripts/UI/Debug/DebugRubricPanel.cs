using System.Collections.Generic;
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

        private readonly List<(TMP_Text summary, TMP_Text goTo, RubricEntry entry)> _rows = new();
        private bool _built;

        protected override void Awake()
        {
            base.Awake();
            if (rowTemplate != null) rowTemplate.SetActive(false);
        }

        private void OnEnable()
        {
            CommandLineAutomation.CommandRequested += OnAutomationCommand;
            if (input != null) input.DebugPanelEvent += Toggle;
        }

        private void OnDisable()
        {
            CommandLineAutomation.CommandRequested -= OnAutomationCommand;
            if (input != null) input.DebugPanelEvent -= Toggle;
        }

        private void OnAutomationCommand(string command)
        {
            if (command == AutomationCommands.RubricPanel && !IsVisible) Toggle();
        }

        public void Toggle()
        {
            if (IsVisible)
            {
                Hide();
                return;
            }

            Build();
            RenderTexts();
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
                _rows.Add((labels.Length > 0 ? labels[0] : null, labels.Length > 1 ? labels[1] : null, entry));
                Button button = row.GetComponentInChildren<Button>(true);
                if (button == null) continue;
                RubricEntry captured = entry;
                button.onClick.AddListener(() => GoTo(captured));
            }
        }

        /// <summary>Texts are written each time the panel opens, so they follow the language chosen in the menu.</summary>
        private void RenderTexts()
        {
            if (titleLabel != null) titleLabel.text = Strings.Rubric.PanelTitle;
            foreach ((TMP_Text summary, TMP_Text goTo, RubricEntry entry) in _rows)
            {
                if (summary != null) summary.text = $"<b>{entry.Title}</b>  <size=80%>{entry.Summary}</size>";
                if (goTo != null) goTo.text = Strings.Rubric.GoTo;
            }
        }

        private void GoTo(RubricEntry entry)
        {
            Hide();
            if (ServiceLocator.TryGet(out GameManager gameManager)) gameManager.LoadScene(entry.SceneName, entry.SpawnId);
        }
    }
}
