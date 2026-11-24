using System.Collections.Generic;
using System.Linq;
using RootAccess.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RootAccess.EditorTools
{
    /// <summary>
    /// One-shot, idempotent project configuration. Run in batchmode:
    /// Unity.exe -batchmode -quit -projectPath RootAccess -executeMethod RootAccess.EditorTools.ProjectSetup.Run
    /// </summary>
    public static class ProjectSetup
    {
        public const string CompanyName = "CerneaBogdanAndrei";
        public const string ProductName = "Root Access";
        public const string ApplicationId = "com.CerneaBogdanAndrei.RootAccess";
        private const int InputSystemPackageOnly = 1;

        [MenuItem("Root Access/Setup/1. Run Project Setup")]
        public static void Run()
        {
            EditorAssetUtil.RunBatchStep("ProjectSetup", () =>
            {
                CreateFolders();
                MoveDefaultAssets();
                ConfigureRenderPipeline();
                ConfigurePlayerSettings();
                ConfigureEditorSettings();
                ConfigureTagsAndLayers();
                ConfigurePhysics2D();
                ConfigureQuality();
                ConfigureInputActions();
                ImportTmpEssentials();
            });
        }

        public static void CreateFolders()
        {
            foreach (string folder in EditorPaths.AllFolders) EditorAssetUtil.EnsureFolder(folder);
            foreach (string scene in EditorPaths.SceneFolders) EditorAssetUtil.EnsureFolder(EditorPaths.SceneFolder(scene));
            foreach (string scripts in EditorPaths.ScriptFolders) EditorAssetUtil.EnsureFolder(EditorPaths.Scripts + "/" + scripts);
            EditorAssetUtil.Log("Folder structure ensured.");
        }

        private static void MoveDefaultAssets()
        {
            MoveIfPresent("Assets/UniversalRenderPipelineGlobalSettings.asset", EditorPaths.SettingsUrp + "/UniversalRenderPipelineGlobalSettings.asset");
            MoveIfPresent("Assets/DefaultVolumeProfile.asset", EditorPaths.SettingsUrp + "/DefaultVolumeProfile.asset");
        }

        private static void MoveIfPresent(string from, string to)
        {
            if (AssetDatabase.LoadMainAssetAtPath(from) == null || AssetDatabase.LoadMainAssetAtPath(to) != null) return;
            string error = AssetDatabase.MoveAsset(from, to);
            if (!string.IsNullOrEmpty(error)) Debug.LogWarning($"{EditorAssetUtil.LogPrefix} Could not move {from}: {error}");
        }

        private static void ConfigureRenderPipeline()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>(EditorPaths.UrpRenderer2D);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(renderer, EditorPaths.UrpRenderer2D);
                ResourceReloader.ReloadAllNullIn(renderer, EditorPaths.UrpPackage);
                EditorAssetUtil.SetFields(renderer, so =>
                {
                    var postProcess = AssetDatabase.LoadAssetAtPath<PostProcessData>(EditorPaths.UrpDefaultPostProcessData);
                    if (postProcess == null) throw new System.InvalidOperationException("URP default PostProcessData not found.");
                    so.Require("m_PostProcessData").objectReferenceValue = postProcess;
                });
                EditorUtility.SetDirty(renderer);
            }

            var universal = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(EditorPaths.UrpUniversalRenderer);
            if (universal == null)
            {
                universal = ScriptableObject.CreateInstance<UniversalRendererData>();
                universal.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(EditorPaths.UrpDefaultPostProcessData);
                AssetDatabase.CreateAsset(universal, EditorPaths.UrpUniversalRenderer);
                ResourceReloader.ReloadAllNullIn(universal, EditorPaths.UrpPackage);
                EditorUtility.SetDirty(universal);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(EditorPaths.UrpAsset);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, EditorPaths.UrpAsset);
            }

            EditorAssetUtil.SetFields(pipeline, so =>
            {
                SerializedProperty list = so.Require("m_RendererDataList");
                list.arraySize = 2;
                list.GetArrayElementAtIndex(RendererIndices.Renderer2D).objectReferenceValue = renderer;
                list.GetArrayElementAtIndex(RendererIndices.Universal).objectReferenceValue = universal;
                so.Require("m_DefaultRendererIndex").intValue = RendererIndices.Renderer2D;
            });

            pipeline.supportsHDR = true;
            pipeline.msaaSampleCount = 1;
            pipeline.renderScale = 1f;
            pipeline.useSRPBatcher = true;
            pipeline.supportsCameraDepthTexture = false;
            pipeline.supportsCameraOpaqueTexture = false;
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            int originalLevel = QualitySettings.GetQualityLevel();
            for (int level = 0; level < QualitySettings.names.Length; level++)
            {
                QualitySettings.SetQualityLevel(level, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(originalLevel, false);
            EditorAssetUtil.Log($"URP 2D pipeline assigned to Graphics and {QualitySettings.names.Length} quality levels.");
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, ApplicationId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);

            Object playerSettingsAsset = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");
            EditorAssetUtil.SetFields(playerSettingsAsset, so => so.Require("activeInputHandler").intValue = InputSystemPackageOnly);

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            EditorAssetUtil.Log("Player settings configured (Linear, 1920x1080, Input System only, Windows x64 Mono).");
        }

        private static void ConfigureEditorSettings()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            VersionControlSettings.mode = "Visible Meta Files";
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            EditorAssetUtil.Log("Editor settings: Force Text serialization, Visible Meta Files, 2D mode.");
        }

        private static void ConfigureTagsAndLayers()
        {
            Object tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            var tagManager = new SerializedObject(tagManagerAsset);

            SerializedProperty tags = tagManager.Require("tags");
            var existingTags = new HashSet<string>();
            for (int i = 0; i < tags.arraySize; i++) existingTags.Add(tags.GetArrayElementAtIndex(i).stringValue);
            foreach (string tag in Tags.Custom.Where(t => !existingTags.Contains(t)))
            {
                tags.arraySize++;
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            }

            SerializedProperty layers = tagManager.Require("layers");
            SetLayer(layers, Layers.Player, Layers.PlayerName);
            SetLayer(layers, Layers.Enemy, Layers.EnemyName);
            SetLayer(layers, Layers.Ground, Layers.GroundName);
            SetLayer(layers, Layers.Wall, Layers.WallName);
            SetLayer(layers, Layers.Interactable, Layers.InteractableName);
            SetLayer(layers, Layers.Trigger, Layers.TriggerName);
            SetLayer(layers, Layers.Water, Layers.WaterName);
            SetLayer(layers, Layers.Projectile, Layers.ProjectileName);
            SetLayer(layers, Layers.Hidden, Layers.HiddenName);

            SerializedProperty sortingLayers = tagManager.Require("m_SortingLayers");
            var existingIds = new Dictionary<string, uint>();
            for (int i = 0; i < sortingLayers.arraySize; i++)
            {
                SerializedProperty element = sortingLayers.GetArrayElementAtIndex(i);
                existingIds[element.FindPropertyRelative("name").stringValue] = element.FindPropertyRelative("uniqueID").uintValue;
            }

            sortingLayers.arraySize = SortingLayers.Ordered.Length;
            for (int i = 0; i < SortingLayers.Ordered.Length; i++)
            {
                string name = SortingLayers.Ordered[i];
                uint id = existingIds.TryGetValue(name, out uint known) ? known : StableSortingLayerId(name);
                SerializedProperty element = sortingLayers.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("name").stringValue = name;
                element.FindPropertyRelative("uniqueID").uintValue = id;
                element.FindPropertyRelative("locked").boolValue = false;
            }

            tagManager.ApplyModifiedPropertiesWithoutUndo();
            EditorAssetUtil.Log("Tags, physics layers and sorting layers configured.");
        }

        private static uint StableSortingLayerId(string name)
        {
            if (name == SortingLayers.Default) return 0;
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in name) hash = (hash ^ c) * 16777619;
                return hash == 0 ? 1u : hash;
            }
        }

        private static void SetLayer(SerializedProperty layers, int index, string name)
        {
            SerializedProperty element = layers.GetArrayElementAtIndex(index);
            if (!string.IsNullOrEmpty(element.stringValue) && element.stringValue != name)
                Debug.LogWarning($"{EditorAssetUtil.LogPrefix} Layer {index} renamed from '{element.stringValue}' to '{name}'.");
            element.stringValue = name;
        }

        private static void ConfigurePhysics2D()
        {
            int[] managed =
            {
                Layers.Default, Layers.Player, Layers.Enemy, Layers.Ground, Layers.Wall, Layers.Interactable,
                Layers.Trigger, Layers.Water, Layers.Projectile, Layers.Hidden
            };
            foreach (int a in managed)
            foreach (int b in managed)
                Physics2D.IgnoreLayerCollision(a, b, true);

            Allow(Layers.Default, Layers.Default, Layers.Player, Layers.Enemy, Layers.Ground, Layers.Wall, Layers.Water, Layers.Projectile, Layers.Hidden);
            Allow(Layers.Player, Layers.Enemy, Layers.Ground, Layers.Wall, Layers.Interactable, Layers.Trigger, Layers.Water, Layers.Projectile);
            Allow(Layers.Hidden, Layers.Ground, Layers.Wall, Layers.Interactable, Layers.Trigger, Layers.Water, Layers.Projectile);
            Allow(Layers.Enemy, Layers.Ground, Layers.Wall, Layers.Water, Layers.Projectile);
            Allow(Layers.Ground, Layers.Ground, Layers.Wall, Layers.Water, Layers.Projectile, Layers.Interactable);
            Allow(Layers.Wall, Layers.Projectile);
            Allow(Layers.Projectile, Layers.Water);

            for (int layer = 0; layer < 32; layer++)
                Physics2D.IgnoreLayerCollision(Layers.IgnoreRaycast, layer, true);

            Physics2D.queriesStartInColliders = false;
            Physics2D.queriesHitTriggers = false;
            EditorAssetUtil.Log("2D layer collision matrix configured.");
        }

        private static void Allow(int layer, params int[] others)
        {
            foreach (int other in others) Physics2D.IgnoreLayerCollision(layer, other, false);
        }

        private static void ConfigureQuality()
        {
            int originalLevel = QualitySettings.GetQualityLevel();
            for (int level = 0; level < QualitySettings.names.Length; level++)
            {
                QualitySettings.SetQualityLevel(level, false);
                QualitySettings.vSyncCount = 1;
                QualitySettings.antiAliasing = 0;
            }
            QualitySettings.SetQualityLevel(originalLevel, false);
        }

        private static void ConfigureInputActions()
        {
            AssetDatabase.ImportAsset(EditorPaths.InputActionsAsset, ImportAssetOptions.ForceUpdate);
            AssetImporter importer = AssetImporter.GetAtPath(EditorPaths.InputActionsAsset);
            if (importer == null) throw new System.InvalidOperationException($"No importer for {EditorPaths.InputActionsAsset}.");

            EditorAssetUtil.SetFields(importer, so =>
            {
                so.Require("m_GenerateWrapperCode").boolValue = true;
                so.Require("m_WrapperCodePath").stringValue = EditorPaths.InputActionsWrapper;
                so.Require("m_WrapperClassName").stringValue = EditorPaths.InputActionsClass;
                so.Require("m_WrapperCodeNamespace").stringValue = EditorPaths.InputActionsNamespace;
            });
            importer.SaveAndReimport();
            EditorAssetUtil.Log($"Input actions wrapper generation enabled -> {EditorPaths.InputActionsWrapper}.");
        }

        /// <summary>
        /// ImportPackage is deferred to the next editor tick, so in batchmode (-quit) it never runs.
        /// scripts/setup.ps1 imports the package with the synchronous -importPackage switch instead.
        /// </summary>
        private static void ImportTmpEssentials()
        {
            if (AssetDatabase.IsValidFolder(EditorPaths.TmpEssentialsFolder))
            {
                EditorAssetUtil.Log("TMP Essential Resources present.");
                return;
            }

            if (Application.isBatchMode)
            {
                Debug.LogWarning($"{EditorAssetUtil.LogPrefix} TMP Essential Resources missing: run scripts/setup.ps1 (uses -importPackage).");
                return;
            }

            AssetDatabase.ImportPackage(EditorPaths.TmpEssentialsPackage, false);
        }
    }
}
