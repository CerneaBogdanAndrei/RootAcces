using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RootAccess.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RootAccess.Tests.PlayMode
{
    /// <summary>
    /// Loads every scene in Build Settings through the real loader and lets it run. The Test Framework fails a test on any
    /// unexpected Debug.LogError or exception, so a green run means every scene boots cleanly.
    /// </summary>
    public sealed class SceneSmokeTests
    {
        private const float RunSeconds = 2f;
        private const float FallenBelowY = -1.5f;
        private const string ProjectScenesFolder = "Assets/_Project/Scenes/";

        /// <summary>Project scenes only: in PlayMode the test runner temporarily adds its own InitTestScene to Build Settings.</summary>
        public static IEnumerable<string> BuildScenes()
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (path.StartsWith(ProjectScenesFolder, System.StringComparison.Ordinal)) yield return Path.GetFileNameWithoutExtension(path);
            }
        }

        [TearDown]
        public void TearDown() => SliceTestUtil.ResetTime();

        [Test]
        public void BuildSettings_ContainTheVerticalSliceScenes()
        {
            CollectionAssert.IsSubsetOf(
                new[]
                {
                    SceneNames.Bootstrap, SceneNames.MainMenu, SceneNames.Hub, SceneNames.Level01, SceneNames.Level02, SceneNames.Level03,
                    SceneNames.Level04, SceneNames.Level05, SceneNames.RubricShowcase, SceneNames.Credits, SceneNames.Intro, SceneNames.Finale,
                    SceneNames.OperatorConsole
                },
                new List<string>(BuildScenes()));
            Assert.AreEqual(SceneNames.Bootstrap, Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(0)),
                "Bootstrap must be build index 0.");
        }

        [UnityTest]
        public IEnumerator Scene_LoadsAndRunsWithoutErrors([ValueSource(nameof(BuildScenes))] string sceneName)
        {
            if (sceneName == SceneNames.Bootstrap)
            {
                yield return SliceTestUtil.LoadAndWait(SceneNames.MainMenu, () => SceneManager.LoadScene(SceneNames.Bootstrap));
            }
            else
            {
                yield return SliceTestUtil.LoadScene(sceneName);
            }

            yield return new WaitForSecondsRealtime(RunSeconds);

            if (SceneNames.IsCutsceneScene(sceneName))
            {
                Assert.AreEqual(GameState.Cutscene, SliceTestUtil.Game.State, $"{sceneName} runs its Timeline in the Cutscene state.");
                yield break;
            }

            if (!SceneNames.IsGameplayScene(sceneName))
            {
                Assert.AreEqual(GameState.MainMenu, SliceTestUtil.Game.State);
                yield break;
            }

            Assert.AreEqual(GameState.Playing, SliceTestUtil.Game.State);
            var player = SliceTestUtil.Player;
            Assert.IsTrue(player.IsAlive, $"The player died within {RunSeconds}s of entering {sceneName}.");
            Assert.Greater(player.Position.y, FallenBelowY, $"The player fell through the floor of {sceneName} (y={player.Position.y:0.00}).");
            Assert.IsTrue(player.IsGrounded, $"The player should be standing on the spawn floor of {sceneName}.");
        }
    }
}
