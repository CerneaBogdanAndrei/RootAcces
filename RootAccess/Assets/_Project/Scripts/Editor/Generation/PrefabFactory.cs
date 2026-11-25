using System;
using RootAccess.AI;
using RootAccess.Audio;
using RootAccess.Core;
using RootAccess.Gameplay;
using RootAccess.Input;
using RootAccess.Player;
using RootAccess.Systems;
using RootAccess.VFX;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace RootAccess.EditorTools
{
    /// <summary>Builds every reusable gameplay prefab (and its variants) plus the persistent SystemsRoot.</summary>
    public static class PrefabFactory
    {
        public static void BuildAll(BuildContext ctx)
        {
            BuildPlayer(ctx);
            BuildEnemies(ctx);
            BuildInteractables(ctx);
            BuildEnvironment(ctx);
            BuildCameraRig(ctx);
            BuildSystemsRoot(ctx);
            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------------- helpers

        public static GameObject Go(string name, Transform parent = null, int layer = Layers.Default, Vector3? local = null)
        {
            var go = new GameObject(name) { layer = layer };
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = local ?? Vector3.zero;
            return go;
        }

        public static SpriteRenderer Sprite(GameObject go, string sprite, Material material, string sortingLayer, int order = 0, Color? color = null)
        {
            if (!go.TryGetComponent(out SpriteRenderer renderer)) renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = GeneratedArt.Sprite(sprite);
            renderer.sharedMaterial = material;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = order;
            renderer.color = color ?? Color.white;
            return renderer;
        }

        public static Light2D Light(Transform parent, string name, Light2D.LightType type, Color color, float intensity, float outerRadius,
            Vector3? local = null, bool shadows = false, float innerAngle = 360f, float outerAngle = 360f)
        {
            GameObject go = Go(name, parent, parent != null ? parent.gameObject.layer : Layers.Default, local);
            var light = go.AddComponent<Light2D>();
            light.lightType = type;
            light.color = color;
            light.intensity = intensity;
            if (type == Light2D.LightType.Point)
            {
                light.pointLightOuterRadius = outerRadius;
                light.pointLightInnerRadius = outerRadius * 0.15f;
                light.pointLightInnerAngle = innerAngle;
                light.pointLightOuterAngle = outerAngle;
            }

            light.shadowsEnabled = shadows;
            light.shadowIntensity = 0.75f;
            light.falloffIntensity = 0.6f;
            EditorAssetUtil.SetFields(light, so =>
            {
                so.Require("m_NormalMapQuality").intValue = (int)Light2D.NormalMapQuality.Accurate;
                so.Require("m_NormalMapDistance").floatValue = 2.5f;
            });
            return light;
        }

        public static void ShadowCaster(GameObject go)
        {
            var caster = go.AddComponent<ShadowCaster2D>();
            caster.selfShadows = false;
            caster.castsShadows = true;
        }

        private static GameObject Save(BuildContext ctx, GameObject go, string folder)
        {
            string path = $"{folder}/{go.name}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            ctx.Prefabs[go.name] = prefab;
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject Variant(BuildContext ctx, GameObject basePrefab, string name, string folder, Action<GameObject> modify)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            instance.name = name;
            modify(instance);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, $"{folder}/{name}.prefab");
            Object.DestroyImmediate(instance);
            ctx.Prefabs[name] = prefab;
            return prefab;
        }

        private static T Box<T>(GameObject go, Vector2 size, Vector2 offset, bool trigger) where T : Collider2D
        {
            var collider = go.AddComponent<T>();
            collider.isTrigger = trigger;
            collider.offset = offset;
            switch (collider)
            {
                case BoxCollider2D box:
                    box.size = size;
                    break;
                case CapsuleCollider2D capsule:
                    capsule.size = size;
                    break;
                case CircleCollider2D circle:
                    circle.radius = size.x * 0.5f;
                    break;
            }

            return collider;
        }

        // ---------------------------------------------------------------- player

        /// <summary>Pooled EMP charge thrown by the player (see PlayerGrenadeThrower / EmpGrenade).</summary>
        private static GameObject BuildEmpGrenade(BuildContext ctx)
        {
            GameObject grenade = Go("EmpGrenade", null, Layers.Default);
            var body = grenade.AddComponent<Rigidbody2D>();
            body.gravityScale = 1.6f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.angularDamping = 0.5f;
            Box<CircleCollider2D>(grenade, new Vector2(0.36f, 0.36f), Vector2.zero, false);
            Sprite(grenade, "pickup_emp", ctx.SpriteUnlit, SortingLayers.VFX, 2);
            grenade.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            Light(grenade.transform, "Glow", Light2D.LightType.Point, new Color(0.4f, 0.8f, 1f), 0.9f, 1.6f);
            grenade.AddComponent<EmpGrenade>();
            return Save(ctx, grenade, EditorPaths.PrefabsPlayer);
        }

        private static void BuildPlayer(BuildContext ctx)
        {
            GameObject grenadePrefab = BuildEmpGrenade(ctx);
            GameObject root = Go("Player", null, Layers.Player);
            root.tag = Tags.Player;
            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.sharedMaterial = ctx.NoFriction;
            Box<CapsuleCollider2D>(root, new Vector2(0.55f, 1.3f), new Vector2(0f, 0.65f), false).sharedMaterial = ctx.NoFriction;

            GameObject visual = Go("Visual", root.transform, Layers.Player);
            SpriteRenderer sprite = Sprite(visual, "player_idle_0", ctx.Dissolve, SortingLayers.Characters, 10);
            var animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = ctx.PlayerAnimator;
            var bridge = visual.AddComponent<PlayerAnimatorBridge>();
            var footsteps = visual.AddComponent<FootstepPlayer>();
            EditorAssetUtil.SetFields(footsteps, so =>
            {
                so.Require("feet").objectReferenceValue = root.transform;
                so.Require("silencedBySkill").boolValue = true;
            });

            Light(root.transform, "PlayerGlow", Light2D.LightType.Point, new Color(0.3f, 0.9f, 1f), 0.5f, 2.4f, new Vector3(0f, 1f, 0f));

            var dissolve = root.AddComponent<DissolveController>();
            EditorAssetUtil.SetFields(dissolve, so => so.SetArray("renderers", new Renderer[] { sprite }));
            var controller = root.AddComponent<PlayerController>();
            var visibility = root.AddComponent<PlayerVisibility>();
            EditorAssetUtil.SetField(visibility, "tintRenderer", sprite);
            EditorAssetUtil.SetFields(controller, so =>
            {
                so.Require("input").objectReferenceValue = ctx.Input;
                so.Require("config").objectReferenceValue = ctx.PlayerConfig;
                so.Require("dissolve").objectReferenceValue = dissolve;
                so.Require("visibility").objectReferenceValue = visibility;
            });
            var health = root.AddComponent<PlayerHealth>();
            EditorAssetUtil.SetField(health, "flashRenderer", sprite);
            var interactor = root.AddComponent<PlayerInteractor>();
            EditorAssetUtil.SetField(interactor, "input", ctx.Input);
            var thrower = root.AddComponent<PlayerGrenadeThrower>();
            EditorAssetUtil.SetFields(thrower, so =>
            {
                so.Require("input").objectReferenceValue = ctx.Input;
                so.Require("grenadePrefab").objectReferenceValue = grenadePrefab;
                so.Require("grenadeItem").objectReferenceValue = ctx.EmpGrenade;
            });
            EditorAssetUtil.SetFields(bridge, so =>
            {
                so.Require("controller").objectReferenceValue = controller;
                so.Require("spriteRenderer").objectReferenceValue = sprite;
            });
            Save(ctx, root, EditorPaths.PrefabsPlayer);
        }

        // ---------------------------------------------------------------- enemies

        private static VisionCone Vision(BuildContext ctx, Transform parent, Vector3 local, string sortingLayer = SortingLayers.Interactables)
        {
            GameObject go = Go("VisionCone", parent, parent.gameObject.layer, local);
            var filter = go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = ctx.VisionCone;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = -5;
            var vision = go.AddComponent<VisionCone>();
            EditorAssetUtil.SetField(vision, "meshFilter", filter);
            return vision;
        }

        private static void BuildEnemies(BuildContext ctx)
        {
            GameObject guard = Go("Guard", null, Layers.Enemy);
            guard.tag = Tags.Enemy;
            var gBody = guard.AddComponent<Rigidbody2D>();
            gBody.freezeRotation = true;
            gBody.gravityScale = 3f;
            gBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            gBody.sharedMaterial = ctx.NoFriction;
            Box<CapsuleCollider2D>(guard, new Vector2(0.6f, 1.35f), new Vector2(0f, 0.68f), false);
            GameObject gVisual = Go("Visual", guard.transform, Layers.Enemy);
            SpriteRenderer gSprite = Sprite(gVisual, "guard_idle_0", ctx.SpriteLit, SortingLayers.Characters, 5);
            var gAnimator = gVisual.AddComponent<Animator>();
            gAnimator.runtimeAnimatorController = ctx.GuardAnimator;
            gVisual.AddComponent<GuardAnimationEvents>();
            var gSteps = gVisual.AddComponent<FootstepPlayer>();
            EditorAssetUtil.SetField(gSteps, "feet", guard.transform);
            VisionCone gVision = Vision(ctx, guard.transform, new Vector3(0f, 1.2f, 0f));
            Light2D flashlight = Light(gVision.transform, "Flashlight", Light2D.LightType.Point, new Color(1f, 0.95f, 0.7f), 0.9f, 6f, null, true, 30f, 60f);
            flashlight.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            GameObject icon = Go("AlertIcon", guard.transform, Layers.Enemy, new Vector3(0f, 1.9f, 0f));
            SpriteRenderer iconSprite = Sprite(icon, "alert_icon", ctx.SpriteUnlit, SortingLayers.VFX);
            iconSprite.enabled = false;
            var g = guard.AddComponent<Guard>();
            EditorAssetUtil.SetFields(g, so =>
            {
                so.Require("config").objectReferenceValue = ctx.GuardBase;
                so.Require("vision").objectReferenceValue = gVision;
                so.Require("animator").objectReferenceValue = gAnimator;
                so.Require("body").objectReferenceValue = gSprite;
                so.Require("alertIcon").objectReferenceValue = iconSprite;
            });
            GameObject guardPrefab = Save(ctx, guard, EditorPaths.PrefabsEnemies);

            Variant(ctx, guardPrefab, "Guard_Heavy", EditorPaths.PrefabsEnemies, v =>
            {
                EditorAssetUtil.SetField(v.GetComponent<Guard>(), "config", ctx.GuardHeavy);
                v.transform.localScale = new Vector3(1.18f, 1.18f, 1f);
                v.transform.Find("Visual").GetComponent<SpriteRenderer>().color = new Color(1f, 0.75f, 0.7f);
            });
            Variant(ctx, guardPrefab, "Guard_Sniper", EditorPaths.PrefabsEnemies, v =>
            {
                EditorAssetUtil.SetField(v.GetComponent<Guard>(), "config", ctx.GuardSniper);
                v.transform.Find("Visual").GetComponent<SpriteRenderer>().color = new Color(0.75f, 0.85f, 1f);
            });

            GameObject camera = Go("SecurityCamera", null, Layers.Enemy);
            GameObject pivot = Go("Pivot", camera.transform, Layers.Enemy);
            Sprite(pivot, "camera_body", ctx.SpriteLit, SortingLayers.Interactables, 4);
            VisionCone cVision = Vision(ctx, pivot.transform, new Vector3(0.75f, 0f, 0f));
            Light2D status = Light(pivot.transform, "StatusLight", Light2D.LightType.Point, new Color(0.3f, 1f, 0.5f), 1.2f, 0.9f, new Vector3(0.6f, 0.15f, 0f));
            var cAnimator = camera.AddComponent<Animator>();
            cAnimator.runtimeAnimatorController = ctx.CameraAnimator;
            var sc = camera.AddComponent<SecurityCamera>();
            EditorAssetUtil.SetFields(sc, so =>
            {
                so.Require("vision").objectReferenceValue = cVision;
                so.Require("pivot").objectReferenceValue = pivot.transform;
                so.Require("statusLight").objectReferenceValue = status;
                so.Require("animator").objectReferenceValue = cAnimator;
            });
            EditorAssetUtil.SetFields(cVision, so => { so.Require("viewDistance").floatValue = 8f; so.Require("viewAngle").floatValue = 38f; });
            Save(ctx, camera, EditorPaths.PrefabsEnemies);

            GameObject drone = Go("Drone", null, Layers.Enemy);
            drone.tag = Tags.Enemy;
            var dBody = drone.AddComponent<Rigidbody2D>();
            dBody.gravityScale = 0f;
            dBody.freezeRotation = true;
            dBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            Box<CircleCollider2D>(drone, new Vector2(0.8f, 0.8f), Vector2.zero, false);
            GameObject dVisual = Go("Visual", drone.transform, Layers.Enemy);
            Sprite(dVisual, "drone_0", ctx.SpriteLit, SortingLayers.Characters, 6);
            dVisual.AddComponent<Animator>().runtimeAnimatorController = ctx.DroneAnimator;
            GameObject eye = Go("Eye", drone.transform, Layers.Enemy, new Vector3(0f, -0.05f, 0f));
            SpriteRenderer eyeSprite = Sprite(eye, "drone_eye", ctx.SpriteUnlit, SortingLayers.Characters, 7);
            Light(drone.transform, "DroneLight", Light2D.LightType.Point, new Color(1f, 0.3f, 0.6f), 0.8f, 2f);
            var d = drone.AddComponent<Drone>();
            EditorAssetUtil.SetField(d, "eye", eyeSprite);
            Save(ctx, drone, EditorPaths.PrefabsEnemies);

            GameObject projectile = Go("Projectile", null, Layers.Projectile);
            var pBody = projectile.AddComponent<Rigidbody2D>();
            pBody.gravityScale = 0f;
            pBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Box<CircleCollider2D>(projectile, new Vector2(0.2f, 0.2f), Vector2.zero, true);
            Sprite(projectile, "projectile", ctx.SpriteUnlit, SortingLayers.VFX);
            GameObject projectilePrefab = Save(ctx, projectile.AddComponent<Projectile>().gameObject, EditorPaths.PrefabsEnemies);

            GameObject turret = Go("Turret", null, Layers.Enemy);
            Sprite(turret, "turret_base", ctx.SpriteLit, SortingLayers.Interactables, 3);
            Box<BoxCollider2D>(turret, new Vector2(1f, 0.45f), new Vector2(0f, 0.22f), false);
            GameObject head = Go("Head", turret.transform, Layers.Enemy, new Vector3(0f, 0.55f, 0f));
            Sprite(head, "turret_head", ctx.SpriteLit, SortingLayers.Interactables, 4);
            GameObject muzzle = Go("Muzzle", head.transform, Layers.Enemy, new Vector3(0.75f, 0f, 0f));
            VisionCone tVision = Vision(ctx, head.transform, new Vector3(0.3f, 0f, 0f));
            EditorAssetUtil.SetFields(tVision, so => { so.Require("viewDistance").floatValue = 9f; so.Require("viewAngle").floatValue = 50f; });
            var t = turret.AddComponent<Turret>();
            EditorAssetUtil.SetFields(t, so =>
            {
                so.Require("vision").objectReferenceValue = tVision;
                so.Require("head").objectReferenceValue = head.transform;
                so.Require("muzzle").objectReferenceValue = muzzle.transform;
                so.Require("projectilePrefab").objectReferenceValue = projectilePrefab.GetComponent<Projectile>();
            });
            Save(ctx, turret, EditorPaths.PrefabsEnemies);
        }

        // ---------------------------------------------------------------- interactables

        private static void BuildInteractables(BuildContext ctx)
        {
            GameObject terminal = Go("Terminal", null, Layers.Interactable);
            Box<BoxCollider2D>(terminal, new Vector2(1.2f, 1.3f), new Vector2(0f, 0.65f), true);
            SpriteRenderer tBody = Sprite(terminal, "terminal", ctx.Dissolve, SortingLayers.Interactables, 2);
            GameObject screen = Go("Screen", terminal.transform, Layers.Interactable, new Vector3(0f, 1.0f, 0f));
            SpriteRenderer screenSprite = Sprite(screen, "terminal_screen", ctx.SpriteUnlit, SortingLayers.Interactables, 3);
            Light(terminal.transform, "ScreenGlow", Light2D.LightType.Point, new Color(0.3f, 1f, 1f), 0.8f, 1.8f, new Vector3(0f, 1f, 0f));
            var glitch = terminal.AddComponent<DissolveController>();
            EditorAssetUtil.SetFields(glitch, so => so.SetArray("renderers", new Renderer[] { tBody }));
            var term = terminal.AddComponent<Terminal>();
            EditorAssetUtil.SetFields(term, so =>
            {
                so.Require("screen").objectReferenceValue = screenSprite;
                so.Require("glitch").objectReferenceValue = glitch;
            });
            GameObject terminalPrefab = Save(ctx, terminal, EditorPaths.PrefabsInteractables);

            void TerminalVariant(string name, TerminalKind kind, HackingMinigameType minigame, HackAction action, string id, int credits, ItemDefinition reward)
            {
                Variant(ctx, terminalPrefab, name, EditorPaths.PrefabsInteractables, v => EditorAssetUtil.SetFields(v.GetComponent<Terminal>(), so =>
                {
                    so.Require("kind").enumValueIndex = (int)kind;
                    so.Require("minigame").enumValueIndex = (int)minigame;
                    so.Require("actionOnSuccess").enumValueIndex = (int)action;
                    so.Require("terminalId").stringValue = id;
                    so.Require("rewardCredits").intValue = credits;
                    so.Require("rewardItem").objectReferenceValue = reward;
                }));
            }

            TerminalVariant("Terminal_Door", TerminalKind.Door, HackingMinigameType.PatternMemory, HackAction.Open, "terminal_door", 25, null);
            TerminalVariant("Terminal_Camera", TerminalKind.Camera, HackingMinigameType.PortScan, HackAction.Disable, "terminal_camera", 25, null);
            TerminalVariant("Terminal_Data", TerminalKind.Data, HackingMinigameType.WireConnect, HackAction.Activate, "terminal_data", 100, ctx.DataShard);

            GameObject door = Go("Door", null, Layers.Wall);
            Sprite(Go("Frame", door.transform, Layers.Wall), "door_frame", ctx.Metal, SortingLayers.Environment, 1);
            GameObject panel = Go("Panel", door.transform, Layers.Wall);
            Sprite(panel, "door_panel", ctx.Metal, SortingLayers.Environment, 2);
            var blocker = Box<BoxCollider2D>(panel, new Vector2(1f, 3f), new Vector2(0f, 1.5f), false);
            panel.AddComponent<SurfaceTag>().SetSurface(SurfaceType.Metal);
            ShadowCaster(panel);
            var doorAnimator = door.AddComponent<Animator>();
            doorAnimator.runtimeAnimatorController = ctx.DoorAnimator;
            var doorComponent = door.AddComponent<Door>();
            EditorAssetUtil.SetFields(doorComponent, so =>
            {
                so.Require("blocker").objectReferenceValue = blocker;
                so.Require("animator").objectReferenceValue = doorAnimator;
            });
            Save(ctx, door, EditorPaths.PrefabsInteractables);

            GameObject elevator = Go("Elevator", null, Layers.Ground);
            var eBody = elevator.AddComponent<Rigidbody2D>();
            eBody.bodyType = RigidbodyType2D.Kinematic;
            Sprite(elevator, "elevator_platform", ctx.Metal, SortingLayers.Environment, 3);
            Box<BoxCollider2D>(elevator, new Vector2(3f, 0.5f), Vector2.zero, false);
            elevator.AddComponent<SurfaceTag>().SetSurface(SurfaceType.Metal);
            GameObject eZone = Go("InteractZone", elevator.transform, Layers.Interactable, new Vector3(0f, 0.8f, 0f));
            Box<BoxCollider2D>(eZone, new Vector2(3f, 1.2f), Vector2.zero, true);
            var eAnimator = elevator.AddComponent<Animator>();
            eAnimator.runtimeAnimatorController = ctx.ElevatorAnimator;
            EditorAssetUtil.SetField(elevator.AddComponent<Elevator>(), "animator", eAnimator);
            Save(ctx, elevator, EditorPaths.PrefabsInteractables);

            GameObject node = Go("NetworkNode", null, Layers.Interactable);
            Box<CircleCollider2D>(node, new Vector2(1.6f, 1.6f), new Vector2(0f, 0.8f), true);
            GameObject nVisual = Go("Visual", node.transform, Layers.Interactable, new Vector3(0f, 0.9f, 0f));
            Sprite(nVisual, "network_node", ctx.Hologram, SortingLayers.Interactables, 4);
            Light2D nGlow = Light(node.transform, "Glow", Light2D.LightType.Point, new Color(0.3f, 0.9f, 1f), 1.1f, 2.6f, new Vector3(0f, 0.9f, 0f));
            GameObject arrival = Go("Arrival", node.transform, Layers.Interactable, new Vector3(0f, 0.05f, 0f));
            var nAnimator = node.AddComponent<Animator>();
            nAnimator.runtimeAnimatorController = ctx.PulseAnimator;
            var nn = node.AddComponent<NetworkNode>();
            EditorAssetUtil.SetFields(nn, so =>
            {
                so.Require("arrivalPoint").objectReferenceValue = arrival.transform;
                so.Require("trailPrefab").objectReferenceValue = ctx.VfxPrefabs[VfxId.TeleportTrail];
                so.Require("glow").objectReferenceValue = nGlow;
                so.Require("animator").objectReferenceValue = nAnimator;
            });
            Save(ctx, node, EditorPaths.PrefabsInteractables);

            GameObject pad = Go("TeleportPad", null, Layers.Interactable);
            Box<BoxCollider2D>(pad, new Vector2(1.6f, 1.4f), new Vector2(0f, 0.7f), true);
            Sprite(pad, "teleport_pad", ctx.SpriteLit, SortingLayers.Interactables, 2);
            GameObject beam = Go("Visual", pad.transform, Layers.Interactable, new Vector3(0f, 0.9f, 0f));
            Sprite(beam, "soft_circle", ctx.Hologram, SortingLayers.Interactables, 3);
            beam.transform.localScale = new Vector3(1.2f, 3f, 1f);
            Light(pad.transform, "Glow", Light2D.LightType.Point, new Color(0.3f, 0.9f, 1f), 0.9f, 2.2f, new Vector3(0f, 0.6f, 0f));
            GameObject padArrival = Go("Arrival", pad.transform, Layers.Interactable, new Vector3(0f, 0.4f, 0f));
            var pAnimator = pad.AddComponent<Animator>();
            pAnimator.runtimeAnimatorController = ctx.PulseAnimator;
            EditorAssetUtil.SetFields(pad.AddComponent<TeleportPad>(), so =>
            {
                so.Require("arrivalPoint").objectReferenceValue = padArrival.transform;
                so.Require("trailPrefab").objectReferenceValue = ctx.VfxPrefabs[VfxId.TeleportTrail];
            });
            Save(ctx, pad, EditorPaths.PrefabsInteractables);

            GameObject checkpoint = Go("Checkpoint", null, Layers.Trigger);
            Box<BoxCollider2D>(checkpoint, new Vector2(1.2f, 2.2f), new Vector2(0f, 1.1f), true);
            GameObject cVisual = Go("Visual", checkpoint.transform, Layers.Trigger);
            Sprite(cVisual, "checkpoint", ctx.SpriteLit, SortingLayers.Interactables, 2);
            Light2D beacon = Light(checkpoint.transform, "Beacon", Light2D.LightType.Point, new Color(1f, 0.4f, 0.2f), 1.2f, 2.5f, new Vector3(0f, 1.35f, 0f));
            GameObject respawn = Go("Respawn", checkpoint.transform, Layers.Trigger, new Vector3(0.8f, 0.1f, 0f));
            var cpAnimator = checkpoint.AddComponent<Animator>();
            cpAnimator.runtimeAnimatorController = ctx.PulseAnimator;
            EditorAssetUtil.SetFields(checkpoint.AddComponent<Checkpoint>(), so =>
            {
                so.Require("respawnPoint").objectReferenceValue = respawn.transform;
                so.Require("animator").objectReferenceValue = cpAnimator;
                so.Require("beacon").objectReferenceValue = beacon;
            });
            Save(ctx, checkpoint, EditorPaths.PrefabsInteractables);

            GameObject pickup = Go("Pickup", null, Layers.Trigger);
            Box<CircleCollider2D>(pickup, new Vector2(0.9f, 0.9f), new Vector2(0f, 0.5f), true);
            GameObject pVisual = Go("Visual", pickup.transform, Layers.Trigger, new Vector3(0f, 0.5f, 0f));
            Sprite(pVisual, "pickup_shard", ctx.SpriteUnlit, SortingLayers.Interactables, 5);
            Light(pickup.transform, "Glow", Light2D.LightType.Point, new Color(1f, 0.5f, 0.9f), 0.8f, 1.4f, new Vector3(0f, 0.5f, 0f));
            EditorAssetUtil.SetFields(pickup.AddComponent<Pickup>(), so =>
            {
                so.Require("item").objectReferenceValue = ctx.DataShard;
                so.Require("visual").objectReferenceValue = pVisual.transform;
            });
            GameObject pickupPrefab = Save(ctx, pickup, EditorPaths.PrefabsInteractables);

            void PickupVariant(string name, ItemDefinition item, string sprite, int credits, Color glow)
            {
                Variant(ctx, pickupPrefab, name, EditorPaths.PrefabsInteractables, v =>
                {
                    EditorAssetUtil.SetFields(v.GetComponent<Pickup>(), so =>
                    {
                        so.Require("item").objectReferenceValue = item;
                        so.Require("credits").intValue = credits;
                    });
                    v.transform.Find("Visual").GetComponent<SpriteRenderer>().sprite = GeneratedArt.Sprite(sprite);
                    v.transform.Find("Glow").GetComponent<Light2D>().color = glow;
                });
            }

            PickupVariant("Pickup_DataShard", ctx.DataShard, "pickup_shard", 0, new Color(1f, 0.4f, 0.8f));
            PickupVariant("Pickup_UsbKey", ctx.UsbKey, "pickup_usb", 0, new Color(0.3f, 0.9f, 1f));
            PickupVariant("Pickup_Emp", ctx.EmpGrenade, "pickup_emp", 0, new Color(1f, 0.85f, 0.3f));
            PickupVariant("Pickup_Credits", null, "pickup_credit", 50, new Color(1f, 0.85f, 0.3f));

            Console(ctx, "JobBoard", typeof(JobBoard), new Color(1f, 0.6f, 0.9f), c => EditorAssetUtil.SetFields(c, so =>
                so.SetArray("contracts", new[] { ctx.SideQuest, ctx.GhostQuest })));
            Console(ctx, "FastTravelTerminal", typeof(FastTravelTerminal), new Color(0.4f, 1f, 1f), c => EditorAssetUtil.SetField(c, "destination", ctx.Level01));
            Console(ctx, "ShopTerminal", typeof(ShopTerminal), new Color(1f, 0.85f, 0.3f), c => EditorAssetUtil.SetFields(c, so =>
            {
                (ItemDefinition item, int price)[] stock = { (ctx.EmpGrenade, 60), (ctx.UsbKey, 90), (ctx.Exploit, 250) };
                SerializedProperty offers = so.Require("offers");
                offers.arraySize = stock.Length;
                for (int i = 0; i < stock.Length; i++)
                {
                    offers.GetArrayElementAtIndex(i).FindPropertyRelative("item").objectReferenceValue = stock[i].item;
                    offers.GetArrayElementAtIndex(i).FindPropertyRelative("price").intValue = stock[i].price;
                }
            }));
            Console(ctx, "SkillTerminal", typeof(SkillTerminal), new Color(0.5f, 1f, 0.5f), null);

            GameObject exit = Go("LevelExit", null, Layers.Trigger);
            Box<BoxCollider2D>(exit, new Vector2(1.5f, 2.6f), new Vector2(0f, 1.3f), true);
            GameObject arrow = Go("Arrow", exit.transform, Layers.Trigger, new Vector3(0f, 1.4f, 0f));
            Sprite(arrow, "exit_arrow", ctx.NeonCyan, SortingLayers.Interactables, 3);
            Light(exit.transform, "Glow", Light2D.LightType.Point, new Color(0.3f, 1f, 1f), 1f, 2.5f, new Vector3(0f, 1.3f, 0f));
            exit.AddComponent<LevelExit>();
            Save(ctx, exit, EditorPaths.PrefabsInteractables);
        }

        private static void Console(BuildContext ctx, string name, Type component, Color screenColor, Action<Component> configure)
        {
            GameObject console = Go(name, null, Layers.Interactable);
            Box<BoxCollider2D>(console, new Vector2(1.2f, 1.4f), new Vector2(0f, 0.7f), true);
            Sprite(console, "terminal", ctx.SpriteLit, SortingLayers.Interactables, 2);
            GameObject screen = Go("Screen", console.transform, Layers.Interactable, new Vector3(0f, 1.6f, 0f));
            Sprite(screen, "network_node", ctx.Hologram, SortingLayers.Interactables, 3, screenColor);
            screen.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            Light(console.transform, "Glow", Light2D.LightType.Point, screenColor, 0.9f, 2f, new Vector3(0f, 1.3f, 0f));
            Component c = console.AddComponent(component);
            configure?.Invoke(c);
            Save(ctx, console, EditorPaths.PrefabsInteractables);
        }

        // ---------------------------------------------------------------- environment

        private static void BuildEnvironment(BuildContext ctx)
        {
            GameObject laser = Go("Hazard_Laser", null, Layers.Trigger);
            Sprite(Go("EmitterTop", laser.transform, Layers.Trigger, new Vector3(0f, 3f, 0f)), "laser_emitter", ctx.SpriteLit, SortingLayers.Interactables, 3);
            Sprite(Go("EmitterBottom", laser.transform, Layers.Trigger), "laser_emitter", ctx.SpriteLit, SortingLayers.Interactables, 3);
            GameObject beam = Go("Beam", laser.transform, Layers.Trigger);
            SpriteRenderer beamSprite = Sprite(beam, "laser_beam", ctx.NeonMagenta, SortingLayers.VFX, 1);
            beam.transform.localScale = new Vector3(1f, 3f, 1f);
            var laserCollider = Box<BoxCollider2D>(laser, new Vector2(0.15f, 3f), new Vector2(0f, 1.5f), true);
            Light2D freeform = Light(laser.transform, "BeamLight", Light2D.LightType.Freeform, new Color(1f, 0.2f, 0.6f), 0.9f, 1f);
            freeform.SetShapePath(new[] { new Vector3(-0.35f, 0f), new Vector3(0.35f, 0f), new Vector3(0.35f, 3f), new Vector3(-0.35f, 3f) });
            EditorAssetUtil.SetFields(laser.AddComponent<Hazard>(), so =>
            {
                so.Require("hazardCollider").objectReferenceValue = laserCollider;
                so.Require("beam").objectReferenceValue = beamSprite;
                so.Require("onSeconds").floatValue = 2f;
                so.Require("offSeconds").floatValue = 1.4f;
                so.Require("damageType").enumValueIndex = (int)DamageType.Laser;
            });
            Save(ctx, laser, EditorPaths.PrefabsEnvironment);

            GameObject floor = Go("Hazard_ElectricFloor", null, Layers.Ground);
            SpriteRenderer floorSprite = Sprite(floor, "white", ctx.NeonCyan, SortingLayers.Environment, 4, new Color(1f, 0.9f, 0.3f));
            floor.transform.localScale = Vector3.one;
            floorSprite.drawMode = SpriteDrawMode.Sliced;
            floorSprite.size = new Vector2(2f, 0.2f);
            var floorCollider = Box<BoxCollider2D>(floor, new Vector2(2f, 0.2f), Vector2.zero, false);
            floor.AddComponent<SurfaceTag>().SetSurface(SurfaceType.Metal);
            EditorAssetUtil.SetFields(floor.AddComponent<Hazard>(), so =>
            {
                so.Require("hazardCollider").objectReferenceValue = floorCollider;
                so.Require("damageType").enumValueIndex = (int)DamageType.Electric;
                so.Require("knockback").floatValue = 9f;
            });
            Save(ctx, floor, EditorPaths.PrefabsEnvironment);

            GameObject hide = Go("HideZone", null, Layers.Trigger);
            Box<BoxCollider2D>(hide, new Vector2(2.4f, 2.4f), new Vector2(0f, 1.2f), true);
            GameObject shade = Go("Shadow", hide.transform, Layers.Trigger, new Vector3(0f, 1.2f, 0f));
            Sprite(shade, "hide_shadow", ctx.SpriteUnlit, SortingLayers.Foreground, 0);
            shade.transform.localScale = new Vector3(3f, 3f, 1f);
            hide.AddComponent<HideZone>();
            Save(ctx, hide, EditorPaths.PrefabsEnvironment);

            GameObject alarm = Go("AlarmZone", null, Layers.Trigger);
            Box<BoxCollider2D>(alarm, new Vector2(2f, 0.6f), new Vector2(0f, 0.3f), true);
            GameObject strip = Go("Indicator", alarm.transform, Layers.Trigger, new Vector3(0f, 0.05f, 0f));
            SpriteRenderer stripSprite = Sprite(strip, "white", ctx.NeonMagenta, SortingLayers.Interactables, 1, new Color(1f, 0.2f, 0.3f));
            strip.transform.localScale = new Vector3(16f, 0.5f, 1f);
            EditorAssetUtil.SetField(alarm.AddComponent<AlarmZone>(), "indicator", stripSprite);
            Save(ctx, alarm, EditorPaths.PrefabsEnvironment);

            GameObject wind = Go("WindZone", null, Layers.Trigger);
            Box<BoxCollider2D>(wind, new Vector2(4f, 4f), new Vector2(0f, 2f), true);
            var steam = (GameObject)PrefabUtility.InstantiatePrefab(ctx.VfxPrefabs[VfxId.Steam], wind.transform);
            steam.transform.localPosition = new Vector3(-2f, 0.2f, 0f);
            steam.transform.localRotation = Quaternion.Euler(0f, 0f, -70f);
            wind.AddComponent<WindZone2D>();
            Save(ctx, wind, EditorPaths.PrefabsEnvironment);

            GameObject water = Go("WaterBody", null, Layers.Water);
            water.tag = Tags.WaterVolume;
            var waterCollider = water.AddComponent<BoxCollider2D>();
            waterCollider.isTrigger = true;
            waterCollider.usedByEffector = true;
            waterCollider.size = new Vector2(10f, 4f);
            var buoyancy = water.AddComponent<BuoyancyEffector2D>();
            buoyancy.surfaceLevel = 2f;
            buoyancy.density = 2.2f;
            buoyancy.linearDamping = 3f;
            buoyancy.angularDamping = 1f;
            water.AddComponent<MeshFilter>();
            var waterRenderer = water.AddComponent<MeshRenderer>();
            waterRenderer.sharedMaterial = ctx.Water;
            waterRenderer.sortingLayerName = SortingLayers.Foreground;
            water.AddComponent<WaterBody2D>();
            Save(ctx, water, EditorPaths.PrefabsEnvironment);

            GameObject crate = Go("Crate", null, Layers.Ground);
            var crateBody = crate.AddComponent<Rigidbody2D>();
            crateBody.mass = 1.5f;
            Sprite(crate, "crate", ctx.SpriteLit, SortingLayers.Interactables, 1);
            Box<BoxCollider2D>(crate, new Vector2(1f, 1f), Vector2.zero, false);
            crate.AddComponent<SurfaceTag>().SetSurface(SurfaceType.Concrete);
            ShadowCaster(crate);
            Save(ctx, crate, EditorPaths.PrefabsEnvironment);

            GameObject tree = Go("Tree", null, Layers.Default);
            Sprite(tree, "tree_trunk", ctx.SpriteLit, SortingLayers.Environment, 1);
            GameObject canopy = Go("Canopy", tree.transform, Layers.Default, new Vector3(0f, 2.3f, 0f));
            Sprite(canopy, "tree_canopy", ctx.GrassWind, SortingLayers.Environment, 2);
            Save(ctx, tree, EditorPaths.PrefabsEnvironment);

            GameObject grass = Go("GrassPatch", null, Layers.Default);
            for (int i = 0; i < 3; i++)
            {
                GameObject blade = Go($"Grass_{i}", grass.transform, Layers.Default, new Vector3(i - 1f, 0f, 0f));
                Sprite(blade, "grass", ctx.GrassWind, SortingLayers.Foreground, i, new Color(0.85f + i * 0.05f, 1f, 0.9f));
            }

            Save(ctx, grass, EditorPaths.PrefabsEnvironment);

            GameObject neon = Go("NeonSign", null, Layers.Default);
            SpriteRenderer neonSprite = Sprite(neon, "neon_sign", ctx.NeonMagenta, SortingLayers.Environment, 5);
            Light2D neonLight = Light(neon.transform, "NeonLight", Light2D.LightType.Point, new Color(1f, 0.2f, 0.7f), 1.3f, 4.5f, null, true);
            EditorAssetUtil.SetFields(neonLight.gameObject.AddComponent<FlickerLight2D>(), so =>
            {
                so.Require("baseIntensity").floatValue = 1.3f;
                so.SetArray("neonSprites", new[] { neonSprite });
            });
            Save(ctx, neon, EditorPaths.PrefabsEnvironment);

            GameObject lamp = Go("StreetLamp", null, Layers.Default);
            Sprite(lamp, "antenna", ctx.SpriteLit, SortingLayers.Environment, 2, new Color(0.7f, 0.75f, 0.85f));
            lamp.transform.localScale = new Vector3(1f, 1.2f, 1f);
            Light2D spot = Light(lamp.transform, "SpotLight", Light2D.LightType.Point, new Color(1f, 0.85f, 0.55f), 1.4f, 7f, new Vector3(0f, 2.3f, 0f), true, 40f, 75f);
            spot.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            Save(ctx, lamp, EditorPaths.PrefabsEnvironment);

            GameObject ac = Go("RooftopAC", null, Layers.Ground);
            Sprite(ac, "rooftop_ac", ctx.Metal, SortingLayers.Environment, 2);
            Box<BoxCollider2D>(ac, new Vector2(1.5f, 0.85f), new Vector2(0f, 0.43f), false);
            ac.AddComponent<SurfaceTag>().SetSurface(SurfaceType.Metal);
            ShadowCaster(ac);
            Save(ctx, ac, EditorPaths.PrefabsEnvironment);

            GameObject spawn = Go("SpawnPoint", null, Layers.Default);
            spawn.tag = Tags.SpawnPoint;
            spawn.AddComponent<SpawnPoint>();
            Save(ctx, spawn, EditorPaths.PrefabsEnvironment);
        }

        // ---------------------------------------------------------------- camera + systems

        private static void BuildCameraRig(BuildContext ctx)
        {
            GameObject rig = Go("CameraRig");
            GameObject cameraGo = Go("Main Camera", rig.transform, Layers.Default, new Vector3(0f, 0f, -10f));
            cameraGo.tag = Tags.MainCamera;
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.03f, 0.02f, 0.07f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            cameraGo.AddComponent<AudioListener>();
            var brain = cameraGo.AddComponent<CinemachineBrain>();
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.8f);
            UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;

            GameObject vcamGo = Go("CM_Follow", rig.transform, Layers.Default, new Vector3(0f, 0f, -10f));
            var vcam = vcamGo.AddComponent<CinemachineCamera>();
            vcam.Priority = 10;
            LensSettings lens = LensSettings.Default;
            lens.OrthographicSize = 6f;
            lens.NearClipPlane = 0.1f;
            lens.FarClipPlane = 100f;
            vcam.Lens = lens;
            var composer = vcamGo.AddComponent<CinemachinePositionComposer>();
            composer.CameraDistance = 10f;
            composer.Damping = new Vector3(0.6f, 0.45f, 0f);
            composer.Lookahead = new LookaheadSettings { Enabled = true, Time = 0.35f, Smoothing = 8f, IgnoreY = true };
            var confiner = vcamGo.AddComponent<CinemachineConfiner2D>();
            confiner.Damping = 0.4f;
            vcamGo.AddComponent<CinemachineImpulseListener>();
            EditorAssetUtil.SetField(vcamGo.AddComponent<CameraController>(), "confiner", confiner);
            Save(ctx, rig, EditorPaths.PrefabsSystems);
        }

        private static void BuildSystemsRoot(BuildContext ctx)
        {
            GameObject root = Go(ResourcePaths.SystemsRoot);

            GameObject game = Go("Game", root.transform);
            var loader = game.AddComponent<SceneLoader>();
            EditorAssetUtil.SetField(game.AddComponent<GameManager>(), "sceneLoader", loader);

            game.AddComponent<CommandLineAutomation>();

            GameObject input = Go("Input", root.transform);
            EditorAssetUtil.SetField(input.AddComponent<InputModeController>(), "inputReader", ctx.Input);

            GameObject session = Go("Session", root.transform);
            EditorAssetUtil.SetField(session.AddComponent<SessionService>(), "database", ctx.Database);
            session.AddComponent<DialogueService>();

            GameObject pool = Go("Pool", root.transform);
            var poolService = pool.AddComponent<PoolService>();

            GameObject audio = Go("Audio", root.transform);
            EditorAssetUtil.SetFields(audio.AddComponent<AudioManager>(), so =>
            {
                so.Require("mixer").objectReferenceValue = ctx.Mixer.Mixer;
                so.Require("sfxGroup").objectReferenceValue = ctx.Mixer.Sfx;
                so.Require("uiGroup").objectReferenceValue = ctx.Mixer.UI;
                so.Require("ambienceGroup").objectReferenceValue = ctx.Mixer.Ambience;
                so.Require("library").objectReferenceValue = ctx.Sfx;
            });
            EditorAssetUtil.SetFields(audio.AddComponent<MusicDirector>(), so =>
            {
                so.Require("musicGroup").objectReferenceValue = ctx.Mixer.Music;
                so.Require("ambienceGroup").objectReferenceValue = ctx.Mixer.Ambience;
                so.Require("menuTheme").objectReferenceValue = GeneratedArt.Music("menu_theme");
                so.Require("stealthLayer").objectReferenceValue = GeneratedArt.Music("stealth_layer");
                so.Require("alertLayer").objectReferenceValue = GeneratedArt.Music("alert_layer");
                so.Require("ambienceLoop").objectReferenceValue = GeneratedArt.Ambience("city_ambience");
            });
            EditorAssetUtil.SetFields(audio.AddComponent<MixerSnapshots>(), so =>
            {
                so.Require("normal").objectReferenceValue = ctx.Mixer.Normal;
                so.Require("underwater").objectReferenceValue = ctx.Mixer.Underwater;
                so.Require("alarm").objectReferenceValue = ctx.Mixer.Alarm;
            });

            GameObject vfx = Go("VFX", root.transform);
            EditorAssetUtil.SetFields(vfx.AddComponent<VfxSpawner>(), so =>
            {
                so.Require("library").objectReferenceValue = ctx.Vfx;
                so.Require("pool").objectReferenceValue = poolService;
            });
            var impulse = vfx.AddComponent<CinemachineImpulseSource>();
            impulse.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            impulse.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
            impulse.ImpulseDefinition.ImpulseDuration = 0.22f;
            vfx.AddComponent<ScreenShake>();

            GameObject events = Go("EventSystem", root.transform);
            events.AddComponent<EventSystem>();
            events.AddComponent<InputSystemUIInputModule>();

            var uiRoot = (GameObject)PrefabUtility.InstantiatePrefab(ctx.Prefab("UIRoot"), root.transform);
            uiRoot.name = "UIRoot";

            string path = $"{EditorPaths.PrefabsSystemsResources}/{ResourcePaths.SystemsRoot}.prefab";
            ctx.Prefabs[ResourcePaths.SystemsRoot] = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }
    }
}
