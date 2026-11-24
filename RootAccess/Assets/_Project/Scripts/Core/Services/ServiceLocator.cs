using System;
using System.Collections.Generic;
using UnityEngine;

namespace RootAccess.Core
{
    /// <summary>
    /// Minimal service registry for long-lived systems (GameManager, SceneLoader, AudioManager...).
    /// Prefer serialized references inside a prefab and EventBus for fire-and-forget messages.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new();

        public static void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                Debug.LogError($"[ServiceLocator] Cannot register a null {typeof(T).Name}.");
                return;
            }

            if (Services.TryGetValue(typeof(T), out object existing) && !ReferenceEquals(existing, service))
                Debug.LogWarning($"[ServiceLocator] Replacing registered {typeof(T).Name}.");
            Services[typeof(T)] = service;
        }

        public static void Unregister<T>(T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out object existing) && ReferenceEquals(existing, service))
                Services.Remove(typeof(T));
        }

        public static T Get<T>() where T : class
        {
            if (TryGet(out T service)) return service;
            Debug.LogError($"[ServiceLocator] No service registered for {typeof(T).Name}. Is the SystemsRoot prefab loaded?");
            return null;
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out object found) && found is T typed && !IsDestroyed(typed))
            {
                service = typed;
                return true;
            }

            service = null;
            return false;
        }

        public static bool IsRegistered<T>() where T : class => TryGet<T>(out _);

        public static void Clear() => Services.Clear();

        private static bool IsDestroyed(object service) => service is UnityEngine.Object unityObject && unityObject == null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Services.Clear();
    }
}
