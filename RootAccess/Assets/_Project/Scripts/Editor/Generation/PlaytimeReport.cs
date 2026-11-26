using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RootAccess.EditorTools
{
    /// <summary>
    /// Writes <c>Logs/PlaytimeEstimate.md</c> after the content build: the first-run duration of every level, summed from
    /// the <see cref="LevelLayout"/> segments, plus the hideout and the cutscenes. It is an estimate for planning; the real
    /// figure comes from a timed playthrough (the Level Complete screen shows the time spent in each level).
    /// </summary>
    internal static class PlaytimeReport
    {
        public const string ReportPath = "Logs/PlaytimeEstimate.md";

        /// <summary>Deaths, retried hacks and backtracking on a first run.</summary>
        public const float RetryFactor = 1.25f;

        /// <summary>Hideout between missions: briefing, shop, skills, contracts, travel (6 visits).</summary>
        public const float HubSeconds = 6 * 40f;

        /// <summary>Intro + finale cutscenes and each level's opening dialogue.</summary>
        public const float CutsceneSeconds = 17f + 16f + 6 * 15f;

        private static readonly List<(string level, IReadOnlyList<(string segment, float seconds)> breakdown)> Levels = new();

        public static void Clear() => Levels.Clear();

        public static void Add(string level, LevelLayout layout) => Levels.Add((level, layout.PlaytimeBreakdown.ToList()));

        public static float TotalSeconds =>
            Levels.Sum(l => l.breakdown.Sum(b => b.seconds)) * RetryFactor + HubSeconds + CutsceneSeconds;

        public static void Write()
        {
            var md = new StringBuilder();
            md.AppendLine("# Durata estimată a unei prime treceri").AppendLine();
            md.AppendLine($"Generat de `SceneBuilder.BuildAll` din segmentele `LevelLayout`. Factor pentru morți și reîncercări: ×{RetryFactor:0.00}.");
            md.AppendLine("Cifra reală vine dintr-o trecere cronometrată (ecranul „Misiune îndeplinită” arată timpul pe nivel).").AppendLine();
            md.AppendLine("| Nivel | Segmente | Estimare (min) |").AppendLine("|---|---|---|");
            foreach ((string level, IReadOnlyList<(string segment, float seconds)> breakdown) in Levels)
                md.AppendLine($"| {level} | {breakdown.Count} | {breakdown.Sum(b => b.seconds) * RetryFactor / 60f:0.0} |");
            md.AppendLine($"| Ascunzătoarea (6 vizite) | – | {HubSeconds / 60f:0.0} |");
            md.AppendLine($"| Cutscene-uri și dialoguri | – | {CutsceneSeconds / 60f:0.0} |");
            md.AppendLine($"| **Total** | | **{TotalSeconds / 60f:0.0}** |").AppendLine();
            foreach ((string level, IReadOnlyList<(string segment, float seconds)> breakdown) in Levels)
            {
                md.AppendLine($"## {level}").AppendLine();
                foreach ((string segment, float seconds) in breakdown) md.AppendLine($"- {segment}: {seconds:0} s");
                md.AppendLine();
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "Logs");
            File.WriteAllText(ReportPath, md.ToString(), new UTF8Encoding(false));
            EditorAssetUtil.Log($"Playtime estimate: {TotalSeconds / 60f:0.0} min (Hub + 5 levels + cutscenes) -> {ReportPath}");
        }
    }
}
