using System.Collections;
using System.Linq;
using NUnit.Framework;
using RootAccess.AI;
using RootAccess.Core;
using RootAccess.Gameplay;
using RootAccess.Player;
using RootAccess.Systems;
using RootAccess.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace RootAccess.Tests.PlayMode
{
    /// <summary>
    /// P3 progression in the real scenes: each level's exit completes it and unlocks the next one, the Travel tab of the
    /// terminal reaches an unlocked level, the main story moves from quest to quest, and guards react to noise and alarms.
    /// </summary>
    public sealed class LevelProgressionTests
    {
        private const string Level02Id = "level02";
        private const string Level03Id = "level03";
        private const string MainQuestId = "main_first_contract";
        private const string OfficeQuestId = "main_office_tower";

        [SetUp]
        public void SetUp()
        {
            Assert.IsTrue(ServiceLocator.TryGet(out SessionService session), "SessionService missing.");
            session.UseSaveDirectory(System.IO.Path.Combine(Application.temporaryCachePath, "RootAccessTestSaves"));
        }

        [TearDown]
        public void TearDown() => SliceTestUtil.ResetTime();

        [UnityTest, Timeout(120000)]
        public IEnumerator Level01_ExitCompletesLevelUnlocksOffice_AndTravelTabGoesThere()
        {
            SessionService session = FreshSession();
            Assert.IsFalse(session.Session.IsLevelUnlocked(Level02Id), "The office tower starts locked.");
            yield return SliceTestUtil.LoadScene(SceneNames.Level01);
            yield return ReachExit("level01");
            Assert.IsTrue(session.Session.IsLevelUnlocked(Level02Id), "Finishing Level 1 must unlock the office tower.");

            yield return SliceTestUtil.LoadScene(SceneNames.Hub);
            TerminalScreenView terminal = Object.FindFirstObjectByType<TerminalScreenView>(FindObjectsInactive.Include);
            Assert.IsNotNull(terminal, "TerminalScreenView missing from UIRoot.");
            LevelDefinition[] levels = session.Database.Levels.Where(l => l != null && l.Order is >= 1 and <= 5).ToArray();
            Assert.IsTrue(terminal.Open(new TerminalScreenRequested(TerminalTab.Travel, destinations: levels)));
            Assert.AreEqual(TerminalTab.Travel, terminal.CurrentTab);
            Assert.AreEqual(5, terminal.EntryCount, "The travel tab lists levels 1–5.");

            int office = terminal.EntryData.ToList().FindIndex(d => d is LevelDefinition l && l.LevelId == Level02Id);
            int serverFarm = terminal.EntryData.ToList().FindIndex(d => d is LevelDefinition l && l.LevelId == Level03Id);
            terminal.Select(serverFarm);
            Assert.IsFalse(terminal.ActionAvailable, "Locked levels cannot be travelled to.");
            terminal.Select(office);
            Assert.IsTrue(terminal.ActionAvailable, "An unlocked level can be travelled to.");
            yield return SliceTestUtil.LoadAndWait(SceneNames.Level02, terminal.RunAction);
            Assert.AreEqual(GameState.Playing, SliceTestUtil.Game.State);
            Assert.IsTrue(SliceTestUtil.Player.IsAlive);
        }

        [UnityTest, Timeout(120000)]
        [TestCase(SceneNames.Level02, "level02", "level03", ExpectedResult = null)]
        [TestCase(SceneNames.Level03, "level03", "level04", ExpectedResult = null)]
        [TestCase(SceneNames.Level04, "level04", "level05", ExpectedResult = null)]
        [TestCase(SceneNames.Level05, "level05", null, ExpectedResult = null)]
        public IEnumerator Level_ExitCompletesLevelAndUnlocksTheNext(string scene, string levelId, string unlocks)
        {
            SessionService session = FreshSession();
            yield return SliceTestUtil.LoadScene(scene);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsTrue(SliceTestUtil.Player.IsGrounded, $"The player must stand on the first floor of {scene}.");
            string next = Object.FindFirstObjectByType<LevelExit>().TargetScene;
            yield return ReachExit(levelId);
            if (unlocks != null) Assert.IsTrue(session.Session.IsLevelUnlocked(unlocks), $"Finishing {levelId} must unlock {unlocks}.");
            else Assert.AreEqual(SceneNames.Credits, next, "The finale's exit rolls the credits.");
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator Sewers_SluiceFloodsTheShaftAndLiftsTheSwimmer()
        {
            FreshSession();
            yield return SliceTestUtil.LoadScene(SceneNames.Level04, "l4_sluice");
            FloodControl sluice = Object.FindFirstObjectByType<FloodControl>();
            Assert.IsNotNull(sluice, "Level 4 needs a sluice.");
            Terminal terminal = Object.FindObjectsByType<Terminal>(FindObjectsSortMode.None).Single(t => t.TerminalId == "l4_sluice");
            Assert.IsTrue(terminal.Targets.Contains(sluice), "The sluice terminal drives the flood control.");
            WaterBody2D water = Object.FindObjectsByType<WaterBody2D>(FindObjectsSortMode.None)
                .OrderBy(w => Vector2.Distance(w.transform.position, sluice.transform.position)).First();
            float dryDepth = water.Depth;

            PlayerController player = SliceTestUtil.Player;
            player.Respawn(new Vector3(water.transform.position.x, water.BottomY + 0.4f, 0f));
            yield return new WaitForSeconds(0.5f);
            sluice.ApplyHack(HackAction.Activate);
            yield return SliceTestUtil.WaitUntil(() => sluice.IsFlooded, 20f, "the shaft to flood");

            Assert.Greater(water.Depth, dryDepth + 8f, "Opening the sluice raises the water by the height of the shaft.");
            Assert.IsTrue(player.InWater, "The player standing in the shaft ends up in the water.");
            Assert.AreEqual(water.SurfaceY, player.WaterSurfaceY, 0.01f, "Swimmers learn the new surface level.");
            yield return new WaitForSeconds(2f);
            Assert.Greater(player.Position.y, water.BottomY + 4f, "Buoyancy carries the swimmer up with the water.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator MainStory_CompletingLevel01QuestStartsTheOfficeQuest()
        {
            SessionService session = FreshSession();
            yield return SliceTestUtil.LoadScene(SceneNames.Level01);
            QuestLog quests = session.Session.Quests;
            Assert.AreEqual(MainQuestId, quests.ActiveMainQuest.Definition.QuestId);

            EventBus<ObjectiveReported>.Publish(new ObjectiveReported(ObjectiveType.Hack, "terminal_l1_door"));
            EventBus<ObjectiveReported>.Publish(new ObjectiveReported(ObjectiveType.Collect, "data_shard"));
            EventBus<ObjectiveReported>.Publish(new ObjectiveReported(ObjectiveType.Reach, "exit_l1"));

            Assert.AreEqual(QuestStatus.Completed, quests.GetStatus(MainQuestId));
            Assert.AreEqual(QuestStatus.Active, quests.GetStatus(OfficeQuestId), "The office quest follows the first contract.");
            Assert.AreEqual(OfficeQuestId, quests.ActiveMainQuest.Definition.QuestId);
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator Guard_InvestigatesNoise_IgnoresDistantNoise_AndAnswersAlarms()
        {
            FreshSession();
            yield return SliceTestUtil.LoadScene(SceneNames.Level01, "l1_park");
            Guard guard = Object.FindObjectsByType<Guard>(FindObjectsSortMode.None).OrderBy(g => g.transform.position.x).First();
            yield return SliceTestUtil.WaitUntil(() => guard.StateId == GuardStateId.Patrol, 3f, "the guard to start patrolling");
            float startX = guard.Position.x;
            yield return SliceTestUtil.WaitUntil(() => Mathf.Abs(guard.Position.x - startX) > 0.5f, 5f, "the guard to walk its patrol route");

            Vector2 ears = guard.Position;
            EventBus<PlayerNoise>.Publish(new PlayerNoise(ears + new Vector2(8f, 0f), 4.5f));
            Assert.AreEqual(GuardStateId.Patrol, guard.StateId, "A footstep 8 m away is out of earshot.");

            // Investigate the side with a clear walkway (crates and walls both block the walk and muffle the sound).
            float side = Physics2D.Raycast(ears + Vector2.up * 0.5f, Vector2.right, 3.5f, Layers.ObstacleMask).collider == null ? 1f : -1f;
            Vector2 noise = ears + new Vector2(3f * side, 0f);
            EventBus<PlayerNoise>.Publish(new PlayerNoise(noise, 4.5f));
            Assert.AreEqual(GuardStateId.Investigate, guard.StateId, "A nearby footstep sends the guard to investigate.");
            Assert.AreEqual(noise, guard.NoisePosition);
            float before = Mathf.Abs(guard.Position.x - noise.x);
            yield return new WaitForSeconds(1f);
            Assert.Less(Mathf.Abs(guard.Position.x - noise.x), before, "The guard walks toward the noise.");

            EventBus<AlarmRaised>.Publish(new AlarmRaised(guard.Position + new Vector2(-6f, 0f)));
            Assert.AreEqual(GuardStateId.Search, guard.StateId, "An alarm in range makes the guard search its position.");
        }

        private static SessionService FreshSession()
        {
            Assert.IsTrue(ServiceLocator.TryGet(out SessionService session), "SessionService missing.");
            session.ResetSession();
            return session;
        }

        private static IEnumerator ReachExit(string levelId)
        {
            LevelExit exit = Object.FindFirstObjectByType<LevelExit>();
            Assert.IsNotNull(exit, "Every level needs a LevelExit.");
            string completed = null;
            System.Action<LevelCompleted> onCompleted = evt => completed = evt.LevelId;
            EventBus<LevelCompleted>.Subscribe(onCompleted);
            try
            {
                PlayerController player = SliceTestUtil.Player;
                player.Respawn(exit.transform.position + Vector3.up * 0.2f);
                yield return SliceTestUtil.WaitUntil(() => completed != null, 5f, "the exit to complete the level");
            }
            finally
            {
                EventBus<LevelCompleted>.Unsubscribe(onCompleted);
            }

            Assert.AreEqual(levelId, completed);
            Assert.AreEqual(GameState.LevelComplete, SliceTestUtil.Game.State);
        }
    }
}
