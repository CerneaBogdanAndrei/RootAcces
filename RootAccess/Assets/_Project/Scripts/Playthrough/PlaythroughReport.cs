using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace RootAccess.Playthrough
{
    /// <summary>What happened in one level of the automated playthrough.</summary>
    internal sealed class LevelRun
    {
        public string Scene;
        public string Name;
        public float StartedAt;
        public float Seconds;
        public int Steps;
        public int StepsDone;
        public int Deaths;
        public int Falls;
        public int Hits;
        public int Alarms;
        public int Hacks;
        public int HacksFailed;
        public int LogsFound;
        public bool Completed;
        public float FrameTimeSum;
        public int Frames;
        public float WorstFrame;
        public readonly List<string> Issues = new();
        public readonly List<string> Shots = new();
        public readonly Dictionary<string, int> HitsBySegment = new();

        public float AverageFps => FrameTimeSum > 0f ? Frames / FrameTimeSum : 0f;
    }

    /// <summary>Collects the run and writes <c>Playthrough.md</c> next to the screenshots.</summary>
    internal sealed class PlaythroughReport
    {
        private readonly List<LevelRun> _levels = new();
        private readonly List<string> _general = new();
        private readonly List<string> _errors = new();
        private readonly string _folder;
        private readonly float _startedAt;

        public PlaythroughReport(string folder)
        {
            _folder = folder;
            _startedAt = Time.realtimeSinceStartup;
        }

        public string Folder => _folder;
        public LevelRun Current { get; private set; }
        public IReadOnlyList<LevelRun> Levels => _levels;

        public LevelRun Begin(string scene, string name)
        {
            Current = new LevelRun { Scene = scene, Name = name, StartedAt = Time.realtimeSinceStartup };
            _levels.Add(Current);
            return Current;
        }

        public void End()
        {
            if (Current != null) Current.Seconds = Time.realtimeSinceStartup - Current.StartedAt;
            Current = null;
        }

        public void Note(string text)
        {
            if (Current != null) Current.Issues.Add(text);
            else _general.Add(text);
            Debug.Log($"[Playthrough] {text}");
        }

        public void Error(string text)
        {
            string where = Current != null ? Current.Name : "meniu/ascunzătoare";
            if (_errors.Count < 200) _errors.Add($"{where}: {text}");
        }

        public void Write(bool finished, bool campaign)
        {
            var md = new StringBuilder();
            float total = Time.realtimeSinceStartup - _startedAt;
            md.AppendLine("# Playthrough automat");
            md.AppendLine();
            md.AppendLine($"Generat de `-ra-playthrough` (build-ul real, input prin tastatură virtuală). Durata totală: {Clock(total)}. " +
                          (campaign
                              ? finished ? "Jocul a fost parcurs până la credite." : "**Parcurgerea nu a ajuns la final.**"
                              : finished ? "Nivelul a fost terminat." : "**Nivelul nu a fost terminat.**"));
            md.AppendLine();
            md.AppendLine("| Nivel | Timp | Pași | Morți | Căderi | Lovituri | Alarme | Spargeri (eșuate) | Jurnale | FPS mediu / cel mai lent cadru | Terminat |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (LevelRun level in _levels)
            {
                md.AppendLine($"| {level.Name} | {Clock(level.Seconds)} | {level.StepsDone}/{level.Steps} | {level.Deaths} | {level.Falls} | {level.Hits} | {level.Alarms} | " +
                              $"{level.Hacks} ({level.HacksFailed}) | {level.LogsFound} | {level.AverageFps.ToString("0", CultureInfo.InvariantCulture)} / " +
                              $"{(level.WorstFrame * 1000f).ToString("0", CultureInfo.InvariantCulture)} ms | {(level.Completed ? "da" : "**nu**")} |");
            }

            if (_general.Count > 0)
            {
                md.AppendLine();
                md.AppendLine("## Meniu, ascunzătoare, cutscene-uri");
                md.AppendLine();
                foreach (string line in _general) md.AppendLine($"- {line}");
            }

            foreach (LevelRun level in _levels)
            {
                md.AppendLine();
                md.AppendLine($"## {level.Name}");
                md.AppendLine();
                if (level.HitsBySegment.Count > 0)
                {
                    var hits = new List<string>();
                    foreach (KeyValuePair<string, int> pair in level.HitsBySegment) hits.Add($"{pair.Key} ×{pair.Value}");
                    md.AppendLine($"- Lovituri pe segmente: {string.Join(", ", hits)}");
                }

                if (level.Issues.Count == 0) md.AppendLine("- Fără probleme pe traseu.");
                foreach (string issue in level.Issues) md.AppendLine($"- {issue}");
                if (level.Shots.Count > 0) md.AppendLine($"- Capturi: {string.Join(", ", level.Shots)}");
            }

            md.AppendLine();
            md.AppendLine("## Erori în log");
            md.AppendLine();
            if (_errors.Count == 0) md.AppendLine("- Niciuna.");
            foreach (string error in _errors) md.AppendLine($"- {error}");

            Directory.CreateDirectory(_folder);
            File.WriteAllText(Path.Combine(_folder, "Playthrough.md"), md.ToString(), new UTF8Encoding(false));
        }

        private static string Clock(float seconds)
        {
            int s = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return $"{s / 60}:{s % 60:00}";
        }
    }
}
