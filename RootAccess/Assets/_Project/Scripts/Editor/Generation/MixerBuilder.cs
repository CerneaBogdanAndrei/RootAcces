using System;
using System.Linq;
using System.Reflection;
using RootAccess.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace RootAccess.EditorTools
{
    /// <summary>
    /// Creates the game's AudioMixer: Master → {Music, SFX, UI, Ambience}, a low-pass on Master, exposed volume
    /// parameters for the settings menu and Normal / Underwater / Alarm snapshots.
    /// Unity has no public API for authoring mixers, so this drives the editor's internal AudioMixerController via
    /// reflection (signatures verified with ReflectionProbe.DumpMixerApi on Unity 6000.3).
    /// </summary>
    public static class MixerBuilder
    {
        public const string MusicGroup = "Music";
        public const string SfxGroup = "SFX";
        public const string UIGroup = "UI";
        public const string AmbienceGroup = "Ambience";
        private const string LowpassEffect = "Lowpass Simple";
        private const string LowpassParameter = "Cutoff freq";
        private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public sealed class Result
        {
            public AudioMixer Mixer;
            public AudioMixerGroup Master, Music, Sfx, UI, Ambience;
            public AudioMixerSnapshot Normal, Underwater, Alarm;
        }

        public static Result Build(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<AudioMixer>(path) == null) Create(path);
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
            if (mixer == null) throw new InvalidOperationException($"Mixer was not created at {path}.");

            var result = new Result
            {
                Mixer = mixer,
                Master = mixer.FindMatchingGroups("Master").FirstOrDefault(),
                Music = Group(mixer, MusicGroup),
                Sfx = Group(mixer, SfxGroup),
                UI = Group(mixer, UIGroup),
                Ambience = Group(mixer, AmbienceGroup),
                Normal = mixer.FindSnapshot(MixerSnapshotNames.Normal),
                Underwater = mixer.FindSnapshot(MixerSnapshotNames.Underwater),
                Alarm = mixer.FindSnapshot(MixerSnapshotNames.Alarm)
            };

            if (result.Music == null || result.Sfx == null || result.UI == null || result.Ambience == null)
                throw new InvalidOperationException("Mixer groups missing after creation.");
            if (result.Normal == null || result.Underwater == null || result.Alarm == null)
                throw new InvalidOperationException("Mixer snapshots missing after creation.");
            return result;
        }

        private static AudioMixerGroup Group(AudioMixer mixer, string name) =>
            mixer.FindMatchingGroups(name).FirstOrDefault(g => g.name == name);

        private static void Create(string path)
        {
            Assembly editor = typeof(Editor).Assembly;
            Type controllerType = Require(editor, "UnityEditor.Audio.AudioMixerController");
            Type effectType = Require(editor, "UnityEditor.Audio.AudioMixerEffectController");
            Type groupPathType = Require(editor, "UnityEditor.Audio.AudioGroupParameterPath");
            Type exposedType = Require(editor, "UnityEditor.Audio.ExposedAudioParameter");

            object controller = Call(controllerType, null, "CreateMixerControllerAtPath", path);
            object master = Get(controller, "masterGroup");

            object music = NewGroup(controller, MusicGroup, master);
            object sfx = NewGroup(controller, SfxGroup, master);
            object ui = NewGroup(controller, UIGroup, master);
            object ambience = NewGroup(controller, AmbienceGroup, master);

            object lowpass = Activator.CreateInstance(effectType, Any, null, new object[] { LowpassEffect }, null);
            AssetDatabase.AddObjectToAsset((Object)lowpass, (Object)controller);
            Call(effectType, lowpass, "PreallocateGUIDs");
            var masterEffects = (Array)Get(master, "effects");
            Call(master.GetType(), master, "InsertEffect", lowpass, masterEffects.Length);
            var lowpassGuid = (GUID)Call(effectType, lowpass, "GetGUIDForParameter", LowpassParameter);

            var snapshots = (Array)Get(controller, "snapshots");
            object normal = snapshots.GetValue(0);
            ((Object)normal).name = MixerSnapshotNames.Normal;
            Set(controller, "TargetSnapshot", normal);
            object underwater = CloneSnapshot(controller, MixerSnapshotNames.Underwater);
            Set(controller, "TargetSnapshot", normal);
            object alarm = CloneSnapshot(controller, MixerSnapshotNames.Alarm);

            var ambienceVolume = (GUID)Call(ambience.GetType(), ambience, "GetGUIDForVolume");
            SetSnapshotValue(normal, lowpassGuid, 22000f);
            SetSnapshotValue(underwater, lowpassGuid, 650f);
            SetSnapshotValue(alarm, lowpassGuid, 22000f);
            SetSnapshotValue(normal, ambienceVolume, 0f);
            SetSnapshotValue(underwater, ambienceVolume, -10f);
            SetSnapshotValue(alarm, ambienceVolume, -22f);
            Set(controller, "TargetSnapshot", normal);

            Expose(controller, groupPathType, exposedType, master, MixerParameters.Master);
            Expose(controller, groupPathType, exposedType, music, MixerParameters.Music);
            Expose(controller, groupPathType, exposedType, sfx, MixerParameters.Sfx);
            Expose(controller, groupPathType, exposedType, ui, MixerParameters.UI);

            EditorUtility.SetDirty((Object)controller);
            AssetDatabase.SaveAssets();
            EditorAssetUtil.Log($"AudioMixer created at {path} (4 groups, 3 snapshots, {MixerParameters.Exposed.Length} exposed params).");
        }

        private static object NewGroup(object controller, string name, object parent)
        {
            object group = Call(controller.GetType(), controller, "CreateNewGroup", name, false);
            Call(controller.GetType(), controller, "AddChildToParent", group, parent);
            return group;
        }

        private static object CloneSnapshot(object controller, string name)
        {
            Call(controller.GetType(), controller, "CloneNewSnapshotFromTarget", false);
            var snapshots = (Array)Get(controller, "snapshots");
            object clone = snapshots.GetValue(snapshots.Length - 1);
            ((Object)clone).name = name;
            return clone;
        }

        private static void SetSnapshotValue(object snapshot, GUID guid, float value) =>
            Call(snapshot.GetType(), snapshot, "SetValue", guid, value);

        private static void Expose(object controller, Type groupPathType, Type exposedType, object group, string parameterName)
        {
            var volumeGuid = (GUID)Call(group.GetType(), group, "GetGUIDForVolume");
            object path = Activator.CreateInstance(groupPathType, Any, null, new[] { group, (object)volumeGuid }, null);
            Call(controller.GetType(), controller, "AddExposedParameter", path);

            var exposed = (Array)Get(controller, "exposedParameters");
            FieldInfo guidField = exposedType.GetField("guid", Any);
            FieldInfo nameField = exposedType.GetField("name", Any);
            for (int i = 0; i < exposed.Length; i++)
            {
                object entry = exposed.GetValue(i);
                if (!guidField!.GetValue(entry).Equals(volumeGuid)) continue;
                nameField!.SetValue(entry, parameterName);
                exposed.SetValue(entry, i);
            }

            Set(controller, "exposedParameters", exposed);
        }

        private static Type Require(Assembly assembly, string name) =>
            assembly.GetType(name) ?? throw new InvalidOperationException($"Internal type {name} not found (Unity API changed?).");

        private static object Call(Type type, object target, string method, params object[] args)
        {
            MethodInfo info = type.GetMethods(Any).FirstOrDefault(m => m.Name == method && m.GetParameters().Length == args.Length)
                              ?? throw new MissingMethodException(type.Name, method);
            return info.Invoke(target, args);
        }

        private static object Get(object target, string property)
        {
            PropertyInfo info = target.GetType().GetProperty(property, Any) ?? throw new MissingMemberException(target.GetType().Name, property);
            return info.GetValue(target);
        }

        private static void Set(object target, string property, object value)
        {
            PropertyInfo info = target.GetType().GetProperty(property, Any) ?? throw new MissingMemberException(target.GetType().Name, property);
            info.SetValue(target, value);
        }
    }
}
