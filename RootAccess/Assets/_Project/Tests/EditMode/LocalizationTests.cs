using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RootAccess.Core;
using RootAccess.EditorTools;
using RootAccess.Systems;
using UnityEngine;

namespace RootAccess.Tests.EditMode
{
    /// <summary>
    /// Romanian/English switch: the current language picks the text, the choice survives a restart, and every text the
    /// player can see (UI strings, items, quests, logs, dialogue, level labels) has a real English twin.
    /// </summary>
    public sealed class LocalizationTests
    {
        private static readonly Regex Placeholders = new(@"\{\d+(:[^}]*)?\}");
        private static readonly char[] RomanianLetters = { 'ă', 'â', 'î', 'ș', 'ț', 'ş', 'ţ', 'Ă', 'Â', 'Î', 'Ș', 'Ț', 'Ş', 'Ţ' };

        [TearDown]
        public void TearDown() => Loc.Set(Language.Romanian);

        [Test]
        public void T_PicksTheCurrentLanguage_AndFallsBackToRomanian()
        {
            Assert.AreEqual("Joc nou", Strings.Menu.NewGame);
            Loc.Set(Language.English);
            Assert.AreEqual("New game", Strings.Menu.NewGame);
            Assert.AreEqual("Text", Loc.T("Text", string.Empty), "A missing English text shows the Romanian one.");
        }

        [Test]
        public void Set_AnnouncesOnlyRealChanges()
        {
            var received = new List<Language>();
            Action<LanguageChanged> handler = evt => received.Add(evt.Language);
            EventBus<LanguageChanged>.Subscribe(handler);
            try
            {
                Loc.Set(Language.Romanian);
                Loc.Set(Language.English);
                Loc.Set(Language.English);
                Loc.Set(Language.Romanian);
            }
            finally
            {
                EventBus<LanguageChanged>.Unsubscribe(handler);
            }

            CollectionAssert.AreEqual(new[] { Language.English, Language.Romanian }, received);
        }

        [Test]
        public void In_ReadsTheOtherLanguage_WithoutSwitching()
        {
            bool announced = false;
            Action<LanguageChanged> handler = _ => announced = true;
            EventBus<LanguageChanged>.Subscribe(handler);
            try
            {
                Assert.AreEqual("PAUSED", Loc.In(Language.English, () => Strings.Menu.Paused));
            }
            finally
            {
                EventBus<LanguageChanged>.Unsubscribe(handler);
            }

            Assert.AreEqual(Language.Romanian, Loc.Current);
            Assert.IsFalse(announced, "Reading a translation must not redraw the game.");
        }

        [Test]
        public void EveryUiString_HasARealEnglishTwin() =>
            AssertTranslated(TextCatalog.Collect(typeof(Strings)));

        [Test]
        public void EverySeedText_HasARealEnglishTwin()
        {
            List<TextCatalog.Entry> entries = TextCatalog.Collect(typeof(SeedContent));
            foreach (SeedContent.Lore.Log log in SeedContent.Lore.Entries)
            {
                entries.Add(new TextCatalog.Entry($"Lore.{log.Id}.Title", log.Title, log.TitleEn));
                entries.Add(new TextCatalog.Entry($"Lore.{log.Id}.Body", log.Body, log.BodyEn));
            }

            Assert.Greater(entries.Count, 150, "The seed content (items, skills, quests, logs, dialogue, labels) is all there.");
            AssertTranslated(entries);
        }

        [Test]
        public void SameRomanianText_AlwaysGetsTheSameEnglishText()
        {
            Dictionary<string, string> map = null;
            Assert.DoesNotThrow(() => map = TextCatalog.EnglishByRomanian(TextCatalog.Collect(typeof(Strings), typeof(SeedContent))));
            Assert.AreEqual("The Office Tower", map["Turnul de birouri"], "The level name and the quest title stay one text.");
            Assert.IsTrue(Translations.TryEnglish(Strings.Menu.NewGame, out string english));
            Assert.AreEqual("New game", english);
            Assert.IsFalse(Translations.TryEnglish("1920 × 1080", out _), "Numbers and names are not translated.");
        }

        [Test]
        public void TheLanguage_IsSavedWithTheSettings_AndOldSettingsReadAsRomanian()
        {
            var store = new MemorySettingsStore();
            store.SetString(PlayerPrefsKeys.Settings, "{\"masterVolume\":0.5}");
            var service = new SettingsService(store);
            service.Load();
            Assert.AreEqual(Language.Romanian, service.Current.Language, "Settings saved before the switch existed stay Romanian.");

            service.SetLanguage(Language.English);
            Assert.AreEqual(Language.English, Loc.Current, "Choosing a language switches the game at once.");
            service.Save();
            Loc.Set(Language.Romanian);

            var restarted = new SettingsService(store);
            restarted.Load();
            restarted.ApplyLanguage();
            Assert.AreEqual(Language.English, restarted.Current.Language);
            Assert.AreEqual(Language.English, Loc.Current, "The next launch starts in the saved language.");
            Assert.AreEqual(0.5f, restarted.Current.masterVolume, 0.0001f);
        }

        [Test]
        public void ContentAssets_ShowTheirEnglishText_WhenEnglishIsChosen()
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            var quest = ScriptableObject.CreateInstance<QuestDefinition>();
            try
            {
                item.Configure("usb", "Cheie USB", "Deschide uși.", ItemCategory.UsbKey, 1, 10);
                item.SetEnglish("USB Key", "Opens doors.");
                var objective = new ObjectiveDefinition("find", "Găsește cheia", ObjectiveType.Collect, "usb");
                objective.SetEnglish("Find the key");
                quest.Configure("q", "Primul contract", "Descriere", true, 0, 0, objective);
                quest.SetEnglish("First Contract", "Description");
                var line = new DialogueLine("TU", "Primit.", "YOU", "Copy that.");

                Assert.AreEqual("Cheie USB", item.DisplayName);
                Assert.AreEqual("Găsește cheia", quest.Objectives[0].Description);
                Loc.Set(Language.English);
                Assert.AreEqual("USB Key", item.DisplayName);
                Assert.AreEqual("Opens doors.", item.Description);
                Assert.AreEqual("First Contract", quest.Title);
                Assert.AreEqual("Find the key", quest.Objectives[0].Description);
                Assert.AreEqual("YOU", line.Speaker);
                Assert.AreEqual("Copy that.", line.Text);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(item);
                UnityEngine.Object.DestroyImmediate(quest);
            }
        }

        private static void AssertTranslated(IEnumerable<TextCatalog.Entry> entries)
        {
            var problems = new List<string>();
            foreach (TextCatalog.Entry entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.English)) problems.Add($"{entry.Path}: no English text");
                else if (entry.English.IndexOfAny(RomanianLetters) >= 0) problems.Add($"{entry.Path}: English text still has Romanian letters: '{entry.English}'");
                string ro = string.Join(",", Placeholders.Matches(entry.Romanian ?? string.Empty).Select(m => m.Value).OrderBy(v => v));
                string en = string.Join(",", Placeholders.Matches(entry.English ?? string.Empty).Select(m => m.Value).OrderBy(v => v));
                if (ro != en) problems.Add($"{entry.Path}: placeholders differ ({ro} vs {en})");
            }

            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
