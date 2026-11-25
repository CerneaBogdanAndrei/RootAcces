using System;
using System.Collections;
using NUnit.Framework;
using RootAccess.Core;
using RootAccess.Player;
using UnityEngine;

namespace RootAccess.Tests.PlayMode
{
    /// <summary>Shared helpers: scene loading through the real GameManager/SceneLoader path and bounded waits.</summary>
    internal static class SliceTestUtil
    {
        public const float LoadTimeoutSeconds = 45f;

        public static GameManager Game
        {
            get
            {
                Assert.IsTrue(ServiceLocator.TryGet(out GameManager gameManager), "GameManager missing: the SystemsRoot prefab was not bootstrapped.");
                return gameManager;
            }
        }

        public static PlayerController Player
        {
            get
            {
                Assert.IsTrue(ServiceLocator.TryGet(out IPlayerTarget target), "No player registered in the loaded scene.");
                var controller = target.Transform.GetComponent<PlayerController>();
                Assert.IsNotNull(controller, "The registered player target has no PlayerController.");
                return controller;
            }
        }

        public static IEnumerator WaitUntil(Func<bool> condition, float timeoutSeconds, string what)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline) Assert.Fail($"Timed out after {timeoutSeconds:0.#}s waiting for {what}.");
                yield return null;
            }
        }

        /// <summary>Loads a scene through <see cref="GameManager.LoadScene"/> and waits for the loading screen to finish.</summary>
        public static IEnumerator LoadScene(string sceneName, string spawnId = null) =>
            LoadAndWait(sceneName, () => Game.LoadScene(sceneName, spawnId));

        /// <summary>Runs <paramref name="trigger"/> (e.g. a menu button click) and waits until <paramref name="sceneName"/> is loaded.</summary>
        public static IEnumerator LoadAndWait(string sceneName, Action trigger)
        {
            if (ServiceLocator.TryGet(out SceneLoader loader))
                yield return WaitUntil(() => !loader.IsLoading, LoadTimeoutSeconds, "the previous scene load to finish");

            bool loaded = false;
            Action<SceneLoadCompleted> onLoaded = evt => loaded |= evt.SceneName == sceneName;
            EventBus<SceneLoadCompleted>.Subscribe(onLoaded);
            try
            {
                trigger();
                yield return WaitUntil(() => loaded, LoadTimeoutSeconds, $"scene '{sceneName}' to load");
            }
            finally
            {
                EventBus<SceneLoadCompleted>.Unsubscribe(onLoaded);
            }
        }

        public static void ResetTime() => Time.timeScale = 1f;
    }
}
