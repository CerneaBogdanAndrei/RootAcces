using UnityEngine;

namespace RootAccess.Core
{
    public readonly struct SceneLoadStarted : IEvent
    {
        public readonly string SceneName;
        public SceneLoadStarted(string sceneName) => SceneName = sceneName;
    }

    public readonly struct SceneLoadProgress : IEvent
    {
        public readonly float Progress;
        public SceneLoadProgress(float progress) => Progress = progress;
    }

    public readonly struct SceneLoadCompleted : IEvent
    {
        public readonly string SceneName;
        public SceneLoadCompleted(string sceneName) => SceneName = sceneName;
    }

    public readonly struct GameStateChanged : IEvent
    {
        public readonly GameState Previous;
        public readonly GameState Current;

        public GameStateChanged(GameState previous, GameState current)
        {
            Previous = previous;
            Current = current;
        }
    }

    public readonly struct SettingsChanged : IEvent
    {
        public readonly GameSettings Settings;
        public SettingsChanged(GameSettings settings) => Settings = settings;
    }

    public readonly struct PlayerHealthChanged : IEvent
    {
        public readonly int Current;
        public readonly int Max;

        public PlayerHealthChanged(int current, int max)
        {
            Current = current;
            Max = max;
        }
    }

    public readonly struct PlayerDied : IEvent
    {
        public readonly Vector3 Position;
        public PlayerDied(Vector3 position) => Position = position;
    }

    public readonly struct PlayerRespawnRequested : IEvent { }

    public readonly struct PlayerRespawned : IEvent
    {
        public readonly Vector3 Position;
        public PlayerRespawned(Vector3 position) => Position = position;
    }

    public readonly struct PlayerHiddenChanged : IEvent
    {
        public readonly bool IsHidden;
        public PlayerHiddenChanged(bool isHidden) => IsHidden = isHidden;
    }

    /// <summary>Raised by AI or failed hacks; the TraceMeter turns it into alert level.</summary>
    public readonly struct TraceNoise : IEvent
    {
        public readonly float Amount;
        public readonly TraceSource Source;
        public readonly Vector2 Position;

        public TraceNoise(float amount, TraceSource source, Vector2 position)
        {
            Amount = amount;
            Source = source;
            Position = position;
        }
    }

    /// <summary>The antenna broadcast of Level 6 advanced, paused or ended (drives the HUD progress bar).</summary>
    public readonly struct UplinkProgressChanged : IEvent
    {
        public readonly float Progress;
        public readonly bool Active;
        public readonly bool PlayerPresent;

        public UplinkProgressChanged(float progress, bool active, bool playerPresent)
        {
            Progress = progress;
            Active = active;
            PlayerPresent = playerPresent;
        }
    }

    public readonly struct AlarmRaised : IEvent
    {
        public readonly Vector2 Position;
        public AlarmRaised(Vector2 position) => Position = position;
    }

    /// <summary>
    /// A sound the player caused (footstep, hard landing, EMP blast). Guards inside <see cref="Radius"/> turn to
    /// investigate <see cref="Position"/>; quieter sources (silent-steps skill) simply publish a smaller radius.
    /// </summary>
    public readonly struct PlayerNoise : IEvent
    {
        public readonly Vector2 Position;
        public readonly float Radius;

        public PlayerNoise(Vector2 position, float radius)
        {
            Position = position;
            Radius = radius;
        }
    }

    public readonly struct CheckpointReached : IEvent
    {
        public readonly string CheckpointId;
        public readonly Vector3 Position;

        public CheckpointReached(string checkpointId, Vector3 position)
        {
            CheckpointId = checkpointId;
            Position = position;
        }
    }

    public readonly struct HackRequested : IEvent
    {
        public readonly int RequestId;
        public readonly HackingMinigameType Minigame;
        public readonly int Difficulty;
        public readonly Vector3 TerminalPosition;

        public HackRequested(int requestId, HackingMinigameType minigame, int difficulty, Vector3 terminalPosition)
        {
            RequestId = requestId;
            Minigame = minigame;
            Difficulty = difficulty;
            TerminalPosition = terminalPosition;
        }
    }

    public readonly struct HackResolved : IEvent
    {
        public readonly int RequestId;
        public readonly bool Success;
        public readonly bool Aborted;

        public HackResolved(int requestId, bool success, bool aborted)
        {
            RequestId = requestId;
            Success = success;
            Aborted = aborted;
        }
    }

    public readonly struct HackSessionStarted : IEvent
    {
        public readonly Vector3 TerminalPosition;
        public HackSessionStarted(Vector3 terminalPosition) => TerminalPosition = terminalPosition;
    }

    public readonly struct HackSessionEnded : IEvent
    {
        public readonly bool Success;
        public HackSessionEnded(bool success) => Success = success;
    }

    public readonly struct InteractionPromptChanged : IEvent
    {
        public readonly string Prompt;
        public readonly bool Visible;

        public InteractionPromptChanged(string prompt, bool visible)
        {
            Prompt = prompt;
            Visible = visible;
        }
    }

    public readonly struct SfxRequested : IEvent
    {
        public readonly SfxId Id;
        public readonly Vector3 Position;
        public readonly bool Spatial;
        public readonly float Volume;

        public SfxRequested(SfxId id, Vector3 position, bool spatial = true, float volume = 1f)
        {
            Id = id;
            Position = position;
            Spatial = spatial;
            Volume = volume;
        }

        public static SfxRequested Ui(SfxId id) => new(id, Vector3.zero, false);
    }

    public readonly struct FootstepRequested : IEvent
    {
        public readonly SurfaceType Surface;
        public readonly Vector3 Position;
        public readonly float Volume;

        public FootstepRequested(SurfaceType surface, Vector3 position, float volume = 1f)
        {
            Surface = surface;
            Position = position;
            Volume = volume;
        }
    }

    public readonly struct VfxRequested : IEvent
    {
        public readonly VfxId Id;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public VfxRequested(VfxId id, Vector3 position, Quaternion rotation)
        {
            Id = id;
            Position = position;
            Rotation = rotation;
        }

        public VfxRequested(VfxId id, Vector3 position) : this(id, position, Quaternion.identity) { }
    }

    public readonly struct ScreenShakeRequested : IEvent
    {
        public readonly float Force;
        public ScreenShakeRequested(float force) => Force = force;
    }

    public readonly struct TeleportStarted : IEvent
    {
        public readonly Vector3 From;
        public readonly Vector3 To;

        public TeleportStarted(Vector3 from, Vector3 to)
        {
            From = from;
            To = to;
        }
    }

    public readonly struct TeleportCompleted : IEvent
    {
        public readonly Vector3 Position;
        public TeleportCompleted(Vector3 position) => Position = position;
    }

    public readonly struct LevelCompleted : IEvent
    {
        public readonly string LevelId;
        public readonly string NextScene;

        public LevelCompleted(string levelId, string nextScene)
        {
            LevelId = levelId;
            NextScene = nextScene;
        }
    }

    /// <summary>Local co-op split-screen on/off: the gameplay camera and the HUD shrink to the left half.</summary>
    public readonly struct CoopViewChanged : IEvent
    {
        public readonly bool Split;
        public CoopViewChanged(bool split) => Split = split;
    }

    public readonly struct CameraZoneEntered : IEvent
    {
        public readonly Collider2D Bounds;
        public readonly float OrthographicSize;

        public CameraZoneEntered(Collider2D bounds, float orthographicSize)
        {
            Bounds = bounds;
            OrthographicSize = orthographicSize;
        }
    }
}
