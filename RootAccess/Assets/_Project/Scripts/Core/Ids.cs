namespace RootAccess.Core
{
    public enum GameState
    {
        Boot,
        MainMenu,
        Loading,
        Playing,
        Paused,
        Hacking,
        Dialogue,
        Cutscene,
        GameOver,
        LevelComplete,
        Terminal
    }

    public enum AudioChannel
    {
        Master,
        Music,
        Sfx,
        UI,
        Ambience
    }

    public enum SfxId
    {
        None,
        Jump,
        DoubleJump,
        Land,
        Footstep,
        Dash,
        Hurt,
        Death,
        UiClick,
        UiHover,
        UiBack,
        HackBeep,
        HackSuccess,
        HackFail,
        DoorOpen,
        DoorClose,
        Pickup,
        Checkpoint,
        TeleportOut,
        TeleportIn,
        Alarm,
        Spotted,
        Splash,
        EmpBlast,
        TurretShot
    }

    public enum VfxId
    {
        None,
        Sparks,
        EmpExplosion,
        Rain,
        Steam,
        DataStream,
        LandingDust,
        Splash,
        Death,
        TeleportOut,
        TeleportIn,
        TeleportTrail,
        PickupGlow
    }

    public enum SurfaceType
    {
        Concrete,
        Metal,
        Glass,
        Grass,
        Water
    }

    public enum HackingMinigameType
    {
        PatternMemory,
        WireConnect,
        PortScan
    }

    public enum HackAction
    {
        Toggle,
        Open,
        Close,
        Disable,
        Activate
    }

    public enum TraceSource
    {
        Camera,
        Guard,
        Drone,
        Turret,
        FailedHack,
        Alarm,
        Noise
    }

    public enum DamageType
    {
        Generic,
        Contact,
        Projectile,
        Laser,
        Electric,
        Fall,
        Drowning
    }
}
