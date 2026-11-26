using System;
using System.Collections.Generic;
using UnityEngine;

namespace RootAccess.EditorTools
{
    /// <summary>One background prop: what it looks like, where it stands and whether it glows.</summary>
    internal sealed class DecorSpec
    {
        public string Prefab;
        public string Sprite;
        public Func<PixelCanvas> Paint;
        public Vector2 Pivot = new(0.5f, 0f);
        /// <summary>Hung on the back wall (posters, screens) instead of standing on the floor.</summary>
        public bool Wall;
        /// <summary>Hangs from the ceiling (lamps, moss): pivot at the top edge.</summary>
        public bool Ceiling;
        public bool Hologram;
        public Color? Glow;
        public Vector2 GlowAt;
        public float GlowRadius = 2f;
        public float GlowIntensity = 0.8f;
        /// <summary>Spot light pointing down (lamps) instead of a round glow.</summary>
        public bool Spot;
    }

    /// <summary>
    /// Background props of every biome (rooftop water towers, office desks, server cooling units, sewer valves, HQ statues)
    /// and the lamps hung in each level. Painted with the shaded primitives in <see cref="Paint"/>, so the 2D lights pick
    /// out their relief through the generated normal maps.
    /// </summary>
    internal static class PropArt
    {
        private static readonly Vector2 Hang = new(0.5f, 1f);
        private static readonly Color32 Ink = Paint.Rgb(8, 6, 18);
        private static readonly Color32 Cyan = Paint.Rgb(46, 242, 255);
        private static readonly Color32 Magenta = Paint.Rgb(255, 46, 166);
        private static readonly Color32 Amber = Paint.Rgb(255, 190, 70);
        private static readonly Color32 Red = Paint.Rgb(255, 60, 72);
        private static readonly Color32 Mint = Paint.Rgb(90, 255, 180);
        private static readonly Color32 White = Paint.Rgb(255, 255, 255);

        private static readonly Ramp Steel = Ramp.From(Paint.Rgb(104, 112, 132));
        private static readonly Ramp DarkSteel = Ramp.From(Paint.Rgb(56, 62, 80));
        private static readonly Ramp Rust = Ramp.From(Paint.Rgb(128, 72, 52));
        private static readonly Ramp Wood = Ramp.From(Paint.Rgb(126, 84, 54));
        private static readonly Ramp Leaf = Ramp.From(Paint.Rgb(60, 150, 84));
        private static readonly Ramp Gold = Ramp.From(Paint.Rgb(222, 176, 84));
        private static readonly Ramp Marble = Ramp.From(Paint.Rgb(196, 192, 204), 0.6f);
        private static readonly Ramp Plastic = Ramp.From(Paint.Rgb(178, 184, 196), 0.7f);

        /// <summary>Every decor prop and lamp, by prefab name.</summary>
        public static readonly DecorSpec[] Decor =
        {
            // Rooftops
            new() { Prefab = "Decor_WaterTower", Sprite = "decor_watertower", Paint = WaterTower },
            new() { Prefab = "Decor_Antennas", Sprite = "decor_antennas", Paint = Antennas, Glow = new Color(1f, 0.2f, 0.25f), GlowAt = new Vector2(-0.03f, 2.85f), GlowRadius = 1.4f },
            new() { Prefab = "Decor_Vent", Sprite = "decor_vent", Paint = Vent },
            new() { Prefab = "Decor_SatDish", Sprite = "decor_satdish", Paint = SatDish },
            new() { Prefab = "Decor_Billboard", Sprite = "decor_billboard", Paint = Billboard, Glow = new Color(1f, 0.3f, 0.8f), GlowAt = new Vector2(0f, 1.6f), GlowRadius = 4.5f, GlowIntensity = 0.9f },
            new() { Prefab = "Decor_RoofShack", Sprite = "decor_roofshack", Paint = RoofShack, Glow = new Color(1f, 0.8f, 0.5f), GlowAt = new Vector2(-0.1f, 1.25f), GlowRadius = 2.4f },
            // Office tower
            new() { Prefab = "Decor_Desk", Sprite = "decor_desk", Paint = Desk, Glow = new Color(0.4f, 0.9f, 1f), GlowAt = new Vector2(-0.4f, 1f), GlowRadius = 1.4f, GlowIntensity = 0.5f },
            new() { Prefab = "Decor_Plant", Sprite = "decor_plant", Paint = Plant },
            new() { Prefab = "Decor_Cabinet", Sprite = "decor_cabinet", Paint = Cabinet },
            new() { Prefab = "Decor_Cooler", Sprite = "decor_cooler", Paint = Cooler },
            new() { Prefab = "Decor_Vending", Sprite = "decor_vending", Paint = Vending, Glow = new Color(1f, 0.45f, 0.4f), GlowAt = new Vector2(0f, 1.1f), GlowRadius = 2f },
            new() { Prefab = "Decor_Printer", Sprite = "decor_printer", Paint = Printer },
            new() { Prefab = "Decor_Chair", Sprite = "decor_chair", Paint = Chair },
            new() { Prefab = "Decor_Clock", Sprite = "decor_clock", Paint = Clock, Wall = true, Pivot = new Vector2(0.5f, 0.5f) },
            new() { Prefab = "Decor_Whiteboard", Sprite = "decor_whiteboard", Paint = Whiteboard, Wall = true, Pivot = new Vector2(0.5f, 0.5f) },
            new() { Prefab = "Decor_Poster", Sprite = "decor_poster", Paint = Poster, Wall = true, Pivot = new Vector2(0.5f, 0.5f) },
            // Server farm
            new() { Prefab = "Decor_Cooling", Sprite = "decor_cooling", Paint = Cooling, Glow = new Color(0.3f, 0.9f, 1f), GlowAt = new Vector2(0f, 1.2f), GlowRadius = 1.6f, GlowIntensity = 0.5f },
            new() { Prefab = "Decor_RackOpen", Sprite = "decor_rackopen", Paint = RackOpen, Glow = new Color(0.3f, 1f, 0.7f), GlowAt = new Vector2(0f, 1.6f), GlowRadius = 2f, GlowIntensity = 0.45f },
            new() { Prefab = "Decor_StatusScreen", Sprite = "decor_statusscreen", Paint = StatusScreen, Glow = new Color(0.3f, 1f, 0.8f), GlowAt = new Vector2(0f, 0.9f), GlowRadius = 1.8f, GlowIntensity = 0.6f },
            new() { Prefab = "Decor_CableSpool", Sprite = "decor_cablespool", Paint = CableSpool },
            // Sewers
            new() { Prefab = "Decor_Barrel", Sprite = "decor_barrel", Paint = Barrel, Glow = new Color(0.5f, 1f, 0.3f), GlowAt = new Vector2(0f, 1f), GlowRadius = 1.3f, GlowIntensity = 0.6f },
            new() { Prefab = "Decor_Valve", Sprite = "decor_valve", Paint = Valve },
            new() { Prefab = "Decor_PipeStack", Sprite = "decor_pipestack", Paint = PipeStack },
            new() { Prefab = "Decor_Ladder", Sprite = "decor_ladder", Paint = Ladder },
            new() { Prefab = "Decor_Junk", Sprite = "decor_junk", Paint = Junk },
            new() { Prefab = "Decor_PipeVertical", Sprite = "decor_pipevertical", Paint = PipeVertical, Wall = true },
            new() { Prefab = "Decor_HangingMoss", Sprite = "decor_hangingmoss", Paint = HangingMoss, Ceiling = true, Pivot = new Vector2(0.5f, 1f) },
            // Corporate HQ
            new() { Prefab = "Decor_Statue", Sprite = "decor_statue", Paint = Statue },
            new() { Prefab = "Decor_PlantTall", Sprite = "decor_planttall", Paint = PlantTall },
            new() { Prefab = "Decor_Bench", Sprite = "decor_bench", Paint = Bench },
            new() { Prefab = "Decor_Reception", Sprite = "decor_reception", Paint = Reception, Glow = new Color(1f, 0.85f, 0.55f), GlowAt = new Vector2(0f, 1.1f), GlowRadius = 2.2f, GlowIntensity = 0.5f },
            new() { Prefab = "Decor_HoloLogo", Sprite = "decor_hologo", Paint = HoloLogo, Hologram = true, Glow = new Color(0.4f, 0.9f, 1f), GlowAt = new Vector2(0f, 0.75f), GlowRadius = 2f },
            new() { Prefab = "Decor_ArtPanel", Sprite = "decor_artpanel", Paint = ArtPanel, Wall = true, Pivot = new Vector2(0.5f, 0.5f) },
            new() { Prefab = "Decor_Screen", Sprite = "decor_screen", Paint = AdScreen, Wall = true, Pivot = new Vector2(0.5f, 0.5f), Glow = new Color(1f, 0.4f, 0.8f), GlowAt = Vector2.zero, GlowRadius = 2.2f, GlowIntensity = 0.6f },
            // Hideout
            new() { Prefab = "Decor_Sofa", Sprite = "decor_sofa", Paint = Sofa },
            new() { Prefab = "Decor_Workbench", Sprite = "decor_workbench", Paint = Workbench, Glow = new Color(0.4f, 1f, 0.9f), GlowAt = new Vector2(0.4f, 1.2f), GlowRadius = 1.8f, GlowIntensity = 0.6f },
            new() { Prefab = "Decor_Rig", Sprite = "decor_rig", Paint = HackerRig, Glow = new Color(0.3f, 0.9f, 1f), GlowAt = new Vector2(0f, 1.4f), GlowRadius = 2.6f, GlowIntensity = 0.8f },
            new() { Prefab = "Decor_Graffiti", Sprite = "decor_graffiti", Paint = Graffiti, Wall = true, Pivot = new Vector2(0.5f, 0.5f) },
            // Halden antenna
            new() { Prefab = "Decor_Windsock", Sprite = "decor_windsock", Paint = Windsock, Glow = new Color(1f, 0.2f, 0.25f), GlowAt = new Vector2(-0.53f, 2.47f), GlowRadius = 1.3f },
            new() { Prefab = "Decor_Transformer", Sprite = "decor_transformer", Paint = Transformer },
            new() { Prefab = "Decor_Radar", Sprite = "decor_radar", Paint = Radar, Glow = new Color(1f, 0.2f, 0.25f), GlowAt = new Vector2(0f, 2.22f), GlowRadius = 1.2f },
            new() { Prefab = "Decor_Mast", Sprite = "decor_mast", Paint = Mast, Glow = new Color(1f, 0.2f, 0.25f), GlowAt = new Vector2(0f, 5.47f), GlowRadius = 1.8f },
            // Lamps (light points down from the fixture)
            new() { Prefab = "StreetLamp", Sprite = "lamp_street", Paint = StreetLamp, Glow = new Color(1f, 0.85f, 0.55f), GlowAt = new Vector2(0.31f, 3.15f), GlowRadius = 7f, GlowIntensity = 1.4f, Spot = true },
            new() { Prefab = "Lamp_Office", Sprite = "lamp_office", Paint = OfficeLamp, Ceiling = true, Pivot = Hang, Glow = new Color(0.85f, 0.93f, 1f), GlowAt = new Vector2(0f, -0.3f), GlowRadius = 9f, GlowIntensity = 1.1f, Spot = true },
            new() { Prefab = "Lamp_Server", Sprite = "lamp_server", Paint = ServerLamp, Ceiling = true, Pivot = Hang, Glow = new Color(0.45f, 0.9f, 1f), GlowAt = new Vector2(0f, -0.35f), GlowRadius = 10f, GlowIntensity = 1.1f, Spot = true },
            new() { Prefab = "Lamp_Sewer", Sprite = "lamp_sewer", Paint = SewerLamp, Ceiling = true, Pivot = Hang, Glow = new Color(0.8f, 1f, 0.65f), GlowAt = new Vector2(0f, -0.65f), GlowRadius = 9f, GlowIntensity = 1.1f, Spot = true },
            new() { Prefab = "Lamp_Beacon", Sprite = "lamp_beacon", Paint = BeaconLamp, Glow = new Color(1f, 0.93f, 0.8f), GlowAt = new Vector2(0.31f, 2.9f), GlowRadius = 7f, GlowIntensity = 1.3f, Spot = true },
            new() { Prefab = "Lamp_Hq", Sprite = "lamp_hq", Paint = HqLamp, Ceiling = true, Pivot = Hang, Glow = new Color(1f, 0.9f, 0.7f), GlowAt = new Vector2(0f, -0.85f), GlowRadius = 9f, GlowIntensity = 1.2f, Spot = true },
        };

        private static PixelCanvas Done(PixelCanvas c) => c.SelectiveOutline(Ink, 0.3f);

        private static void Px(PixelCanvas c, float x, float y, Color32 color) => c.Set(Mathf.RoundToInt(x), Mathf.RoundToInt(y), color);

        private static void Screen(PixelCanvas c, int x, int y, int w, int h, Color32 glow, int seed, bool graph = false)
        {
            c.Rect(x - 1, y - 1, w + 2, h + 2, Paint.Rgb(16, 16, 26));
            c.DitherGradient(x, y, w, h, Paint.Shade(glow, 0.22f), Paint.Shade(glow, 0.42f), 3);
            var random = new System.Random(seed);
            if (graph)
            {
                int prev = y + h / 2;
                for (int xx = x + 1; xx < x + w - 1; xx++)
                {
                    int yy = Mathf.Clamp(prev + random.Next(3) - 1, y + 1, y + h - 2);
                    c.Line(xx - 1, prev, xx, yy, glow);
                    prev = yy;
                }
            }
            else
            {
                for (int line = y + h - 3; line > y; line -= 2)
                    c.Rect(x + 2, line, Mathf.Max(1, random.Next(w - 4)), 1, Paint.Mix(glow, White, 0.3f));
            }

            c.Blend(x + w - 2, y + h - 2, new Color(1f, 1f, 1f, 0.5f));
        }

        // ---------------------------------------------------------------- rooftops

        private static PixelCanvas WaterTower()
        {
            var c = new PixelCanvas(64, 100);
            foreach ((int bottom, int top) in new[] { (10, 16), (54, 48), (22, 24), (42, 40) })
                c.Line(bottom, 1, top, 44, DarkSteel.Dark, 2);
            c.Line(14, 14, 50, 30, DarkSteel.Deep).Line(50, 14, 14, 30, DarkSteel.Deep).Rect(13, 30, 38, 2, DarkSteel.Base);
            c.Cylinder(8, 44, 48, 38, Wood);
            for (int x = 10; x < 56; x += 5) c.Rect(x, 44, 1, 38, Wood.Deep);
            foreach (int hoop in new[] { 48, 62, 76 }) c.Cylinder(8, hoop, 48, 2, Steel.Darker(0.85f));
            var roof = new List<Vector2> { new(5, 82), new(59, 82), new(34, 97), new(30, 97) };
            c.Polygon(roof, DarkSteel.Base);
            c.Polygon(new List<Vector2> { new(5, 82), new(32, 82), new(32, 97), new(30, 97) }, DarkSteel.Light);
            c.Rect(31, 97, 2, 2, DarkSteel.Dark);
            c.Line(58, 44, 58, 82, Steel.Dark).Rect(56, 50, 4, 1, Steel.Base).Rect(56, 60, 4, 1, Steel.Base).Rect(56, 70, 4, 1, Steel.Base);
            c.Text("H2O", 22, 60, Paint.Shade(Wood.Light, 1.2f));
            return Done(c);
        }

        private static PixelCanvas Antennas()
        {
            var c = new PixelCanvas(32, 96);
            c.Bevel(9, 0, 14, 4, DarkSteel);
            c.Rect(15, 4, 2, 86, DarkSteel.Base).Rect(15, 4, 1, 86, DarkSteel.Light);
            for (int y = 8; y < 80; y += 8)
            {
                c.Line(12, y, 19, y + 8, DarkSteel.Dark);
                c.Line(19, y, 12, y + 8, DarkSteel.Dark);
            }

            c.Rect(11, 4, 1, 76, DarkSteel.Dark).Rect(20, 4, 1, 76, DarkSteel.Dark);
            foreach (int y in new[] { 40, 62 }) c.Rect(5, y, 22, 1, Steel.Base);
            c.Line(26, 62, 28, 70, Steel.Dark).Line(5, 40, 3, 50, Steel.Dark);
            c.Rect(24, 22, 6, 10, Steel.Base).Frame(24, 22, 6, 10, DarkSteel.Deep);
            c.Rect(14, 90, 4, 3, Red).Rect(15, 93, 2, 1, Paint.Rgb(255, 200, 200));
            c.Glow(16, 91, 5, new Color(1f, 0.2f, 0.25f, 0.6f));
            return Done(c);
        }

        private static PixelCanvas Vent()
        {
            var c = new PixelCanvas(32, 32);
            c.Bevel(5, 0, 22, 12, Steel);
            for (int y = 3; y < 10; y += 2) c.Rect(8, y, 16, 1, Steel.Deep);
            c.Cylinder(11, 12, 10, 9, Steel.Darker(0.9f));
            c.Dome(16, 22, 11, 4.5f, Steel);
            c.Rect(6, 20, 20, 1, Steel.Deep);
            c.Streaks(5, 0, 22, 12, Paint.Rgb(60, 40, 40), 4, 3);
            return Done(c);
        }

        private static PixelCanvas SatDish()
        {
            var c = new PixelCanvas(48, 48);
            c.Bevel(14, 0, 20, 5, DarkSteel);
            c.Cylinder(22, 5, 4, 18, DarkSteel);
            c.Dome(26, 30, 16, 13, Plastic);
            c.Ellipse(27, 31, 11, 9, Plastic.Dark);
            c.Dome(28, 32, 9, 7, Plastic.Darker(0.85f));
            c.Line(28, 32, 38, 40, DarkSteel.Dark).Line(22, 25, 38, 40, DarkSteel.Dark);
            c.Dome(38, 40, 2.4f, 2.4f, DarkSteel);
            c.Set(38, 41, Red);
            return Done(c);
        }

        private static PixelCanvas Billboard()
        {
            var c = new PixelCanvas(128, 84);
            foreach (int x in new[] { 26, 100 })
            {
                c.Cylinder(x, 0, 4, 28, DarkSteel);
                c.Line(x + 2, 4, x - 10, 26, DarkSteel.Dark);
            }

            c.Bevel(2, 26, 124, 4, DarkSteel);
            for (int x = 4; x < 124; x += 4) c.Rect(x, 30, 1, 3, DarkSteel.Dark);
            c.Bevel(4, 32, 120, 48, DarkSteel);
            c.DitherGradient(7, 35, 114, 42, Paint.Rgb(70, 16, 90), Paint.Rgb(230, 40, 150), 6);
            // A city silhouette + sun on the ad, then the slogan.
            c.Circle(92, 58, 13, Paint.Rgb(255, 200, 90));
            for (int i = 0; i < 6; i++) c.Rect(78, 50 + i * 3, 30, 1, Paint.Rgb(230, 70, 150));
            int bx = 7;
            var random = new System.Random(5);
            while (bx < 121)
            {
                int w = 5 + random.Next(9), h = 6 + random.Next(16);
                c.Rect(bx, 35, Mathf.Min(w, 121 - bx), h, Paint.Rgb(40, 10, 60));
                bx += w;
            }

            c.Text("HALDEN", 12, 62, White, 2);
            c.Text("CLOUD", 12, 52, Paint.Rgb(255, 220, 255));
            c.Text("NU DORMIM", 12, 44, Paint.Rgb(255, 170, 230));
            for (int x = 14; x < 124; x += 28)
            {
                c.Line(x, 80, x + 4, 82, DarkSteel.Base);
                c.Rect(x + 3, 81, 4, 2, Steel.Light);
            }

            return Done(c);
        }

        private static PixelCanvas RoofShack()
        {
            var c = new PixelCanvas(64, 64);
            Ramp wall = Ramp.From(Paint.Rgb(96, 90, 110));
            c.Bevel(4, 0, 56, 50, wall);
            c.Speckle(4, 0, 56, 50, wall.Dark, 0.05f, 3);
            c.Bevel(1, 50, 62, 6, DarkSteel);
            c.Rect(1, 49, 62, 1, Paint.Rgb(26, 20, 34));
            c.Bevel(20, 0, 16, 34, Steel.Darker(0.8f));
            c.Rect(24, 22, 8, 6, Paint.Rgb(255, 210, 140)).Frame(24, 22, 8, 6, DarkSteel.Deep);
            c.Set(33, 15, Amber);
            c.Bevel(18, 38, 20, 3, DarkSteel);
            c.Rect(26, 36, 4, 2, Paint.Rgb(255, 230, 170));
            c.Glow(28, 34, 9, new Color(1f, 0.85f, 0.5f, 0.35f));
            c.Text("EXIT", 42, 40, Mint);
            c.Line(8, 10, 14, 26, Magenta).Line(14, 26, 12, 12, Magenta).Line(42, 12, 54, 14, Cyan).Line(44, 18, 52, 8, Cyan);
            c.Cylinder(52, 20, 4, 40, Steel.Darker(0.9f));
            c.Streaks(4, 0, 56, 49, Paint.Rgb(40, 30, 44), 9, 5);
            return Done(c);
        }

        // ---------------------------------------------------------------- office

        private static PixelCanvas Desk()
        {
            var c = new PixelCanvas(64, 46);
            Ramp laminate = Ramp.From(Paint.Rgb(150, 112, 80), 0.7f);
            c.Bevel(2, 0, 3, 20, DarkSteel).Bevel(36, 0, 24, 20, Ramp.From(Paint.Rgb(120, 124, 140)));
            for (int y = 4; y < 18; y += 6) c.Rect(38, y, 20, 1, DarkSteel.Dark).Rect(46, y + 2, 4, 1, Steel.Light);
            c.Bevel(0, 20, 64, 4, laminate);
            c.Bevel(18, 24, 4, 4, DarkSteel).Bevel(12, 24, 16, 1, DarkSteel);
            c.Bevel(6, 27, 28, 18, Ramp.From(Paint.Rgb(40, 42, 54)));
            Screen(c, 8, 29, 24, 14, Cyan, 7);
            c.Bevel(34, 24, 16, 2, Plastic);
            for (int x = 35; x < 49; x += 2) c.Set(x, 25, Plastic.Deep);
            c.Cylinder(54, 24, 5, 6, Ramp.From(Paint.Rgb(230, 230, 236)));
            c.Rect(59, 26, 1, 2, Paint.Rgb(200, 200, 210));
            return Done(c);
        }

        private static PixelCanvas Plant()
        {
            var c = new PixelCanvas(28, 44);
            var random = new System.Random(4);
            for (int i = 0; i < 9; i++)
            {
                float angle = -70f + i * 17f + (float)random.NextDouble() * 8f;
                float length = 14f + (float)random.NextDouble() * 12f;
                Vector2 tip = new Vector2(14f, 12f) + new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad)) * length;
                c.Capsule(new Vector2(14f, 12f), tip, 1.2f, 2.6f, i % 3 == 0 ? Leaf.Darker(0.8f) : Leaf);
            }

            c.Polygon(new List<Vector2> { new(6, 13), new(22, 13), new(19, 1), new(9, 1) }, Paint.Rgb(190, 96, 70));
            c.Rect(6, 11, 16, 2, Paint.Rgb(220, 130, 96)).Rect(9, 1, 2, 10, Paint.Rgb(150, 70, 54));
            return Done(c);
        }

        private static PixelCanvas Cabinet()
        {
            var c = new PixelCanvas(30, 44);
            Ramp metal = Ramp.From(Paint.Rgb(130, 136, 150));
            c.Bevel(2, 0, 26, 42, metal);
            for (int i = 0; i < 3; i++)
            {
                int y = 2 + i * 13;
                c.Bevel(4, y, 22, 12, metal, 1);
                c.Rect(11, y + 7, 8, 2, metal.Deep).Rect(11, y + 8, 8, 1, metal.Light);
                c.Rect(12, y + 3, 6, 2, Paint.Rgb(230, 226, 210));
            }

            c.Rect(8, 42, 8, 2, Paint.Rgb(230, 226, 210)).Rect(18, 42, 5, 1, Paint.Rgb(200, 190, 160));
            return Done(c);
        }

        private static PixelCanvas Cooler()
        {
            var c = new PixelCanvas(22, 48);
            c.Bevel(3, 0, 16, 26, Plastic);
            c.Rect(6, 14, 4, 2, Red).Rect(12, 14, 4, 2, Cyan).Rect(5, 6, 12, 4, Plastic.Dark);
            c.Cylinder(4, 26, 14, 18, Ramp.From(Paint.Rgb(120, 190, 240, 255)));
            c.Rect(4, 36, 14, 1, Paint.Rgb(170, 220, 255));
            c.Dome(11, 44, 7, 2.5f, Ramp.From(Paint.Rgb(110, 180, 230)));
            c.Rect(8, 28, 1, 12, Paint.Rgb(220, 245, 255));
            return Done(c);
        }

        private static PixelCanvas Vending()
        {
            var c = new PixelCanvas(42, 66);
            Ramp body = Ramp.From(Paint.Rgb(170, 40, 54));
            c.Bevel(2, 0, 38, 64, body);
            c.Rect(5, 16, 24, 44, Paint.Rgb(20, 18, 30));
            c.DitherGradient(6, 17, 22, 42, Paint.Rgb(40, 50, 80), Paint.Rgb(120, 150, 200), 4);
            Color32[] cans = { Cyan, Amber, Mint, Magenta, Paint.Rgb(255, 120, 60) };
            for (int row = 0; row < 5; row++)
            {
                c.Rect(6, 19 + row * 8, 22, 1, Paint.Rgb(200, 210, 230));
                for (int col = 0; col < 5; col++) c.Rect(7 + col * 4, 20 + row * 8, 3, 5, cans[(row + col * 2) % cans.Length]);
            }

            c.Blend(8, 56, new Color(1f, 1f, 1f, 0.4f));
            c.Line(8, 30, 20, 58, Paint.Rgb(200, 220, 255, 255));
            c.Bevel(31, 30, 7, 26, body.Darker(0.7f));
            for (int i = 0; i < 4; i++) c.Rect(33, 46 - i * 4, 3, 2, Paint.Rgb(230, 230, 240));
            c.Rect(32, 34, 5, 3, Paint.Rgb(30, 30, 40)).Rect(33, 35, 3, 1, Mint);
            c.Rect(6, 4, 22, 8, Paint.Rgb(16, 14, 22)).Rect(7, 5, 20, 1, Paint.Rgb(60, 60, 70));
            c.Text("SODA", 9, 58, White);
            return Done(c);
        }

        private static PixelCanvas Printer()
        {
            var c = new PixelCanvas(40, 34);
            c.Bevel(4, 0, 32, 16, Ramp.From(Paint.Rgb(90, 94, 110)));
            c.Bevel(2, 16, 36, 12, Plastic);
            c.Rect(6, 24, 22, 2, Plastic.Deep).Rect(8, 28, 18, 4, Paint.Rgb(240, 240, 236)).Rect(8, 28, 18, 1, Paint.Rgb(200, 200, 196));
            c.Rect(30, 20, 5, 4, Paint.Rgb(40, 44, 56)).Set(31, 22, Mint);
            c.Set(33, 22, Amber);
            return Done(c);
        }

        private static PixelCanvas Chair()
        {
            var c = new PixelCanvas(26, 36);
            Ramp fabric = Ramp.From(Paint.Rgb(50, 58, 80));
            c.Rect(5, 1, 16, 2, DarkSteel.Dark).Rect(4, 0, 3, 2, DarkSteel.Deep).Rect(19, 0, 3, 2, DarkSteel.Deep);
            c.Cylinder(12, 3, 3, 10, Steel);
            c.Capsule(new Vector2(5, 14), new Vector2(20, 14), 2.6f, 2.6f, fabric);
            c.Capsule(new Vector2(7, 18), new Vector2(5, 32), 3f, 3.6f, fabric);
            c.Line(20, 17, 20, 13, DarkSteel.Base).Rect(16, 18, 5, 1, DarkSteel.Base);
            return Done(c);
        }

        private static PixelCanvas Clock()
        {
            var c = new PixelCanvas(18, 18);
            c.Dome(9, 9, 8, 8, Steel);
            c.Circle(9, 9, 6.5f, Paint.Rgb(236, 236, 230));
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f * Mathf.Deg2Rad;
                Px(c, 8.5f + Mathf.Cos(a) * 5.3f, 8.5f + Mathf.Sin(a) * 5.3f, Paint.Rgb(60, 60, 70));
            }

            c.Line(9, 9, 9, 13, Paint.Rgb(20, 20, 30)).Line(9, 9, 12, 8, Paint.Rgb(20, 20, 30)).Set(9, 9, Red);
            return Done(c);
        }

        private static PixelCanvas Whiteboard()
        {
            var c = new PixelCanvas(66, 42);
            c.Bevel(1, 1, 64, 40, Steel);
            c.Rect(3, 4, 60, 35, Paint.Rgb(234, 238, 242));
            c.Rect(3, 4, 60, 1, Paint.Rgb(200, 206, 214));
            c.Frame(8, 22, 14, 10, Paint.Rgb(40, 90, 200)).Frame(32, 26, 14, 8, Paint.Rgb(40, 90, 200)).Frame(32, 10, 14, 8, Paint.Rgb(200, 50, 60));
            c.Line(22, 27, 32, 30, Paint.Rgb(40, 40, 50)).Line(22, 25, 32, 14, Paint.Rgb(40, 40, 50));
            c.Text("Q4", 11, 25, Paint.Rgb(40, 90, 200)).Text("KPI", 34, 28, Paint.Rgb(40, 90, 200));
            c.Line(50, 8, 54, 16, Paint.Rgb(30, 140, 80)).Line(54, 16, 57, 12, Paint.Rgb(30, 140, 80)).Line(57, 12, 61, 30, Paint.Rgb(30, 140, 80));
            c.Bevel(14, 0, 30, 3, DarkSteel);
            c.Rect(18, 2, 5, 1, Red).Rect(26, 2, 5, 1, Paint.Rgb(40, 90, 200));
            return Done(c);
        }

        private static PixelCanvas Poster()
        {
            var c = new PixelCanvas(32, 44);
            c.Rect(1, 1, 30, 42, Paint.Rgb(230, 226, 214));
            c.DitherGradient(3, 12, 26, 29, Paint.Rgb(30, 60, 120), Paint.Rgb(70, 150, 220), 4);
            c.Polygon(new List<Vector2> { new(3, 12), new(14, 30), new(20, 22), new(29, 34), new(29, 12) }, Paint.Rgb(30, 40, 70));
            c.Polygon(new List<Vector2> { new(11, 25), new(14, 30), new(17, 26) }, White);
            c.Text("LOIAL", 6, 5, Paint.Rgb(30, 30, 40));
            c.Rect(1, 42, 2, 2, Steel.Light).Rect(29, 42, 2, 2, Steel.Light);
            return Done(c);
        }

        // ---------------------------------------------------------------- server farm

        private static PixelCanvas Cooling()
        {
            var c = new PixelCanvas(50, 66);
            Ramp body = Ramp.From(Paint.Rgb(70, 82, 104));
            c.Bevel(2, 0, 46, 64, body);
            c.Dome(25, 38, 17, 17, DarkSteel.Darker(0.6f));
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                c.Capsule(new Vector2(25, 38), new Vector2(25 + Mathf.Cos(a) * 13f, 38 + Mathf.Sin(a) * 13f), 1.2f, 3.2f, Steel.Darker(0.8f));
            }

            c.Dome(25, 38, 4, 4, Steel);
            for (int y = 22; y <= 54; y += 4) c.Rect(9, y, 32, 1, Paint.Rgb(30, 34, 46));
            for (int x = 9; x <= 41; x += 4) c.Rect(x, 22, 1, 32, Paint.Rgb(30, 34, 46));
            c.Rect(6, 6, 38, 10, Paint.Rgb(24, 28, 38));
            for (int x = 8; x < 42; x += 3) c.Rect(x, 7, 1, 8, Paint.Rgb(50, 58, 76));
            c.Set(8, 59, Mint);
            c.Set(11, 59, Cyan);
            c.Set(14, 59, Amber);
            c.Text("CRAC-2", 20, 57, Paint.Rgb(170, 200, 230));
            return Done(c);
        }

        private static PixelCanvas RackOpen()
        {
            var c = new PixelCanvas(50, 98);
            c.Bevel(2, 0, 46, 96, DarkSteel);
            c.Rect(5, 3, 40, 90, Paint.Rgb(14, 16, 24));
            var random = new System.Random(12);
            for (int y = 6; y < 88; y += 7)
            {
                c.Bevel(7, y, 36, 5, Ramp.From(Paint.Rgb(46, 52, 68)), 1);
                for (int l = 0; l < 3; l++)
                    if (random.NextDouble() < 0.75) c.Set(9 + l * 2, y + 2, random.NextDouble() < 0.7 ? Mint : random.NextDouble() < 0.6 ? Cyan : Red);
                c.Rect(32, y + 2, 9, 1, Paint.Rgb(80, 90, 110));
            }

            Color32[] cable = { Cyan, Magenta, Amber, Paint.Rgb(70, 76, 96) };
            for (int i = 0; i < 4; i++)
            {
                int x0 = 36 + i * 2, y0 = 80 - i * 14;
                for (int t = 0; t <= 20; t++)
                {
                    float f = t / 20f;
                    Px(c, x0 - f * (10 + i * 3), y0 - Mathf.Sin(f * Mathf.PI) * (8 + i * 2) - f * 6, cable[i]);
                }
            }

            return Done(c);
        }

        private static PixelCanvas StatusScreen()
        {
            var c = new PixelCanvas(48, 42);
            c.Cylinder(22, 0, 4, 12, DarkSteel);
            c.Bevel(14, 0, 20, 3, DarkSteel);
            c.Bevel(2, 12, 44, 28, Ramp.From(Paint.Rgb(40, 44, 58)));
            Screen(c, 5, 15, 38, 22, Mint, 3, graph: true);
            c.Text("CPU", 7, 30, Mint);
            c.Rect(30, 31, 10, 3, Paint.Rgb(255, 80, 90)).Rect(30, 31, 7, 3, Amber);
            return Done(c);
        }

        /// <summary>Cable drum seen from the side: two wooden flanges with the cable wound between them.</summary>
        private static PixelCanvas CableSpool()
        {
            var c = new PixelCanvas(36, 28);
            Ramp cable = Ramp.From(Paint.Rgb(40, 140, 200));
            c.Pipe(8, 4, 20, 18, cable);
            for (int x = 9; x < 28; x += 2) c.Rect(x, 4, 1, 18, cable.Dark);
            c.Dome(6, 13, 3.4f, 12.5f, Wood);
            c.Dome(30, 13, 3.4f, 12.5f, Wood);
            c.Dome(30, 13, 1.4f, 1.6f, Wood.Darker(0.6f));
            c.Line(26, 4, 34, 1, cable.Base);
            return Done(c);
        }

        // ---------------------------------------------------------------- sewers

        private static PixelCanvas Barrel()
        {
            var c = new PixelCanvas(26, 34);
            c.Cylinder(3, 0, 20, 30, Ramp.From(Paint.Rgb(200, 160, 40)));
            foreach (int y in new[] { 4, 15, 26 }) c.Cylinder(3, y, 20, 2, Ramp.From(Paint.Rgb(120, 96, 30)));
            c.Polygon(new List<Vector2> { new(9, 9), new(17, 9), new(13, 16) }, Paint.Rgb(30, 26, 20));
            c.Set(13, 11, Paint.Rgb(200, 160, 40));
            c.Dome(13, 31, 10, 2.5f, Ramp.From(Paint.Rgb(90, 220, 60)));
            c.Rect(10, 24, 2, 6, Paint.Rgb(110, 230, 70)).Rect(16, 22, 2, 8, Paint.Rgb(110, 230, 70));
            c.Streaks(3, 0, 20, 30, Paint.Rgb(90, 50, 30), 5, 4);
            return Done(c);
        }

        private static PixelCanvas Valve()
        {
            var c = new PixelCanvas(34, 44);
            c.Cylinder(12, 0, 10, 44, Rust);
            c.Bevel(9, 18, 16, 4, Rust.Darker(0.8f));
            c.Cylinder(15, 22, 4, 6, Steel);
            Ramp wheel = Ramp.From(Paint.Rgb(200, 50, 50));
            for (int i = 0; i < 24; i++)
            {
                float a = i / 24f * Mathf.PI * 2f;
                c.Dome(17 + Mathf.Cos(a) * 10f, 32 + Mathf.Sin(a) * 4f, 1.6f, 1.6f, wheel);
            }

            c.Line(7, 32, 27, 32, wheel.Dark).Line(17, 28, 17, 36, wheel.Dark);
            c.Dome(17, 32, 2.2f, 2.2f, Steel);
            return Done(c);
        }

        private static PixelCanvas PipeStack()
        {
            var c = new PixelCanvas(66, 34);
            Ramp[] colours = { Rust, Ramp.From(Paint.Rgb(70, 100, 80)), Ramp.From(Paint.Rgb(90, 96, 110)) };
            int[] widths = { 64, 56, 44 };
            for (int i = 0; i < 3; i++)
            {
                int y = 1 + i * 10, w = widths[i];
                c.Pipe(1, y, w, 10, colours[i]);
                c.Pipe(1 + w - 6, y - 1, 4, 12, colours[i].Darker(0.85f));
                c.Pipe(4, y - 1, 3, 12, colours[i].Darker(0.85f));
            }

            c.Streaks(1, 1, 64, 30, Paint.Rgb(50, 80, 50), 12, 5);
            return Done(c);
        }

        private static PixelCanvas Ladder()
        {
            var c = new PixelCanvas(26, 98);
            c.Cylinder(3, 0, 3, 97, Rust).Cylinder(20, 0, 3, 97, Rust);
            for (int y = 4; y < 96; y += 7) c.Pipe(6, y, 14, 2, Rust.Darker(0.9f));
            c.Streaks(3, 0, 20, 97, Paint.Rgb(70, 110, 60), 4, 4);
            return Done(c);
        }

        private static PixelCanvas Junk()
        {
            var c = new PixelCanvas(50, 26);
            Ramp rubber = Ramp.From(Paint.Rgb(40, 40, 46));
            c.Dome(12, 11, 10, 10, rubber);
            c.Dome(12, 11, 4.5f, 4.5f, Ramp.From(Paint.Rgb(70, 74, 80)));
            c.Ellipse(12, 11, 2.5f, 2.5f, Paint.Rgb(20, 20, 24));
            c.Capsule(new Vector2(20, 2), new Vector2(46, 9), 2f, 2f, Wood.Darker(0.8f));
            c.Capsule(new Vector2(26, 9), new Vector2(42, 3), 1.6f, 1.6f, Wood);
            c.Capsule(new Vector2(40, 2), new Vector2(44, 14), 1.4f, 1.4f, Ramp.From(Paint.Rgb(60, 140, 90)));
            c.Rect(42, 14, 2, 2, Paint.Rgb(200, 220, 200));
            return Done(c);
        }

        private static PixelCanvas PipeVertical()
        {
            var c = new PixelCanvas(18, 130);
            Ramp pipe = Ramp.From(Paint.Rgb(78, 96, 84));
            c.Cylinder(4, 0, 10, 130, pipe);
            for (int y = 12; y < 128; y += 30) c.Cylinder(2, y, 14, 4, pipe.Darker(0.8f));
            c.Streaks(4, 0, 10, 130, Paint.Rgb(60, 110, 60), 3, 3);
            return Done(c);
        }

        private static PixelCanvas HangingMoss()
        {
            var c = new PixelCanvas(34, 50);
            Ramp moss = Ramp.From(Paint.Rgb(70, 140, 70));
            var random = new System.Random(8);
            for (int x = 2; x < 32; x++)
            {
                int length = 6 + random.Next(random.NextDouble() < 0.3 ? 40 : 18);
                for (int d = 0; d < length; d++)
                    c.Set(x, 49 - d, d < 3 ? moss.Light : d > length - 3 ? moss.Deep : (x + d) % 5 == 0 ? moss.Dark : moss.Base);
                if (random.NextDouble() < 0.15) c.Set(x, 49 - length - 2, Paint.Rgb(140, 230, 160));
            }

            return Done(c);
        }

        // ---------------------------------------------------------------- corporate HQ

        private static PixelCanvas Statue()
        {
            var c = new PixelCanvas(42, 98);
            c.Bevel(5, 0, 32, 24, Marble);
            c.Rect(5, 22, 32, 2, Gold.Base).Rect(5, 2, 32, 1, Gold.Dark);
            c.Text("HALDEN", 9, 9, Gold.Light);
            c.Cylinder(19, 24, 4, 20, Gold.Darker(0.8f));
            for (int i = 0; i < 40; i++)
            {
                float a = i / 40f * Mathf.PI * 2f;
                c.Dome(21 + Mathf.Cos(a) * 14f, 66 + Mathf.Sin(a) * 20f, 3.6f, 3.6f, Gold);
            }

            c.Dome(21, 66, 6, 6, Gold);
            c.Glow(21, 66, 8, new Color(1f, 0.9f, 0.6f, 0.35f));
            return Done(c);
        }

        private static PixelCanvas PlantTall()
        {
            var c = new PixelCanvas(34, 76);
            for (int i = 0; i < 4; i++)
            {
                int x = 10 + i * 4;
                c.Capsule(new Vector2(x, 14), new Vector2(x + (i - 1.5f) * 3f, 52 + i * 5), 1.2f, 1f, Ramp.From(Paint.Rgb(110, 150, 70)));
                for (int l = 0; l < 4; l++)
                {
                    float y = 30 + l * 10 + i * 3;
                    float side = (l + i) % 2 == 0 ? 1f : -1f;
                    c.Capsule(new Vector2(x + (i - 1.5f) * 3f * (y - 14) / 40f, y), new Vector2(x + side * 9f, y + 5f), 0.8f, 2.4f, Leaf);
                }
            }

            c.Cylinder(6, 0, 22, 16, Ramp.From(Paint.Rgb(40, 40, 50)));
            c.Rect(6, 14, 22, 2, Gold.Base);
            return Done(c);
        }

        private static PixelCanvas Bench()
        {
            var c = new PixelCanvas(66, 26);
            Ramp leather = Ramp.From(Paint.Rgb(120, 40, 50));
            c.Rect(6, 0, 3, 12, Gold.Dark).Rect(57, 0, 3, 12, Gold.Dark).Rect(6, 11, 54, 1, Gold.Base);
            c.Capsule(new Vector2(4, 15), new Vector2(62, 15), 3.6f, 3.6f, leather);
            for (int x = 10; x < 60; x += 8) c.Set(x, 16, leather.Deep);
            return Done(c);
        }

        private static PixelCanvas Reception()
        {
            var c = new PixelCanvas(98, 44);
            Ramp front = Ramp.From(Paint.Rgb(40, 40, 52));
            c.Bevel(2, 0, 94, 28, front);
            c.Bevel(0, 28, 98, 4, Marble);
            c.Rect(2, 12, 94, 2, Gold.Base).Rect(2, 13, 94, 1, Gold.Light);
            for (int i = 0; i < 6; i++)
            {
                float a = (i * 60f + 30f) * Mathf.Deg2Rad, b = ((i + 1) * 60f + 30f) * Mathf.Deg2Rad;
                c.Line(49 + Mathf.RoundToInt(Mathf.Cos(a) * 7), 20 + Mathf.RoundToInt(Mathf.Sin(a) * 7), 49 + Mathf.RoundToInt(Mathf.Cos(b) * 7), 20 + Mathf.RoundToInt(Mathf.Sin(b) * 7), Gold.Light);
            }

            c.Text("H", 48, 18, Gold.Light);
            c.Bevel(66, 32, 22, 12, Ramp.From(Paint.Rgb(30, 32, 40)));
            Screen(c, 68, 34, 18, 8, Paint.Rgb(255, 200, 120), 9);
            c.Cylinder(14, 32, 6, 8, Ramp.From(Paint.Rgb(230, 230, 236)));
            c.Dome(17, 41, 4, 3, Leaf);
            return Done(c);
        }

        private static PixelCanvas HoloLogo()
        {
            var c = new PixelCanvas(48, 48);
            c.Rect(14, 0, 20, 3, White).Rect(18, 3, 12, 2, Paint.Rgb(200, 200, 200));
            for (int ring = 0; ring < 2; ring++)
            {
                float r = 16f - ring * 5f;
                for (int i = 0; i < 6; i++)
                {
                    float a = (i * 60f + 30f) * Mathf.Deg2Rad, b = ((i + 1) * 60f + 30f) * Mathf.Deg2Rad;
                    c.Line(24 + Mathf.RoundToInt(Mathf.Cos(a) * r), 26 + Mathf.RoundToInt(Mathf.Sin(a) * r), 24 + Mathf.RoundToInt(Mathf.Cos(b) * r), 26 + Mathf.RoundToInt(Mathf.Sin(b) * r), White, ring == 0 ? 2 : 1);
                }
            }

            c.Text("H", 21, 22, White, 2);
            for (int y = 8; y < 44; y += 3) c.Rect(6, y, 36, 1, Paint.Rgb(255, 255, 255, 40));
            return c;
        }

        private static PixelCanvas ArtPanel()
        {
            var c = new PixelCanvas(66, 42);
            c.Bevel(1, 1, 64, 40, Gold);
            c.Rect(4, 4, 58, 34, Paint.Rgb(236, 228, 214));
            c.Polygon(new List<Vector2> { new(4, 4), new(30, 4), new(14, 30) }, Paint.Rgb(200, 50, 60));
            c.Circle(42, 24, 9, Paint.Rgb(30, 60, 140));
            c.Rect(26, 10, 30, 5, Paint.Rgb(240, 190, 60));
            c.Line(8, 34, 60, 8, Paint.Rgb(20, 20, 26), 2);
            return Done(c);
        }

        private static PixelCanvas AdScreen()
        {
            var c = new PixelCanvas(66, 38);
            c.Bevel(1, 1, 64, 36, Ramp.From(Paint.Rgb(30, 30, 40)));
            c.DitherGradient(4, 4, 58, 30, Paint.Rgb(60, 10, 80), Paint.Rgb(255, 60, 150), 5);
            c.Circle(54, 14, 7, Paint.Rgb(255, 210, 120));
            for (int i = 0; i < 3; i++) c.Rect(47, 9 + i * 3, 14, 1, Paint.Rgb(255, 90, 160));
            c.Text("HALDEN", 7, 23, White, 2);
            c.Text("VIITORUL TAU", 7, 8, Paint.Rgb(255, 220, 255));
            return Done(c);
        }

        // ---------------------------------------------------------------- hideout

        private static PixelCanvas Sofa()
        {
            var c = new PixelCanvas(74, 34);
            Ramp fabric = Ramp.From(Paint.Rgb(110, 60, 120));
            c.Rect(6, 0, 3, 5, DarkSteel.Dark).Rect(65, 0, 3, 5, DarkSteel.Dark);
            c.Capsule(new Vector2(10, 22), new Vector2(64, 22), 8f, 8f, fabric.Darker(0.85f));
            c.Capsule(new Vector2(8, 10), new Vector2(66, 10), 6f, 6f, fabric);
            c.Capsule(new Vector2(5, 8), new Vector2(5, 20), 4f, 4f, fabric);
            c.Capsule(new Vector2(69, 8), new Vector2(69, 20), 4f, 4f, fabric);
            c.Rect(37, 6, 1, 9, fabric.Deep);
            c.Dome(54, 21, 6, 5, Ramp.From(Paint.Rgb(46, 200, 220)));
            return Done(c);
        }

        private static PixelCanvas Workbench()
        {
            var c = new PixelCanvas(66, 50);
            c.Bevel(2, 0, 4, 22, Wood.Darker(0.7f)).Bevel(60, 0, 4, 22, Wood.Darker(0.7f));
            c.Bevel(0, 22, 66, 5, Wood);
            c.Rect(6, 8, 54, 2, Wood.Dark);
            c.Bevel(8, 27, 20, 10, DarkSteel);
            c.Rect(10, 29, 16, 6, Paint.Rgb(20, 60, 40));
            for (int i = 0; i < 6; i++) c.Set(11 + i * 3, 31 + i % 2 * 2, i % 2 == 0 ? Amber : Mint);
            c.Line(30, 27, 44, 33, Paint.Rgb(200, 60, 60)).Line(30, 28, 44, 34, Paint.Rgb(60, 120, 230));
            c.Bevel(44, 27, 14, 6, Steel);
            c.Bevel(48, 33, 3, 12, DarkSteel);
            c.Dome(53, 45, 5, 3, DarkSteel);
            c.Glow(53, 42, 7, new Color(0.5f, 1f, 0.9f, 0.4f));
            c.Bevel(2, 40, 40, 2, Wood.Darker(0.8f));
            foreach (int x in new[] { 6, 14, 22, 30 }) c.Rect(x, 42, 4, 6, x % 16 == 6 ? Rust.Base : Steel.Dark);
            return Done(c);
        }

        private static PixelCanvas HackerRig()
        {
            var c = new PixelCanvas(98, 64);
            c.Bevel(2, 0, 4, 24, DarkSteel).Bevel(92, 0, 4, 24, DarkSteel);
            c.Bevel(0, 24, 98, 4, Ramp.From(Paint.Rgb(50, 50, 64)));
            int[] xs = { 8, 36, 64 };
            for (int i = 0; i < xs.Length; i++)
            {
                int y = i == 1 ? 34 : 30;
                c.Bevel(xs[i], y, 26, 18, Ramp.From(Paint.Rgb(34, 36, 48)));
                Screen(c, xs[i] + 2, y + 2, 22, 14, i == 1 ? Mint : i == 0 ? Cyan : Magenta, 20 + i, graph: i == 2);
                c.Rect(xs[i] + 11, 28, 4, y - 28, DarkSteel.Base);
            }

            c.Bevel(36, 52, 26, 10, Ramp.From(Paint.Rgb(34, 36, 48)));
            Screen(c, 38, 54, 22, 6, Amber, 31);
            c.Bevel(30, 28, 38, 2, Plastic.Darker(0.5f));
            for (int x = 32; x < 66; x += 2) c.Set(x, 29, Cyan);
            c.Bevel(74, 0, 16, 24, Ramp.From(Paint.Rgb(40, 40, 56)));
            for (int y = 4; y < 22; y += 4) c.Rect(77, y, 10, 1, Paint.Rgb(20, 20, 30)).Set(78, y + 2, Mint);
            c.Cylinder(12, 28, 4, 5, Ramp.From(Paint.Rgb(230, 80, 90)));
            return Done(c);
        }

        private static PixelCanvas Graffiti()
        {
            var c = new PixelCanvas(80, 36);
            c.Text("ROOT", 4, 14, Magenta, 4);
            c.Text("ACCESS", 18, 4, Cyan, 2);
            for (int x = 4; x < 76; x += 7) c.Rect(x, 13 - x % 5, 1, 2 + x % 4, Magenta);
            c.Polygon(new List<Vector2> { new(68, 18), new(76, 28), new(72, 30), new(64, 22) }, Amber);
            return c;
        }

        // ---------------------------------------------------------------- lamps

        // ---------------------------------------------------------------- Halden antenna (Level 6)

        private static PixelCanvas Windsock()
        {
            var c = new PixelCanvas(52, 84);
            c.Bevel(3, 0, 12, 4, DarkSteel);
            c.Cylinder(7, 4, 4, 72, DarkSteel);
            c.Rect(5, 74, 8, 2, Steel.Base);
            // The sock tapers from the ring to the tail, in red/white bands, blown out to the right.
            for (int i = 0; i < 5; i++)
            {
                float f0 = i / 5f, f1 = (i + 1) / 5f;
                float x0 = 12f + f0 * 38f, x1 = 12f + f1 * 38f;
                float y0 = Mathf.Lerp(74f, 64f, f0), y1 = Mathf.Lerp(74f, 64f, f1);
                float h0 = Mathf.Lerp(6f, 2.5f, f0), h1 = Mathf.Lerp(6f, 2.5f, f1);
                Color32 band = i % 2 == 0 ? Red : Paint.Rgb(236, 236, 244);
                c.Polygon(new List<Vector2> { new(x0, y0 - h0), new(x1, y1 - h1), new(x1, y1 + h1), new(x0, y0 + h0) }, band);
                c.Line(Mathf.RoundToInt(x0), Mathf.RoundToInt(y0 - h0), Mathf.RoundToInt(x1), Mathf.RoundToInt(y1 - h1), Paint.Shade(band, 0.6f));
            }

            c.Rect(6, 78, 6, 3, Red);
            c.Glow(9, 79, 6, new Color(1f, 0.2f, 0.25f, 0.55f));
            return Done(c);
        }

        private static PixelCanvas Transformer()
        {
            var c = new PixelCanvas(60, 52);
            Ramp body = Ramp.From(Paint.Rgb(72, 98, 92));
            c.Bevel(4, 0, 52, 36, body);
            for (int x = 8; x < 52; x += 4) c.Rect(x, 4, 1, 24, body.Dark);
            c.Bevel(2, 36, 56, 3, DarkSteel);
            foreach (int x in new[] { 12, 27, 42 })
            {
                c.Cylinder(x, 39, 5, 9, Ramp.From(Paint.Rgb(206, 196, 176)));
                for (int y = 41; y < 48; y += 2) c.Rect(x, y, 5, 1, Paint.Rgb(150, 140, 126));
                c.Rect(x + 1, 48, 3, 2, Steel.Light);
            }

            c.Line(14, 50, 29, 50, DarkSteel.Deep).Line(29, 50, 44, 50, DarkSteel.Deep);
            c.Polygon(new List<Vector2> { new(22, 9), new(38, 9), new(30, 25) }, Amber);
            c.Rect(29, 15, 2, 6, Ink).Rect(29, 11, 2, 2, Ink);
            c.Text("PERICOL", 17, 29, Paint.Rgb(255, 230, 160));
            c.Streaks(4, 0, 52, 36, Paint.Rgb(40, 50, 46), 61, 4);
            return Done(c);
        }

        private static PixelCanvas Radar()
        {
            var c = new PixelCanvas(60, 76);
            c.Bevel(20, 0, 20, 5, DarkSteel);
            c.Cylinder(27, 5, 6, 40, DarkSteel);
            c.Dome(30, 47, 9, 5, Steel);
            c.Bevel(3, 52, 54, 11, Plastic);
            for (int x = 6; x < 55; x += 4) c.Rect(x, 54, 1, 7, Plastic.Dark);
            c.Rect(3, 63, 54, 2, DarkSteel.Base);
            c.Rect(29, 65, 2, 5, DarkSteel.Base);
            c.Rect(28, 70, 4, 3, Red);
            c.Glow(30, 71, 5, new Color(1f, 0.2f, 0.25f, 0.5f));
            return Done(c);
        }

        private static PixelCanvas Mast()
        {
            var c = new PixelCanvas(40, 180);
            c.Bevel(7, 0, 26, 5, DarkSteel);
            const int top = 168;
            float Left(float y) => Mathf.Lerp(10f, 18f, (y - 5f) / (top - 5f));
            float Right(float y) => Mathf.Lerp(30f, 22f, (y - 5f) / (top - 5f));
            for (int y = 8; y < top - 6; y += 14)
            {
                int y1 = Mathf.Min(top, y + 14);
                c.Line(Mathf.RoundToInt(Left(y)), y, Mathf.RoundToInt(Right(y1)), y1, DarkSteel.Dark);
                c.Line(Mathf.RoundToInt(Right(y)), y, Mathf.RoundToInt(Left(y1)), y1, DarkSteel.Dark);
                c.Line(Mathf.RoundToInt(Left(y)), y, Mathf.RoundToInt(Right(y)), y, DarkSteel.Base);
            }

            c.Line(10, 5, 18, top, DarkSteel.Base, 2).Line(30, 5, 22, top, DarkSteel.Base, 2);
            c.Line(11, 5, 19, top, DarkSteel.Light);
            c.Rect(19, top, 2, 8, DarkSteel.Base);
            foreach (int y in new[] { 92, top + 7 })
            {
                c.Rect(18, y, 4, 3, Red);
                c.Glow(20, y + 1, 6, new Color(1f, 0.2f, 0.25f, 0.55f));
            }

            return Done(c);
        }

        private static PixelCanvas BeaconLamp()
        {
            var c = new PixelCanvas(34, 116);
            c.Bevel(10, 0, 12, 5, DarkSteel);
            c.Cylinder(14, 5, 4, 96, DarkSteel);
            c.Rect(12, 40, 8, 3, Paint.Rgb(236, 236, 244)).Rect(12, 46, 8, 3, Red);
            c.Bevel(12, 99, 18, 3, DarkSteel);
            // Two floodlight boxes tilted down towards the deck.
            c.Polygon(new List<Vector2> { new(20, 92), new(32, 95), new(31, 102), new(19, 99) }, DarkSteel.Base);
            c.Line(21, 92, 32, 95, Paint.Rgb(255, 240, 210), 2);
            c.Glow(27, 93, 6, new Color(1f, 0.92f, 0.75f, 0.5f));
            c.Rect(14, 108, 5, 4, Red);
            c.Glow(16, 110, 6, new Color(1f, 0.2f, 0.25f, 0.6f));
            return Done(c);
        }

        private static PixelCanvas StreetLamp()
        {
            var c = new PixelCanvas(34, 112);
            c.Bevel(10, 0, 12, 5, DarkSteel);
            c.Cylinder(14, 5, 4, 95, DarkSteel);
            c.Cylinder(13, 30, 6, 3, DarkSteel.Darker(0.8f));
            for (int t = 0; t <= 14; t++)
            {
                float f = t / 14f;
                c.Dome(16 + f * 12f, 99 + Mathf.Sin(f * Mathf.PI) * 6f - f * 2f, 1.6f, 1.6f, DarkSteel);
            }

            c.Capsule(new Vector2(22, 101), new Vector2(32, 99), 2.6f, 2.2f, DarkSteel);
            c.Rect(23, 97, 9, 2, Paint.Rgb(255, 236, 190));
            c.Glow(27, 96, 7, new Color(1f, 0.85f, 0.55f, 0.5f));
            c.Bevel(16, 60, 10, 14, Ramp.From(Paint.Rgb(200, 40, 120)));
            c.Text("7", 19, 64, White);
            return Done(c);
        }

        private static PixelCanvas OfficeLamp()
        {
            var c = new PixelCanvas(50, 16);
            c.Rect(8, 6, 1, 10, DarkSteel.Dark).Rect(41, 6, 1, 10, DarkSteel.Dark);
            c.Bevel(1, 3, 48, 4, Plastic);
            c.Rect(4, 1, 42, 2, Paint.Rgb(240, 250, 255));
            c.Rect(4, 1, 42, 1, Paint.Rgb(200, 230, 255));
            return Done(c);
        }

        private static PixelCanvas ServerLamp()
        {
            var c = new PixelCanvas(34, 16);
            c.Rect(16, 6, 2, 10, DarkSteel.Dark);
            c.Bevel(1, 3, 32, 4, DarkSteel);
            c.Rect(3, 1, 28, 2, Paint.Rgb(140, 240, 255));
            return Done(c);
        }

        private static PixelCanvas SewerLamp()
        {
            var c = new PixelCanvas(18, 26);
            c.Rect(8, 14, 2, 12, Paint.Rgb(30, 30, 30));
            c.Bevel(4, 12, 10, 3, Rust);
            c.Dome(9, 8, 4, 5, Ramp.From(Paint.Rgb(255, 240, 180)));
            for (int x = 4; x <= 14; x += 3) c.Line(x, 12, 9, 2, Rust.Dark);
            c.Glow(9, 8, 8, new Color(0.85f, 1f, 0.7f, 0.4f));
            return c.SelectiveOutline(Ink, 0.3f);
        }

        private static PixelCanvas HqLamp()
        {
            var c = new PixelCanvas(42, 34);
            c.Rect(20, 14, 2, 20, Gold.Dark);
            c.Polygon(new List<Vector2> { new(3, 6), new(39, 6), new(30, 16), new(12, 16) }, Gold.Base);
            c.Polygon(new List<Vector2> { new(3, 6), new(21, 6), new(21, 16), new(12, 16) }, Gold.Light);
            c.Rect(6, 4, 30, 2, Paint.Rgb(255, 244, 210));
            for (int i = 0; i < 5; i++) c.Rect(8 + i * 6, 1, 2, 3, Paint.Rgb(255, 230, 180, 200));
            return Done(c);
        }
    }
}
