using System.Collections.Generic;

namespace RootAccess.EditorTools
{
    /// <summary>
    /// Single list of the painted art (characters, tiles, props, backdrops) with the sprite names the builders use.
    /// <see cref="PlaceholderArtGenerator"/> saves it as assets; <see cref="ArtPreview"/> draws it into contact sheets.
    /// </summary>
    internal static class ArtCatalog
    {
        /// <summary>Character frames keyed by sprite name (player_*, guard_*, heavy_*, sniper_*, fixer_*).</summary>
        public static Dictionary<string, PixelCanvas> Characters()
        {
            var all = new Dictionary<string, PixelCanvas>();
            Add(all, "player_", CharacterArt.PlayerFrames());
            Add(all, "guard_", CharacterArt.GuardFrames(CharacterArt.GuardStyle()));
            Add(all, "heavy_", CharacterArt.GuardFrames(CharacterArt.HeavyStyle()));
            Add(all, "sniper_", CharacterArt.GuardFrames(CharacterArt.SniperStyle()));
            Add(all, "fixer_", CharacterArt.FixerFrames());
            return all;
        }

        /// <summary>Contact sheets for <see cref="ArtPreview"/>: characters, every biome's tiles, props and gameplay sprites.</summary>
        public static IEnumerable<(string name, Dictionary<string, PixelCanvas> frames, int scale)> PreviewSheets()
        {
            yield return ("player", CharacterArt.PlayerFrames(), 4);
            yield return ("guards", Merge(
                CharacterArt.GuardFrames(CharacterArt.GuardStyle()),
                CharacterArt.GuardFrames(CharacterArt.HeavyStyle()),
                CharacterArt.GuardFrames(CharacterArt.SniperStyle())), 4);
            yield return ("fixer", CharacterArt.FixerFrames(), 4);

            var tiles = new Dictionary<string, Dictionary<string, PixelCanvas>>();
            foreach ((string name, PixelCanvas canvas) in TileArt.PaintAll())
            {
                string biome = name.Substring(0, name.IndexOf('_'));
                if (!tiles.TryGetValue(biome, out Dictionary<string, PixelCanvas> set)) tiles[biome] = set = new Dictionary<string, PixelCanvas>();
                set[name] = canvas;
            }

            foreach (KeyValuePair<string, Dictionary<string, PixelCanvas>> biome in tiles) yield return ($"tiles_{biome.Key}", biome.Value, 3);

            var props = new Dictionary<string, PixelCanvas>();
            foreach (DecorSpec spec in PropArt.Decor) props[spec.Sprite] = spec.Paint();
            yield return ("props", props, 2);

            var gameplay = new Dictionary<string, PixelCanvas>();
            var skylines = new Dictionary<string, PixelCanvas>();
            foreach ((string name, PixelCanvas canvas, _) in GameplayArt.All())
            {
                if (name.StartsWith("skyline")) skylines[name] = canvas;
                else if (!name.StartsWith("player_") && !name.StartsWith("guard_") && !name.StartsWith("heavy_") && !name.StartsWith("sniper_") && !name.StartsWith("fixer_"))
                    gameplay[name] = canvas;
            }

            yield return ("gameplay", gameplay, 3);
            yield return ("skylines", skylines, 1);
        }

        private static void Add(Dictionary<string, PixelCanvas> into, string prefix, Dictionary<string, PixelCanvas> frames)
        {
            foreach (KeyValuePair<string, PixelCanvas> frame in frames) into[prefix + frame.Key] = frame.Value;
        }

        private static Dictionary<string, PixelCanvas> Merge(params Dictionary<string, PixelCanvas>[] sets)
        {
            var merged = new Dictionary<string, PixelCanvas>();
            for (int i = 0; i < sets.Length; i++)
                foreach (KeyValuePair<string, PixelCanvas> frame in sets[i])
                    merged[$"{i}_{frame.Key}"] = frame.Value;
            return merged;
        }
    }
}
