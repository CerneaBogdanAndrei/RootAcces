using UnityEngine;

namespace RootAccess.Core
{
    /// <summary>Something the player can use with the Interact action (terminals, doors, nodes, NPCs).</summary>
    public interface IInteractable
    {
        string Prompt { get; }
        Vector3 InteractionPosition { get; }
        bool CanInteract(IInteractor interactor);
        void Interact(IInteractor interactor);
    }

    public interface IInteractor
    {
        Transform Transform { get; }
        GameObject GameObject { get; }
    }

    public readonly struct DamageInfo
    {
        public readonly int Amount;
        public readonly Vector2 SourcePosition;
        public readonly float Knockback;
        public readonly DamageType Type;

        public DamageInfo(int amount, Vector2 sourcePosition, float knockback = 6f, DamageType type = DamageType.Generic)
        {
            Amount = amount;
            SourcePosition = sourcePosition;
            Knockback = knockback;
            Type = type;
        }
    }

    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(in DamageInfo damage);
    }

    /// <summary>
    /// Something that can fall into a pit and be put back on solid ground (the player). Pits cost health instead of
    /// killing outright, so a missed jump between rooftops is a setback, not a restart.
    /// </summary>
    public interface IFallRecoverable
    {
        /// <summary>Takes <paramref name="damage"/>; if still alive, reappears at <paramref name="safePosition"/>.</summary>
        void RecoverFromFall(int damage, Vector3 safePosition);
    }

    /// <summary>
    /// A moving surface that carries whoever stands on it (the gondolas of Level 6). The player adds
    /// <see cref="CarryVelocity"/> to its own run speed while grounded on it, so it rides along instead of sliding off.
    /// </summary>
    public interface ISurfaceCarrier
    {
        Vector2 CarryVelocity { get; }
    }

    /// <summary>Anything a terminal (or EMP) can affect: doors, cameras, lights, elevators, network nodes.</summary>
    public interface IHackable
    {
        string HackLabel { get; }
        bool IsHacked { get; }
        void ApplyHack(HackAction action);
    }

    /// <summary>Interactables the "remote hack" skill can reach from further away (terminals).</summary>
    public interface IRemoteInteractable { }

    /// <summary>Electronics an EMP blast can knock out for a while (cameras, drones, turrets, guard gear).</summary>
    public interface IEmpTarget
    {
        bool IsEmpDisabled { get; }
        void ApplyEmp(float seconds);
    }

    public interface IHideable
    {
        bool IsHidden { get; }
        void SetInHideZone(bool inside);
    }

    public readonly struct WaterVolumeInfo
    {
        public readonly float SurfaceY;
        public readonly float Density;
        public readonly Collider2D Volume;

        public WaterVolumeInfo(float surfaceY, float density, Collider2D volume)
        {
            SurfaceY = surfaceY;
            Density = density;
            Volume = volume;
        }
    }

    public interface IWaterAffectable
    {
        void EnterWater(in WaterVolumeInfo water);
        void ExitWater(Collider2D volume);

        /// <summary>The water level of a volume the swimmer is in changed (flooding).</summary>
        void UpdateWaterSurface(float surfaceY);
    }

    /// <summary>Implemented by the player so network nodes / teleport pads can move it with a VFX sequence.</summary>
    public interface ITeleportable
    {
        Transform Transform { get; }
        bool CanTeleport { get; }
        void BeginTeleport();
        void CompleteTeleport(Vector3 destination);
    }

    /// <summary>What enemies need to know about the player, registered in the ServiceLocator.</summary>
    public interface IPlayerTarget
    {
        Transform Transform { get; }
        Vector2 Position { get; }
        Vector2 Velocity { get; }
        bool IsHidden { get; }
        bool IsAlive { get; }
    }

    /// <summary>Game session facade used by menus (implemented in the Systems assembly).</summary>
    public interface ISessionService
    {
        bool HasSave { get; }

        /// <summary>Fresh session state (starting quest, unlocked levels) without loading a scene.</summary>
        void ResetSession();

        void NewGame();
        bool ContinueGame();
        void SaveGame();
    }
}
