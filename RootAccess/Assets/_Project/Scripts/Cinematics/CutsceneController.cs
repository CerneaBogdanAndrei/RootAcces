using System;
using RootAccess.Core;
using UnityEngine;
using UnityEngine.Playables;

namespace RootAccess.Cinematics
{
    /// <summary>
    /// Plays a Timeline cutscene (intro / finale), switches the game into the Cutscene state while it runs and
    /// optionally loads a scene afterwards. <see cref="Skip"/> jumps to the end.
    /// </summary>
    [RequireComponent(typeof(PlayableDirector))]
    public sealed class CutsceneController : MonoBehaviour
    {
        [SerializeField] private bool playOnStart;
        [SerializeField] private string sceneAfterCutscene;

        private PlayableDirector _director;
        private GameManager _gameManager;
        private GameState _stateBefore = GameState.Playing;

        public event Action Finished;
        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            _director = GetComponent<PlayableDirector>();
            _director.playOnAwake = false;
        }

        private void OnEnable() => _director.stopped += OnStopped;

        private void OnDisable() => _director.stopped -= OnStopped;

        private void Start()
        {
            ServiceLocator.TryGet(out _gameManager);
            if (playOnStart) Play();
        }

        public void Play()
        {
            if (IsPlaying || _director.playableAsset == null) return;
            IsPlaying = true;
            if (_gameManager != null)
            {
                _stateBefore = _gameManager.State;
                _gameManager.SetState(GameState.Cutscene);
            }

            _director.time = 0;
            _director.Play();
        }

        public void Skip()
        {
            if (!IsPlaying) return;
            _director.time = _director.duration;
            _director.Evaluate();
            _director.Stop();
        }

        private void OnStopped(PlayableDirector director)
        {
            if (!IsPlaying) return;
            IsPlaying = false;
            if (_gameManager != null && _gameManager.State == GameState.Cutscene) _gameManager.SetState(_stateBefore);
            Finished?.Invoke();
            if (!string.IsNullOrEmpty(sceneAfterCutscene) && _gameManager != null) _gameManager.LoadScene(sceneAfterCutscene);
        }
    }
}
