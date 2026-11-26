using System.Collections.Generic;
using UnityEngine;

namespace RootAccess.EditorTools
{
    /// <summary>
    /// Gameplay sprites: terminals, doors, enemies, pickups, the rigged tree, grass and the parallax skylines. Every sprite
    /// keeps the pixel size the prefabs were built around (colliders, child offsets), only the painting changed.
    /// </summary>
    internal static class GameplayArt
    {
        private static readonly Vector2 Feet = new(0.5f, 0f);
        private static readonly Vector2 Centre = new(0.5f, 0.5f);
        private static readonly Color32 Ink = Paint.Rgb(8, 6, 18);
        private static readonly Color32 Cyan = Paint.Rgb(46, 242, 255);
        private static readonly Color32 Magenta = Paint.Rgb(255, 46, 166);
        private static readonly Color32 Amber = Paint.Rgb(255, 196, 64);
        private static readonly Color32 Red = Paint.Rgb(255, 60, 72);
        private static readonly Color32 White = Paint.Rgb(255, 255, 255);

        private static readonly Ramp Steel = Ramp.From(Paint.Rgb(104, 112, 132));
        private static readonly Ramp DarkSteel = Ramp.From(Paint.Rgb(56, 62, 80));
        private static readonly Ramp Shell = Ramp.From(Paint.Rgb(206, 210, 220), 0.7f);
        private static readonly Ramp Bark = Ramp.From(Paint.Rgb(92, 62, 48));

        /// <summary>Every gameplay sprite with its pivot.</summary>
        public static IEnumerable<(string name, PixelCanvas canvas, Vector2 pivot)> All()
        {
            foreach (KeyValuePair<string, PixelCanvas> frame in ArtCatalog.Characters()) yield return (frame.Key, frame.Value, Feet);

            yield return ("terminal", Terminal(), Feet);
            yield return ("door_panel", DoorPanel(), Feet);
            yield return ("door_frame", DoorFrame(), Feet);
            yield return ("teleport_pad", TeleportPad(), Feet);
            yield return ("checkpoint", Checkpoint(), Feet);
            yield return ("elevator_platform", Elevator(), Centre);
            yield return ("crate", Crate(), Centre);
            yield return ("laser_emitter", LaserEmitter(), Centre);
            yield return ("pickup_usb", Usb(), Centre);
            yield return ("pickup_shard", Shard(), Centre);
            yield return ("pickup_emp", Emp(), Centre);
            yield return ("pickup_credit", Credit(), Centre);
            yield return ("pickup_lore", LoreChip(), Centre);
            yield return ("alert_icon", AlertIcon(), Feet);
            for (int i = 0; i < 2; i++) yield return ($"drone_{i}", Drone(i), Centre);
            yield return ("camera_body", CameraBody(), new Vector2(0.05f, 0.5f));
            yield return ("turret_base", TurretBase(), Feet);
            yield return ("turret_head", TurretHead(), new Vector2(0.25f, 0.5f));
            yield return ("rooftop_ac", RooftopAc(), Feet);
            yield return ("server_rack", ServerRack(), Feet);
            yield return ("sewer_pipe", SewerPipe(), Centre);
            yield return ("neon_sign", NeonSign(), Centre);
            yield return ("grass", Grass(), Feet);
            yield return (PlaceholderArtGenerator.RiggedTree, Tree(), Feet);
            yield return ("skyline_far", Skyline(512, 200, 5, Paint.Rgb(40, 30, 74), Paint.Rgb(70, 46, 110), 0.12f, false), Feet);
            yield return ("skyline_mid", Skyline(512, 232, 9, Paint.Rgb(22, 18, 46), Paint.Rgb(46, 30, 80), 0.18f, true), Feet);
            yield return ("skyline_near", Skyline(512, 168, 15, Paint.Rgb(10, 8, 22), Paint.Rgb(26, 18, 46), 0.24f, true), Feet);
            yield return (KeyArtSprite, KeyArt(), Centre);
        }

        /// <summary>Full-screen title illustration behind the main menu (480×270, shown ×4 with point filtering).</summary>
        public const string KeyArtSprite = "menu_keyart";

        private static PixelCanvas Done(PixelCanvas c) => c.SelectiveOutline(Ink, 0.3f);

        /// <summary>Alpha-blends <paramref name="source"/> onto <paramref name="target"/> at (x, y), upscaled by <paramref name="scale"/>.</summary>
        private static void Blit(PixelCanvas target, PixelCanvas source, int x, int y, int scale = 1, Color? tint = null)
        {
            Color multiply = tint ?? Color.white;
            for (int sy = 0; sy < source.Height; sy++)
            for (int sx = 0; sx < source.Width; sx++)
            {
                Color c = source.Get(sx, sy);
                if (c.a <= 0f) continue;
                c *= multiply;
                for (int dy = 0; dy < scale; dy++)
                for (int dx = 0; dx < scale; dx++)
                    target.Blend(x + sx * scale + dx, y + sy * scale + dy, c);
            }
        }

        private static PixelCanvas Decor(string prefab)
        {
            foreach (DecorSpec spec in PropArt.Decor)
                if (spec.Prefab == prefab) return spec.Paint();
            throw new KeyNotFoundException($"[GameplayArt] No decor named {prefab}.");
        }

        /// <summary>
        /// Title illustration: dusk-purple sky with a big moon, three skyline layers, a billboard, a patrol drone, and the
        /// hacker on the edge of a rooftop (right of the menu panel), with rain over everything.
        /// </summary>
        private static PixelCanvas KeyArt()
        {
            const int w = 480, h = 270;
            var c = new PixelCanvas(w, h);
            c.DitherGradient(0, 0, w, h, Paint.Rgb(120, 40, 120), Paint.Rgb(6, 5, 22), 12);
            var random = new System.Random(2026);
            for (int i = 0; i < 140; i++)
            {
                int x = random.Next(w), y = 120 + random.Next(h - 120);
                c.Set(x, y, Color32.Lerp(Paint.Rgb(150, 140, 200), White, (float)random.NextDouble()));
            }

            c.Glow(372, 196, 70, new Color(1f, 0.55f, 0.85f, 0.35f), 1.6f);
            c.Circle(372, 196, 30, Paint.Rgb(255, 214, 236));
            c.Circle(380, 204, 7, Paint.Rgb(240, 190, 220)).Circle(360, 186, 5, Paint.Rgb(240, 196, 224)).Circle(384, 182, 3, Paint.Rgb(236, 186, 216));
            foreach ((int y, int x0, int len) in new[] { (190, 300, 140), (178, 330, 110), (206, 340, 90) })
                c.Rect(x0, y, len, 2, Paint.Rgb(64, 26, 84));

            Blit(c, Skyline(w, 190, 41, Paint.Rgb(52, 32, 90), Paint.Rgb(90, 52, 130), 0.12f, false), 0, 34);
            Blit(c, Decor("Decor_Billboard"), 96, 112);
            Blit(c, Skyline(w, 160, 42, Paint.Rgb(26, 18, 54), Paint.Rgb(52, 32, 92), 0.2f, true), 0, 18);
            Blit(c, Skyline(w, 120, 43, Paint.Rgb(12, 9, 28), Paint.Rgb(30, 20, 52), 0.26f, true), 0, 0);

            // Rooftop in the foreground, right of the menu panel.
            const int roof = 54;
            c.Bricks(214, 0, w - 214, roof - 6, 16, 8, Paint.Rgb(40, 22, 40), Paint.Rgb(92, 50, 66), 77, 0.12f);
            c.Bevel(210, roof - 6, w - 210, 6, Ramp.From(Paint.Rgb(132, 130, 156)));
            c.Streaks(214, 0, w - 214, roof - 6, Paint.Rgb(30, 16, 30), 78, 10);
            Blit(c, Decor("Decor_WaterTower"), 404, roof);
            Blit(c, Decor("Decor_Antennas"), 380, roof);
            Blit(c, Decor("Decor_Vent"), 352, roof);

            CharacterArt.Pose pose = CharacterArt.Stand();
            pose.Flutter = 0.95f;
            pose.HeadTilt = 10f;
            Blit(c, CharacterArt.Draw(pose, CharacterArt.PlayerStyle()), 228, roof - 4, 2);
            c.Glow(278, roof + 64, 14, new Color(0.2f, 0.95f, 1f, 0.25f));

            Blit(c, Drone(0), 296, 196, 2);
            c.Glow(328, 214, 10, new Color(1f, 0.3f, 0.7f, 0.35f));

            for (int i = 0; i < 380; i++)
            {
                int x = random.Next(w + 40) - 20, y = random.Next(h), len = 4 + random.Next(6);
                for (int d = 0; d < len; d++) c.Blend(x - d / 3, y - d, new Color(0.7f, 0.8f, 1f, 0.22f));
            }

            return c;
        }

        // ---------------------------------------------------------------- interactables

        private static PixelCanvas Terminal()
        {
            var c = new PixelCanvas(32, 42);
            c.Bevel(5, 0, 22, 4, DarkSteel);
            c.Bevel(10, 4, 12, 15, DarkSteel.Darker(0.85f));
            for (int y = 7; y < 17; y += 3) c.Rect(12, y, 8, 1, DarkSteel.Deep);
            c.Bevel(3, 19, 26, 4, Steel);
            for (int x = 5; x < 27; x += 2) c.Set(x, 21, Steel.Deep);
            c.Bevel(2, 23, 28, 18, Steel.Darker(0.8f));
            // Screen recess: the Screen child (20×12 px at y = 32) sits in it and tints by hack state.
            c.Rect(5, 25, 22, 14, Paint.Rgb(10, 12, 20));
            c.Rect(2, 40, 28, 1, Cyan).Set(28, 24, Paint.Rgb(80, 255, 170));
            c.Set(26, 24, Amber);
            return Done(c);
        }

        private static PixelCanvas DoorPanel()
        {
            var c = new PixelCanvas(32, 96);
            Ramp plate = Ramp.From(Paint.Rgb(92, 100, 120));
            for (int i = 0; i < 6; i++) c.Bevel(1, 1 + i * 16, 30, 15, plate, 1);
            c.Rect(15, 0, 2, 96, plate.Deep);
            c.Stripes(1, 1, 30, 7, Amber, Paint.Rgb(30, 28, 30), 6);
            c.Rect(13, 40, 6, 16, Paint.Rgb(20, 22, 30)).Rect(14, 41, 4, 14, Amber);
            for (int y = 43; y < 54; y += 3) c.Rect(14, y, 4, 1, Paint.Rgb(255, 230, 150));
            foreach (int y in new[] { 12, 28, 60, 76, 92 })
            {
                c.Set(4, y, plate.Light);
                c.Set(27, y, plate.Light);
            }

            c.Streaks(1, 0, 30, 96, Paint.Rgb(40, 40, 50), 3, 4);
            return Done(c);
        }

        private static PixelCanvas DoorFrame()
        {
            var c = new PixelCanvas(40, 104);
            c.Bevel(0, 0, 5, 104, DarkSteel).Bevel(35, 0, 5, 104, DarkSteel);
            c.Bevel(0, 97, 40, 7, DarkSteel);
            c.Stripes(5, 98, 30, 4, Amber, Paint.Rgb(30, 28, 30), 6);
            c.Rect(1, 60, 3, 6, Paint.Rgb(20, 20, 30)).Set(2, 64, Red);
            c.Set(2, 62, Paint.Rgb(80, 255, 170));
            return c;
        }

        private static PixelCanvas TeleportPad()
        {
            var c = new PixelCanvas(48, 14);
            c.Bevel(2, 0, 44, 5, DarkSteel);
            c.Dome(24, 6, 20, 3.2f, Steel);
            c.Rect(8, 8, 32, 2, Cyan).Rect(12, 10, 24, 1, Paint.Rgb(180, 250, 255));
            for (int x = 6; x < 44; x += 6) c.Set(x, 2, Cyan);
            return Done(c);
        }

        private static PixelCanvas Checkpoint()
        {
            var c = new PixelCanvas(18, 50);
            c.Bevel(2, 0, 14, 4, DarkSteel);
            c.Cylinder(7, 4, 4, 34, DarkSteel);
            for (int y = 8; y < 36; y += 6) c.Rect(7, y, 4, 1, Amber);
            c.Bevel(3, 38, 12, 3, Steel);
            c.Dome(9, 44, 5, 5, Ramp.From(Paint.Rgb(240, 240, 250), 0.5f));
            c.Rect(4, 41, 10, 1, Steel.Dark);
            return Done(c);
        }

        private static PixelCanvas Elevator()
        {
            var c = new PixelCanvas(96, 16);
            c.Bevel(1, 2, 94, 12, Steel);
            c.Stripes(1, 12, 94, 2, Amber, Paint.Rgb(30, 28, 30), 6);
            for (int x = 6; x < 92; x += 12)
            {
                c.Rect(x, 5, 6, 4, Steel.Dark);
                c.Rect(x + 1, 6, 4, 2, Steel.Deep);
            }

            c.Rect(1, 0, 94, 2, Paint.Rgb(46, 220, 255));
            return Done(c);
        }

        private static PixelCanvas Crate()
        {
            var c = new PixelCanvas(32, 32);
            Ramp panel = Ramp.From(Paint.Rgb(70, 92, 120));
            c.Bevel(1, 1, 30, 30, panel);
            c.Bevel(1, 1, 30, 4, DarkSteel).Bevel(1, 27, 30, 4, DarkSteel);
            c.Bevel(1, 1, 4, 30, DarkSteel).Bevel(27, 1, 4, 30, DarkSteel);
            c.Line(6, 6, 25, 25, panel.Dark, 2).Line(6, 25, 25, 6, panel.Dark, 2);
            c.Rect(9, 13, 14, 7, Paint.Rgb(30, 36, 48));
            c.Text("RA7", 10, 14, Amber);
            foreach ((int x, int y) in new[] { (3, 3), (28, 3), (3, 28), (28, 28) }) c.Set(x, y, Steel.Light);
            return Done(c);
        }

        private static PixelCanvas LaserEmitter()
        {
            var c = new PixelCanvas(14, 10);
            c.Bevel(1, 2, 12, 7, DarkSteel);
            c.Rect(5, 0, 4, 2, Magenta).Rect(6, 1, 2, 1, Paint.Rgb(255, 200, 230));
            c.Set(3, 6, Red);
            return Done(c);
        }

        private static PixelCanvas Usb()
        {
            var c = new PixelCanvas(16, 16);
            c.Bevel(2, 4, 9, 8, Ramp.From(Paint.Rgb(40, 44, 70)));
            c.Bevel(11, 5, 4, 6, Steel);
            c.Rect(12, 7, 2, 1, Steel.Deep).Rect(12, 9, 2, 1, Steel.Deep);
            c.Rect(4, 7, 5, 2, Cyan).Set(4, 10, Magenta);
            return Done(c);
        }

        private static PixelCanvas Shard()
        {
            var c = new PixelCanvas(16, 16);
            c.Polygon(new List<Vector2> { new(8, 15), new(13, 9), new(8, 1), new(3, 9) }, Paint.Rgb(200, 30, 140));
            c.Polygon(new List<Vector2> { new(8, 15), new(8, 1), new(3, 9) }, Paint.Rgb(255, 110, 200));
            c.Polygon(new List<Vector2> { new(8, 15), new(10, 9), new(8, 5), new(6, 9) }, Paint.Rgb(255, 200, 240));
            c.Set(7, 12, White);
            return Done(c);
        }

        private static PixelCanvas Emp()
        {
            var c = new PixelCanvas(16, 16);
            c.Dome(8, 7, 5.5f, 5.5f, Ramp.From(Paint.Rgb(70, 82, 104)));
            c.Bevel(6, 11, 4, 3, Steel);
            c.Rect(9, 13, 4, 1, Steel.Dark);
            c.Circle(8, 7, 2.2f, Amber);
            c.Set(7, 8, Paint.Rgb(255, 250, 200));
            return Done(c);
        }

        private static PixelCanvas Credit()
        {
            var c = new PixelCanvas(16, 16);
            c.Dome(8, 8, 6.5f, 6.5f, Ramp.From(Paint.Rgb(240, 190, 60)));
            c.Circle(8, 8, 4f, Paint.Rgb(200, 140, 30));
            c.Text("C", 7, 6, Paint.Rgb(255, 236, 150));
            c.Set(5, 11, White);
            return Done(c);
        }

        /// <summary>Data log: a small datapad with glowing text lines and an amber status LED.</summary>
        private static PixelCanvas LoreChip()
        {
            var c = new PixelCanvas(16, 16);
            c.Bevel(2, 2, 12, 11, Ramp.From(Paint.Rgb(40, 60, 72)));
            c.Rect(4, 4, 8, 7, Paint.Rgb(14, 36, 34));
            Color32 text = Paint.Rgb(120, 255, 190);
            c.Rect(5, 9, 6, 1, text).Rect(5, 7, 4, 1, text).Rect(5, 5, 5, 1, Paint.Shade(text, 0.7f));
            c.Rect(6, 13, 4, 2, Steel.Base);
            c.Set(12, 3, Amber);
            return Done(c);
        }

        private static PixelCanvas AlertIcon()
        {
            var c = new PixelCanvas(10, 18);
            c.Rect(3, 6, 4, 11, Paint.Rgb(255, 220, 60)).Rect(3, 1, 4, 3, Paint.Rgb(255, 220, 60));
            c.Rect(4, 7, 1, 9, Paint.Rgb(255, 250, 200));
            return c.Outline(Paint.Rgb(60, 10, 10));
        }

        // ---------------------------------------------------------------- enemies

        private static PixelCanvas Drone(int frame)
        {
            var c = new PixelCanvas(32, 24);
            Ramp hull = Ramp.From(Paint.Rgb(70, 78, 98));
            c.Capsule(new Vector2(4, 11), new Vector2(28, 11), 3f, 3f, hull.Darker(0.8f));
            c.Dome(16, 11, 10, 7, hull);
            c.Rect(7, 12, 18, 1, Magenta);
            c.Dome(16, 9, 4.5f, 4f, Ramp.From(Paint.Rgb(24, 24, 34)));
            c.Bevel(14, 17, 4, 3, DarkSteel);
            int rotor = frame == 0 ? 13 : 5;
            c.Rect(16 - rotor, 20, rotor * 2, 2, Paint.Rgb(150, 160, 180, frame == 0 ? 200 : 255));
            c.Rect(16 - rotor, 21, rotor * 2, 1, Paint.Rgb(210, 220, 240, 160));
            c.Rect(3, 9, 3, 2, Paint.Rgb(255, 120, 180)).Rect(26, 9, 3, 2, Paint.Rgb(255, 120, 180));
            return Done(c);
        }

        private static PixelCanvas CameraBody()
        {
            var c = new PixelCanvas(28, 14);
            c.Bevel(0, 4, 6, 6, DarkSteel);
            c.Capsule(new Vector2(8, 7), new Vector2(21, 7), 4.4f, 4f, Shell);
            c.Rect(8, 10, 12, 1, Shell.Light);
            c.Dome(23, 7, 3.6f, 3.6f, Ramp.From(Paint.Rgb(30, 30, 40)));
            c.Circle(23.5f, 7, 1.6f, Cyan);
            c.Set(23, 8, White);
            return Done(c);
        }

        private static PixelCanvas TurretBase()
        {
            var c = new PixelCanvas(32, 18);
            c.Bevel(1, 0, 30, 6, DarkSteel);
            c.Stripes(2, 1, 28, 2, Amber, Paint.Rgb(30, 28, 30), 6);
            c.Dome(16, 9, 9, 7, Steel.Darker(0.85f));
            c.Rect(8, 8, 16, 1, Magenta);
            return Done(c);
        }

        private static PixelCanvas TurretHead()
        {
            var c = new PixelCanvas(28, 12);
            c.Dome(7, 6, 6, 5.5f, Steel);
            c.Capsule(new Vector2(11, 7), new Vector2(25, 7), 2f, 1.6f, DarkSteel);
            c.Capsule(new Vector2(11, 4), new Vector2(23, 4), 1.4f, 1.2f, DarkSteel.Darker(0.8f));
            c.Rect(25, 6, 2, 3, Magenta);
            c.Set(5, 8, Red);
            return Done(c);
        }

        // ---------------------------------------------------------------- environment

        private static PixelCanvas RooftopAc()
        {
            var c = new PixelCanvas(48, 32);
            Ramp body = Ramp.From(Paint.Rgb(150, 156, 170), 0.8f);
            c.Bevel(2, 2, 44, 25, body);
            c.Rect(4, 0, 4, 2, DarkSteel.Dark).Rect(40, 0, 4, 2, DarkSteel.Dark);
            c.Dome(16, 14, 9, 9, Ramp.From(Paint.Rgb(40, 44, 54)));
            for (int i = 0; i < 4; i++)
            {
                float a = (i * 90f + 20f) * Mathf.Deg2Rad;
                c.Line(16, 14, 16 + Mathf.RoundToInt(Mathf.Cos(a) * 7), 14 + Mathf.RoundToInt(Mathf.Sin(a) * 7), body.Dark, 2);
            }

            c.Dome(16, 14, 2, 2, body);
            for (int y = 6; y < 24; y += 3) c.Rect(29, y, 14, 1, body.Deep);
            c.Streaks(2, 2, 44, 25, Paint.Rgb(80, 60, 60), 6, 3);
            return Done(c);
        }

        private static PixelCanvas ServerRack()
        {
            var c = new PixelCanvas(48, 96);
            c.Bevel(1, 0, 46, 96, DarkSteel);
            c.RackFace(4, 3, 40, 88, 77);
            c.Rect(4, 92, 40, 2, Paint.Rgb(46, 200, 255));
            c.Rect(42, 40, 2, 16, Steel.Light);
            return Done(c);
        }

        private static PixelCanvas SewerPipe()
        {
            var c = new PixelCanvas(32, 16);
            Ramp pipe = Ramp.From(Paint.Rgb(78, 100, 84));
            c.Pipe(0, 2, 32, 12, pipe);
            c.Pipe(13, 0, 6, 16, pipe.Darker(0.8f));
            c.Set(14, 8, pipe.Light);
            c.Streaks(0, 2, 32, 12, Paint.Rgb(60, 110, 60), 21, 2);
            return c;
        }

        private static PixelCanvas NeonSign()
        {
            var c = new PixelCanvas(96, 32);
            c.Frame(2, 2, 92, 28, White, 2);
            c.Text("24/7", 18, 6, White, 4);
            return c;
        }

        private static PixelCanvas Grass()
        {
            var c = new PixelCanvas(32, 16);
            var random = new System.Random(17);
            Ramp blade = Ramp.From(Paint.Rgb(60, 150, 80));
            for (int i = 0; i < 26; i++)
            {
                int x = random.Next(32), height = 5 + random.Next(11);
                int lean = random.Next(5) - 2;
                for (int y = 0; y < height; y++)
                {
                    float t = y / (float)height;
                    int px = x + Mathf.RoundToInt(lean * t * t);
                    c.Set(px, y, t < 0.3f ? blade.Dark : t < 0.75f ? blade.Base : blade.Light);
                }
            }

            return c;
        }

        /// <summary>
        /// Tree for the 2D Animation rig (96×160: the bone chain in <see cref="SpriteRigFactory.TreeBones"/> runs up the
        /// middle): a tapered trunk with two boughs and a canopy of shaded leaf clusters.
        /// </summary>
        private static PixelCanvas Tree()
        {
            var c = new PixelCanvas(96, 160);
            c.Capsule(new Vector2(48, 2), new Vector2(48, 112), 5.5f, 3f, Bark);
            c.Capsule(new Vector2(40, 0), new Vector2(46, 8), 2.4f, 3f, Bark);
            c.Capsule(new Vector2(56, 0), new Vector2(50, 8), 2.4f, 3f, Bark);
            c.Capsule(new Vector2(48, 70), new Vector2(26, 100), 2.6f, 1.4f, Bark);
            c.Capsule(new Vector2(48, 86), new Vector2(72, 112), 2.4f, 1.3f, Bark);
            var random = new System.Random(33);
            Ramp[] leaves = { Ramp.From(Paint.Rgb(46, 110, 70)), Ramp.From(Paint.Rgb(60, 140, 78)), Ramp.From(Paint.Rgb(80, 160, 90)) };
            for (int layer = 0; layer < 3; layer++)
            for (int i = 0; i < 16; i++)
            {
                float x = 16 + (float)random.NextDouble() * 64, y = 88 + (float)random.NextDouble() * 58 + layer * 2;
                float r = 7 + (float)random.NextDouble() * 7 - layer;
                c.Dome(x, y, r, r * 0.85f, leaves[layer], layer * 0.08f);
            }

            c.Speckle(10, 80, 76, 78, Paint.Rgb(150, 220, 140), 0.02f, 5);
            for (int i = 0; i < 6; i++) c.Set(20 + random.Next(56), 92 + random.Next(50), Paint.Rgb(255, 120, 200));
            return Done(c);
        }

        /// <summary>
        /// Parallax city: stepped towers with antennas and spires, window grids lit per building (some fully dark), a
        /// few rooftop neon signs, and haze fading the bottom into the street glow.
        /// </summary>
        private static PixelCanvas Skyline(int width, int height, int seed, Color32 body, Color32 rim, float windowDensity, bool signs)
        {
            var c = new PixelCanvas(width, height);
            var random = new System.Random(seed);
            Color32[] lights = { Paint.Rgb(255, 200, 110), Paint.Rgb(255, 214, 150), Paint.Rgb(90, 220, 255), Paint.Rgb(255, 80, 180) };
            int x = 0;
            while (x < width)
            {
                int w = 22 + random.Next(52), h = height / 4 + random.Next(height * 3 / 4 - 8);
                w = Mathf.Min(w, width - x);
                c.Rect(x, 0, w, h, body);
                c.Rect(x, h - 1, w, 1, rim);
                if (random.NextDouble() < 0.5)
                {
                    int sw = w / 2 + random.Next(w / 3), sx = x + (w - sw) / 2, sh = 6 + random.Next(16);
                    c.Rect(sx, h, sw, sh, body).Rect(sx, h + sh - 1, sw, 1, rim);
                    h += sh;
                }

                if (random.NextDouble() < 0.45)
                {
                    int ax = x + 3 + random.Next(Mathf.Max(1, w - 6)), ah = 8 + random.Next(26);
                    c.Rect(ax, h, 1, ah, body);
                    c.Set(ax, h + ah, Paint.Rgb(255, 60, 80));
                }

                bool dark = random.NextDouble() < 0.2;
                bool stripes = random.NextDouble() < 0.25;
                Color32 tone = lights[random.Next(lights.Length)];
                for (int wy = 5; wy < h - 4; wy += 5)
                for (int wx = x + 3; wx < x + w - 3; wx += 4)
                {
                    if (dark || random.NextDouble() > windowDensity) continue;
                    if (stripes) c.Rect(wx, wy, 3, 1, tone);
                    else c.Rect(wx, wy, 2, 2, random.NextDouble() < 0.7 ? tone : lights[random.Next(lights.Length)]);
                }

                if (signs && random.NextDouble() < 0.18 && w > 26)
                {
                    int sy = h / 2 + random.Next(Mathf.Max(1, h / 3)), sx = x + 4;
                    Color32 neon = random.NextDouble() < 0.5 ? Paint.Rgb(255, 60, 170) : Paint.Rgb(60, 230, 255);
                    c.Rect(sx, sy, 3, 14, neon).Rect(sx + 4, sy + 12, 10, 2, neon);
                }

                x += w + random.Next(3);
            }

            // Street haze: fade the lowest quarter towards a warm purple glow.
            int haze = height / 4;
            for (int y = 0; y < haze; y++)
            {
                float t = 1f - y / (float)haze;
                for (int xx = 0; xx < width; xx++)
                    if (c.Get(xx, y).a > 0)
                        c.Set(xx, y, Color32.Lerp(c.Get(xx, y), Paint.Rgb(110, 50, 120), t * 0.45f * Paint.Dither(xx, y) + t * 0.25f));
            }

            return c;
        }
    }
}
