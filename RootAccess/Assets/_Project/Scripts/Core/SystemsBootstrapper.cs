using UnityEngine;

namespace RootAccess.Core
{
    /// <summary>
    /// Guarantees the persistent SystemsRoot prefab exists before any scene loads, so every scene
    /// (including ones opened directly in the Editor) has GameManager, audio, pools and UI available.
    /// </summary>
    public static class SystemsBootstrapper
    {
        public static GameObject Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureSystems()
        {
            if (Instance != null) return;
            GameObject prefab = Resources.Load<GameObject>(ResourcePaths.SystemsRoot);
            if (prefab == null)
            {
                Debug.LogWarning($"[Bootstrap] Resources/{ResourcePaths.SystemsRoot} prefab not found; run Root Access/Build/Build All Scenes.");
                return;
            }

            Instance = Object.Instantiate(prefab);
            Instance.name = prefab.name;
            Object.DontDestroyOnLoad(Instance);
        }
    }
}
