using NUnit.Framework;
using RootAccess.Systems;
using UnityEngine;

namespace RootAccess.Tests.EditMode
{
    public sealed class ShopTests
    {
        private ItemDefinition _emp;
        private ItemDefinition _key;

        [SetUp]
        public void SetUp()
        {
            _emp = ScriptableObject.CreateInstance<ItemDefinition>();
            _emp.Configure("emp", "EMP", string.Empty, ItemCategory.Gadget, 3, 30);
            _key = ScriptableObject.CreateInstance<ItemDefinition>();
            _key.Configure("key", "Key", string.Empty, ItemCategory.UsbKey, 1, 40);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_emp);
            Object.DestroyImmediate(_key);
        }

        [Test]
        public void Buy_ChargesAndAddsItem()
        {
            var wallet = new Wallet(100);
            var inventory = new Inventory(4);

            Assert.AreEqual(PurchaseResult.Success, Shop.TryBuy(wallet, inventory, new ShopOffer(_emp, 60)));

            Assert.AreEqual(40, wallet.Credits);
            Assert.AreEqual(1, inventory.Count(_emp));
        }

        [Test]
        public void Buy_WithoutEnoughCreditsChangesNothing()
        {
            var wallet = new Wallet(50);
            var inventory = new Inventory(4);

            Assert.AreEqual(PurchaseResult.NotEnoughCredits, Shop.TryBuy(wallet, inventory, new ShopOffer(_emp, 60)));

            Assert.AreEqual(50, wallet.Credits);
            Assert.AreEqual(0, inventory.Count(_emp));
        }

        [Test]
        public void Buy_IntoFullInventoryDoesNotCharge()
        {
            var wallet = new Wallet(500);
            var inventory = new Inventory(1);
            inventory.Add(_key);

            Assert.AreEqual(PurchaseResult.InventoryFull, Shop.TryBuy(wallet, inventory, new ShopOffer(_emp, 60)));
            Assert.AreEqual(500, wallet.Credits, "The vendor must not take credits for an item that does not fit.");
        }

        [Test]
        public void Buy_StacksIntoExistingSlotEvenWhenOtherSlotsAreFull()
        {
            var wallet = new Wallet(500);
            var inventory = new Inventory(1);
            inventory.Add(_emp);

            Assert.AreEqual(PurchaseResult.Success, Shop.TryBuy(wallet, inventory, new ShopOffer(_emp, 60)));
            Assert.AreEqual(2, inventory.Count(_emp));
        }

        [Test]
        public void InvalidOffer_IsRejected()
        {
            Assert.AreEqual(PurchaseResult.InvalidOffer, Shop.CanBuy(new Wallet(100), new Inventory(2), new ShopOffer(null, 10)));
            Assert.AreEqual(PurchaseResult.InvalidOffer, Shop.CanBuy(null, new Inventory(2), new ShopOffer(_emp, 10)));
        }

        [Test]
        public void FreeOffer_IsAlwaysAffordable()
        {
            var wallet = new Wallet();
            Assert.AreEqual(PurchaseResult.Success, Shop.TryBuy(wallet, new Inventory(2), new ShopOffer(_emp, 0)));
            Assert.AreEqual(0, wallet.Credits);
        }
    }
}
