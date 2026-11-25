using System.Collections;
using System.Linq;
using NUnit.Framework;
using RootAccess.AI;
using RootAccess.Core;
using RootAccess.Gameplay;
using RootAccess.Player;
using RootAccess.Systems;
using RootAccess.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace RootAccess.Tests.PlayMode
{
    /// <summary>
    /// Personal terminal (inventory, shop, skill tree, contracts) opened from the real hub consoles, skill effects applied
    /// to the player, and the EMP grenade knocking out electronics.
    /// </summary>
    public sealed class TerminalAndGadgetTests
    {
        private const string EmpId = "emp_grenade";
        private const int EmpPrice = 60;
        private const int StartingEmps = 2;

        [TearDown]
        public void TearDown()
        {
            TerminalScreenView view = Object.FindFirstObjectByType<TerminalScreenView>(FindObjectsInactive.Include);
            if (view != null) view.Close();
            SliceTestUtil.ResetTime();
        }

        [UnityTest]
        public IEnumerator Terminal_OpensOnInventoryAndStopsTime()
        {
            yield return FreshScene(SceneNames.Hub);
            TerminalScreenView view = View();

            Assert.IsTrue(view.Open(new TerminalScreenRequested(TerminalTab.Inventory)));
            Assert.AreEqual(GameState.Terminal, SliceTestUtil.Game.State);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.AreEqual(TerminalTab.Inventory, view.CurrentTab);
            InventorySlot emp = view.EntryData.OfType<InventorySlot>().SingleOrDefault(s => s.Item.ItemId == EmpId);
            Assert.IsNotNull(emp, "A new game starts with EMP grenades in the inventory.");
            Assert.AreEqual(StartingEmps, emp.Quantity);

            view.Close();
            Assert.AreEqual(GameState.Playing, SliceTestUtil.Game.State);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator ShopTerminal_SellsEmpGrenadeForCredits()
        {
            yield return FreshScene(SceneNames.Hub);
            SessionService session = Session();
            int credits = session.Session.Wallet.Credits;

            Object.FindFirstObjectByType<ShopTerminal>().Interact(SliceTestUtil.Player);
            TerminalScreenView view = View();
            Assert.IsTrue(view.IsVisible);
            Assert.AreEqual(TerminalTab.Shop, view.CurrentTab);

            view.Select(IndexOf(view, d => d is ShopOffer offer && offer.Item.ItemId == EmpId));
            Assert.IsTrue(view.ActionAvailable, "The starting credits must cover one EMP grenade.");
            view.RunAction();

            Assert.AreEqual(credits - EmpPrice, session.Session.Wallet.Credits);
            Assert.AreEqual(StartingEmps + 1, session.Session.Inventory.Count(session.Database.FindItem(EmpId)));
            StringAssert.Contains(EmpPrice.ToString(), view.StatusText);
        }

        [UnityTest]
        public IEnumerator SkillTerminal_UnlocksSkillsAndAppliesTheirEffects()
        {
            yield return FreshScene(SceneNames.Hub);
            SessionService session = Session();
            session.Session.Skills.AddXp(400);
            var health = SliceTestUtil.Player.GetComponent<PlayerHealth>();
            int baseMax = health.Max;

            Object.FindFirstObjectByType<SkillTerminal>().Interact(SliceTestUtil.Player);
            TerminalScreenView view = View();
            Assert.AreEqual(TerminalTab.Skills, view.CurrentTab);
            Assert.AreEqual(session.Database.Skills.Length, view.EntryCount, "Every skill of the three branches is listed.");

            view.Select(IndexOf(view, d => d is SkillDefinition s && s.Prerequisites.Length > 0 && s.Effect == SkillEffect.RemoteHack));
            Assert.IsFalse(view.ActionAvailable, "Skills with missing prerequisites cannot be unlocked.");

            view.Select(IndexOf(view, d => d is SkillDefinition s && s.Effect == SkillEffect.DashCooldown));
            view.RunAction();
            Assert.IsTrue(session.HasSkillEffect(SkillEffect.DashCooldown));

            view.Select(IndexOf(view, d => d is SkillDefinition s && s.Effect == SkillEffect.ArmorPlating));
            Assert.IsTrue(view.ActionAvailable, "Armor plating becomes available once its prerequisite is unlocked.");
            view.RunAction();
            Assert.IsTrue(session.HasSkillEffect(SkillEffect.ArmorPlating));
            Assert.AreEqual(baseMax + 1, health.Max, "Armor plating adds one hit point.");
            Assert.AreEqual(100, session.Session.Skills.Xp, "400 XP - 120 (servo legs) - 180 (armor).");
        }

        [UnityTest]
        public IEnumerator PersonalTerminal_ShowsSkillsButCannotUnlockThem()
        {
            yield return FreshScene(SceneNames.Hub);
            Session().Session.Skills.AddXp(1000);
            TerminalScreenView view = View();

            view.Open(new TerminalScreenRequested(TerminalTab.Skills));
            view.Select(0);

            Assert.IsFalse(view.ActionAvailable, "Only the hideout's skill terminal unlocks skills.");
        }

        [UnityTest]
        public IEnumerator JobBoard_AcceptsAContractOnce()
        {
            yield return FreshScene(SceneNames.Hub);
            SessionService session = Session();

            Object.FindFirstObjectByType<JobBoard>().Interact(SliceTestUtil.Player);
            TerminalScreenView view = View();
            Assert.AreEqual(TerminalTab.Contracts, view.CurrentTab);
            var contract = (QuestDefinition)view.EntryData[0];
            view.Select(0);
            Assert.IsTrue(view.ActionAvailable);
            view.RunAction();

            Assert.AreEqual(QuestStatus.Active, session.Session.Quests.GetStatus(contract.QuestId));
            view.Select(0);
            Assert.IsFalse(view.ActionAvailable, "An accepted contract cannot be accepted again.");
        }

        [UnityTest]
        public IEnumerator EmpGrenade_IsThrownFromInventoryAndDisablesDroneAndTurret()
        {
            yield return FreshScene(SceneNames.RubricShowcase, "room_12");
            SessionService session = Session();
            ItemDefinition empItem = session.Database.FindItem(EmpId);
            var thrower = SliceTestUtil.Player.GetComponent<PlayerGrenadeThrower>();

            Assert.IsTrue(thrower.Throw(Vector2.right));
            Assert.AreEqual(StartingEmps - 1, session.Session.Inventory.Count(empItem), "Throwing consumes one grenade.");
            yield return null;

            EmpGrenade grenade = Object.FindFirstObjectByType<EmpGrenade>();
            Assert.IsNotNull(grenade, "The grenade is spawned from the pool.");
            Vector2 player = SliceTestUtil.Player.Position;
            Turret turret = Object.FindObjectsByType<Turret>(FindObjectsSortMode.None).OrderBy(t => Vector2.Distance(t.transform.position, player)).First();
            Drone drone = Object.FindObjectsByType<Drone>(FindObjectsSortMode.None).OrderBy(d => Vector2.Distance(d.transform.position, turret.transform.position)).First();
            Assert.IsFalse(turret.IsEmpDisabled);
            Assert.IsFalse(drone.IsEmpDisabled);

            grenade.transform.position = (drone.transform.position + turret.transform.position) * 0.5f;
            int affected = grenade.Detonate();

            Assert.GreaterOrEqual(affected, 2);
            Assert.IsTrue(turret.IsEmpDisabled, "The turret must be knocked out by the blast.");
            Assert.IsTrue(drone.IsEmpDisabled, "The drone must be knocked out by the blast.");
        }

        private static IEnumerator FreshScene(string scene, string spawn = null)
        {
            Assert.IsTrue(ServiceLocator.TryGet(out ISessionService session), "Session service missing.");
            session.ResetSession();
            yield return SliceTestUtil.LoadScene(scene, spawn);
        }

        private static SessionService Session()
        {
            Assert.IsTrue(ServiceLocator.TryGet(out SessionService session), "SessionService missing.");
            return session;
        }

        private static TerminalScreenView View()
        {
            TerminalScreenView view = Object.FindFirstObjectByType<TerminalScreenView>(FindObjectsInactive.Include);
            Assert.IsNotNull(view, "TerminalScreenView missing from UIRoot.");
            return view;
        }

        private static int IndexOf(TerminalScreenView view, System.Func<object, bool> match)
        {
            for (int i = 0; i < view.EntryData.Count; i++)
                if (match(view.EntryData[i])) return i;
            Assert.Fail("No matching entry in the terminal list.");
            return -1;
        }
    }
}
