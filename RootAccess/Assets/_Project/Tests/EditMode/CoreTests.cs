using System;
using System.Collections.Generic;
using NUnit.Framework;
using RootAccess.Core;
using RootAccess.Systems;
using UnityEngine;
using UnityEngine.TestTools;

namespace RootAccess.Tests.EditMode
{
    public sealed class EventBusTests
    {
        private readonly struct Ping : IEvent
        {
            public readonly int Value;
            public Ping(int value) => Value = value;
        }

        [SetUp]
        public void SetUp() => EventBus<Ping>.Clear();

        [TearDown]
        public void TearDown() => EventBus<Ping>.Clear();

        [Test]
        public void Publish_ReachesEverySubscriberOnce()
        {
            var received = new List<int>();
            Action<Ping> handler = p => received.Add(p.Value);
            EventBus<Ping>.Subscribe(handler);
            EventBus<Ping>.Subscribe(handler);
            EventBus<Ping>.Subscribe(p => received.Add(p.Value * 10));

            EventBus<Ping>.Publish(new Ping(3));

            CollectionAssert.AreEqual(new[] { 3, 30 }, received);
        }

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            int calls = 0;
            Action<Ping> handler = _ => calls++;
            EventBus<Ping>.Subscribe(handler);
            EventBus<Ping>.Unsubscribe(handler);

            EventBus<Ping>.Publish(new Ping(1));

            Assert.AreEqual(0, calls);
            Assert.AreEqual(0, EventBus<Ping>.HandlerCount);
        }

        [Test]
        public void UnsubscribeDuringPublish_IsSafeAndCompactedAfterwards()
        {
            int second = 0;
            Action<Ping> secondHandler = _ => second++;
            Action<Ping> first = null;
            first = _ =>
            {
                EventBus<Ping>.Unsubscribe(first);
                EventBus<Ping>.Unsubscribe(secondHandler);
            };
            EventBus<Ping>.Subscribe(first);
            EventBus<Ping>.Subscribe(secondHandler);

            EventBus<Ping>.Publish(new Ping(1));

            Assert.AreEqual(0, second, "A handler removed mid-publish must not run.");
            Assert.AreEqual(0, EventBus<Ping>.HandlerCount);
        }

        [Test]
        public void ThrowingHandler_DoesNotBreakOtherSubscribers()
        {
            int calls = 0;
            EventBus<Ping>.Subscribe(_ => throw new InvalidOperationException("boom"));
            EventBus<Ping>.Subscribe(_ => calls++);
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: boom");

            EventBus<Ping>.Publish(new Ping(1));

            Assert.AreEqual(1, calls);
        }
    }

    public sealed class ServiceLocatorTests
    {
        private interface IClock { }

        private sealed class Clock : IClock { }

        [TearDown]
        public void TearDown() => ServiceLocator.Clear();

        [Test]
        public void RegisterThenTryGet_ReturnsService()
        {
            var clock = new Clock();
            ServiceLocator.Register<IClock>(clock);

            Assert.IsTrue(ServiceLocator.TryGet(out IClock found));
            Assert.AreSame(clock, found);
        }

        [Test]
        public void Unregister_OnlyRemovesTheSameInstance()
        {
            var clock = new Clock();
            ServiceLocator.Register<IClock>(clock);

            ServiceLocator.Unregister<IClock>(new Clock());
            Assert.IsTrue(ServiceLocator.IsRegistered<IClock>());

            ServiceLocator.Unregister<IClock>(clock);
            Assert.IsFalse(ServiceLocator.IsRegistered<IClock>());
        }

        [Test]
        public void DestroyedUnityObjects_AreTreatedAsMissing()
        {
            var go = new GameObject("service");
            Transform service = go.transform;
            ServiceLocator.Register(service);

            UnityEngine.Object.DestroyImmediate(go);

            Assert.IsFalse(ServiceLocator.TryGet(out Transform _));
        }
    }

    public sealed class StateMachineTests
    {
        private sealed class RecordingState : IState
        {
            private readonly string _name;
            private readonly List<string> _log;

            public RecordingState(string name, List<string> log)
            {
                _name = name;
                _log = log;
            }

            public void Enter() => _log.Add($"{_name}.enter");
            public void Tick(float deltaTime) => _log.Add($"{_name}.tick");
            public void FixedTick(float deltaTime) { }
            public void Exit() => _log.Add($"{_name}.exit");
        }

        [Test]
        public void ChangeState_RunsExitThenEnterAndTracksTime()
        {
            var log = new List<string>();
            var fsm = new StateMachine();
            var idle = new RecordingState("idle", log);
            var run = new RecordingState("run", log);

            fsm.ChangeState(idle);
            fsm.Tick(0.5f);
            fsm.ChangeState(run);
            fsm.ChangeState(run);

            CollectionAssert.AreEqual(new[] { "idle.enter", "idle.tick", "idle.exit", "run.enter" }, log);
            Assert.AreSame(idle, fsm.Previous);
            Assert.AreEqual(0f, fsm.TimeInState);
        }
    }

    public sealed class WalletTests
    {
        [Test]
        public void Spend_IsAllOrNothing()
        {
            var wallet = new Wallet(100);

            Assert.IsFalse(wallet.TrySpend(150));
            Assert.AreEqual(100, wallet.Credits);
            Assert.IsTrue(wallet.TrySpend(60));
            Assert.AreEqual(40, wallet.Credits);
            Assert.IsFalse(wallet.TrySpend(-5));
        }

        [Test]
        public void Add_IgnoresNonPositiveAndRaisesChanged()
        {
            var wallet = new Wallet();
            int last = -1;
            wallet.Changed += c => last = c;

            wallet.Add(0);
            Assert.AreEqual(-1, last);
            wallet.Add(25);
            Assert.AreEqual(25, last);
        }
    }
}
