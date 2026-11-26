using System.Collections;
using System.Linq;
using NUnit.Framework;
using RootAccess.AI;
using RootAccess.Core;
using RootAccess.Gameplay;
using RootAccess.Player;
using RootAccess.Systems;
using UnityEngine;
using UnityEngine.TestTools;

namespace RootAccess.Tests.PlayMode
{
    /// <summary>
    /// Level 6 (the Halden antenna) in the real scene: the gondola carries a player standing on it, the hacked fan lifts
    /// the player over a cliff too tall to jump, the rusty grates give way and come back, and the broadcast at the
    /// transmitter calls up the drones, then opens the way to the exit.
    /// </summary>
    public sealed class SpireTests
    {
        [SetUp]
        public void SetUp()
        {
            Assert.IsTrue(ServiceLocator.TryGet(out SessionService session), "SessionService missing.");
            session.UseSaveDirectory(System.IO.Path.Combine(Application.temporaryCachePath, "RootAccessTestSaves"));
            session.ResetSession();
        }

        [TearDown]
        public void TearDown() => SliceTestUtil.ResetTime();

        [UnityTest, Timeout(90000)]
        public IEnumerator Gondola_CarriesThePlayerStandingOnIt()
        {
            yield return SliceTestUtil.LoadScene(SceneNames.Level06, "l6_gondola");
            MovingPlatform gondola = Object.FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None).OrderBy(g => g.transform.position.x).First();
            PlayerController player = SliceTestUtil.Player;
            float span = gondola.PointB.x - gondola.PointA.x;
            Assert.Greater(span, 5f, "The gondola crosses a wide pit.");

            yield return SliceTestUtil.WaitUntil(() => Mathf.Abs(gondola.transform.position.x - gondola.PointA.x) < 0.05f && gondola.HeadingToB,
                20f, "the gondola to park at the near ledge");
            player.Respawn(gondola.transform.position + Vector3.up * 0.6f);
            yield return SliceTestUtil.WaitUntil(() => player.IsGrounded, 3f, "the player to land on the gondola");
            float startX = player.Position.x;

            yield return SliceTestUtil.WaitUntil(() => Mathf.Abs(gondola.transform.position.x - gondola.PointB.x) < 0.05f, 20f, "the gondola to reach the far ledge");
            Assert.Greater(player.Position.x - startX, span * 0.8f, "A player standing still rides along to the far side.");
            Assert.IsTrue(player.IsAlive);
            Assert.Greater(player.Position.y, gondola.transform.position.y, "The rider is still on the deck, not in the pit.");
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator HackedFan_LiftsThePlayerOverTheCliff()
        {
            yield return SliceTestUtil.LoadScene(SceneNames.Level06, "l6_fans");
            Updraft fan = Object.FindObjectsByType<Updraft>(FindObjectsSortMode.None).OrderBy(f => f.transform.position.x).First();
            Terminal terminal = Object.FindObjectsByType<Terminal>(FindObjectsSortMode.None).Single(t => t.TerminalId == "l6_fan");
            Assert.IsTrue(terminal.Targets.Contains(fan), "The fan terminal drives the first fan.");
            Assert.IsFalse(fan.IsRunning, "The first fan starts switched off.");

            PlayerController player = SliceTestUtil.Player;
            Vector3 grill = fan.transform.position;
            player.Respawn(grill + Vector3.up * 0.3f);
            yield return new WaitForSeconds(1f);
            Assert.Less(player.Position.y, grill.y + 1f, "A stopped fan lifts nobody.");

            fan.ApplyHack(HackAction.Activate);
            Assert.IsTrue(fan.IsRunning);
            float bottom = player.Position.y;
            yield return SliceTestUtil.WaitUntil(() => player.Position.y > bottom + 9f, 6f, "the updraft to lift the player above the cliff");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator Grate_GivesWay_ThenComesBack()
        {
            yield return SliceTestUtil.LoadScene(SceneNames.Level06, "l6_grates");
            CrumblingPlatform grate = Object.FindObjectsByType<CrumblingPlatform>(FindObjectsSortMode.None).OrderBy(g => g.transform.position.x).First();
            PlayerController player = SliceTestUtil.Player;
            player.Respawn(grate.transform.position + Vector3.up * 0.5f);
            yield return SliceTestUtil.WaitUntil(() => grate.IsShaking || !grate.IsSolid, 2f, "the grate to start shaking under the player");
            yield return SliceTestUtil.WaitUntil(() => !grate.IsSolid, 2f, "the grate to drop out");
            yield return SliceTestUtil.WaitUntil(() => grate.IsSolid && !grate.IsShaking, 8f, "the grate to come back");
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator Broadcast_RaisesTheDrones_ThenOpensTheWayToTheExit()
        {
            yield return SliceTestUtil.LoadScene(SceneNames.Level06, "l6_uplink");
            UplinkStation station = Object.FindFirstObjectByType<UplinkStation>();
            Assert.IsNotNull(station, "Level 6 ends at the transmitter.");
            Terminal terminal = Object.FindObjectsByType<Terminal>(FindObjectsSortMode.None).Single(t => t.TerminalId == "l6_uplink");
            Assert.IsTrue(terminal.Targets.Contains(station), "The uplink terminal arms the transmitter.");
            Door door = Object.FindObjectsByType<Door>(FindObjectsSortMode.None).OrderBy(d => Mathf.Abs(d.transform.position.x - station.transform.position.x)).First();
            Assert.IsFalse(door.IsOpen, "The way to the exit stays shut until the broadcast is done.");
            Drone[] waves = Object.FindObjectsByType<Drone>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(d => d.name.StartsWith("Drone_Uplink")).ToArray();
            Assert.AreEqual(3, waves.Length, "Three drones wait for the broadcast.");
            Assert.IsTrue(waves.All(d => !d.gameObject.activeInHierarchy), "They stay switched off until it starts.");

            string reported = null;
            System.Action<ObjectiveReported> onReported = evt => { if (evt.Type == ObjectiveType.Reach) reported = evt.TargetId; };
            EventBus<ObjectiveReported>.Subscribe(onReported);
            try
            {
                station.ApplyHack(HackAction.Activate);
                PlayerController player = SliceTestUtil.Player;
                player.Respawn(station.transform.position + Vector3.right * 4f + Vector3.up * 0.3f);
                yield return SliceTestUtil.WaitUntil(() => station.Transfer.Started, 3f, "the broadcast to start with the player at the transmitter");
                Assert.GreaterOrEqual(station.ReleasedWaves, 1, "The first drone comes as soon as it starts.");
                Assert.IsTrue(waves.Any(d => d.gameObject.activeInHierarchy));

                station.Step(station.Transfer.Seconds);
                Assert.IsTrue(station.Transfer.Completed);
                Assert.IsTrue(door.IsOpen, "A finished broadcast opens the door to the exit.");
                Assert.AreEqual("l6_broadcast", reported, "The broadcast objective is reported.");
                Assert.IsTrue(waves.Where(d => d.gameObject.activeInHierarchy).All(d => d.IsEmpDisabled), "The drones it called up drop out.");
            }
            finally
            {
                EventBus<ObjectiveReported>.Unsubscribe(onReported);
            }
        }
    }
}
