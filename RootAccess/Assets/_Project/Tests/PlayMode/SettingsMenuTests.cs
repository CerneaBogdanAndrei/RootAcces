using System.Collections;
using System.Linq;
using NUnit.Framework;
using RootAccess.Core;
using RootAccess.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RootAccess.Tests.PlayMode
{
    /// <summary>The settings screen must show the stored values and write slider changes back to the service.</summary>
    public sealed class SettingsMenuTests
    {
        private SettingsMenuView _view;

        [TearDown]
        public void TearDown()
        {
            if (_view != null) _view.Close();
            SliceTestUtil.ResetTime();
        }

        [UnityTest]
        public IEnumerator VolumeSliders_ShowTheCurrentSettings()
        {
            yield return SliceTestUtil.LoadScene(SceneNames.MainMenu);
            Assert.IsTrue(ServiceLocator.TryGet(out SettingsService settings));
            _view = Object.FindFirstObjectByType<SettingsMenuView>(FindObjectsInactive.Include);
            Assert.IsNotNull(_view);

            _view.Open(null);
            yield return null;

            GameSettings current = settings.Current;
            AssertSlider("Master", current.masterVolume);
            AssertSlider("Music", current.musicVolume);
            AssertSlider("Sfx", current.sfxVolume);
            AssertSlider("UI", current.uiVolume);
        }

        [UnityTest]
        public IEnumerator MovingASlider_UpdatesTheSetting()
        {
            yield return SliceTestUtil.LoadScene(SceneNames.MainMenu);
            Assert.IsTrue(ServiceLocator.TryGet(out SettingsService settings));
            _view = Object.FindFirstObjectByType<SettingsMenuView>(FindObjectsInactive.Include);
            _view.Open(null);
            float previous = settings.Current.sfxVolume;
            try
            {
                Slider("Sfx").value = 0.42f;
                Assert.AreEqual(0.42f, settings.Current.sfxVolume, 0.001f);
            }
            finally
            {
                settings.SetVolume(AudioChannel.Sfx, previous);
            }
        }

        private void AssertSlider(string row, float expected)
        {
            Slider slider = Slider(row);
            Assert.AreEqual(expected, slider.value, 0.001f, $"Slider '{row}' shows {slider.value:0.00} but the setting is {expected:0.00}.");
            Assert.AreEqual(expected, slider.fillRect.anchorMax.x, 0.02f, $"Slider '{row}' fill does not match its value.");
        }

        private Slider Slider(string row)
        {
            Slider slider = _view.GetComponentsInChildren<Slider>(true).SingleOrDefault(s => s.transform.parent != null && s.transform.parent.name == row);
            Assert.IsNotNull(slider, $"No slider in row '{row}'.");
            return slider;
        }
    }
}
