using System;
using System.Collections.Generic;
using UnityEngine;

namespace RootAccess.Core
{
    /// <summary>How the second player joins: nobody, same PC (split-screen, gamepad) or LAN (host / client).</summary>
    public enum CoopMode
    {
        None,
        LocalSplit,
        LanHost,
        LanClient
    }

    public enum OperatorTargetKind : byte
    {
        Door,
        Camera,
        Guard
    }

    /// <summary>
    /// Something the Operator can act on from the map: doors toggle, cameras loop their feed, guards get distracted by a
    /// fake noise. Implemented by gameplay objects, which register themselves in <see cref="OperatorRegistry"/>.
    /// </summary>
    public interface IOperatorTarget
    {
        string OperatorId { get; }
        OperatorTargetKind OperatorKind { get; }
        Vector2 OperatorPosition { get; }

        /// <summary>Door open / camera looped or disabled / guard alerted.</summary>
        bool OperatorActive { get; }

        bool CanOperate { get; }
        void Operate();
    }

    /// <summary>Live operator targets of the loaded level (registered in OnEnable, removed in OnDisable).</summary>
    public static class OperatorRegistry
    {
        private static readonly List<IOperatorTarget> Targets = new();

        public static IReadOnlyList<IOperatorTarget> All => Targets;

        public static void Register(IOperatorTarget target)
        {
            if (target != null && !Targets.Contains(target)) Targets.Add(target);
        }

        public static void Unregister(IOperatorTarget target) => Targets.Remove(target);

        public static IOperatorTarget Find(string id)
        {
            foreach (IOperatorTarget target in Targets)
                if (target.OperatorId == id) return target;
            return null;
        }
    }

    /// <summary>One map icon of the snapshot sent to the Operator.</summary>
    public readonly struct OperatorEntity
    {
        public readonly string Id;
        public readonly OperatorTargetKind Kind;
        public readonly Vector2 Position;
        public readonly bool Active;
        public readonly bool Available;

        public OperatorEntity(string id, OperatorTargetKind kind, Vector2 position, bool active, bool available)
        {
            Id = id;
            Kind = kind;
            Position = position;
            Active = active;
            Available = available;
        }
    }

    /// <summary>What the Operator sees, ~10 times a second: the infiltrator, every target and the operator's energy.</summary>
    public sealed class OperatorSnapshot
    {
        public Vector2 PlayerPosition { get; set; }
        public float Energy { get; set; }
        public float MaxEnergy { get; set; }
        public List<OperatorEntity> Entities { get; } = new();
    }

    /// <summary>Coarse level outline for the map (1 cell per world unit, true = solid), sent once per level.</summary>
    public sealed class OperatorLevelMap
    {
        public OperatorLevelMap(string levelName, Rect bounds, int width, int height, bool[] solid)
        {
            LevelName = levelName ?? string.Empty;
            Bounds = bounds;
            Width = width;
            Height = height;
            Solid = solid ?? new bool[width * height];
            if (Solid.Length != width * height) throw new ArgumentException("Solid cell count does not match width × height.");
        }

        public string LevelName { get; }
        public Rect Bounds { get; }
        public int Width { get; }
        public int Height { get; }
        public bool[] Solid { get; }

        public bool IsSolid(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height && Solid[y * Width + x];
    }

    /// <summary>The Operator's request: act on one target.</summary>
    public readonly struct OperatorCommand
    {
        public readonly string TargetId;
        public OperatorCommand(string targetId) => TargetId = targetId;
    }

    /// <summary>Level info published by the scene (id and camera bounds) so systems outside Gameplay can map the level.</summary>
    public interface ILevelInfo
    {
        string LevelId { get; }
        Rect Bounds { get; }
    }

    /// <summary>
    /// Transport between the infiltrator's game and the Operator's console. <see cref="LocalOperatorLink"/> (same PC,
    /// split-screen) and the Netcode LAN link implement it; the console and the command handling are shared.
    /// </summary>
    public interface IOperatorLink : IDisposable
    {
        bool IsConnected { get; }

        /// <summary>Operator side.</summary>
        event Action<OperatorSnapshot> SnapshotReceived;

        /// <summary>Operator side.</summary>
        event Action<OperatorLevelMap> LevelMapReceived;

        /// <summary>Infiltrator side.</summary>
        event Action<OperatorCommand> CommandReceived;

        void SendSnapshot(OperatorSnapshot snapshot);
        void SendLevelMap(OperatorLevelMap map);
        void SendCommand(OperatorCommand command);
    }

    /// <summary>Both roles in one process: every send is delivered immediately to the other side.</summary>
    public sealed class LocalOperatorLink : IOperatorLink
    {
        public bool IsConnected => true;
        public event Action<OperatorSnapshot> SnapshotReceived;
        public event Action<OperatorLevelMap> LevelMapReceived;
        public event Action<OperatorCommand> CommandReceived;

        public void SendSnapshot(OperatorSnapshot snapshot) => SnapshotReceived?.Invoke(snapshot);
        public void SendLevelMap(OperatorLevelMap map) => LevelMapReceived?.Invoke(map);
        public void SendCommand(OperatorCommand command) => CommandReceived?.Invoke(command);

        public void Dispose()
        {
            SnapshotReceived = null;
            LevelMapReceived = null;
            CommandReceived = null;
        }
    }

    /// <summary>
    /// Facade the UI uses for co-op (implemented by the Net assembly's CoopSession): start/stop a mode, read the latest
    /// snapshot and map, send Operator commands.
    /// </summary>
    public interface IOperatorConsole
    {
        CoopMode Mode { get; }
        bool IsLinked { get; }
        string Status { get; }
        OperatorSnapshot LatestSnapshot { get; }
        OperatorLevelMap LatestMap { get; }

        /// <summary>Raised on the main thread whenever the snapshot, the map or the status changes.</summary>
        event Action Changed;

        void StartLocal();
        bool Host();
        bool Join(string address);
        void Stop();
        void SendCommand(string targetId);
    }
}
