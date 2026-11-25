using System.Collections.Generic;
using NUnit.Framework;
using RootAccess.Systems;
using UnityEngine;

namespace RootAccess.Tests.EditMode
{
    public sealed class SkillTreeTests
    {
        private readonly List<Object> _created = new();
        private SkillDefinition _ghost;
        private SkillDefinition _shadowWalker;
        private SkillDefinition _doubleJump;

        [SetUp]
        public void SetUp()
        {
            _ghost = Skill("ghost", 100, SkillEffect.SilentSteps);
            _shadowWalker = Skill("shadow_walker", 150, SkillEffect.LongerShadows, _ghost);
            _doubleJump = Skill("double_jump", 120, SkillEffect.DoubleJump);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created) Object.DestroyImmediate(created);
            _created.Clear();
        }

        private SkillDefinition Skill(string id, int cost, SkillEffect effect, params SkillDefinition[] requires)
        {
            var skill = ScriptableObject.CreateInstance<SkillDefinition>();
            skill.Configure(id, id, string.Empty, SkillBranch.Stealth, cost, effect, requires);
            _created.Add(skill);
            return skill;
        }

        [Test]
        public void Unlock_WithoutEnoughXpFails()
        {
            var tree = new SkillTree();
            tree.AddXp(99);

            Assert.AreEqual(SkillUnlockResult.NotEnoughXp, tree.TryUnlock(_ghost));
            Assert.AreEqual(99, tree.Xp);
            Assert.IsFalse(tree.IsUnlocked(_ghost));
        }

        [Test]
        public void Unlock_SpendsXpAndGrantsEffect()
        {
            var tree = new SkillTree();
            SkillDefinition unlocked = null;
            tree.SkillUnlocked += s => unlocked = s;
            tree.AddXp(130);

            Assert.AreEqual(SkillUnlockResult.Success, tree.TryUnlock(_ghost));

            Assert.AreEqual(30, tree.Xp);
            Assert.IsTrue(tree.IsUnlocked(_ghost));
            Assert.IsTrue(tree.HasEffect(SkillEffect.SilentSteps));
            Assert.AreSame(_ghost, unlocked);
        }

        [Test]
        public void Unlock_RequiresPrerequisites()
        {
            var tree = new SkillTree();
            tree.AddXp(1000);

            Assert.AreEqual(SkillUnlockResult.MissingPrerequisite, tree.TryUnlock(_shadowWalker));
            Assert.AreEqual(1000, tree.Xp, "A rejected unlock must not cost XP.");

            Assert.AreEqual(SkillUnlockResult.Success, tree.TryUnlock(_ghost));
            Assert.AreEqual(SkillUnlockResult.Success, tree.TryUnlock(_shadowWalker));
            Assert.AreEqual(750, tree.Xp);
        }

        [Test]
        public void Unlock_TwiceReportsAlreadyUnlocked()
        {
            var tree = new SkillTree();
            tree.AddXp(500);
            tree.TryUnlock(_doubleJump);

            Assert.AreEqual(SkillUnlockResult.AlreadyUnlocked, tree.TryUnlock(_doubleJump));
            Assert.AreEqual(380, tree.Xp);
        }

        [Test]
        public void Unlock_InvalidSkillIsRejected()
        {
            var tree = new SkillTree();
            Assert.AreEqual(SkillUnlockResult.InvalidSkill, tree.TryUnlock(null));
            Assert.AreEqual(SkillUnlockResult.InvalidSkill, tree.TryUnlock(Skill(string.Empty, 0, SkillEffect.None)));
        }

        [Test]
        public void PrerequisiteCheckRunsBeforeXpCheck()
        {
            var tree = new SkillTree();
            Assert.AreEqual(SkillUnlockResult.MissingPrerequisite, tree.CanUnlock(_shadowWalker));
        }

        [Test]
        public void AddXp_IgnoresNonPositiveAmounts()
        {
            var tree = new SkillTree();
            tree.AddXp(-10);
            tree.AddXp(0);
            Assert.AreEqual(0, tree.Xp);
        }

        [Test]
        public void LoadFrom_RestoresUnlocksAndEffects()
        {
            var tree = new SkillTree();
            var lookup = new Dictionary<string, SkillDefinition> { ["ghost"] = _ghost, ["double_jump"] = _doubleJump };

            tree.LoadFrom(40, new[] { "ghost", "double_jump", "unknown" }, id => lookup.TryGetValue(id, out SkillDefinition s) ? s : null);

            Assert.AreEqual(40, tree.Xp);
            Assert.IsTrue(tree.IsUnlocked(_ghost));
            Assert.IsTrue(tree.HasEffect(SkillEffect.DoubleJump));
            Assert.AreEqual(SkillUnlockResult.NotEnoughXp, tree.CanUnlock(_shadowWalker), "Prerequisite satisfied by the loaded save.");
        }
    }
}
