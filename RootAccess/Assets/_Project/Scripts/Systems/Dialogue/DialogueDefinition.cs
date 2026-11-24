using System;
using UnityEngine;

namespace RootAccess.Systems
{
    [Serializable]
    public sealed class DialogueLine
    {
        [SerializeField] private string speaker;
        [SerializeField, TextArea] private string text;

        public DialogueLine(string speakerName, string lineText)
        {
            speaker = speakerName;
            text = lineText;
        }

        public string Speaker => speaker;
        public string Text => text;
    }

    /// <summary>Data-driven linear conversation shown by the dialogue box.</summary>
    [CreateAssetMenu(menuName = "Root Access/Dialogue", fileName = "Dialogue_")]
    public sealed class DialogueDefinition : ScriptableObject
    {
        [SerializeField] private string dialogueId;
        [SerializeField] private DialogueLine[] lines = new DialogueLine[0];

        public string DialogueId => dialogueId;
        public DialogueLine[] Lines => lines;

        public void Configure(string id, params DialogueLine[] dialogueLines)
        {
            dialogueId = id;
            lines = dialogueLines ?? new DialogueLine[0];
        }
    }
}
