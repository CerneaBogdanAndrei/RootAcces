using System;
using System.IO;
using NUnit.Framework;
using RootAccess.Core;
using RootAccess.Systems;
using UnityEngine;
using UnityEngine.TestTools;

namespace RootAccess.Tests.EditMode
{
    public sealed class SaveSystemTests
    {
        private const string Slot = "test_slot";

        [Serializable]
        private sealed class SampleV1
        {
            public string name;
            public int credits;
        }

        [Serializable]
        private sealed class SampleV2
        {
            public string name;
            public int credits;
            public int xp;
        }

        private string _directory;

        [SetUp]
        public void SetUp() => _directory = Path.Combine(Path.GetTempPath(), "RootAccessTests", Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        [Test]
        public void SaveThenLoad_RoundTripsData()
        {
            var system = new SaveSystem(_directory);

            Assert.IsTrue(system.Save(Slot, new SampleV1 { name = "Root", credits = 150 }));
            SaveLoadStatus status = system.TryLoad(Slot, out SampleV1 loaded);

            Assert.AreEqual(SaveLoadStatus.Success, status);
            Assert.AreEqual("Root", loaded.name);
            Assert.AreEqual(150, loaded.credits);
            Assert.IsTrue(system.Exists(Slot));
            Assert.IsFalse(File.Exists(system.GetPath(Slot) + ".tmp"), "The temporary file must be replaced atomically.");
        }

        [Test]
        public void SavedFile_ContainsVersionedEnvelope()
        {
            var system = new SaveSystem(_directory);
            system.Save(Slot, new SampleV1 { name = "x" });

            var envelope = JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(system.GetPath(Slot)));

            Assert.AreEqual(SaveSystem.CurrentVersion, envelope.version);
            Assert.IsFalse(string.IsNullOrEmpty(envelope.savedAtUtc));
            StringAssert.Contains("\"name\":\"x\"", envelope.payload);
        }

        [Test]
        public void Load_MissingSlotReportsNotFound()
        {
            var system = new SaveSystem(_directory);
            Assert.AreEqual(SaveLoadStatus.NotFound, system.TryLoad("nothing", out SampleV1 _));
        }

        [Test]
        public void Load_NewerVersionReportsMismatch()
        {
            new SaveSystem(_directory, version: 3).Save(Slot, new SampleV1 { name = "future" });

            SaveLoadStatus status = new SaveSystem(_directory, version: 1).TryLoad(Slot, out SampleV1 data);

            Assert.AreEqual(SaveLoadStatus.VersionMismatch, status);
            Assert.IsNull(data);
        }

        [Test]
        public void Load_OlderVersionWithoutMigrationReportsMismatch()
        {
            new SaveSystem(_directory, version: 1).Save(Slot, new SampleV1 { name = "old" });

            SaveLoadStatus status = new SaveSystem(_directory, version: 2).TryLoad(Slot, out SampleV2 _);

            Assert.AreEqual(SaveLoadStatus.VersionMismatch, status);
        }

        [Test]
        public void Load_OlderVersionIsUpgradedThroughMigrations()
        {
            new SaveSystem(_directory, version: 1).Save(Slot, new SampleV1 { name = "old", credits = 40 });
            var system = new SaveSystem(_directory, version: 3);
            system.RegisterMigration(1, json =>
            {
                var v1 = JsonUtility.FromJson<SampleV1>(json);
                return JsonUtility.ToJson(new SampleV2 { name = v1.name, credits = v1.credits, xp = 0 });
            });
            system.RegisterMigration(2, json =>
            {
                var v2 = JsonUtility.FromJson<SampleV2>(json);
                v2.xp = v2.credits / 2;
                return JsonUtility.ToJson(v2);
            });

            SaveLoadStatus status = system.TryLoad(Slot, out SampleV2 migrated);

            Assert.AreEqual(SaveLoadStatus.Success, status);
            Assert.AreEqual("old", migrated.name);
            Assert.AreEqual(20, migrated.xp);
        }

        [Test]
        public void Load_GarbageFileReportsCorrupted()
        {
            var system = new SaveSystem(_directory);
            Directory.CreateDirectory(_directory);
            File.WriteAllText(system.GetPath(Slot), "{ this is not json");

            Assert.AreEqual(SaveLoadStatus.Corrupted, system.TryLoad(Slot, out SampleV1 _));
        }

        [Test]
        public void Save_NullDataIsRefused()
        {
            var system = new SaveSystem(_directory);
            LogAssert.Expect(LogType.Error, $"[SaveSystem] Refusing to save null data to slot '{Slot}'.");

            Assert.IsFalse(system.Save<SampleV1>(Slot, null));
            Assert.IsFalse(system.Exists(Slot));
        }

        [Test]
        public void Delete_RemovesSlot()
        {
            var system = new SaveSystem(_directory);
            system.Save(Slot, new SampleV1());

            Assert.IsTrue(system.Delete(Slot));
            Assert.IsFalse(system.Exists(Slot));
            Assert.IsFalse(system.Delete(Slot));
        }

        [Test]
        public void GameSession_RoundTripsThroughSaveSystem()
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.Configure("emp", "EMP", string.Empty, ItemCategory.Gadget, 3, 10);
            var skill = ScriptableObject.CreateInstance<SkillDefinition>();
            skill.Configure("double_jump", "Salt dublu", string.Empty, SkillBranch.Hardware, 50, SkillEffect.DoubleJump);
            try
            {
                var session = new GameSession(TraceSettings.Default, startingCredits: 100);
                session.SceneName = "Level01_Rooftops";
                session.SetCheckpoint("Level01_Rooftops", "cp_1", new Vector2(55f, 0f));
                session.Inventory.Add(item, 2);
                session.Skills.AddXp(80);
                Assert.AreEqual(SkillUnlockResult.Success, session.Skills.TryUnlock(skill));
                session.UnlockLevel("level01");
                session.MarkTerminalHacked("terminal_door");

                var system = new SaveSystem(_directory);
                Assert.IsTrue(system.Save(Slot, session.ToSaveData()));
                Assert.AreEqual(SaveLoadStatus.Success, system.TryLoad(Slot, out SaveData data));

                var restored = new GameSession(TraceSettings.Default);
                restored.LoadFrom(data, id => id == "emp" ? item : null, _ => null, id => id == "double_jump" ? skill : null);

                Assert.AreEqual(100, restored.Wallet.Credits);
                Assert.AreEqual(30, restored.Skills.Xp);
                Assert.IsTrue(restored.Skills.HasEffect(SkillEffect.DoubleJump));
                Assert.AreEqual(2, restored.Inventory.Count(item));
                Assert.IsTrue(restored.HasCheckpoint);
                Assert.AreEqual(new Vector2(55f, 0f), restored.CheckpointPosition);
                Assert.IsTrue(restored.IsLevelUnlocked("level01"));
                Assert.IsTrue(restored.IsTerminalHacked("terminal_door"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(item);
                UnityEngine.Object.DestroyImmediate(skill);
            }
        }
    }
}
