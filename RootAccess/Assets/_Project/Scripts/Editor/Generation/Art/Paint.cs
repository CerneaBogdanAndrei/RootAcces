using System.Collections.Generic;
using UnityEngine;

namespace RootAccess.EditorTools
{
    /// <summary>
    /// Four-tone colour ramp with hue shifting (shadows lean towards violet, highlights towards warm), the way pixel
    /// artists shade by hand. <see cref="Pick"/> maps a lighting value to one of the tones.
    /// </summary>
    internal readonly struct Ramp
    {
        public readonly Color32 Deep, Dark, Base, Light;

        public Ramp(Color32 deep, Color32 dark, Color32 @base, Color32 light)
        {
            Deep = deep;
            Dark = dark;
            Base = @base;
            Light = light;
        }

        public static Ramp From(Color32 baseColor, float contrast = 1f)
        {
            Color b = baseColor;
            Color shadowHue = new(0.32f, 0.22f, 0.55f);
            Color lightHue = new(1f, 0.95f, 0.8f);
            Color dark = Color.Lerp(b * (1f - 0.32f * contrast), shadowHue * (1f - 0.32f * contrast), 0.18f);
            Color deep = Color.Lerp(b * (1f - 0.58f * contrast), shadowHue * 0.35f, 0.3f);
            Color light = Color.Lerp(b * (1f + 0.32f * contrast), lightHue, 0.16f);
            dark.a = deep.a = light.a = b.a;
            return new Ramp(deep, dark, baseColor, light);
        }

        public Color32 Pick(float lighting) =>
            lighting < 0.12f ? Deep : lighting < 0.42f ? Dark : lighting < 0.8f ? Base : Light;

        public Ramp Darker(float factor) =>
            new(Paint.Shade(Deep, factor), Paint.Shade(Dark, factor), Paint.Shade(Base, factor), Paint.Shade(Light, factor));
    }

    /// <summary>
    /// Shaded drawing primitives for the procedural pixel art: capsules, ellipses, polygons and bevelled boxes that pick
    /// their colour from a <see cref="Ramp"/> by a fake 3D normal and also store that normal for the sprite's normal map
    /// (so 2D lights shade the art the same way it was painted). Plus dithering, rim light and a 3×5 pixel font.
    /// </summary>
    internal static class Paint
    {
        /// <summary>Key light used when baking shading: from the upper left, slightly towards the viewer.</summary>
        public static readonly Vector3 KeyLight = new Vector3(-0.45f, 0.6f, 0.66f).normalized;

        public static Color32 Rgb(int r, int g, int b, int a = 255) => new((byte)r, (byte)g, (byte)b, (byte)a);

        public static Color32 Shade(Color32 c, float factor) =>
            new((byte)Mathf.Clamp(c.r * factor, 0, 255), (byte)Mathf.Clamp(c.g * factor, 0, 255), (byte)Mathf.Clamp(c.b * factor, 0, 255), c.a);

        public static Color32 Mix(Color32 a, Color32 b, float t) => Color32.Lerp(a, b, t);

        public static Color32 Alpha(Color32 c, int alpha) => new(c.r, c.g, c.b, (byte)Mathf.Clamp(alpha, 0, 255));

        private static readonly int[,] Bayer4 =
        {
            { 0, 8, 2, 10 }, { 12, 4, 14, 6 }, { 3, 11, 1, 9 }, { 15, 7, 13, 5 }
        };

        /// <summary>Ordered-dither threshold in [0, 1) for a pixel.</summary>
        public static float Dither(int x, int y) => (Bayer4[y & 3, x & 3] + 0.5f) / 16f;

        // ---------------------------------------------------------------- shaded primitives

        /// <summary>Tapered capsule from a (radius ra) to b (radius rb), shaded as a cylinder lit by <see cref="KeyLight"/>.</summary>
        public static void Capsule(this PixelCanvas c, Vector2 a, Vector2 b, float ra, float rb, Ramp ramp, float lightBias = 0f)
        {
            float r = Mathf.Max(ra, rb);
            int minX = Mathf.FloorToInt(Mathf.Min(a.x, b.x) - r - 1), maxX = Mathf.CeilToInt(Mathf.Max(a.x, b.x) + r + 1);
            int minY = Mathf.FloorToInt(Mathf.Min(a.y, b.y) - r - 1), maxY = Mathf.CeilToInt(Mathf.Max(a.y, b.y) + r + 1);
            Vector2 ab = b - a;
            float len2 = Mathf.Max(ab.sqrMagnitude, 0.0001f);
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                Vector2 closest = a + ab * t;
                float radius = Mathf.Lerp(ra, rb, t);
                Vector2 d = p - closest;
                float dist = d.magnitude;
                if (dist > radius) continue;
                Vector2 n2 = radius > 0.01f ? d / radius : Vector2.zero;
                ShadePixel(c, x, y, n2, ramp, lightBias);
            }
        }

        /// <summary>Ellipse shaded as a dome.</summary>
        public static void Dome(this PixelCanvas c, float cx, float cy, float rx, float ry, Ramp ramp, float lightBias = 0f)
        {
            for (int y = Mathf.FloorToInt(cy - ry); y <= Mathf.CeilToInt(cy + ry); y++)
            for (int x = Mathf.FloorToInt(cx - rx); x <= Mathf.CeilToInt(cx + rx); x++)
            {
                var n2 = new Vector2((x + 0.5f - cx) / rx, (y + 0.5f - cy) / ry);
                if (n2.sqrMagnitude > 1f) continue;
                ShadePixel(c, x, y, n2, ramp, lightBias);
            }
        }

        private static void ShadePixel(PixelCanvas c, int x, int y, Vector2 n2, Ramp ramp, float lightBias)
        {
            float z = Mathf.Sqrt(Mathf.Max(0f, 1f - n2.sqrMagnitude));
            var n = new Vector3(n2.x, n2.y, z);
            float lighting = Vector3.Dot(n, KeyLight) * 0.85f + 0.15f + lightBias;
            // Dither the band edges a little so the tones blend like hand-placed pixels.
            lighting += (Dither(x, y) - 0.5f) * 0.12f;
            c.Set(x, y, ramp.Pick(lighting));
            c.SetNormal(x, y, n);
        }

        /// <summary>Upright cylinder (tank, barrel, pole): columns shaded by their facing, rows flat.</summary>
        public static PixelCanvas Cylinder(this PixelCanvas c, int x, int y, int w, int h, Ramp ramp, float lightBias = 0f)
        {
            for (int xx = x; xx < x + w; xx++)
            {
                float nx = (xx - x + 0.5f) / w * 2f - 1f;
                for (int yy = y; yy < y + h; yy++) ShadePixel(c, xx, yy, new Vector2(nx * 0.98f, 0f), ramp, lightBias);
            }

            return c;
        }

        /// <summary>Horizontal cylinder (pipe, rolled cable): rows shaded by their facing.</summary>
        public static PixelCanvas Pipe(this PixelCanvas c, int x, int y, int w, int h, Ramp ramp, float lightBias = 0f)
        {
            for (int yy = y; yy < y + h; yy++)
            {
                float ny = (yy - y + 0.5f) / h * 2f - 1f;
                for (int xx = x; xx < x + w; xx++) ShadePixel(c, xx, yy, new Vector2(0f, ny * 0.98f), ramp, lightBias);
            }

            return c;
        }

        /// <summary>Box with a bevel: lit top/left edges, shaded bottom/right edges, flat face.</summary>
        public static PixelCanvas Bevel(this PixelCanvas c, int x, int y, int w, int h, Ramp ramp, int bevel = 1)
        {
            for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
            {
                int fromLeft = xx - x, fromRight = x + w - 1 - xx, fromBottom = yy - y, fromTop = y + h - 1 - yy;
                Color32 color = ramp.Base;
                Vector3 n = Vector3.forward;
                if (fromTop < bevel) { color = ramp.Light; n = new Vector3(0f, 0.7f, 0.7f); }
                else if (fromLeft < bevel) { color = ramp.Light; n = new Vector3(-0.7f, 0f, 0.7f); }
                else if (fromBottom < bevel) { color = ramp.Deep; n = new Vector3(0f, -0.7f, 0.7f); }
                else if (fromRight < bevel) { color = ramp.Dark; n = new Vector3(0.7f, 0f, 0.7f); }
                c.Set(xx, yy, color);
                c.SetNormal(xx, yy, n);
            }

            return c;
        }

        /// <summary>Filled polygon (even-odd scanline), flat colour.</summary>
        public static PixelCanvas Polygon(this PixelCanvas c, IReadOnlyList<Vector2> points, Color32 color)
        {
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (Vector2 p in points)
            {
                minY = Mathf.Min(minY, p.y);
                maxY = Mathf.Max(maxY, p.y);
            }

            var crossings = new List<float>();
            for (int y = Mathf.FloorToInt(minY); y <= Mathf.CeilToInt(maxY); y++)
            {
                float sy = y + 0.5f;
                crossings.Clear();
                for (int i = 0; i < points.Count; i++)
                {
                    Vector2 p0 = points[i], p1 = points[(i + 1) % points.Count];
                    if ((p0.y <= sy && p1.y > sy) || (p1.y <= sy && p0.y > sy))
                        crossings.Add(p0.x + (sy - p0.y) / (p1.y - p0.y) * (p1.x - p0.x));
                }

                crossings.Sort();
                for (int i = 0; i + 1 < crossings.Count; i += 2)
                for (int x = Mathf.CeilToInt(crossings[i] - 0.5f); x <= Mathf.FloorToInt(crossings[i + 1] - 0.5f); x++)
                    c.Set(x, y, color);
            }

            return c;
        }

        /// <summary>Vertical gradient inside a rectangle with ordered dithering between the two end colours.</summary>
        public static PixelCanvas DitherGradient(this PixelCanvas c, int x, int y, int w, int h, Color32 bottom, Color32 top, int steps = 6)
        {
            for (int yy = y; yy < y + h; yy++)
            {
                float t = h <= 1 ? 0f : (yy - y) / (float)(h - 1);
                for (int xx = x; xx < x + w; xx++)
                {
                    float q = Mathf.Floor(t * steps + Dither(xx, yy)) / steps;
                    c.Set(xx, yy, Color32.Lerp(bottom, top, Mathf.Clamp01(q)));
                }
            }

            return c;
        }

        /// <summary>Soft additive glow (alpha-blended) around a point.</summary>
        public static PixelCanvas Glow(this PixelCanvas c, float cx, float cy, float radius, Color color, float falloff = 2f)
        {
            int minX = Mathf.FloorToInt(cx - radius), maxX = Mathf.CeilToInt(cx + radius);
            int minY = Mathf.FloorToInt(cy - radius), maxY = Mathf.CeilToInt(cy + radius);
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) / radius;
                if (d >= 1f) continue;
                Color g = color;
                g.a *= Mathf.Pow(1f - d, falloff);
                c.Blend(x, y, g);
            }

            return c;
        }

        /// <summary>
        /// Rim light: opaque pixels whose neighbour in <paramref name="side"/> is transparent get tinted, which separates a
        /// dark silhouette from a dark background (neon bouncing off the edge).
        /// </summary>
        public static PixelCanvas Rim(this PixelCanvas c, Color32 rim, Vector2Int side, float strength = 0.6f)
        {
            var copy = (Color32[])c.Pixels.Clone();
            for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                if (copy[y * c.Width + x].a < 128) continue;
                int nx = x + side.x, ny = y + side.y;
                bool edge = nx < 0 || ny < 0 || nx >= c.Width || ny >= c.Height || copy[ny * c.Width + nx].a < 128;
                if (!edge) continue;
                c.Set(x, y, Color32.Lerp(copy[y * c.Width + x], rim, strength));
            }

            return c;
        }

        /// <summary>Outline whose colour is a darkened version of the pixel it borders (softer than pure black).</summary>
        public static PixelCanvas SelectiveOutline(this PixelCanvas c, Color32 darkest, float factor = 0.35f)
        {
            var copy = (Color32[])c.Pixels.Clone();
            for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                if (copy[y * c.Width + x].a > 0) continue;
                Color32 neighbour = default;
                bool found = false;
                foreach (Vector2Int o in Neighbours)
                {
                    int nx = x + o.x, ny = y + o.y;
                    if (nx < 0 || ny < 0 || nx >= c.Width || ny >= c.Height) continue;
                    Color32 n = copy[ny * c.Width + nx];
                    if (n.a < 160) continue;
                    neighbour = n;
                    found = true;
                    break;
                }

                if (found) c.Set(x, y, Color32.Lerp(darkest, Shade(neighbour, factor), 0.5f));
            }

            return c;
        }

        private static readonly Vector2Int[] Neighbours = { new(-1, 0), new(1, 0), new(0, -1), new(0, 1) };

        /// <summary>Hard-edged rectangle outline.</summary>
        public static PixelCanvas Frame(this PixelCanvas c, int x, int y, int w, int h, Color32 color, int thickness = 1) =>
            c.Rect(x, y, w, thickness, color).Rect(x, y + h - thickness, w, thickness, color)
                .Rect(x, y, thickness, h, color).Rect(x + w - thickness, y, thickness, h, color);

        /// <summary>Diagonal hazard stripes inside a rectangle.</summary>
        public static PixelCanvas Stripes(this PixelCanvas c, int x, int y, int w, int h, Color32 a, Color32 b, int period = 6)
        {
            for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
                c.Set(xx, yy, ((xx + yy) / (period / 2)) % 2 == 0 ? a : b);
            return c;
        }

        /// <summary>Speckle: sets random pixels inside a rectangle (grime, stars, gravel).</summary>
        public static PixelCanvas Speckle(this PixelCanvas c, int x, int y, int w, int h, Color32 color, float density, int seed)
        {
            var random = new System.Random(seed);
            for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
                if (random.NextDouble() < density && c.Get(xx, yy).a > 0)
                    c.Set(xx, yy, color);
            return c;
        }

        // ---------------------------------------------------------------- 3x5 pixel font

        private static readonly Dictionary<char, string> Glyphs = new()
        {
            ['A'] = "010101111101101", ['B'] = "110101110101110", ['C'] = "011100100100011", ['D'] = "110101101101110",
            ['E'] = "111100110100111", ['F'] = "111100110100100", ['G'] = "011100101101011", ['H'] = "101101111101101",
            ['I'] = "111010010010111", ['J'] = "001001001101010", ['K'] = "101101110101101", ['L'] = "100100100100111",
            ['M'] = "101111111101101", ['N'] = "101111111111101", ['O'] = "010101101101010", ['P'] = "110101110100100",
            ['Q'] = "010101101110011", ['R'] = "110101110101101", ['S'] = "011100010001110", ['T'] = "111010010010010",
            ['U'] = "101101101101111", ['V'] = "101101101101010", ['W'] = "101101111111101", ['X'] = "101101010101101",
            ['Y'] = "101101010010010", ['Z'] = "111001010100111", ['0'] = "111101101101111", ['1'] = "010110010010111",
            ['2'] = "110001010100111", ['3'] = "110001010001110", ['4'] = "101101111001001", ['5'] = "111100110001110",
            ['6'] = "011100111101111", ['7'] = "111001010010010", ['8'] = "111101111101111", ['9'] = "111101111001110",
            ['/'] = "001001010100100", ['-'] = "000000111000000", ['.'] = "000000000000010", ['!'] = "010010010000010",
            [':'] = "000010000010000", ['+'] = "000010111010000", [' '] = "000000000000000", ['>'] = "100010001010100"
        };

        /// <summary>Width in pixels of <paramref name="text"/> drawn with <see cref="Text"/>.</summary>
        public static int TextWidth(string text, int scale = 1) => text.Length == 0 ? 0 : (text.Length * 4 - 1) * scale;

        /// <summary>Draws upper-case text with the built-in 3×5 font; (x, y) is the bottom-left corner.</summary>
        public static PixelCanvas Text(this PixelCanvas c, string text, int x, int y, Color32 color, int scale = 1)
        {
            int cursor = x;
            foreach (char raw in text.ToUpperInvariant())
            {
                if (!Glyphs.TryGetValue(raw, out string glyph)) glyph = Glyphs[' '];
                for (int row = 0; row < 5; row++)
                for (int col = 0; col < 3; col++)
                    if (glyph[row * 3 + col] == '1')
                        c.Rect(cursor + col * scale, y + (4 - row) * scale, scale, scale, color);
                cursor += 4 * scale;
            }

            return c;
        }
    }
}
