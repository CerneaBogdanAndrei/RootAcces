using System.Linq;
using RootAccess.Core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace RootAccess.EditorTools
{
    /// <summary>
    /// Builds sprite-swap AnimationClips (with Animation Events for footsteps / hit frames) and the Animator
    /// Controllers for the player, guards, drones, cameras, doors and pulsing props.
    /// </summary>
    public static class AnimationFactory
    {
        private const float PixelFps = 12f;

        public static void BuildAll(BuildContext ctx)
        {
            ctx.PlayerAnimator = BuildPlayer();
            ctx.GuardAnimator = BuildGuard("Guard", "guard_");
            ctx.HeavyAnimator = BuildGuard("GuardHeavy", "heavy_");
            ctx.SniperAnimator = BuildGuard("GuardSniper", "sniper_");
            ctx.FixerAnimator = BuildSimpleLoop("Fixer", "Idle", SpriteClip("Fixer", "Idle", 5f, true,
                Seq("fixer_idle_", 4).Concat(Seq("fixer_idle_", 4)).Concat(Seq("fixer_talk_", 4)).ToArray()));
            ctx.DroneAnimator = BuildSimpleLoop("Drone", "Hover", SpriteClip("Drone", "Hover", 14f, true, "drone_0", "drone_1"));
            ctx.CameraAnimator = BuildToggle("SecurityCamera", "Pivot", new Color(1f, 1f, 1f), new Color(0.35f, 0.35f, 0.4f));
            ctx.DoorAnimator = BuildDoor();
            ctx.PulseAnimator = BuildPulse("Pulse", "Visual");
            ctx.ElevatorAnimator = BuildToggle("Elevator", string.Empty, new Color(0.6f, 1f, 1f), Color.white);
            AssetDatabase.SaveAssets();
        }

        private static string Folder(string entity)
        {
            string folder = $"{EditorPaths.Animations}/{entity}";
            EditorAssetUtil.EnsureFolder(folder);
            return folder;
        }

        private static AnimationClip SaveClip(string entity, string clipName, AnimationClip clip, bool loop)
        {
            string path = $"{Folder(entity)}/{entity}_{clipName}.anim";
            AssetDatabase.DeleteAsset(path);
            clip.name = $"{entity}_{clipName}";
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        /// <summary>Frame names <paramref name="prefix"/>0 … <paramref name="prefix"/>(count − 1).</summary>
        private static string[] Seq(string prefix, int count) => Enumerable.Range(0, count).Select(i => prefix + i).ToArray();

        private static AnimationClip SpriteClip(string entity, string clipName, float fps, bool loop, params string[] sprites)
        {
            var clip = new AnimationClip { frameRate = fps };
            var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
            var keys = new ObjectReferenceKeyframe[sprites.Length + 1];
            for (int i = 0; i < sprites.Length; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = GeneratedArt.Sprite(sprites[i]) };
            keys[sprites.Length] = new ObjectReferenceKeyframe { time = sprites.Length / fps, value = GeneratedArt.Sprite(sprites[sprites.Length - 1]) };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            return SaveClip(entity, clipName, clip, loop);
        }

        private static void AddEvents(AnimationClip clip, string function, params float[] times)
        {
            AnimationUtility.SetAnimationEvents(clip, times.Select(t => new AnimationEvent { time = t, functionName = function }).ToArray());
            EditorUtility.SetDirty(clip);
        }

        private static AnimatorController NewController(string entity)
        {
            string path = $"{Folder(entity)}/{entity}.controller";
            AssetDatabase.DeleteAsset(path);
            return AnimatorController.CreateAnimatorControllerAtPath(path);
        }

        private static AnimatorStateTransition Transition(AnimatorState from, AnimatorState to, float duration = 0f)
        {
            AnimatorStateTransition t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = duration;
            return t;
        }

        private static AnimatorStateTransition AnyTransition(AnimatorStateMachine machine, AnimatorState to)
        {
            AnimatorStateTransition t = machine.AddAnyStateTransition(to);
            t.hasExitTime = false;
            t.duration = 0f;
            t.canTransitionToSelf = false;
            return t;
        }

        // ---------------------------------------------------------------- player

        private static AnimatorController BuildPlayer()
        {
            const string entity = "Player";
            const float runFps = 14f;
            AnimationClip idle = SpriteClip(entity, "Idle", 5f, true, Seq("player_idle_", 4));
            AnimationClip run = SpriteClip(entity, "Run", runFps, true, Seq("player_run_", 8));
            AddEvents(run, AnimationEventNames.Footstep, 0f, 4f / runFps);
            AnimationClip jump = SpriteClip(entity, "Jump", PixelFps, true, "player_jump");
            AnimationClip fall = SpriteClip(entity, "Fall", 8f, true, Seq("player_fall_", 2));
            AnimationClip dash = SpriteClip(entity, "Dash", PixelFps, true, "player_dash");
            AnimationClip wall = SpriteClip(entity, "WallSlide", PixelFps, true, "player_wallslide");
            AnimationClip crouch = SpriteClip(entity, "Crouch", 3f, true, Seq("player_crouch_", 2));
            AnimationClip swim = SpriteClip(entity, "Swim", 6f, true, Seq("player_swim_", 4));
            AnimationClip hack = SpriteClip(entity, "Hack", 6f, true, Seq("player_hack_", 4));
            AnimationClip hurt = SpriteClip(entity, "Hurt", 4f, false, "player_hurt");
            AnimationClip death = SpriteClip(entity, "Death", 5f, false, Seq("player_death_", 3));

            AnimatorController controller = NewController(entity);
            controller.AddParameter(AnimatorParams.SpeedName, AnimatorControllerParameterType.Float);
            controller.AddParameter(AnimatorParams.VerticalSpeedName, AnimatorControllerParameterType.Float);
            controller.AddParameter(AnimatorParams.GroundedName, AnimatorControllerParameterType.Bool);
            controller.AddParameter(AnimatorParams.CrouchingName, AnimatorControllerParameterType.Bool);
            controller.AddParameter(AnimatorParams.DashingName, AnimatorControllerParameterType.Bool);
            controller.AddParameter(AnimatorParams.WallSlidingName, AnimatorControllerParameterType.Bool);
            controller.AddParameter(AnimatorParams.SwimmingName, AnimatorControllerParameterType.Bool);
            controller.AddParameter(AnimatorParams.HackingName, AnimatorControllerParameterType.Bool);
            controller.AddParameter(AnimatorParams.DeadName, AnimatorControllerParameterType.Bool);
            controller.AddParameter(AnimatorParams.HurtName, AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState locomotion = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = AnimatorParams.SpeedName;
            tree.useAutomaticThresholds = false;
            tree.AddChild(idle, 0f);
            tree.AddChild(run, 0.25f);
            sm.defaultState = locomotion;

            AnimatorState sCrouch = sm.AddState("Crouch");
            sCrouch.motion = crouch;
            AnimatorState sJump = sm.AddState("Jump");
            sJump.motion = jump;
            AnimatorState sFall = sm.AddState("Fall");
            sFall.motion = fall;
            AnimatorState sDash = sm.AddState("Dash");
            sDash.motion = dash;
            AnimatorState sWall = sm.AddState("WallSlide");
            sWall.motion = wall;
            AnimatorState sSwim = sm.AddState("Swim");
            sSwim.motion = swim;
            AnimatorState sHack = sm.AddState("Hack");
            sHack.motion = hack;
            AnimatorState sHurt = sm.AddState("Hurt");
            sHurt.motion = hurt;
            AnimatorState sDeath = sm.AddState("Death");
            sDeath.motion = death;

            AnyTransition(sm, sDeath).AddCondition(AnimatorConditionMode.If, 0, AnimatorParams.DeadName);
            AnyTransition(sm, sHurt).AddCondition(AnimatorConditionMode.If, 0, AnimatorParams.HurtName);
            AnyTransition(sm, sDash).AddCondition(AnimatorConditionMode.If, 0, AnimatorParams.DashingName);
            AnyTransition(sm, sHack).AddCondition(AnimatorConditionMode.If, 0, AnimatorParams.HackingName);
            AnyTransition(sm, sSwim).AddCondition(AnimatorConditionMode.If, 0, AnimatorParams.SwimmingName);
            AnyTransition(sm, sWall).AddCondition(AnimatorConditionMode.If, 0, AnimatorParams.WallSlidingName);

            AnimatorStateTransition t = Transition(locomotion, sCrouch);
            t.AddCondition(AnimatorConditionMode.If, 0, AnimatorParams.CrouchingName);
            Transition(sCrouch, locomotion).AddCondition(AnimatorConditionMode.IfNot, 0, AnimatorParams.CrouchingName);
            Transition(sCrouch, sFall).AddCondition(AnimatorConditionMode.IfNot, 0, AnimatorParams.GroundedName);

            t = Transition(locomotion, sJump);
            t.AddCondition(AnimatorConditionMode.IfNot, 0, AnimatorParams.GroundedName);
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, AnimatorParams.VerticalSpeedName);
            t = Transition(locomotion, sFall);
            t.AddCondition(AnimatorConditionMode.IfNot, 0, AnimatorParams.GroundedName);
            t.AddCondition(AnimatorConditionMode.Less, -0.1f, AnimatorParams.VerticalSpeedName);
            Transition(sJump, sFall).AddCondition(AnimatorConditionMode.Less, 0f, AnimatorParams.VerticalSpeedName);
            Transition(sJump, locomotion).AddCondition(AnimatorConditionMode.If, 0, AnimatorParams.GroundedName);
            Transition(sFall, locomotion).AddCondition(AnimatorConditionMode.If, 0, AnimatorParams.GroundedName);

            ExitBool(sDash, AnimatorParams.DashingName, locomotion, sFall);
            ExitBool(sWall, AnimatorParams.WallSlidingName, locomotion, sFall);
            Transition(sSwim, sFall).AddCondition(AnimatorConditionMode.IfNot, 0, AnimatorParams.SwimmingName);
            Transition(sHack, locomotion).AddCondition(AnimatorConditionMode.IfNot, 0, AnimatorParams.HackingName);

            t = sHurt.AddTransition(locomotion);
            t.hasExitTime = true;
            t.exitTime = 1f;
            t.duration = 0f;
            t.AddCondition(AnimatorConditionMode.If, 0, AnimatorParams.GroundedName);
            t = sHurt.AddTransition(sFall);
            t.hasExitTime = true;
            t.exitTime = 1f;
            t.duration = 0f;
            t.AddCondition(AnimatorConditionMode.IfNot, 0, AnimatorParams.GroundedName);
            Transition(sDeath, locomotion).AddCondition(AnimatorConditionMode.IfNot, 0, AnimatorParams.DeadName);

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void ExitBool(AnimatorState from, string parameter, AnimatorState grounded, AnimatorState airborne)
        {
            AnimatorStateTransition t = Transition(from, grounded);
            t.AddCondition(AnimatorConditionMode.IfNot, 0, parameter);
            t.AddCondition(AnimatorConditionMode.If, 0, AnimatorParams.GroundedName);
            t = Transition(from, airborne);
            t.AddCondition(AnimatorConditionMode.IfNot, 0, parameter);
            t.AddCondition(AnimatorConditionMode.IfNot, 0, AnimatorParams.GroundedName);
        }

        // ---------------------------------------------------------------- guard

        /// <summary>Guard controller for one archetype; <paramref name="prefix"/> selects its frames (guard_, heavy_, sniper_).</summary>
        private static AnimatorController BuildGuard(string entity, string prefix)
        {
            const float walkFps = 9f;
            AnimationClip idle = SpriteClip(entity, "Idle", 4f, true, Seq(prefix + "idle_", 4));
            AnimationClip walk = SpriteClip(entity, "Walk", walkFps, true, Seq(prefix + "walk_", 6));
            AddEvents(walk, AnimationEventNames.Footstep, 0f, 3f / walkFps);
            AnimationClip attack = SpriteClip(entity, "Attack", 10f, false, Seq(prefix + "attack_", 3));
            AddEvents(attack, AnimationEventNames.HitFrame, 1f / 10f);

            AnimatorController controller = NewController(entity);
            controller.AddParameter(AnimatorParams.SpeedName, AnimatorControllerParameterType.Float);
            controller.AddParameter(AnimatorParams.AlertName, AnimatorControllerParameterType.Bool);
            controller.AddParameter(AnimatorParams.AttackName, AnimatorControllerParameterType.Trigger);
            AnimatorStateMachine sm = controller.layers[0].stateMachine;

            AnimatorState locomotion = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = AnimatorParams.SpeedName;
            tree.useAutomaticThresholds = false;
            tree.AddChild(idle, 0f);
            tree.AddChild(walk, 0.15f);
            sm.defaultState = locomotion;

            AnimatorState sAttack = sm.AddState("Attack");
            sAttack.motion = attack;
            AnyTransition(sm, sAttack).AddCondition(AnimatorConditionMode.If, 0, AnimatorParams.AttackName);
            AnimatorStateTransition back = sAttack.AddTransition(locomotion);
            back.hasExitTime = true;
            back.exitTime = 1f;
            back.duration = 0f;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        // ---------------------------------------------------------------- props

        private static AnimatorController BuildSimpleLoop(string entity, string stateName, AnimationClip clip)
        {
            AnimatorController controller = NewController(entity);
            AnimatorState state = controller.layers[0].stateMachine.AddState(stateName);
            state.motion = clip;
            controller.layers[0].stateMachine.defaultState = state;
            return controller;
        }

        private static AnimationClip ColorClip(string entity, string clipName, string path, Color from, Color to, float seconds, bool loop)
        {
            var clip = new AnimationClip();
            string[] channels = { "r", "g", "b", "a" };
            for (int i = 0; i < 4; i++)
            {
                AnimationCurve curve = loop
                    ? new AnimationCurve(new Keyframe(0f, from[i]), new Keyframe(seconds * 0.5f, to[i]), new Keyframe(seconds, from[i]))
                    : AnimationCurve.Constant(0f, seconds, to[i]);
                clip.SetCurve(path, typeof(SpriteRenderer), $"m_Color.{channels[i]}", curve);
            }

            return SaveClip(entity, clipName, clip, loop);
        }

        /// <summary>Two-state Active toggle animating a SpriteRenderer colour (cameras on/off, elevator lights).</summary>
        private static AnimatorController BuildToggle(string entity, string path, Color on, Color off)
        {
            AnimationClip active = ColorClip(entity, "Active", path, on, Color.Lerp(on, Color.white, 0.5f), 1.2f, true);
            AnimationClip inactive = ColorClip(entity, "Inactive", path, off, off, 0.1f, false);
            AnimatorController controller = NewController(entity);
            controller.AddParameter(AnimatorParams.ActiveName, AnimatorControllerParameterType.Bool);
            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState sOn = sm.AddState("Active");
            sOn.motion = active;
            AnimatorState sOff = sm.AddState("Inactive");
            sOff.motion = inactive;
            sm.defaultState = sOn;
            Transition(sOn, sOff, 0.1f).AddCondition(AnimatorConditionMode.IfNot, 0, AnimatorParams.ActiveName);
            Transition(sOff, sOn, 0.1f).AddCondition(AnimatorConditionMode.If, 0, AnimatorParams.ActiveName);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        /// <summary>Scale pulse when Active (network nodes, checkpoints, pads).</summary>
        private static AnimatorController BuildPulse(string entity, string path)
        {
            var pulse = new AnimationClip();
            AnimationCurve scale = new(new Keyframe(0f, 1f), new Keyframe(0.6f, 1.12f), new Keyframe(1.2f, 1f));
            pulse.SetCurve(path, typeof(Transform), "m_LocalScale.x", scale);
            pulse.SetCurve(path, typeof(Transform), "m_LocalScale.y", scale);
            pulse = SaveClip(entity, "Active", pulse, true);

            var idle = new AnimationClip();
            idle.SetCurve(path, typeof(Transform), "m_LocalScale.x", AnimationCurve.Constant(0f, 0.1f, 0.9f));
            idle.SetCurve(path, typeof(Transform), "m_LocalScale.y", AnimationCurve.Constant(0f, 0.1f, 0.9f));
            idle = SaveClip(entity, "Idle", idle, false);

            AnimatorController controller = NewController(entity);
            controller.AddParameter(AnimatorParams.ActiveName, AnimatorControllerParameterType.Bool);
            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState sIdle = sm.AddState("Idle");
            sIdle.motion = idle;
            AnimatorState sActive = sm.AddState("Active");
            sActive.motion = pulse;
            sm.defaultState = sIdle;
            Transition(sIdle, sActive, 0.2f).AddCondition(AnimatorConditionMode.If, 0, AnimatorParams.ActiveName);
            Transition(sActive, sIdle, 0.2f).AddCondition(AnimatorConditionMode.IfNot, 0, AnimatorParams.ActiveName);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        /// <summary>Door slides its "Panel" child up (Opening) or down (Closing), holding the last frame.</summary>
        private static AnimatorController BuildDoor()
        {
            const string entity = "Door";
            const float travel = 2.9f;
            AnimationClip Slide(string name, float from, float to, float seconds)
            {
                var clip = new AnimationClip();
                clip.SetCurve("Panel", typeof(Transform), "m_LocalPosition.y", AnimationCurve.EaseInOut(0f, from, seconds, to));
                return SaveClip(entity, name, clip, false);
            }

            AnimationClip closed = Slide("Closed", 0f, 0f, 0.05f);
            AnimationClip opening = Slide("Opening", 0f, travel, 0.45f);
            AnimationClip closing = Slide("Closing", travel, 0f, 0.35f);

            AnimatorController controller = NewController(entity);
            controller.AddParameter(AnimatorParams.IsOpenName, AnimatorControllerParameterType.Bool);
            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState sClosed = sm.AddState("Closed");
            sClosed.motion = closed;
            AnimatorState sOpening = sm.AddState("Opening");
            sOpening.motion = opening;
            AnimatorState sClosing = sm.AddState("Closing");
            sClosing.motion = closing;
            sm.defaultState = sClosed;
            Transition(sClosed, sOpening).AddCondition(AnimatorConditionMode.If, 0, AnimatorParams.IsOpenName);
            Transition(sOpening, sClosing).AddCondition(AnimatorConditionMode.IfNot, 0, AnimatorParams.IsOpenName);
            Transition(sClosing, sOpening).AddCondition(AnimatorConditionMode.If, 0, AnimatorParams.IsOpenName);
            EditorUtility.SetDirty(controller);
            return controller;
        }
    }
}
