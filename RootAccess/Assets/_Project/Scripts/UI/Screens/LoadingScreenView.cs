using RootAccess.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RootAccess.UI
{
    /// <summary>Async loading screen: fades in on load start, renders progress, fades out when the scene is live.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class LoadingScreenView : MonoBehaviour
    {
        [SerializeField] private Image progressFill;
        [SerializeField] private TMP_Text progressLabel;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private float fadeSpeed = 6f;

        private CanvasGroup _group;
        private float _targetAlpha;
        private float _progress;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            if (titleLabel != null) titleLabel.text = Strings.Menu.Loading;
        }

        private void OnEnable()
        {
            EventBus<SceneLoadStarted>.Subscribe(OnStarted);
            EventBus<SceneLoadProgress>.Subscribe(OnProgress);
            EventBus<SceneLoadCompleted>.Subscribe(OnCompleted);
        }

        private void OnDisable()
        {
            EventBus<SceneLoadStarted>.Unsubscribe(OnStarted);
            EventBus<SceneLoadProgress>.Unsubscribe(OnProgress);
            EventBus<SceneLoadCompleted>.Unsubscribe(OnCompleted);
        }

        private void Update()
        {
            _group.alpha = Mathf.MoveTowards(_group.alpha, _targetAlpha, fadeSpeed * Time.unscaledDeltaTime);
            if (progressFill != null) progressFill.fillAmount = Mathf.MoveTowards(progressFill.fillAmount, _progress, 3f * Time.unscaledDeltaTime);
        }

        private void OnStarted(SceneLoadStarted evt)
        {
            _targetAlpha = 1f;
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
            _progress = 0f;
            if (progressFill != null) progressFill.fillAmount = 0f;
        }

        private void OnProgress(SceneLoadProgress evt)
        {
            _progress = evt.Progress;
            if (progressLabel != null) progressLabel.text = $"{Mathf.RoundToInt(evt.Progress * 100f)}%";
        }

        private void OnCompleted(SceneLoadCompleted evt)
        {
            _targetAlpha = 0f;
            _group.blocksRaycasts = false;
        }
    }
}
