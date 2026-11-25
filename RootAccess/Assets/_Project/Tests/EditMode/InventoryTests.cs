using System.Collections.Generic;
using NUnit.Framework;
using RootAccess.Systems;
using UnityEngine;

namespace RootAccess.Tests.EditMode
{
    public sealed class InventoryTests
    {
        private ItemDefinition _emp;
        private ItemDefinition _usbKey;
        private readonly List<Object> _created = new();

        [SetUp]
        public void SetUp()
        {
            _emp = Item("emp", 5);
            _usbKey = Item("usb", 1);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created) Object.DestroyImmediate(created);
            _created.Clear();
        }

        private ItemDefinition Item(string id, int maxStack)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.Configure(id, id, string.Empty, ItemCategory.Gadget, maxStack, 10);
            _created.Add(item);
            return item;
        }

        [Test]
        public void Add_StacksIntoExistingSlot()
        {
            var inventory = new Inventory(4);

            Assert.AreEqual(0, inventory.Add(_emp, 2));
            Assert.AreEqual(0, inventory.Add(_emp, 2));

            Assert.AreEqual(4, inventory.Count(_emp));
            Assert.AreEqual(4, inventory.Slots[0].Quantity);
            Assert.IsTrue(inventory.Slots[1].IsEmpty);
        }

        [Test]
        public void Add_SpillsIntoNewSlotWhenStackIsFull()
        {
            var inventory = new Inventory(4);

            inventory.Add(_emp, 7);

            Assert.AreEqual(5, inventory.Slots[0].Quantity);
            Assert.AreEqual(2, inventory.Slots[1].Quantity);
            Assert.AreEqual(7, inventory.Count(_emp));
        }

        [Test]
        public void Add_NonStackableItemsUseOneSlotEach()
        {
            var inventory = new Inventory(3);

            inventory.Add(_usbKey, 2);

            Assert.AreEqual(1, inventory.Slots[0].Quantity);
            Assert.AreEqual(1, inventory.Slots[1].Quantity);
            Assert.IsTrue(inventory.Slots[2].IsEmpty);
        }

        [Test]
        public void Add_ReturnsOverflowWhenFull()
        {
            var inventory = new Inventory(2);

            int overflow = inventory.Add(_emp, 13);

            Assert.AreEqual(3, overflow);
            Assert.AreEqual(10, inventory.Count(_emp));
            Assert.IsFalse(inventory.CanAdd(_emp));
            Assert.AreEqual(1, inventory.Add(_usbKey), "A full inventory rejects new item types entirely.");
        }

        [Test]
        public void Add_InvalidInputChangesNothing()
        {
            var inventory = new Inventory(2);
            int changes = 0;
            inventory.Changed += () => changes++;

            Assert.AreEqual(1, inventory.Add(null, 1), "Nothing is stored, so the whole quantity overflows.");
            Assert.AreEqual(0, inventory.Add(_emp, 0));
            Assert.AreEqual(0, changes);
        }

        [Test]
        public void CanAdd_AccountsForPartialStacks()
        {
            var inventory = new Inventory(1);
            inventory.Add(_emp, 3);

            Assert.IsTrue(inventory.CanAdd(_emp, 2));
            Assert.IsFalse(inventory.CanAdd(_emp, 3));
        }

        [Test]
        public void Remove_TakesExactAmountAcrossStacks()
        {
            var inventory = new Inventory(4);
            inventory.Add(_emp, 7);

            Assert.IsTrue(inventory.Remove(_emp, 3));

            Assert.AreEqual(4, inventory.Count(_emp));
            Assert.AreEqual(4, inventory.Slots[0].Quantity, "Removal drains the last stack first.");
            Assert.IsTrue(inventory.Slots[1].IsEmpty);
        }

        [Test]
        public void Remove_MoreThanOwnedIsAllOrNothing()
        {
            var inventory = new Inventory(4);
            inventory.Add(_emp, 2);

            Assert.IsFalse(inventory.Remove(_emp, 3));
            Assert.AreEqual(2, inventory.Count(_emp));
            Assert.IsFalse(inventory.Remove(_usbKey));
        }

        [Test]
        public void Changed_FiresOnAddAndRemove()
        {
            var inventory = new Inventory(4);
            int changes = 0;
            inventory.Changed += () => changes++;

            inventory.Add(_emp, 1);
            inventory.Remove(_emp, 1);

            Assert.AreEqual(2, changes);
        }

        [Test]
        public void SaveData_RoundTripsThroughIds()
        {
            var inventory = new Inventory(4);
            inventory.Add(_emp, 6);
            inventory.Add(_usbKey, 1);

            List<ItemStackData> data = inventory.ToData();
            var restored = new Inventory(4);
            restored.LoadFrom(data, id => id == "emp" ? _emp : id == "usb" ? _usbKey : null);

            Assert.AreEqual(6, restored.Count(_emp));
            Assert.AreEqual(1, restored.Count(_usbKey));
        }

        [Test]
        public void LoadFrom_SkipsUnknownItems()
        {
            var restored = new Inventory(2);
            restored.LoadFrom(new[] { new ItemStackData { itemId = "removed_item", quantity = 3 } }, _ => null);

            Assert.IsTrue(restored.Slots[0].IsEmpty);
        }
    }
}
