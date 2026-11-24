using System;
using UnityEngine;

namespace RootAccess.EditorTools
{
    /// <summary>Tiny software rasterizer used to paint placeholder pixel art deterministically.</summary>
    public sealed class PixelCanvas
    {
        private readonly Color32[] _pixels;

        public PixelCanvas(int width, int height)
        {
            Width = width;
            Height = height;
            _pixels = new Color32[width * height];
        }

        public int Width { get; }
        public int Height { get; }
        public Color32[] Pixels => _pixels;

        public Color32 Get(int x, int y) => InBounds(x, y) ? _pixels[y * Width + x] : default;

        public void Set(int x, int y, Color32 color)
        {
            if (InBounds(x, y)) _pixels[y * Width + x] = color;
        }

        public void Blend(int x, int y, Color color)
        {
            if (!InBounds(x, y)) return;
            Color under = _pixels[y * Width + x];
            float a = color.a + under.a * (1f - color.a);
            if (a <= 0f) return;
            Color result = (color * color.a + under * under.a * (1f - color.a)) / a;
            result.a = a;
            _pixels[y * Width + x] = result;
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public PixelCanvas Fill(Color32 color)
        {
            for (int i = 0; i < _pixels.Length; i++) _pixels[i] = color;
            return this;
        }

        public PixelCanvas Rect(int x, int y, int w, int h, Color32 color)
        {
            for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
                Set(xx, yy, color);
            return this;
        }

        public PixelCanvas Circle(float cx, float cy, float radius, Color32 color)
        {
            int minX = Mathf.FloorToInt(cx - radius), maxX = Mathf.CeilToInt(cx + radius);
            int minY = Mathf.FloorToInt(cy - radius), maxY = Mathf.CeilToInt(cy + radius);
            float r2 = radius * radius;
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                if (dx * dx + dy * dy <= r2) Set(x, y, color);
            }

            return this;
        }

        public PixelCanvas Ellipse(float cx, float cy, float rx, float ry, Color32 color)
        {
            for (int y = Mathf.FloorToInt(cy - ry); y <= Mathf.CeilToInt(cy + ry); y++)
            for (int x = Mathf.FloorToInt(cx - rx); x <= Mathf.CeilToInt(cx + rx); x++)
            {
                float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                if (dx * dx + dy * dy <= 1f) Set(x, y, color);
            }

            return this;
        }

        public PixelCanvas SoftCircle(float cx, float cy, float radius, Color color, float falloff = 2f)
        {
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) / radius;
                if (d >= 1f) continue;
                Color c = color;
                c.a *= Mathf.Pow(1f - d, falloff);
                Blend(x, y, c);
            }

            return this;
        }

        public PixelCanvas Line(int x0, int y0, int x1, int y1, Color32 color, int thickness = 1)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), 1);
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                Rect(x - thickness / 2, y - thickness / 2, thickness, thickness, color);
            }

            return this;
        }

        public PixelCanvas VerticalGradient(Color bottom, Color top)
        {
            for (int y = 0; y < Height; y++)
            {
                Color32 c = Color.Lerp(bottom, top, y / (float)Mathf.Max(1, Height - 1));
                for (int x = 0; x < Width; x++) Set(x, y, c);
            }

            return this;
        }

        public PixelCanvas Noise(float amount, int seed)
        {
            var random = new System.Random(seed);
            for (int i = 0; i < _pixels.Length; i++)
            {
                if (_pixels[i].a == 0) continue;
                float n = ((float)random.NextDouble() - 0.5f) * 2f * amount;
                Color c = _pixels[i];
                _pixels[i] = new Color(Mathf.Clamp01(c.r + n), Mathf.Clamp01(c.g + n), Mathf.Clamp01(c.b + n), c.a);
            }

            return this;
        }

        /// <summary>Adds a 1px dark outline around opaque pixels (readability on busy backgrounds).</summary>
        public PixelCanvas Outline(Color32 color)
        {
            var copy = (Color32[])_pixels.Clone();
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                if (copy[y * Width + x].a > 0) continue;
                bool edge = false;
                for (int oy = -1; oy <= 1 && !edge; oy++)
                for (int ox = -1; ox <= 1 && !edge; ox++)
                {
                    int nx = x + ox, ny = y + oy;
                    if (nx < 0 || ny < 0 || nx >= Width || ny >= Height) continue;
                    edge = copy[ny * Width + nx].a > 128;
                }

                if (edge) _pixels[y * Width + x] = color;
            }

            return this;
        }

        public PixelCanvas Tint(Color multiply)
        {
            for (int i = 0; i < _pixels.Length; i++)
            {
                Color c = _pixels[i];
                _pixels[i] = new Color(c.r * multiply.r, c.g * multiply.g, c.b * multiply.b, c.a * multiply.a);
            }

            return this;
        }

        public PixelCanvas Clone()
        {
            var copy = new PixelCanvas(Width, Height);
            Array.Copy(_pixels, copy._pixels, _pixels.Length);
            return copy;
        }

        public PixelCanvas FlipHorizontal()
        {
            var copy = (Color32[])_pixels.Clone();
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                _pixels[y * Width + x] = copy[y * Width + (Width - 1 - x)];
            return this;
        }

        /// <summary>Tangent-space normal map from luminance (tiles) or alpha bevel (sprites).</summary>
        public PixelCanvas ToNormalMap(float strength, bool fromAlpha)
        {
            var height = new float[Width * Height];
            for (int i = 0; i < _pixels.Length; i++)
            {
                Color c = _pixels[i];
                height[i] = fromAlpha ? c.a : c.grayscale * c.a;
            }

            if (fromAlpha) height = Blur(height, 2);
            var normal = new PixelCanvas(Width, Height);
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                float l = height[y * Width + Mathf.Max(0, x - 1)];
                float r = height[y * Width + Mathf.Min(Width - 1, x + 1)];
                float d = height[Mathf.Max(0, y - 1) * Width + x];
                float u = height[Mathf.Min(Height - 1, y + 1) * Width + x];
                Vector3 n = new Vector3((l - r) * strength, (d - u) * strength, 1f).normalized;
                normal.Set(x, y, new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f));
            }

            return normal;
        }

        private float[] Blur(float[] source, int radius)
        {
            var result = new float[source.Length];
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                float sum = 0f;
                int count = 0;
                for (int oy = -radius; oy <= radius; oy++)
                for (int ox = -radius; ox <= radius; ox++)
                {
                    int nx = Mathf.Clamp(x + ox, 0, Width - 1), ny = Mathf.Clamp(y + oy, 0, Height - 1);
                    sum += source[ny * Width + nx];
                    count++;
                }

                result[y * Width + x] = sum / count;
            }

            return result;
        }

        public byte[] EncodePng()
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            texture.SetPixels32(_pixels);
            texture.Apply();
            byte[] png = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);
            return png;
        }
    }
}
