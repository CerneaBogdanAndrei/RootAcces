using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RootAccess.EditorTools
{
    /// <summary>
    /// Writes contact sheets of the procedural art to <c>RootAccess/Logs/ArtPreview</c> without touching any asset, so the
    /// look can be iterated on in a quick batchmode run (<c>-executeMethod RootAccess.EditorTools.ArtPreview.Run</c>).
    /// </summary>
    public static class ArtPreview
    {
        private static readonly Color32 Backdrop = new(24, 20, 40, 255);

        [MenuItem("Root Access/Debug/Write Art Preview")]
        public static void Run()
        {
            EditorAssetUtil.RunBatchStep("ArtPreview", () =>
            {
                string folder = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Logs", "ArtPreview");
                Directory.CreateDirectory(folder);
                foreach ((string name, Dictionary<string, PixelCanvas> frames, int scale) in ArtCatalog.PreviewSheets())
                    File.WriteAllBytes(Path.Combine(folder, name + ".png"), Sheet(frames, scale).EncodePng());
                EditorAssetUtil.Log($"Art preview written to {folder}");
            });
        }

        /// <summary>Frames side by side on a dark backdrop, upscaled with nearest neighbour.</summary>
        public static PixelCanvas Sheet(Dictionary<string, PixelCanvas> frames, int scale)
        {
            int columns = Mathf.Min(8, frames.Count);
            int cellW = 0, cellH = 0;
            foreach (PixelCanvas f in frames.Values)
            {
                cellW = Mathf.Max(cellW, f.Width);
                cellH = Mathf.Max(cellH, f.Height);
            }

            int rows = Mathf.CeilToInt(frames.Count / (float)columns);
            const int pad = 2;
            var sheet = new PixelCanvas((cellW + pad) * columns * scale, (cellH + pad) * rows * scale).Fill(Backdrop);
            int index = 0;
            foreach (PixelCanvas frame in frames.Values)
            {
                int col = index % columns, row = rows - 1 - index / columns;
                int ox = col * (cellW + pad) * scale, oy = row * (cellH + pad) * scale;
                for (int y = 0; y < frame.Height; y++)
                for (int x = 0; x < frame.Width; x++)
                {
                    Color c = frame.Get(x, y);
                    if (c.a <= 0f) continue;
                    for (int sy = 0; sy < scale; sy++)
                    for (int sx = 0; sx < scale; sx++)
                        sheet.Blend(ox + x * scale + sx, oy + y * scale + sy, c);
                }

                index++;
            }

            return sheet;
        }
    }
}
