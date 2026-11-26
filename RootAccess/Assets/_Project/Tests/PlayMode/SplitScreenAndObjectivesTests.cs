using System.Collections;
using System.Linq;
using NUnit.Framework;
using RootAccess.Core;
using RootAccess.Hacking;
using RootAccess.Systems;
using RootAccess.UI;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace RootAccess.Tests.PlayMode
{
    /// <summary>
    /// Split-screen co-op keeps the Infiltrator's dialogue box and hacking minigame on the left half, next to the Operator's
    /// console, and draws every menu above the console instead of under it; the HUD lists every objective of the contract.
    /// </summary>
    public sealed class SplitScreenAndObjectivesTests
    {
        private const float Tolerance = 1f;

        [SetUp]
        public void SetUp()
        {
            Assert.IsTrue(ServiceLocator.TryGet(out SessionService session), "SessionService missing.");
            session.UseSaveDirectory(System.IO.Path.Combine(Application.temporaryCachePath, "RootAccessTestSaves"));
            session.ResetSession();
        }

        [TearDown]
        public void TearDown()
        {
            if (ServiceLocator.TryGet(out IOperatorConsole console)) console.Stop();
            SliceTestUtil.ResetTime();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator SplitScreen_KeepsDialogueAndHackingOnTheLeft_AndMenusAboveTheConsole()
        {
            Assert.IsTrue(ServiceLocator.TryGet(out IOperatorConsole console));
            console.StartLocal();
            yield return SliceTestUtil.LoadScene(SceneNames.Level01, "l1_security");
            OperatorConsoleView operatorView = Object.FindFirstObjectByType<OperatorConsoleView>(FindObjectsInactive.Include);
            yield return SliceTestUtil.WaitUntil(() => operatorView.IsVisible, 5f, "the Operator console on the right half");
            yield return null;
            Canvas.ForceUpdateCanvases();

            var canvas = (RectTransform)operatorView.transform.parent;
            Assert.Greater(canvas.rect.width, 100f, "The UI canvas has a real size.");
            var dialogue = (RectTransform)Object.FindFirstObjectByType<DialogueBoxView>(FindObjectsInactive.Include).transform;
            var hacking = (RectTransform)Object.FindFirstObjectByType<HackingOverlay>(FindObjectsInactive.Include).transform.Find("Frame");
            var hud = (RectTransform)Object.FindFirstObjectByType<HudView>(FindObjectsInactive.Include).transform;

            // The canvas pivot is the screen centre, so the Infiltrator's half is everything left of x = 0.
            Assert.LessOrEqual(SpanX(dialogue, canvas).max, Tolerance, "The dialogue box stays on the Infiltrator's half.");
            Assert.LessOrEqual(SpanX(hacking, canvas).max, Tolerance, "The hacking minigame stays on the Infiltrator's half.");
            Assert.Greater(SpanX(dialogue, canvas).width, canvas.rect.width * 0.4f, "Scaled to fit, not shrunk to nothing.");
            Assert.AreEqual(0.5f, hud.anchorMax.x, 1e-3f, "The HUD follows the gameplay camera onto the left half.");

            int consoleIndex = operatorView.transform.GetSiblingIndex();
            Component[] menus =
            {
                Object.FindFirstObjectByType<PauseMenuView>(FindObjectsInactive.Include),
                Object.FindFirstObjectByType<SettingsMenuView>(FindObjectsInactive.Include),
                Object.FindFirstObjectByType<TerminalScreenView>(FindObjectsInactive.Include),
                Object.FindFirstObjectByType<GameOverView>(FindObjectsInactive.Include),
                Object.FindFirstObjectByType<LevelCompleteView>(FindObjectsInactive.Include)
            };
            foreach (Component menu in menus)
                Assert.Greater(menu.transform.GetSiblingIndex(), consoleIndex, $"{menu.GetType().Name} is drawn whole, above the Operator console.");

            console.Stop();
            yield return null;
            Canvas.ForceUpdateCanvases();
            (float min, float max, float _) = SpanX(dialogue, canvas);
            Assert.Less(min, 0f, "Without co-op the dialogue box is centred again.");
            Assert.Greater(max, 0f);
            Assert.AreEqual(1f, hud.anchorMax.x, 1e-3f);
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator Hud_ListsEveryObjective_TicksThemOff_AndPointsToTheTravelTerminal()
        {
            Assert.IsTrue(ServiceLocator.TryGet(out SessionService session));
            yield return SliceTestUtil.LoadScene(SceneNames.Hub, "hub_center");
            TMP_Text list = Object.FindFirstObjectByType<HudView>(FindObjectsInactive.Include)
                .GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == "QuestObjective");
            QuestProgress quest = session.Session.Quests.ActiveMainQuest;
            Assert.IsNotNull(quest, "A new game starts with the first contract.");

            foreach (ObjectiveProgress objective in quest.Objectives)
                StringAssert.Contains(objective.Definition.Description, list.text, "Every objective of the contract is listed.");
            string first = quest.Objectives[0].Definition.Description;
            StringAssert.Contains(Strings.Hud.ObjectiveCurrent + first, list.text, "The first step is marked as the one to do now.");
            string level01 = session.Database.Levels.Single(l => l.SceneName == SceneNames.Level01).DisplayName;
            StringAssert.Contains(string.Format(Strings.Hud.TravelHint, level01), list.text, "In the hideout the HUD says where to go.");

            EventBus<ObjectiveReported>.Publish(new ObjectiveReported(ObjectiveType.Reach, quest.Objectives[0].Definition.TargetId));
            yield return null;
            StringAssert.Contains($"<s>{Strings.Panels.ObjectiveDone}{first}</s>", list.text, "A finished step is ticked and struck through.");
            StringAssert.Contains(Strings.Hud.ObjectiveCurrent + quest.Objectives[1].Definition.Description, list.text, "The next step takes over.");
        }

        private static (float min, float max, float width) SpanX(RectTransform rect, RectTransform canvas)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            float a = canvas.InverseTransformPoint(corners[0]).x;
            float b = canvas.InverseTransformPoint(corners[2]).x;
            return (Mathf.Min(a, b), Mathf.Max(a, b), Mathf.Abs(b - a));
        }
    }
}
