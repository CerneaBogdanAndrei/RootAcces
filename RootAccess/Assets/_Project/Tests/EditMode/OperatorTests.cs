using System.IO;
using NUnit.Framework;
using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Tests.EditMode
{
    public sealed class OperatorTests
    {
        [Test]
        public void Model_SpendsByKindAndRegenerates()
        {
            var model = new OperatorModel(100f, 10f);
            Assert.IsTrue(model.TrySpend(OperatorTargetKind.Door));
            Assert.AreEqual(100f - OperatorModel.CostOf(OperatorTargetKind.Door), model.Energy, 1e-3f);
            Assert.IsTrue(model.TrySpend(OperatorTargetKind.Door));
            Assert.IsFalse(model.TrySpend(OperatorTargetKind.Door), "No energy, no door.");
            Assert.IsTrue(model.CanAfford(OperatorTargetKind.Guard), "A distraction is cheaper than a door.");
            model.Tick(2f);
            Assert.AreEqual(100f - 2 * OperatorModel.CostOf(OperatorTargetKind.Door) + 20f, model.Energy, 1e-3f);
            model.Tick(100f);
            Assert.AreEqual(model.MaxEnergy, model.Energy, "Regeneration stops at the maximum.");
        }

        [Test]
        public void Wire_SnapshotRoundTrip()
        {
            var snapshot = new OperatorSnapshot { PlayerPosition = new Vector2(12.5f, 3f), Energy = 42f, MaxEnergy = 100f };
            snapshot.Entities.Add(new OperatorEntity("101", OperatorTargetKind.Door, new Vector2(20f, 0f), false, true));
            snapshot.Entities.Add(new OperatorEntity("ăîș-202", OperatorTargetKind.Camera, new Vector2(19f, 7.5f), true, false));
            snapshot.Entities.Add(new OperatorEntity("303", OperatorTargetKind.Guard, new Vector2(-4f, 0f), true, true));

            var decoded = new OperatorSnapshot();
            OperatorWire.DecodeSnapshot(OperatorWire.EncodeSnapshot(snapshot), decoded);
            Assert.AreEqual(snapshot.PlayerPosition, decoded.PlayerPosition);
            Assert.AreEqual(42f, decoded.Energy);
            Assert.AreEqual(3, decoded.Entities.Count);
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(snapshot.Entities[i].Id, decoded.Entities[i].Id);
                Assert.AreEqual(snapshot.Entities[i].Kind, decoded.Entities[i].Kind);
                Assert.AreEqual(snapshot.Entities[i].Position, decoded.Entities[i].Position);
                Assert.AreEqual(snapshot.Entities[i].Active, decoded.Entities[i].Active);
                Assert.AreEqual(snapshot.Entities[i].Available, decoded.Entities[i].Available);
            }
        }

        [Test]
        public void Wire_LevelMapIsBitPacked()
        {
            const int width = 13, height = 5;
            var solid = new bool[width * height];
            for (int i = 0; i < solid.Length; i++) solid[i] = i % 3 == 0 || i == solid.Length - 1;
            var map = new OperatorLevelMap("Level01_Rooftops", new Rect(-6f, -3f, width, height), width, height, solid);

            byte[] bytes = OperatorWire.EncodeLevelMap(map);
            Assert.Less(bytes.Length, 64, "Cells travel as bits, not bools.");
            OperatorLevelMap decoded = OperatorWire.DecodeLevelMap(bytes);
            Assert.AreEqual(map.LevelName, decoded.LevelName);
            Assert.AreEqual(map.Bounds, decoded.Bounds);
            CollectionAssert.AreEqual(solid, decoded.Solid);
            Assert.IsTrue(decoded.IsSolid(0, 0));
            Assert.IsFalse(decoded.IsSolid(-1, 0));
        }

        [Test]
        public void Wire_CommandRoundTripAndVersionCheck()
        {
            Assert.AreEqual("door-7", OperatorWire.DecodeCommand(OperatorWire.EncodeCommand(new OperatorCommand("door-7"))).TargetId);
            byte[] bytes = OperatorWire.EncodeCommand(new OperatorCommand("x"));
            bytes[0] = 99;
            Assert.Throws<InvalidDataException>(() => OperatorWire.DecodeCommand(bytes));
        }

        [Test]
        public void LocalLink_DeliversBothWays()
        {
            using var link = new LocalOperatorLink();
            OperatorSnapshot seen = null;
            string command = null;
            link.SnapshotReceived += s => seen = s;
            link.CommandReceived += c => command = c.TargetId;
            var snapshot = new OperatorSnapshot();
            link.SendSnapshot(snapshot);
            link.SendCommand(new OperatorCommand("cam-1"));
            Assert.AreSame(snapshot, seen);
            Assert.AreEqual("cam-1", command);
            Assert.IsTrue(link.IsConnected);
        }
    }
}
