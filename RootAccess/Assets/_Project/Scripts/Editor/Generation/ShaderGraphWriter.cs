using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace RootAccess.EditorTools
{
    /// <summary>
    /// Minimal writer for Shader Graph 17 assets (multi-JSON: one object per line, blank line between objects).
    /// Nodes are written without their slots unless a slot needs a non-default value: Shader Graph recreates the slots of
    /// built-in nodes in <c>UpdateNodeAfterDeserialization</c>, so only node types, fields and edges must be correct.
    /// The block/target layout mirrors URP's own "2D Sprite Lit/Unlit" graph templates.
    /// </summary>
    internal sealed class ShaderGraphWriter
    {
        public enum SpriteTarget { Lit, Unlit }

        public enum BlendMode { Alpha = 0, Premultiply = 1, Additive = 2, Multiply = 3 }

        public readonly struct Node
        {
            public Node(string id) => Id = id;
            public string Id { get; }
        }

        public readonly struct Property
        {
            public Property(string id, string kind)
            {
                Id = id;
                Kind = kind;
            }

            public string Id { get; }
            public string Kind { get; }
        }

        private const string NodeNs = "UnityEditor.ShaderGraph.";
        private const string PropertyNs = "UnityEditor.ShaderGraph.Internal.";

        private readonly List<string> _objects = new();
        private readonly List<string> _nodes = new();
        private readonly List<string> _properties = new();
        private readonly List<string> _edges = new();
        private readonly List<string> _vertexBlocks = new();
        private readonly List<string> _fragmentBlocks = new();
        private readonly string _graphPath;
        private readonly string _seed;
        private readonly string _targetId;
        private int _idCounter;
        private readonly Stack<Vector2> _pendingPositions = new();
        private float _autoY;

        /// <param name="seed">Makes every object id deterministic, so regenerating an unchanged graph produces identical bytes.</param>
        public ShaderGraphWriter(string graphPath, string seed, SpriteTarget target, BlendMode blend)
        {
            _graphPath = graphPath;
            _seed = seed;
            _targetId = NewId();
            string subTargetId = NewId();
            string subTargetType = target == SpriteTarget.Lit ? "UniversalSpriteLitSubTarget" : "UniversalSpriteUnlitSubTarget";
            _objects.Add(Obj(
                ("m_SGVersion", "1"), ("m_Type", Str("UnityEditor.Rendering.Universal.ShaderGraph.UniversalTarget")), ("m_ObjectId", Str(_targetId)),
                ("m_Datas", "[]"), ("m_ActiveSubTarget", Ref(subTargetId)), ("m_AllowMaterialOverride", "false"), ("m_SurfaceType", "1"),
                ("m_ZTestMode", "4"), ("m_ZWriteControl", "0"), ("m_AlphaMode", ((int)blend).ToString(CultureInfo.InvariantCulture)),
                ("m_RenderFace", "2"), ("m_AlphaClip", "false"), ("m_CastShadows", "false"), ("m_ReceiveShadows", "false"),
                ("m_DisableTint", "false"), ("m_AdditionalMotionVectorMode", "0"), ("m_AlembicMotionVectors", "false"),
                ("m_SupportsLODCrossFade", "false"), ("m_CustomEditorGUI", Str(string.Empty)), ("m_SupportVFX", "false")));
            _objects.Add(Obj(("m_SGVersion", "0"), ("m_Type", Str("UnityEditor.Rendering.Universal.ShaderGraph." + subTargetType)), ("m_ObjectId", Str(subTargetId))));

            VertexPosition = AddBlock(_vertexBlocks, "VertexDescription.Position", "PositionMaterialSlot", "Position", "Position", 1, Vec3(0f, 0f, 0f), ("m_Space", "0"));
            AddBlock(_vertexBlocks, "VertexDescription.Normal", "NormalMaterialSlot", "Normal", "Normal", 1, Vec3(0f, 0f, 0f), ("m_Space", "0"));
            AddBlock(_vertexBlocks, "VertexDescription.Tangent", "TangentMaterialSlot", "Tangent", "Tangent", 1, Vec3(0f, 0f, 0f), ("m_Space", "0"));
            BaseColor = AddBlock(_fragmentBlocks, "SurfaceDescription.BaseColor", "ColorRGBMaterialSlot", "Base Color", "BaseColor", 2, Vec3(0.5f, 0.5f, 0.5f),
                ("m_ColorMode", "0"), ("m_DefaultColor", "{\"r\":0.5,\"g\":0.5,\"b\":0.5,\"a\":1.0}"));
            if (target == SpriteTarget.Lit)
            {
                SpriteMask = AddBlock(_fragmentBlocks, "SurfaceDescription.SpriteMask", "ColorRGBAMaterialSlot", "Sprite Mask", "SpriteMask", 2, Vec4(1f, 1f, 1f, 1f));
                NormalTS = AddBlock(_fragmentBlocks, "SurfaceDescription.NormalTS", "NormalMaterialSlot", "Normal (Tangent Space)", "NormalTS", 2, Vec3(0f, 0f, 0f), ("m_Space", "3"));
            }

            Alpha = AddBlock(_fragmentBlocks, "SurfaceDescription.Alpha", "Vector1MaterialSlot", "Alpha", "Alpha", 2, "1.0");
        }

        public Node VertexPosition { get; }
        public Node BaseColor { get; }
        public Node Alpha { get; }
        public Node SpriteMask { get; }
        public Node NormalTS { get; }

        // ---------------------------------------------------------------- properties

        public Property Texture(string reference, string display, bool isMain, int defaultType)
        {
            string id = NewId();
            _objects.Add(Obj(PropertyHeader(0, "Texture2DShaderProperty", id, reference, display, true).Concat(new[]
            {
                ("m_Precision", "0"), ("overrideHLSLDeclaration", "false"), ("hlslDeclarationOverride", "0"), ("m_Hidden", "false"),
                ("m_PerRendererData", "false"), ("m_customAttributes", "[]"), ("m_Value", "{\"m_SerializedTexture\":\"\",\"m_Guid\":\"\"}"),
                ("isMainTexture", isMain ? "true" : "false"), ("useTilingAndOffset", "true"), ("useTexelSize", "true"),
                ("m_Modifiable", "true"), ("m_DefaultType", defaultType.ToString(CultureInfo.InvariantCulture))
            }).ToArray()));
            _properties.Add(id);
            return new Property(id, "Texture");
        }

        public Property Float(string reference, string display, float value, Vector2? range = null)
        {
            string id = NewId();
            Vector2 r = range ?? new Vector2(0f, 1f);
            _objects.Add(Obj(PropertyHeader(1, "Vector1ShaderProperty", id, reference, display, true).Concat(new[]
            {
                ("m_Precision", "0"), ("overrideHLSLDeclaration", "false"), ("hlslDeclarationOverride", "0"), ("m_Hidden", "false"),
                ("m_Value", Num(value)), ("m_FloatType", range.HasValue ? "1" : "0"), ("m_RangeValues", $"{{\"x\":{Num(r.x)},\"y\":{Num(r.y)}}}")
            }).ToArray()));
            _properties.Add(id);
            return new Property(id, "Float");
        }

        public Property Color(string reference, string display, Color value, bool hdr)
        {
            string id = NewId();
            _objects.Add(Obj(PropertyHeader(3, "ColorShaderProperty", id, reference, display, true).Concat(new[]
            {
                ("m_Precision", "0"), ("overrideHLSLDeclaration", "false"), ("hlslDeclarationOverride", "0"), ("m_Hidden", "false"),
                ("m_Value", $"{{\"r\":{Num(value.r)},\"g\":{Num(value.g)},\"b\":{Num(value.b)},\"a\":{Num(value.a)}}}"),
                ("isMainColor", "false"), ("m_ColorMode", hdr ? "1" : "0")
            }).ToArray()));
            _properties.Add(id);
            return new Property(id, "Color");
        }

        // ---------------------------------------------------------------- nodes

        /// <summary>Adds a built-in node. <paramref name="fields"/> are extra serialized fields; <paramref name="slots"/> are
        /// explicit slot objects (only needed for custom function signatures or non-default constants).</summary>
        /// <summary>
        /// Places the next node at (x, y) in the graph editor (cosmetic). Positions form a stack, so nested calls such as
        /// <c>g.At(a).Binary(..., g.At(b).PropertyNode(p), 0)</c> place the inner node at b and the outer one at a.
        /// </summary>
        public ShaderGraphWriter At(float x, float y)
        {
            _pendingPositions.Push(new Vector2(x, y));
            return this;
        }

        public Node Add(string type, int version = 0, (string key, string value)[] fields = null, params string[] slots) =>
            AddNamed(type, type.Replace("Node", string.Empty), version, fields, slots);

        public Node AddNamed(string type, string name, int version, (string key, string value)[] fields, params string[] slots)
        {
            string id = NewId();
            var slotRefs = new List<string>();
            foreach (string slot in slots)
            {
                string slotId = NewId();
                _objects.Add(slot.Replace("\"m_ObjectId\":\"$\"", $"\"m_ObjectId\":\"{slotId}\""));
                slotRefs.Add(Ref(slotId));
            }

            var entries = new List<(string, string)>
            {
                ("m_SGVersion", version.ToString(CultureInfo.InvariantCulture)), ("m_Type", Str(NodeNs + type)), ("m_ObjectId", Str(id)),
                ("m_Group", "{\"m_Id\":\"\"}"), ("m_Name", Str(name)), ("m_DrawState", DrawState()),
                ("m_Slots", "[" + string.Join(",", slotRefs) + "]"), ("synonyms", "[]"), ("m_Precision", "0"), ("m_PreviewExpanded", "false"),
                ("m_DismissedVersion", "0"), ("m_PreviewMode", "0"), ("m_CustomColors", "{\"m_SerializableColors\":[]}")
            };
            if (fields != null) entries.AddRange(fields);
            _objects.Add(Obj(entries.ToArray()));
            _nodes.Add(id);
            return new Node(id);
        }

        public Node PropertyNode(Property property) =>
            Add("PropertyNode", 0, new[] { ("m_Property", Ref(property.Id)) });

        public Node SampleTexture(Property texture, bool normalMap = false)
        {
            Vector2 at = _pendingPositions.Count > 0 ? _pendingPositions.Peek() : new Vector2(-1600f, _autoY += 140f);
            Node sample = Add("SampleTexture2DNode", 0, new[]
            {
                ("m_TextureType", normalMap ? "1" : "0"), ("m_NormalMapSpace", "0"), ("m_EnableGlobalMipBias", "true"), ("m_MipSamplingMode", "0")
            });
            At(at.x - 230f, at.y);
            Connect(PropertyNode(texture), 0, sample, SampleSlots.Texture);
            return sample;
        }

        public Node Position(int space) => Add("PositionNode", 1, new[] { ("m_Space", space.ToString(CultureInfo.InvariantCulture)), ("m_PositionSource", "0") });

        /// <summary>Custom Function node in File mode: <paramref name="hlslGuid"/> is the include, the function name gets _float/_half appended.</summary>
        public Node CustomFunction(string displayName, string function, string hlslGuid, (string name, int dims)[] inputs, (string name, int dims)[] outputs)
        {
            var slots = new List<string>();
            int slotId = 0;
            foreach ((string name, int dims) in inputs) slots.Add(VectorSlot(slotId++, name, dims, input: true));
            foreach ((string name, int dims) in outputs) slots.Add(VectorSlot(slotId++, name, dims, input: false));
            return AddNamed("CustomFunctionNode", displayName, 1, new[]
            {
                ("m_SourceType", "0"), ("m_FunctionName", Str(function)), ("m_FunctionSource", Str(hlslGuid)),
                ("m_FunctionSourceUsePragmas", "true"), ("m_FunctionBody", Str(string.Empty))
            }, slots.ToArray());
        }

        public void Connect(Node from, int fromSlot, Node to, int toSlot) =>
            _edges.Add($"{{\"m_OutputSlot\":{{\"m_Node\":{Ref(from.Id)},\"m_SlotId\":{fromSlot}}},\"m_InputSlot\":{{\"m_Node\":{Ref(to.Id)},\"m_SlotId\":{toSlot}}}}}");

        /// <summary>Shortcut for a two-input math node (Add, Subtract, Multiply, Step...): returns the node, output slot 2.</summary>
        public Node Binary(string type, Node a, int aSlot, Node b, int bSlot)
        {
            Node node = Add(type);
            Connect(a, aSlot, node, 0);
            Connect(b, bSlot, node, 1);
            return node;
        }

        /// <summary>Shortcut for a one-input math node (Sine, Absolute, Fraction, Ceiling...): output slot 1.</summary>
        public Node Unary(string type, Node input, int slot)
        {
            Node node = Add(type);
            Connect(input, slot, node, 0);
            return node;
        }

        public string Serialize()
        {
            string categoryId = NewId();
            _objects.Add(Obj(("m_SGVersion", "0"), ("m_Type", Str(NodeNs + "CategoryData")), ("m_ObjectId", Str(categoryId)), ("m_Name", Str(string.Empty)),
                ("m_ChildObjectList", RefList(_properties))));

            var all = new List<string>(_vertexBlocks);
            all.AddRange(_fragmentBlocks);
            all.AddRange(_nodes);
            string graph = Obj(
                ("m_SGVersion", "3"), ("m_Type", Str(NodeNs + "GraphData")), ("m_ObjectId", Str(NewId())), ("m_Properties", RefList(_properties)),
                ("m_Keywords", "[]"), ("m_Dropdowns", "[]"), ("m_CategoryData", "[" + Ref(categoryId) + "]"), ("m_Nodes", RefList(all)),
                ("m_GroupDatas", "[]"), ("m_StickyNoteDatas", "[]"), ("m_Edges", "[" + string.Join(",", _edges) + "]"),
                ("m_VertexContext", $"{{\"m_Position\":{{\"x\":0.0,\"y\":0.0}},\"m_Blocks\":{RefList(_vertexBlocks)}}}"),
                ("m_FragmentContext", $"{{\"m_Position\":{{\"x\":0.0,\"y\":200.0}},\"m_Blocks\":{RefList(_fragmentBlocks)}}}"),
                ("m_PreviewData", "{\"serializedMesh\":{\"m_SerializedMesh\":\"{\\\"mesh\\\":{\\\"instanceID\\\":0}}\",\"m_Guid\":\"\"},\"preventRotation\":false}"),
                ("m_Path", Str(_graphPath)), ("m_GraphPrecision", "1"), ("m_PreviewMode", "2"), ("m_OutputNode", "{\"m_Id\":\"\"}"),
                ("m_SubDatas", "[]"), ("m_ActiveTargets", "[" + Ref(_targetId) + "]"));

            var sb = new StringBuilder();
            sb.Append(graph);
            foreach (string obj in _objects) sb.Append("\n\n").Append(obj);
            sb.Append('\n');
            return sb.ToString();
        }

        // ---------------------------------------------------------------- slot ids of the built-in nodes used here

        public static class SampleSlots
        {
            public const int Rgba = 0, R = 4, G = 5, B = 6, A = 7, Texture = 1, Uv = 2, Sampler = 3;
        }

        public static class SplitSlots
        {
            public const int In = 0, R = 1, G = 2, B = 3, A = 4;
        }

        public const int BinaryOut = 2;
        public const int UnaryOut = 1;
        public const int LerpA = 0, LerpB = 1, LerpT = 2, LerpOut = 3;
        public const int NoiseUv = 0, NoiseScale = 1, NoiseOut = 2;
        public const int TimeOut = 0;

        // ---------------------------------------------------------------- helpers

        private Node AddBlock(List<string> context, string descriptor, string slotType, string display, string shaderName, int stage, string value,
            params (string, string)[] extra)
        {
            string slotId = NewId();
            var slot = new List<(string, string)>
            {
                ("m_SGVersion", "0"), ("m_Type", Str(NodeNs + slotType)), ("m_ObjectId", Str(slotId)), ("m_Id", "0"), ("m_DisplayName", Str(display)),
                ("m_SlotType", "0"), ("m_Hidden", "false"), ("m_ShaderOutputName", Str(shaderName)), ("m_StageCapability", stage.ToString(CultureInfo.InvariantCulture)),
                ("m_Value", value), ("m_DefaultValue", value), ("m_Labels", "[]")
            };
            slot.AddRange(extra);
            _objects.Add(Obj(slot.ToArray()));

            string id = NewId();
            _objects.Add(Obj(
                ("m_SGVersion", "0"), ("m_Type", Str(NodeNs + "BlockNode")), ("m_ObjectId", Str(id)), ("m_Group", "{\"m_Id\":\"\"}"),
                ("m_Name", Str(descriptor)), ("m_DrawState", "{\"m_Expanded\":true,\"m_Position\":{\"serializedVersion\":\"2\",\"x\":0.0,\"y\":0.0,\"width\":0.0,\"height\":0.0}}"),
                ("m_Slots", "[" + Ref(slotId) + "]"), ("synonyms", "[]"), ("m_Precision", "0"), ("m_PreviewExpanded", "true"), ("m_DismissedVersion", "0"),
                ("m_PreviewMode", "0"), ("m_CustomColors", "{\"m_SerializableColors\":[]}"), ("m_SerializedDescriptor", Str(descriptor))));
            context.Add(id);
            return new Node(id);
        }

        private IEnumerable<(string, string)> PropertyHeader(int version, string type, string id, string reference, string display, bool exposed) => new[]
        {
            ("m_SGVersion", version.ToString(CultureInfo.InvariantCulture)), ("m_Type", Str(PropertyNs + type)), ("m_ObjectId", Str(id)),
            ("m_Guid", $"{{\"m_GuidSerialized\":\"{new Guid(Hash("guid:" + id).Substring(0, 32))}\"}}"), ("promotedFromAssetID", Str(string.Empty)), ("promotedFromCategoryName", Str(string.Empty)),
            ("promotedOrdering", "-1"), ("m_Name", Str(display)), ("m_DefaultRefNameVersion", "1"), ("m_RefNameGeneratedByDisplayName", Str(display)),
            ("m_DefaultReferenceName", Str(reference)), ("m_OverrideReferenceName", Str(reference)), ("m_GeneratePropertyBlock", exposed ? "true" : "false"),
            ("m_UseCustomSlotLabel", "false"), ("m_CustomSlotLabel", Str(string.Empty)), ("m_DismissedVersion", "0")
        };

        private static string VectorSlot(int id, string name, int dims, bool input)
        {
            string type = dims switch { 1 => "Vector1MaterialSlot", 2 => "Vector2MaterialSlot", 3 => "Vector3MaterialSlot", _ => "Vector4MaterialSlot" };
            string value = dims switch { 1 => "0.0", 2 => "{\"x\":0.0,\"y\":0.0}", 3 => Vec3(0f, 0f, 0f), _ => Vec4(0f, 0f, 0f, 0f) };
            return Obj(("m_SGVersion", "0"), ("m_Type", Str(NodeNs + type)), ("m_ObjectId", "\"$\""), ("m_Id", id.ToString(CultureInfo.InvariantCulture)),
                ("m_DisplayName", Str(name)), ("m_SlotType", input ? "0" : "1"), ("m_Hidden", "false"), ("m_ShaderOutputName", Str(name)),
                ("m_StageCapability", "3"), ("m_Value", value), ("m_DefaultValue", value), ("m_Labels", "[]"));
        }

        private string DrawState()
        {
            Vector2 p = _pendingPositions.Count > 0 ? _pendingPositions.Pop() : new Vector2(-1600f, _autoY += 140f);
            return $"{{\"m_Expanded\":true,\"m_Position\":{{\"serializedVersion\":\"2\",\"x\":{Num(p.x)},\"y\":{Num(p.y)},\"width\":160.0,\"height\":120.0}}}}";
        }

        private string NewId() => Hash($"{_seed}:{_idCounter++}");

        private static string Hash(string text)
        {
            using var md5 = MD5.Create();
            byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(text));
            var sb = new StringBuilder(32);
            foreach (byte b in bytes) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        private static string Obj(params (string key, string value)[] entries)
        {
            var sb = new StringBuilder("{");
            for (int i = 0; i < entries.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(Str(entries[i].key)).Append(':').Append(entries[i].value);
            }

            return sb.Append('}').ToString();
        }

        private static string Ref(string id) => $"{{\"m_Id\":\"{id}\"}}";

        private static string RefList(IEnumerable<string> ids)
        {
            var refs = new List<string>();
            foreach (string id in ids) refs.Add(Ref(id));
            return "[" + string.Join(",", refs) + "]";
        }

        private static string Str(string value)
        {
            var sb = new StringBuilder("\"");
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }

            return sb.Append('"').ToString();
        }

        private static string Num(float value)
        {
            string s = value.ToString("R", CultureInfo.InvariantCulture);
            return s.Contains('.') || s.Contains('E') ? s : s + ".0";
        }

        private static string Vec3(float x, float y, float z) => $"{{\"x\":{Num(x)},\"y\":{Num(y)},\"z\":{Num(z)}}}";

        private static string Vec4(float x, float y, float z, float w) => $"{{\"x\":{Num(x)},\"y\":{Num(y)},\"z\":{Num(z)},\"w\":{Num(w)}}}";
    }
}
