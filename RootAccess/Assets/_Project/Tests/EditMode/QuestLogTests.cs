using System.Collections.Generic;
using NUnit.Framework;
using RootAccess.Systems;
using UnityEngine;

namespace RootAccess.Tests.EditMode
{
    public sealed class QuestLogTests
    {
        private readonly List<Object> _created = new();
        private QuestDefinition _heist;
        private QuestDefinition _sideJob;

        [SetUp]
        public void SetUp()
        {
            _heist = Quest("heist", true,
                new ObjectiveDefinition("reach", "Reach the roof", ObjectiveType.Reach, "level01"),
                new ObjectiveDefinition("hack", "Hack the door", ObjectiveType.Hack, "terminal_door"),
                new ObjectiveDefinition("shards", "Collect shards", ObjectiveType.Collect, "data_shard", 2),
                new ObjectiveDefinition("ghost", "Stay hidden", ObjectiveType.AvoidDetection, string.Empty),
                new ObjectiveDefinition("bonus", "Find the stash", ObjectiveType.Collect, "stash", 1, isOptional: true));
            _sideJob = Quest("side", false, new ObjectiveDefinition("hack", "Hack the data terminal", ObjectiveType.Hack, "terminal_data"));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created) Object.DestroyImmediate(created);
            _created.Clear();
        }

        private QuestDefinition Quest(string id, bool main, params ObjectiveDefinition[] objectives)
        {
            var quest = ScriptableObject.CreateInstance<QuestDefinition>();
            quest.Configure(id, id, string.Empty, main, 100, 50, objectives);
            _created.Add(quest);
            return quest;
        }

        private static void CompleteRequired(QuestLog log)
        {
            log.Report(ObjectiveType.Reach, "level01");
            log.Report(ObjectiveType.Hack, "terminal_door");
            log.Report(ObjectiveType.Collect, "data_shard", 2);
        }

        [Test]
        public void Start_SameQuestTwiceIsRejected()
        {
            var log = new QuestLog();
            Assert.IsTrue(log.Start(_heist));
            Assert.IsFalse(log.Start(_heist));
            Assert.IsFalse(log.Start(null));
        }

        [Test]
        public void Report_AdvancesMatchingObjectivesOnly()
        {
            var log = new QuestLog();
            log.Start(_heist);

            Assert.IsFalse(log.Report(ObjectiveType.Hack, "terminal_other"));
            Assert.IsTrue(log.Report(ObjectiveType.Collect, "data_shard"));

            QuestProgress progress = log.Get("heist");
            Assert.AreEqual(1, progress.Objectives[2].Count);
            Assert.IsFalse(progress.Objectives[2].IsComplete);
            Assert.AreEqual(0, progress.Objectives[1].Count);
        }

        [Test]
        public void Report_CountIsClampedToRequirement()
        {
            var log = new QuestLog();
            log.Start(_heist);

            log.Report(ObjectiveType.Collect, "data_shard", 5);

            Assert.AreEqual(2, log.Get("heist").Objectives[2].Count);
        }

        [Test]
        public void FirstIncomplete_FollowsObjectiveOrder()
        {
            var log = new QuestLog();
            log.Start(_heist);
            log.Report(ObjectiveType.Reach, "level01");

            Assert.AreEqual("hack", log.Get("heist").FirstIncomplete().Definition.ObjectiveId);
        }

        [Test]
        public void TryComplete_RequiresAllRequiredObjectives()
        {
            var log = new QuestLog();
            log.Start(_heist);
            log.Report(ObjectiveType.Reach, "level01");

            Assert.IsFalse(log.TryComplete("heist"));
            Assert.AreEqual(QuestStatus.Active, log.GetStatus("heist"));
        }

        [Test]
        public void TryComplete_IgnoresOptionalObjectivesAndAwardsStealth()
        {
            var log = new QuestLog();
            QuestProgress completed = null;
            log.QuestCompleted += q => completed = q;
            log.Start(_heist);
            CompleteRequired(log);

            Assert.IsTrue(log.TryComplete("heist"));
            Assert.AreEqual(QuestStatus.Completed, log.GetStatus("heist"));
            Assert.IsTrue(log.StealthBonusEarned("heist"));
            Assert.AreEqual(1, log.Get("heist").Objectives[3].Count, "A clean run records the stealth objective as achieved on completion.");
            Assert.AreSame(_heist, completed.Definition);
            Assert.IsFalse(log.TryComplete("heist"), "A quest completes only once.");
        }

        [Test]
        public void AvoidDetection_FailsOnDetectionButQuestStillCompletes()
        {
            var log = new QuestLog();
            log.Start(_heist);
            log.ReportDetected();
            CompleteRequired(log);

            Assert.IsTrue(log.TryComplete("heist"));
            Assert.IsFalse(log.StealthBonusEarned("heist"));
            Assert.IsTrue(log.Get("heist").Objectives[3].Failed);
        }

        [Test]
        public void AvoidDetection_CannotBeReportedDirectly()
        {
            var log = new QuestLog();
            log.Start(_heist);
            Assert.IsFalse(log.Report(ObjectiveType.AvoidDetection, string.Empty));
        }

        [Test]
        public void ActiveMainQuest_PrefersMainStory()
        {
            var log = new QuestLog();
            log.Start(_sideJob);
            log.Start(_heist);

            Assert.AreSame(_heist, log.ActiveMainQuest.Definition);
            CompleteRequired(log);
            log.TryComplete("heist");
            Assert.AreSame(_sideJob, log.ActiveMainQuest.Definition, "Falls back to a side contract once the story quest is done.");
        }

        [Test]
        public void Report_ReachesEveryActiveQuest()
        {
            var log = new QuestLog();
            QuestDefinition other = Quest("other", false, new ObjectiveDefinition("hack", "Hack", ObjectiveType.Hack, "terminal_door"));
            log.Start(_heist);
            log.Start(other);
            int updates = 0;
            log.QuestUpdated += _ => updates++;

            log.Report(ObjectiveType.Hack, "terminal_door");

            Assert.AreEqual(2, updates);
            Assert.IsTrue(log.Get("other").RequiredObjectivesDone);
        }

        [Test]
        public void SaveData_RoundTripsProgress()
        {
            var log = new QuestLog();
            log.Start(_heist);
            log.Report(ObjectiveType.Collect, "data_shard");
            log.ReportDetected();

            var restored = new QuestLog();
            restored.LoadFrom(log.ToData(), id => id == "heist" ? _heist : null);

            QuestProgress progress = restored.Get("heist");
            Assert.AreEqual(QuestStatus.Active, progress.Status);
            Assert.AreEqual(1, progress.Objectives[2].Count);
            Assert.IsTrue(progress.Objectives[3].Failed);
        }

        [Test]
        public void TryComplete_StartsTheFollowUpQuest()
        {
            QuestDefinition office = Quest("office", true, new ObjectiveDefinition("vault", "Hack the vault", ObjectiveType.Hack, "l2_vault"));
            _heist.SetFollowUp(office);
            var log = new QuestLog();
            var started = new List<string>();
            log.QuestStarted += q => started.Add(q.Definition.QuestId);
            log.Start(_heist);
            CompleteRequired(log);

            Assert.IsNull(log.Get("office"), "The follow-up waits until the current quest completes.");
            Assert.IsTrue(log.TryComplete("heist"));
            Assert.AreEqual(QuestStatus.Active, log.GetStatus("office"));
            Assert.AreSame(office, log.ActiveMainQuest.Definition, "The chain moves the main story on to the next quest.");
            CollectionAssert.AreEqual(new[] { "heist", "office" }, started);
        }

        [Test]
        public void SetFollowUp_IgnoresSelfReference()
        {
            _heist.SetFollowUp(_heist);
            Assert.IsNull(_heist.FollowUpQuest);
            var log = new QuestLog();
            log.Start(_heist);
            CompleteRequired(log);
            Assert.IsTrue(log.TryComplete("heist"));
            Assert.AreEqual(1, new List<QuestProgress>(log.All).Count);
        }
    }
}
