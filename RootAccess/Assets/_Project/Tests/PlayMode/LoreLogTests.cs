using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RootAccess.Core;
using RootAccess.Gameplay;
using RootAccess.Systems;
using RootAccess.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace RootAccess.Tests.PlayMode
{
    /// <summary>
    /// Data logs in the real scenes: every level hides three distinct logs, touching one records it (and hides it on the
    /// next visit), and the terminal's "Jurnale" tab lists all fifteen with the found one readable.
    /// </summary>
    public sealed class LoreLogTests
    {
        private static readonly string[] Levels =
        {
            SceneNames.Level01, SceneNames.Level02, SceneNames.Level03, SceneNames.Level04, SceneNames.Level05
        };

        [SetUp]
        public void SetUp()
        {
            Assert.IsTrue(ServiceLocator.TryGet(out SessionService session), "SessionService missing.");
            session.UseSaveDirectory(System.IO.Path.Combine(Application.temporaryCachePath, "RootAccessTestSaves"));
            session.ResetSession();
        }

        [TearDown]
        public void TearDown() => SliceTestUtil.ResetTime();

        [UnityTest, Timeout(120000)]
        public IEnumerator EveryLevel_HidesThreeDistinctLogs()
        {
            Assert.IsTrue(ServiceLocator.TryGet(out SessionService session));
            var seen = new HashSet<string>();
            foreach (string scene in Levels)
            {
                yield return SliceTestUtil.LoadScene(scene);
                LoreLog[] logs = Object.FindObjectsByType<LoreLog>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Assert.AreEqual(3, logs.Length, $"{scene} hides three data logs.");
                foreach (LoreLog log in logs)
                {
                    Assert.IsNotNull(log.Log, $"{log.name} in {scene} has no LoreDefinition.");
                    Assert.IsTrue(seen.Add(log.Log.LoreId), $"{log.Log.LoreId} is placed twice.");
                }
            }

            Assert.AreEqual(session.Database.Lore.Length, seen.Count, "Every log of the database is placed in exactly one level.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator TouchingALog_RecordsIt_AndTheTerminalShowsIt()
        {
            Assert.IsTrue(ServiceLocator.TryGet(out SessionService session));
            yield return SliceTestUtil.LoadScene(SceneNames.Level01);
            LoreLog log = Object.FindObjectsByType<LoreLog>(FindObjectsSortMode.None).OrderBy(l => l.transform.position.x).First();
            int credits = session.Session.Wallet.Credits;

            SliceTestUtil.Player.Respawn(log.transform.position + Vector3.up * 0.1f);
            yield return SliceTestUtil.WaitUntil(() => session.Session.Lore.Has(log.Log.LoreId), 5f, "the log to be collected");
            Assert.IsFalse(log.gameObject.activeSelf, "A collected log disappears.");
            Assert.Greater(session.Session.Wallet.Credits, credits, "Finding a log pays a small reward.");

            TerminalScreenView terminal = Object.FindFirstObjectByType<TerminalScreenView>(FindObjectsInactive.Include);
            Assert.IsNotNull(terminal, "TerminalScreenView missing from UIRoot.");
            Assert.IsTrue(terminal.Open(new TerminalScreenRequested(TerminalTab.Lore)));
            Assert.AreEqual(TerminalTab.Lore, terminal.CurrentTab);
            Assert.AreEqual(session.Database.Lore.Length, terminal.EntryCount, "The tab lists every log, found or not.");
            Assert.IsTrue(terminal.EntryData.Any(d => d is LoreDefinition l && l.LoreId == log.Log.LoreId));
            terminal.Close();

            yield return SliceTestUtil.LoadScene(SceneNames.Level01);
            yield return null;
            Assert.AreEqual(2, Object.FindObjectsByType<LoreLog>(FindObjectsSortMode.None).Length, "Found logs stay hidden on the next visit.");
        }
    }
}
