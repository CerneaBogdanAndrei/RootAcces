using UnityEngine;

namespace RootAccess.Net
{
    /// <summary>Player 1 in asymmetric co-op: the platforming infiltrator (owns the PlayerController).</summary>
    public interface IInfiltratorRole
    {
        ulong ClientId { get; }
        Vector2 Position { get; }
        void RequestInteract(string interactableId);
    }

    /// <summary>
    /// Player 2 in asymmetric co-op: the remote operator with a map view. Operator actions are server-authoritative
    /// requests mirrored onto the same IHackable targets terminals use.
    /// </summary>
    public interface IOperatorRole
    {
        ulong ClientId { get; }
        void RequestOpenDoor(string doorId);
        void RequestLoopCamera(string cameraId, float seconds);
        void PingLocation(Vector2 worldPosition);
    }

    public enum CoopRole
    {
        Infiltrator,
        Operator
    }
}
