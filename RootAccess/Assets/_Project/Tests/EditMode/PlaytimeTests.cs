using NUnit.Framework;
using RootAccess.Core;
using RootAccess.Systems;
using UnityEngine;

namespace RootAccess.Tests.EditMode
{
    public sealed class PlaytimeTests
    {
        [TestCase(0f, "0:00")]
        [TestCase(7.9f, "0:07")]
        [TestCase(425f, "7:05")]
        [TestCase(3725f, "1:02:05")]
        public void Clock_FormatsDurations(float seconds, string expected) => Assert.AreEqual(expected, TimeFormat.Clock(seconds));

        [Test]
        public void RecordLevelTime_KeepsTheBestTime()
        {
            var session = new GameSession(TraceSettings.Default);
            Assert.IsTrue(session.RecordLevelTime("level01", 420f));
            Assert.IsFalse(session.RecordLevelTime("level01", 500f), "A slower run is not a new best.");
            Assert.IsTrue(session.RecordLevelTime("level01", 380f));
            Assert.IsFalse(session.RecordLevelTime("", 10f));
            Assert.IsFalse(session.RecordLevelTime("level02", 0f));
            Assert.AreEqual(380f, session.LevelTimes["level01"]);
        }

        [Test]
        public void LevelTimes_SurviveSaveAndLoad_AndOldSavesLoadWithout()
        {
            var session = new GameSession(TraceSettings.Default);
            session.RecordLevelTime("level01", 401f);
            session.RecordLevelTime("level02", 455f);
            SaveData data = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(session.ToSaveData()));

            var loaded = new GameSession(TraceSettings.Default);
            loaded.LoadFrom(data, _ => null, _ => null, _ => null);
            Assert.AreEqual(401f, loaded.LevelTimes["level01"]);
            Assert.AreEqual(455f, loaded.LevelTimes["level02"]);

            SaveData legacy = JsonUtility.FromJson<SaveData>("{\"sceneName\":\"Hub\",\"credits\":10}");
            Assert.IsNotNull(legacy.levelTimes, "Saves written before level times existed still load.");
            var fromLegacy = new GameSession(TraceSettings.Default);
            fromLegacy.LoadFrom(legacy, _ => null, _ => null, _ => null);
            Assert.AreEqual(0, fromLegacy.LevelTimes.Count);
        }
    }
}
