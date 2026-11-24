using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RootAccess.Core
{
    /// <summary>
    /// Async single-scene loader. Publishes start/progress/completed events that the loading screen renders;
    /// holds activation until the minimum display time has passed so the transition never flickers.
    /// </summary>
    public sealed class SceneLoader : MonoBehaviour
    {
        private const float ActivationThreshold = 0.9f;

        [SerializeField] private float minimumDisplaySeconds = 0.75f;
        [SerializeField] private float fadeOutSeconds = 0.25f;

        public bool IsLoading { get; private set; }
        public string CurrentScene => SceneManager.GetActiveScene().name;

        private void Awake() => ServiceLocator.Register(this);

        private void OnDestroy() => ServiceLocator.Unregister(this);

        public bool Load(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[SceneLoader] Already loading a scene; ignored request for '{sceneName}'.");
                return false;
            }

            if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[SceneLoader] Scene '{sceneName}' is not in Build Settings.");
                return false;
            }

            StartCoroutine(LoadRoutine(sceneName));
            return true;
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            IsLoading = true;
            EventBus<SceneLoadStarted>.Publish(new SceneLoadStarted(sceneName));
            EventBus<SceneLoadProgress>.Publish(new SceneLoadProgress(0f));
            yield return null;

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                Debug.LogError($"[SceneLoader] LoadSceneAsync returned null for '{sceneName}'.");
                IsLoading = false;
                yield break;
            }

            operation.allowSceneActivation = false;
            float startedAt = Time.unscaledTime;
            while (operation.progress < ActivationThreshold || Time.unscaledTime - startedAt < minimumDisplaySeconds)
            {
                float loadProgress = Mathf.Clamp01(operation.progress / ActivationThreshold);
                float timeProgress = Mathf.Clamp01((Time.unscaledTime - startedAt) / minimumDisplaySeconds);
                EventBus<SceneLoadProgress>.Publish(new SceneLoadProgress(Mathf.Min(loadProgress, timeProgress)));
                yield return null;
            }

            EventBus<SceneLoadProgress>.Publish(new SceneLoadProgress(1f));
            operation.allowSceneActivation = true;
            while (!operation.isDone) yield return null;

            float fadeEnd = Time.unscaledTime + fadeOutSeconds;
            while (Time.unscaledTime < fadeEnd) yield return null;

            IsLoading = false;
            EventBus<SceneLoadCompleted>.Publish(new SceneLoadCompleted(sceneName));
        }
    }
}
