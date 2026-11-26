using System;
using RootAccess.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RootAccess.EditorTools
{
    /// <summary>
    /// Look of one level: which tiles go where (pickers over tile coordinates, so windows line up in columns and
    /// pillars repeat), the ambient light, lamp colour, footstep surface, decor props and the parallax mood. The
    /// pickers return tile names; <see cref="Tile"/> resolves them to the Tile assets built by <see cref="AssetFactory"/>.
    /// </summary>
    internal sealed class LevelTheme
    {
        /// <summary>Picks a tile name from the cell, its row counted from the top and the bottom of the filled area.</summary>
        public delegate string Picker(int x, int y, int fromTop, int fromBottom);

        public Biome Biome { get; private set; }
        public bool Outdoor { get; private set; }
        public Color Ambient { get; private set; }
        public float AmbientIntensity { get; private set; }
        public Color Lamp { get; private set; }
        public Color BackdropTint { get; private set; } = new(0.62f, 0.6f, 0.7f);
        public SurfaceType GroundSurface { get; private set; } = SurfaceType.Concrete;
        public SurfaceType WallSurface { get; private set; } = SurfaceType.Concrete;
        /// <summary>Background decor prefabs scattered along floors (see <c>LevelLayout.DressFloors</c>).</summary>
        public string[] Decor { get; private set; } = Array.Empty<string>();
        /// <summary>Wall-mounted decor (posters, screens, pipes) placed higher up on interior backdrops.</summary>
        public string[] WallDecor { get; private set; } = Array.Empty<string>();
        /// <summary>Lamp prefab hung from interior ceilings / planted outdoors by <c>LevelLayout.Decor</c>.</summary>
        public string LampPrefab { get; private set; } = "StreetLamp";
        /// <summary>Show the city skyline behind the level (outdoors, or through big windows).</summary>
        public bool Skyline { get; private set; }

        public Picker Ground { get; private set; }
        public Picker Wall { get; private set; }
        public Picker Ceiling { get; private set; }
        public Picker Back { get; private set; }
        public string Platform => TileArt.Name(Biome, "plat", 0);

        private BuildContext _ctx;

        public TileBase Tile(string name) => _ctx.ThemeTile(name);

        public static int Hash(int x, int y)
        {
            unchecked
            {
                int h = x * 73856093 ^ y * 19349663;
                h ^= h >> 13;
                h *= 0x5bd1e995;
                return (h ^ (h >> 15)) & int.MaxValue;
            }
        }

        private string N(string role, int index) => TileArt.Name(Biome, role, index);

        /// <summary>A variant of <paramref name="role"/> by cell hash, among at most <paramref name="variants"/> that the biome has.</summary>
        private string Pick(string role, int x, int y, int variants) =>
            N(role, Hash(x, y) % Mathf.Max(1, Mathf.Min(variants, TileArt.Count(Biome, role))));

        public static LevelTheme For(Biome biome, BuildContext ctx)
        {
            var t = new LevelTheme { Biome = biome, _ctx = ctx };
            t.Wall = (x, y, _, _) => t.Pick("wall", x, y, 2);
            t.Ceiling = (x, y, _, _) => t.N("ceil", 0);
            t.Ground = (x, y, fromTop, _) => fromTop == 0 ? t.Pick("top", x, y, biome is Biome.Rooftop or Biome.Sewer ? 3 : 2) : t.Pick("body", x, y, 2);
            t.Back = (x, y, _, _) => t.Pick("back", x, y, 2);

            switch (biome)
            {
                case Biome.Rooftop:
                    t.Outdoor = true;
                    t.Skyline = true;
                    t.Ambient = new Color(0.62f, 0.62f, 0.95f);
                    t.AmbientIntensity = 0.55f;
                    t.Lamp = new Color(1f, 0.82f, 0.55f);
                    t.Decor = new[] { "Decor_WaterTower", "Decor_Antennas", "Decor_Vent", "Decor_SatDish", "Decor_Billboard", "Decor_RoofShack" };
                    t.Ground = (x, y, fromTop, _) =>
                    {
                        if (fromTop == 0) return t.Pick("top", x, y, 3);
                        if (fromTop >= 2 && fromTop % 3 == 2 && Mod(x, 4) == 1)
                        {
                            int roll = Hash(x, y) % 10;
                            return t.N("body", roll < 5 ? 3 : roll < 7 ? 4 : 5);
                        }

                        return t.N("body", Hash(x, y) % 10 < 8 ? Hash(y, x) % 2 : 2);
                    };
                    break;
                case Biome.Office:
                    t.Ambient = new Color(0.7f, 0.74f, 0.95f);
                    t.AmbientIntensity = 0.5f;
                    t.Lamp = new Color(0.85f, 0.93f, 1f);
                    t.GroundSurface = SurfaceType.Concrete;
                    t.LampPrefab = "Lamp_Office";
                    t.Decor = new[] { "Decor_Desk", "Decor_Plant", "Decor_Cabinet", "Decor_Cooler", "Decor_Vending", "Decor_Printer", "Decor_Chair" };
                    t.WallDecor = new[] { "Decor_Clock", "Decor_Whiteboard", "Decor_Poster" };
                    t.Ground = (x, y, fromTop, _) => fromTop == 0 ? t.Pick("top", x, y, 2) : t.N("body", Hash(x, y) % 10 == 0 ? 2 : Hash(x, y) % 2);
                    t.Ceiling = (x, y, _, _) => t.N("ceil", Mod(x, 6) == 3 ? 1 : 0);
                    t.Back = (x, y, fromTop, fromBottom) =>
                    {
                        if (Mod(x, 16) == 0) return t.N("back", 4);
                        if (fromBottom == 0) return t.N("back", 2);
                        if (fromTop == 0) return t.N("back", 1);
                        if (fromBottom >= 2 && fromBottom <= 4 && (Mod(x, 8) == 3 || Mod(x, 8) == 4)) return t.N("back", 3);
                        return t.N("back", 0);
                    };
                    break;
                case Biome.Server:
                    t.Ambient = new Color(0.5f, 0.7f, 1f);
                    t.AmbientIntensity = 0.48f;
                    t.Lamp = new Color(0.55f, 0.9f, 1f);
                    t.GroundSurface = SurfaceType.Metal;
                    t.WallSurface = SurfaceType.Metal;
                    t.LampPrefab = "Lamp_Server";
                    t.Decor = new[] { "Decor_Cooling", "Decor_RackOpen", "Decor_StatusScreen", "Decor_CableSpool" };
                    t.Ground = (x, y, fromTop, _) => fromTop == 0 ? t.Pick("top", x, y, 2) : t.Pick("body", x, y, 3);
                    t.Back = (x, y, fromTop, fromBottom) =>
                    {
                        if (fromBottom <= 3) return Mod(x, 7) == 3 ? t.N("back", 2) : t.N("back", Hash(x, 0) % 2);
                        if (fromBottom == 4) return t.N("back", 3);
                        return t.N("back", Mod(x, 6) == 0 ? 4 : 2);
                    };
                    break;
                case Biome.Sewer:
                    t.Ambient = new Color(0.55f, 0.85f, 0.68f);
                    t.AmbientIntensity = 0.46f;
                    t.Lamp = new Color(0.75f, 1f, 0.7f);
                    t.LampPrefab = "Lamp_Sewer";
                    t.Decor = new[] { "Decor_Barrel", "Decor_Valve", "Decor_PipeStack", "Decor_Ladder", "Decor_Junk" };
                    t.WallDecor = new[] { "Decor_PipeVertical", "Decor_HangingMoss" };
                    t.Ground = (x, y, fromTop, _) => fromTop == 0 ? t.Pick("top", x, y, 3) : t.N("body", Hash(x, y) % 12 == 0 ? 2 : Hash(x, y) % 2);
                    t.Back = (x, y, fromTop, fromBottom) =>
                    {
                        if (fromBottom == 0) return t.N("back", 2);
                        if (fromBottom == 2 && Mod(x, 9) == 4) return t.N("back", 3);
                        return t.Pick("back", x, y, 2);
                    };
                    break;
                case Biome.Hq:
                    t.Ambient = new Color(0.95f, 0.82f, 0.75f);
                    t.AmbientIntensity = 0.5f;
                    t.Lamp = new Color(1f, 0.9f, 0.72f);
                    t.LampPrefab = "Lamp_Hq";
                    t.Decor = new[] { "Decor_Statue", "Decor_PlantTall", "Decor_Bench", "Decor_Reception", "Decor_HoloLogo" };
                    t.WallDecor = new[] { "Decor_ArtPanel", "Decor_Screen" };
                    t.Back = (x, y, fromTop, fromBottom) =>
                    {
                        int m = Mod(x, 10);
                        if (m == 0) return t.N("back", 4);
                        if (fromBottom >= 1 && fromBottom <= 4 && m >= 4 && m <= 6) return t.N("back", 3);
                        if (fromBottom == 3 && m == 8) return t.N("back", 2);
                        return t.N("back", fromBottom == 0 ? 1 : 0);
                    };
                    break;
                case Biome.Spire:
                    t.Outdoor = true;
                    t.Skyline = true;
                    t.Ambient = new Color(0.58f, 0.66f, 1f);
                    t.AmbientIntensity = 0.5f;
                    t.Lamp = new Color(1f, 0.93f, 0.82f);
                    t.GroundSurface = SurfaceType.Metal;
                    t.WallSurface = SurfaceType.Metal;
                    t.LampPrefab = "Lamp_Beacon";
                    t.Decor = new[] { "Decor_Windsock", "Decor_Transformer", "Decor_Radar", "Decor_Mast", "Decor_SatDish", "Decor_Antennas" };
                    // Deck on top, lattice below with a horizontal chord every fourth row and the odd aviation light.
                    t.Ground = (x, y, fromTop, _) =>
                    {
                        if (fromTop == 0) return t.N("top", Hash(x, 7) % 6 == 0 ? 1 : 0);
                        if (fromTop % 4 == 3) return t.N("body", 1);
                        return t.N("body", Hash(x, y) % 29 == 0 ? 2 : 0);
                    };
                    t.Wall = (x, y, _, _) => t.N("wall", Mod(y, 6) == 0 ? 1 : 0);
                    break;
                case Biome.Hideout:
                    t.Ambient = new Color(0.75f, 0.6f, 0.9f);
                    t.AmbientIntensity = 0.5f;
                    t.Lamp = new Color(1f, 0.78f, 0.5f);
                    t.LampPrefab = "Lamp_Office";
                    t.Back = (x, y, fromTop, fromBottom) =>
                        Mod(x, 9) == 4 && (fromBottom == 4 || fromBottom == 5) ? t.N("back", 2) : t.Pick("back", x, y, 2);
                    break;
                default:
                    t.Ambient = new Color(0.8f, 0.82f, 0.95f);
                    t.AmbientIntensity = 0.62f;
                    t.Lamp = new Color(0.85f, 0.95f, 1f);
                    t.LampPrefab = "Lamp_Office";
                    t.GroundSurface = SurfaceType.Metal;
                    t.Back = (x, y, _, fromBottom) => t.N("back", fromBottom % 4 == 3 ? 1 : 0);
                    break;
            }

            return t;
        }

        private static int Mod(int a, int m) => ((a % m) + m) % m;
    }
}
