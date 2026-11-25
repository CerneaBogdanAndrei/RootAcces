using System.Collections.Generic;
using System.Linq;
using RootAccess.AI;
using RootAccess.Core;
using RootAccess.Gameplay;
using RootAccess.Systems;
using RootAccess.VFX;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace RootAccess.EditorTools
{
    /// <summary>
    /// One-click (or batchmode) content pipeline: builds every asset, prefab and scene of the vertical slice and
    /// registers the scenes in Build Settings. Safe to re-run: scenes and prefabs are regenerated from code.
    /// </summary>
    public static class SceneBuilder
    {
        public static readonly string[] BuildOrder =
        {
            SceneNames.Bootstrap, SceneNames.MainMenu, SceneNames.Hub, SceneNames.Level01, SceneNames.Level02, SceneNames.Level03,
            SceneNames.Level04, SceneNames.Level05, SceneNames.RubricShowcase, SceneNames.Credits, SceneNames.Intro, SceneNames.Finale,
            SceneNames.OperatorConsole
        };

        internal sealed class GameplayScene
        {
            public PolygonCollider2D CameraBounds;
            public GameObject Player;
            public GameObject Rig;
            public Transform Root;
            public Transform Grid;
            public Tilemap Ground;
            public Tilemap Walls;
            public Tilemap Backdrop;
        }

        [MenuItem("Root Access/Build/Build All Content (assets, prefabs, scenes)")]
        public static void BuildAll()
        {
            EditorAssetUtil.RunBatchStep("SceneBuilder", () =>
            {
                var ctx = new BuildContext();
                PlaytimeReport.Clear();
                AssetFactory.BuildAll(ctx);
                AnimationFactory.BuildAll(ctx);
                VfxFactory.BuildAll(ctx);
                UIRootBuilder.BuildUIRoot(ctx);
                UIRootBuilder.BuildMainMenuCanvas(ctx);
                UIRootBuilder.BuildCreditsCanvas(ctx);
                PrefabFactory.BuildAll(ctx);

                BuildBootstrap();
                BuildMainMenu(ctx);
                BuildHub(ctx);
                BuildLevel01(ctx);
                BuildLevel02(ctx);
                BuildLevel03(ctx);
                BuildLevel04(ctx);
                BuildLevel05(ctx);
                CutsceneFactory.BuildAll(ctx);
                PlaytimeReport.Write();
                BuildRubricShowcase(ctx);
                BuildCredits(ctx);
                BuildOperatorConsole(ctx);
                ConfigureBuildSettings();
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            });
        }

        public static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = BuildOrder.Select(s => new EditorBuildSettingsScene(EditorPaths.ScenePath(s), true)).ToArray();
            EditorAssetUtil.Log($"Build Settings: {string.Join(", ", BuildOrder)}");
        }

        // ---------------------------------------------------------------- shared helpers

        internal static Scene NewScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        internal static void Save(Scene scene, string name)
        {
            BakeTilemapColliders(scene, name);
            EditorAssetUtil.EnsureFolder(EditorPaths.SceneFolder(name));
            if (!EditorSceneManager.SaveScene(scene, EditorPaths.ScenePath(name)))
                throw new System.InvalidOperationException($"Could not save scene {name}.");
        }

        /// <summary>
        /// Batchmode never runs the tilemap collider's deferred update, so the composite would be saved with no paths and
        /// the player would fall through every floor. Generate the shapes explicitly and fail loudly if any are missing.
        /// </summary>
        private static void BakeTilemapColliders(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (TilemapCollider2D tileCollider in root.GetComponentsInChildren<TilemapCollider2D>(true))
            {
                var tilemap = tileCollider.GetComponent<Tilemap>();
                tilemap.CompressBounds();
                tileCollider.ProcessTilemapChanges();
                int paths = 0;
                if (tileCollider.TryGetComponent(out CompositeCollider2D composite))
                {
                    composite.GenerateGeometry();
                    paths = composite.pathCount;
                }

                int tiles = tilemap.GetUsedTilesCount();
                if (tiles > 0 && composite != null && paths == 0)
                    throw new System.InvalidOperationException($"{name}/{tileCollider.name}: {tiles} tile(s) but the composite collider has no paths.");
                EditorAssetUtil.Log($"{name}/{tileCollider.name}: {tiles} tile type(s), {paths} collider path(s), shapes={tileCollider.shapeCount}");
            }
        }

        internal static GameObject Place(GameObject prefab, Vector3 position, Transform parent = null, string name = null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.position = position;
            if (name != null) instance.name = name;
            return instance;
        }

        internal static GameObject Place(BuildContext ctx, string prefab, Vector3 position, Transform parent = null, string name = null) =>
            Place(ctx.Prefab(prefab), position, parent, name);

        internal static Transform Group(string name, Transform parent = null) => PrefabFactory.Go(name, parent).transform;

        internal static Light2D GlobalLight(Transform parent, Color color, float intensity)
        {
            Light2D light = PrefabFactory.Light(parent, "Global Light 2D", Light2D.LightType.Global, color, intensity, 1f);
            return light;
        }

        internal static void PostVolume(Transform parent, VolumeProfile profile)
        {
            GameObject go = PrefabFactory.Go("Global Volume", parent);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        internal static TextMeshPro Label(Transform parent, string text, Vector3 position, float size, Color color, float width = 18f)
        {
            GameObject go = PrefabFactory.Go("Label", parent, Layers.Default, position);
            var label = go.AddComponent<TextMeshPro>();
            if (TMP_Settings.defaultFontAsset != null) label.font = TMP_Settings.defaultFontAsset;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(width, 4f);
            label.textWrappingMode = TextWrappingModes.Normal;
            go.GetComponent<MeshRenderer>().sortingLayerName = SortingLayers.Foreground;
            return label;
        }

        internal static PolygonCollider2D Bounds(Transform parent, string name, Rect rect)
        {
            GameObject go = PrefabFactory.Go(name, parent, Layers.IgnoreRaycast);
            var polygon = go.AddComponent<PolygonCollider2D>();
            polygon.isTrigger = true;
            polygon.points = new[]
            {
                new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMin),
                new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMin, rect.yMax)
            };
            return polygon;
        }

        internal static GameplayScene Gameplay(BuildContext ctx, string levelId, VolumeProfile profile, Color ambient, float ambientIntensity,
            Rect cameraBounds, Vector3 spawn, string spawnId, bool skybox)
        {
            var scene = new GameplayScene { Root = Group("--- Level ---") };
            Transform setup = Group("--- Setup ---");
            GlobalLight(setup, ambient, ambientIntensity);
            PostVolume(setup, profile);

            GameObject spawnPoint = Place(ctx, "SpawnPoint", spawn, setup, $"Spawn_{spawnId}");
            EditorAssetUtil.SetFields(spawnPoint.GetComponent<SpawnPoint>(), so => so.Require("spawnId").stringValue = spawnId);

            scene.Player = Place(ctx, "Player", spawn, null, "Player");
            scene.Rig = Place(ctx, "CameraRig", spawn, null, "CameraRig");
            scene.Rig.transform.position = Vector3.zero;
            PolygonCollider2D bounds = Bounds(setup, "CameraBounds", cameraBounds);
            scene.CameraBounds = bounds;
            var confiner = scene.Rig.GetComponentInChildren<CinemachineConfiner2D>();
            confiner.BoundingShape2D = bounds;
            var vcam = scene.Rig.GetComponentInChildren<CinemachineCamera>();
            vcam.Target.TrackingTarget = scene.Player.transform;
            vcam.transform.position = spawn + Vector3.back * 10f;
            Camera camera = scene.Rig.GetComponentInChildren<Camera>();
            camera.transform.position = spawn + Vector3.back * 10f;
            if (skybox) SkyRig(ctx, setup, camera);

            var context = setup.gameObject.AddComponent<LevelContext>();
            EditorAssetUtil.SetFields(context, so =>
            {
                so.Require("levelId").stringValue = levelId;
                so.Require("player").objectReferenceValue = scene.Player.transform;
                so.Require("defaultSpawn").objectReferenceValue = spawnPoint.GetComponent<SpawnPoint>();
                so.Require("levelBounds").objectReferenceValue = bounds;
            });

            scene.Grid = Group("Grid", scene.Root);
            scene.Grid.gameObject.AddComponent<Grid>();
            scene.Backdrop = Tilemap(scene.Grid, "Backdrop", Layers.Default, ctx.Concrete, SortingLayers.Background, 0, false, SurfaceType.Concrete, new Color(0.45f, 0.42f, 0.55f));
            scene.Ground = Tilemap(scene.Grid, "Ground", Layers.Ground, ctx.Metal, SortingLayers.Environment, 0, true, SurfaceType.Metal, Color.white);
            scene.Walls = Tilemap(scene.Grid, "Walls", Layers.Wall, ctx.Concrete, SortingLayers.Environment, 1, true, SurfaceType.Concrete, Color.white);
            return scene;
        }

        /// <summary>The 2D renderer cannot draw skyboxes: a Universal-renderer sky camera renders it into the backdrop quad.</summary>
        internal static void SkyRig(BuildContext ctx, Transform parent, Camera gameplayCamera)
        {
            RenderSettings.skybox = ctx.Skybox;
            GameObject skyCameraGo = PrefabFactory.Go("SkyCamera", parent);
            var skyCamera = skyCameraGo.AddComponent<Camera>();
            skyCamera.orthographic = false;
            skyCamera.fieldOfView = 60f;
            skyCamera.clearFlags = CameraClearFlags.Skybox;
            skyCamera.cullingMask = 0;
            skyCamera.depth = -10f;
            skyCamera.GetUniversalAdditionalCameraData().SetRenderer(RendererIndices.Universal);

            GameObject quad = PrefabFactory.Go("SkyBackdrop", parent);
            quad.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var renderer = quad.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = ctx.SkyBackdrop;
            renderer.sortingLayerName = SortingLayers.Background;
            renderer.sortingOrder = -100;

            var backdrop = PrefabFactory.Go("Sky", parent).AddComponent<SkyboxBackdrop>();
            EditorAssetUtil.SetFields(backdrop, so =>
            {
                so.Require("gameplayCamera").objectReferenceValue = gameplayCamera;
                so.Require("skyCamera").objectReferenceValue = skyCamera;
                so.Require("backdrop").objectReferenceValue = renderer;
            });
        }

        internal static Tilemap Tilemap(Transform grid, string name, int layer, Material material, string sortingLayer, int order, bool collider,
            SurfaceType surface, Color tint)
        {
            GameObject go = PrefabFactory.Go(name, grid, layer);
            var tilemap = go.AddComponent<Tilemap>();
            tilemap.color = tint;
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = order;
            if (!collider) return tilemap;

            var tileCollider = go.AddComponent<TilemapCollider2D>();
            tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            var composite = go.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            go.AddComponent<SurfaceTag>().SetSurface(surface);
            return tilemap;
        }

        /// <summary>
        /// Fills a rectangle (inclusive). Uses the batched SetTiles API: on Unity 6000.3 the single-cell
        /// SetTile(Vector3Int, TileBase) overload silently does nothing when called from batchmode editor scripts.
        /// </summary>
        internal static void Fill(Tilemap tilemap, int x0, int x1, int y0, int y1, TileBase tile, TileBase top = null)
        {
            int count = (x1 - x0 + 1) * (y1 - y0 + 1);
            if (count <= 0) return;
            var positions = new Vector3Int[count];
            var tiles = new TileBase[count];
            int i = 0;
            for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
            {
                positions[i] = new Vector3Int(x, y, 0);
                tiles[i] = y == y1 && top != null ? top : tile;
                i++;
            }

            tilemap.SetTiles(positions, tiles);
        }

        internal static void Ground(BuildContext ctx, GameplayScene scene, int x0, int x1, int topY, int depth = 4) =>
            Fill(scene.Ground, x0, x1, topY - depth, topY - 1, ctx.TileMetal, ctx.TileMetalTop);

        internal static void Spawn(BuildContext ctx, Transform parent, string id, Vector3 position)
        {
            GameObject spawn = Place(ctx, "SpawnPoint", position, parent, $"Spawn_{id}");
            EditorAssetUtil.SetFields(spawn.GetComponent<SpawnPoint>(), so => so.Require("spawnId").stringValue = id);
        }

        internal static void Parallax(BuildContext ctx, Transform parent, float fromX, float toX, float baseY)
        {
            Transform root = Group("Parallax", parent);
            (string sprite, Vector2 factor, float y, string layer, float scale, Color tint)[] layers =
            {
                ("skyline_far", new Vector2(0.85f, 0.9f), baseY + 1f, SortingLayers.Background, 2f, new Color(0.8f, 0.75f, 1f)),
                ("skyline_mid", new Vector2(0.6f, 0.7f), baseY, SortingLayers.Parallax, 2f, Color.white),
                ("skyline_near", new Vector2(0.35f, 0.45f), baseY - 1f, SortingLayers.Parallax, 2f, new Color(0.8f, 0.8f, 0.9f))
            };

            int order = 0;
            foreach (var layer in layers)
            {
                GameObject group = PrefabFactory.Go(layer.sprite, root);
                group.transform.position = new Vector3(0f, layer.y, 0f);
                for (float x = fromX; x < toX; x += 16f * layer.scale)
                {
                    GameObject piece = PrefabFactory.Go("Segment", group.transform, Layers.Default, new Vector3(x, 0f, 0f));
                    piece.transform.localScale = Vector3.one * layer.scale;
                    PrefabFactory.Sprite(piece, layer.sprite, ctx.SpriteUnlit, layer.layer, order, layer.tint);
                }

                EditorAssetUtil.SetFields(group.AddComponent<ParallaxLayer>(), so => so.Require("factor").vector2Value = layer.factor);
                order++;
            }
        }

        internal static FoliageWind Foliage(Transform parent, IEnumerable<Transform> bones)
        {
            GameObject go = PrefabFactory.Go("Wind", parent);
            var wind = go.AddComponent<FoliageWind>();
            EditorAssetUtil.SetFields(wind, so => so.SetArray("swayBones", bones.ToArray()));
            return wind;
        }


        // ---------------------------------------------------------------- Bootstrap / menu / credits

        private static void BuildBootstrap()
        {
            Scene scene = NewScene();
            GameObject camera = PrefabFactory.Go("Camera");
            var cam = camera.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;
            camera.AddComponent<AudioListener>();
            PrefabFactory.Go("Bootstrap").AddComponent<BootstrapLoader>();
            Save(scene, SceneNames.Bootstrap);
        }

        private static void BuildMainMenu(BuildContext ctx)
        {
            Scene scene = NewScene();
            RenderSettings.skybox = ctx.Skybox;
            Transform setup = Group("--- Setup ---");
            GameObject cameraGo = PrefabFactory.Go("Main Camera", setup);
            cameraGo.tag = Tags.MainCamera;
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = false;
            camera.fieldOfView = 60f;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            UniversalAdditionalCameraData menuCameraData = camera.GetUniversalAdditionalCameraData();
            menuCameraData.SetRenderer(RendererIndices.Universal);
            menuCameraData.renderPostProcessing = true;
            cameraGo.AddComponent<AudioListener>();
            cameraGo.AddComponent<SlowRotate>();
            GlobalLight(setup, new Color(0.7f, 0.6f, 1f), 0.8f);
            PostVolume(setup, ctx.MenuProfile);
            Place(ctx, "MainMenuCanvas", Vector3.zero);
            Save(scene, SceneNames.MainMenu);
        }

        private static void BuildCredits(BuildContext ctx)
        {
            Scene scene = NewScene();
            RenderSettings.skybox = ctx.Skybox;
            GameObject cameraGo = PrefabFactory.Go("Main Camera");
            cameraGo.tag = Tags.MainCamera;
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.GetUniversalAdditionalCameraData().SetRenderer(RendererIndices.Universal);
            cameraGo.AddComponent<SlowRotate>();
            cameraGo.AddComponent<AudioListener>();
            GlobalLight(null, Color.white, 1f);
            PostVolume(null, ctx.MenuProfile);
            Place(ctx, "CreditsCanvas", Vector3.zero);
            Save(scene, SceneNames.Credits);
        }

        /// <summary>LAN Operator's scene: just a camera; the console lives in UIRoot and draws what the host sends.</summary>
        private static void BuildOperatorConsole(BuildContext ctx)
        {
            Scene scene = NewScene();
            GameObject cameraGo = PrefabFactory.Go("Main Camera");
            cameraGo.tag = Tags.MainCamera;
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.01f, 0.01f, 0.03f);
            cameraGo.AddComponent<AudioListener>();
            GlobalLight(null, Color.white, 1f);
            Save(scene, SceneNames.OperatorConsole);
        }

        // ---------------------------------------------------------------- Hub

        private static void BuildHub(BuildContext ctx)
        {
            Scene scene = NewScene();
            GameplayScene hub = Gameplay(ctx, SeedContent.Ids.Hub, ctx.HubProfile, new Color(0.55f, 0.45f, 0.85f), 0.35f,
                new Rect(-16f, -2f, 32f, 13f), new Vector3(0f, 0f, 0f), "hub_center", false);

            Ground(ctx, hub, -16, 15, 0, 2);
            Fill(hub.Walls, -16, -15, 0, 10, ctx.TileBrick);
            Fill(hub.Walls, 14, 15, 0, 10, ctx.TileBrick);
            Fill(hub.Walls, -16, 15, 10, 11, ctx.TileBrick);
            Fill(hub.Backdrop, -14, 13, 0, 9, ctx.TileBrick);
            Fill(hub.Backdrop, -6, -2, 4, 7, ctx.TileGlass);

            Transform props = Group("Props", hub.Root);
            Place(ctx, "JobBoard", new Vector3(-10f, 0f), props);
            Place(ctx, "ShopTerminal", new Vector3(-6f, 0f), props);
            Place(ctx, "SkillTerminal", new Vector3(2f, 0f), props);
            GameObject travel = Place(ctx, "FastTravelTerminal", new Vector3(7f, 0f), props, "FastTravel_Levels");
            EditorAssetUtil.SetFields(travel.GetComponent<FastTravelTerminal>(), so =>
            {
                so.Require("destination").objectReferenceValue = null;
                so.SetArray("destinations", BuildContext.Fresh(ctx.StoryLevels));
            });
            GameObject showcase = Place(ctx, "FastTravelTerminal", new Vector3(11f, 0f), props, "FastTravel_Showcase");
            EditorAssetUtil.SetField(showcase.GetComponent<FastTravelTerminal>(), "destination", BuildContext.Fresh(ctx.RubricLevel));
            Place(ctx, "Crate", new Vector3(-13f, 0.5f), props);
            Place(ctx, "Crate", new Vector3(-12.6f, 1.5f), props);
            Place(ctx, "RooftopAC", new Vector3(-2f, 0f), props);
            GameObject sign = Place(ctx, "NeonSign", new Vector3(4.5f, 6.5f), props);
            sign.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            Label(props, SeedContent.Labels.HubWelcome, new Vector3(0f, 8.5f), 6f, new Color(0.3f, 1f, 1f));
            GameObject briefing = PrefabFactory.Go("DialogueZone_Briefing", props, Layers.Trigger, new Vector3(2.5f, 1f));
            briefing.AddComponent<BoxCollider2D>().size = new Vector2(1f, 3f);
            EditorAssetUtil.SetField(briefing.AddComponent<DialogueZone>(), "dialogue", BuildContext.Fresh(ctx.Briefing));

            Transform lights = Group("Lights", hub.Root);
            Light2D lampA = PrefabFactory.Light(lights, "CeilingSpot_A", Light2D.LightType.Point, new Color(1f, 0.85f, 0.6f), 1.3f, 8f, new Vector3(-8f, 9.5f), true, 35f, 70f);
            lampA.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
            Light2D lampB = PrefabFactory.Light(lights, "CeilingSpot_B", Light2D.LightType.Point, new Color(0.7f, 0.9f, 1f), 1.1f, 8f, new Vector3(6f, 9.5f), true, 35f, 70f);
            lampB.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
            Light2D window = PrefabFactory.Light(lights, "WindowGlow_Freeform", Light2D.LightType.Freeform, new Color(0.5f, 0.3f, 1f), 0.8f, 1f, new Vector3(-6f, 4f));
            window.SetShapePath(new[] { new Vector3(0f, 0f), new Vector3(5f, 0f), new Vector3(6.5f, -4f), new Vector3(-1f, -4f) });
            PrefabFactory.Light(lights, "Fill_Point", Light2D.LightType.Point, new Color(1f, 0.4f, 0.8f), 0.6f, 6f, new Vector3(0f, 3f));
            Save(scene, SceneNames.Hub);
        }

        // ---------------------------------------------------------------- Level 01

        /// <summary>
        /// Rooftops & park (6–8 min): tutorial start, park with wind, rooftop gaps, wall-jump chimney, first guard,
        /// camera gate (story door), laser tunnel, node hop to the data shard, drone arena, last gaps, exit.
        /// </summary>
        private static void BuildLevel01(BuildContext ctx)
        {
            Scene scene = NewScene();
            GameplayScene level = Gameplay(ctx, SeedContent.Ids.Level01, ctx.Level01Profile, new Color(0.55f, 0.62f, 0.95f), 0.4f,
                new Rect(-8f, -6f, 40f, 20f), new Vector3(0f, 0f), "l1_start", true);
            var layout = new LevelLayout(ctx, level, "l1", -6);

            GameObject dialogue = PrefabFactory.Go("DialogueZone_Intro", layout.Props, Layers.Trigger, layout.P(9f, 1f));
            dialogue.AddComponent<BoxCollider2D>().size = new Vector2(1f, 3f);
            EditorAssetUtil.SetField(dialogue.AddComponent<DialogueZone>(), "dialogue", BuildContext.Fresh(ctx.Level01Intro));
            Place(ctx, "NeonSign", layout.P(7f, 5f), layout.Props);
            layout.Flat(8, SeedContent.Labels.L1Move);

            // Park on the rooftop: trees, grass, wind.
            layout.Spawn("l1_park", 2f);
            var canopies = new List<Transform>();
            foreach (float x in new[] { 4f, 9.5f, 15f })
                canopies.AddRange(PrefabFactory.SwayBones(Place(ctx, "Tree", layout.P(x), layout.Props)));
            for (float x = 2f; x < 19f; x += 3f) Place(ctx, "GrassPatch", layout.P(x), layout.Props);
            FoliageWind wind = Foliage(layout.Props, canopies);
            GameObject windZone = Place(ctx, "WindZone", layout.P(12f), layout.Props);
            EditorAssetUtil.SetField(windZone.GetComponent<WindZone2D>(), "foliage", wind);
            Place(ctx, "Pickup_Credits", layout.P(9.5f, 4.5f), layout.Props);
            layout.Flat(20);

            layout.Spawn("l1_movement", 1f);
            layout.Rooftops(4, SeedContent.Labels.L1Gaps);
            layout.WallJumpChimney(10, SeedContent.Labels.L1WallJump);
            layout.Flat(4);
            layout.Checkpoint("l1_cp1");

            // Security block: guard, catwalk, camera gate with the story door, laser tunnel.
            layout.Spawn("l1_security", 1f);
            layout.Hint(SeedContent.Labels.L1Stealth, 9f, 7f, 18f);
            layout.GuardPatrol("Guard", 18, catwalk: true);
            layout.Hint(SeedContent.Labels.L1Gate, 6f, 4.2f, 12f);
            layout.CameraGate(SeedContent.Ids.TerminalLevel01Camera, SeedContent.Ids.TerminalLevel01Door, "Terminal_Camera", "Terminal_Door");
            layout.Hint(SeedContent.Labels.L1Lasers, 6f, 9f, 14f);
            layout.LaserCorridor(2, SeedContent.Ids.TerminalLevel01Lasers);

            // Network nodes: the street is blocked by a tower; the node carries you onto its roof (data shard).
            GameObject dataTerminal = Place(ctx, "Terminal_Data", layout.P(3f), layout.Props, "Terminal_Data");
            ConfigureTerminal(dataTerminal, SeedContent.Ids.TerminalLevel01Data);
            layout.Hint(SeedContent.Labels.L1Nodes, 9f, 5f, 12f);
            layout.Flat(6);
            layout.NodeHop("l1_nodes", 10, "Pickup_DataShard");
            layout.Checkpoint("l1_cp2");

            // Drone arena with A* pathfinding around pillars.
            layout.Hint(SeedContent.Labels.L1Drones, 12f, 9.5f, 20f);
            layout.DroneArena(2, "l1_drone", "Pickup_UsbKey");
            layout.Rooftops(2);
            layout.Exit(SeedContent.Ids.ExitLevel01, SeedContent.Ids.Level01, ctx.Level02);
            Rect rect = layout.Finish();
            PlaytimeReport.Add(Strings.Levels.Level01, layout);

            Parallax(ctx, level.Root, rect.xMin - 40f, rect.xMax + 40f, -2f);
            for (float x = rect.xMin + 20f; x < rect.xMax; x += 45f)
                Place(ctx.VfxPrefabs[VfxId.Rain], new Vector3(x, 20f), layout.Props, "Rain");
            Save(scene, SceneNames.Level01);
        }

        // ---------------------------------------------------------------- Level 02

        /// <summary>
        /// Office tower interior (6–8 min): lobby, patrolled hall, camera gate, laser corridor, elevator to the second
        /// storey, sniper on a balcony, turret hall, data vault (story objective), heavy guard, roof exit.
        /// </summary>
        private static void BuildLevel02(BuildContext ctx)
        {
            Scene scene = NewScene();
            GameplayScene level = Gameplay(ctx, SeedContent.Ids.Level02, ctx.Level02Profile, new Color(0.5f, 0.55f, 0.8f), 0.35f,
                new Rect(-8f, -4f, 40f, 16f), new Vector3(0f, 0f), "l2_start", false);
            var layout = new LevelLayout(ctx, level, "l2", -6, ceiling: 9);

            GameObject dialogue = PrefabFactory.Go("DialogueZone_Office", layout.Props, Layers.Trigger, layout.P(8f, 1f));
            dialogue.AddComponent<BoxCollider2D>().size = new Vector2(1f, 3f);
            EditorAssetUtil.SetField(dialogue.AddComponent<DialogueZone>(), "dialogue", BuildContext.Fresh(ctx.Level02Intro));
            Light2D window = PrefabFactory.Light(layout.Lights, "LobbyWindow_Freeform", Light2D.LightType.Freeform, new Color(0.4f, 0.5f, 1f), 0.7f, 1f, layout.P(3f, 6f));
            window.SetShapePath(new[] { new Vector3(0f, 0f), new Vector3(5f, 0f), new Vector3(6.5f, -5f), new Vector3(-1f, -5f) });
            layout.Flat(12, SeedContent.Labels.L2Lobby);

            layout.Spawn("l2_security", 1f);
            layout.GuardPatrol("Guard", 18, catwalk: true);
            layout.CameraGate(SeedContent.Ids.TerminalLevel02Camera, SeedContent.Ids.TerminalLevel02Door, doorDifficulty: 2);
            layout.LaserCorridor(3, SeedContent.Ids.TerminalLevel02Lasers);
            layout.Checkpoint("l2_cp1");

            layout.Hint(SeedContent.Labels.L2Elevator, 4f, 4.5f, 12f);
            layout.ElevatorUp(SeedContent.Ids.TerminalLevel02Elevator, 9, "l2_elevator");

            layout.Spawn("l2_upper", 1f);
            layout.Hint(SeedContent.Labels.L2Sniper, 10f, 7.5f, 16f);
            layout.GuardPatrol("Guard_Sniper", 20, catwalk: true);
            layout.Hint(SeedContent.Labels.L2Turrets, 11f, 7.5f, 16f);
            layout.TurretHall(2);
            layout.Flat(8);
            layout.Checkpoint("l2_cp2", "l2_vault");
            layout.DataVault(SeedContent.Ids.TerminalLevel02Vault);
            layout.GuardPatrol("Guard_Heavy", 16, catwalk: false);
            layout.DroneArena(1);
            layout.Exit(SeedContent.Ids.ExitLevel02, SeedContent.Ids.Level02, ctx.Level03);
            layout.Finish();
            PlaytimeReport.Add(Strings.Levels.Level02, layout);
            Save(scene, SceneNames.Level02);
        }

        // ---------------------------------------------------------------- Levels 03–05

        /// <summary>Lobby of an interior level: intro dialogue trigger, tutorial hint, decor.</summary>
        private static void IntroRoom(LevelLayout layout, DialogueDefinition dialogue, string hint, string decor, int width = 12)
        {
            GameObject zone = PrefabFactory.Go("DialogueZone_Intro", layout.Props, Layers.Trigger, layout.P(7f, 1f));
            zone.AddComponent<BoxCollider2D>().size = new Vector2(1f, 3f);
            EditorAssetUtil.SetField(zone.AddComponent<DialogueZone>(), "dialogue", BuildContext.Fresh(dialogue));
            if (decor != null) layout.Dress(decor, 2, width - 1, 3, decor == "SewerPipe" ? 6.5f : 0f);
            layout.Flat(width, hint);
        }

        /// <summary>
        /// Server farm (6–7 min): rack halls patrolled by drones, a long laser corridor, a chain of server towers crossed
        /// node to node, a hard camera gate, a heavy guard and the server core (story hack), then the turret hall.
        /// </summary>
        private static void BuildLevel03(BuildContext ctx)
        {
            Scene scene = NewScene();
            GameplayScene level = Gameplay(ctx, SeedContent.Ids.Level03, ctx.Level03Profile, new Color(0.35f, 0.55f, 0.9f), 0.32f,
                new Rect(-8f, -2f, 40f, 16f), new Vector3(0f, 0f), "l3_start", false);
            var layout = new LevelLayout(ctx, level, "l3", -6, ceiling: 11);
            IntroRoom(layout, ctx.Level03Intro, SeedContent.Labels.L3Start, "ServerRack");

            layout.Dress("ServerRack", 2, 22, 4);
            layout.DroneArena(2, "l3_drones");
            layout.LaserCorridor(4, "l3_lasers");
            layout.Checkpoint("l3_cp1");
            layout.Hint(SeedContent.Labels.L3Chain, 6f, 5f, 14f);
            layout.NodeChain(3, "l3_nodes");
            layout.CameraGate("l3_gate_camera", "l3_gate_door", doorDifficulty: 3);
            layout.Dress("ServerRack", 1, 17, 5);
            layout.GuardPatrol("Guard_Heavy", 18, catwalk: true);
            layout.Dress("ServerRack", 2, 22, 4);
            layout.DroneArena(3);
            layout.Checkpoint("l3_cp2", "l3_core");
            layout.DataVault(SeedContent.Ids.TerminalLevel03Core, "Terminal_Core", 3);
            layout.TurretHall(2);
            layout.Exit(SeedContent.Ids.ExitLevel03, SeedContent.Ids.Level03, ctx.Level04);
            layout.Finish();
            PlaytimeReport.Add(Strings.Levels.Level03, layout);
            Save(scene, SceneNames.Level03);
        }

        /// <summary>
        /// Sewers (6–7 min): swimming basins with floating crates, walls that force a dive, the sluice puzzle (flood the
        /// shaft to reach the ledge), lasers, drones and a guarded gate under the HQ foundations.
        /// </summary>
        private static void BuildLevel04(BuildContext ctx)
        {
            Scene scene = NewScene();
            GameplayScene level = Gameplay(ctx, SeedContent.Ids.Level04, ctx.Level04Profile, new Color(0.4f, 0.7f, 0.55f), 0.32f,
                new Rect(-8f, -2f, 40f, 16f), new Vector3(0f, 0f), "l4_start", false);
            var layout = new LevelLayout(ctx, level, "l4", -6, ceiling: 10);
            IntroRoom(layout, ctx.Level04Intro, SeedContent.Labels.L4Start, "SewerPipe");

            layout.Spawn("l4_water", -3f);
            layout.WaterPool(12, 4, dive: false, crates: 3);
            layout.GuardPatrol("Guard", 16, catwalk: true);
            layout.Hint(SeedContent.Labels.L4Dive, 8f, 6f, 14f);
            layout.WaterPool(16, 5, dive: true, crates: 1);
            layout.Checkpoint("l4_cp1");
            layout.Hint(SeedContent.Labels.L4Sluice, 3f, 4.5f, 12f);
            layout.FloodShaft(SeedContent.Ids.TerminalLevel04Sluice, "l4_sluice");
            layout.Dress("SewerPipe", 0, 20, 8, 6.5f);
            layout.LaserCorridor(3, "l4_lasers");
            layout.DroneArena(2);
            layout.WaterPool(14, 4, dive: true, crates: 2);
            layout.Checkpoint("l4_cp2", "l4_gate");
            layout.CameraGate("l4_gate_camera", "l4_gate_door", doorDifficulty: 3);
            layout.GuardPatrol("Guard_Heavy", 16, catwalk: false);
            layout.Exit(SeedContent.Ids.ExitLevel04, SeedContent.Ids.Level04, ctx.Level05);
            layout.Finish();
            PlaytimeReport.Add(Strings.Levels.Level04, layout);
            Save(scene, SceneNames.Level04);
        }

        /// <summary>
        /// Corporate HQ, the finale (7–8 min): every mechanic in sequence (gate, heavy guard, lasers, executive elevator,
        /// sniper, drones, turrets, node chain, mainframe hack) and a rooftop exit that rolls the credits.
        /// </summary>
        private static void BuildLevel05(BuildContext ctx)
        {
            Scene scene = NewScene();
            GameplayScene level = Gameplay(ctx, SeedContent.Ids.Level05, ctx.Level05Profile, new Color(0.75f, 0.55f, 0.5f), 0.34f,
                new Rect(-8f, -2f, 40f, 16f), new Vector3(0f, 0f), "l5_start", false);
            var layout = new LevelLayout(ctx, level, "l5", -6, ceiling: 10);
            IntroRoom(layout, ctx.Level05Intro, SeedContent.Labels.L5Start, null);

            layout.CameraGate("l5_gate_camera", "l5_gate_door", doorDifficulty: 3);
            layout.Spawn("l5_security", 1f);
            layout.GuardPatrol("Guard_Heavy", 18, catwalk: true);
            layout.LaserCorridor(4, "l5_lasers", 0.45f);
            layout.Checkpoint("l5_cp1");
            layout.ElevatorUp(SeedContent.Ids.TerminalLevel05Elevator, 10, "l5_elevator");
            layout.GuardPatrol("Guard_Sniper", 20, catwalk: true);
            layout.DroneArena(3, "l5_drones");
            layout.TurretHall(2);
            layout.Flat(8);
            layout.Checkpoint("l5_cp2");
            layout.Dress("ServerRack", 1, 6, 4);
            layout.NodeChain(2, "l5_nodes");
            layout.Hint(SeedContent.Labels.L5Mainframe, 6f, 5f, 12f);
            layout.DataVault(SeedContent.Ids.TerminalLevel05Mainframe, "Terminal_Mainframe", 4);
            layout.GuardPatrol("Guard_Heavy", 16, catwalk: false);
            layout.Exit(SeedContent.Ids.ExitLevel05, SeedContent.Ids.Level05, null, SceneNames.Finale);
            layout.Finish();
            PlaytimeReport.Add(Strings.Levels.Level05, layout);
            Save(scene, SceneNames.Level05);
        }

        internal static void ConfigureTerminal(GameObject terminal, string id, params MonoBehaviour[] targets)
        {
            EditorAssetUtil.SetFields(terminal.GetComponent<Terminal>(), so =>
            {
                so.Require("terminalId").stringValue = id;
                so.SetArray("targets", targets);
            });
        }

        internal static void LinkNodes(GameObject a, GameObject b, bool requiresHack)
        {
            EditorAssetUtil.SetFields(a.GetComponent<NetworkNode>(), so =>
            {
                so.Require("linkedNode").objectReferenceValue = b.GetComponent<NetworkNode>();
                so.Require("requiresHack").boolValue = requiresHack;
            });
            EditorAssetUtil.SetFields(b.GetComponent<NetworkNode>(), so =>
            {
                so.Require("linkedNode").objectReferenceValue = a.GetComponent<NetworkNode>();
                so.Require("requiresHack").boolValue = requiresHack;
            });
        }

        // ---------------------------------------------------------------- Rubric showcase

        private const int RoomCount = 14;
        private const int RoomWidth = 20;
        private const int RoomStride = 24;

        private static void BuildRubricShowcase(BuildContext ctx)
        {
            Scene scene = NewScene();
            GameplayScene showcase = Gameplay(ctx, "rubric", ctx.ShowcaseProfile, new Color(0.7f, 0.7f, 0.9f), 0.55f,
                new Rect(-2f, -3f, RoomWidth + 4f, 15f), new Vector3(5f, 0f), "showcase_start", false);
            string[] titles =
            {
                Strings.Rubric.Movement, Strings.Rubric.Materials, Strings.Rubric.Environment, Strings.Rubric.Water, Strings.Rubric.Lighting,
                Strings.Rubric.Prefabs, Strings.Rubric.Particles, Strings.Rubric.Menu, Strings.Rubric.Sound, Strings.Rubric.Animation,
                Strings.Rubric.Collisions, Strings.Rubric.Scripts, Strings.Rubric.Cinematics, Strings.Rubric.Multiplayer
            };
            string[] summaries =
            {
                SeedContent.Rubric.Movement, SeedContent.Rubric.Materials, SeedContent.Rubric.Environment, SeedContent.Rubric.Water,
                SeedContent.Rubric.Lighting, SeedContent.Rubric.Prefabs, SeedContent.Rubric.Particles, SeedContent.Rubric.Menu,
                SeedContent.Rubric.Sound, SeedContent.Rubric.Animation, SeedContent.Rubric.Collisions, SeedContent.Rubric.Scripts,
                SeedContent.Rubric.Cinematics, SeedContent.Rubric.Multiplayer
            };

            var entryPads = new GameObject[RoomCount];
            var exitPads = new GameObject[RoomCount];
            for (int i = 0; i < RoomCount; i++)
            {
                int x0 = i * RoomStride;
                Transform room = Group($"Room_{i + 1:00}", showcase.Root);
                bool waterRoom = i == 3;
                if (waterRoom)
                {
                    Ground(ctx, showcase, x0, x0 + 3, 0, 6);
                    Ground(ctx, showcase, x0 + 16, x0 + RoomWidth, 0, 6);
                    Fill(showcase.Ground, x0, x0 + RoomWidth, -8, -6, ctx.TileMetal);
                }
                else
                {
                    Ground(ctx, showcase, x0, x0 + RoomWidth, 0);
                }

                Fill(showcase.Walls, x0 - 1, x0 - 1, 0, 11, ctx.TileBrick);
                Fill(showcase.Walls, x0 + RoomWidth + 1, x0 + RoomWidth + 1, 0, 11, ctx.TileBrick);
                Fill(showcase.Backdrop, x0, x0 + RoomWidth, 0, 11, ctx.TileBrick);
                Label(room, $"{titles[i]}", new Vector3(x0 + 10.5f, 6.6f), 6f, new Color(0.3f, 1f, 1f), 18f);
                Label(room, summaries[i], new Vector3(x0 + 10.5f, 5.75f), 3f, new Color(0.9f, 0.9f, 1f), 17f);
                Label(room, SeedContent.Labels.ShowcaseHint, new Vector3(x0 + 10.5f, 4.95f), 2.4f, new Color(0.6f, 0.65f, 0.8f), 17f);
                Spawn(ctx, room, $"room_{i + 1:00}", new Vector3(x0 + 5f, 0f));
                entryPads[i] = Place(ctx, "TeleportPad", new Vector3(x0 + 2f, 0f), room, "Pad_Entry");
                exitPads[i] = Place(ctx, "TeleportPad", new Vector3(x0 + RoomWidth - 1.5f, 0f), room, "Pad_Exit");

                GameObject zone = PrefabFactory.Go("CameraZone", room, Layers.Trigger, new Vector3(x0 + 10.5f, 5f));
                zone.AddComponent<BoxCollider2D>().size = new Vector2(RoomWidth, 12f);
                PolygonCollider2D bounds = Bounds(room, "RoomBounds", new Rect(x0 - 2f, -3f - (waterRoom ? 6f : 0f), RoomWidth + 4f, 15f + (waterRoom ? 6f : 0f)));
                EditorAssetUtil.SetFields(zone.AddComponent<CameraZone>(), so => so.Require("confinerShape").objectReferenceValue = bounds);

                PopulateRoom(ctx, i, room, x0, showcase);
            }

            for (int i = 0; i < RoomCount; i++)
            {
                TeleportPad exit = exitPads[i].GetComponent<TeleportPad>();
                TeleportPad nextEntry = entryPads[(i + 1) % RoomCount].GetComponent<TeleportPad>();
                TeleportPad previousExit = exitPads[(i + RoomCount - 1) % RoomCount].GetComponent<TeleportPad>();
                EditorAssetUtil.SetField(exit, "destination", nextEntry);
                EditorAssetUtil.SetField(entryPads[i].GetComponent<TeleportPad>(), "destination", previousExit);
            }

            float width = RoomCount * RoomStride;
            GameObject pathfinding = PrefabFactory.Go("Pathfinding", showcase.Root, Layers.Default, new Vector3(width * 0.5f - 2f, 4f));
            EditorAssetUtil.SetFields(pathfinding.AddComponent<PathfindingGrid>(), so => so.Require("size").vector2Value = new Vector2(width + 4f, 18f));
            pathfinding.AddComponent<PathRequestQueue>();
            Save(scene, SceneNames.RubricShowcase);
        }

        private static void PopulateRoom(BuildContext ctx, int index, Transform room, int x0, GameplayScene scene)
        {
            Vector3 P(float x, float y) => new(x0 + x, y, 0f);
            switch (index)
            {
                case 0:
                    Fill(scene.Ground, x0 + 7, x0 + 9, 3, 3, ctx.TileMetalTop);
                    Fill(scene.Walls, x0 + 12, x0 + 12, 2, 8, ctx.TileConcrete);
                    Fill(scene.Walls, x0 + 15, x0 + 15, 0, 8, ctx.TileConcrete);
                    break;
                case 1:
                    Fill(scene.Ground, x0 + 5, x0 + 6, 2, 3, ctx.TileMetal);
                    Fill(scene.Walls, x0 + 8, x0 + 9, 2, 3, ctx.TileConcrete);
                    Fill(scene.Backdrop, x0 + 11, x0 + 12, 2, 3, ctx.TileGlass);
                    Place(ctx, "NeonSign", P(15f, 3f), room);
                    GameObject holo = Place(ctx, "NetworkNode", P(11.5f, 0f), room);
                    EditorAssetUtil.SetFields(holo.GetComponent<NetworkNode>(), so => so.Require("requiresHack").boolValue = false);
                    GameObject dissolve = Place(ctx, "Terminal", P(17f, 0f), room);
                    dissolve.AddComponent<DissolveDemoLoop>();
                    PrefabFactory.Light(room, "MovingKeyLight", Light2D.LightType.Point, new Color(1f, 0.9f, 0.8f), 1.4f, 5f, P(7f, 4f));
                    break;
                case 2:
                    var canopies = new List<Transform>();
                    canopies.AddRange(PrefabFactory.SwayBones(Place(ctx, "Tree", P(6f, 0f), room)));
                    canopies.AddRange(PrefabFactory.SwayBones(Place(ctx, "Tree", P(13f, 0f), room)));
                    for (float x = 3f; x < 18f; x += 3f) Place(ctx, "GrassPatch", P(x, 0f), room);
                    FoliageWind wind = Foliage(room, canopies);
                    GameObject windZone = Place(ctx, "WindZone", P(9f, 0f), room);
                    EditorAssetUtil.SetField(windZone.GetComponent<WindZone2D>(), "foliage", wind);
                    break;
                case 3:
                    GameObject water = Place(ctx, "WaterBody", P(10f, -2.5f), room);
                    EditorAssetUtil.SetFields(water.GetComponent<WaterBody2D>(), so =>
                    {
                        so.Require("width").floatValue = 12f;
                        so.Require("depth").floatValue = 5f;
                    });
                    water.GetComponent<BoxCollider2D>().size = new Vector2(12f, 5f);
                    water.GetComponent<BuoyancyEffector2D>().surfaceLevel = 2.5f;
                    foreach (float x in new[] { 7f, 10f, 13f }) Place(ctx, "Crate", P(x, 1f), room);
                    break;
                case 4:
                    PrefabFactory.Light(room, "Point", Light2D.LightType.Point, new Color(0.3f, 0.9f, 1f), 1.3f, 5f, P(4f, 3f), true);
                    Light2D spot = PrefabFactory.Light(room, "Spot", Light2D.LightType.Point, new Color(1f, 0.85f, 0.5f), 1.5f, 8f, P(10f, 8f), true, 25f, 50f);
                    spot.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
                    Light2D freeform = PrefabFactory.Light(room, "Freeform", Light2D.LightType.Freeform, new Color(1f, 0.3f, 0.8f), 0.9f, 1f, P(14f, 1f));
                    freeform.SetShapePath(new[] { new Vector3(0f, 0f), new Vector3(4f, 0f), new Vector3(3f, 4f), new Vector3(1f, 4f) });
                    Place(ctx, "NeonSign", P(16f, 5f), room);
                    Place(ctx, "Crate", P(7f, 0.5f), room);
                    Place(ctx, "Crate", P(11f, 0.5f), room);
                    Place(ctx, "RooftopAC", P(5f, 0f), room);
                    break;
                case 5:
                    Place(ctx, "Guard", P(5f, 0f), room);
                    Place(ctx, "Guard_Heavy", P(8f, 0f), room);
                    Place(ctx, "Guard_Sniper", P(11f, 0f), room);
                    Place(ctx, "Terminal_Door", P(13.5f, 0f), room);
                    Place(ctx, "Terminal_Camera", P(15f, 0f), room);
                    Place(ctx, "Terminal_Data", P(16.5f, 0f), room);
                    GameObject elevator = Place(ctx, "Elevator", P(4f, 4f), room);
                    Transform a = PrefabFactory.Go("Elevator_A", room, Layers.Default, P(4f, 4f)).transform;
                    Transform b = PrefabFactory.Go("Elevator_B", room, Layers.Default, P(4f, 8f)).transform;
                    EditorAssetUtil.SetFields(elevator.GetComponent<Elevator>(), so =>
                    {
                        so.Require("pointA").objectReferenceValue = a;
                        so.Require("pointB").objectReferenceValue = b;
                        so.Require("locked").boolValue = false;
                    });
                    break;
                case 6:
                    PrefabFactory.Go("VfxDemo", room, Layers.Default, P(8f, 2f)).AddComponent<VfxDemoEmitter>();
                    Place(ctx.VfxPrefabs[VfxId.Steam], P(13f, 0f), room);
                    GameObject rain = Place(ctx.VfxPrefabs[VfxId.Rain], P(10f, 12f), room);
                    rain.transform.localScale = new Vector3(0.5f, 1f, 1f);
                    GameObject nodeA = Place(ctx, "NetworkNode", P(5f, 0f), room);
                    GameObject nodeB = Place(ctx, "NetworkNode", P(15f, 4f), room);
                    Fill(scene.Ground, x0 + 13, x0 + 17, 3, 3, ctx.TileMetalTop);
                    LinkNodes(nodeA, nodeB, false);
                    break;
                case 8:
                    Place(ctx, "AlarmZone", P(9f, 0f), room);
                    GameObject glass = PrefabFactory.Go("GlassFloor", room, Layers.Ground, P(13f, 0.1f));
                    glass.AddComponent<BoxCollider2D>().size = new Vector2(3f, 0.2f);
                    glass.AddComponent<SurfaceTag>().SetSurface(SurfaceType.Glass);
                    PrefabFactory.Sprite(glass, "white", ctx.Glass, SortingLayers.Interactables, 0, new Color(0.6f, 0.9f, 1f, 0.6f)).transform.localScale = new Vector3(12f, 0.8f, 1f);
                    GameObject grass = PrefabFactory.Go("GrassFloor", room, Layers.Ground, P(5f, 0.1f));
                    grass.AddComponent<BoxCollider2D>().size = new Vector2(3f, 0.2f);
                    grass.AddComponent<SurfaceTag>().SetSurface(SurfaceType.Grass);
                    Place(ctx, "GrassPatch", P(5f, 0.1f), room);
                    break;
                case 9:
                    Place(ctx, "Drone", P(6f, 5f), room);
                    Place(ctx, "SecurityCamera", P(10f, 7f), room);
                    GameObject door = Place(ctx, "Door", P(14.5f, 0f), room);
                    PrefabFactory.Go("DoorDemo", room).AddComponent<PeriodicHackTrigger>();
                    EditorAssetUtil.SetFields(room.Find("DoorDemo").GetComponent<PeriodicHackTrigger>(), so => so.SetArray("targets", new MonoBehaviour[] { door.GetComponent<Door>() }));
                    break;
                case 10:
                    Place(ctx, "Hazard_Laser", P(8f, 0f), room);
                    Place(ctx, "Hazard_ElectricFloor", P(12f, 0.1f), room);
                    Place(ctx, "Pickup_Credits", P(15f, 0f), room);
                    Place(ctx, "Crate", P(5f, 0.5f), room);
                    Place(ctx, "Checkpoint", P(17f, 0f), room);
                    break;
                case 11:
                    Fill(scene.Walls, x0 + 8, x0 + 8, 1, 5, ctx.TileConcrete);
                    Fill(scene.Walls, x0 + 12, x0 + 12, 4, 9, ctx.TileConcrete);
                    Place(ctx, "Drone", P(15f, 6f), room);
                    Place(ctx, "Turret", P(17f, 0f), room);
                    break;
                case 13:
                    Place(ctx, "Door", P(14.5f, 0f), room, "CoopDemoDoor");
                    Place(ctx, "SecurityCamera", P(10f, 7f), room, "CoopDemoCamera");
                    Label(room, SeedContent.Labels.CoopRoomHint, P(10.5f, 3.6f), 2.6f, new Color(1f, 0.85f, 0.4f), 17f);
                    break;
            }
        }
    }
}
