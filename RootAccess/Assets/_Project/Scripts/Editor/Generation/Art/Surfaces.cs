using UnityEngine;

namespace RootAccess.EditorTools
{
    /// <summary>
    /// Material painters shared by tiles, props and backdrops: bricks, planks, marble, perforated metal, riveted panels,
    /// moss, drips, a night-city window view and a server-rack face. All of them write normals where they bevel, so the
    /// 2D lights pick out the relief.
    /// </summary>
    internal static class Surfaces
    {
        /// <summary>Running-bond bricks with per-brick tone jitter and a bevel on each brick.</summary>
        public static PixelCanvas Bricks(this PixelCanvas c, int x, int y, int w, int h, int brickW, int brickH, Color32 mortar, Color32 brick,
            int seed, float jitter = 0.12f)
        {
            c.Rect(x, y, w, h, mortar);
            var random = new System.Random(seed);
            for (int row = 0; row * brickH < h; row++)
            {
                int offset = row % 2 == 0 ? 0 : brickW / 2;
                for (int bx = -brickW; bx < w + brickW; bx += brickW)
                {
                    float tone = 1f + ((float)random.NextDouble() - 0.5f) * 2f * jitter;
                    Ramp ramp = Ramp.From(Paint.Shade(brick, tone), 0.7f);
                    int x0 = Mathf.Max(x, x + bx + offset + 1), x1 = Mathf.Min(x + w, x + bx + offset + brickW);
                    int y0 = y + row * brickH + 1, y1 = Mathf.Min(y + h, y + row * brickH + brickH);
                    if (x1 - x0 <= 0 || y1 - y0 <= 0) continue;
                    c.Bevel(x0, y0, x1 - x0, y1 - y0, ramp);
                }
            }

            return c;
        }

        /// <summary>Horizontal planks with grain lines and staggered joints.</summary>
        public static PixelCanvas Planks(this PixelCanvas c, int x, int y, int w, int h, int plankH, Color32 wood, int seed)
        {
            var random = new System.Random(seed);
            for (int row = 0; row * plankH < h; row++)
            {
                Ramp ramp = Ramp.From(Paint.Shade(wood, 0.9f + (float)random.NextDouble() * 0.2f), 0.6f);
                int y0 = y + row * plankH;
                int hh = Mathf.Min(plankH, y + h - y0);
                c.Bevel(x, y0, w, hh, ramp);
                for (int g = 0; g < 3; g++)
                {
                    int gy = y0 + 1 + random.Next(Mathf.Max(1, hh - 2));
                    int gx = x + random.Next(w);
                    c.Rect(gx, gy, 3 + random.Next(6), 1, ramp.Dark);
                }

                int joint = x + (row * 13 + seed) % w;
                c.Rect(joint, y0, 1, hh, ramp.Deep);
            }

            return c;
        }

        /// <summary>Polished stone with thin wandering veins.</summary>
        public static PixelCanvas Marble(this PixelCanvas c, int x, int y, int w, int h, Color32 stone, Color32 vein, int seed)
        {
            c.Rect(x, y, w, h, stone);
            var random = new System.Random(seed);
            for (int v = 0; v < 3; v++)
            {
                float vx = x + (float)random.NextDouble() * w, vy = y;
                float dx = ((float)random.NextDouble() - 0.5f) * 1.4f;
                while (vy < y + h)
                {
                    c.Set(Mathf.RoundToInt(vx), Mathf.RoundToInt(vy), vein);
                    vx += dx + ((float)random.NextDouble() - 0.5f) * 1.2f;
                    if (vx < x) vx += w;
                    if (vx >= x + w) vx -= w;
                    vy += 1f;
                }
            }

            for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
                c.SetNormal(xx, yy, Vector3.forward);
            return c;
        }

        /// <summary>Riveted metal plate with a bevel and rivets in the corners.</summary>
        public static PixelCanvas RivetPanel(this PixelCanvas c, int x, int y, int w, int h, Ramp ramp, bool rivets = true)
        {
            c.Bevel(x, y, w, h, ramp, 1);
            if (!rivets) return c;
            foreach ((int rx, int ry) in new[] { (x + 2, y + 2), (x + w - 3, y + 2), (x + 2, y + h - 3), (x + w - 3, y + h - 3) })
            {
                c.Set(rx, ry, ramp.Light);
                c.Set(rx + 1, ry - 1, ramp.Deep);
            }

            return c;
        }

        /// <summary>Perforated metal: a hole grid on a bevelled plate.</summary>
        public static PixelCanvas Perforated(this PixelCanvas c, int x, int y, int w, int h, Ramp ramp, int spacing = 4)
        {
            c.Bevel(x, y, w, h, ramp, 1);
            for (int yy = y + 2; yy < y + h - 1; yy += spacing)
            for (int xx = x + 2; xx < x + w - 1; xx += spacing)
            {
                c.Set(xx, yy, ramp.Deep);
                c.Set(xx, yy + 1, ramp.Dark);
            }

            return c;
        }

        /// <summary>Moss / slime along the top edge of a rectangle, dripping down a few pixels.</summary>
        public static PixelCanvas MossTop(this PixelCanvas c, int x, int y, int w, int topY, Color32 moss, int seed, int maxDrip = 5)
        {
            var random = new System.Random(seed);
            Ramp ramp = Ramp.From(moss);
            for (int xx = x; xx < x + w; xx++)
            {
                int depth = 1 + random.Next(3);
                if (random.NextDouble() < 0.18) depth += random.Next(maxDrip);
                for (int d = 0; d < depth; d++)
                    c.Set(xx, topY - d, d == 0 ? ramp.Light : d < 2 ? ramp.Base : ramp.Dark);
            }

            return c;
        }

        /// <summary>Vertical grime streaks (rain run-off) of random length from the top edge.</summary>
        public static PixelCanvas Streaks(this PixelCanvas c, int x, int y, int w, int h, Color32 grime, int seed, int count = 3)
        {
            var random = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                int sx = x + random.Next(w), len = h / 3 + random.Next(h / 2);
                for (int d = 0; d < len; d++)
                {
                    int sy = y + h - 1 - d;
                    Color under = c.Get(sx, sy);
                    if (under.a == 0f) continue;
                    c.Set(sx, sy, Color32.Lerp(c.Get(sx, sy), grime, 0.45f * (1f - d / (float)len)));
                }
            }

            return c;
        }

        /// <summary>A window looking out over the night city: gradient sky, tower silhouettes, lit windows, glass glare.</summary>
        public static PixelCanvas CityWindow(this PixelCanvas c, int x, int y, int w, int h, int seed, Color32 frame, bool warmRoom = false)
        {
            c.DitherGradient(x, y, w, h, Paint.Rgb(46, 22, 70), Paint.Rgb(10, 10, 30), 4);
            var random = new System.Random(seed);
            int bx = x;
            while (bx < x + w)
            {
                int bw = 3 + random.Next(5), bh = h / 4 + random.Next(h * 3 / 4);
                c.Rect(bx, y, bw, bh, Paint.Rgb(14, 12, 30));
                for (int wy = y + 2; wy < y + bh - 1; wy += 2)
                for (int wx = bx + 1; wx < bx + bw - 1; wx += 2)
                    if (random.NextDouble() < 0.25)
                        c.Set(wx, wy, random.NextDouble() < 0.6 ? Paint.Rgb(255, 200, 110) : Paint.Rgb(90, 220, 255));
                bx += bw + random.Next(2);
            }

            // Glass glare and reflections.
            for (int i = 0; i < Mathf.Min(w, h); i += 1)
                if (i % 7 < 2) c.Blend(x + i, y + h - 1 - i, new Color(1f, 1f, 1f, 0.12f));
            if (warmRoom) c.Rect(x, y, w, 1, Paint.Rgb(255, 180, 90, 120));
            return c.Frame(x - 1, y - 1, w + 2, h + 2, frame);
        }

        /// <summary>Front of a server cabinet section: blade slots with status LEDs.</summary>
        public static PixelCanvas RackFace(this PixelCanvas c, int x, int y, int w, int h, int seed)
        {
            Ramp body = Ramp.From(Paint.Rgb(34, 40, 54));
            c.Bevel(x, y, w, h, body);
            var random = new System.Random(seed);
            for (int sy = y + 2; sy < y + h - 3; sy += 4)
            {
                c.Rect(x + 2, sy, w - 4, 3, Paint.Rgb(22, 26, 36)).Rect(x + 2, sy + 2, w - 4, 1, Paint.Rgb(52, 60, 76));
                int leds = 1 + random.Next(3);
                for (int l = 0; l < leds; l++)
                {
                    double roll = random.NextDouble();
                    Color32 led = roll < 0.55 ? Paint.Rgb(60, 255, 190) : roll < 0.8 ? Paint.Rgb(46, 200, 255) : roll < 0.93 ? Paint.Rgb(255, 190, 60) : Paint.Rgb(255, 60, 90);
                    c.Set(x + 3 + l * 2, sy + 1, led);
                }

                c.Rect(x + w - 8, sy + 1, 4, 1, Paint.Rgb(70, 80, 100));
            }

            return c;
        }

        /// <summary>Two-colour gradient fill without dithering (smooth: used by bilinear backdrops).</summary>
        public static PixelCanvas SmoothGradient(this PixelCanvas c, int x, int y, int w, int h, Color bottom, Color top)
        {
            for (int yy = y; yy < y + h; yy++)
            {
                Color32 col = Color.Lerp(bottom, top, h <= 1 ? 0f : (yy - y) / (float)(h - 1));
                for (int xx = x; xx < x + w; xx++) c.Set(xx, yy, col);
            }

            return c;
        }
    }
}
