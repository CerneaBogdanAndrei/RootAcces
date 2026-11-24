using System;

namespace RootAccess.Systems
{
    /// <summary>Steps through a <see cref="DialogueDefinition"/>; the view only renders the current line.</summary>
    public sealed class DialogueRunner
    {
        private DialogueDefinition _current;
        private int _index = -1;

        public event Action<DialogueLine> LineShown;
        public event Action<DialogueDefinition> Ended;

        public bool IsRunning => _current != null;
        public DialogueLine CurrentLine => IsRunning && _index >= 0 && _index < _current.Lines.Length ? _current.Lines[_index] : null;

        public bool Start(DialogueDefinition dialogue)
        {
            if (dialogue == null || dialogue.Lines.Length == 0) return false;
            _current = dialogue;
            _index = -1;
            Advance();
            return true;
        }

        public void Advance()
        {
            if (!IsRunning) return;
            _index++;
            if (_index >= _current.Lines.Length)
            {
                Stop();
                return;
            }

            LineShown?.Invoke(_current.Lines[_index]);
        }

        public void Stop()
        {
            if (!IsRunning) return;
            DialogueDefinition finished = _current;
            _current = null;
            _index = -1;
            Ended?.Invoke(finished);
        }
    }
}
