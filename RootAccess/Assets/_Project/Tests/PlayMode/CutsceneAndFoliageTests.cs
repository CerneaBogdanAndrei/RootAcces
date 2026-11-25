using System.Collections;
using System.Linq;
using NUnit.Framework;
using RootAccess.Cinematics;
using RootAccess.Core;
using RootAccess.VFX;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.TestTools;
using UnityEngine.Timeline;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

namespace RootAccess.Tests.PlayMode
{
    /// <summary>
    /// P5: both Timeline cutscenes play in the Cutscene state with every track kind and can be skipped into the next
    /// scene; the park trees are SpriteSkin rigs whose mesh bends when the wind sways their bones.
    /// </summary>
    public sealed class CutsceneAndFoliageTests
    {
        [TearDown]
        public void TearDown() => SliceTestUtil.ResetTime();

        [UnityTest, Timeout(90000)]
        [TestCase(SceneNames.Intro, SceneNames.Hub, ExpectedResult = null)]
        [TestCase(SceneNames.Finale, SceneNames.Credits, ExpectedResult = null)]
        public IEnumerator Cutscene_PlaysTimelineAndSkipLoadsNextScene(string cutscene, string next)
        {
            yield return SliceTestUtil.LoadScene(cutscene);
            CutsceneController controller = Object.FindFirstObjectByType<CutsceneController>();
            Assert.IsNotNull(controller, $"{cutscene} needs a CutsceneController.");
            yield return SliceTestUtil.WaitUntil(() => controller.IsPlaying, 3f, "the cutscene to start");
            Assert.AreEqual(GameState.Cutscene, SliceTestUtil.Game.State);

            var director = controller.GetComponent<PlayableDirector>();
            var timeline = (TimelineAsset)director.playableAsset;
            var tracks = timeline.GetOutputTracks().ToArray();
            Assert.IsTrue(tracks.OfType<AnimationTrack>().Any(), "Animation track (puppet)");
            Assert.IsTrue(tracks.OfType<CinemachineTrack>().Any(), "Cinemachine track (shots)");
            Assert.IsTrue(tracks.OfType<ActivationTrack>().Any(), "Activation tracks (captions)");
            Assert.IsTrue(tracks.OfType<SignalTrack>().Any(), "Signal track (shake / VFX)");
            Assert.Greater(timeline.duration, 10.0, "A real cutscene, not a stub.");

            yield return new WaitForSeconds(1.5f);
            Assert.Greater(director.time, 0.5, "The timeline advances while the cutscene plays.");
            yield return SliceTestUtil.LoadAndWait(next, controller.Skip);
            Assert.AreNotEqual(GameState.Cutscene, SliceTestUtil.Game.State, "Skipping leaves the Cutscene state.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator ParkTrees_AreSpriteSkinRigsBentByTheWind()
        {
            yield return SliceTestUtil.LoadScene(SceneNames.Level01, "l1_park");
            SpriteSkin[] trees = Object.FindObjectsByType<SpriteSkin>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(trees.Length, 3, "The rooftop park has rigged trees.");
            SpriteSkin tree = trees[0];
            Assert.AreEqual(4, tree.boneTransforms.Length, "Each tree has a 4-bone chain.");
            Assert.AreEqual(4, tree.GetComponent<SpriteRenderer>().sprite.GetBones().Length);
            Assert.IsNotNull(Object.FindFirstObjectByType<FoliageWind>(), "Wind drives the bones.");

            Transform crown = tree.boneTransforms[3];
            Vector3[] before = tree.GetDeformedVertexPositionData().ToArray();
            Quaternion rest = crown.localRotation;
            yield return new WaitForSeconds(0.6f);
            Vector3[] after = tree.GetDeformedVertexPositionData().ToArray();
            Assert.AreNotEqual(rest, crown.localRotation, "FoliageWind rotates the bones.");
            Assert.AreEqual(before.Length, after.Length);
            Assert.IsTrue(before.Zip(after, (a, b) => (a - b).sqrMagnitude).Any(d => d > 1e-6f), "SpriteSkin deforms the mesh with the bones.");
        }
    }
}
