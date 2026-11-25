using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace RootAccess.EditorTools
{
    /// <summary>
    /// Generates every placeholder asset procedurally (no downloads): character/enemy frames, props, tiles with
    /// normal maps, parallax skylines, a 6-sided night skybox, VFX textures and synthesized SFX / music loops.
    /// Real art packs later replace these files in place (same names) or live under Assets/ThirdParty.
    /// </summary>
    public static class PlaceholderArtGenerator
    {
        private static readonly Color32 Clear = new(0, 0, 0, 0);
        private static readonly Color32 Outline = new(10, 8, 20, 255);
        private static readonly Color32 Cyan = new(46, 242, 255, 255);
        private static readonly Color32 Magenta = new(255, 46, 166, 255);
        private static readonly Color32 Yellow = new(255, 210, 63, 255);
        private static readonly Color32 White = new(255, 255, 255, 255);

        private sealed class SpriteSpec
        {
            public string Path;
            public Vector2 Pivot;
            public string NormalPath;
            public bool Bilinear;
        }

        private struct Pose
        {
            public int LegL, LegR, LiftL, LiftR, ArmL, ArmR, Lean, Bob;
            public bool Crouch, ArmForward, ArmUp, Lying, Swim;
        }

        private struct HumanoidPalette
        {
            public Color32 Body, BodyDark, Trim, Legs, Boots, Head, Visor;
        }

        private static readonly List<SpriteSpec> Sprites = new();
        private static readonly List<string> NormalMaps = new();
        private static readonly List<string> PlainTextures = new();

        [MenuItem("Root Access/Setup/2. Generate Placeholder Art")]
        public static void GenerateAll()
        {
            EditorAssetUtil.RunBatchStep("PlaceholderArtGenerator", () =>
            {
                Sprites.Clear();
                NormalMaps.Clear();
                PlainTextures.Clear();
                EditorAssetUtil.EnsureFolder(EditorPaths.Sprites + "/Normals");

                PaintPlayer();
                PaintGuard();
                PaintEnemies();
                PaintInteractables();
                PaintEnvironment();
                PaintVfxTextures();
                PaintTiles();
                PaintSkybox();
                GenerateAudio();

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                ConfigureImporters();
                EditorAssetUtil.Log($"Generated {Sprites.Count} sprites, {NormalMaps.Count} normal maps, {PlainTextures.Count} textures.");
            });
        }

        // ---------------------------------------------------------------- characters

        private static void PaintPlayer()
        {
            var palette = new HumanoidPalette
            {
                Body = new Color32(59, 42, 92, 255), BodyDark = new Color32(40, 28, 66, 255), Trim = Cyan,
                Legs = new Color32(30, 30, 46, 255), Boots = new Color32(20, 20, 28, 255),
                Head = new Color32(48, 34, 78, 255), Visor = Cyan
            };

            var poses = new Dictionary<string, Pose>
            {
                ["idle_0"] = new Pose(),
                ["idle_1"] = new Pose { Bob = -1 },
                ["run_0"] = new Pose { LegL = -4, LiftL = 2, LegR = 3, ArmL = 2, ArmR = -2, Lean = 1 },
                ["run_1"] = new Pose { LegL = -1, LegR = 1, LiftR = 4, Lean = 1, Bob = 1 },
                ["run_2"] = new Pose { LegL = 3, LegR = -4, LiftR = 2, ArmL = -2, ArmR = 2, Lean = 1 },
                ["run_3"] = new Pose { LegL = 1, LiftL = 4, LegR = -1, Lean = 1, Bob = 1 },
                ["jump"] = new Pose { LiftL = 5, LiftR = 3, LegL = -2, LegR = 2, ArmL = 4, ArmR = 4 },
                ["fall"] = new Pose { LegL = -4, LegR = 4, LiftL = 1, LiftR = 2, ArmL = 6, ArmR = 6 },
                ["dash"] = new Pose { Lean = 4, LegL = -6, LiftL = 3, LegR = -3, LiftR = 1, ArmForward = true },
                ["wallslide"] = new Pose { Lean = -1, ArmUp = true, LegL = -1, LegR = 1, LiftR = 3 },
                ["crouch"] = new Pose { Crouch = true, LegL = -3, LegR = 3 },
                ["swim_0"] = new Pose { Swim = true, ArmL = 5, ArmR = -1, LiftL = 2 },
                ["swim_1"] = new Pose { Swim = true, ArmL = -1, ArmR = 5, LiftR = 2 },
                ["hack_0"] = new Pose { ArmForward = true },
                ["hack_1"] = new Pose { ArmForward = true, Bob = -1 },
                ["hurt"] = new Pose { Lean = -3, ArmL = 5, ArmR = 5, LegL = -2, LegR = 2 },
                ["death_0"] = new Pose { Lean = -4, Crouch = true, ArmL = 6, ArmR = 6 },
                ["death_1"] = new Pose { Lying = true }
            };

            foreach (KeyValuePair<string, Pose> pose in poses)
            {
                PixelCanvas canvas = PaintHumanoid(pose.Value, palette, helmet: false);
                if (pose.Key.StartsWith("hack")) canvas.Rect(27, 30 - pose.Value.Bob, 5, 7, new Color32(46, 242, 255, 170));
                if (pose.Key == "hurt") canvas.Tint(new Color(1f, 0.55f, 0.55f));
                if (pose.Key == "dash")
                    for (int i = 0; i < 4; i++) canvas.Rect(0, 18 + i * 4, 6 - i, 1, new Color32(46, 242, 255, 160));
                SaveSprite($"player_{pose.Key}", canvas, new Vector2(0.5f, 0f), withNormal: true);
            }
        }

        private static void PaintGuard()
        {
            var palette = new HumanoidPalette
            {
                Body = new Color32(70, 84, 104, 255), BodyDark = new Color32(48, 58, 74, 255), Trim = Yellow,
                Legs = new Color32(38, 44, 56, 255), Boots = new Color32(22, 24, 30, 255),
                Head = new Color32(40, 46, 58, 255), Visor = new Color32(255, 51, 68, 255)
            };

            var poses = new Dictionary<string, Pose>
            {
                ["idle_0"] = new Pose(),
                ["idle_1"] = new Pose { Bob = -1 },
                ["walk_0"] = new Pose { LegL = -3, LiftL = 1, LegR = 2, ArmL = 1, ArmR = -1 },
                ["walk_1"] = new Pose { LegL = -1, LegR = 1, LiftR = 3, Bob = 1 },
                ["walk_2"] = new Pose { LegL = 2, LegR = -3, LiftR = 1, ArmL = -1, ArmR = 1 },
                ["walk_3"] = new Pose { LegL = 1, LiftL = 3, LegR = -1, Bob = 1 },
                ["attack_0"] = new Pose { ArmUp = true, Lean = -1 },
                ["attack_1"] = new Pose { ArmForward = true, Lean = 2, LegL = -3, LegR = 3 },
                ["attack_2"] = new Pose { ArmForward = true, Lean = 1 }
            };

            foreach (KeyValuePair<string, Pose> pose in poses)
            {
                PixelCanvas canvas = PaintHumanoid(pose.Value, palette, helmet: true);
                if (pose.Key == "attack_1") canvas.Rect(26, 29, 6, 2, Yellow);
                SaveSprite($"guard_{pose.Key}", canvas, new Vector2(0.5f, 0f), withNormal: true);
            }

            SaveSprite("alert_icon", new PixelCanvas(8, 16).Rect(2, 5, 4, 11, White).Rect(2, 0, 4, 3, White), new Vector2(0.5f, 0f));
        }

        private static PixelCanvas PaintHumanoid(Pose p, HumanoidPalette pal, bool helmet)
        {
            var c = new PixelCanvas(32, 48);
            if (p.Lying)
            {
                c.Rect(4, 1, 20, 7, pal.Body).Rect(2, 1, 5, 4, pal.Legs).Circle(26, 5, 5, pal.Head).Rect(25, 4, 4, 2, pal.Visor);
                return c.Outline(Outline);
            }

            if (p.Swim)
            {
                c.Rect(4, 18, 20, 9, pal.Body).Rect(4, 21, 20, 1, pal.Trim).Rect(0, 19 + p.LiftL, 6, 3, pal.Legs).Rect(0, 24 - p.LiftR, 6, 3, pal.Legs);
                c.Rect(22, 26 + p.ArmL, 8, 3, pal.BodyDark).Rect(22, 16 + p.ArmR, 8, 3, pal.BodyDark).Circle(27, 23, 5, pal.Head).Rect(27, 23, 4, 2, pal.Visor);
                return c.Outline(Outline);
            }

            int hip = p.Crouch ? 10 : 15;
            int torsoTop = p.Crouch ? 25 : 31;
            DrawLeg(c, 11 + p.LegL, p.LiftL, hip, pal);
            DrawLeg(c, 17 + p.LegR, p.LiftR, hip, pal);

            int cx = 16 + p.Lean;
            int bob = p.Bob;
            c.Rect(cx - 6, hip + bob, 12, torsoTop - hip, pal.Body);
            c.Rect(cx - 6, hip + bob + 3, 12, 1, pal.Trim);
            c.Rect(cx - 2, hip + bob + 6, 4, 5, pal.BodyDark);

            c.Rect(cx - 8, torsoTop - 10 + p.ArmL + bob, 3, 9, pal.BodyDark);
            if (p.ArmForward) c.Rect(cx + 5, torsoTop - 6 + bob, 10, 3, pal.BodyDark);
            else if (p.ArmUp) c.Rect(cx + 5, torsoTop - 3 + bob, 3, 11, pal.BodyDark);
            else c.Rect(cx + 5, torsoTop - 10 + p.ArmR + bob, 3, 9, pal.BodyDark);

            int headY = torsoTop + 5 + bob;
            c.Circle(cx + 0.5f, headY, 6, pal.Head);
            if (helmet) c.Rect(cx - 6, headY + 2, 13, 3, pal.BodyDark);
            c.Rect(cx, headY - 1, 6, 2, pal.Visor);
            return c.Outline(Outline);
        }

        private static void DrawLeg(PixelCanvas c, int x, int lift, int hip, HumanoidPalette pal)
        {
            c.Rect(x, lift + 2, 4, Mathf.Max(1, hip - lift - 2), pal.Legs);
            c.Rect(x, lift, 6, 2, pal.Boots);
        }

        // ---------------------------------------------------------------- enemies and props

        private static void PaintEnemies()
        {
            Color32 metal = new(60, 66, 80, 255);
            for (int frame = 0; frame < 2; frame++)
            {
                var drone = new PixelCanvas(32, 24);
                drone.Ellipse(16, 10, 12, 6, metal).Rect(6, 9, 20, 2, Magenta);
                int rotor = frame == 0 ? 12 : 6;
                drone.Rect(16 - rotor, 19, rotor * 2, 2, new Color32(140, 150, 170, 255)).Rect(15, 15, 2, 4, metal);
                SaveSprite($"drone_{frame}", drone.Outline(Outline), new Vector2(0.5f, 0.5f), withNormal: true);
            }

            SaveSprite("drone_eye", new PixelCanvas(8, 8).Circle(4, 4, 3.5f, White), new Vector2(0.5f, 0.5f));

            var cameraBody = new PixelCanvas(28, 14);
            cameraBody.Rect(0, 5, 6, 4, new Color32(40, 40, 48, 255)).Rect(5, 2, 18, 10, new Color32(210, 214, 222, 255))
                .Rect(5, 2, 18, 2, new Color32(150, 156, 168, 255)).Circle(23, 7, 4, new Color32(20, 20, 26, 255)).Circle(23, 7, 2, Cyan);
            SaveSprite("camera_body", cameraBody.Outline(Outline), new Vector2(0.05f, 0.5f), withNormal: true);

            var turretBase = new PixelCanvas(32, 16);
            turretBase.Rect(2, 0, 28, 6, metal).Rect(8, 6, 16, 8, new Color32(80, 86, 100, 255)).Rect(8, 6, 16, 1, Magenta);
            SaveSprite("turret_base", turretBase.Outline(Outline), new Vector2(0.5f, 0f), withNormal: true);
            var turretHead = new PixelCanvas(28, 12);
            turretHead.Circle(7, 6, 6, new Color32(90, 96, 112, 255)).Rect(10, 4, 18, 4, new Color32(50, 54, 64, 255)).Rect(24, 4, 4, 4, Magenta);
            SaveSprite("turret_head", turretHead.Outline(Outline), new Vector2(0.25f, 0.5f), withNormal: true);

            SaveSprite("projectile", new PixelCanvas(12, 4).Rect(0, 1, 12, 2, Magenta).Rect(8, 0, 4, 4, White), new Vector2(0.5f, 0.5f));
        }

        private static void PaintInteractables()
        {
            Color32 steel = new(86, 94, 110, 255);
            Color32 steelDark = new(54, 60, 72, 255);

            var door = new PixelCanvas(32, 96);
            door.Rect(0, 0, 32, 96, steel);
            for (int y = 8; y < 96; y += 16) door.Rect(2, y, 28, 2, steelDark);
            door.Rect(13, 0, 6, 96, steelDark).Rect(14, 40, 4, 14, Yellow);
            SaveSprite("door_panel", door.Noise(0.03f, 3).Outline(Outline), new Vector2(0.5f, 0f), withNormal: true);
            SaveSprite("door_frame", new PixelCanvas(40, 104).Rect(0, 0, 4, 104, steelDark).Rect(36, 0, 4, 104, steelDark).Rect(0, 98, 40, 6, steelDark), new Vector2(0.5f, 0f));

            var terminal = new PixelCanvas(32, 40);
            terminal.Rect(4, 0, 24, 26, steelDark).Rect(2, 24, 28, 16, steel).Rect(6, 4, 20, 3, Cyan).Rect(6, 10, 20, 2, steel);
            SaveSprite("terminal", terminal.Outline(Outline), new Vector2(0.5f, 0f), withNormal: true);
            SaveSprite("terminal_screen", new PixelCanvas(20, 12).Fill(White).Rect(2, 2, 16, 1, new Color32(255, 255, 255, 120)), new Vector2(0.5f, 0.5f));

            var node = new PixelCanvas(32, 32);
            for (int i = 0; i < 16; i++)
            {
                node.Rect(16 - i, 16 + (15 - i), 2, 1, White).Rect(16 + i - 1, 16 + (15 - i), 2, 1, White);
                node.Rect(16 - i, 16 - (15 - i), 2, 1, White).Rect(16 + i - 1, 16 - (15 - i), 2, 1, White);
            }

            node.Circle(16, 16, 5, White);
            SaveSprite("network_node", node, new Vector2(0.5f, 0.5f));

            var pad = new PixelCanvas(48, 12);
            pad.Rect(0, 0, 48, 6, steelDark).Rect(4, 6, 40, 4, steel).Rect(6, 10, 36, 2, Cyan);
            SaveSprite("teleport_pad", pad.Outline(Outline), new Vector2(0.5f, 0f), withNormal: true);

            var beacon = new PixelCanvas(16, 48);
            beacon.Rect(6, 0, 4, 40, steelDark).Rect(2, 0, 12, 4, steel).Circle(8, 42, 5, White);
            SaveSprite("checkpoint", beacon.Outline(Outline), new Vector2(0.5f, 0f));

            SaveSprite("pickup_usb", new PixelCanvas(16, 16).Rect(3, 4, 7, 8, new Color32(40, 40, 60, 255)).Rect(10, 6, 4, 4, new Color32(200, 200, 210, 255)).Rect(4, 7, 5, 2, Cyan).Outline(Outline), new Vector2(0.5f, 0.5f));
            var shard = new PixelCanvas(16, 16);
            for (int y = 0; y < 14; y++)
            {
                int half = y < 7 ? y / 2 + 1 : (13 - y) / 2 + 1;
                shard.Rect(8 - half, y + 1, half * 2, 1, y % 3 == 0 ? White : Magenta);
            }

            SaveSprite("pickup_shard", shard.Outline(Outline), new Vector2(0.5f, 0.5f));
            SaveSprite("pickup_emp", new PixelCanvas(16, 16).Circle(8, 7, 5, new Color32(70, 80, 96, 255)).Rect(6, 11, 4, 3, new Color32(140, 150, 170, 255)).Circle(8, 7, 2, Yellow).Outline(Outline), new Vector2(0.5f, 0.5f));
            SaveSprite("pickup_credit", new PixelCanvas(16, 16).Circle(8, 8, 6, Yellow).Rect(7, 4, 2, 8, new Color32(160, 120, 20, 255)).Outline(Outline), new Vector2(0.5f, 0.5f));
            SaveSprite("laser_beam", new PixelCanvas(4, 32).Rect(1, 0, 2, 32, White).Rect(0, 0, 1, 32, new Color32(255, 255, 255, 90)).Rect(3, 0, 1, 32, new Color32(255, 255, 255, 90)), new Vector2(0.5f, 0f));
            SaveSprite("laser_emitter", new PixelCanvas(12, 8).Rect(0, 0, 12, 8, steelDark).Rect(4, 0, 4, 2, Magenta).Outline(Outline), new Vector2(0.5f, 0.5f));

            var platform = new PixelCanvas(96, 16);
            platform.Rect(0, 0, 96, 16, steel).Rect(0, 13, 96, 3, Cyan);
            for (int x = 6; x < 96; x += 12) platform.Rect(x, 4, 2, 2, steelDark);
            SaveSprite("elevator_platform", platform.Outline(Outline), new Vector2(0.5f, 0.5f), withNormal: true);

            var crate = new PixelCanvas(32, 32);
            crate.Rect(0, 0, 32, 32, new Color32(120, 84, 48, 255)).Rect(0, 0, 32, 3, new Color32(80, 54, 30, 255)).Rect(0, 29, 32, 3, new Color32(80, 54, 30, 255))
                .Line(2, 2, 29, 29, new Color32(80, 54, 30, 255), 2);
            SaveSprite("crate", crate.Noise(0.04f, 9).Outline(Outline), new Vector2(0.5f, 0.5f), withNormal: true);

            SaveSprite("hide_shadow", new PixelCanvas(32, 32).SoftCircle(16, 16, 16, new Color(0.02f, 0.02f, 0.06f, 0.9f), 0.6f), new Vector2(0.5f, 0.5f), bilinear: true);
            SaveSprite("exit_arrow", new PixelCanvas(24, 24).Rect(2, 9, 12, 6, Cyan).Line(12, 2, 22, 12, Cyan, 3).Line(12, 22, 22, 12, Cyan, 3), new Vector2(0.5f, 0.5f));
        }

        private static void PaintEnvironment()
        {
            var trunk = new PixelCanvas(24, 96);
            trunk.Rect(8, 0, 8, 80, new Color32(78, 52, 40, 255)).Rect(4, 0, 16, 6, new Color32(60, 40, 30, 255)).Line(12, 50, 2, 74, new Color32(78, 52, 40, 255), 3);
            SaveSprite("tree_trunk", trunk.Noise(0.05f, 11).Outline(Outline), new Vector2(0.5f, 0f), withNormal: true);

            var canopy = new PixelCanvas(96, 72);
            var random = new System.Random(21);
            for (int i = 0; i < 26; i++)
            {
                float x = 14 + (float)random.NextDouble() * 68, y = 12 + (float)random.NextDouble() * 48;
                byte g = (byte)(90 + random.Next(70));
                canopy.Circle(x, y, 9 + (float)random.NextDouble() * 9, new Color32((byte)(20 + random.Next(20)), g, (byte)(60 + random.Next(40)), 255));
            }

            SaveSprite("tree_canopy", canopy.Noise(0.05f, 12).Outline(Outline), new Vector2(0.5f, 0.1f), withNormal: true);

            var grass = new PixelCanvas(32, 16);
            for (int x = 0; x < 32; x++)
            {
                int height = 6 + (x * 7 + 3) % 10;
                byte g = (byte)(120 + (x * 37) % 90);
                grass.Rect(x, 0, 1, height, new Color32(30, g, 70, 255));
            }

            SaveSprite("grass", grass, new Vector2(0.5f, 0f), withNormal: true);

            var neon = new PixelCanvas(96, 32);
            neon.Rect(2, 2, 92, 2, White).Rect(2, 28, 92, 2, White).Rect(2, 2, 2, 28, White).Rect(92, 2, 2, 28, White);
            int[] glyphX = { 12, 32, 52, 72 };
            foreach (int gx in glyphX) neon.Rect(gx, 9, 2, 14, White).Rect(gx, 21, 10, 2, White).Rect(gx, 9, 10, 2, White).Rect(gx + 8, 9, 2, 14, White);
            SaveSprite("neon_sign", neon, new Vector2(0.5f, 0.5f));

            SaveSprite("sky_gradient", new PixelCanvas(8, 256).VerticalGradient(new Color(0.08f, 0.03f, 0.14f), new Color(0.01f, 0.01f, 0.05f)), new Vector2(0.5f, 0.5f), bilinear: true);
            SaveSprite("skyline_far", PaintSkyline(512, 192, 5, new Color32(28, 22, 52, 255), 0.25f), new Vector2(0.5f, 0f));
            SaveSprite("skyline_mid", PaintSkyline(512, 224, 9, new Color32(18, 14, 36, 255), 0.4f), new Vector2(0.5f, 0f));
            SaveSprite("skyline_near", PaintSkyline(512, 160, 15, new Color32(10, 8, 22, 255), 0.55f), new Vector2(0.5f, 0f));

            var ac = new PixelCanvas(48, 32);
            ac.Rect(0, 0, 48, 28, new Color32(120, 126, 136, 255)).Circle(16, 14, 9, new Color32(60, 64, 72, 255)).Rect(30, 6, 14, 2, new Color32(80, 84, 92, 255)).Rect(30, 12, 14, 2, new Color32(80, 84, 92, 255));
            SaveSprite("rooftop_ac", ac.Outline(Outline), new Vector2(0.5f, 0f), withNormal: true);
            SaveSprite("antenna", new PixelCanvas(16, 64).Rect(7, 0, 2, 60, new Color32(90, 96, 110, 255)).Circle(8, 60, 3, Magenta), new Vector2(0.5f, 0f));

            // Server farm rack: blade slots with status LEDs (Level03 / Level05 decor).
            var rack = new PixelCanvas(48, 96);
            rack.Rect(0, 0, 48, 96, new Color32(34, 38, 48, 255)).Rect(3, 3, 42, 90, new Color32(22, 25, 32, 255));
            for (int y = 8, row = 0; y < 88; y += 10, row++)
            {
                rack.Rect(6, y, 36, 6, new Color32(52, 58, 70, 255)).Rect(9, y + 2, 3, 2, Cyan);
                rack.Rect(15, y + 2, 3, 2, row % 3 == 0 ? Magenta : Cyan).Rect(28, y + 2, 11, 1, new Color32(90, 96, 110, 255));
            }

            SaveSprite("server_rack", rack.Noise(0.02f, 7).Outline(Outline), new Vector2(0.5f, 0f), withNormal: true);

            // Sewer pipe segment, tiled along the walls of Level04.
            var pipe = new PixelCanvas(32, 16);
            pipe.Rect(0, 2, 32, 12, new Color32(70, 92, 74, 255)).Rect(0, 10, 32, 2, new Color32(110, 140, 112, 255))
                .Rect(0, 2, 32, 2, new Color32(40, 54, 44, 255)).Rect(14, 0, 4, 16, new Color32(56, 74, 60, 255));
            SaveSprite("sewer_pipe", pipe.Noise(0.04f, 11).Outline(Outline), new Vector2(0.5f, 0.5f), withNormal: true);
        }

        private static PixelCanvas PaintSkyline(int width, int height, int seed, Color32 color, float windowDensity)
        {
            var canvas = new PixelCanvas(width, height);
            var random = new System.Random(seed);
            int x = 0;
            while (x < width)
            {
                int w = 24 + random.Next(56);
                int h = height / 4 + random.Next(height * 3 / 4);
                canvas.Rect(x, 0, w, h, color);
                if (random.NextDouble() < 0.3) canvas.Rect(x + w / 2, h, 2, 12 + random.Next(20), color);
                for (int wy = 6; wy < h - 6; wy += 7)
                for (int wx = x + 4; wx < x + w - 4; wx += 6)
                {
                    if (random.NextDouble() > windowDensity) continue;
                    Color32 light = random.NextDouble() < 0.5 ? new Color32(255, 200, 90, 200) : random.NextDouble() < 0.5 ? new Color32(46, 242, 255, 170) : new Color32(255, 46, 166, 170);
                    canvas.Rect(wx, wy, 3, 3, light);
                }

                x += w + random.Next(4);
            }

            return canvas;
        }

        private static void PaintVfxTextures()
        {
            SaveSprite("white", new PixelCanvas(4, 4).Fill(White), new Vector2(0.5f, 0.5f));
            SaveSprite("soft_circle", new PixelCanvas(32, 32).SoftCircle(16, 16, 16, Color.white, 1.6f), new Vector2(0.5f, 0.5f), bilinear: true);
            SaveSprite("spark", new PixelCanvas(16, 4).SoftCircle(8, 2, 8, Color.white, 0.7f), new Vector2(0.5f, 0.5f), bilinear: true);
            SaveSprite("raindrop", new PixelCanvas(2, 12).Fill(new Color32(180, 220, 255, 150)), new Vector2(0.5f, 0.5f));
            var smoke = new PixelCanvas(32, 32).SoftCircle(16, 16, 15, new Color(1f, 1f, 1f, 0.8f), 1.2f).Noise(0.12f, 31);
            SaveSprite("smoke", smoke, new Vector2(0.5f, 0.5f), bilinear: true);
            var ring = new PixelCanvas(32, 32);
            for (int a = 0; a < 360; a += 3)
            {
                float rad = a * Mathf.Deg2Rad;
                ring.Circle(16 + Mathf.Cos(rad) * 13, 16 + Mathf.Sin(rad) * 13, 1.6f, White);
            }

            SaveSprite("ring", ring, new Vector2(0.5f, 0.5f), bilinear: true);
            SaveSprite("data_bit", new PixelCanvas(4, 6).Fill(White).Rect(1, 1, 2, 4, new Color32(255, 255, 255, 120)), new Vector2(0.5f, 0.5f));
        }

        // ---------------------------------------------------------------- tiles and sky

        private static void PaintTiles()
        {
            Color32 concrete = new(104, 104, 112, 255);
            var concreteTile = new PixelCanvas(32, 32).Fill(concrete).Noise(0.06f, 41);
            concreteTile.Rect(0, 0, 32, 1, new Color32(80, 80, 88, 255)).Rect(0, 0, 1, 32, new Color32(80, 80, 88, 255));
            SaveTile("tile_concrete", concreteTile);

            Color32 metal = new(88, 96, 112, 255);
            var metalTile = new PixelCanvas(32, 32).Fill(metal);
            metalTile.Rect(0, 0, 32, 2, new Color32(60, 66, 78, 255)).Rect(0, 0, 2, 32, new Color32(60, 66, 78, 255)).Rect(0, 30, 32, 2, new Color32(120, 128, 144, 255));
            foreach (Vector2Int rivet in new[] { new Vector2Int(5, 5), new Vector2Int(26, 5), new Vector2Int(5, 26), new Vector2Int(26, 26) })
                metalTile.Circle(rivet.x, rivet.y, 1.6f, new Color32(150, 158, 176, 255));
            SaveTile("tile_metal", metalTile.Noise(0.025f, 42));

            var edgeTile = metalTile.Clone().Rect(0, 26, 32, 6, new Color32(60, 66, 78, 255)).Rect(0, 30, 32, 2, Cyan);
            SaveTile("tile_metal_top", edgeTile);

            var brick = new PixelCanvas(32, 32).Fill(new Color32(70, 40, 52, 255));
            for (int row = 0; row < 4; row++)
            {
                int offset = row % 2 == 0 ? 0 : 8;
                for (int col = -1; col < 3; col++)
                    brick.Rect(col * 16 + offset + 1, row * 8 + 1, 14, 6, new Color32(110, 62, 70, 255));
            }

            SaveTile("tile_brick", brick.Noise(0.05f, 43));

            var glass = new PixelCanvas(32, 32).Fill(new Color32(120, 200, 255, 90));
            glass.Line(4, 4, 20, 28, new Color32(255, 255, 255, 120), 2).Rect(0, 0, 32, 2, new Color32(70, 90, 110, 255)).Rect(0, 30, 32, 2, new Color32(70, 90, 110, 255));
            SaveTile("tile_glass", glass, withNormal: false);
        }

        private static void PaintSkybox()
        {
            string[] faces = { "front", "back", "left", "right", "up", "down" };
            for (int i = 0; i < faces.Length; i++)
            {
                const int size = 256;
                var canvas = new PixelCanvas(size, size);
                if (faces[i] == "down")
                {
                    canvas.Fill(new Color32(6, 4, 12, 255));
                }
                else if (faces[i] == "up")
                {
                    canvas.Fill(new Color32(4, 3, 14, 255));
                    AddStars(canvas, 160, 70 + i);
                }
                else
                {
                    canvas.VerticalGradient(new Color(0.22f, 0.07f, 0.28f), new Color(0.02f, 0.02f, 0.08f));
                    AddStars(canvas, 70, 50 + i);
                    var random = new System.Random(80 + i);
                    int x = 0;
                    while (x < size)
                    {
                        int w = 8 + random.Next(22);
                        int h = 10 + random.Next(60);
                        canvas.Rect(x, 0, w, h, new Color32(14, 8, 26, 255));
                        for (int wy = 4; wy < h - 3; wy += 5)
                        for (int wx = x + 2; wx < x + w - 2; wx += 4)
                            if (random.NextDouble() < 0.2) canvas.Set(wx, wy, new Color32(255, 180, 90, 255));
                        x += w;
                    }
                }

                string path = GeneratedArt.SkyboxFacePath(faces[i]);
                WriteAsset(path, canvas.EncodePng());
                PlainTextures.Add(path);
            }
        }

        private static void AddStars(PixelCanvas canvas, int count, int seed)
        {
            var random = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                int x = random.Next(canvas.Width), y = random.Next(canvas.Height / 3, canvas.Height);
                byte b = (byte)(150 + random.Next(105));
                canvas.Set(x, y, new Color32(b, b, (byte)Mathf.Min(255, b + 30), 255));
            }
        }

        // ---------------------------------------------------------------- audio

        private static void GenerateAudio()
        {
            const ProceduralAudio.Wave sine = ProceduralAudio.Wave.Sine;
            const ProceduralAudio.Wave square = ProceduralAudio.Wave.Square;
            const ProceduralAudio.Wave saw = ProceduralAudio.Wave.Saw;
            const ProceduralAudio.Wave tri = ProceduralAudio.Wave.Triangle;
            const ProceduralAudio.Wave noise = ProceduralAudio.Wave.Noise;

            Sfx("jump", 0.18f, b => ProceduralAudio.AddTone(b, square, 260, 620, 0, 0.18f, 0.4f));
            Sfx("double_jump", 0.22f, b => { ProceduralAudio.AddTone(b, square, 400, 900, 0, 0.12f, 0.35f); ProceduralAudio.AddTone(b, tri, 600, 1200, 0.08f, 0.14f, 0.3f); });
            Sfx("land", 0.2f, b => { ProceduralAudio.AddTone(b, noise, 900, 200, 0, 0.2f, 0.7f, 0.002f, 0.15f, 3); ProceduralAudio.AddTone(b, sine, 120, 50, 0, 0.15f, 0.6f); });
            Sfx("dash", 0.25f, b => ProceduralAudio.AddTone(b, noise, 6000, 1500, 0, 0.25f, 0.6f, 0.01f, 0.15f, 4));
            Sfx("hurt", 0.3f, b => { ProceduralAudio.AddTone(b, saw, 420, 140, 0, 0.3f, 0.5f); ProceduralAudio.AddTone(b, noise, 3000, 800, 0, 0.12f, 0.3f, 0.002f, 0.1f, 5); });
            Sfx("death", 0.9f, b => { ProceduralAudio.AddTone(b, saw, 300, 40, 0, 0.9f, 0.5f, 0.01f, 0.4f); ProceduralAudio.AddTone(b, noise, 2000, 100, 0, 0.6f, 0.3f, 0.01f, 0.4f, 6); });
            Sfx("ui_click", 0.06f, b => ProceduralAudio.AddTone(b, square, 1200, 900, 0, 0.06f, 0.3f, 0.001f, 0.03f));
            Sfx("ui_hover", 0.04f, b => ProceduralAudio.AddTone(b, sine, 1800, 1800, 0, 0.04f, 0.2f, 0.001f, 0.02f));
            Sfx("ui_back", 0.1f, b => ProceduralAudio.AddTone(b, square, 700, 400, 0, 0.1f, 0.3f));
            Sfx("hack_beep", 0.08f, b => ProceduralAudio.AddTone(b, square, 1500, 1500, 0, 0.08f, 0.3f, 0.001f, 0.02f));
            Sfx("hack_success", 0.5f, b => { float[] n = { 523, 659, 784, 1047 }; for (int i = 0; i < n.Length; i++) ProceduralAudio.AddTone(b, tri, n[i], n[i], i * 0.1f, 0.18f, 0.35f); });
            Sfx("hack_fail", 0.45f, b => { ProceduralAudio.AddTone(b, square, 300, 120, 0, 0.45f, 0.35f); ProceduralAudio.AddTone(b, square, 305, 118, 0, 0.45f, 0.25f); });
            Sfx("door_open", 0.6f, b => { ProceduralAudio.AddTone(b, noise, 500, 1800, 0, 0.55f, 0.4f, 0.05f, 0.2f, 7); ProceduralAudio.AddTone(b, sine, 80, 120, 0, 0.6f, 0.3f); });
            Sfx("door_close", 0.4f, b => { ProceduralAudio.AddTone(b, noise, 1800, 400, 0, 0.35f, 0.4f, 0.02f, 0.1f, 8); ProceduralAudio.AddTone(b, sine, 90, 50, 0.3f, 0.1f, 0.6f); });
            Sfx("pickup", 0.3f, b => { ProceduralAudio.AddTone(b, sine, 988, 988, 0, 0.1f, 0.4f); ProceduralAudio.AddTone(b, sine, 1319, 1319, 0.08f, 0.22f, 0.4f); });
            Sfx("checkpoint", 0.7f, b => { float[] n = { 392, 523, 659, 784 }; for (int i = 0; i < n.Length; i++) ProceduralAudio.AddTone(b, sine, n[i], n[i], i * 0.12f, 0.3f, 0.3f, 0.01f, 0.2f); });
            Sfx("teleport_out", 0.5f, b => { ProceduralAudio.AddTone(b, sine, 300, 2400, 0, 0.5f, 0.4f, 0.02f, 0.1f); ProceduralAudio.AddTone(b, noise, 2000, 8000, 0, 0.5f, 0.2f, 0.05f, 0.1f, 9); });
            Sfx("teleport_in", 0.5f, b => { ProceduralAudio.AddTone(b, sine, 2400, 300, 0, 0.5f, 0.4f, 0.02f, 0.2f); ProceduralAudio.AddTone(b, noise, 8000, 2000, 0, 0.4f, 0.2f, 0.01f, 0.2f, 10); });
            Sfx("alarm", 1.2f, b => { for (int i = 0; i < 3; i++) ProceduralAudio.AddTone(b, saw, 600, 900, i * 0.4f, 0.4f, 0.35f, 0.01f, 0.05f); });
            Sfx("spotted", 0.35f, b => { ProceduralAudio.AddTone(b, square, 880, 880, 0, 0.12f, 0.35f); ProceduralAudio.AddTone(b, square, 1320, 1320, 0.14f, 0.2f, 0.35f); });
            Sfx("splash", 0.6f, b => ProceduralAudio.AddTone(b, noise, 4000, 300, 0, 0.6f, 0.6f, 0.005f, 0.4f, 11));
            Sfx("emp", 0.8f, b => { ProceduralAudio.AddTone(b, sine, 200, 30, 0, 0.8f, 0.6f, 0.005f, 0.5f); ProceduralAudio.AddTone(b, noise, 6000, 500, 0, 0.5f, 0.4f, 0.005f, 0.3f, 12); });
            Sfx("turret_shot", 0.15f, b => { ProceduralAudio.AddTone(b, square, 1400, 300, 0, 0.15f, 0.35f); ProceduralAudio.AddTone(b, noise, 5000, 2000, 0, 0.06f, 0.3f, 0.001f, 0.04f, 13); });

            Footsteps("step_concrete", 2, 1800, 0.06f, 20);
            Footsteps("step_metal", 2, 4200, 0.08f, 22, ringHz: 900);
            Footsteps("step_glass", 1, 6000, 0.05f, 24, ringHz: 2400);
            Footsteps("step_grass", 2, 3500, 0.1f, 26);
            Footsteps("step_water", 1, 2500, 0.15f, 28);

            Music("menu_theme", 8f, b =>
            {
                float[] chords = { 110f, 87.31f, 130.81f, 98f };
                for (int bar = 0; bar < 4; bar++)
                {
                    float root = chords[bar];
                    ProceduralAudio.AddTone(b, sine, root, root, bar * 2f, 2f, 0.25f, 0.3f, 0.3f);
                    ProceduralAudio.AddTone(b, sine, root * 1.5f, root * 1.5f, bar * 2f, 2f, 0.15f, 0.3f, 0.3f);
                    for (int step = 0; step < 8; step++)
                    {
                        float note = root * 4f * (step % 4 == 3 ? 1.5f : step % 2 == 0 ? 1f : 1.25f);
                        ProceduralAudio.AddTone(b, tri, note, note, bar * 2f + step * 0.25f, 0.22f, 0.12f, 0.005f, 0.15f);
                    }
                }
            });

            Music("stealth_layer", 8f, b =>
            {
                for (int beat = 0; beat < 16; beat++)
                {
                    float t = beat * 0.5f;
                    ProceduralAudio.AddTone(b, sine, 55f, 50f, t, 0.35f, 0.45f, 0.01f, 0.25f);
                    ProceduralAudio.AddTone(b, noise, 9000, 9000, t + 0.25f, 0.04f, 0.08f, 0.001f, 0.03f, 40 + beat);
                }

                ProceduralAudio.AddTone(b, sine, 220f, 220f, 0f, 4f, 0.08f, 1f, 1f);
                ProceduralAudio.AddTone(b, sine, 207.65f, 207.65f, 4f, 4f, 0.08f, 1f, 1f);
            });

            Music("alert_layer", 8f, b =>
            {
                for (int step = 0; step < 32; step++)
                {
                    float t = step * 0.25f;
                    ProceduralAudio.AddTone(b, saw, step % 8 < 4 ? 110f : 98f, step % 8 < 4 ? 110f : 98f, t, 0.2f, 0.18f, 0.005f, 0.08f);
                    if (step % 4 == 0) ProceduralAudio.AddTone(b, sine, 140f, 40f, t, 0.18f, 0.6f, 0.002f, 0.1f);
                    if (step % 4 == 2) ProceduralAudio.AddTone(b, noise, 5000, 1500, t, 0.12f, 0.3f, 0.001f, 0.08f, 60 + step);
                    float lead = step % 3 == 0 ? 440f : step % 3 == 1 ? 523.25f : 659.25f;
                    ProceduralAudio.AddTone(b, square, lead, lead, t, 0.12f, 0.06f, 0.002f, 0.05f);
                }
            });

            Ambience("city_ambience", 6f, b =>
            {
                ProceduralAudio.AddTone(b, noise, 400, 400, 0f, 6f, 0.35f, 0.5f, 0.5f, 90);
                ProceduralAudio.AddTone(b, sine, 60f, 60f, 0f, 6f, 0.12f, 0.5f, 0.5f);
                ProceduralAudio.AddTone(b, noise, 2500, 1200, 2f, 2.5f, 0.08f, 0.8f, 0.8f, 91);
            });
        }

        private static void Footsteps(string name, int variants, float brightness, float length, int seed, float ringHz = 0f)
        {
            for (int v = 0; v < variants; v++)
            {
                int variant = v;
                Sfx($"{name}_{v}", length + 0.05f, b =>
                {
                    ProceduralAudio.AddTone(b, ProceduralAudio.Wave.Noise, brightness * (1f + variant * 0.15f), brightness * 0.3f, 0, length, 0.6f, 0.001f, length * 0.8f, seed + variant);
                    if (ringHz > 0f) ProceduralAudio.AddTone(b, ProceduralAudio.Wave.Sine, ringHz, ringHz * 0.9f, 0, length, 0.15f, 0.001f, length * 0.9f);
                });
            }
        }

        private static void Sfx(string name, float seconds, System.Action<float[]> synth) => Synthesize(GeneratedArt.SfxPath(name), seconds, synth, loop: false);
        private static void Music(string name, float seconds, System.Action<float[]> synth) => Synthesize(GeneratedArt.MusicPath(name), seconds, synth, loop: true);
        private static void Ambience(string name, float seconds, System.Action<float[]> synth) => Synthesize(GeneratedArt.AmbiencePath(name), seconds, synth, loop: true);

        private static void Synthesize(string assetPath, float seconds, System.Action<float[]> synth, bool loop)
        {
            float[] buffer = ProceduralAudio.Buffer(seconds);
            synth(buffer);
            ProceduralAudio.Normalize(buffer, loop ? 0.6f : 0.85f);
            if (loop) ProceduralAudio.MakeLoopable(buffer);
            ProceduralAudio.WriteWav(FullPath(assetPath), buffer);
        }

        // ---------------------------------------------------------------- file + importer plumbing

        private static void SaveSprite(string name, PixelCanvas canvas, Vector2 pivot, bool withNormal = false, bool bilinear = false)
        {
            string path = GeneratedArt.SpritePath(name);
            WriteAsset(path, canvas.EncodePng());
            string normalPath = null;
            if (withNormal)
            {
                normalPath = GeneratedArt.NormalPath(name);
                WriteAsset(normalPath, canvas.ToNormalMap(3f, fromAlpha: true).EncodePng());
                NormalMaps.Add(normalPath);
            }

            Sprites.Add(new SpriteSpec { Path = path, Pivot = pivot, NormalPath = normalPath, Bilinear = bilinear });
        }

        private static void SaveTile(string name, PixelCanvas canvas, bool withNormal = true)
        {
            string path = GeneratedArt.TilePath(name);
            WriteAsset(path, canvas.EncodePng());
            string normalPath = null;
            if (withNormal)
            {
                normalPath = GeneratedArt.TileNormalPath(name);
                WriteAsset(normalPath, canvas.ToNormalMap(4f, fromAlpha: false).EncodePng());
                NormalMaps.Add(normalPath);
            }

            Sprites.Add(new SpriteSpec { Path = path, Pivot = new Vector2(0.5f, 0.5f), NormalPath = normalPath });
        }

        private static void WriteAsset(string assetPath, byte[] bytes)
        {
            string full = FullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(full) ?? ".");
            File.WriteAllBytes(full, bytes);
        }

        private static string FullPath(string assetPath) =>
            Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, assetPath);

        private static void ConfigureImporters()
        {
            foreach (string normalPath in NormalMaps)
            {
                if (AssetImporter.GetAtPath(normalPath) is not TextureImporter importer) continue;
                importer.textureType = TextureImporterType.NormalMap;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            foreach (string texturePath in PlainTextures)
            {
                if (AssetImporter.GetAtPath(texturePath) is not TextureImporter importer) continue;
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            var factories = new SpriteDataProviderFactories();
            factories.Init();
            foreach (SpriteSpec spec in Sprites)
            {
                if (AssetImporter.GetAtPath(spec.Path) is not TextureImporter importer) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = GeneratedArt.PixelsPerUnit;
                importer.filterMode = spec.Bilinear ? FilterMode.Bilinear : FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = spec.Pivot;
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();

                if (spec.NormalPath == null) continue;
                var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(spec.NormalPath);
                if (normal == null) continue;
                ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
                provider.InitSpriteEditorDataProvider();
                var secondary = provider.GetDataProvider<ISecondaryTextureDataProvider>();
                secondary.textures = new[] { new SecondarySpriteTexture { name = GeneratedArt.NormalMapProperty, texture = normal } };
                provider.Apply();
                importer.SaveAndReimport();
            }

            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { EditorPaths.Audio }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not AudioImporter audio) continue;
                bool isLoop = path.Contains("/Music/") || path.Contains("/Ambience/");
                AudioImporterSampleSettings sample = audio.defaultSampleSettings;
                sample.loadType = isLoop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
                sample.compressionFormat = isLoop ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
                audio.defaultSampleSettings = sample;
                audio.forceToMono = !isLoop;
                audio.SaveAndReimport();
            }
        }
    }
}
