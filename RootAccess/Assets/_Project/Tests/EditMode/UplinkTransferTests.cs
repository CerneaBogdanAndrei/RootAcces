using NUnit.Framework;
using RootAccess.Core;

namespace RootAccess.Tests.EditMode
{
    /// <summary>The Level 6 broadcast as plain state: it only runs while the player holds the transmitter.</summary>
    public sealed class UplinkTransferTests
    {
        [Test]
        public void NothingHappens_BeforeTheTransferStarts()
        {
            var transfer = new UplinkTransfer(20f, 3);
            Assert.IsFalse(transfer.Tick(5f, true));
            Assert.AreEqual(0f, transfer.Progress);
            Assert.AreEqual(0, transfer.WavesDue, "No defenders before the broadcast starts.");
            Assert.IsTrue(transfer.Start());
            Assert.IsFalse(transfer.Start(), "A transfer starts only once.");
        }

        [Test]
        public void Progress_PausesAwayFromTheTransmitter_AndNeverResets()
        {
            var transfer = new UplinkTransfer(20f, 2);
            transfer.Start();
            transfer.Tick(5f, true);
            Assert.AreEqual(0.25f, transfer.Progress, 0.0001f);
            transfer.Tick(10f, false);
            Assert.AreEqual(0.25f, transfer.Progress, 0.0001f, "Stepping away pauses the upload.");
            transfer.Tick(5f, true);
            Assert.AreEqual(50, transfer.Percent, "Coming back resumes where it stopped.");
        }

        [Test]
        public void Completes_ExactlyOnce_AfterTheFullTimeAtTheTransmitter()
        {
            var transfer = new UplinkTransfer(10f, 1);
            transfer.Start();
            Assert.IsFalse(transfer.Tick(9.5f, true));
            Assert.IsTrue(transfer.Tick(1f, true), "The tick that reaches 100% reports completion.");
            Assert.IsTrue(transfer.Completed);
            Assert.IsFalse(transfer.Running);
            Assert.IsFalse(transfer.Tick(1f, true), "A finished transfer never completes again.");
            Assert.AreEqual(100, transfer.Percent);
        }

        [Test]
        public void Waves_AreDueAtEvenFractions_CappedAtTheirCount()
        {
            var transfer = new UplinkTransfer(30f, 3);
            transfer.Start();
            Assert.AreEqual(1, transfer.WavesDue, "The first wave comes as soon as the broadcast starts.");
            transfer.Tick(9f, true);
            Assert.AreEqual(1, transfer.WavesDue);
            transfer.Tick(1.5f, true);
            Assert.AreEqual(2, transfer.WavesDue, "A third of the way: the second wave.");
            transfer.Tick(10f, true);
            Assert.AreEqual(3, transfer.WavesDue);
            transfer.Tick(10f, true);
            Assert.AreEqual(3, transfer.WavesDue, "Never more waves than there are.");
        }
    }
}
