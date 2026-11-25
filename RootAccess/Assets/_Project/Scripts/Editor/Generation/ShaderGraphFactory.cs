using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Node = RootAccess.EditorTools.ShaderGraphWriter.Node;
using Property = RootAccess.EditorTools.ShaderGraphWriter.Property;
using Sample = RootAccess.EditorTools.ShaderGraphWriter.SampleSlots;
using Split = RootAccess.EditorTools.ShaderGraphWriter.SplitSlots;

namespace RootAccess.EditorTools
{
    /// <summary>
    /// Generates the project's Shader Graph assets (rubric item 2: hologram, dissolve/glitch, neon pulse; item 3: grass
    /// wind that bends around the player) and verifies that each one imports as an error-free shader. The graphs are built
    /// from regular Shader Graph nodes, so they can be opened, read and tweaked in the Shader Graph editor.
    /// </summary>
    internal static class ShaderGraphFactory
    {
        public const string Hologram = "SG_Hologram";
        public const string Dissolve = "SG_SpriteDissolve";
        public const string NeonPulse = "SG_NeonPulse";
        public const string GrassWind = "SG_GrassWind";
        public const string WaterDistortion = "SG_WaterDistortion";

        /// <summary>Global texture of the 2D renderer: everything drawn up to <c>Renderer2DData.cameraSortingLayerTextureBound</c>.</summary>
        public const string CameraSortingLayerTexture = "_CameraSortingLayerTexture";

        private const string GraphCategory = "RootAccess";
        private const string Folder = EditorPaths.Shaders + "/Graphs";
        private const string IncludePath = EditorPaths.Shaders + "/RootAccessShaderGraph.hlsl";
        private const int WhiteTexture = 0;
        private const int BumpTexture = 3;
        private const int WorldSpace = 2;
        private const int ObjectSpace = 0;

        public static readonly string[] All = { Hologram, Dissolve, NeonPulse, GrassWind, WaterDistortion };

        public static string ShaderName(string graph) => $"{GraphCategory}/{graph}";

        public static string PathOf(string graph) => $"{Folder}/{graph}.shadergraph";

        public static void BuildAll()
        {
            EditorAssetUtil.EnsureFolder(Folder);
            AssetDatabase.ImportAsset(IncludePath, ImportAssetOptions.ForceSynchronousImport);
            string includeGuid = AssetDatabase.AssetPathToGUID(IncludePath);
            if (string.IsNullOrEmpty(includeGuid)) throw new InvalidOperationException($"Shader Graph include missing: {IncludePath}");

            Write(Hologram, BuildHologram());
            Write(Dissolve, BuildDissolve());
            Write(NeonPulse, BuildNeonPulse());
            Write(GrassWind, BuildGrassWind(includeGuid));
            Write(WaterDistortion, BuildWaterDistortion());
            foreach (string graph in All) Validate(graph);
        }

        // ---------------------------------------------------------------- graphs

        /// <summary>Additive hologram: tinted sprite × world-space scanlines × flicker.</summary>
        private static string BuildHologram()
        {
            var g = new ShaderGraphWriter(GraphCategory, Hologram, ShaderGraphWriter.SpriteTarget.Unlit, ShaderGraphWriter.BlendMode.Additive);
            Property mainTex = g.Texture("_MainTex", "Sprite Texture", true, WhiteTexture);
            Property tint = g.Color("_Tint", "Tint", new Color(0.3f, 1.6f, 2.2f, 1f), true);
            Property density = g.Float("_ScanlineDensity", "Scanline Density", 28f);
            Property scanSpeed = g.Float("_ScanSpeed", "Scan Speed", 1.5f);
            Property thickness = g.Float("_ScanlineEdge", "Scanline Thickness", 0.5f, new Vector2(0f, 1f));
            Property dim = g.Float("_ScanlineDim", "Scanline Dim", 0.55f, new Vector2(0f, 1f));
            Property flickerSpeed = g.Float("_FlickerSpeed", "Flicker Speed", 9f);
            Property flickerMin = g.Float("_FlickerMin", "Flicker Minimum", 0.85f, new Vector2(0f, 1f));
            Property opacity = g.Float("_Opacity", "Opacity", 0.85f, new Vector2(0f, 1f));

            Node tinted = TintedSprite(g, mainTex, tint, out Node spriteColor);

            Node worldY = g.At(-1500f, 520f).Position(WorldSpace);
            Node splitPos = g.At(-1320f, 520f).Unary("SplitNode", worldY, 0);
            Node scaledY = g.At(-1120f, 520f).Binary("MultiplyNode", splitPos, Split.G, g.At(-1320f, 640f).PropertyNode(density), 0);
            Node time = g.At(-1500f, 760f).Add("TimeNode");
            Node scroll = g.At(-1120f, 700f).Binary("MultiplyNode", time, ShaderGraphWriter.TimeOut, g.At(-1320f, 800f).PropertyNode(scanSpeed), 0);
            Node scanArg = g.At(-920f, 600f).Binary("SubtractNode", scaledY, ShaderGraphWriter.BinaryOut, scroll, ShaderGraphWriter.BinaryOut);
            Node scanFrac = g.At(-740f, 600f).Unary("FractionNode", scanArg, ShaderGraphWriter.BinaryOut);
            Node band = g.At(-560f, 600f).Binary("StepNode", g.At(-740f, 720f).PropertyNode(thickness), 0, scanFrac, ShaderGraphWriter.UnaryOut);
            Node scan = Lerp(g, -380f, 600f, g.At(-560f, 740f).PropertyNode(dim), 0, band, ShaderGraphWriter.BinaryOut);

            Node flickerArg = g.At(-1120f, 900f).Binary("MultiplyNode", time, ShaderGraphWriter.TimeOut, g.At(-1320f, 960f).PropertyNode(flickerSpeed), 0);
            Node sine = g.At(-920f, 900f).Unary("SineNode", flickerArg, ShaderGraphWriter.BinaryOut);
            Node wave = g.At(-740f, 900f).Unary("AbsoluteNode", sine, ShaderGraphWriter.UnaryOut);
            Node flicker = Lerp(g, -380f, 860f, g.At(-560f, 980f).PropertyNode(flickerMin), 0, wave, ShaderGraphWriter.UnaryOut);

            Node scanned = g.At(-200f, 200f).Binary("MultiplyNode", tinted, ShaderGraphWriter.BinaryOut, scan, ShaderGraphWriter.LerpOut);
            Node lit = g.At(-60f, 260f).Binary("MultiplyNode", scanned, ShaderGraphWriter.BinaryOut, flicker, ShaderGraphWriter.LerpOut);
            g.Connect(lit, ShaderGraphWriter.BinaryOut, g.BaseColor, 0);

            Node split = g.At(-560f, 360f).Unary("SplitNode", spriteColor, ShaderGraphWriter.BinaryOut);
            Node alpha = g.At(-200f, 400f).Binary("MultiplyNode", split, Split.A, g.At(-380f, 460f).PropertyNode(opacity), 0);
            g.Connect(alpha, ShaderGraphWriter.BinaryOut, g.Alpha, 0);
            return g.Serialize();
        }

        /// <summary>Lit sprite with a noise dissolve and an HDR edge (teleports, glitching hacked terminals, death).</summary>
        private static string BuildDissolve()
        {
            var g = new ShaderGraphWriter(GraphCategory, Dissolve, ShaderGraphWriter.SpriteTarget.Lit, ShaderGraphWriter.BlendMode.Alpha);
            Property mainTex = g.Texture("_MainTex", "Diffuse", true, WhiteTexture);
            Property maskTex = g.Texture("_MaskTex", "Mask", false, WhiteTexture);
            Property normalMap = g.Texture("_NormalMap", "Normal Map", false, BumpTexture);
            Property amount = g.Float("_DissolveAmount", "Dissolve Amount", 0f, new Vector2(0f, 1f));
            Property edgeColor = g.Color("_EdgeColor", "Edge Color", new Color(0.2f, 2.5f, 3f, 1f), true);
            Property edgeWidth = g.Float("_EdgeWidth", "Edge Width", 0.08f, new Vector2(0.01f, 0.3f));
            Property noiseScale = g.Float("_NoiseScale", "Noise Scale", 18f);

            Node spriteColor = SpriteColor(g, mainTex);
            LitInputs(g, maskTex, normalMap);

            Node noise = g.At(-1100f, 760f).Add("NoiseNode", 1, new[] { ("m_HashType", "0") });
            g.Connect(g.At(-1320f, 820f).PropertyNode(noiseScale), 0, noise, ShaderGraphWriter.NoiseScale);
            Node amountNode = g.At(-1100f, 960f).PropertyNode(amount);
            Node visible = g.At(-860f, 760f).Binary("StepNode", amountNode, 0, noise, ShaderGraphWriter.NoiseOut);
            Node active = g.At(-860f, 1000f).Unary("CeilingNode", amountNode, 0);
            Node width = g.At(-680f, 1000f).Binary("MultiplyNode", g.At(-860f, 1120f).PropertyNode(edgeWidth), 0, active, ShaderGraphWriter.UnaryOut);
            Node limit = g.At(-500f, 960f).Binary("AddNode", amountNode, 0, width, ShaderGraphWriter.BinaryOut);
            Node beyond = g.At(-320f, 900f).Binary("StepNode", limit, ShaderGraphWriter.BinaryOut, noise, ShaderGraphWriter.NoiseOut);
            Node edge = g.At(-320f, 760f).Binary("SubtractNode", visible, ShaderGraphWriter.BinaryOut, beyond, ShaderGraphWriter.BinaryOut);
            Node glow = g.At(-160f, 700f).Binary("MultiplyNode", g.At(-320f, 640f).PropertyNode(edgeColor), 0, edge, ShaderGraphWriter.BinaryOut);
            Node color = g.At(-160f, 200f).Binary("AddNode", spriteColor, ShaderGraphWriter.BinaryOut, glow, ShaderGraphWriter.BinaryOut);
            g.Connect(color, ShaderGraphWriter.BinaryOut, g.BaseColor, 0);

            Node split = g.At(-500f, 360f).Unary("SplitNode", spriteColor, ShaderGraphWriter.BinaryOut);
            Node alpha = g.At(-160f, 420f).Binary("MultiplyNode", split, Split.A, visible, ShaderGraphWriter.BinaryOut);
            g.Connect(alpha, ShaderGraphWriter.BinaryOut, g.Alpha, 0);
            return g.Serialize();
        }

        /// <summary>HDR emissive neon tube: brightness pulses over time, phase-shifted along world X so signs do not blink in sync.</summary>
        private static string BuildNeonPulse()
        {
            var g = new ShaderGraphWriter(GraphCategory, NeonPulse, ShaderGraphWriter.SpriteTarget.Unlit, ShaderGraphWriter.BlendMode.Alpha);
            Property mainTex = g.Texture("_MainTex", "Sprite Texture", true, WhiteTexture);
            Property emission = g.Color("_EmissionColor", "Emission", new Color(3f, 0.4f, 2.4f, 1f), true);
            Property pulseSpeed = g.Float("_PulseSpeed", "Pulse Speed", 2f);
            Property pulseMin = g.Float("_PulseMin", "Pulse Minimum", 0.55f, new Vector2(0f, 1f));
            Property phaseByX = g.Float("_PhaseByX", "Phase by World X", 0.5f);

            Node emissive = TintedSprite(g, mainTex, emission, out Node spriteColor);

            Node world = g.At(-1500f, 560f).Position(WorldSpace);
            Node splitPos = g.At(-1320f, 560f).Unary("SplitNode", world, 0);
            Node phase = g.At(-1120f, 560f).Binary("MultiplyNode", splitPos, Split.R, g.At(-1320f, 680f).PropertyNode(phaseByX), 0);
            Node time = g.At(-1320f, 780f).Add("TimeNode");
            Node scaled = g.At(-1120f, 760f).Binary("MultiplyNode", time, ShaderGraphWriter.TimeOut, g.At(-1320f, 900f).PropertyNode(pulseSpeed), 0);
            Node argument = g.At(-920f, 660f).Binary("AddNode", scaled, ShaderGraphWriter.BinaryOut, phase, ShaderGraphWriter.BinaryOut);
            Node sine = g.At(-740f, 660f).Unary("SineNode", argument, ShaderGraphWriter.BinaryOut);
            Node wave = g.At(-560f, 660f).Unary("AbsoluteNode", sine, ShaderGraphWriter.UnaryOut);
            Node pulse = Lerp(g, -380f, 620f, g.At(-560f, 780f).PropertyNode(pulseMin), 0, wave, ShaderGraphWriter.UnaryOut);

            Node lit = g.At(-200f, 200f).Binary("MultiplyNode", emissive, ShaderGraphWriter.BinaryOut, pulse, ShaderGraphWriter.LerpOut);
            g.Connect(lit, ShaderGraphWriter.BinaryOut, g.BaseColor, 0);
            Node split = g.At(-380f, 380f).Unary("SplitNode", spriteColor, ShaderGraphWriter.BinaryOut);
            g.Connect(split, Split.A, g.Alpha, 0);
            return g.Serialize();
        }

        /// <summary>Lit foliage: the vertex stage sways with the global wind and bends away from the player (Custom Function).</summary>
        private static string BuildGrassWind(string includeGuid)
        {
            var g = new ShaderGraphWriter(GraphCategory, GrassWind, ShaderGraphWriter.SpriteTarget.Lit, ShaderGraphWriter.BlendMode.Alpha);
            Property mainTex = g.Texture("_MainTex", "Diffuse", true, WhiteTexture);
            Property maskTex = g.Texture("_MaskTex", "Mask", false, WhiteTexture);
            Property normalMap = g.Texture("_NormalMap", "Normal Map", false, BumpTexture);
            Property windWeight = g.Float("_WindWeight", "Wind Weight", 1f, new Vector2(0f, 3f));
            Property bendRadius = g.Float("_BendRadius", "Player Bend Radius", 1.2f);
            Property bendStrength = g.Float("_BendStrength", "Player Bend Strength", 0.35f);

            Node position = g.At(-900f, -520f).Position(ObjectSpace);
            Node uv = g.At(-900f, -400f).Add("UVNode", 0, new[] { ("m_OutputChannel", "0") });
            Node foliage = g.At(-560f, -460f).CustomFunction("Foliage Wind", "RA_FoliagePosition", includeGuid,
                new[] { ("PositionOS", 3), ("UV", 2), ("WindWeight", 1), ("BendRadius", 1), ("BendStrength", 1) },
                new[] { ("Out", 3) });
            g.Connect(position, 0, foliage, 0);
            g.Connect(uv, 0, foliage, 1);
            g.Connect(g.At(-900f, -280f).PropertyNode(windWeight), 0, foliage, 2);
            g.Connect(g.At(-900f, -220f).PropertyNode(bendRadius), 0, foliage, 3);
            g.Connect(g.At(-900f, -160f).PropertyNode(bendStrength), 0, foliage, 4);
            g.Connect(foliage, 5, g.VertexPosition, 0);

            Node spriteColor = SpriteColor(g, mainTex);
            LitInputs(g, maskTex, normalMap);
            g.Connect(spriteColor, ShaderGraphWriter.BinaryOut, g.BaseColor, 0);
            Node split = g.At(-380f, 380f).Unary("SplitNode", spriteColor, ShaderGraphWriter.BinaryOut);
            g.Connect(split, Split.A, g.Alpha, 0);
            return g.Serialize();
        }

        /// <summary>
        /// Underwater refraction for the water mesh: samples what the 2D renderer drew behind the water
        /// (<see cref="CameraSortingLayerTexture"/>) at a screen UV wobbled by scrolling world-space noise, then tints it
        /// with the mesh's vertex colours (light surface band, dark depths) and adds a share of that colour on top.
        /// </summary>
        private static string BuildWaterDistortion()
        {
            var g = new ShaderGraphWriter(GraphCategory, WaterDistortion, ShaderGraphWriter.SpriteTarget.Unlit, ShaderGraphWriter.BlendMode.Alpha);
            Property scene = g.Texture(CameraSortingLayerTexture, "Camera Sorting Layer Texture", false, WhiteTexture, exposed: false);
            Property tint = g.Color("_Tint", "Tint", new Color(0.55f, 0.85f, 1f, 1f), false);
            Property strength = g.Float("_Strength", "Distortion Strength", 0.012f, new Vector2(0f, 0.05f));
            Property speed = g.Float("_Speed", "Flow Speed", 0.6f);
            Property noiseScale = g.Float("_NoiseScale", "Ripple Scale", 2.5f);
            Property glow = g.Float("_Glow", "Water Colour", 0.35f, new Vector2(0f, 1f));

            Node world = g.At(-1700f, 300f).Position(WorldSpace);
            Node time = g.At(-1700f, 460f).Add("TimeNode");
            Node flow = g.At(-1500f, 460f).Binary("MultiplyNode", time, ShaderGraphWriter.TimeOut, g.At(-1700f, 560f).PropertyNode(speed), 0);
            Node noiseUv = g.At(-1300f, 360f).Binary("AddNode", world, 0, flow, ShaderGraphWriter.BinaryOut);
            Node noise = g.At(-1100f, 360f).Add("NoiseNode", 1, new[] { ("m_HashType", "0") });
            g.Connect(noiseUv, ShaderGraphWriter.BinaryOut, noise, ShaderGraphWriter.NoiseUv);
            g.Connect(g.At(-1300f, 520f).PropertyNode(noiseScale), 0, noise, ShaderGraphWriter.NoiseScale);
            Node offset = g.At(-880f, 400f).Binary("MultiplyNode", noise, ShaderGraphWriter.NoiseOut, g.At(-1100f, 540f).PropertyNode(strength), 0);
            Node screen = g.At(-880f, 160f).Add("ScreenPositionNode", 0, new[] { ("m_ScreenSpaceType", "0") });
            Node uv = g.At(-680f, 260f).Binary("AddNode", screen, 0, offset, ShaderGraphWriter.BinaryOut);

            Node sample = g.At(-420f, 120f).SampleTexture(scene);
            g.Connect(uv, ShaderGraphWriter.BinaryOut, sample, Sample.Uv);
            Node vertexColor = g.At(-420f, 400f).Add("VertexColorNode");
            Node shaded = g.At(-200f, 160f).Binary("MultiplyNode", sample, Sample.Rgba, vertexColor, 0);
            Node tinted = g.At(-40f, 200f).Binary("MultiplyNode", shaded, ShaderGraphWriter.BinaryOut, g.At(-200f, 320f).PropertyNode(tint), 0);
            // Own colour on top of the refraction, so the water reads even over a black background (bright surface band).
            Node own = g.At(-40f, 400f).Binary("MultiplyNode", vertexColor, 0, g.At(-200f, 520f).PropertyNode(glow), 0);
            Node water = g.At(120f, 260f).Binary("AddNode", tinted, ShaderGraphWriter.BinaryOut, own, ShaderGraphWriter.BinaryOut);
            g.Connect(water, ShaderGraphWriter.BinaryOut, g.BaseColor, 0);
            Node split = g.At(-200f, 460f).Unary("SplitNode", vertexColor, 0);
            g.Connect(split, Split.A, g.Alpha, 0);
            return g.Serialize();
        }

        // ---------------------------------------------------------------- shared fragments

        /// <summary>Sprite texture × SpriteRenderer colour (vertex colour); output slot 2 of the returned Multiply node.</summary>
        private static Node SpriteColor(ShaderGraphWriter g, Property mainTex)
        {
            Node texture = g.At(-1100f, 0f).SampleTexture(mainTex);
            Node vertexColor = g.At(-1100f, 260f).Add("VertexColorNode");
            return g.At(-860f, 120f).Binary("MultiplyNode", texture, Sample.Rgba, vertexColor, 0);
        }

        private static Node TintedSprite(ShaderGraphWriter g, Property mainTex, Property tint, out Node spriteColor)
        {
            spriteColor = SpriteColor(g, mainTex);
            return g.At(-620f, 120f).Binary("MultiplyNode", spriteColor, ShaderGraphWriter.BinaryOut, g.At(-860f, 260f).PropertyNode(tint), 0);
        }

        /// <summary>Mask and normal map of the 2D renderer, wired straight into the Sprite Lit blocks.</summary>
        private static void LitInputs(ShaderGraphWriter g, Property maskTex, Property normalMap)
        {
            Node mask = g.At(-700f, -180f).SampleTexture(maskTex);
            g.Connect(mask, Sample.Rgba, g.SpriteMask, 0);
            Node normal = g.At(-700f, 480f).SampleTexture(normalMap, normalMap: true);
            g.Connect(normal, Sample.Rgba, g.NormalTS, 0);
        }

        /// <summary>Lerp(A, 1, T): slot B keeps its default of 1.</summary>
        private static Node Lerp(ShaderGraphWriter g, float x, float y, Node a, int aSlot, Node t, int tSlot)
        {
            Node lerp = g.At(x, y).Add("LerpNode");
            g.Connect(a, aSlot, lerp, ShaderGraphWriter.LerpA);
            g.Connect(t, tSlot, lerp, ShaderGraphWriter.LerpT);
            return lerp;
        }

        // ---------------------------------------------------------------- io + validation

        private static void Write(string graph, string content)
        {
            string path = PathOf(graph);
            string fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath) && File.ReadAllText(fullPath) == content) return;
            File.WriteAllText(fullPath, content, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static void Validate(string graph)
        {
            string path = PathOf(graph);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader == null) throw new InvalidOperationException($"{path} did not import as a shader.");
            ShaderMessage[] errors = ShaderUtil.GetShaderMessages(shader).Where(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
            if (ShaderUtil.ShaderHasError(shader) || errors.Length > 0)
                throw new InvalidOperationException($"{path} has shader errors: {string.Join(" | ", errors.Select(e => $"{e.message} (line {e.line})"))}");
            if (shader.name != ShaderName(graph))
                throw new InvalidOperationException($"{path} imported as '{shader.name}', expected '{ShaderName(graph)}'.");
            EditorAssetUtil.Log($"Shader Graph {shader.name}: {shader.GetPropertyCount()} properties, {shader.passCount} passes, no errors.");
        }
    }
}
