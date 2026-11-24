using System.Threading.Tasks;
using UnityEngine;

namespace RootAccess.Net
{
    /// <summary>Relay allocation facade (host creates a join code, client joins with it).</summary>
    public interface IRelayService
    {
        bool IsAvailable { get; }
        Task<string> CreateHostAllocationAsync(int maxPlayers);
        Task<bool> JoinWithCodeAsync(string joinCode);
    }

    /// <summary>
    /// Placeholder until ROADMAP P7: the Multiplayer Services / Relay integration is intentionally not wired yet.
    /// Calls log and fail gracefully so menus can already be built against the interface.
    /// </summary>
    public sealed class RelayServiceStub : IRelayService
    {
        private const string NotImplemented = "[Relay] Co-op relay is planned for ROADMAP P7 and not implemented yet.";

        public bool IsAvailable => false;

        public Task<string> CreateHostAllocationAsync(int maxPlayers)
        {
            Debug.LogWarning(NotImplemented);
            return Task.FromResult<string>(null);
        }

        public Task<bool> JoinWithCodeAsync(string joinCode)
        {
            Debug.LogWarning(NotImplemented);
            return Task.FromResult(false);
        }
    }
}
