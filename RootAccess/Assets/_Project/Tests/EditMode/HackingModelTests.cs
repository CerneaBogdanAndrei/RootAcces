using NUnit.Framework;
using RootAccess.Hacking;

namespace RootAccess.Tests.EditMode
{
    public sealed class PatternMemoryModelTests
    {
        private static PatternInputResult PlayRound(PatternMemoryModel model)
        {
            model.BeginInput();
            PatternInputResult last = PatternInputResult.Ignored;
            int length = model.CurrentLength;
            for (int i = 0; i < length; i++) last = model.Submit(model.Sequence[i]);
            return last;
        }

        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            var a = new PatternMemoryModel(9, 3, 3, 1234);
            var b = new PatternMemoryModel(9, 3, 3, 1234);
            CollectionAssert.AreEqual(a.Sequence, b.Sequence);
        }

        [Test]
        public void Sequence_HasNoImmediateRepeatsAndStaysInRange()
        {
            var model = new PatternMemoryModel(4, 4, 6, 99);
            for (int i = 0; i < model.Sequence.Count; i++)
            {
                Assert.That(model.Sequence[i], Is.InRange(0, 3));
                if (i > 0) Assert.AreNotEqual(model.Sequence[i - 1], model.Sequence[i]);
            }
        }

        [Test]
        public void CorrectInput_CompletesAllRoundsWithGrowingLength()
        {
            var model = new PatternMemoryModel(9, 3, 3, 7);

            Assert.AreEqual(3, model.CurrentLength);
            Assert.AreEqual(PatternInputResult.RoundComplete, PlayRound(model));
            Assert.AreEqual(4, model.CurrentLength);
            Assert.AreEqual(PatternInputResult.RoundComplete, PlayRound(model));
            Assert.AreEqual(PatternInputResult.PuzzleComplete, PlayRound(model));
            Assert.IsTrue(model.IsComplete);
        }

        [Test]
        public void WrongInput_FailsPuzzle()
        {
            var model = new PatternMemoryModel(9, 3, 3, 7);
            model.BeginInput();
            int wrong = (model.Sequence[0] + 1) % 9;

            Assert.AreEqual(PatternInputResult.Wrong, model.Submit(wrong));
            Assert.IsTrue(model.IsFailed);
            Assert.AreEqual(PatternInputResult.Ignored, model.Submit(model.Sequence[0]));
        }

        [Test]
        public void InputBeforeBeginIsIgnored()
        {
            var model = new PatternMemoryModel(9, 3, 3, 7);
            Assert.AreEqual(PatternInputResult.Ignored, model.Submit(model.Sequence[0]));
        }
    }

    public sealed class WireConnectModelTests
    {
        private static int RightIndexForColor(WireConnectModel model, int color)
        {
            for (int i = 0; i < model.WireCount; i++)
                if (model.RightColor(i) == color) return i;
            return -1;
        }

        [Test]
        public void MatchingEveryWire_CompletesPuzzle()
        {
            var model = new WireConnectModel(4, 1, 42);
            WireResult last = WireResult.Ignored;
            for (int left = 0; left < 4; left++)
            {
                Assert.AreEqual(WireResult.Selected, model.SelectLeft(left));
                last = model.ConnectRight(RightIndexForColor(model, model.LeftColor(left)));
            }

            Assert.AreEqual(WireResult.Complete, last);
            Assert.IsTrue(model.IsComplete);
            Assert.AreEqual(0, model.Mistakes);
        }

        [Test]
        public void WrongColour_CountsMistakeThenFails()
        {
            var model = new WireConnectModel(4, 1, 42);
            int wrongRight = RightIndexForColor(model, 1);

            model.SelectLeft(0);
            Assert.AreEqual(WireResult.Mistake, model.ConnectRight(wrongRight));
            model.SelectLeft(0);
            Assert.AreEqual(WireResult.Failed, model.ConnectRight(wrongRight));
            Assert.IsTrue(model.IsFailed);
            Assert.AreEqual(WireResult.Ignored, model.SelectLeft(1));
        }

        [Test]
        public void ConnectWithoutSelection_IsIgnored()
        {
            var model = new WireConnectModel(3, 0, 1);
            Assert.AreEqual(WireResult.Ignored, model.ConnectRight(0));
        }

        [Test]
        public void ConnectedSocketsCannotBeReused()
        {
            var model = new WireConnectModel(3, 2, 5);
            int right = RightIndexForColor(model, 0);
            model.SelectLeft(0);
            model.ConnectRight(right);

            Assert.IsTrue(model.IsLeftDone(0));
            Assert.IsTrue(model.IsRightDone(right));
            Assert.AreEqual(WireResult.Ignored, model.SelectLeft(0));
            model.SelectLeft(1);
            Assert.AreEqual(WireResult.Ignored, model.ConnectRight(right));
        }

        [Test]
        public void SameSeed_ProducesSameShuffle()
        {
            var a = new WireConnectModel(6, 1, 77);
            var b = new WireConnectModel(6, 1, 77);
            for (int i = 0; i < 6; i++) Assert.AreEqual(a.RightColor(i), b.RightColor(i));
        }
    }

    public sealed class PortScanModelTests
    {
        private static void AimInside(PortScanModel model) => model.SetCursor(model.WindowStart + model.WindowWidth * 0.5f);

        private static void AimOutside(PortScanModel model) =>
            model.SetCursor(model.WindowStart > 0.5f ? model.WindowStart - 0.1f : model.WindowEnd + 0.05f);

        [Test]
        public void HitsInsideWindow_CompletePuzzle()
        {
            var model = new PortScanModel(3, 1, 0.8f, 0.2f, 11);

            AimInside(model);
            Assert.AreEqual(PortScanResult.Hit, model.Submit());
            AimInside(model);
            Assert.AreEqual(PortScanResult.Hit, model.Submit());
            AimInside(model);
            Assert.AreEqual(PortScanResult.Complete, model.Submit());
            Assert.IsTrue(model.IsComplete);
        }

        [Test]
        public void Hit_SpeedsUpCursor()
        {
            var model = new PortScanModel(3, 1, 1f, 0.2f, 11);
            AimInside(model);
            model.Submit();
            Assert.Greater(model.Speed, 1f);
        }

        [Test]
        public void MissesBeyondAllowance_FailPuzzle()
        {
            var model = new PortScanModel(3, 1, 0.8f, 0.2f, 11);

            AimOutside(model);
            Assert.AreEqual(PortScanResult.Miss, model.Submit());
            AimOutside(model);
            Assert.AreEqual(PortScanResult.Failed, model.Submit());
            Assert.IsTrue(model.IsFailed);
            Assert.AreEqual(PortScanResult.Ignored, model.Submit());
        }

        [Test]
        public void Tick_CursorBouncesInsideUnitRange()
        {
            var model = new PortScanModel(3, 1, 1f, 0.2f, 11);
            model.SetCursor(0.9f);

            model.Tick(0.3f);

            Assert.AreEqual(0.8f, model.Cursor, 0.0001f);
            model.Tick(0.1f);
            Assert.AreEqual(0.7f, model.Cursor, 0.0001f, "After bouncing the cursor keeps moving backwards.");
        }
    }
}
