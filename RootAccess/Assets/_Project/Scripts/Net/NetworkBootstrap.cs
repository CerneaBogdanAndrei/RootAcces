using Unity.Netcode;
using UnityEngine;

namespace RootAccess.Net
{
    /// <summary>
    /// Co-op entry point stub: starts/stops Netcode for GameObjects as host or client. Gameplay replication
    /// (mirroring GameSession models into NetworkVariables) is ROADMAP P7.
    /// </summary>
    public sealed class NetworkBootstrap : MonoBehaviour
    {
        private readonly IRelayService _relay = new RelayServiceStub();

        public IRelayService Relay => _relay;
        public bool IsRunning => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        public bool StartHost()
        {
            if (!TryGetManager(out NetworkManager manager)) return false;
            return manager.StartHost();
        }

        public bool StartClient()
        {
            if (!TryGetManager(out NetworkManager manager)) return false;
            return manager.StartClient();
        }

        public void Shutdown()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) NetworkManager.Singleton.Shutdown();
        }

        private static bool TryGetManager(out NetworkManager manager)
        {
            manager = NetworkManager.Singleton;
            if (manager != null) return true;
            Debug.LogWarning("[NetworkBootstrap] No NetworkManager in the scene; co-op is not set up yet (ROADMAP P7).");
            return false;
        }
    }
}
