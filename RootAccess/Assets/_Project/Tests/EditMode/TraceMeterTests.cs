using System.Collections.Generic;
using NUnit.Framework;
using RootAccess.Systems;

namespace RootAccess.Tests.EditMode
{
    public sealed class TraceMeterTests
    {
        private static TraceSettings Settings => new()
        {
            suspiciousThreshold = 25f,
            alertThreshold = 60f,
            decayPerSecond = 10f,
            decayDelaySeconds = 2f,
            lockdownFloor = 50f
        };

        [TestCase(0f, TraceLevel.Clear)]
        [TestCase(24.9f, TraceLevel.Clear)]
        [TestCase(25f, TraceLevel.Suspicious)]
        [TestCase(59.9f, TraceLevel.Suspicious)]
        [TestCase(60f, TraceLevel.Alert)]
        [TestCase(99.9f, TraceLevel.Alert)]
        [TestCase(100f, TraceLevel.Lockdown)]
        public void Evaluate_MapsThresholdsToLevels(float value, TraceLevel expected)
        {
            Assert.AreEqual(expected, new TraceMeter(Settings).Evaluate(value));
        }

        [Test]
        public void Add_RaisesLevelAndFiresEvents()
        {
            var meter = new TraceMeter(Settings);
            var transitions = new List<TraceLevel>();
            meter.LevelChanged += (_, next) => transitions.Add(next);

            meter.Add(30f);
            meter.Add(40f);

            Assert.AreEqual(70f, meter.Value);
            Assert.AreEqual(TraceLevel.Alert, meter.Level);
            CollectionAssert.AreEqual(new[] { TraceLevel.Suspicious, TraceLevel.Alert }, transitions);
        }

        [Test]
        public void Add_ClampsAtMaxAndTriggersLockdownOnce()
        {
            var meter = new TraceMeter(Settings);
            int lockdowns = 0;
            meter.LockdownTriggered += () => lockdowns++;

            meter.Add(150f);
            meter.Add(10f);

            Assert.AreEqual(TraceMeter.Max, meter.Value);
            Assert.IsTrue(meter.IsLockdown);
            Assert.AreEqual(1, lockdowns);
        }

        [Test]
        public void Add_IgnoresNonPositiveAmounts()
        {
            var meter = new TraceMeter(Settings);
            meter.Add(-5f);
            meter.Add(0f);
            Assert.AreEqual(0f, meter.Value);
        }

        [Test]
        public void Tick_DoesNotDecayDuringQuietDelay()
        {
            var meter = new TraceMeter(Settings);
            meter.Add(40f);

            meter.Tick(1.5f);

            Assert.AreEqual(40f, meter.Value);
        }

        [Test]
        public void Tick_DecaysAfterDelayAndDropsLevel()
        {
            var meter = new TraceMeter(Settings);
            meter.Add(30f);

            meter.Tick(1.5f);
            meter.Tick(1f);

            Assert.AreEqual(20f, meter.Value, 0.001f);
            Assert.AreEqual(TraceLevel.Clear, meter.Level);
        }

        [Test]
        public void Tick_NeverGoesBelowZero()
        {
            var meter = new TraceMeter(Settings);
            meter.Add(5f);

            meter.Tick(2f);
            meter.Tick(10f);

            Assert.AreEqual(0f, meter.Value);
        }

        [Test]
        public void Lockdown_DecayStopsAtFloorAndLevelStaysLockdown()
        {
            var meter = new TraceMeter(Settings);
            meter.Add(100f);

            meter.Tick(2f);
            for (int i = 0; i < 20; i++) meter.Tick(1f);

            Assert.AreEqual(50f, meter.Value, 0.001f);
            Assert.AreEqual(TraceLevel.Lockdown, meter.Level);
        }

        [Test]
        public void ResetMeter_ClearsValueAndLockdown()
        {
            var meter = new TraceMeter(Settings);
            meter.Add(100f);

            meter.ResetMeter();

            Assert.AreEqual(0f, meter.Value);
            Assert.AreEqual(TraceLevel.Clear, meter.Level);
        }

        [Test]
        public void NewIncrease_RestartsQuietDelay()
        {
            var meter = new TraceMeter(Settings);
            meter.Add(40f);
            meter.Tick(2f);
            meter.Tick(1f);
            float afterDecay = meter.Value;

            meter.Add(5f);
            meter.Tick(1f);

            Assert.AreEqual(afterDecay + 5f, meter.Value, 0.001f);
        }
    }
}
