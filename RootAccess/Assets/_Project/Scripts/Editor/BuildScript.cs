using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RootAccess.EditorTools
{
    /// <summary>
    /// Windows x64 player build. Menu: Root Access/Build/Build Windows x64. Batchmode:
    /// Unity.exe -batchmode -quit -projectPath RootAccess -executeMethod RootAccess.EditorTools.BuildScript.BuildWindows
    /// Exits with code 1 when the build fails. Pass -ra-development for a development build.
    /// </summary>
    public static class BuildScript
    {
        public const string OutputDirectory = "Builds/Windows";
        public const string ExecutableName = "RootAccess.exe";
        private const string DevelopmentArg = "-ra-development";

        /// <summary>Player version shown on the main menu (Application.version). 1.1 = painted art pass, 1.2 = Level 6 (the Halden antenna).</summary>
        public const string GameVersion = "1.3";

        public static string OutputPath => $"{OutputDirectory}/{ExecutableName}";

        [MenuItem("Root Access/Build/Build Windows x64")]
        public static void BuildWindowsMenu() => BuildWindows();

        public static void BuildWindows()
        {
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                EditorAssetUtil.Fail("No enabled scenes in Build Settings. Run Root Access/Build/Build All Content first.");
                return;
            }

            PlayerSettings.bundleVersion = GameVersion;
            bool development = Environment.GetCommandLineArgs().Contains(DevelopmentArg);
            Directory.CreateDirectory(OutputDirectory);
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = development ? BuildOptions.Development : BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            EditorAssetUtil.Log($"Build {summary.result}: {summary.outputPath} | {summary.totalSize / (1024f * 1024f):0.0} MB | " +
                                $"{summary.totalTime.TotalSeconds:0}s | errors {summary.totalErrors}, warnings {summary.totalWarnings} | scenes: {string.Join(", ", scenes.Select(Path.GetFileNameWithoutExtension))}");

            if (summary.result == BuildResult.Succeeded) return;
            EditorAssetUtil.Fail($"Windows build failed with result {summary.result}.");
        }
    }
}
