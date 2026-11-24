using System.Text;
using RootAccess.Core;
using RootAccess.Systems;
using TMPro;
using UnityEngine;

namespace RootAccess.UI
{
    /// <summary>Text quest log (skeleton): every known contract with objective progress.</summary>
    public sealed class QuestLogView : MonoBehaviour
    {
        [SerializeField] private TMP_Text body;

        private readonly StringBuilder _builder = new(512);

        public void Render()
        {
            if (body == null || !ServiceLocator.TryGet(out SessionService session)) return;
            _builder.Clear();
            foreach (QuestProgress quest in session.Session.Quests.All)
            {
                _builder.Append(quest.Status == QuestStatus.Completed ? "[OK] " : "> ").Append(quest.Definition.Title).Append('\n');
                foreach (ObjectiveProgress objective in quest.Objectives)
                {
                    _builder.Append("    ").Append(objective.IsComplete ? "[x] " : objective.Failed ? "[!] " : "[ ] ")
                        .Append(objective.Definition.Description);
                    if (objective.Definition.Type != ObjectiveType.AvoidDetection)
                        _builder.Append(" (").Append(objective.Count).Append('/').Append(objective.Definition.RequiredCount).Append(')');
                    _builder.Append('\n');
                }
            }

            body.text = _builder.Length > 0 ? _builder.ToString() : Strings.Hud.NoActiveQuest;
        }
    }
}
