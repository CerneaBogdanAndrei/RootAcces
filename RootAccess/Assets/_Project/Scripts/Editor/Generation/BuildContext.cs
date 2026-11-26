using System.Collections.Generic;
using RootAccess.AI;
using RootAccess.Audio;
using RootAccess.Core;
using RootAccess.Input;
using RootAccess.Player;
using RootAccess.Systems;
using RootAccess.UI;
using RootAccess.VFX;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

namespace RootAccess.EditorTools
{
    /// <summary>Everything the build pipeline creates, handed from one factory to the next.</summary>
    public sealed class BuildContext
    {
        public Material SpriteLit, SpriteUnlit, Metal, Concrete, Glass, NeonCyan, NeonMagenta, Hologram, Dissolve, GrassWind;
        public Material Water, WaterDistortion, ParticlesAdditive, ParticlesAlpha, VisionCone, Skybox, SkyBackdrop;
        public PhysicsMaterial2D NoFriction;

        public InputReader Input;
        public PlayerMovementConfig PlayerConfig;
        public GuardConfig GuardBase, GuardHeavy, GuardSniper;
        public ItemDefinition UsbKey, DataShard, EmpGrenade, Exploit;
        public SkillDefinition[] Skills;
        public QuestDefinition GhostQuest;
        public SkillDefinition SilentSteps, Overclock, ServoLegs;
        public QuestDefinition MainQuest, SideQuest, OfficeQuest, FarmQuest, SewersQuest, HqQuest;
        public LevelDefinition HubLevel, Level01, Level02, Level03, Level04, Level05, RubricLevel;
        public LevelDefinition[] Levels;
        public DialogueDefinition Briefing, Level01Intro, Level02Intro, Level03Intro, Level04Intro, Level05Intro;
        public GameDatabase Database;
        public SfxLibrary Sfx;
        public VfxLibrary Vfx;
        public RubricCatalog Rubric;
        public MixerBuilder.Result Mixer;

        public RuntimeAnimatorController PlayerAnimator, GuardAnimator, HeavyAnimator, SniperAnimator, FixerAnimator, DroneAnimator, CameraAnimator, DoorAnimator;
        public RuntimeAnimatorController PulseAnimator, ElevatorAnimator;

        /// <summary>See-through window tile (hideout window, materials room of the showcase).</summary>
        public TileBase TileGlass;

        /// <summary>Biome tiles by name (see <see cref="TileArt"/> / <see cref="LevelTheme"/>).</summary>
        public readonly Dictionary<string, TileBase> ThemeTiles = new();

        /// <summary>A biome tile by name; throws when the name was never painted (a typo in a <see cref="LevelTheme"/> picker).</summary>
        public TileBase ThemeTile(string name)
        {
            if (ThemeTiles.TryGetValue(name, out TileBase tile)) return tile;
            throw new KeyNotFoundException($"Theme tile '{name}' was not built (TileArt/AssetFactory.BuildTiles).");
        }

        public VolumeProfile MenuProfile, HubProfile, Level01Profile, Level02Profile, Level03Profile, Level04Profile, Level05Profile, ShowcaseProfile;

        /// <summary>Levels 1–5 in order: what the hideout's fast-travel console lists.</summary>
        public LevelDefinition[] StoryLevels => new[] { Level01, Level02, Level03, Level04, Level05 };

        public readonly Dictionary<VfxId, GameObject> VfxPrefabs = new();
        public readonly Dictionary<string, GameObject> Prefabs = new();

        /// <summary>
        /// Scene generation opens a new scene per level, and Unity may unload ScriptableObjects that nothing loaded
        /// references in between: the C# wrapper kept here then compares equal to null and a SerializedProperty assigned
        /// from it silently stores "None". Reload such an asset from disk (by its instance id) before wiring it into a scene.
        /// </summary>
        public static T Fresh<T>(T asset) where T : Object
        {
            if (ReferenceEquals(asset, null) || asset != null) return asset;
            string path = AssetDatabase.GetAssetPath(asset.GetInstanceID());
            T reloaded = string.IsNullOrEmpty(path) ? EditorUtility.InstanceIDToObject(asset.GetInstanceID()) as T : AssetDatabase.LoadAssetAtPath<T>(path);
            if (reloaded == null) throw new System.InvalidOperationException($"Asset of type {typeof(T).Name} was unloaded and could not be reloaded.");
            EditorAssetUtil.Log($"Reloaded unloaded asset {path} ({typeof(T).Name}).");
            return reloaded;
        }

        public static T[] Fresh<T>(T[] assets) where T : Object
        {
            var result = new T[assets.Length];
            for (int i = 0; i < assets.Length; i++) result[i] = Fresh(assets[i]);
            return result;
        }

        public GameObject Prefab(string name)
        {
            if (Prefabs.TryGetValue(name, out GameObject prefab)) return prefab;
            throw new KeyNotFoundException($"Prefab '{name}' was not built yet.");
        }
    }
}
