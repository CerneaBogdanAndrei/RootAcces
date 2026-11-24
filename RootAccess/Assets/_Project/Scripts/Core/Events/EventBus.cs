using System;
using System.Collections.Generic;
using UnityEngine;

namespace RootAccess.Core
{
    /// <summary>Marker for payloads that travel through <see cref="EventBus{T}"/>.</summary>
    public interface IEvent { }

    /// <summary>
    /// Static, type-safe publish/subscribe channel. Struct payloads keep publishing allocation-free;
    /// handlers removed while a publish is running are compacted once the outermost publish finishes.
    /// </summary>
    public static class EventBus<T> where T : struct, IEvent
    {
        private static readonly List<Action<T>> Handlers = new();
        private static readonly Predicate<Action<T>> IsNull = h => h == null;
        private static int _publishDepth;
        private static bool _hasPendingRemovals;

        static EventBus()
        {
            EventBusRegistry.Register(Clear);
        }

        public static int HandlerCount => Handlers.Count;

        public static void Subscribe(Action<T> handler)
        {
            if (handler == null)
            {
                Debug.LogError($"[EventBus] Tried to subscribe a null handler to {typeof(T).Name}.");
                return;
            }

            if (!Handlers.Contains(handler)) Handlers.Add(handler);
        }

        public static void Unsubscribe(Action<T> handler)
        {
            if (handler == null) return;
            int index = Handlers.IndexOf(handler);
            if (index < 0) return;

            if (_publishDepth > 0)
            {
                Handlers[index] = null;
                _hasPendingRemovals = true;
            }
            else
            {
                Handlers.RemoveAt(index);
            }
        }

        public static void Publish(T payload)
        {
            _publishDepth++;
            try
            {
                int count = Handlers.Count;
                for (int i = 0; i < count; i++)
                {
                    Action<T> handler = Handlers[i];
                    if (handler == null) continue;
                    try
                    {
                        handler(payload);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }
            }
            finally
            {
                _publishDepth--;
                if (_publishDepth == 0 && _hasPendingRemovals)
                {
                    Handlers.RemoveAll(IsNull);
                    _hasPendingRemovals = false;
                }
            }
        }

        public static void Clear()
        {
            Handlers.Clear();
            _publishDepth = 0;
            _hasPendingRemovals = false;
        }
    }

    /// <summary>Tracks every closed EventBus type so all channels can be reset together (domain reload, tests).</summary>
    public static class EventBusRegistry
    {
        private static readonly List<Action> ClearActions = new();

        internal static void Register(Action clear) => ClearActions.Add(clear);

        public static void ClearAll()
        {
            for (int i = 0; i < ClearActions.Count; i++) ClearActions[i]();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => ClearAll();
    }
}
