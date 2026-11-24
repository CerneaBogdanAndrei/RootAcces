using System.Text;
using RootAccess.Core;
using RootAccess.Systems;
using TMPro;
using UnityEngine;

namespace RootAccess.UI
{
    /// <summary>Text skill overview (skeleton for the node-graph skill tree): branch, cost, state.</summary>
    public sealed class SkillTreeView : MonoBehaviour
    {
        [SerializeField] private TMP_Text body;

        private readonly StringBuilder _builder = new(512);

        public void Render()
        {
            if (body == null || !ServiceLocator.TryGet(out SessionService session)) return;
            SkillTree tree = session.Session.Skills;
            _builder.Clear();
            _builder.Append("XP: ").Append(tree.Xp).Append('\n');
            foreach (SkillDefinition skill in session.Database.Skills)
            {
                if (skill == null) continue;
                SkillUnlockResult state = tree.CanUnlock(skill);
                string marker = state == SkillUnlockResult.AlreadyUnlocked ? "[x]" : state == SkillUnlockResult.Success ? "[+]" : "[ ]";
                _builder.Append(marker).Append(' ').Append(skill.DisplayName)
                    .Append("  [").Append(skill.Branch).Append(", ").Append(skill.XpCost).Append(" XP]\n");
            }

            body.text = _builder.ToString();
        }
    }
}
