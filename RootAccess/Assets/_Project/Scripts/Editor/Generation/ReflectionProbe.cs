using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RootAccess.EditorTools
{
    /// <summary>Diagnostics: dumps internal editor API signatures the mixer builder relies on (used when Unity changes them).</summary>
    public static class ReflectionProbe
    {
        private static readonly string[] TypeNames =
        {
            "UnityEditor.Audio.AudioMixerController", "UnityEditor.Audio.AudioMixerGroupController",
            "UnityEditor.Audio.AudioMixerSnapshotController", "UnityEditor.Audio.AudioMixerEffectController",
            "UnityEditor.Audio.AudioGroupParameterPath", "UnityEditor.Audio.AudioParameterPath",
            "UnityEditor.Audio.ExposedAudioParameter"
        };

        public static void DumpMixerApi()
        {
            Assembly editor = typeof(Editor).Assembly;
            var sb = new StringBuilder();
            foreach (string typeName in TypeNames)
            {
                Type type = editor.GetType(typeName);
                sb.AppendLine($"== {typeName}: {(type == null ? "MISSING" : type.BaseType?.Name)}");
                if (type == null) continue;
                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
                foreach (ConstructorInfo c in type.GetConstructors(flags))
                    sb.AppendLine($"  ctor({string.Join(", ", c.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name))})");
                foreach (MethodInfo m in type.GetMethods(flags).Where(m => !m.IsSpecialName))
                    sb.AppendLine($"  {(m.IsStatic ? "static " : "")}{m.ReturnType.Name} {m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name))})");
                foreach (PropertyInfo p in type.GetProperties(flags))
                    sb.AppendLine($"  prop {p.PropertyType.Name} {p.Name}");
                foreach (FieldInfo f in type.GetFields(flags))
                    sb.AppendLine($"  field {f.FieldType.Name} {f.Name}");
            }

            Debug.Log("[RootAccess][Probe]\n" + sb);
        }
    }
}
