using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RootAccess.Systems;
using UnityEngine;

namespace RootAccess.Tests.EditMode
{
    /// <summary>The HUD's objective list: the next step stands out, done steps are ticked, bonuses and failures apart.</summary>
    public sealed class ObjectiveChecklistTests
    {
        private QuestDefinition _quest;
        private QuestLog _log;

        [SetUp]
        public void SetUp()
        {
            _quest = ScriptableObject.CreateInstance<QuestDefinition>();
            _quest.Configure("heist", "Heist", string.Empty, true, 100, 50,
                new ObjectiveDefinition("reach", "Reach the roof", ObjectiveType.Reach, "level01"),
                new ObjectiveDefinition("hack", "Hack the door", ObjectiveType.Hack, "terminal_door"),
                new ObjectiveDefinition("logs", "Find logs", ObjectiveType.Collect, "log", 3),
                new ObjectiveDefinition("ghost", "Bonus: stay hidden", ObjectiveType.AvoidDetection, string.Empty, 1, true));
            _log = new QuestLog();
            _log.Start(_quest);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_quest);

        private List<ChecklistMark> Marks() => ObjectiveChecklist.For(_log.Get("heist")).Select(l => l.Mark).ToList();

        [Test]
        public void TheFirstOpenStep_IsCurrent_LaterOnesPending_BonusApart()
        {
            CollectionAssert.AreEqual(new[] { ChecklistMark.Current, ChecklistMark.Pending, ChecklistMark.Pending, ChecklistMark.Bonus }, Marks());
            Assert.AreEqual("Reach the roof", ObjectiveChecklist.For(_log.Get("heist"))[0].Text, "Lines keep the quest's order.");
        }

        [Test]
        public void DoneSteps_AreTicked_AndTheNextOneBecomesCurrent()
        {
            _log.Report(ObjectiveType.Reach, "level01");
            CollectionAssert.AreEqual(new[] { ChecklistMark.Done, ChecklistMark.Current, ChecklistMark.Pending, ChecklistMark.Bonus }, Marks());

            _log.Report(ObjectiveType.Hack, "terminal_door");
            _log.Report(ObjectiveType.Collect, "log");
            ChecklistLine logs = ObjectiveChecklist.For(_log.Get("heist"))[2];
            Assert.AreEqual(ChecklistMark.Current, logs.Mark);
            Assert.IsTrue(logs.ShowsCounter, "A step that needs several finds shows its counter.");
            Assert.AreEqual(1, logs.Count);
            Assert.AreEqual(3, logs.Required);
            Assert.IsFalse(ObjectiveChecklist.For(_log.Get("heist"))[1].ShowsCounter, "Single steps carry no 0/1 counter.");
        }

        [Test]
        public void TheAlarm_FailsTheStealthBonus()
        {
            _log.ReportDetected();
            Assert.AreEqual(ChecklistMark.Failed, Marks()[3]);
            Assert.AreEqual(ChecklistMark.Current, Marks()[0], "The alarm does not change what to do next.");
        }

        [Test]
        public void NoQuest_NoLines() => Assert.IsEmpty(ObjectiveChecklist.For(null));
    }
}
