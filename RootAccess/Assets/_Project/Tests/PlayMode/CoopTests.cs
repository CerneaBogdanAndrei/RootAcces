using System.Collections;
using System.Linq;
using NUnit.Framework;
using RootAccess.AI;
using RootAccess.Core;
using RootAccess.Gameplay;
using RootAccess.Systems;
using RootAccess.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace RootAccess.Tests.PlayMode
{
    /// <summary>
    /// P7 co-op. Local split-screen: the Operator console receives the level and acts on a door, a camera and a guard
    /// through the shared link; the camera and HUD shrink to the left half. LAN: hosting opens a Netcode listener that
    /// runs a level without errors and shuts down cleanly (the two-PC flow is covered by scripts/coop-lan-smoke.ps1).
    /// </summary>
    public sealed class CoopTests
    {
        private IOperatorConsole Console
        {
            get
            {
                Assert.IsTrue(ServiceLocator.TryGet(out IOperatorConsole console), "CoopSession missing from SystemsRoot.");
                return console;
            }
        }

        [SetUp]
        public void SetUp()
        {
            Assert.IsTrue(ServiceLocator.TryGet(out SessionService session));
            session.UseSaveDirectory(System.IO.Path.Combine(Application.temporaryCachePath, "RootAccessTestSaves"));
            session.ResetSession();
        }

        [TearDown]
        public void TearDown()
        {
            if (ServiceLocator.TryGet(out IOperatorConsole console)) console.Stop();
            SliceTestUtil.ResetTime();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator LocalSplit_OperatorSeesLevelAndActsOnDoorCameraAndGuard()
        {
            IOperatorConsole console = Console;
            console.StartLocal();
            Assert.AreEqual(CoopMode.LocalSplit, console.Mode);
            yield return SliceTestUtil.LoadScene(SceneNames.Level01, "l1_security");
            yield return SliceTestUtil.WaitUntil(() => console.LatestSnapshot != null && console.LatestMap != null, 5f, "the first snapshot and the level map");

            Assert.AreEqual(0.5f, Camera.main.rect.width, 1e-3f, "Split-screen: the gameplay camera keeps the left half.");
            OperatorConsoleView view = Object.FindFirstObjectByType<OperatorConsoleView>(FindObjectsInactive.Include);
            yield return SliceTestUtil.WaitUntil(() => view.IsVisible && view.TargetCount > 0, 3f, "the Operator console on the right half");
            OperatorSnapshot snapshot = console.LatestSnapshot;
            Assert.IsTrue(snapshot.Entities.Any(e => e.Kind == OperatorTargetKind.Door));
            Assert.IsTrue(snapshot.Entities.Any(e => e.Kind == OperatorTargetKind.Camera));
            Assert.IsTrue(snapshot.Entities.Any(e => e.Kind == OperatorTargetKind.Guard));
            Assert.Greater(console.LatestMap.Solid.Count(s => s), 100, "The map carries the level's walls and floors.");

            Terminal doorTerminal = Object.FindObjectsByType<Terminal>(FindObjectsSortMode.None).Single(t => t.TerminalId == "terminal_l1_door");
            Door door = doorTerminal.Targets.OfType<Door>().Single();
            Assert.IsFalse(door.IsOpen);
            console.SendCommand(door.OperatorId);
            Assert.IsTrue(door.IsOpen, "The Operator opens the story door remotely.");

            SecurityCamera camera = Object.FindObjectsByType<Terminal>(FindObjectsSortMode.None)
                .Single(t => t.TerminalId == "terminal_l1_camera").Targets.OfType<SecurityCamera>().Single();
            console.SendCommand(camera.OperatorId);
            Assert.IsTrue(camera.IsHacked, "The Operator loops the camera feed.");

            Guard guard = Object.FindObjectsByType<Guard>(FindObjectsSortMode.None).First();
            yield return SliceTestUtil.WaitUntil(() => guard.CanOperate, 3f, "the guard to be distractible");
            float energyBefore = ((Net.CoopSession)console).Model.Energy;
            console.SendCommand(guard.OperatorId);
            Assert.AreEqual(GuardStateId.Investigate, guard.StateId, "A distraction sends the guard to investigate a fake noise.");
            Assert.Less(((Net.CoopSession)console).Model.Energy, energyBefore, "Every action costs Operator energy.");

            console.Stop();
            yield return null;
            Assert.AreEqual(1f, Camera.main.rect.width, 1e-3f, "Stopping co-op restores the full screen.");
            Assert.IsFalse(view.IsVisible);
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator LanHost_ListensRunsALevelAndShutsDown()
        {
            IOperatorConsole console = Console;
            Assert.IsTrue(console.Host(), $"Hosting failed: {console.Status}");
            Assert.AreEqual(CoopMode.LanHost, console.Mode);
            Assert.IsNotNull(NetworkManager.Singleton);
            Assert.IsTrue(NetworkManager.Singleton.IsHost && NetworkManager.Singleton.IsListening);

            yield return SliceTestUtil.LoadScene(SceneNames.Level01, "l1_security");
            yield return new WaitForSecondsRealtime(1f);
            Assert.AreEqual(1f, Camera.main.rect.width, 1e-3f, "LAN host plays full screen (the Operator is on the other PC).");
            Assert.AreEqual(CoopMode.LanHost, console.Mode, "Loading a level keeps the session.");

            console.Stop();
            yield return null;
            Assert.AreEqual(CoopMode.None, console.Mode);
            Assert.IsTrue(NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening, "Stop shuts the listener down.");
        }
    }
}
