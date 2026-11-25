using System.Collections.Generic;
using System.Linq;
using RootAccess.Cinematics;
using RootAccess.Core;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.Timeline;

namespace RootAccess.EditorTools
{
    /// <summary>
    /// Builds the two Timeline cutscenes (bonus "cinematics"): the intro after New Game and the finale after Level 5.
    /// Each is a small scene with a rooftop set, a puppet of the hacker, two Cinemachine cameras and a subtitle canvas,
    /// driven by a Timeline asset with Animation (puppet walk + sprite frames), Cinemachine (shots with blends),
    /// Activation (titles, subtitles, the puppet vanishing) and Signal tracks (shake, alarm, teleport VFX).
    /// <see cref="CutsceneController"/> plays it on start and loads the next scene; <see cref="SkipCutscene"/> skips it.
    /// </summary>
    internal static class CutsceneFactory
    {
        private const string Folder = EditorPaths.Root + "/Timelines";
        private const float RunFrameSeconds = 0.1f;
        private const float IdleFrameSeconds = 0.5f;

        private sealed class Stage
        {
            public PlayableDirector Director;
            public TimelineAsset Timeline;
            public Animator Puppet;
            public Transform Body;
            public CinemachineBrain Brain;
            public CinemachineCamera Wide;
            public CinemachineCamera Close;
            public GameObject Signals;
            public Transform Captions;
        }

        public static void BuildAll(BuildContext ctx)
        {
            EditorAssetUtil.EnsureFolder(Folder);
            BuildIntro(ctx);
            BuildFinale(ctx);
        }

        // ---------------------------------------------------------------- the two cutscenes

        private static void BuildIntro(BuildContext ctx)
        {
            Scene scene = SceneBuilder.NewScene();
            Stage stage = BuildStage(ctx, "TL_Intro", SceneNames.Hub, ctx.Level01Profile, -6f);
            const float duration = 17f;

            Shot(stage, stage.Wide, 0f, 6.5f);
            Shot(stage, stage.Close, 5.5f, duration);
            Walk(stage, "AC_Intro_Puppet", duration, -6f, 6f, 5f, 12f);
            Show(stage, Caption(stage, "Title", Strings.Cutscenes.IntroTitle, 96f, new Vector2(0f, 120f), UIFactory.Cyan), 0.5f, 4.5f);
            Show(stage, Caption(stage, "Line_1", Strings.Cutscenes.Intro1), 1f, 6f);
            Show(stage, Caption(stage, "Line_2", Strings.Cutscenes.Intro2), 6.5f, 11.5f);
            Show(stage, Caption(stage, "Line_3", Strings.Cutscenes.Intro3), 12f, 16.5f);
            Show(stage, stage.Body.gameObject, 0f, 13.6f);
            Signal(stage, "SIG_DataStream", 12.5f, r => r.PlayDataStream);
            Signal(stage, "SIG_Teleport", 13.5f, r => r.PlayTeleport);
            Signal(stage, "SIG_Shake", 13.5f, r => r.Shake, 0.5f);
            Finish(stage, scene, SceneNames.Intro);
        }

        private static void BuildFinale(BuildContext ctx)
        {
            Scene scene = SceneBuilder.NewScene();
            Stage stage = BuildStage(ctx, "TL_Finale", SceneNames.Credits, ctx.Level05Profile, -4f);
            const float duration = 16f;

            Shot(stage, stage.Close, 0f, 7f);
            Shot(stage, stage.Wide, 6f, duration);
            Walk(stage, "AC_Finale_Puppet", duration, -4f, 5f, 0.5f, 6f);
            Show(stage, Caption(stage, "Line_1", Strings.Cutscenes.Finale1), 0.5f, 5f);
            Show(stage, Caption(stage, "Line_2", Strings.Cutscenes.Finale2), 5.5f, 9.5f);
            Show(stage, Caption(stage, "Title", Strings.Cutscenes.FinaleTitle, 80f, new Vector2(0f, 120f), UIFactory.Cyan), 9f, 15f);
            Show(stage, Caption(stage, "Line_3", Strings.Cutscenes.Finale3), 10f, 15.5f);
            Show(stage, stage.Body.gameObject, 0f, 8.1f);
            Signal(stage, "SIG_Alarm", 1f, r => r.PlayAlarm);
            Signal(stage, "SIG_Shake", 1f, r => r.Shake, 0.4f);
            Signal(stage, "SIG_Teleport", 8f, r => r.PlayTeleport);
            Signal(stage, "SIG_ShakeStrong", 8f, r => r.Shake, 0.8f);
            Finish(stage, scene, SceneNames.Finale);
        }

        // ---------------------------------------------------------------- set

        /// <summary>Rooftop set at night: lights, skyline, rain, the puppet, two cameras, subtitles and the director.</summary>
        private static Stage BuildStage(BuildContext ctx, string timelineName, string nextScene, UnityEngine.Rendering.VolumeProfile profile, float puppetX)
        {
            var stage = new Stage();
            Transform setup = SceneBuilder.Group("--- Setup ---");
            SceneBuilder.GlobalLight(setup, new Color(0.55f, 0.6f, 0.95f), 0.45f);
            SceneBuilder.PostVolume(setup, BuildContext.Fresh(profile));

            GameObject cameraGo = PrefabFactory.Go("Main Camera", setup);
            cameraGo.tag = Tags.MainCamera;
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.01f, 0.05f);
            camera.transform.position = new Vector3(0f, 3f, -10f);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            cameraGo.AddComponent<AudioListener>();
            stage.Brain = cameraGo.AddComponent<CinemachineBrain>();
            stage.Brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 1.2f);

            Transform set = SceneBuilder.Group("--- Set ---");
            SceneBuilder.Parallax(ctx, set, -40f, 60f, -2f);
            Transform grid = SceneBuilder.Group("Grid", set);
            grid.gameObject.AddComponent<Grid>();
            Tilemap roof = SceneBuilder.Tilemap(grid, "Roof", Layers.Default, ctx.Metal, SortingLayers.Environment, 0, false, SurfaceType.Metal, Color.white);
            SceneBuilder.Fill(roof, -14, 14, -6, -1, ctx.TileMetal, ctx.TileMetalTop);
            SceneBuilder.Place(ctx, "NeonSign", new Vector3(-8f, 5f), set);
            SceneBuilder.Place(ctx, "NeonSign", new Vector3(9f, 6f), set);
            SceneBuilder.Place(ctx, "StreetLamp", new Vector3(-11f, 0f), set);
            SceneBuilder.Place(ctx, "StreetLamp", new Vector3(11f, 0f), set);
            SceneBuilder.Place(ctx, "RooftopAC", new Vector3(-2f, 0f), set);
            SceneBuilder.Place(ctx, "NetworkNode", new Vector3(7.5f, 0f), set, "Uplink");
            SceneBuilder.Place(ctx.VfxPrefabs[VfxId.Rain], new Vector3(0f, 18f), set, "Rain");

            GameObject puppet = PrefabFactory.Go("Puppet", set);
            stage.Puppet = puppet.AddComponent<Animator>();
            stage.Body = PrefabFactory.Go("Body", puppet.transform, Layers.Default, new Vector3(puppetX, 0f, 0f)).transform;
            PrefabFactory.Sprite(stage.Body.gameObject, "player_idle_0", ctx.SpriteLit, SortingLayers.Characters, 10);
            PrefabFactory.Light(stage.Body, "Glow", Light2D.LightType.Point, new Color(0.3f, 0.9f, 1f), 0.6f, 2.4f, new Vector3(0f, 1f, 0f));

            stage.Wide = VirtualCamera("CM_Wide", setup, new Vector3(0f, 3.5f, -10f), 7f, null);
            stage.Close = VirtualCamera("CM_Close", setup, new Vector3(puppetX, 1.5f, -10f), 3.6f, stage.Body);

            stage.Signals = PrefabFactory.Go("Signals", setup);
            EditorAssetUtil.SetField(stage.Signals.AddComponent<CutsceneSignalReceiver>(), "effectAnchor", stage.Body);
            stage.Signals.AddComponent<SignalReceiver>();

            GameObject canvas = UIFactory.Canvas("Captions", 60);
            stage.Captions = canvas.transform;
            TextMeshProUGUI skip = UIFactory.Text("SkipHint", canvas.transform, Strings.Cutscenes.Skip, 24f, TextAlignmentOptions.Right, UIFactory.Muted);
            UIFactory.Place(skip.rectTransform, new Vector2(1f, 0f), new Vector2(-40f, 36f), new Vector2(600f, 40f));

            GameObject director = PrefabFactory.Go("Cutscene", setup);
            stage.Director = director.AddComponent<PlayableDirector>();
            stage.Timeline = Timeline(timelineName);
            stage.Director.playableAsset = stage.Timeline;
            stage.Director.playOnAwake = false;
            stage.Director.extrapolationMode = DirectorWrapMode.None;
            EditorAssetUtil.SetFields(director.AddComponent<CutsceneController>(), so =>
            {
                so.Require("playOnStart").boolValue = true;
                so.Require("sceneAfterCutscene").stringValue = nextScene;
            });
            EditorAssetUtil.SetField(director.AddComponent<SkipCutscene>(), "input", BuildContext.Fresh(ctx.Input));
            return stage;
        }

        private static CinemachineCamera VirtualCamera(string name, Transform parent, Vector3 position, float size, Transform follow)
        {
            GameObject go = PrefabFactory.Go(name, parent, Layers.Default, position);
            var vcam = go.AddComponent<CinemachineCamera>();
            LensSettings lens = LensSettings.Default;
            lens.OrthographicSize = size;
            lens.NearClipPlane = 0.1f;
            lens.FarClipPlane = 100f;
            vcam.Lens = lens;
            if (follow == null) return vcam;
            vcam.Target.TrackingTarget = follow;
            var composer = go.AddComponent<CinemachinePositionComposer>();
            composer.CameraDistance = 10f;
            composer.Damping = new Vector3(0.8f, 0.6f, 0f);
            composer.TargetOffset = new Vector3(0f, 1.2f, 0f);
            return vcam;
        }

        private static GameObject Caption(Stage stage, string name, string text, float size = 40f, Vector2? position = null, Color? color = null)
        {
            TextMeshProUGUI label = UIFactory.Text(name, stage.Captions, text, size, TextAlignmentOptions.Center, color ?? Color.white,
                size > 60f ? FontStyles.Bold : FontStyles.Normal);
            UIFactory.Place(label.rectTransform, new Vector2(0.5f, position.HasValue ? 0.5f : 0f), position ?? new Vector2(0f, 110f), new Vector2(1500f, 160f));
            label.gameObject.SetActive(false);
            return label.gameObject;
        }

        // ---------------------------------------------------------------- timeline

        /// <summary>Reuses the timeline asset (stable GUID, small diffs) but rebuilds every track from scratch.</summary>
        private static TimelineAsset Timeline(string name)
        {
            string path = $"{Folder}/{name}.playable";
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
            if (timeline == null)
            {
                timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                AssetDatabase.CreateAsset(timeline, path);
            }

            foreach (TrackAsset track in timeline.GetRootTracks().ToList()) timeline.DeleteTrack(track);
            foreach (TrackAsset track in timeline.GetOutputTracks().ToList()) timeline.DeleteTrack(track);
            if (timeline.markerTrack != null) timeline.DeleteTrack(timeline.markerTrack);
            return timeline;
        }

        private static T Track<T>(Stage stage, string name, Object binding) where T : TrackAsset, new()
        {
            T track = stage.Timeline.GetOutputTracks().OfType<T>().FirstOrDefault(t => t.name == name) ?? stage.Timeline.CreateTrack<T>(null, name);
            if (binding != null) stage.Director.SetGenericBinding(track, binding);
            return track;
        }

        /// <summary>A Cinemachine shot; shots that overlap blend (the overlap is the blend time).</summary>
        private static void Shot(Stage stage, CinemachineCamera vcam, float start, float end)
        {
            var track = Track<CinemachineTrack>(stage, "Cameras", stage.Brain);
            TimelineClip clip = track.CreateClip<CinemachineShot>();
            clip.start = start;
            clip.duration = end - start;
            clip.displayName = vcam.name;
            var shot = (CinemachineShot)clip.asset;
            shot.VirtualCamera.exposedName = GUID.Generate().ToString();
            stage.Director.SetReferenceValue(shot.VirtualCamera.exposedName, vcam);
        }

        /// <summary>Activation clip: the object is visible only between <paramref name="start"/> and <paramref name="end"/>.</summary>
        private static void Show(Stage stage, GameObject target, float start, float end)
        {
            var track = stage.Timeline.CreateTrack<ActivationTrack>(null, target.name);
            stage.Director.SetGenericBinding(track, target);
            TimelineClip clip = track.CreateDefaultClip();
            clip.start = start;
            clip.duration = end - start;
            track.postPlaybackState = ActivationTrack.PostPlaybackState.Inactive;
        }

        /// <summary>
        /// Puppet animation: the Body walks from <paramref name="fromX"/> to <paramref name="toX"/> between the two times,
        /// flipping through the run frames, and idles before and after. The curves target the child "Body", so the
        /// Animator root never moves (no root-motion offsets to manage).
        /// </summary>
        private static void Walk(Stage stage, string clipName, float duration, float fromX, float toX, float walkStart, float walkEnd)
        {
            string path = $"{Folder}/{clipName}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = clipName };
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.ClearCurves();
            var x = new AnimationCurve(new Keyframe(0f, fromX), new Keyframe(walkStart, fromX), new Keyframe(walkEnd, toX), new Keyframe(duration, toX));
            for (int i = 0; i < x.length; i++) AnimationUtility.SetKeyLeftTangentMode(x, i, AnimationUtility.TangentMode.Linear);
            for (int i = 0; i < x.length; i++) AnimationUtility.SetKeyRightTangentMode(x, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("Body", typeof(Transform), "m_LocalPosition.x"), x);

            Sprite[] idle = Frames("player_idle_");
            Sprite[] run = Frames("player_run_");
            var keys = new List<ObjectReferenceKeyframe>();
            int frame = 0;
            for (float t = 0f; t < duration; frame++)
            {
                bool walking = t >= walkStart && t < walkEnd;
                Sprite[] set = walking ? run : idle;
                keys.Add(new ObjectReferenceKeyframe { time = t, value = set[frame % set.Length] });
                t += walking ? RunFrameSeconds : IdleFrameSeconds;
            }

            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("Body", typeof(SpriteRenderer), "m_Sprite"), keys.ToArray());
            EditorUtility.SetDirty(clip);

            var track = Track<AnimationTrack>(stage, "Puppet", stage.Puppet);
            TimelineClip timelineClip = track.CreateClip(clip);
            timelineClip.start = 0f;
            timelineClip.duration = duration;
        }

        private static Sprite[] Frames(string prefix)
        {
            var frames = new List<Sprite>();
            for (int i = 0; i < 16; i++)
            {
                string path = GeneratedArt.SpritePath(prefix + i);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) break;
                frames.Add(sprite);
            }

            if (frames.Count == 0) throw new System.InvalidOperationException($"[CutsceneFactory] No sprites named {prefix}N.");
            return frames.ToArray();
        }

        /// <summary>Signal emitter at <paramref name="time"/> wired to a <see cref="CutsceneSignalReceiver"/> method.</summary>
        private static void Signal(Stage stage, string signalName, float time, System.Func<CutsceneSignalReceiver, UnityAction> method)
        {
            SignalAsset asset = SignalAssetNamed(signalName);
            UnityEvent reaction = Reaction(stage, asset);
            UnityEventTools.AddVoidPersistentListener(reaction, method(stage.Signals.GetComponent<CutsceneSignalReceiver>()));
            Emitter(stage, asset, time);
        }

        private static void Signal(Stage stage, string signalName, float time, System.Func<CutsceneSignalReceiver, UnityAction<float>> method, float argument)
        {
            SignalAsset asset = SignalAssetNamed(signalName);
            UnityEvent reaction = Reaction(stage, asset);
            UnityEventTools.AddFloatPersistentListener(reaction, method(stage.Signals.GetComponent<CutsceneSignalReceiver>()), argument);
            Emitter(stage, asset, time);
        }

        private static UnityEvent Reaction(Stage stage, SignalAsset asset)
        {
            var receiver = stage.Signals.GetComponent<SignalReceiver>();
            if (receiver.GetReaction(asset) == null) receiver.AddReaction(asset, new UnityEvent());
            return receiver.GetReaction(asset);
        }

        private static void Emitter(Stage stage, SignalAsset asset, float time)
        {
            var track = Track<SignalTrack>(stage, "Signals", stage.Signals);
            var emitter = track.CreateMarker<SignalEmitter>(time);
            emitter.asset = asset;
            emitter.retroactive = false;
            emitter.emitOnce = true;
        }

        private static SignalAsset SignalAssetNamed(string name) =>
            EditorAssetUtil.LoadOrCreateAsset<SignalAsset>($"{Folder}/{name}.signal");

        private static void Finish(Stage stage, Scene scene, string sceneName)
        {
            EditorUtility.SetDirty(stage.Timeline);
            AssetDatabase.SaveAssets();
            EditorAssetUtil.Log($"Cutscene {sceneName}: {stage.Timeline.GetOutputTracks().Count()} tracks, {stage.Timeline.duration:0.0}s.");
            SceneBuilder.Save(scene, sceneName);
        }
    }
}
