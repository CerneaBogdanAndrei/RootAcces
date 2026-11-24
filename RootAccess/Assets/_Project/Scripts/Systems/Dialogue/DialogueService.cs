using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Systems
{
    /// <summary>Hosts the <see cref="DialogueRunner"/> and bridges it to the EventBus and game state.</summary>
    public sealed class DialogueService : MonoBehaviour
    {
        private readonly DialogueRunner _runner = new();
        private GameManager _gameManager;
        private GameState _stateBefore;

        public bool IsRunning => _runner.IsRunning;

        private void Awake() => ServiceLocator.Register(this);

        private void OnEnable()
        {
            _runner.LineShown += OnLineShown;
            _runner.Ended += OnEnded;
            EventBus<DialogueRequested>.Subscribe(OnDialogueRequested);
        }

        private void OnDisable()
        {
            _runner.LineShown -= OnLineShown;
            _runner.Ended -= OnEnded;
            EventBus<DialogueRequested>.Unsubscribe(OnDialogueRequested);
        }

        private void OnDestroy() => ServiceLocator.Unregister(this);

        public void Advance() => _runner.Advance();

        public void Skip() => _runner.Stop();

        private void OnDialogueRequested(DialogueRequested evt)
        {
            if (_runner.IsRunning || evt.Dialogue == null) return;
            if (ServiceLocator.TryGet(out _gameManager))
            {
                _stateBefore = _gameManager.State;
                _gameManager.SetState(GameState.Dialogue);
            }

            _runner.Start(evt.Dialogue);
        }

        private void OnLineShown(DialogueLine line) => EventBus<DialogueLineShown>.Publish(new DialogueLineShown(line));

        private void OnEnded(DialogueDefinition _)
        {
            EventBus<DialogueEnded>.Publish(new DialogueEnded());
            if (_gameManager != null && _gameManager.State == GameState.Dialogue) _gameManager.SetState(_stateBefore);
        }
    }
}
