using NUnit.Framework;
using RootAccess.AI;
using UnityEngine;

namespace RootAccess.Tests.EditMode
{
    public sealed class HearingTests
    {
        [Test]
        public void CanHear_InsideRadiusOnly()
        {
            Assert.IsTrue(Hearing.CanHear(Vector2.zero, new Vector2(4f, 0f), 4.5f, false));
            Assert.IsFalse(Hearing.CanHear(Vector2.zero, new Vector2(5f, 0f), 4.5f, false));
        }

        [Test]
        public void Walls_MuffleTheNoise()
        {
            Vector2 source = new(3f, 0f);
            Assert.IsTrue(Hearing.CanHear(Vector2.zero, source, 4.5f, false));
            Assert.IsFalse(Hearing.CanHear(Vector2.zero, source, 4.5f, true), "Behind a wall only 45% of the radius carries.");
            Assert.AreEqual(4.5f * Hearing.OcclusionFactor, Hearing.EffectiveRadius(4.5f, true), 1e-4f);
        }

        [Test]
        public void SilentSteps_AndSensitivity_ScaleTheRadius()
        {
            const float footstep = 4.5f;
            const float silentStepsVolume = 0.3f;
            Vector2 guard = new(3f, 0f);
            Assert.IsTrue(Hearing.CanHear(guard, Vector2.zero, footstep, false));
            Assert.IsFalse(Hearing.CanHear(guard, Vector2.zero, footstep * silentStepsVolume, false), "Silent steps cut the radius to 30%.");
            Assert.IsFalse(Hearing.CanHear(guard, Vector2.zero, footstep, false, 0.6f), "A less sensitive guard (sniper) hears less far.");
            Assert.IsFalse(Hearing.CanHear(guard, guard, 0f, false), "A zero-radius noise is never heard.");
        }
    }
}
