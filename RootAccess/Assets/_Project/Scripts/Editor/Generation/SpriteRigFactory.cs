using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.U2D;

namespace RootAccess.EditorTools
{
    /// <summary>
    /// Rigs generated sprites for 2D Animation (rubric item 3: trees with bones). Writes a bone chain and a weighted grid
    /// mesh through the Sprite Editor data providers, exactly what the Skinning Editor would store; the 2D Animation
    /// post-processor then bakes bind poses on import, so a <c>SpriteSkin</c> can deform the sprite.
    /// <para>Spaces (as the package expects): the root bone position is in pixels from the sprite rect's bottom-left,
    /// child bones are in pixels along their parent's local axes, vertices are in pixels inside the rect.</para>
    /// </summary>
    internal static class SpriteRigFactory
    {
        /// <summary>Bone chain of the rigged tree, bottom to top: (name, length in pixels).</summary>
        public static readonly (string name, float length)[] TreeBones =
        {
            ("Trunk", 56f), ("Bough", 40f), ("Canopy", 36f), ("Crown", 28f)
        };

        private const int GridColumns = 5;
        private const int GridRows = 9;
        private const float BlendPixels = 12f;

        /// <summary>Vertical bone chain from the bottom centre of the sprite, weighted by height with soft joints.</summary>
        public static void RigVerticalChain(string spritePath, (string name, float length)[] bones)
        {
            if (AssetImporter.GetAtPath(spritePath) is not TextureImporter importer)
                throw new System.InvalidOperationException($"[SpriteRigFactory] No texture importer at {spritePath}.");

            var factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            SpriteRect rect = provider.GetSpriteRects()[0];
            Vector2 size = rect.rect.size;

            var spriteBones = new List<SpriteBone>(bones.Length);
            var joints = new float[bones.Length + 1];
            for (int i = 0; i < bones.Length; i++)
            {
                bool root = i == 0;
                spriteBones.Add(new SpriteBone
                {
                    name = bones[i].name,
                    guid = GUID.Generate().ToString(),
                    // The root stands at the bottom centre pointing up; each child continues along its parent's +X axis.
                    position = root ? new Vector3(size.x * 0.5f, 0f, 0f) : new Vector3(bones[i - 1].length, 0f, 0f),
                    rotation = root ? Quaternion.Euler(0f, 0f, 90f) : Quaternion.identity,
                    length = bones[i].length,
                    parentId = i - 1,
                    color = new Color32(80, 255, 160, 255)
                });
                joints[i + 1] = joints[i] + bones[i].length;
            }

            var vertices = new Vertex2DMetaData[GridColumns * GridRows];
            for (int row = 0; row < GridRows; row++)
            for (int column = 0; column < GridColumns; column++)
            {
                var position = new Vector2(size.x * column / (GridColumns - 1), size.y * row / (GridRows - 1));
                vertices[row * GridColumns + column] = new Vertex2DMetaData { position = position, boneWeight = WeightAt(position.y, joints) };
            }

            var indices = new List<int>((GridColumns - 1) * (GridRows - 1) * 6);
            for (int row = 0; row < GridRows - 1; row++)
            for (int column = 0; column < GridColumns - 1; column++)
            {
                int a = row * GridColumns + column, b = a + 1, c = a + GridColumns, d = c + 1;
                indices.AddRange(new[] { a, c, b, b, c, d });
            }

            var mesh = provider.GetDataProvider<ISpriteMeshDataProvider>();
            mesh.SetVertices(rect.spriteID, vertices);
            mesh.SetIndices(rect.spriteID, indices.ToArray());
            mesh.SetEdges(rect.spriteID, OutlineEdges());
            provider.GetDataProvider<ISpriteBoneDataProvider>().SetBones(rect.spriteID, spriteBones);
            provider.Apply();
            importer.SaveAndReimport();
            EditorAssetUtil.Log($"Rigged {spritePath}: {bones.Length} bones, {vertices.Length} weighted vertices.");
        }

        /// <summary>The bone a height belongs to, blended linearly with the neighbouring bone near each joint.</summary>
        internal static BoneWeight WeightAt(float y, float[] joints)
        {
            int bones = joints.Length - 1;
            int bone = bones - 1;
            for (int i = 0; i < bones; i++)
                if (y < joints[i + 1]) { bone = i; break; }

            var weight = new BoneWeight { boneIndex0 = bone, weight0 = 1f };
            float toUpper = joints[bone + 1] - y;
            float fromLower = y - joints[bone];
            if (bone < bones - 1 && toUpper < BlendPixels)
            {
                float t = 0.5f * (1f - toUpper / BlendPixels);
                weight = new BoneWeight { boneIndex0 = bone, weight0 = 1f - t, boneIndex1 = bone + 1, weight1 = t };
            }
            else if (bone > 0 && fromLower < BlendPixels)
            {
                float t = 0.5f * (1f - fromLower / BlendPixels);
                weight = new BoneWeight { boneIndex0 = bone, weight0 = 1f - t, boneIndex1 = bone - 1, weight1 = t };
            }

            return weight;
        }

        private static Vector2Int[] OutlineEdges()
        {
            var edges = new List<Vector2Int>();
            for (int c = 0; c < GridColumns - 1; c++) edges.Add(new Vector2Int(c, c + 1));
            for (int r = 0; r < GridRows - 1; r++)
                edges.Add(new Vector2Int(r * GridColumns + GridColumns - 1, (r + 1) * GridColumns + GridColumns - 1));
            for (int c = GridColumns - 1; c > 0; c--)
                edges.Add(new Vector2Int((GridRows - 1) * GridColumns + c, (GridRows - 1) * GridColumns + c - 1));
            for (int r = GridRows - 1; r > 0; r--) edges.Add(new Vector2Int(r * GridColumns, (r - 1) * GridColumns));
            return edges.ToArray();
        }
    }
}
