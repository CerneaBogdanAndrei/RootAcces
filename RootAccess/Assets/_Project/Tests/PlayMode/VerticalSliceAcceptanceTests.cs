using System.Collections;
using System.Linq;
using NUnit.Framework;
using RootAccess.AI;
using RootAccess.Audio;
using RootAccess.Core;
using RootAccess.Gameplay;
using RootAccess.Hacking;
using RootAccess.Player;
using RootAccess.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RootAccess.Tests.PlayMode
{
    /// <summary>
    /// Phase D acceptance criterion as one scripted run through the real scenes, UI and systems:
    /// menu → New Game → Hub → Level 1 → hack the door terminal (Pattern Memory) → door opens → network node teleport →
    /// drone chases with an A* path → pause → change music volume → back to the menu → Rubric Showcase.
    /// Only player input is simulated; everything else (events, overlay, loader, mixer) runs as in the build.
    /// </summary>
    public sealed class VerticalSliceAcceptanceTests
    {
        private const string NodesSpawn = "l1_nodes";
        private const float TestMusicVolume = 0.25f;

        private float _previousMusicVolume = -1f;

        [TearDown]
        public void TearDown()
        {
            SliceTestUtil.ResetTime();
            if (_previousMusicVolume >= 0f && ServiceLocator.TryGet(out SettingsService settings))
                settings.SetVolume(AudioChannel.Music, _previousMusicVolume);
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator MenuToShowcase_FullVerticalSliceFlow()
        {
            yield return SliceTestUtil.LoadScene(SceneNames.MainMenu);
            Assert.AreEqual(GameState.MainMenu, SliceTestUtil.Game.State);

            Button newGame = MenuButton("NewGame");
            yield return SliceTestUtil.LoadAndWait(SceneNames.Hub, () => newGame.onClick.Invoke());
            Assert.AreEqual(GameState.Playing, SliceTestUtil.Game.State);
            yield return new WaitForSecondsRealtime(1f);
            Assert.IsTrue(SliceTestUtil.Player.IsGrounded, "The player must stand on the Hub floor.");

            yield return SliceTestUtil.LoadScene(SceneNames.Level01, NodesSpawn);
            PlayerController player = SliceTestUtil.Player;
            yield return HackDoorTerminal(player);
            yield return TeleportThroughNetworkNode(player);
            yield return DroneChases(player);

            SliceTestUtil.Game.TogglePause();
            Assert.AreEqual(GameState.Paused, SliceTestUtil.Game.State);
            Assert.AreEqual(0f, Time.timeScale);
            PauseMenuView pause = Object.FindFirstObjectByType<PauseMenuView>(FindObjectsInactive.Include);
            Assert.IsNotNull(pause, "Pause menu missing from UIRoot.");
            Assert.IsTrue(pause.IsVisible, "The pause menu must open when the game is paused.");

            ChangeMusicVolume();

            Button pauseToMenu = pause.GetComponentsInChildren<Button>(true).Single(b => b.name == "MainMenu");
            yield return SliceTestUtil.LoadAndWait(SceneNames.MainMenu, () => pauseToMenu.onClick.Invoke());
            Assert.AreEqual(GameState.MainMenu, SliceTestUtil.Game.State);
            Assert.AreEqual(1f, Time.timeScale, "Leaving the pause menu for the main menu must restore time.");

            Button rubric = MenuButton("Rubric");
            yield return SliceTestUtil.LoadAndWait(SceneNames.RubricShowcase, () => rubric.onClick.Invoke());
            Assert.AreEqual(GameState.Playing, SliceTestUtil.Game.State);
            Assert.GreaterOrEqual(Object.FindObjectsByType<TeleportPad>(FindObjectsSortMode.None).Length, 24, "Showcase rooms are linked by teleport pads.");
            yield return new WaitForSecondsRealtime(1f);
            Assert.IsTrue(SliceTestUtil.Player.IsAlive);
        }

        private static Button MenuButton(string name)
        {
            MainMenuView menu = Object.FindFirstObjectByType<MainMenuView>();
            Assert.IsNotNull(menu, "MainMenuView not found in the MainMenu scene.");
            Button button = menu.GetComponentsInChildren<Button>(true).SingleOrDefault(b => b.name == name);
            Assert.IsNotNull(button, $"Main menu button '{name}' not found.");
            Assert.IsTrue(button.interactable, $"Main menu button '{name}' is disabled.");
            return button;
        }

        private static IEnumerator HackDoorTerminal(PlayerController player)
        {
            Terminal terminal = Object.FindObjectsByType<Terminal>(FindObjectsSortMode.None).Single(t => t.Kind == TerminalKind.Door);
            Door door = Object.FindFirstObjectByType<Door>();
            Assert.IsFalse(door.IsOpen, "The service door starts closed.");
            Assert.IsTrue(terminal.CanInteract(player));

            terminal.Interact(player);
            Assert.AreEqual(GameState.Hacking, SliceTestUtil.Game.State, "Interacting with a terminal opens the hacking overlay.");
            Assert.AreEqual(PlayerStateId.Hack, player.StateId);

            PatternMemoryMinigame minigame = Object.FindFirstObjectByType<PatternMemoryMinigame>();
            Assert.IsNotNull(minigame, "The Pattern Memory minigame should be running.");
            yield return SolvePatternMemory(minigame);

            yield return SliceTestUtil.WaitUntil(() => door.IsOpen, 5f, "the door to open after a successful hack");
            Assert.IsTrue(terminal.IsHacked);
            yield return SliceTestUtil.WaitUntil(() => SliceTestUtil.Game.State == GameState.Playing, 5f, "the hacking overlay to close");
            Assert.AreNotEqual(PlayerStateId.Hack, player.StateId);
        }

        private static IEnumerator SolvePatternMemory(PatternMemoryMinigame minigame)
        {
            float deadline = Time.realtimeSinceStartup + 40f;
            while (minigame.Model == null || !minigame.Model.IsComplete)
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, "Timed out while solving Pattern Memory.");
                PatternMemoryModel model = minigame.Model;
                Assert.IsFalse(model != null && model.IsFailed, "Pattern Memory failed although the correct sequence was entered.");
                if (model != null && model.AwaitingInput)
                {
                    int length = model.CurrentLength;
                    for (int i = 0; i < length; i++) minigame.PressCell(model.Sequence[i]);
                }

                yield return null;
            }
        }

        private static IEnumerator TeleportThroughNetworkNode(PlayerController player)
        {
            NetworkNode[] nodes = Object.FindObjectsByType<NetworkNode>(FindObjectsSortMode.None).OrderBy(n => n.transform.position.y).ToArray();
            Assert.GreaterOrEqual(nodes.Length, 2, "Level 1 needs a linked network node pair.");
            NetworkNode ground = nodes[0];
            NetworkNode rooftop = nodes[^1];
            Assert.IsTrue(ground.CanInteract(player), "The ground node should be usable.");

            ground.Interact(player);
            Assert.AreEqual(PlayerStateId.Teleport, player.StateId);
            yield return SliceTestUtil.WaitUntil(() => player.StateId != PlayerStateId.Teleport, 5f, "the teleport to finish");

            Assert.Less(Vector2.Distance(player.Position, rooftop.ArrivalPosition), 1f, "The player should arrive at the linked rooftop node.");
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsTrue(player.IsAlive);
            Assert.Greater(player.Position.y, rooftop.transform.position.y - 1f, "The player should stay on the rooftop after arriving.");
        }

        private static IEnumerator DroneChases(PlayerController player)
        {
            Drone drone = Object.FindFirstObjectByType<Drone>();
            Assert.IsNotNull(drone, "Level 1 needs a drone.");
            Assert.IsFalse(drone.IsChasing, "The drone should idle until it sees the player.");

            Vector3 below = new(drone.transform.position.x, 0f, 0f);
            player.Respawn(below);
            yield return SliceTestUtil.WaitUntil(() => drone.IsChasing, 5f, "the drone to spot the player");
            yield return SliceTestUtil.WaitUntil(() => drone.CurrentPath.Count > 1, 5f, "an A* path from the drone to the player");

            float startDistance = Vector2.Distance(drone.transform.position, player.Position);
            yield return new WaitForSecondsRealtime(1f);
            float endDistance = Vector2.Distance(drone.transform.position, player.Position);
            Assert.Less(endDistance, startDistance, "The chasing drone should close the distance to the player.");
        }

        private void ChangeMusicVolume()
        {
            Assert.IsTrue(ServiceLocator.TryGet(out SettingsService settings), "SettingsService missing.");
            Assert.IsTrue(ServiceLocator.TryGet(out AudioManager audio), "AudioManager missing.");
            Assert.IsNotNull(audio.Mixer, "AudioManager has no mixer.");

            _previousMusicVolume = settings.Current.musicVolume;
            settings.SetVolume(AudioChannel.Music, 1f);
            Assert.IsTrue(audio.Mixer.GetFloat(MixerParameters.Music, out float fullDb), "Music volume is not an exposed mixer parameter.");
            settings.SetVolume(AudioChannel.Music, TestMusicVolume);
            Assert.IsTrue(audio.Mixer.GetFloat(MixerParameters.Music, out float quietDb));

            Assert.AreEqual(TestMusicVolume, settings.Current.musicVolume);
            Assert.Less(quietDb, fullDb - 6f, "Lowering the music slider must attenuate the Music mixer group.");
        }
    }
}
