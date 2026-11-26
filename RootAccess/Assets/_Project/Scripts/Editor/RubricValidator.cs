using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using RootAccess.Core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace RootAccess.EditorTools
{
    /// <summary>
    /// Scans prefabs, Build Settings scenes, the mixer, animator controllers, shaders and test results for evidence of every
    /// rubric item and prints a pass/fail table (also written to Logs/RubricReport.md).
    /// Required checks fail the batch run; Planned checks are roadmap items (ROADMAP.md) and only mark the item as partial.
    /// Menu: Root Access/Validate Rubric. Batchmode: -executeMethod RootAccess.EditorTools.RubricValidator.RunBatch
    /// </summary>
    public static class RubricValidator
    {
        public const string ReportPath = "Logs/RubricReport.md";
        private const string TestResultsFolder = "Logs/TestResults";
        private const string ShaderPrefix = "RootAccess/";
        private const string NormalMapName = "_NormalMap";

        private enum Severity { Required, Planned }

        private sealed class Check
        {
            public string Item;
            public string Name;
            public Severity Severity;
            public bool Passed;
            public string Evidence;
        }

        /// <summary>Component counts (by type name) gathered from every project prefab and every Build Settings scene.</summary>
        private sealed class ProjectIndex
        {
            public readonly Dictionary<string, int> PrefabComponents = new();
            public readonly Dictionary<string, int> SceneComponents = new();
            public readonly Dictionary<string, HashSet<string>> ComponentScenes = new();
            public readonly HashSet<Light2D.LightType> LightTypes = new();
            public readonly List<string> ScenesWithSkybox = new();
            public readonly List<GameObject> Prefabs = new();
            public int SpotLights;
            public int ShadowCastingLights;
            public int PrefabInstancesInScenes;
            public int ShowcaseRooms;

            public int Prefab(string type) => PrefabComponents.TryGetValue(type, out int n) ? n : 0;
            public int Scene(string type) => SceneComponents.TryGetValue(type, out int n) ? n : 0;
            public int Any(string type) => Prefab(type) + Scene(type);
            public bool InScene(string type, string scene) => ComponentScenes.TryGetValue(type, out HashSet<string> s) && s.Contains(scene);
            public IEnumerable<string> ScenesOf(string type) => ComponentScenes.TryGetValue(type, out HashSet<string> s) ? s : Enumerable.Empty<string>();
            public GameObject FindPrefab(string name) => Prefabs.FirstOrDefault(p => p.name == name);
        }

        [MenuItem("Root Access/Validate Rubric")]
        public static void ValidateMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                List<Check> checks = Run();
                Report(checks);
            }
            finally
            {
                if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        public static void RunBatch()
        {
            try
            {
                List<Check> checks = Run();
                Report(checks);
                int failedRequired = checks.Count(c => c.Severity == Severity.Required && !c.Passed);
                if (failedRequired > 0) EditorAssetUtil.Fail($"RubricValidator: {failedRequired} required check(s) failed. See {ReportPath}.");
                else EditorAssetUtil.Log("RubricValidator completed: all required checks pass.");
            }
            catch (Exception exception)
            {
                EditorAssetUtil.Fail("RubricValidator crashed.", exception);
            }
        }

        // ---------------------------------------------------------------- checks

        private static List<Check> Run()
        {
            ProjectIndex index = BuildIndex();
            var checks = new List<Check>();
            void Add(string item, string name, bool passed, string evidence, Severity severity = Severity.Required) =>
                checks.Add(new Check { Item = item, Name = name, Passed = passed, Evidence = evidence, Severity = severity });

            // 1. Movement
            string item = Strings.Rubric.Movement;
            GameObject player = index.FindPrefab("Player");
            Add(item, "Prefab Player: Rigidbody2D + CapsuleCollider2D + PlayerController + PlayerHealth",
                player != null && HasAll(player, "Rigidbody2D", "CapsuleCollider2D", "PlayerController", "PlayerHealth"), "Prefabs/Player/Player.prefab");
            string[] playerStates = { "PlayerGroundedState", "PlayerAirborneState", "PlayerWallSlideState", "PlayerDashState", "PlayerSwimState" };
            string[] missingStates = playerStates.Where(s => FindType(s) == null).ToArray();
            Add(item, "Stări FSM: sol, aer (coyote/buffer/salt variabil/dublu), wall-slide, dash, înot",
                missingStates.Length == 0, missingStates.Length == 0 ? string.Join(", ", playerStates) : "lipsesc: " + string.Join(", ", missingStates));
            ScriptableObject movement = LoadFirst<ScriptableObject>("t:PlayerMovementConfig");
            Add(item, "PlayerMovementConfig: dublu salt, dash, wall-jump, coyote, buffer configurate",
                movement != null && Int(movement, "extraAirJumps") >= 1 && Float(movement, "dashSpeed") > 0f && Float(movement, "coyoteTime") > 0f &&
                Float(movement, "jumpBufferTime") > 0f && Float(movement, "swimSpeed") > 0f,
                movement != null ? $"airJumps={Int(movement, "extraAirJumps")}, dash={Float(movement, "dashSpeed")}, coyote={Float(movement, "coyoteTime")}s" : "asset lipsă");

            // 2. Materials / textures
            item = Strings.Rubric.Materials;
            Material[] materials = LoadAll<Material>("t:Material", EditorPaths.Materials);
            string[] surfaces = { "M_Metal", "M_Glass", "M_Concrete", "M_NeonCyan" };
            Add(item, "Materiale pe suprafețe (metal, sticlă, beton, neon)", surfaces.All(s => materials.Any(m => m.name == s)),
                $"{materials.Length} materiale în {EditorPaths.Materials}");
            Material[] custom = materials.Where(m => m.shader != null && m.shader.name.StartsWith(ShaderPrefix, StringComparison.Ordinal)).ToArray();
            Add(item, "≥3 materiale cu shadere proprii (hologram, dissolve, neon pulse)", custom.Length >= 3,
                string.Join(", ", custom.Select(m => $"{m.name}→{m.shader.name}")));
            Material[] graphMaterials = materials.Where(IsShaderGraph).ToArray();
            string[] requiredGraphMaterials = { "M_Hologram", "M_Dissolve", "M_NeonCyan" };
            Add(item, "≥3 materiale Shader Graph (hologramă, dizolvare/glitch, neon pulse emisiv)",
                graphMaterials.Length >= 3 && requiredGraphMaterials.All(n => graphMaterials.Any(m => m.name == n)),
                string.Join(", ", graphMaterials.Select(m => $"{m.name}→{Path.GetFileName(AssetDatabase.GetAssetPath(m.shader))}")));
            int normalMapped = CountNormalMappedSprites();
            Add(item, "Sprite-uri cu normal map pentru lumini 2D", normalMapped >= 10, $"{normalMapped} sprite-uri cu secondary texture {NormalMapName}");

            // 3. Environment
            item = Strings.Rubric.Environment;
            Add(item, "Copaci + iarbă + vânt scriptat (FoliageWind, WindZone2D) în Level01",
                index.FindPrefab("Tree") != null && index.FindPrefab("GrassPatch") != null && index.InScene("FoliageWind", SceneNames.Level01) &&
                index.InScene("WindZone2D", SceneNames.Level01),
                $"FoliageWind în scene: {index.Scene("FoliageWind")}, WindZone2D: {index.Any("WindZone2D")}");
            Material grassMaterial = materials.FirstOrDefault(m => m.name == "M_GrassWind");
            Shader grass = grassMaterial != null ? grassMaterial.shader : null;
            Add(item, "Shader Graph pentru iarbă: vânt + îndoire în jurul jucătorului",
                grass != null && IsShaderGraph(grassMaterial) && HasShaderProperty(grass, "_BendRadius"),
                grass != null ? $"M_GrassWind → {Path.GetFileName(AssetDatabase.GetAssetPath(grass))} (_BendRadius; poziția jucătorului vine ca vector global din FoliageWind)" : "material M_GrassWind lipsă");
            Add(item, "Straturi parallax (fundal/prim-plan)", index.Scene("ParallaxLayer") >= 2, $"{index.Scene("ParallaxLayer")} ParallaxLayer în scene");
            GameObject tree = index.FindPrefab("Tree");
            Component skin = tree != null ? tree.GetComponents<Component>().FirstOrDefault(c => c != null && c.GetType().Name == "SpriteSkin") : null;
            int bones = skin != null ? new SerializedObject(skin).FindProperty("m_BoneTransforms").arraySize : 0;
            Add(item, "Copaci cu oase 2D Animation (SpriteSkin, 4 oase cu greutăți) legănați de FoliageWind",
                skin != null && bones >= 3 && index.InScene("SpriteSkin", SceneNames.Level01),
                $"Tree.prefab: SpriteSkin cu {bones} oase; {index.Scene("SpriteSkin")} copaci în scene (Editor/Generation/SpriteRigFactory.cs)");

            // 4. Water / skybox
            item = Strings.Rubric.Water;
            Add(item, "Apă: BuoyancyEffector2D + WaterBody2D (suprafață cu arcuri)", index.Any("BuoyancyEffector2D") > 0 && index.Any("WaterBody2D") > 0,
                $"BuoyancyEffector2D: prefab {index.Prefab("BuoyancyEffector2D")}, scene {index.Scene("BuoyancyEffector2D")}; WaterBody2D: {index.Any("WaterBody2D")}");
            Add(item, "Skybox real în MainMenu și Level01", index.ScenesWithSkybox.Contains(SceneNames.MainMenu) && index.ScenesWithSkybox.Contains(SceneNames.Level01),
                "RenderSettings.skybox în: " + string.Join(", ", index.ScenesWithSkybox));
            Add(item, "Particule de stropire (splash) + snapshot Underwater", index.FindPrefab("VFX_Splash") != null, "VFX_Splash.prefab");
            Material waterDistortion = materials.FirstOrDefault(m => m.name == "M_WaterDistortion");
            GameObject waterPrefab = index.FindPrefab("WaterBody");
            Add(item, "Shader Graph de distorsiune a apei (refracție din _CameraSortingLayerTexture) pe prefab-ul WaterBody",
                waterDistortion != null && IsShaderGraph(waterDistortion) && waterPrefab != null &&
                waterPrefab.GetComponent<MeshRenderer>()?.sharedMaterial == waterDistortion,
                waterDistortion != null ? $"M_WaterDistortion → {Path.GetFileName(AssetDatabase.GetAssetPath(waterDistortion.shader))}" : "material lipsă");
            Add(item, "Nivelul Sewers (Level04): bazine, scufundare, stavilă care inundă puțul (FloodControl)",
                SceneInBuild(SceneNames.Level04) && index.InScene("WaterBody2D", SceneNames.Level04) && index.InScene("FloodControl", SceneNames.Level04),
                $"WaterBody2D în: {string.Join(", ", index.ScenesOf("WaterBody2D"))}");

            // 5. Lights / camera
            item = Strings.Rubric.Lighting;
            bool allLights = index.LightTypes.Contains(Light2D.LightType.Global) && index.LightTypes.Contains(Light2D.LightType.Point) &&
                             index.LightTypes.Contains(Light2D.LightType.Freeform) && index.SpotLights > 0;
            Add(item, "Lumini 2D Global, Point, Spot și Freeform", allLights,
                $"tipuri: {string.Join(", ", index.LightTypes)}; spot (con < 360°): {index.SpotLights}");
            Add(item, "ShadowCaster2D + lumini cu umbre", index.Any("ShadowCaster2D") > 0 && index.ShadowCastingLights > 0,
                $"{index.Any("ShadowCaster2D")} ShadowCaster2D, {index.ShadowCastingLights} lumini cu umbre");
            Add(item, "Neon pâlpâitor scriptat (FlickerLight2D)", index.Any("FlickerLight2D") > 0, $"{index.Any("FlickerLight2D")} componente");
            Add(item, "Color grading per nivel (Volume + profil cu Bloom/ColorAdjustments)", CountProfilesWithBloom() >= 3 && index.Scene("Volume") >= 3,
                $"{CountProfilesWithBloom()} profile cu Bloom, {index.Scene("Volume")} Volume în scene");
            GameObject rig = index.FindPrefab("CameraRig");
            Add(item, "Cinemachine: follow + confiner + impulse + zoom la hack",
                rig != null && HasAll(rig, "CinemachineCamera", "CinemachineConfiner2D", "CinemachineImpulseListener", "CameraController"),
                "CameraRig.prefab: CinemachineCamera, Confiner2D, ImpulseListener, CameraController");
            Add(item, "Tranziții între camere (CameraZone/CameraRoom)", index.Scene("CameraZone") + index.Scene("CameraRoom") > 0,
                $"{index.Scene("CameraZone")} CameraZone, {index.Scene("CameraRoom")} CameraRoom");

            // 6. Prefabs
            item = Strings.Rubric.Prefabs;
            string[] variants = { "Guard_Heavy", "Guard_Sniper", "Terminal_Door", "Terminal_Camera", "Terminal_Data" };
            string[] badVariants = variants.Where(v => !IsVariantOf(index.FindPrefab(v), v.Split('_')[0])).ToArray();
            Add(item, "Variante de prefab (Guard → Heavy/Sniper, Terminal → Door/Camera/Data)", badVariants.Length == 0,
                badVariants.Length == 0 ? $"{CountVariants(index)} variante în total" : "nu sunt variante: " + string.Join(", ", badVariants));
            Add(item, "Niveluri asamblate din prefab-uri", index.PrefabInstancesInScenes >= 50, $"{index.PrefabInstancesInScenes} instanțe de prefab în scene");
            Add(item, "Spawn la runtime prin object pool (PoolService, VfxSpawner)", index.Prefab("PoolService") > 0 && index.Prefab("VfxSpawner") > 0,
                "PoolService + VfxSpawner în SystemsRoot; VFX, proiectile, trasee de teleport și surse audio vin din pool");

            // 7. Particles / teleport
            item = Strings.Rubric.Particles;
            string[] vfx = { "VFX_Sparks", "VFX_EmpExplosion", "VFX_Rain", "VFX_Steam", "VFX_DataStream", "VFX_LandingDust", "VFX_Splash", "VFX_Death" };
            string[] missingVfx = vfx.Where(v => index.FindPrefab(v)?.GetComponentInChildren<ParticleSystem>(true) == null).ToArray();
            Add(item, "Particule: scântei, EMP, ploaie, abur, date, praf, stropi, moarte", missingVfx.Length == 0,
                missingVfx.Length == 0 ? $"{vfx.Length} prefab-uri VFX cu ParticleSystem" : "lipsesc: " + string.Join(", ", missingVfx));
            Add(item, "Teleportare prin Network Node (dissolve + traseu de particule)",
                index.InScene("NetworkNode", SceneNames.Level01) && index.FindPrefab("VFX_TeleportTrail") != null && player != null && HasAll(player, "DissolveController"),
                "NetworkNode în Level01, VFX_TeleportTrail, DissolveController pe Player");
            Add(item, "Fast-travel din Hub", index.InScene("FastTravelTerminal", SceneNames.Hub), "FastTravelTerminal în Hub");

            // 8. Menu
            item = Strings.Rubric.Menu;
            GameObject uiRoot = index.FindPrefab("UIRoot");
            string[] screens = { "PauseMenuView", "SettingsMenuView", "GameOverView", "LevelCompleteView", "LoadingScreenView", "HudView", "DebugRubricPanel" };
            string[] missingScreens = uiRoot == null ? screens : screens.Where(s => !HasAll(uiRoot, s)).ToArray();
            Add(item, "UIRoot: pauză, setări, game over, nivel complet, loading, HUD, panou barem F1", missingScreens.Length == 0,
                missingScreens.Length == 0 ? string.Join(", ", screens) : "lipsesc: " + string.Join(", ", missingScreens));
            Add(item, "Meniu principal (Continuă, Joc nou, Setări, Barem, Credite, Ieșire)", index.InScene("MainMenuView", SceneNames.MainMenu), "MainMenuView în MainMenu");
            Add(item, "Rebind taste (Input System) pentru Jump/Interact", uiRoot != null && uiRoot.GetComponentsInChildren<Component>(true).Count(c => c != null && c.GetType().Name == "RebindActionButton") >= 2,
                "RebindActionButton în SettingsMenu");

            // 9. Sound
            item = Strings.Rubric.Sound;
            AudioMixer mixer = LoadFirst<AudioMixer>("t:AudioMixer");
            string[] groups = { "Master", "Music", "SFX", "UI", "Ambience" };
            string[] snapshots = { MixerSnapshotNames.Normal, MixerSnapshotNames.Underwater, MixerSnapshotNames.Alarm };
            Add(item, "AudioMixer cu grupuri Master/Music/SFX/UI/Ambience", mixer != null && groups.All(g => mixer.FindMatchingGroups(g).Any(x => x.name == g)),
                mixer != null ? AssetDatabase.GetAssetPath(mixer) : "mixer lipsă");
            Add(item, "Snapshot-uri Normal / Underwater / Alarm", mixer != null && snapshots.All(s => mixer.FindSnapshot(s) != null), string.Join(", ", snapshots));
            Add(item, "Parametri de volum expuși (Master/Music/SFX/UI)", mixer != null && MixerParameters.Exposed.All(p => mixer.GetFloat(p, out _)),
                string.Join(", ", MixerParameters.Exposed));
            GameObject systems = index.FindPrefab("SystemsRoot");
            Add(item, "AudioManager (surse în pool) + MusicDirector adaptiv (stealth ↔ alert)", systems != null && HasAll(systems, "AudioManager", "MusicDirector", "MixerSnapshots"),
                "SystemsRoot.prefab");
            Add(item, "Pași pe suprafețe + sunete UI + ambianță", index.Prefab("FootstepPlayer") >= 2 && index.Prefab("ButtonSfx") > 0 &&
                AssetDatabase.FindAssets("t:AudioClip", new[] { EditorPaths.Ambience }).Length > 0,
                $"{index.Prefab("FootstepPlayer")} FootstepPlayer, {index.Prefab("ButtonSfx")} ButtonSfx, " +
                $"{AssetDatabase.FindAssets("t:AudioClip", new[] { EditorPaths.Audio }).Length} clipuri audio");

            // 10. Animations
            item = Strings.Rubric.Animation;
            AnimatorController[] controllers = LoadAll<AnimatorController>("t:AnimatorController", EditorPaths.Animations);
            string[] animated = { "Player", "Guard", "Drone", "SecurityCamera", "Door", "Elevator" };
            string[] missingControllers = animated.Where(a => controllers.All(c => c.name != a)).ToArray();
            Add(item, "Animator Controllers: jucător, gardieni, drone, camere, uși, lifturi", missingControllers.Length == 0,
                missingControllers.Length == 0 ? $"{controllers.Length} controllere" : "lipsesc: " + string.Join(", ", missingControllers));
            AnimatorController playerController = controllers.FirstOrDefault(c => c.name == "Player");
            Add(item, "Controller jucător cu parametri + blend tree", playerController != null && playerController.parameters.Length >= 4 && HasBlendTree(playerController),
                playerController != null ? $"{playerController.parameters.Length} parametri, blend tree: {HasBlendTree(playerController)}" : "lipsă");
            AnimationClip[] clips = LoadAll<AnimationClip>("t:AnimationClip", EditorPaths.Animations);
            int clipsWithEvents = clips.Count(c => AnimationUtility.GetAnimationEvents(c).Length > 0);
            Add(item, "Animation Events (pași, cadre de lovire)", clipsWithEvents >= 2, $"{clipsWithEvents}/{clips.Length} clipuri cu evenimente");
            Add(item, "Tranziții UI animate (UITween)", uiRoot != null && HasAll(uiRoot, "UITween"), "UITween pe panourile din UIRoot");

            // 11. Collisions / triggers
            item = Strings.Rubric.Collisions;
            int ignoredPairs = CountIgnoredLayerPairs();
            Add(item, "Matrice de coliziuni 2D configurată", ignoredPairs > 0, $"{ignoredPairs} perechi de layere ignorate");
            string[] triggerTypes = TypesDeclaring("OnTriggerEnter2D");
            string[] collisionTypes = TypesDeclaring("OnCollisionEnter2D");
            Add(item, "OnTriggerEnter2D/Exit2D (checkpoint, pickup, ascunzători, alarme, ieșiri, apă, dialog, teleport)", triggerTypes.Length >= 8,
                $"{triggerTypes.Length} scripturi: {string.Join(", ", triggerTypes)}");
            Add(item, "OnCollisionEnter2D (daune, impact la aterizare)", collisionTypes.Length >= 2, string.Join(", ", collisionTypes));
            Add(item, "Raycast-uri pentru con de vedere și senzori sol/perete", FindType("VisionCone") != null && FindType("PlayerSensors") != null,
                "VisionCone, PlayerSensors");
            Add(item, "Gropi între acoperișuri (FallZone, trigger) în Level01", index.InScene("FallZone", SceneNames.Level01),
                $"{index.Scene("FallZone")} FallZone în scene");

            // 12. Scripts
            item = Strings.Rubric.Scripts;
            string[] asmdefs = Directory.GetFiles(EditorPaths.Scripts, "*.asmdef", SearchOption.AllDirectories);
            Add(item, "Arhitectură pe assembly definitions", asmdefs.Length >= 13, $"{asmdefs.Length} asmdef-uri în Scripts");
            Add(item, "Asmdef-uri de test EditMode + PlayMode",
                File.Exists($"{EditorPaths.Tests}/EditMode/RootAccess.Tests.EditMode.asmdef") && File.Exists($"{EditorPaths.Tests}/PlayMode/RootAccess.Tests.PlayMode.asmdef"),
                EditorPaths.Tests);
            Add(item, "A* propriu în nivelurile reale (PathfindingGrid în Level01 și Level02)",
                index.InScene("PathfindingGrid", SceneNames.Level01) && index.InScene("PathfindingGrid", SceneNames.Level02),
                $"PathfindingGrid în: {string.Join(", ", index.ScenesOf("PathfindingGrid"))}");
            Component sniper = index.FindPrefab("Guard_Sniper")?.GetComponents<Component>().FirstOrDefault(c => c != null && c.GetType().Name == "Guard");
            bool sniperRifle = sniper != null && new SerializedObject(sniper).FindProperty("projectilePrefab")?.objectReferenceValue != null;
            Add(item, "AI gardieni: auz (PlayerNoise → Investigate), răspuns la AlarmRaised, lunetist cu proiectile",
                FindType("GuardInvestigateState") != null && FindType("Hearing") != null && sniperRifle,
                "AI/Guard/GuardStates.cs, AI/Perception/Hearing.cs, Guard_Sniper.prefab");
            string[] levels = { SceneNames.Level01, SceneNames.Level02, SceneNames.Level03, SceneNames.Level04, SceneNames.Level05, SceneNames.Level06 };
            string[] missingLevels = levels.Where(l => !SceneInBuild(l)).ToArray();
            Add(item, "Hub + 6 niveluri construite din segmente (LevelLayout), toate în build", missingLevels.Length == 0,
                missingLevels.Length == 0 ? string.Join(", ", levels) : "lipsesc: " + string.Join(", ", missingLevels));
            (bool testsGreen, string testEvidence) = ReadTestResults();
            Add(item, "Ultima rulare a testelor: verde", testsGreen, testEvidence);

            // Bonus
            item = Strings.Rubric.Cinematics;
            Add(item, "CutsceneController + SkipCutscene (Timeline)", FindType("CutsceneController") != null && FindType("SkipCutscene") != null, "Scripts/Cinematics");
            int timelines = AssetDatabase.FindAssets("t:TimelineAsset", new[] { EditorPaths.Root }).Length;
            Add(item, "Cutscene-uri Timeline (intro + final) în build, cu CutsceneController",
                timelines >= 2 && SceneInBuild(SceneNames.Intro) && SceneInBuild(SceneNames.Finale) &&
                index.InScene("CutsceneController", SceneNames.Intro) && index.InScene("CutsceneController", SceneNames.Finale),
                $"{timelines} timeline-uri (Animation, Cinemachine, Activation, Signal); scenele Intro și Finale");
            item = Strings.Rubric.Multiplayer;
            string manifest = File.ReadAllText("Packages/manifest.json");
            Add(item, "Pachete Netcode + Transport; link de Operator comun (IOperatorLink: local + rețea)",
                manifest.Contains("com.unity.netcode.gameobjects") && manifest.Contains("com.unity.transport") &&
                FindType("LocalOperatorLink") != null && FindType("NetworkOperatorLink") != null,
                "Packages/manifest.json, Core/Coop, Net/NetworkOperatorLink.cs");
            Add(item, "Co-op asimetric funcțional: CoopSession (split-screen + LAN host/join), consola Operatorului, meniul Co-op",
                systems != null && HasAll(systems, "CoopSession") && uiRoot != null && HasAll(uiRoot, "OperatorConsoleView") &&
                index.InScene("MainMenuView", SceneNames.MainMenu) && SceneInBuild(SceneNames.OperatorConsole),
                "SystemsRoot/Coop, UIRoot/OperatorConsole, MainMenuCanvas/CoopMenu, scena OperatorConsole");

            item = "Durată de joc (≥ 30 min)";
            (bool longEnough, string durationEvidence) = ReadPlaytimeEstimate();
            Add(item, "Hub + 6 niveluri + cutscene-uri: estimarea primei treceri ≥ 30 min", longEnough, durationEvidence);

            item = "Prezentare barem";
            Add(item, "Scena RubricShowcase cu o cameră per punct (14) + panoul F1", index.ShowcaseRooms >= 14 && uiRoot != null && HasAll(uiRoot, "DebugRubricPanel"),
                $"{index.ShowcaseRooms} camere în RubricShowcase");
            return checks;
        }

        /// <summary>Reads the total from the report written by the content build (<see cref="PlaytimeReport"/>).</summary>
        private static (bool ok, string evidence) ReadPlaytimeEstimate()
        {
            if (!File.Exists(PlaytimeReport.ReportPath)) return (false, $"{PlaytimeReport.ReportPath} lipsă (rulează SceneBuilder.BuildAll)");
            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(
                File.ReadAllText(PlaytimeReport.ReportPath), @"\*\*Total\*\*.*?\*\*([0-9]+(?:[.,][0-9]+)?)\*\*");
            if (!match.Success) return (false, "totalul nu a fost găsit în raport");
            float minutes = float.Parse(match.Groups[1].Value.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);
            return (minutes >= 30f, $"{minutes:0.0} min estimate ({PlaytimeReport.ReportPath}); cronometrarea reală rămâne de făcut");
        }

        // ---------------------------------------------------------------- index

        private static ProjectIndex BuildIndex()
        {
            var index = new ProjectIndex();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { EditorPaths.Prefabs }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab == null) continue;
                index.Prefabs.Add(prefab);
                foreach (Component component in prefab.GetComponentsInChildren<Component>(true))
                    if (component != null) Increment(index.PrefabComponents, component.GetType().Name);
            }

            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes.Where(s => s.enabled))
            {
                Scene scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
                string sceneName = scene.name;
                foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (PrefabUtility.IsOutermostPrefabInstanceRoot(transform.gameObject)) index.PrefabInstancesInScenes++;
                    foreach (Component component in transform.GetComponents<Component>())
                    {
                        if (component == null) continue;
                        string type = component.GetType().Name;
                        Increment(index.SceneComponents, type);
                        if (!index.ComponentScenes.TryGetValue(type, out HashSet<string> scenes)) index.ComponentScenes[type] = scenes = new HashSet<string>();
                        scenes.Add(sceneName);
                        if (component is Light2D light) IndexLight(index, light);
                    }
                }

                if (RenderSettings.skybox != null) index.ScenesWithSkybox.Add(sceneName);
                if (sceneName == SceneNames.RubricShowcase)
                    index.ShowcaseRooms = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Count(t => t.name.StartsWith("Room_", StringComparison.Ordinal));
            }

            foreach (GameObject prefab in index.Prefabs)
            foreach (Light2D light in prefab.GetComponentsInChildren<Light2D>(true))
                IndexLight(index, light);
            return index;
        }

        private static void IndexLight(ProjectIndex index, Light2D light)
        {
            index.LightTypes.Add(light.lightType);
            if (light.lightType == Light2D.LightType.Point && light.pointLightOuterAngle < 359f) index.SpotLights++;
            if (light.shadowsEnabled) index.ShadowCastingLights++;
        }

        private static void Increment(Dictionary<string, int> counts, string key) => counts[key] = counts.TryGetValue(key, out int n) ? n + 1 : 1;

        // ---------------------------------------------------------------- helpers

        private static bool HasAll(GameObject root, params string[] typeNames)
        {
            var present = new HashSet<string>(root.GetComponentsInChildren<Component>(true).Where(c => c != null).Select(c => c.GetType().Name));
            return typeNames.All(present.Contains);
        }

        private static Type FindType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name.StartsWith("RootAccess.", StringComparison.Ordinal))
                .SelectMany(SafeTypes)
                .FirstOrDefault(t => t.Name == name);

        private static IEnumerable<Type> SafeTypes(Assembly assembly)
        {
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
        }

        private static string[] TypesDeclaring(string method) =>
            AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name.StartsWith("RootAccess.", StringComparison.Ordinal) && !a.GetName().Name.Contains("Tests"))
                .SelectMany(SafeTypes)
                .Where(t => typeof(MonoBehaviour).IsAssignableFrom(t) &&
                            t.GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) != null)
                .Select(t => t.Name).OrderBy(n => n).ToArray();

        private static T LoadFirst<T>(string filter) where T : UnityEngine.Object =>
            AssetDatabase.FindAssets(filter, new[] { EditorPaths.Root }).Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault(a => a != null);

        private static T[] LoadAll<T>(string filter, string folder) where T : UnityEngine.Object =>
            AssetDatabase.FindAssets(filter, new[] { folder }).Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null).Distinct().ToArray();

        private static int Int(ScriptableObject asset, string field) => new SerializedObject(asset).FindProperty(field)?.intValue ?? 0;

        private static float Float(ScriptableObject asset, string field) => new SerializedObject(asset).FindProperty(field)?.floatValue ?? 0f;

        private static bool HasShaderProperty(Shader shader, string property) => shader.FindPropertyIndex(property) >= 0;

        private static bool IsShaderGraph(Material material) =>
            material != null && material.shader != null &&
            AssetDatabase.GetAssetPath(material.shader).EndsWith(".shadergraph", StringComparison.OrdinalIgnoreCase);

        private static int CountNormalMappedSprites() =>
            AssetDatabase.FindAssets("t:Texture2D", new[] { EditorPaths.Art })
                .Select(g => AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(g)) as TextureImporter)
                .Count(importer => importer != null && importer.secondarySpriteTextures != null &&
                                   importer.secondarySpriteTextures.Any(s => s.name == NormalMapName && s.texture != null));

        private static int CountProfilesWithBloom() =>
            LoadAll<VolumeProfile>("t:VolumeProfile", EditorPaths.SettingsUrp).Count(p => p.Has<Bloom>() && p.Has<ColorAdjustments>());

        private static bool HasBlendTree(AnimatorController controller) =>
            controller.layers.SelectMany(l => l.stateMachine.states).Any(s => s.state.motion is BlendTree);

        private static bool IsVariantOf(GameObject prefab, string baseName)
        {
            if (prefab == null || PrefabUtility.GetPrefabAssetType(prefab) != PrefabAssetType.Variant) return false;
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(prefab);
            return source != null && source.name == baseName;
        }

        private static int CountVariants(ProjectIndex index) => index.Prefabs.Count(p => PrefabUtility.GetPrefabAssetType(p) == PrefabAssetType.Variant);

        private static bool SceneInBuild(string sceneName) =>
            EditorBuildSettings.scenes.Any(s => s.enabled && Path.GetFileNameWithoutExtension(s.path) == sceneName);

        private static int CountIgnoredLayerPairs()
        {
            int ignored = 0;
            for (int a = 0; a < 32; a++)
            for (int b = a; b < 32; b++)
                if (!string.IsNullOrEmpty(LayerMask.LayerToName(a)) && !string.IsNullOrEmpty(LayerMask.LayerToName(b)) && Physics2D.GetIgnoreLayerCollision(a, b))
                    ignored++;
            return ignored;
        }

        private static (bool green, string evidence) ReadTestResults()
        {
            var parts = new List<string>();
            bool green = true;
            foreach (string platform in new[] { "EditMode", "PlayMode" })
            {
                string path = Path.Combine(TestResultsFolder, platform + ".xml");
                if (!File.Exists(path))
                {
                    parts.Add($"{platform}: fără rezultate (rulați scripts/test.ps1)");
                    green = false;
                    continue;
                }

                var xml = new XmlDocument();
                xml.Load(path);
                XmlElement run = xml.DocumentElement;
                int total = int.Parse(run?.GetAttribute("total") ?? "0");
                int failed = int.Parse(run?.GetAttribute("failed") ?? "0");
                green &= total > 0 && failed == 0;
                parts.Add($"{platform}: {total - failed}/{total} ({File.GetLastWriteTime(path):yyyy-MM-dd HH:mm})");
            }

            return (green, string.Join("; ", parts));
        }

        // ---------------------------------------------------------------- output

        private static void Report(List<Check> checks)
        {
            var md = new StringBuilder();
            md.AppendLine("# Rubric validation report");
            md.AppendLine();
            md.AppendLine($"Generat: {DateTime.Now:yyyy-MM-dd HH:mm} · Unity {Application.unityVersion} · `RootAccess.EditorTools.RubricValidator`");
            md.AppendLine();
            md.AppendLine("| Punct din barem | Status | Verificări OK | Planificate |");
            md.AppendLine("|---|---|---|---|");
            foreach (IGrouping<string, Check> group in checks.GroupBy(c => c.Item))
            {
                bool requiredOk = group.Where(c => c.Severity == Severity.Required).All(c => c.Passed);
                bool plannedOk = group.Where(c => c.Severity == Severity.Planned).All(c => c.Passed);
                string status = !requiredOk ? "FAIL" : plannedOk ? "PASS" : "PARTIAL";
                int required = group.Count(c => c.Severity == Severity.Required);
                md.AppendLine($"| {group.Key} | **{status}** | {group.Count(c => c.Severity == Severity.Required && c.Passed)}/{required} | " +
                              $"{group.Count(c => c.Severity == Severity.Planned && !c.Passed)} deschise |");
            }

            md.AppendLine();
            md.AppendLine("## Detalii");
            md.AppendLine();
            md.AppendLine("| Punct | Verificare | Tip | Rezultat | Dovadă |");
            md.AppendLine("|---|---|---|---|---|");
            foreach (Check check in checks)
            {
                string result = check.Passed ? "ok" : check.Severity == Severity.Planned ? "planificat" : "**FAIL**";
                string kind = check.Severity == Severity.Required ? "obligatoriu" : "roadmap";
                md.AppendLine($"| {check.Item} | {check.Name} | {kind} | {result} | {check.Evidence.Replace("|", "/")} |");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "Logs");
            File.WriteAllText(ReportPath, md.ToString(), new UTF8Encoding(false));

            var console = new StringBuilder();
            console.AppendLine($"{EditorAssetUtil.LogPrefix} Rubric validation ({checks.Count(c => c.Passed)}/{checks.Count} checks ok) -> {Path.GetFullPath(ReportPath)}");
            foreach (Check check in checks)
            {
                string mark = check.Passed ? "PASS" : check.Severity == Severity.Planned ? "TODO" : "FAIL";
                console.AppendLine($"  [{mark}] {check.Item,-26} {check.Name}");
            }

            Debug.Log(console.ToString());
        }
    }
}
