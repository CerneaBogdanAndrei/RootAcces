using System.Collections.Generic;
using RootAccess.AI;
using RootAccess.Audio;
using RootAccess.Core;
using RootAccess.Input;
using RootAccess.Player;
using RootAccess.Systems;
using RootAccess.UI;
using RootAccess.VFX;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

namespace RootAccess.EditorTools
{
    /// <summary>Everything the build pipeline creates, handed from one factory to the next.</summary>
    public sealed class BuildContext
    {
        public Material SpriteLit, SpriteUnlit, Metal, Concrete, Glass, NeonCyan, NeonMagenta, Hologram, Dissolve, GrassWind;
        public Material Water, ParticlesAdditive, ParticlesAlpha, VisionCone, Skybox, SkyBackdrop;
        public PhysicsMaterial2D NoFriction;

        public InputReader Input;
        public PlayerMovementConfig PlayerConfig;
        public GuardConfig GuardBase, GuardHeavy, GuardSniper;
        public ItemDefinition UsbKey, DataShard, EmpGrenade, Exploit;
        public SkillDefinition[] Skills;
        public QuestDefinition GhostQuest;
        public SkillDefinition SilentSteps, Overclock, ServoLegs;
        public QuestDefinition MainQuest, SideQuest;
        public LevelDefinition HubLevel, Level01, RubricLevel;
        public LevelDefinition[] Levels;
        public DialogueDefinition Briefing, Level01Intro;
        public GameDatabase Database;
        public SfxLibrary Sfx;
        public VfxLibrary Vfx;
        public RubricCatalog Rubric;
        public MixerBuilder.Result Mixer;

        public RuntimeAnimatorController PlayerAnimator, GuardAnimator, DroneAnimator, CameraAnimator, DoorAnimator;
        public RuntimeAnimatorController PulseAnimator, ElevatorAnimator;

        public TileBase TileMetal, TileMetalTop, TileConcrete, TileBrick, TileGlass;
        public VolumeProfile MenuProfile, HubProfile, Level01Profile, ShowcaseProfile;

        public readonly Dictionary<VfxId, GameObject> VfxPrefabs = new();
        public readonly Dictionary<string, GameObject> Prefabs = new();

        public GameObject Prefab(string name)
        {
            if (Prefabs.TryGetValue(name, out GameObject prefab)) return prefab;
            throw new KeyNotFoundException($"Prefab '{name}' was not built yet.");
        }
    }
}
