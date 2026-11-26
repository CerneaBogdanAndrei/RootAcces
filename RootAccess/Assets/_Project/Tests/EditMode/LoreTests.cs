using System.Collections.Generic;
using NUnit.Framework;
using RootAccess.Systems;
using UnityEngine;

namespace RootAccess.Tests.EditMode
{
    public sealed class LoreTests
    {
        private readonly List<Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created) Object.DestroyImmediate(created);
            _created.Clear();
        }

        private LoreDefinition Log(string id, string level)
        {
            var log = ScriptableObject.CreateInstance<LoreDefinition>();
            log.Configure(id, id, "body", level, _created.Count);
            _created.Add(log);
            return log;
        }

        [Test]
        public void Collect_RecordsOnceAndRaisesEventOnce()
        {
            var archive = new LoreArchive();
            var raised = new List<string>();
            archive.Collected += raised.Add;

            Assert.IsTrue(archive.Collect("lore_a"));
            Assert.IsFalse(archive.Collect("lore_a"), "a log is found only once");
            Assert.IsFalse(archive.Collect(string.Empty));
            Assert.IsFalse(archive.Collect(null));

            Assert.AreEqual(1, archive.Count);
            Assert.IsTrue(archive.Has("lore_a"));
            Assert.IsFalse(archive.Has("lore_b"));
            CollectionAssert.AreEqual(new[] { "lore_a" }, raised);
        }

        [Test]
        public void CountInLevel_CountsOnlyThatLevelsFoundLogs()
        {
            var logs = new[] { Log("a", "level01"), Log("b", "level01"), Log("c", "level02") };
            var archive = new LoreArchive();
            archive.Collect("a");
            archive.Collect("c");

            Assert.AreEqual(1, archive.CountInLevel(logs, "level01"));
            Assert.AreEqual(1, archive.CountInLevel(logs, "level02"));
            Assert.AreEqual(0, archive.CountInLevel(logs, "level03"));
        }

        [Test]
        public void LoadFrom_KeepsOrderAndDropsDuplicatesAndBlanks()
        {
            var archive = new LoreArchive();
            archive.LoadFrom(new[] { "b", "a", "b", "", null, "c" });

            CollectionAssert.AreEqual(new[] { "b", "a", "c" }, archive.FoundIds);
            CollectionAssert.AreEqual(new[] { "b", "a", "c" }, archive.ToData());
        }

        [Test]
        public void GameSession_SavesAndRestoresFoundLogs()
        {
            var session = new GameSession(TraceSettings.Default);
            session.Lore.Collect("lore_l1_patrol");
            session.Lore.Collect("lore_l3_root");
            SaveData data = session.ToSaveData();

            var restored = new GameSession(TraceSettings.Default);
            restored.LoadFrom(data, _ => null, _ => null, _ => null);

            Assert.AreEqual(2, restored.Lore.Count);
            Assert.IsTrue(restored.Lore.Has("lore_l3_root"));
        }

        [Test]
        public void OldSaveWithoutLogs_LoadsWithEmptyArchive()
        {
            var data = JsonUtility.FromJson<SaveData>("{\"sceneName\":\"Hub\",\"credits\":10}");
            var session = new GameSession(TraceSettings.Default);
            session.LoadFrom(data, _ => null, _ => null, _ => null);
            Assert.AreEqual(0, session.Lore.Count);
        }

        [Test]
        public void ReportTo_AdvancesOnlyTheNamedQuest()
        {
            var archivist = ScriptableObject.CreateInstance<QuestDefinition>();
            archivist.Configure("archivist", "archivist", string.Empty, false, 0, 0,
                new ObjectiveDefinition("logs", "Find logs", ObjectiveType.Collect, "lore_any", 5));
            var other = ScriptableObject.CreateInstance<QuestDefinition>();
            other.Configure("other", "other", string.Empty, false, 0, 0,
                new ObjectiveDefinition("logs", "Find logs", ObjectiveType.Collect, "lore_any", 5));
            _created.Add(archivist);
            _created.Add(other);
            var log = new QuestLog();
            log.Start(archivist);
            log.Start(other);

            Assert.IsTrue(log.ReportTo("archivist", ObjectiveType.Collect, "lore_any", 3));
            Assert.IsFalse(log.ReportTo("missing", ObjectiveType.Collect, "lore_any", 3));

            Assert.AreEqual(3, log.Get("archivist").Objectives[0].Count);
            Assert.AreEqual(0, log.Get("other").Objectives[0].Count);
        }
    }
}
