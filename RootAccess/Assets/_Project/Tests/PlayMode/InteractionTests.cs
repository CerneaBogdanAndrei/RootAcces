using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using RootAccess.Core;
using RootAccess.Gameplay;
using RootAccess.Player;
using RootAccess.Systems;
using UnityEngine;
using UnityEngine.TestTools;

namespace RootAccess.Tests.PlayMode
{
    /// <summary>
    /// The player's own interaction detection (<see cref="PlayerInteractor"/>), as in play: standing at a terminal or a
    /// network node selects it and shows its prompt. The other tests call <c>Interact</c> directly and skip this path.
    /// </summary>
    public sealed class InteractionTests
    {
        [SetUp]
        public void SetUp()
        {
            Assert.IsTrue(ServiceLocator.TryGet(out SessionService session), "SessionService missing.");
            session.UseSaveDirectory(System.IO.Path.Combine(Application.temporaryCachePath, "RootAccessTestSaves"));
            session.ResetSession();
        }

        [TearDown]
        public void TearDown() => SliceTestUtil.ResetTime();

        [UnityTest, Timeout(60000)]
        public IEnumerator StandingAtATerminal_SelectsIt_AndShowsItsPrompt()
        {
            yield return SliceTestUtil.LoadScene(SceneNames.Level01, "l1_security");
            Terminal terminal = UnityEngine.Object.FindObjectsByType<Terminal>(FindObjectsSortMode.None).Single(t => t.TerminalId == "terminal_l1_camera");
            yield return AssertSelected(terminal, terminal.transform.position, Strings.Prompts.Hack);
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator StandingAtANetworkNode_SelectsIt()
        {
            yield return SliceTestUtil.LoadScene(SceneNames.Level01, "l1_nodes");
            NetworkNode node = UnityEngine.Object.FindObjectsByType<NetworkNode>(FindObjectsSortMode.None).OrderBy(n => n.transform.position.y).First();
            yield return AssertSelected(node, node.transform.position, Strings.Prompts.Teleport);
        }

        private static IEnumerator AssertSelected(Component target, Vector3 position, string expectedPrompt)
        {
            PlayerController player = SliceTestUtil.Player;
            var interactor = player.GetComponent<PlayerInteractor>();
            string prompt = null;
            bool visible = false;
            Action<InteractionPromptChanged> onPrompt = evt =>
            {
                prompt = evt.Prompt;
                visible = evt.Visible;
            };
            EventBus<InteractionPromptChanged>.Subscribe(onPrompt);
            try
            {
                player.Respawn(position + Vector3.up * 0.1f);
                yield return SliceTestUtil.WaitUntil(() => interactor.Current != null, 2f, $"the player to select {target.name} while standing at it");
                Assert.AreSame(target, interactor.Current as Component, "The closest interactable is the one under the player.");
                Assert.IsTrue(visible, "Its prompt is shown.");
                Assert.AreEqual(expectedPrompt, prompt);
            }
            finally
            {
                EventBus<InteractionPromptChanged>.Unsubscribe(onPrompt);
            }
        }
    }
}
