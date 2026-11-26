using System.Collections.Generic;
using UnityEngine;

namespace RootAccess.EditorTools
{
    /// <summary>Visual families of the levels: each one has its own tiles, backdrop, props and light mood.</summary>
    public enum Biome { Rooftop, Office, Server, Sewer, Hq, Hideout, Lab }

    /// <summary>
    /// 32×32 tiles per <see cref="Biome"/>, in roles: <c>top</c> (walkable surface row), <c>body</c> (the mass under it),
    /// <c>wall</c> (obstacles, shafts), <c>ceil</c> (interior ceilings), <c>back</c> (backdrop wall behind the action) and
    /// <c>plat</c> (one-tile catwalks). Indices inside a role carry meaning for the pickers in <see cref="LevelTheme"/>
    /// (e.g. rooftop body 3–5 are windows), so keep the order when adding variants.
    /// </summary>
    internal static class TileArt
    {
        private const int S = 32;

        public static string Name(Biome biome, string role, int index) => $"{biome.ToString().ToLowerInvariant()}_{role}_{index}";

        /// <summary>Every tile of every biome, ready to save.</summary>
        public static IEnumerable<(string name, PixelCanvas canvas)> PaintAll()
        {
            foreach ((Biome biome, string role, int index, PixelCanvas canvas) in All())
                yield return (Name(biome, role, index), canvas);
        }

        private static Dictionary<(Biome, string), int> _counts;

        /// <summary>How many variants each role of a biome has (read by the asset factory to create Tile assets).</summary>
        public static Dictionary<(Biome, string), int> Counts()
        {
            if (_counts != null) return _counts;
            var counts = new Dictionary<(Biome, string), int>();
            foreach ((Biome biome, string role, int index, _) in All())
                counts[(biome, role)] = Mathf.Max(counts.TryGetValue((biome, role), out int n) ? n : 0, index + 1);
            return _counts = counts;
        }

        /// <summary>Number of variants of <paramref name="role"/> in <paramref name="biome"/> (0 if the biome has none).</summary>
        public static int Count(Biome biome, string role) => Counts().TryGetValue((biome, role), out int n) ? n : 0;

        private static IEnumerable<(Biome, string, int, PixelCanvas)> All()
        {
            foreach (var t in Rooftop()) yield return t;
            foreach (var t in Office()) yield return t;
            foreach (var t in Server()) yield return t;
            foreach (var t in Sewer()) yield return t;
            foreach (var t in Hq()) yield return t;
            foreach (var t in Hideout()) yield return t;
            foreach (var t in Lab()) yield return t;
        }

        private static PixelCanvas New() => new(S, S);

        // ---------------------------------------------------------------- rooftops (Level 1)

        private static readonly Color32 RoofBrick = Paint.Rgb(104, 56, 70);
        private static readonly Color32 RoofMortar = Paint.Rgb(46, 28, 44);

        private static PixelCanvas Facade(int seed) => New().Bricks(0, 0, S, S, 16, 8, RoofMortar, RoofBrick, seed, 0.1f);

        private static IEnumerable<(Biome, string, int, PixelCanvas)> Rooftop()
        {
            const Biome b = Biome.Rooftop;
            Ramp coping = Ramp.From(Paint.Rgb(150, 150, 170));
            for (int i = 0; i < 3; i++)
            {
                PixelCanvas top = Facade(10 + i);
                top.Bevel(0, 26, S, 6, coping);
                top.Rect(0, 25, S, 1, Paint.Rgb(30, 20, 36));
                top.Speckle(0, 26, S, 5, coping.Dark, 0.08f, 20 + i);
                if (i == 1) top.Rect(20, 18, 6, 6, Paint.Rgb(40, 40, 52)).Frame(20, 18, 6, 6, Paint.Rgb(90, 92, 110)).Rect(21, 20, 4, 1, Paint.Rgb(70, 72, 86));
                if (i == 2) top.Rect(6, 0, 2, 25, Paint.Rgb(70, 74, 90)).Rect(6, 0, 1, 25, Paint.Rgb(110, 116, 134));
                top.Streaks(0, 0, S, 25, Paint.Rgb(30, 20, 30), 40 + i, 2);
                yield return (b, "top", i, top);
            }

            yield return (b, "body", 0, Facade(1));
            yield return (b, "body", 1, Facade(2));
            yield return (b, "body", 2, Facade(3).Streaks(0, 0, S, S, Paint.Rgb(26, 18, 28), 7, 4));
            yield return (b, "body", 3, Window(Facade(4), Paint.Rgb(255, 196, 110), Paint.Rgb(150, 80, 40), 1));
            yield return (b, "body", 4, Window(Facade(5), Paint.Rgb(90, 220, 255), Paint.Rgb(30, 70, 120), 2));
            yield return (b, "body", 5, Window(Facade(6), Paint.Rgb(40, 40, 60), Paint.Rgb(20, 20, 34), 3));

            Ramp concrete = Ramp.From(Paint.Rgb(92, 88, 108));
            for (int i = 0; i < 2; i++)
            {
                PixelCanvas wall = New().Bevel(0, 0, S, S, concrete);
                wall.Rect(0, 15, S, 1, concrete.Dark).Rect(15 + i * 2, 0, 1, S, concrete.Dark);
                wall.Speckle(0, 0, S, S, concrete.Dark, 0.05f, 60 + i).Streaks(0, 0, S, S, Paint.Rgb(60, 40, 50), 61 + i, 3);
                yield return (b, "wall", i, wall);
            }

            yield return (b, "ceil", 0, New().Bevel(0, 0, S, S, concrete).Rect(0, 4, S, 1, concrete.Dark));
            yield return (b, "back", 0, Facade(70).Tint(new Color(0.8f, 0.8f, 0.9f)));
            yield return (b, "plat", 0, Girder(Ramp.From(Paint.Rgb(110, 116, 140)), Paint.Rgb(255, 190, 60)));
        }

        /// <summary>Facade window: frame, lit or dark glass with blinds, a sill.</summary>
        private static PixelCanvas Window(PixelCanvas c, Color32 light, Color32 deep, int seed)
        {
            c.Rect(7, 6, 18, 20, Paint.Rgb(30, 24, 36));
            c.DitherGradient(8, 7, 16, 18, deep, light, 4);
            var random = new System.Random(seed);
            for (int y = 9; y < 25; y += 3) c.Rect(8, y, 16, 1, Paint.Shade(deep, 0.8f));
            if (seed != 3 && random.NextDouble() < 0.9)
                c.Rect(12 + random.Next(6), 7, 4, 7 + random.Next(4), Paint.Shade(deep, 0.55f));
            c.Rect(15, 7, 1, 18, Paint.Rgb(30, 24, 36));
            c.Bevel(6, 4, 20, 2, Ramp.From(Paint.Rgb(150, 150, 166)));
            return c;
        }

        /// <summary>Steel I-beam catwalk with a hazard edge.</summary>
        private static PixelCanvas Girder(Ramp steel, Color32 hazard)
        {
            PixelCanvas c = New();
            c.Bevel(0, 22, S, 10, steel).Rect(0, 24, S, 2, steel.Dark);
            for (int x = 0; x < S; x += 8) c.Line(x, 23, x + 7, 31, steel.Dark).Line(x + 7, 23, x, 31, steel.Dark);
            c.Bevel(0, 29, S, 3, steel);
            c.Stripes(0, 22, S, 2, hazard, Paint.Rgb(30, 30, 30), 6);
            return c;
        }

        // ---------------------------------------------------------------- office tower (Level 2)

        private static IEnumerable<(Biome, string, int, PixelCanvas)> Office()
        {
            const Biome b = Biome.Office;
            Ramp slab = Ramp.From(Paint.Rgb(88, 94, 112));
            for (int i = 0; i < 2; i++)
            {
                PixelCanvas top = New().Bevel(0, 0, S, 25, slab).Speckle(0, 0, S, 24, slab.Dark, 0.06f, 80 + i);
                top.Planks(0, 25, S, 6, 6, Paint.Rgb(112, 74, 56), 81 + i);
                top.Rect(0, 31, S, 1, Paint.Rgb(200, 170, 140));
                top.Rect(0, 24, S, 1, Paint.Rgb(40, 34, 40));
                yield return (b, "top", i, top);
            }

            for (int i = 0; i < 3; i++)
            {
                PixelCanvas body = New().Bevel(0, 0, S, S, slab).Rect(0, 10 + i * 4, S, 1, slab.Dark).Speckle(0, 0, S, S, slab.Dark, 0.05f, 90 + i);
                if (i == 2) body.Rect(4, 0, 6, S, Paint.Rgb(70, 76, 92)).Rect(5, 0, 1, S, Paint.Rgb(120, 126, 140));
                yield return (b, "body", i, body);
            }

            Ramp drywall = Ramp.From(Paint.Rgb(150, 156, 172));
            yield return (b, "wall", 0, New().Bevel(0, 0, S, S, drywall).Rect(15, 0, 1, S, drywall.Dark));
            yield return (b, "wall", 1, New().Bevel(0, 0, S, S, drywall).Rect(0, 15, S, 1, drywall.Dark).Rect(15, 0, 1, S, drywall.Dark));
            Ramp ceiling = Ramp.From(Paint.Rgb(120, 124, 138));
            PixelCanvas ceil0 = New().Fill(ceiling.Base).Frame(0, 0, S, S, ceiling.Dark).Speckle(1, 1, S - 2, S - 2, ceiling.Dark, 0.1f, 95);
            yield return (b, "ceil", 0, ceil0);
            PixelCanvas ceil1 = ceil0.Clone().Rect(3, 3, 26, 6, Paint.Rgb(230, 240, 255)).Frame(3, 3, 26, 6, Paint.Rgb(150, 160, 180));
            yield return (b, "ceil", 1, ceil1);

            Ramp paint = Ramp.From(Paint.Rgb(70, 80, 106));
            PixelCanvas wallPaint = New().Fill(paint.Base).Speckle(0, 0, S, S, paint.Dark, 0.05f, 100);
            yield return (b, "back", 0, wallPaint);
            yield return (b, "back", 1, wallPaint.Clone().Rect(0, 30, S, 2, paint.Light).Rect(0, 29, S, 1, paint.Dark));
            PixelCanvas wainscot = New().Planks(0, 0, S, S, 8, Paint.Rgb(80, 54, 46), 101).Rect(0, 0, S, 3, Paint.Rgb(40, 30, 30)).Rect(0, 29, S, 3, Paint.Rgb(130, 96, 74));
            yield return (b, "back", 2, wainscot);
            yield return (b, "back", 3, wallPaint.Clone().CityWindow(1, 0, 30, S, 102, Paint.Rgb(40, 44, 58)));
            yield return (b, "back", 4, New().Bevel(0, 0, S, S, Ramp.From(Paint.Rgb(96, 104, 126))).Rect(4, 0, 1, S, Paint.Rgb(70, 76, 96)).Rect(27, 0, 1, S, Paint.Rgb(70, 76, 96)));
            yield return (b, "plat", 0, Girder(Ramp.From(Paint.Rgb(120, 126, 146)), Paint.Rgb(255, 190, 60)));
        }

        // ---------------------------------------------------------------- server farm (Level 3)

        private static IEnumerable<(Biome, string, int, PixelCanvas)> Server()
        {
            const Biome b = Biome.Server;
            Ramp steel = Ramp.From(Paint.Rgb(52, 62, 84));
            Ramp floor = Ramp.From(Paint.Rgb(104, 116, 140));
            for (int i = 0; i < 2; i++)
            {
                PixelCanvas top = New().Bevel(0, 0, S, 24, steel);
                Cables(top, 4, 18, i);
                top.Perforated(0, 24, 16, 8, floor).Perforated(16, 24, 16, 8, floor);
                top.Rect(0, 31, S, 1, Paint.Rgb(120, 240, 255));
                yield return (b, "top", i, top);
            }

            for (int i = 0; i < 3; i++)
            {
                PixelCanvas body = New().Bevel(0, 0, S, S, steel);
                if (i < 2) Cables(body, 6, 22, i + 3);
                else body.RivetPanel(2, 2, 28, 28, steel.Darker(0.85f));
                yield return (b, "body", i, body);
            }

            Ramp plate = Ramp.From(Paint.Rgb(60, 70, 92));
            PixelCanvas wall0 = New().RivetPanel(0, 0, S, S, plate);
            for (int y = 6; y < 26; y += 3) wall0.Rect(8, y, 16, 1, plate.Deep);
            yield return (b, "wall", 0, wall0);
            yield return (b, "wall", 1, New().RivetPanel(0, 0, S, S, plate).Stripes(0, 0, S, 4, Paint.Rgb(255, 190, 60), Paint.Rgb(30, 30, 36), 6));
            yield return (b, "ceil", 0, New().RivetPanel(0, 0, S, S, plate.Darker(0.8f)).Rect(0, 2, S, 3, Paint.Rgb(30, 36, 50)).Rect(0, 3, S, 1, Paint.Rgb(46, 200, 255)));

            yield return (b, "back", 0, New().RackFace(1, 0, 30, S, 110));
            yield return (b, "back", 1, New().RackFace(1, 0, 30, S, 111));
            PixelCanvas vent = New().Bevel(0, 0, S, S, Ramp.From(Paint.Rgb(40, 46, 62)));
            for (int y = 4; y < 28; y += 3) vent.Rect(4, y, 24, 2, Paint.Rgb(20, 24, 34)).Rect(4, y + 1, 24, 1, Paint.Rgb(56, 64, 84));
            yield return (b, "back", 2, vent);
            PixelCanvas tray = New().Fill(Paint.Rgb(26, 30, 42));
            Cables(tray, 10, 20, 7);
            tray.Rect(0, 8, S, 2, Paint.Rgb(70, 80, 100)).Rect(0, 22, S, 2, Paint.Rgb(70, 80, 100));
            yield return (b, "back", 3, tray);
            yield return (b, "back", 4, New().Fill(Paint.Rgb(24, 28, 40)).Rect(14, 0, 4, S, Paint.Rgb(30, 160, 210)).Rect(15, 0, 2, S, Paint.Rgb(140, 240, 255)));
            yield return (b, "plat", 0, Girder(Ramp.From(Paint.Rgb(90, 104, 130)), Paint.Rgb(46, 220, 255)));
        }

        private static void Cables(PixelCanvas c, int y0, int y1, int seed)
        {
            Color32[] colours = { Paint.Rgb(46, 200, 255), Paint.Rgb(255, 60, 160), Paint.Rgb(70, 76, 96), Paint.Rgb(255, 196, 60), Paint.Rgb(40, 44, 56) };
            var random = new System.Random(seed);
            for (int y = y0; y < y1; y += 2)
            {
                Color32 col = colours[random.Next(colours.Length)];
                int sag = random.Next(2);
                for (int x = 0; x < S; x++) c.Set(x, y - (x > 8 && x < 24 ? sag : 0), col);
            }
        }

        // ---------------------------------------------------------------- sewers (Level 4)

        private static IEnumerable<(Biome, string, int, PixelCanvas)> Sewer()
        {
            const Biome b = Biome.Sewer;
            Color32 stone = Paint.Rgb(70, 82, 78), mortar = Paint.Rgb(26, 32, 30), moss = Paint.Rgb(80, 150, 70);
            for (int i = 0; i < 3; i++)
            {
                PixelCanvas top = New().Bricks(0, 0, S, S, 16, 10, mortar, stone, 120 + i, 0.15f);
                top.Bevel(0, 27, S, 5, Ramp.From(Paint.Rgb(92, 100, 96)));
                top.MossTop(0, 0, S, 31, moss, 130 + i, i == 1 ? 7 : 4);
                yield return (b, "top", i, top);
            }

            for (int i = 0; i < 3; i++)
            {
                PixelCanvas body = New().Bricks(0, 0, S, S, 16, 10, mortar, Paint.Shade(stone, 0.85f), 140 + i, 0.15f);
                if (i == 2) body.Rect(12, 10, 8, 8, Paint.Rgb(14, 16, 16)).Frame(11, 9, 10, 10, Paint.Rgb(60, 64, 62));
                body.Streaks(0, 0, S, S, Paint.Rgb(50, 90, 50), 150 + i, 2);
                yield return (b, "body", i, body);
            }

            yield return (b, "wall", 0, New().Bricks(0, 0, S, S, 32, 16, mortar, Paint.Rgb(86, 92, 88), 160, 0.12f));
            yield return (b, "wall", 1, New().Bricks(0, 0, S, S, 32, 16, mortar, Paint.Rgb(80, 88, 84), 161, 0.12f).Streaks(0, 0, S, S, Paint.Rgb(60, 110, 60), 162, 3));
            yield return (b, "ceil", 0, New().Bricks(0, 0, S, S, 16, 8, mortar, Paint.Shade(stone, 0.7f), 163, 0.12f));

            Color32 tunnel = Paint.Rgb(58, 66, 64);
            yield return (b, "back", 0, New().Bricks(0, 0, S, S, 8, 4, Paint.Rgb(22, 26, 26), tunnel, 170, 0.15f));
            yield return (b, "back", 1, New().Bricks(0, 0, S, S, 8, 4, Paint.Rgb(22, 26, 26), tunnel, 171, 0.15f).Streaks(0, 0, S, S, Paint.Rgb(70, 120, 60), 172, 4));
            PixelCanvas tide = New().Bricks(0, 0, S, S, 8, 4, Paint.Rgb(22, 26, 26), tunnel, 173, 0.15f);
            tide.Rect(0, 8, S, 3, Paint.Rgb(52, 84, 56)).Rect(0, 11, S, 1, Paint.Rgb(90, 140, 80));
            yield return (b, "back", 2, tide);
            PixelCanvas grate = New().Bricks(0, 0, S, S, 8, 4, Paint.Rgb(22, 26, 26), tunnel, 174, 0.15f);
            grate.Rect(6, 8, 20, 16, Paint.Rgb(10, 12, 12)).Frame(5, 7, 22, 18, Paint.Rgb(80, 84, 82));
            for (int x = 8; x < 26; x += 3) grate.Rect(x, 8, 1, 16, Paint.Rgb(64, 68, 66));
            yield return (b, "back", 3, grate);
            yield return (b, "plat", 0, Girder(Ramp.From(Paint.Rgb(96, 104, 96)), Paint.Rgb(255, 190, 60)));
        }

        // ---------------------------------------------------------------- corporate HQ (Level 5)

        private static IEnumerable<(Biome, string, int, PixelCanvas)> Hq()
        {
            const Biome b = Biome.Hq;
            Color32 gold = Paint.Rgb(232, 186, 90);
            for (int i = 0; i < 2; i++)
            {
                PixelCanvas top = New().Marble(0, 0, S, S, Paint.Rgb(30, 30, 40), Paint.Rgb(60, 60, 76), 180 + i);
                top.Rect(0, 29, S, 3, Paint.Rgb(46, 46, 60)).Rect(0, 31, S, 1, Paint.Rgb(130, 130, 160)).Rect(0, 27, S, 1, gold);
                for (int x = 0; x < S; x += 2) top.Blend(x, 30, new Color(1f, 1f, 1f, 0.15f));
                yield return (b, "top", i, top);
            }

            for (int i = 0; i < 2; i++)
                yield return (b, "body", i, New().Marble(0, 0, S, S, Paint.Rgb(26, 26, 36), Paint.Rgb(52, 52, 68), 190 + i));

            yield return (b, "wall", 0, New().Marble(0, 0, S, S, Paint.Rgb(170, 168, 176), Paint.Rgb(120, 118, 130), 200).Rect(0, 0, 2, S, Paint.Rgb(120, 118, 130)));
            yield return (b, "wall", 1, New().Marble(0, 0, S, S, Paint.Rgb(160, 158, 168), Paint.Rgb(110, 108, 120), 201).Rect(0, 15, S, 1, gold));
            yield return (b, "ceil", 0, New().Fill(Paint.Rgb(24, 24, 32)).Rect(0, 2, S, 1, gold).Rect(6, 6, 20, 2, Paint.Rgb(250, 230, 190)));

            PixelCanvas glass = New().DitherGradient(0, 0, S, S, Paint.Rgb(18, 22, 40), Paint.Rgb(30, 36, 62), 4).Frame(0, 0, S, S, Paint.Rgb(150, 120, 70));
            for (int i = 0; i < S; i++) if (i % 9 < 2) glass.Blend(i, S - 1 - i, new Color(1f, 1f, 1f, 0.08f));
            yield return (b, "back", 0, glass);
            yield return (b, "back", 1, glass.Clone().Rect(0, 15, S, 1, Paint.Rgb(150, 120, 70)));
            PixelCanvas logo = glass.Clone();
            Hexagon(logo, 16, 16, 9, gold);
            logo.Text("H", 15, 14, gold);
            yield return (b, "back", 2, logo);
            yield return (b, "back", 3, New().CityWindow(0, 0, S, S, 203, Paint.Rgb(150, 120, 70)));
            yield return (b, "back", 4, New().Marble(0, 0, S, S, Paint.Rgb(70, 66, 74), Paint.Rgb(96, 90, 100), 204).Rect(3, 0, 1, S, gold).Rect(28, 0, 1, S, gold));
            yield return (b, "plat", 0, Girder(Ramp.From(Paint.Rgb(70, 70, 86)), gold));
        }

        private static void Hexagon(PixelCanvas c, float cx, float cy, float r, Color32 color)
        {
            for (int i = 0; i < 6; i++)
            {
                float a0 = (i * 60f + 30f) * Mathf.Deg2Rad, a1 = ((i + 1) * 60f + 30f) * Mathf.Deg2Rad;
                c.Line(Mathf.RoundToInt(cx + Mathf.Cos(a0) * r), Mathf.RoundToInt(cy + Mathf.Sin(a0) * r),
                    Mathf.RoundToInt(cx + Mathf.Cos(a1) * r), Mathf.RoundToInt(cy + Mathf.Sin(a1) * r), color);
            }
        }

        // ---------------------------------------------------------------- hideout (Hub) and lab (showcase)

        private static IEnumerable<(Biome, string, int, PixelCanvas)> Hideout()
        {
            const Biome b = Biome.Hideout;
            Color32 brick = Paint.Rgb(112, 64, 58), mortar = Paint.Rgb(44, 28, 30);
            for (int i = 0; i < 2; i++)
                yield return (b, "top", i, New().Bricks(0, 0, S, 24, 16, 8, mortar, brick, 210 + i).Planks(0, 24, S, 8, 4, Paint.Rgb(120, 82, 54), 212 + i).Rect(0, 23, S, 1, Paint.Rgb(30, 20, 20)));
            for (int i = 0; i < 2; i++) yield return (b, "body", i, New().Bricks(0, 0, S, S, 16, 8, mortar, Paint.Shade(brick, 0.85f), 214 + i));
            yield return (b, "wall", 0, New().Bricks(0, 0, S, S, 16, 8, mortar, Paint.Shade(brick, 0.9f), 216));
            yield return (b, "ceil", 0, New().Planks(0, 0, S, S, 8, Paint.Rgb(70, 50, 40), 217));
            yield return (b, "back", 0, New().Bricks(0, 0, S, S, 16, 8, mortar, Paint.Shade(brick, 0.75f), 218));
            yield return (b, "back", 1, New().Bricks(0, 0, S, S, 16, 8, mortar, Paint.Shade(brick, 0.7f), 219).Streaks(0, 0, S, S, Paint.Rgb(30, 20, 26), 220, 3));
            yield return (b, "back", 2, New().Bricks(0, 0, S, S, 16, 8, mortar, Paint.Shade(brick, 0.75f), 221).CityWindow(4, 4, 24, 24, 222, Paint.Rgb(60, 44, 44)));
            yield return (b, "plat", 0, Girder(Ramp.From(Paint.Rgb(110, 100, 100)), Paint.Rgb(255, 190, 60)));
        }

        private static IEnumerable<(Biome, string, int, PixelCanvas)> Lab()
        {
            const Biome b = Biome.Lab;
            Ramp panel = Ramp.From(Paint.Rgb(150, 158, 178));
            for (int i = 0; i < 2; i++) yield return (b, "top", i, New().RivetPanel(0, 0, S, S, panel).Rect(0, 30, S, 2, Paint.Rgb(46, 220, 255)));
            for (int i = 0; i < 2; i++) yield return (b, "body", i, New().RivetPanel(0, 0, S, S, panel.Darker(0.75f), i == 0));
            yield return (b, "wall", 0, New().RivetPanel(0, 0, S, S, Ramp.From(Paint.Rgb(120, 128, 150))));
            yield return (b, "ceil", 0, New().RivetPanel(0, 0, S, S, panel.Darker(0.6f)).Rect(4, 2, 24, 2, Paint.Rgb(220, 240, 255)));
            Ramp wall = Ramp.From(Paint.Rgb(62, 68, 90));
            yield return (b, "back", 0, New().Bevel(0, 0, S, S, wall).Rect(0, 16, S, 1, wall.Dark));
            yield return (b, "back", 1, New().Bevel(0, 0, S, S, wall).Rect(0, 16, S, 1, Paint.Rgb(46, 160, 200)));
            yield return (b, "plat", 0, Girder(Ramp.From(Paint.Rgb(130, 140, 160)), Paint.Rgb(46, 220, 255)));
        }
    }
}
