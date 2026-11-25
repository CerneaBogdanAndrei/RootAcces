using UnityEngine;

namespace RootAccess.Core
{
    /// <summary>Physics layer indices. Water reuses Unity's built-in layer 4.</summary>
    public static class Layers
    {
        public const int Default = 0;
        public const int IgnoreRaycast = 2;
        public const int Water = 4;
        public const int UI = 5;
        public const int Player = 8;
        public const int Enemy = 9;
        public const int Ground = 10;
        public const int Wall = 11;
        public const int Interactable = 12;
        public const int Trigger = 13;
        public const int Projectile = 14;
        public const int Hidden = 15;

        public const string PlayerName = "Player";
        public const string EnemyName = "Enemy";
        public const string GroundName = "Ground";
        public const string WallName = "Wall";
        public const string InteractableName = "Interactable";
        public const string TriggerName = "Trigger";
        public const string WaterName = "Water";
        public const string ProjectileName = "Projectile";
        public const string HiddenName = "Hidden";

        public static int ToMask(int layer) => 1 << layer;

        public static readonly int SolidMask = ToMask(Ground) | ToMask(Wall) | ToMask(Default);
        public static readonly int ObstacleMask = ToMask(Ground) | ToMask(Wall);
        public static readonly int PlayerMask = ToMask(Player);
        public static readonly int InteractableMask = ToMask(Interactable);
        public static readonly int DamageableMask = ToMask(Player) | ToMask(Hidden) | ToMask(Enemy);
    }

    public static class Tags
    {
        public const string Untagged = "Untagged";
        public const string Player = "Player";
        public const string MainCamera = "MainCamera";
        public const string Enemy = "Enemy";
        public const string Checkpoint = "Checkpoint";
        public const string Interactable = "Interactable";
        public const string Hazard = "Hazard";
        public const string SpawnPoint = "SpawnPoint";
        public const string WaterVolume = "WaterVolume";
        public const string Terminal = "Terminal";
        public const string Collectible = "Collectible";

        public static readonly string[] Custom =
        {
            Enemy, Checkpoint, Interactable, Hazard, SpawnPoint, WaterVolume, Terminal, Collectible
        };
    }

    public static class SortingLayers
    {
        public const string Background = "Background";
        public const string Parallax = "Parallax";
        public const string Environment = "Environment";
        public const string Default = "Default";
        public const string Interactables = "Interactables";
        public const string Characters = "Characters";
        public const string Foreground = "Foreground";
        public const string VFX = "VFX";
        public const string UI = "UI";

        /// <summary>Back-to-front draw order used by ProjectSetup.</summary>
        public static readonly string[] Ordered =
        {
            Background, Parallax, Environment, Default, Interactables, Characters, Foreground, VFX, UI
        };
    }

    /// <summary>Indices into the URP asset's renderer list (ProjectSetup creates both).</summary>
    public static class RendererIndices
    {
        public const int Renderer2D = 0;
        public const int Universal = 1;
    }

    public static class SceneNames
    {
        public const string Bootstrap = "Bootstrap";
        public const string MainMenu = "MainMenu";
        public const string Hub = "Hub";
        public const string Level01 = "Level01_Rooftops";
        public const string Level02 = "Level02_Office";
        public const string Level03 = "Level03_ServerFarm";
        public const string Level04 = "Level04_Sewers";
        public const string Level05 = "Level05_HQ";
        public const string RubricShowcase = "RubricShowcase";
        public const string Credits = "Credits";
        public const string Intro = "Intro";
        public const string Finale = "Finale";

        /// <summary>LAN Operator's screen: no level, only the console fed by the host.</summary>
        public const string OperatorConsole = "OperatorConsole";

        /// <summary>Timeline-only scenes: no player, no HUD; they load the next scene when the cutscene ends or is skipped.</summary>
        public static bool IsCutsceneScene(string sceneName) => sceneName == Intro || sceneName == Finale;

        public static bool IsGameplayScene(string sceneName) =>
            sceneName == Hub || sceneName == Level01 || sceneName == Level02 || sceneName == Level03 ||
            sceneName == Level04 || sceneName == Level05 || sceneName == RubricShowcase;
    }

    public static class MixerParameters
    {
        public const string Master = "MasterVolume";
        public const string Music = "MusicVolume";
        public const string Sfx = "SfxVolume";
        public const string UI = "UiVolume";

        /// <summary>Exposed volumes (driven by settings). Ambience stays unexposed so snapshots can duck it.</summary>
        public static readonly string[] Exposed = { Master, Music, Sfx, UI };
    }

    public static class MixerSnapshotNames
    {
        public const string Normal = "Normal";
        public const string Underwater = "Underwater";
        public const string Alarm = "Alarm";
    }

    public static class ShaderIds
    {
        public static readonly int DissolveAmount = Shader.PropertyToID("_DissolveAmount");
        public static readonly int EdgeColor = Shader.PropertyToID("_EdgeColor");
        public static readonly int GlobalWind = Shader.PropertyToID("_RA_Wind");
        public static readonly int GlobalPlayerPosition = Shader.PropertyToID("_RA_PlayerPosition");
        public static readonly int PulseSpeed = Shader.PropertyToID("_PulseSpeed");
        public static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    }

    public static class AnimatorParams
    {
        public static readonly int Speed = Animator.StringToHash("Speed");
        public static readonly int VerticalSpeed = Animator.StringToHash("VerticalSpeed");
        public static readonly int Grounded = Animator.StringToHash("Grounded");
        public static readonly int Crouching = Animator.StringToHash("Crouching");
        public static readonly int Dashing = Animator.StringToHash("Dashing");
        public static readonly int WallSliding = Animator.StringToHash("WallSliding");
        public static readonly int Swimming = Animator.StringToHash("Swimming");
        public static readonly int Hacking = Animator.StringToHash("Hacking");
        public static readonly int Hurt = Animator.StringToHash("Hurt");
        public static readonly int Dead = Animator.StringToHash("Dead");
        public static readonly int Alert = Animator.StringToHash("Alert");
        public static readonly int Attack = Animator.StringToHash("Attack");
        public static readonly int IsOpen = Animator.StringToHash("IsOpen");
        public static readonly int Active = Animator.StringToHash("Active");
        public static readonly int Show = Animator.StringToHash("Show");

        public const string SpeedName = "Speed";
        public const string VerticalSpeedName = "VerticalSpeed";
        public const string GroundedName = "Grounded";
        public const string CrouchingName = "Crouching";
        public const string DashingName = "Dashing";
        public const string WallSlidingName = "WallSliding";
        public const string SwimmingName = "Swimming";
        public const string HackingName = "Hacking";
        public const string HurtName = "Hurt";
        public const string DeadName = "Dead";
        public const string AlertName = "Alert";
        public const string AttackName = "Attack";
        public const string IsOpenName = "IsOpen";
        public const string ActiveName = "Active";
        public const string ShowName = "Show";
    }

    public static class AnimationEventNames
    {
        public const string Footstep = "OnFootstep";
        public const string HitFrame = "OnHitFrame";
    }

    public static class SaveSlots
    {
        public const string Main = "slot0";
    }

    public static class PlayerPrefsKeys
    {
        public const string Settings = "RootAccess.Settings";
    }

    public static class ResourcePaths
    {
        public const string SystemsRoot = "SystemsRoot";
    }
}
