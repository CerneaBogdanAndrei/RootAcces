using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RootAccess.EditorTools
{
    /// <summary>Small helpers shared by every batchmode editor tool.</summary>
    public static class EditorAssetUtil
    {
        public const string LogPrefix = "[RootAccess]";
        public const string FailPrefix = "[RootAccess][FAIL]";

        public static void EnsureFolder(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || AssetDatabase.IsValidFolder(assetPath)) return;
            string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            string leaf = Path.GetFileName(assetPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf))
                throw new ArgumentException($"Invalid folder path '{assetPath}'.");
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        public static T LoadOrCreateAsset<T>(string assetPath, Action<T> initialize = null) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null) return existing;
            EnsureFolder(Path.GetDirectoryName(assetPath)?.Replace('\\', '/'));
            var created = ScriptableObject.CreateInstance<T>();
            initialize?.Invoke(created);
            AssetDatabase.CreateAsset(created, assetPath);
            return created;
        }

        /// <summary>Assigns a serialized (usually private) field through SerializedObject so editor tools can wire references.</summary>
        public static void SetField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(fieldName);
            if (property == null)
                throw new InvalidOperationException($"{target.GetType().Name} has no serialized field '{fieldName}'.");
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetFields(Object target, Action<SerializedObject> edit)
        {
            var so = new SerializedObject(target);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static SerializedProperty Require(this SerializedObject so, string fieldName)
        {
            SerializedProperty property = so.FindProperty(fieldName);
            if (property == null)
                throw new InvalidOperationException($"{so.targetObject.GetType().Name} has no serialized field '{fieldName}'.");
            return property;
        }

        public static void SetArray<T>(this SerializedObject so, string fieldName, T[] values) where T : Object
        {
            SerializedProperty property = so.Require(fieldName);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        public static void Log(string message) => Debug.Log($"{LogPrefix} {message}");

        public static void Fail(string message, Exception exception = null)
        {
            Debug.LogError($"{FailPrefix} {message}{(exception != null ? "\n" + exception : string.Empty)}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }

        public static void RunBatchStep(string stepName, Action action)
        {
            try
            {
                action();
                AssetDatabase.SaveAssets();
                Log($"{stepName} completed.");
            }
            catch (Exception e)
            {
                Fail($"{stepName} failed.", e);
                if (!Application.isBatchMode) throw;
            }
        }
    }
}
