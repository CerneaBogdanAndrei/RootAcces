using UnityEditor;
using UnityEngine;

namespace RootAccess.EditorTools
{
    /// <summary>Names and paths of every procedurally generated asset, shared by the generator and the builders.</summary>
    public static class GeneratedArt
    {
        public const int PixelsPerUnit = 32;
        public const string NormalSuffix = "_n";
        public const string NormalMapProperty = "_NormalMap";

        public static string SpritePath(string name) => $"{EditorPaths.Sprites}/{name}.png";
        public static string NormalPath(string name) => $"{EditorPaths.Sprites}/Normals/{name}{NormalSuffix}.png";
        public static string TilePath(string name) => $"{EditorPaths.Tilesets}/{name}.png";
        public static string TileNormalPath(string name) => $"{EditorPaths.Tilesets}/{name}{NormalSuffix}.png";
        public static string SkyboxFacePath(string face) => $"{EditorPaths.Skybox}/sky_{face}.png";
        public static string SfxPath(string name) => $"{EditorPaths.Sfx}/{name}.wav";
        public static string MusicPath(string name) => $"{EditorPaths.Music}/{name}.wav";
        public static string AmbiencePath(string name) => $"{EditorPaths.Ambience}/{name}.wav";

        public static Sprite Sprite(string name) => Require<Sprite>(SpritePath(name));
        public static Sprite Tile(string name) => Require<Sprite>(TilePath(name));
        public static AudioClip Sfx(string name) => Require<AudioClip>(SfxPath(name));
        public static AudioClip Music(string name) => Require<AudioClip>(MusicPath(name));
        public static AudioClip Ambience(string name) => Require<AudioClip>(AmbiencePath(name));
        public static Texture2D Texture(string path) => Require<Texture2D>(path);

        public static T Require<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new System.InvalidOperationException($"Generated asset missing: {path}. Run Root Access/Setup/2. Generate Placeholder Art first.");
            return asset;
        }
    }
}
