using UnityEngine;

namespace RootAccess.Core
{
    /// <summary>Placed in the Bootstrap scene (build index 0): hands off to the main menu once systems are up.</summary>
    public sealed class BootstrapLoader : MonoBehaviour
    {
        [SerializeField] private string firstScene = SceneNames.MainMenu;

        private void Start()
        {
            if (ServiceLocator.TryGet(out GameManager gameManager)) gameManager.LoadScene(firstScene);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(firstScene);
        }
    }
}
