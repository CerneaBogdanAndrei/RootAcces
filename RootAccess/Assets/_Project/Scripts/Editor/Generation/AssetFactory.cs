using System.Linq;
using RootAccess.AI;
using RootAccess.Audio;
using RootAccess.Core;
using RootAccess.Input;
using RootAccess.Player;
using RootAccess.Systems;
using RootAccess.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace RootAccess.EditorTools
{
    /// <summary>Materials, physics materials, ScriptableObject data, tiles and post-processing profiles.</summary>
    public static class AssetFactory
    {
        private const string SpriteLitShader = "Universal Render Pipeline/2D/Sprite-Lit-Default";
        private const string SpriteUnlitShader = "Universal Render Pipeline/2D/Sprite-Unlit-Default";
        private const string WaterShader = "RootAccess/Water Surface";
        private const string ParticlesShader = "RootAccess/Particles Additive";
        private const string SkyboxShader = "Skybox/6 Sided";
        private const string BackdropShader = "RootAccess/Backdrop";

        public static void BuildAll(BuildContext ctx)
        {
            ShaderGraphFactory.BuildAll();
            BuildMaterials(ctx);
            BuildData(ctx);
            BuildTiles(ctx);
            BuildVolumeProfiles(ctx);
            ctx.Mixer = MixerBuilder.Build(EditorPaths.Mixers + "/RootAccessMixer.mixer");
            BuildSfxLibrary(ctx);
            BuildRubric(ctx);
            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------------- materials

        private static void BuildMaterials(BuildContext ctx)
        {
            ctx.SpriteLit = Mat("M_SpriteLit", SpriteLitShader);
            ctx.SpriteUnlit = Mat("M_SpriteUnlit", SpriteUnlitShader);
            ctx.Metal = Mat("M_Metal", SpriteLitShader, m => m.SetColor("_Color", new Color(0.85f, 0.9f, 1f)));
            ctx.Concrete = Mat("M_Concrete", SpriteLitShader, m => m.SetColor("_Color", new Color(0.95f, 0.93f, 0.9f)));
            ctx.Glass = Mat("M_Glass", SpriteLitShader, m => m.SetColor("_Color", new Color(0.7f, 0.9f, 1f, 0.6f)));
            ctx.NeonCyan = Mat("M_NeonCyan", ShaderGraphFactory.ShaderName(ShaderGraphFactory.NeonPulse), m => { m.SetColor(ShaderIds.EmissionColor, new Color(0.3f, 2.6f, 3.2f)); m.SetFloat(ShaderIds.PulseSpeed, 2.2f); });
            ctx.NeonMagenta = Mat("M_NeonMagenta", ShaderGraphFactory.ShaderName(ShaderGraphFactory.NeonPulse), m => { m.SetColor(ShaderIds.EmissionColor, new Color(3.2f, 0.4f, 2.4f)); m.SetFloat(ShaderIds.PulseSpeed, 1.6f); });
            ctx.Hologram = Mat("M_Hologram", ShaderGraphFactory.ShaderName(ShaderGraphFactory.Hologram));
            ctx.Dissolve = Mat("M_Dissolve", ShaderGraphFactory.ShaderName(ShaderGraphFactory.Dissolve));
            ctx.GrassWind = Mat("M_GrassWind", ShaderGraphFactory.ShaderName(ShaderGraphFactory.GrassWind));
            ctx.Water = Mat("M_Water", WaterShader);
            ctx.WaterDistortion = Mat("M_WaterDistortion", ShaderGraphFactory.ShaderName(ShaderGraphFactory.WaterDistortion), m =>
            {
                m.SetColor("_Tint", new Color(0.55f, 0.85f, 1f, 1f));
                m.SetFloat("_Strength", 0.012f);
                m.SetFloat("_Speed", 0.6f);
                m.SetFloat("_NoiseScale", 2.5f);
                m.SetFloat("_Glow", 0.35f);
            });
            CaptureBehindWater();
            Texture2D soft = GeneratedArt.Sprite("soft_circle").texture;
            ctx.ParticlesAdditive = Mat("M_ParticlesAdditive", ParticlesShader, m => m.SetTexture("_MainTex", soft));
            ctx.ParticlesAlpha = Mat("M_ParticlesAlpha", SpriteUnlitShader, m => m.SetTexture("_MainTex", GeneratedArt.Sprite("smoke").texture));
            ctx.VisionCone = Mat("M_VisionCone", SpriteUnlitShader, m => m.SetTexture("_MainTex", GeneratedArt.Sprite("white").texture));
            ctx.Skybox = Mat("M_SkyboxNightCity", SkyboxShader, m =>
            {
                m.SetTexture("_FrontTex", GeneratedArt.Texture(GeneratedArt.SkyboxFacePath("front")));
                m.SetTexture("_BackTex", GeneratedArt.Texture(GeneratedArt.SkyboxFacePath("back")));
                m.SetTexture("_LeftTex", GeneratedArt.Texture(GeneratedArt.SkyboxFacePath("left")));
                m.SetTexture("_RightTex", GeneratedArt.Texture(GeneratedArt.SkyboxFacePath("right")));
                m.SetTexture("_UpTex", GeneratedArt.Texture(GeneratedArt.SkyboxFacePath("up")));
                m.SetTexture("_DownTex", GeneratedArt.Texture(GeneratedArt.SkyboxFacePath("down")));
                m.SetFloat("_Exposure", 1.1f);
            });

            ctx.SkyBackdrop = Mat("M_SkyBackdrop", BackdropShader);

            string physicsPath = EditorPaths.Materials + "/PM_NoFriction.physicsMaterial2D";
            ctx.NoFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(physicsPath);
            if (ctx.NoFriction == null)
            {
                ctx.NoFriction = new PhysicsMaterial2D("PM_NoFriction") { friction = 0f, bounciness = 0f };
                AssetDatabase.CreateAsset(ctx.NoFriction, physicsPath);
            }
        }

        /// <summary>
        /// The water distortion graph samples the 2D renderer's camera sorting layer texture: capture everything up to the
        /// Characters layer (background, level, crates, the swimming player); water itself draws later, in Foreground.
        /// </summary>
        private static void CaptureBehindWater()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>(EditorPaths.UrpRenderer2D);
            if (renderer == null) throw new System.InvalidOperationException($"Renderer2DData missing at {EditorPaths.UrpRenderer2D} (run ProjectSetup).");
            EditorAssetUtil.SetFields(renderer, so =>
            {
                so.Require("m_UseCameraSortingLayersTexture").boolValue = true;
                so.Require("m_CameraSortingLayersTextureBound").intValue = SortingLayer.NameToID(SortingLayers.Characters);
            });
            EditorUtility.SetDirty(renderer);
        }

        private static Material Mat(string name, string shaderName, System.Action<Material> configure = null)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new System.InvalidOperationException($"Shader '{shaderName}' not found.");
            string path = $"{EditorPaths.Materials}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            configure?.Invoke(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        // ---------------------------------------------------------------- data

        private static void BuildData(BuildContext ctx)
        {
            ctx.Input = EditorAssetUtil.LoadOrCreateAsset<InputReader>(EditorPaths.SettingsInput + "/InputReader.asset");
            ctx.PlayerConfig = EditorAssetUtil.LoadOrCreateAsset<PlayerMovementConfig>(EditorPaths.ScriptableObjects + "/PlayerMovementConfig.asset");
            ctx.GuardBase = Guard("GuardConfig_Base", 1.8f, 5f, 7f, 70f, 1.4f, 1, 18f);
            ctx.GuardHeavy = Guard("GuardConfig_Heavy", 1.4f, 3.8f, 6f, 80f, 1.1f, 2, 22f);
            ctx.GuardSniper = Guard("GuardConfig_Sniper", 1.2f, 3.5f, 12f, 28f, 2.2f, 1, 30f);
            ctx.GuardBase.ConfigureSenses(1f, 22f);
            ctx.GuardHeavy.ConfigureSenses(1.2f, 26f, false, 1.25f, 1.3f);
            ctx.GuardSniper.ConfigureSenses(0.6f, 30f, true, 10f, 1.8f);

            ctx.UsbKey = Item(SeedContent.Ids.UsbKey, SeedContent.Items.UsbKey, SeedContent.Items.UsbKeyDesc, ItemCategory.UsbKey, 3, 40, "pickup_usb");
            ctx.DataShard = Item(SeedContent.Ids.DataShard, SeedContent.Items.DataShard, SeedContent.Items.DataShardDesc, ItemCategory.DataShard, 10, 75, "pickup_shard");
            ctx.EmpGrenade = Item(SeedContent.Ids.EmpGrenade, SeedContent.Items.EmpGrenade, SeedContent.Items.EmpGrenadeDesc, ItemCategory.Gadget, 3, 30, "pickup_emp");
            ctx.Exploit = Item(SeedContent.Ids.Exploit, SeedContent.Items.Exploit, SeedContent.Items.ExploitDesc, ItemCategory.Exploit, 1, 200, "pickup_credit");
            var itemDb = EditorAssetUtil.LoadOrCreateAsset<ItemDatabase>(EditorPaths.SoItems + "/ItemDatabase.asset");
            itemDb.SetItems(new[] { ctx.UsbKey, ctx.DataShard, ctx.EmpGrenade, ctx.Exploit });
            EditorUtility.SetDirty(itemDb);

            ctx.SilentSteps = Skill(SeedContent.Ids.SkillSilentSteps, SeedContent.Skills.SilentSteps, SeedContent.Skills.SilentStepsDesc, SkillBranch.Stealth, 100, SkillEffect.SilentSteps);
            SkillDefinition camouflage = Skill(SeedContent.Ids.SkillCamouflage, SeedContent.Skills.Camouflage, SeedContent.Skills.CamouflageDesc, SkillBranch.Stealth, 180, SkillEffect.LongerShadows, ctx.SilentSteps);
            ctx.Overclock = Skill(SeedContent.Ids.SkillOverclock, SeedContent.Skills.Overclock, SeedContent.Skills.OverclockDesc, SkillBranch.Hacking, 120, SkillEffect.FasterHacking);
            SkillDefinition secondChance = Skill(SeedContent.Ids.SkillSecondChance, SeedContent.Skills.SecondChance, SeedContent.Skills.SecondChanceDesc, SkillBranch.Hacking, 160, SkillEffect.ExtraHackAttempt, ctx.Overclock);
            SkillDefinition remoteHack = Skill(SeedContent.Ids.SkillRemoteHack, SeedContent.Skills.RemoteHack, SeedContent.Skills.RemoteHackDesc, SkillBranch.Hacking, 240, SkillEffect.RemoteHack, secondChance);
            ctx.ServoLegs = Skill(SeedContent.Ids.SkillServoLegs, SeedContent.Skills.ServoLegs, SeedContent.Skills.ServoLegsDesc, SkillBranch.Hardware, 120, SkillEffect.DashCooldown);
            SkillDefinition armor = Skill(SeedContent.Ids.SkillArmor, SeedContent.Skills.Armor, SeedContent.Skills.ArmorDesc, SkillBranch.Hardware, 180, SkillEffect.ArmorPlating, ctx.ServoLegs);
            SkillDefinition capacitors = Skill(SeedContent.Ids.SkillEmpCapacitors, SeedContent.Skills.EmpCapacitors, SeedContent.Skills.EmpCapacitorsDesc, SkillBranch.Hardware, 220, SkillEffect.EmpCapacity, ctx.ServoLegs);
            ctx.Skills = new[] { ctx.SilentSteps, camouflage, ctx.Overclock, secondChance, remoteHack, ctx.ServoLegs, armor, capacitors };

            ctx.MainQuest = Quest(SeedContent.Ids.MainQuest, SeedContent.Quests.MainTitle, SeedContent.Quests.MainDesc, true, 300, 150,
                new ObjectiveDefinition("reach_l1", SeedContent.Quests.ReachLevel, ObjectiveType.Reach, SeedContent.Ids.Level01),
                new ObjectiveDefinition("hack_door", SeedContent.Quests.HackDoor, ObjectiveType.Hack, SeedContent.Ids.TerminalLevel01Door),
                new ObjectiveDefinition("collect_shard", SeedContent.Quests.CollectShard, ObjectiveType.Collect, SeedContent.Ids.DataShard),
                new ObjectiveDefinition("reach_exit", SeedContent.Quests.ReachExit, ObjectiveType.Reach, SeedContent.Ids.ExitLevel01),
                new ObjectiveDefinition("stealth", SeedContent.Quests.Stealth, ObjectiveType.AvoidDetection, string.Empty, 1, true));
            ctx.SideQuest = Quest(SeedContent.Ids.SideQuest, SeedContent.Quests.SideTitle, SeedContent.Quests.SideDesc, false, 120, 60,
                new ObjectiveDefinition("collect_usb", SeedContent.Quests.CollectUsb, ObjectiveType.Collect, SeedContent.Ids.UsbKey),
                new ObjectiveDefinition("hack_camera", SeedContent.Quests.HackCamera, ObjectiveType.Hack, SeedContent.Ids.TerminalLevel01Camera));
            ctx.OfficeQuest = Quest(SeedContent.Ids.MainQuestLevel02, SeedContent.Quests.OfficeTitle, SeedContent.Quests.OfficeDesc, true, 400, 220,
                new ObjectiveDefinition("reach_l2", SeedContent.Quests.OfficeReach, ObjectiveType.Reach, SeedContent.Ids.Level02),
                new ObjectiveDefinition("hack_elevator", SeedContent.Quests.OfficeElevator, ObjectiveType.Hack, SeedContent.Ids.TerminalLevel02Elevator),
                new ObjectiveDefinition("hack_vault", SeedContent.Quests.OfficeVault, ObjectiveType.Hack, SeedContent.Ids.TerminalLevel02Vault),
                new ObjectiveDefinition("reach_exit_l2", SeedContent.Quests.OfficeExit, ObjectiveType.Reach, SeedContent.Ids.ExitLevel02),
                new ObjectiveDefinition("stealth_l2", SeedContent.Quests.Stealth, ObjectiveType.AvoidDetection, string.Empty, 1, true));
            ctx.FarmQuest = Quest(SeedContent.Ids.MainQuestLevel03, SeedContent.Quests.FarmTitle, SeedContent.Quests.FarmDesc, true, 450, 260,
                new ObjectiveDefinition("reach_l3", SeedContent.Quests.FarmReach, ObjectiveType.Reach, SeedContent.Ids.Level03),
                new ObjectiveDefinition("hack_core", SeedContent.Quests.FarmCore, ObjectiveType.Hack, SeedContent.Ids.TerminalLevel03Core),
                new ObjectiveDefinition("reach_exit_l3", SeedContent.Quests.FarmExit, ObjectiveType.Reach, SeedContent.Ids.ExitLevel03),
                new ObjectiveDefinition("stealth_l3", SeedContent.Quests.Stealth, ObjectiveType.AvoidDetection, string.Empty, 1, true));
            ctx.SewersQuest = Quest(SeedContent.Ids.MainQuestLevel04, SeedContent.Quests.SewersTitle, SeedContent.Quests.SewersDesc, true, 450, 260,
                new ObjectiveDefinition("reach_l4", SeedContent.Quests.SewersReach, ObjectiveType.Reach, SeedContent.Ids.Level04),
                new ObjectiveDefinition("hack_sluice", SeedContent.Quests.SewersSluice, ObjectiveType.Hack, SeedContent.Ids.TerminalLevel04Sluice),
                new ObjectiveDefinition("reach_exit_l4", SeedContent.Quests.SewersExit, ObjectiveType.Reach, SeedContent.Ids.ExitLevel04),
                new ObjectiveDefinition("stealth_l4", SeedContent.Quests.Stealth, ObjectiveType.AvoidDetection, string.Empty, 1, true));
            ctx.HqQuest = Quest(SeedContent.Ids.MainQuestLevel05, SeedContent.Quests.HqTitle, SeedContent.Quests.HqDesc, true, 1000, 400,
                new ObjectiveDefinition("reach_l5", SeedContent.Quests.HqReach, ObjectiveType.Reach, SeedContent.Ids.Level05),
                new ObjectiveDefinition("hack_exec_elevator", SeedContent.Quests.HqElevator, ObjectiveType.Hack, SeedContent.Ids.TerminalLevel05Elevator),
                new ObjectiveDefinition("hack_mainframe", SeedContent.Quests.HqMainframe, ObjectiveType.Hack, SeedContent.Ids.TerminalLevel05Mainframe),
                new ObjectiveDefinition("reach_exit_l5", SeedContent.Quests.HqExit, ObjectiveType.Reach, SeedContent.Ids.ExitLevel05),
                new ObjectiveDefinition("stealth_l5", SeedContent.Quests.Stealth, ObjectiveType.AvoidDetection, string.Empty, 1, true));
            ctx.MainQuest.SetFollowUp(ctx.OfficeQuest);
            ctx.OfficeQuest.SetFollowUp(ctx.FarmQuest);
            ctx.FarmQuest.SetFollowUp(ctx.SewersQuest);
            ctx.SewersQuest.SetFollowUp(ctx.HqQuest);
            ctx.HqQuest.SetFollowUp(null);
            foreach (QuestDefinition quest in new[] { ctx.MainQuest, ctx.OfficeQuest, ctx.FarmQuest, ctx.SewersQuest, ctx.HqQuest })
                EditorUtility.SetDirty(quest);
            ctx.GhostQuest = Quest(SeedContent.Ids.GhostQuest, SeedContent.Quests.GhostTitle, SeedContent.Quests.GhostDesc, false, 200, 120,
                new ObjectiveDefinition("ghost_exit", SeedContent.Quests.GhostExit, ObjectiveType.Reach, SeedContent.Ids.ExitLevel01),
                new ObjectiveDefinition("ghost_stealth", SeedContent.Quests.Stealth, ObjectiveType.AvoidDetection, string.Empty));

            ctx.HubLevel = Level(SeedContent.Ids.Hub, SceneNames.Hub, Strings.Levels.Hub, true, 0);
            ctx.Level01 = Level(SeedContent.Ids.Level01, SceneNames.Level01, Strings.Levels.Level01, true, 1, SeedContent.LevelInfo.Level01);
            ctx.Level02 = Level(SeedContent.Ids.Level02, SceneNames.Level02, Strings.Levels.Level02, false, 2, SeedContent.LevelInfo.Level02);
            ctx.Level03 = Level(SeedContent.Ids.Level03, SceneNames.Level03, Strings.Levels.Level03, false, 3, SeedContent.LevelInfo.Level03);
            ctx.Level04 = Level(SeedContent.Ids.Level04, SceneNames.Level04, Strings.Levels.Level04, false, 4, SeedContent.LevelInfo.Level04);
            ctx.Level05 = Level(SeedContent.Ids.Level05, SceneNames.Level05, Strings.Levels.Level05, false, 5, SeedContent.LevelInfo.Level05);
            ctx.RubricLevel = Level("rubric", SceneNames.RubricShowcase, Strings.Levels.RubricShowcase, true, 99);
            ctx.Levels = new[] { ctx.HubLevel, ctx.Level01, ctx.Level02, ctx.Level03, ctx.Level04, ctx.Level05, ctx.RubricLevel };

            ctx.Briefing = Dialogue("dlg_briefing",
                new DialogueLine(SeedContent.Dialogue.Operator, SeedContent.Dialogue.Briefing1),
                new DialogueLine(SeedContent.Dialogue.Operator, SeedContent.Dialogue.Briefing2),
                new DialogueLine(SeedContent.Dialogue.Hacker, SeedContent.Dialogue.Briefing3));
            ctx.Level01Intro = Dialogue("dlg_level01_intro",
                new DialogueLine(SeedContent.Dialogue.Operator, SeedContent.Dialogue.Intro1),
                new DialogueLine(SeedContent.Dialogue.Operator, SeedContent.Dialogue.Intro2),
                new DialogueLine(SeedContent.Dialogue.Operator, SeedContent.Dialogue.Intro3));
            ctx.Level02Intro = Dialogue("dlg_level02_intro",
                new DialogueLine(SeedContent.Dialogue.Operator, SeedContent.Dialogue.Office1),
                new DialogueLine(SeedContent.Dialogue.Operator, SeedContent.Dialogue.Office2),
                new DialogueLine(SeedContent.Dialogue.Hacker, SeedContent.Dialogue.Office3));
            ctx.Level03Intro = Dialogue("dlg_level03_intro",
                new DialogueLine(SeedContent.Dialogue.Operator, SeedContent.Dialogue.Farm1),
                new DialogueLine(SeedContent.Dialogue.Operator, SeedContent.Dialogue.Farm2));
            ctx.Level04Intro = Dialogue("dlg_level04_intro",
                new DialogueLine(SeedContent.Dialogue.Operator, SeedContent.Dialogue.Sewers1),
                new DialogueLine(SeedContent.Dialogue.Operator, SeedContent.Dialogue.Sewers2));
            ctx.Level05Intro = Dialogue("dlg_level05_intro",
                new DialogueLine(SeedContent.Dialogue.Operator, SeedContent.Dialogue.Hq1),
                new DialogueLine(SeedContent.Dialogue.Operator, SeedContent.Dialogue.Hq2),
                new DialogueLine(SeedContent.Dialogue.Hacker, SeedContent.Dialogue.Hq3));

            ctx.Database = EditorAssetUtil.LoadOrCreateAsset<GameDatabase>(EditorPaths.ScriptableObjects + "/GameDatabase.asset");
            EditorAssetUtil.SetFields(ctx.Database, so =>
            {
                so.Require("items").objectReferenceValue = itemDb;
                so.SetArray("quests", new[] { ctx.MainQuest, ctx.OfficeQuest, ctx.FarmQuest, ctx.SewersQuest, ctx.HqQuest, ctx.SideQuest, ctx.GhostQuest });
                so.SetArray("skills", ctx.Skills);
                so.SetArray("levels", ctx.Levels);
                so.Require("startingQuest").objectReferenceValue = ctx.MainQuest;
                so.Require("startingCredits").intValue = 150;
                SerializedProperty grants = so.Require("startingItems");
                grants.arraySize = 1;
                grants.GetArrayElementAtIndex(0).FindPropertyRelative("item").objectReferenceValue = ctx.EmpGrenade;
                grants.GetArrayElementAtIndex(0).FindPropertyRelative("quantity").intValue = 2;
            });
        }

        private static GuardConfig Guard(string name, float patrol, float chase, float distance, float angle, float gain, int damage, float trace)
        {
            var config = EditorAssetUtil.LoadOrCreateAsset<GuardConfig>($"{EditorPaths.ScriptableObjects}/{name}.asset");
            config.Configure(patrol, chase, distance, angle, gain, damage, trace);
            EditorUtility.SetDirty(config);
            return config;
        }

        private static ItemDefinition Item(string id, string display, string description, ItemCategory category, int stack, int value, string icon)
        {
            var item = EditorAssetUtil.LoadOrCreateAsset<ItemDefinition>($"{EditorPaths.SoItems}/Item_{id}.asset");
            item.Configure(id, display, description, category, stack, value, GeneratedArt.Sprite(icon));
            EditorUtility.SetDirty(item);
            return item;
        }

        private static SkillDefinition Skill(string id, string display, string description, SkillBranch branch, int cost, SkillEffect effect,
            params SkillDefinition[] requires)
        {
            var skill = EditorAssetUtil.LoadOrCreateAsset<SkillDefinition>($"{EditorPaths.SoSkills}/Skill_{id}.asset");
            skill.Configure(id, display, description, branch, cost, effect, requires);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static QuestDefinition Quest(string id, string title, string description, bool main, int credits, int xp,
            params ObjectiveDefinition[] objectives)
        {
            var quest = EditorAssetUtil.LoadOrCreateAsset<QuestDefinition>($"{EditorPaths.SoQuests}/Quest_{id}.asset");
            quest.Configure(id, title, description, main, credits, xp, objectives);
            EditorUtility.SetDirty(quest);
            return quest;
        }

        private static LevelDefinition Level(string id, string scene, string display, bool unlocked, int order, string description = "")
        {
            var level = EditorAssetUtil.LoadOrCreateAsset<LevelDefinition>($"{EditorPaths.SoLevels}/Level_{id}.asset");
            level.Configure(id, scene, display, unlocked, order);
            level.SetDescription(description);
            EditorUtility.SetDirty(level);
            return level;
        }

        private static DialogueDefinition Dialogue(string id, params DialogueLine[] lines)
        {
            var dialogue = EditorAssetUtil.LoadOrCreateAsset<DialogueDefinition>($"{EditorPaths.SoDialogue}/{id}.asset");
            dialogue.Configure(id, lines);
            EditorUtility.SetDirty(dialogue);
            return dialogue;
        }

        // ---------------------------------------------------------------- tiles & post

        private static void BuildTiles(BuildContext ctx)
        {
            ctx.TileMetal = Tile("tile_metal");
            ctx.TileMetalTop = Tile("tile_metal_top");
            ctx.TileConcrete = Tile("tile_concrete");
            ctx.TileBrick = Tile("tile_brick");
            ctx.TileGlass = Tile("tile_glass", UnityEngine.Tilemaps.Tile.ColliderType.None);
        }

        private static TileBase Tile(string sprite, Tile.ColliderType collider = UnityEngine.Tilemaps.Tile.ColliderType.Grid)
        {
            var tile = EditorAssetUtil.LoadOrCreateAsset<Tile>($"{EditorPaths.Tilesets}/Tiles/{sprite}.asset");
            tile.sprite = GeneratedArt.Tile(sprite);
            tile.colliderType = collider;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static void BuildVolumeProfiles(BuildContext ctx)
        {
            ctx.MenuProfile = Profile("VP_MainMenu", new Color(0.95f, 0.85f, 1f), 1.1f);
            ctx.HubProfile = Profile("VP_Hub", new Color(1f, 0.88f, 0.95f), 0.9f);
            ctx.Level01Profile = Profile("VP_Level01_Rooftops", new Color(0.82f, 0.92f, 1f), 0.8f);
            ctx.Level02Profile = Profile("VP_Level02_Office", new Color(0.9f, 0.95f, 1f), 0.75f);
            ctx.Level03Profile = Profile("VP_Level03_ServerFarm", new Color(0.75f, 0.92f, 1f), 1f);
            ctx.Level04Profile = Profile("VP_Level04_Sewers", new Color(0.78f, 1f, 0.85f), 0.7f);
            ctx.Level05Profile = Profile("VP_Level05_HQ", new Color(1f, 0.88f, 0.8f), 0.95f);
            ctx.ShowcaseProfile = Profile("VP_RubricShowcase", Color.white, 0.8f);
        }

        private static VolumeProfile Profile(string name, Color tint, float bloomIntensity)
        {
            VolumeProfile profile = EditorAssetUtil.LoadOrCreateAsset<VolumeProfile>($"{EditorPaths.SettingsUrp}/{name}.asset");
            foreach (VolumeComponent existing in profile.components.ToArray())
            {
                profile.components.Remove(existing);
                Object.DestroyImmediate(existing, true);
            }

            var bloom = AddComponent<Bloom>(profile);
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(bloomIntensity);
            bloom.scatter.Override(0.6f);
            bloom.highQualityFiltering.Override(false);
            bloom.downscale.Override(BloomDownscaleMode.Quarter);
            bloom.maxIterations.Override(4);

            var color = AddComponent<ColorAdjustments>(profile);
            color.colorFilter.Override(tint);
            color.saturation.Override(12f);
            color.contrast.Override(8f);

            var vignette = AddComponent<Vignette>(profile);
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.45f);

            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static T AddComponent<T>(VolumeProfile profile) where T : VolumeComponent
        {
            T component = profile.Add<T>(true);
            component.name = typeof(T).Name;
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        // ---------------------------------------------------------------- audio & rubric

        private static void BuildSfxLibrary(BuildContext ctx)
        {
            ctx.Sfx = EditorAssetUtil.LoadOrCreateAsset<SfxLibrary>(EditorPaths.SoAudio + "/SfxLibrary.asset");
            SfxEntry E(SfxId id, string clip, float volume = 1f, AudioChannel channel = AudioChannel.Sfx) =>
                new(id, new[] { GeneratedArt.Sfx(clip) }, volume, 0.06f, channel);

            ctx.Sfx.SetEntries(new[]
            {
                E(SfxId.Jump, "jump", 0.6f), E(SfxId.DoubleJump, "double_jump", 0.6f), E(SfxId.Land, "land", 0.8f),
                E(SfxId.Dash, "dash", 0.7f), E(SfxId.Hurt, "hurt"), E(SfxId.Death, "death"),
                E(SfxId.UiClick, "ui_click", 0.7f, AudioChannel.UI), E(SfxId.UiHover, "ui_hover", 0.4f, AudioChannel.UI),
                E(SfxId.UiBack, "ui_back", 0.7f, AudioChannel.UI), E(SfxId.HackBeep, "hack_beep", 0.5f, AudioChannel.UI),
                E(SfxId.HackSuccess, "hack_success", 0.8f, AudioChannel.UI), E(SfxId.HackFail, "hack_fail", 0.8f, AudioChannel.UI),
                E(SfxId.DoorOpen, "door_open"), E(SfxId.DoorClose, "door_close"), E(SfxId.Pickup, "pickup", 0.8f),
                E(SfxId.Checkpoint, "checkpoint", 0.8f), E(SfxId.TeleportOut, "teleport_out"), E(SfxId.TeleportIn, "teleport_in"),
                E(SfxId.Alarm, "alarm", 0.7f), E(SfxId.Spotted, "spotted", 0.8f), E(SfxId.Splash, "splash"),
                E(SfxId.EmpBlast, "emp"), E(SfxId.TurretShot, "turret_shot", 0.6f)
            }, new[]
            {
                new FootstepEntry(SurfaceType.Concrete, new[] { GeneratedArt.Sfx("step_concrete_0"), GeneratedArt.Sfx("step_concrete_1") }, 0.45f),
                new FootstepEntry(SurfaceType.Metal, new[] { GeneratedArt.Sfx("step_metal_0"), GeneratedArt.Sfx("step_metal_1") }, 0.5f),
                new FootstepEntry(SurfaceType.Glass, new[] { GeneratedArt.Sfx("step_glass_0") }, 0.45f),
                new FootstepEntry(SurfaceType.Grass, new[] { GeneratedArt.Sfx("step_grass_0"), GeneratedArt.Sfx("step_grass_1") }, 0.4f),
                new FootstepEntry(SurfaceType.Water, new[] { GeneratedArt.Sfx("step_water_0") }, 0.5f)
            });
            EditorUtility.SetDirty(ctx.Sfx);
        }

        private static void BuildRubric(BuildContext ctx)
        {
            ctx.Rubric = EditorAssetUtil.LoadOrCreateAsset<RubricCatalog>(EditorPaths.ScriptableObjects + "/RubricCatalog.asset");
            ctx.Rubric.SetEntries(new[]
            {
                new RubricEntry(Strings.Rubric.Movement, SeedContent.Rubric.Movement, SceneNames.Level01, "l1_movement"),
                new RubricEntry(Strings.Rubric.Materials, SeedContent.Rubric.Materials, SceneNames.RubricShowcase, "room_02"),
                new RubricEntry(Strings.Rubric.Environment, SeedContent.Rubric.Environment, SceneNames.Level01, "l1_park"),
                new RubricEntry(Strings.Rubric.Water, SeedContent.Rubric.Water, SceneNames.Level04, "l4_water"),
                new RubricEntry(Strings.Rubric.Lighting, SeedContent.Rubric.Lighting, SceneNames.Hub, "hub_center"),
                new RubricEntry(Strings.Rubric.Prefabs, SeedContent.Rubric.Prefabs, SceneNames.RubricShowcase, "room_06"),
                new RubricEntry(Strings.Rubric.Particles, SeedContent.Rubric.Particles, SceneNames.Level01, "l1_nodes"),
                new RubricEntry(Strings.Rubric.Menu, SeedContent.Rubric.Menu, SceneNames.MainMenu, string.Empty),
                new RubricEntry(Strings.Rubric.Sound, SeedContent.Rubric.Sound, SceneNames.RubricShowcase, "room_09"),
                new RubricEntry(Strings.Rubric.Animation, SeedContent.Rubric.Animation, SceneNames.RubricShowcase, "room_10"),
                new RubricEntry(Strings.Rubric.Collisions, SeedContent.Rubric.Collisions, SceneNames.Level01, "l1_security"),
                new RubricEntry(Strings.Rubric.Scripts, SeedContent.Rubric.Scripts, SceneNames.Level01, "l1_drone"),
                new RubricEntry(Strings.Rubric.Cinematics, SeedContent.Rubric.Cinematics, SceneNames.Intro, string.Empty),
                new RubricEntry(Strings.Rubric.Multiplayer, SeedContent.Rubric.Multiplayer, SceneNames.MainMenu, string.Empty)
            });
            EditorUtility.SetDirty(ctx.Rubric);
        }
    }
}
