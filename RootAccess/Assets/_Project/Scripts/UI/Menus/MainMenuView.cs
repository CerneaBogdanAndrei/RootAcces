using RootAccess.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RootAccess.UI
{
    /// <summary>Main menu: Continue, New Game, Settings, Rubric Showcase, Credits, Quit.</summary>
    public sealed class MainMenuView : UIPanel
    {
        [SerializeField] private Button continueButton;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button rubricButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private TMP_Text versionLabel;

        private SettingsMenuView _settings;

        protected override void Awake()
        {
            base.Awake();
            continueButton.onClick.AddListener(OnContinue);
            newGameButton.onClick.AddListener(OnNewGame);
            settingsButton.onClick.AddListener(OnSettings);
            rubricButton.onClick.AddListener(() => Load(SceneNames.RubricShowcase));
            creditsButton.onClick.AddListener(() => Load(SceneNames.Credits));
            quitButton.onClick.AddListener(() => { if (ServiceLocator.TryGet(out GameManager gm)) gm.QuitGame(); });
            if (versionLabel != null) versionLabel.text = $"{Strings.Menu.Version} · {Application.version}";
        }

        private void Start()
        {
            bool hasSave = ServiceLocator.TryGet(out ISessionService session) && session.HasSave;
            continueButton.interactable = hasSave;
            ServiceLocator.TryGet(out _settings);
            Show();
        }

        private void OnContinue()
        {
            if (ServiceLocator.TryGet(out ISessionService session) && !session.ContinueGame()) continueButton.interactable = false;
        }

        private void OnNewGame()
        {
            if (ServiceLocator.TryGet(out ISessionService session)) session.NewGame();
            else Load(SceneNames.Hub);
        }

        private void OnSettings()
        {
            if (_settings == null && !ServiceLocator.TryGet(out _settings)) return;
            Hide();
            _settings.Open(Show);
        }

        private static void Load(string scene)
        {
            if (ServiceLocator.TryGet(out GameManager gameManager)) gameManager.LoadScene(scene);
        }
    }
}
