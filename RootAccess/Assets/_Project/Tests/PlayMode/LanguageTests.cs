using System.Collections;
using System.Linq;
using NUnit.Framework;
using RootAccess.Core;
using RootAccess.Systems;
using RootAccess.UI;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RootAccess.Tests.PlayMode
{
    /// <summary>
    /// Romanian/English from the menus: the main menu switch and the settings drop-down change every caption at once,
    /// and a level started in English shows its hints, quest and item names in English.
    /// </summary>
    public sealed class LanguageTests
    {
        private SettingsService _settings;
        private Language _before;

        [SetUp]
        public void SetUp()
        {
            Assert.IsTrue(ServiceLocator.TryGet(out _settings), "SettingsService missing.");
            _before = _settings.Current.Language;
            _settings.SetLanguage(Language.Romanian);
        }

        [TearDown]
        public void TearDown()
        {
            if (Object.FindFirstObjectByType<SettingsMenuView>() is { } view) view.Close();
            _settings.SetLanguage(_before);
            _settings.Save();
            SliceTestUtil.ResetTime();
        }

        [UnityTest]
        public IEnumerator MainMenuSwitch_ChangesEveryCaption_AndIsSaved()
        {
            yield return SliceTestUtil.LoadScene(SceneNames.MainMenu);
            MainMenuView menu = Object.FindFirstObjectByType<MainMenuView>();
            Assert.IsNotNull(menu);
            Button language = Button(menu, "Language");
            TMP_Text newGame = Caption(Button(menu, "NewGame"));
            Assert.AreEqual("Joc nou", newGame.text);
            Assert.AreEqual("Limba: Română", Caption(language).text);

            language.onClick.Invoke();
            yield return null;
            Assert.AreEqual(Language.English, Loc.Current);
            Assert.AreEqual(Language.English, _settings.Current.Language, "The choice is stored with the settings.");
            Assert.AreEqual("New game", newGame.text);
            Assert.AreEqual("Settings", Caption(Button(menu, "Settings")).text);
            Assert.AreEqual("Language: English", Caption(language).text);

            language.onClick.Invoke();
            yield return null;
            Assert.AreEqual("Joc nou", newGame.text, "Switching back restores the Romanian captions.");
        }

        [UnityTest]
        public IEnumerator SettingsDropDown_SwitchesTheLanguage()
        {
            yield return SliceTestUtil.LoadScene(SceneNames.MainMenu);
            SettingsMenuView view = Object.FindFirstObjectByType<SettingsMenuView>(FindObjectsInactive.Include);
            view.Open(null);
            yield return null;

            TMP_Dropdown dropdown = view.GetComponentsInChildren<TMP_Dropdown>(true).Single(d => d.transform.parent.name == "Language");
            CollectionAssert.AreEqual(new[] { "Română", "English" }, dropdown.options.Select(o => o.text).ToArray());
            Assert.AreEqual(0, dropdown.value);
            TMP_Text title = view.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "Title");
            Assert.AreEqual("SETĂRI", title.text);

            dropdown.value = 1;
            yield return null;
            Assert.AreEqual(Language.English, Loc.Current);
            Assert.AreEqual("SETTINGS", title.text);
            TMP_Text rowLabel = dropdown.transform.parent.GetComponentInChildren<TMP_Text>(true);
            Assert.AreEqual("Language", rowLabel.text);
        }

        [UnityTest]
        public IEnumerator LevelInEnglish_ShowsEnglishHints_QuestsAndItems()
        {
            Assert.IsTrue(ServiceLocator.TryGet(out SessionService session));
            session.UseSaveDirectory(System.IO.Path.Combine(Application.temporaryCachePath, "RootAccessTestSaves"));
            session.ResetSession();
            _settings.SetLanguage(Language.English);
            yield return SliceTestUtil.LoadScene(SceneNames.Level01, "l1_start");

            LocalizedLabel[] hints = Object.FindObjectsByType<LocalizedLabel>(FindObjectsSortMode.None)
                .Where(l => l.gameObject.scene.name == SceneNames.Level01).ToArray();
            Assert.Greater(hints.Length, 5, "Level 1 has its tutorial hints baked with both languages.");
            foreach (LocalizedLabel hint in hints)
                Assert.AreEqual(hint.English, hint.GetComponent<TMP_Text>().text, $"Hint '{hint.Romanian}' is still in Romanian.");

            Assert.AreEqual("EMP Grenade", session.Database.FindItem("emp_grenade").DisplayName);
            Assert.AreEqual("Rooftops and Park", session.Database.Levels.Single(l => l.SceneName == SceneNames.Level01).DisplayName);
            Assert.AreEqual("First Contract", session.Database.StartingQuest.Title);

            _settings.SetLanguage(Language.Romanian);
            yield return null;
            Assert.AreEqual(hints[0].Romanian, hints[0].GetComponent<TMP_Text>().text, "Hints follow a switch made during the level.");
        }

        private static Button Button(Component root, string name)
        {
            Button button = root.GetComponentsInChildren<Button>(true).SingleOrDefault(b => b.name == name);
            Assert.IsNotNull(button, $"No '{name}' button.");
            return button;
        }

        private static TMP_Text Caption(Button button) => button.GetComponentInChildren<TMP_Text>(true);
    }
}
