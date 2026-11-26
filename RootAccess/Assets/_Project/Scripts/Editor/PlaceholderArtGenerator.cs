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
        private static readonly Color32 Cyan = new(46, 242, 255, 255);
        private static readonly Color32 Magenta = new(255, 46, 166, 255);
        private static readonly Color32 White = new(255, 255, 255, 255);

        private sealed class SpriteSpec
        {
            public string Path;
            public Vector2 Pivot;
            public string NormalPath;
            public bool Bilinear;
        }

        private static readonly List<SpriteSpec> Sprites = new();
        private static readonly List<string> NormalMaps = new();
        private static readonly List<string> PlainTextures = new();
        private static readonly HashSet<string> Written = new();

        /// <summary>Tree sprite rigged with bones for SpriteSkin (see <see cref="SpriteRigFactory"/>).</summary>
        public const string RiggedTree = "tree_rigged";

        [MenuItem("Root Access/Setup/2. Generate Placeholder Art")]
        public static void GenerateAll()
        {
            EditorAssetUtil.RunBatchStep("PlaceholderArtGenerator", () =>
            {
                Sprites.Clear();
                NormalMaps.Clear();
                PlainTextures.Clear();
                Written.Clear();
                EditorAssetUtil.EnsureFolder(EditorPaths.Sprites + "/Normals");

                PaintGameplay();
                PaintDecor();
                PaintEffectsSprites();
                PaintVfxTextures();
                PaintTiles();
                PaintSkybox();
                GenerateAudio();

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                RemoveStale(EditorPaths.Sprites);
                RemoveStale(EditorPaths.Tilesets);
                ConfigureImporters();
                SpriteRigFactory.RigVerticalChain(GeneratedArt.SpritePath(RiggedTree), SpriteRigFactory.TreeBones);
                EditorAssetUtil.Log($"Generated {Sprites.Count} sprites, {NormalMaps.Count} normal maps, {PlainTextures.Count} textures.");
            });
        }

        // ---------------------------------------------------------------- painted art (Generation/Art)

        /// <summary>Characters, interactables, enemies, the rigged tree, grass and skylines (see <see cref="GameplayArt"/>).</summary>
        private static void PaintGameplay()
        {
            foreach ((string name, PixelCanvas canvas, Vector2 pivot) in GameplayArt.All())
                SaveSprite(name, canvas, pivot, withNormal: !name.StartsWith("skyline") && !name.StartsWith("cloud") && name != "arc_bolt" &&
                                                            name != "alert_icon" && name != GameplayArt.KeyArtSprite);
        }

        /// <summary>Background props and lamps of every biome (see <see cref="PropArt"/>).</summary>
        private static void PaintDecor()
        {
            foreach (DecorSpec spec in PropArt.Decor) SaveSprite(spec.Sprite, spec.Paint(), spec.Pivot, withNormal: !spec.Hologram);
        }

        /// <summary>Flat/emissive sprites that are tinted at runtime or drawn with unlit materials.</summary>
        private static void PaintEffectsSprites()
        {
            SaveSprite("drone_eye", new PixelCanvas(8, 8).Circle(4, 4, 3.5f, White), new Vector2(0.5f, 0.5f));
            SaveSprite("projectile", new PixelCanvas(14, 6).SoftCircle(10, 3, 4, Color.white, 0.8f).Rect(0, 2, 10, 2, Magenta).Rect(8, 2, 4, 2, White), new Vector2(0.5f, 0.5f));
            SaveSprite("terminal_screen", new PixelCanvas(20, 12).Fill(White).Rect(2, 2, 16, 1, new Color32(255, 255, 255, 120)).Rect(2, 5, 10, 1, new Color32(255, 255, 255, 150)).Rect(2, 8, 13, 1, new Color32(255, 255, 255, 150)), new Vector2(0.5f, 0.5f));

            var node = new PixelCanvas(32, 32);
            for (int i = 0; i < 16; i++)
            {
                node.Rect(16 - i, 16 + (15 - i), 2, 1, White).Rect(16 + i - 1, 16 + (15 - i), 2, 1, White);
                node.Rect(16 - i, 16 - (15 - i), 2, 1, White).Rect(16 + i - 1, 16 - (15 - i), 2, 1, White);
            }

            node.Circle(16, 16, 5, White).Circle(16, 16, 2.5f, new Color32(255, 255, 255, 120));
            for (int a = 0; a < 360; a += 45)
            {
                float rad = a * Mathf.Deg2Rad;
                node.Set(16 + Mathf.RoundToInt(Mathf.Cos(rad) * 9), 16 + Mathf.RoundToInt(Mathf.Sin(rad) * 9), White);
            }

            SaveSprite("network_node", node, new Vector2(0.5f, 0.5f));
            SaveSprite("laser_beam", new PixelCanvas(4, 32).Rect(1, 0, 2, 32, White).Rect(0, 0, 1, 32, new Color32(255, 255, 255, 90)).Rect(3, 0, 1, 32, new Color32(255, 255, 255, 90)), new Vector2(0.5f, 0f));
            SaveSprite("hide_shadow", new PixelCanvas(32, 32).SoftCircle(16, 16, 16, new Color(0.02f, 0.02f, 0.06f, 0.9f), 0.6f), new Vector2(0.5f, 0.5f), bilinear: true);
            SaveSprite("exit_arrow", new PixelCanvas(24, 24).Rect(2, 9, 12, 6, Cyan).Line(12, 2, 22, 12, Cyan, 3).Line(12, 22, 22, 12, Cyan, 3), new Vector2(0.5f, 0.5f));
            SaveSprite("sky_gradient", new PixelCanvas(8, 256).VerticalGradient(new Color(0.2f, 0.06f, 0.24f), new Color(0.01f, 0.01f, 0.05f)), new Vector2(0.5f, 0.5f), bilinear: true);
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

        /// <summary>Biome tiles (<see cref="TileArt"/>) plus the see-through window pane.</summary>
        private static void PaintTiles()
        {
            var glass = new PixelCanvas(32, 32).Fill(new Color32(120, 200, 255, 90));
            glass.Line(4, 4, 20, 28, new Color32(255, 255, 255, 120), 2).Rect(0, 0, 32, 2, new Color32(70, 90, 110, 255)).Rect(0, 30, 32, 2, new Color32(70, 90, 110, 255));
            SaveTile("tile_glass", glass, withNormal: false);

            foreach ((string name, PixelCanvas canvas) in TileArt.PaintAll()) SaveTile(name, canvas);
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
                WriteAsset(normalPath, (canvas.HasNormals ? canvas.PaintedNormalMap() : canvas.ToNormalMap(3f, fromAlpha: true)).EncodePng());
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
                WriteAsset(normalPath, (canvas.HasNormals ? canvas.PaintedNormalMap() : canvas.ToNormalMap(4f, fromAlpha: false)).EncodePng());
                NormalMaps.Add(normalPath);
            }

            Sprites.Add(new SpriteSpec { Path = path, Pivot = new Vector2(0.5f, 0.5f), NormalPath = normalPath });
        }

        /// <summary>Deletes generated PNGs a previous run wrote but this one did not (renamed or retired art).</summary>
        private static void RemoveStale(string folder)
        {
            int removed = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png") || Written.Contains(path)) continue;
                AssetDatabase.DeleteAsset(path);
                removed++;
            }

            if (removed > 0) EditorAssetUtil.Log($"Removed {removed} stale texture(s) from {folder}.");
        }

        private static void WriteAsset(string assetPath, byte[] bytes)
        {
            Written.Add(assetPath);
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
