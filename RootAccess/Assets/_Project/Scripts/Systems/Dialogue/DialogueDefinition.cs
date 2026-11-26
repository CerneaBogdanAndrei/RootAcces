using System;
using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Systems
{
    [Serializable]
    public sealed class DialogueLine
    {
        [SerializeField] private string speaker;
        [SerializeField, TextArea] private string text;
        [SerializeField] private string speakerEn;
        [SerializeField, TextArea] private string textEn;

        public DialogueLine(string speakerName, string lineText, string speakerNameEn = null, string lineTextEn = null)
        {
            speaker = speakerName;
            text = lineText;
            speakerEn = speakerNameEn;
            textEn = lineTextEn;
        }

        public string Speaker => Loc.T(speaker, speakerEn);
        public string Text => Loc.T(text, textEn);
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
