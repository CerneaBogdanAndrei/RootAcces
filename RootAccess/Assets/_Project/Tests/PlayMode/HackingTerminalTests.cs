using System.Collections;
using System.Linq;
using NUnit.Framework;
using RootAccess.AI;
using RootAccess.Core;
using RootAccess.Gameplay;
using RootAccess.Hacking;
using RootAccess.Player;
using RootAccess.Systems;
using UnityEngine;
using UnityEngine.TestTools;

namespace RootAccess.Tests.PlayMode
{
    /// <summary>
    /// The two terminal variants not covered by the acceptance flow, hacked through the real overlay in Level 1:
    /// Terminal_Camera runs Port Scan and disables the security camera, Terminal_Data runs Wire Connect and pays out.
    /// </summary>
    public sealed class HackingTerminalTests
    {
        private const string Spawn = "l1_nodes";
        private const string DataShardId = "data_shard";

        [TearDown]
        public void TearDown() => SliceTestUtil.ResetTime();

        [UnityTest, Timeout(120000)]
        public IEnumerator CameraTerminal_PortScanDisablesSecurityCamera()
        {
            yield return LoadFreshLevel();
            PlayerController player = SliceTestUtil.Player;
            Terminal terminal = Terminal(TerminalKind.Camera);
            SecurityCamera camera = Object.FindFirstObjectByType<SecurityCamera>();
            Assert.IsFalse(camera.IsHacked);

            terminal.Interact(player);
            PortScanMinigame minigame = Object.FindFirstObjectByType<PortScanMinigame>();
            Assert.IsNotNull(minigame, "Camera terminals must run the Port Scan minigame.");
            Assert.AreEqual(GameState.Hacking, SliceTestUtil.Game.State);

            float deadline = Time.realtimeSinceStartup + 20f;
            while (!minigame.Model.IsComplete)
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, "Timed out while solving Port Scan.");
                Assert.IsFalse(minigame.Model.IsFailed, "Port Scan failed although every scan was inside the window.");
                minigame.Model.SetCursor(minigame.Model.WindowStart + minigame.Model.WindowWidth * 0.5f);
                minigame.OnSubmit();
                yield return null;
            }

            yield return SliceTestUtil.WaitUntil(() => terminal.IsHacked, 5f, "the camera terminal to resolve the hack");
            Assert.IsTrue(camera.IsHacked, "A hacked camera terminal must disable its security camera.");
            yield return SliceTestUtil.WaitUntil(() => SliceTestUtil.Game.State == GameState.Playing, 5f, "the overlay to close");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator DataTerminal_WireConnectPaysCreditsAndDataShard()
        {
            yield return LoadFreshLevel();
            PlayerController player = SliceTestUtil.Player;
            Terminal terminal = Terminal(TerminalKind.Data);
            Assert.IsTrue(ServiceLocator.TryGet(out SessionService session), "SessionService missing.");
            int creditsBefore = session.Session.Wallet.Credits;

            terminal.Interact(player);
            WireConnectMinigame minigame = Object.FindFirstObjectByType<WireConnectMinigame>();
            Assert.IsNotNull(minigame, "Data terminals must run the Wire Connect minigame.");
            WireConnectModel model = minigame.Model;
            for (int left = 0; left < model.WireCount; left++)
            {
                minigame.PressLeft(left);
                minigame.PressRight(RightSocketFor(model, model.LeftColor(left)));
            }

            Assert.IsTrue(model.IsComplete, "Matching every colour must complete Wire Connect.");
            Assert.AreEqual(0, model.Mistakes);
            yield return SliceTestUtil.WaitUntil(() => terminal.IsHacked, 5f, "the data terminal to resolve the hack");

            Assert.Greater(session.Session.Wallet.Credits, creditsBefore, "Data terminals pay credits.");
            Assert.IsTrue(session.Session.Inventory.Slots.Any(s => !s.IsEmpty && s.Item.ItemId == DataShardId), "Data terminals award a data shard.");
        }

        private static IEnumerator LoadFreshLevel()
        {
            Assert.IsTrue(ServiceLocator.TryGet(out ISessionService session), "Session service missing.");
            session.ResetSession();
            yield return SliceTestUtil.LoadScene(SceneNames.Level01, Spawn);
        }

        private static Terminal Terminal(TerminalKind kind) =>
            Object.FindObjectsByType<Terminal>(FindObjectsSortMode.None).Single(t => t.Kind == kind);

        private static int RightSocketFor(WireConnectModel model, int color)
        {
            for (int i = 0; i < model.WireCount; i++)
                if (model.RightColor(i) == color) return i;
            Assert.Fail($"No right socket carries colour {color}.");
            return -1;
        }
    }
}
