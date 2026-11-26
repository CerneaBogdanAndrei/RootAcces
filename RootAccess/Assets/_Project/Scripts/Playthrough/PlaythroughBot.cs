using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RootAccess.Core;
using RootAccess.Gameplay;
using RootAccess.Hacking;
using RootAccess.Player;
using RootAccess.Systems;
using RootAccess.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RootAccess.Playthrough
{
    /// <summary>
    /// Automated playthrough of the real build (<c>-ra-playthrough</c>): New game, intro, the hideout (contracts, travel),
    /// levels 1–6 along their generated <see cref="LevelRoute"/>, the finale and the credits. It plays with real input on a
    /// virtual keyboard (run, jump, double jump, dash, wall-jump, crouch, swim, interact) and solves every hack through the
    /// overlay. Whatever it cannot pass is noted, photographed and skipped with a teleport ("assist"), so the run always
    /// reaches the end and the report (<c>Playthrough.md</c>) lists every problem.
    /// <para><c>-ra-playthrough-level Level03_ServerFarm</c> plays one level only; <c>-ra-playthrough-out dir</c> picks the
    /// output folder; add <c>-ra-invulnerable</c> to check the route alone (hits are still counted).</para>
    /// </summary>
    public sealed class PlaythroughBot : MonoBehaviour
    {
        private const string Arg = "-ra-playthrough";
        private const string LevelArg = "-ra-playthrough-level";
        private const string OutArg = "-ra-playthrough-out";
        private const string IgnoredHitLog = "Hit ignored (automation)";

        private enum Result { Running, Done, Died, Failed }

        private sealed class Outcome
        {
            public Result Result = Result.Running;
            public string Reason;
            public void Done() => Result = Result.Done;
            public void Died() => Result = Result.Died;
            public void Fail(string reason)
            {
                Result = Result.Failed;
                Reason = reason;
            }
        }

        private BotKeyboard _keys;
        private readonly HackSolver _solver = new();
        private PlaythroughReport _report;
        private HackingOverlay _overlay;
        private PlayerController _player;
        private RouteStep _step;
        private int _stepIndex;
        private int _resumeIndex;
        private bool _died;
        private bool _fell;
        private bool _levelDone;
        private bool _promptVisible;
        private int _lastHealth = -1;
        private float _jumpReleaseAt;
        private int _airJumpStage;
        private float _dialogueTapAt;
        private float _retryAt = -1f;
        private float _levelLoadedAt;
        private int _shotIndex;
        private string _loadedScene;
        private int _lastHitFrame = -1;
        private readonly Queue<string> _trace = new();
        private string _lastHitSegment;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), Arg) < 0 && Value(LevelArg) == null) return;
            var go = new GameObject("PlaythroughBot");
            DontDestroyOnLoad(go);
            go.AddComponent<PlaythroughBot>();
        }

        private static string Value(string key)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, key);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        // ---------------------------------------------------------------- lifetime

        private void Awake()
        {
            // The bot plays while the window may be in the background: keep its virtual keyboard enabled without focus.
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            _keys = new BotKeyboard();
            string folder = Value(OutArg) ?? Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Playthrough");
            _report = new PlaythroughReport(folder);
            Directory.CreateDirectory(folder);
            Application.logMessageReceived += OnLog;
        }

        private void OnEnable()
        {
            EventBus<PlayerDied>.Subscribe(OnDied);
            EventBus<PlayerRespawned>.Subscribe(OnRespawned);
            EventBus<PlayerHealthChanged>.Subscribe(OnHealth);
            EventBus<CheckpointReached>.Subscribe(OnCheckpoint);
            EventBus<LevelCompleted>.Subscribe(OnLevelCompleted);
            EventBus<HackSessionEnded>.Subscribe(OnHackEnded);
            EventBus<HackSessionStarted>.Subscribe(OnHackStarted);
            EventBus<TraceLevelChanged>.Subscribe(OnTraceLevel);
            EventBus<InteractionPromptChanged>.Subscribe(OnPrompt);
            EventBus<SceneLoadCompleted>.Subscribe(OnSceneLoaded);
        }

        private void OnDisable()
        {
            EventBus<PlayerDied>.Unsubscribe(OnDied);
            EventBus<PlayerRespawned>.Unsubscribe(OnRespawned);
            EventBus<PlayerHealthChanged>.Unsubscribe(OnHealth);
            EventBus<CheckpointReached>.Unsubscribe(OnCheckpoint);
            EventBus<LevelCompleted>.Unsubscribe(OnLevelCompleted);
            EventBus<HackSessionEnded>.Unsubscribe(OnHackEnded);
            EventBus<HackSessionStarted>.Unsubscribe(OnHackStarted);
            EventBus<TraceLevelChanged>.Unsubscribe(OnTraceLevel);
            EventBus<InteractionPromptChanged>.Unsubscribe(OnPrompt);
            EventBus<SceneLoadCompleted>.Unsubscribe(OnSceneLoaded);
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
            _keys?.Dispose();
        }

        private void Start() => StartCoroutine(Run());

        private void Update()
        {
            GameState state = ServiceLocator.TryGet(out GameManager game) ? game.State : GameState.Boot;
            if (state == GameState.Hacking)
            {
                if (_overlay == null) _overlay = FindFirstObjectByType<HackingOverlay>();
                _solver.Tick(_overlay);
            }

            if (state == GameState.Dialogue && Time.unscaledTime >= _dialogueTapAt)
            {
                _keys.Tap(Key.Enter);
                _dialogueTapAt = Time.unscaledTime + 0.6f;
            }

            if (state == GameState.GameOver)
            {
                if (_retryAt < 0f) _retryAt = Time.unscaledTime + 1.5f;
                else if (Time.unscaledTime >= _retryAt)
                {
                    _retryAt = -1f;
                    ClickButton(FindFirstObjectByType<GameOverView>(FindObjectsInactive.Include), "Retry");
                }
            }

            LevelRun run = _report.Current;
            if (run != null && Time.unscaledTime - _levelLoadedAt > 2f && state == GameState.Playing)
            {
                float dt = Time.unscaledDeltaTime;
                run.FrameTimeSum += dt;
                run.Frames++;
                run.WorstFrame = Mathf.Max(run.WorstFrame, dt);
            }
        }

        private void LateUpdate()
        {
            if (_player != null && _report.Current != null)
            {
                _trace.Enqueue($"t={Time.time:0.00} x={_player.Position.x:0.00} y={_player.Position.y:0.00} vy={_player.Velocity.y:0.0} " +
                               $"{_player.StateId} g={(_player.IsGrounded ? 1 : 0)} keys={Keys()}");
                while (_trace.Count > 90) _trace.Dequeue();
            }

            if (_airJumpStage == 1)
            {
                _keys.Release(Key.Space);
                _airJumpStage = 2;
            }
            else if (_airJumpStage == 2)
            {
                _keys.Hold(Key.Space);
                _jumpReleaseAt = Time.time + 0.3f;
                _airJumpStage = 0;
            }
            else if (_keys.IsHeld(Key.Space) && Time.time > _jumpReleaseAt)
            {
                _keys.Release(Key.Space);
            }

            _keys.Flush();
        }

        // ---------------------------------------------------------------- campaign

        private IEnumerator Run()
        {
            yield return WaitScene(SceneNames.MainMenu, 60f);
            yield return new WaitForSecondsRealtime(2f);
            string single = Value(LevelArg);
            bool finished;
            if (single != null)
            {
                yield return SingleLevel(single);
                finished = _report.Levels.Count > 0 && _report.Levels[0].Completed;
            }
            else
            {
                var outcome = new Outcome();
                yield return Campaign(outcome);
                finished = outcome.Result == Result.Done;
            }

            _keys.ReleaseAll();
            _keys.Flush();
            _report.Write(finished, single == null);
            Debug.Log($"[Playthrough] Report written to {_report.Folder}");
            yield return new WaitForSecondsRealtime(1f);
            Application.Quit();
        }

        private IEnumerator SingleLevel(string scene)
        {
            if (!ServiceLocator.TryGet(out SessionService session) || !ServiceLocator.TryGet(out GameManager game)) yield break;
            session.ResetSession();
            game.LoadScene(scene);
            yield return WaitScene(scene, 60f);
            yield return PlayLevel(scene, LevelName(session, scene));
        }

        private IEnumerator Campaign(Outcome outcome)
        {
            Shot("meniu-principal");
            ClickButton(FindFirstObjectByType<MainMenuView>(), "NewGame");
            yield return WaitScene(SceneNames.Intro, 30f);
            yield return new WaitForSecondsRealtime(4f);
            Shot("intro");
            yield return WaitScene(SceneNames.Hub, 40f, tapToSkip: true);
            if (_loadedScene != SceneNames.Hub)
            {
                _report.Note("Intro-ul nu a dus în ascunzătoare.");
                yield break;
            }

            if (!ServiceLocator.TryGet(out SessionService session)) yield break;
            LevelDefinition[] levels = session.Database.Levels.Where(l => l != null && l.Order is >= 1 and <= 6).OrderBy(l => l.Order).ToArray();
            for (int i = 0; i < levels.Length; i++)
            {
                LevelDefinition level = levels[i];
                yield return HubVisit(level, i == 0);
                if (_loadedScene != level.SceneName)
                {
                    _report.Note($"Călătoria spre „{level.DisplayName}” nu a pornit; încarc nivelul direct.");
                    if (ServiceLocator.TryGet(out GameManager game)) game.LoadScene(level.SceneName);
                    yield return WaitScene(level.SceneName, 40f);
                }

                yield return PlayLevel(level.SceneName, level.DisplayName);
                yield return LevelComplete(level, i == levels.Length - 1);
            }

            yield return WaitScene(SceneNames.Credits, 90f, tapToSkip: false);
            yield return new WaitForSecondsRealtime(2f);
            Shot("credite");
            if (_loadedScene == SceneNames.Credits) outcome.Done();
            else _report.Note("Finalul nu a dus la credite.");
        }

        private IEnumerator HubVisit(LevelDefinition level, bool first)
        {
            yield return WaitScene(SceneNames.Hub, 40f);
            yield return WaitPlayer();
            yield return WaitPlaying(40f);
            Shot($"hub-L{level.Order}");

            if (first)
            {
                JobBoard board = FindFirstObjectByType<JobBoard>();
                if (board != null)
                {
                    yield return UseHubConsole(board.transform.position.x);
                    TerminalScreenView terminal = FindFirstObjectByType<TerminalScreenView>(FindObjectsInactive.Include);
                    if (terminal != null && terminal.IsVisible)
                    {
                        Shot("hub-contracte");
                        Button entry = Entries(terminal).FirstOrDefault();
                        if (entry != null)
                        {
                            entry.onClick.Invoke();
                            yield return null;
                            ClickButton(terminal, "Action");
                            yield return new WaitForSecondsRealtime(0.5f);
                            Shot("hub-contract-acceptat");
                        }

                        _keys.Tap(Key.Escape);
                        yield return WaitPlaying(5f);
                    }
                    else _report.Note("Panoul de contracte nu s-a deschis.");
                }
            }

            FastTravelTerminal travel = FindObjectsByType<FastTravelTerminal>(FindObjectsSortMode.None).FirstOrDefault(t => t.name == "FastTravel_Levels");
            if (travel == null)
            {
                _report.Note("Nu există terminalul de călătorie în ascunzătoare.");
                yield break;
            }

            yield return UseHubConsole(travel.transform.position.x);
            TerminalScreenView view = FindFirstObjectByType<TerminalScreenView>(FindObjectsInactive.Include);
            if (view == null || !view.IsVisible)
            {
                _report.Note("Terminalul de călătorie nu s-a deschis.");
                yield break;
            }

            Shot($"hub-calatorie-L{level.Order}");
            Button destination = Entries(view).FirstOrDefault(b => Label(b).StartsWith($"{level.Order}.", StringComparison.Ordinal));
            if (destination == null)
            {
                _report.Note($"„{level.DisplayName}” lipsește din tab-ul Călătorie.");
                _keys.Tap(Key.Escape);
                yield break;
            }

            destination.onClick.Invoke();
            yield return null;
            ClickButton(view, "Action");
            yield return WaitScene(level.SceneName, 40f);
        }

        private IEnumerator UseHubConsole(float x)
        {
            var walk = new Outcome();
            yield return WalkTo(new Vector2(x, _player != null ? _player.Position.y : 0f), 20f, walk);
            Move(0f);
            yield return WaitFor(() => _promptVisible, 2f);
            _keys.Tap(Key.E);
            yield return new WaitForSecondsRealtime(0.8f);
        }

        private IEnumerator LevelComplete(LevelDefinition level, bool last)
        {
            LevelCompleteView view = FindFirstObjectByType<LevelCompleteView>(FindObjectsInactive.Include);
            yield return WaitFor(() => view != null && view.IsVisible, 10f);
            if (view == null || !view.IsVisible)
            {
                _report.Note($"Ecranul „Misiune îndeplinită” nu a apărut după „{level.DisplayName}”.");
                yield break;
            }

            yield return new WaitForSecondsRealtime(1.2f);
            Shot($"L{level.Order}-misiune-indeplinita");
            ClickButton(view, "Continue");
            if (!last) yield return WaitScene(SceneNames.Hub, 40f);
        }

        // ---------------------------------------------------------------- level

        private IEnumerator PlayLevel(string scene, string name)
        {
            yield return WaitPlayer();
            LevelRoute route = FindFirstObjectByType<LevelRoute>();
            LevelRun run = _report.Begin(scene, name);
            _levelLoadedAt = Time.unscaledTime;
            if (route == null || route.Steps.Length == 0)
            {
                _report.Note("Nivelul nu are traseu (LevelRoute).");
                _report.End();
                yield break;
            }

            ServiceLocator.TryGet(out SessionService session);
            int logsAtStart = session != null ? session.Session.Lore.Count : 0;
            RouteStep[] steps = route.Steps;
            run.Steps = steps.Length;
            _levelDone = false;
            _resumeIndex = 0;
            int lastDeathStep = -1;
            int deathsHere = 0;
            int i = 0;
            while (i < steps.Length && !_levelDone)
            {
                _stepIndex = i;
                _step = steps[i];
                if (_step == null)
                {
                    i++;
                    continue;
                }

                if (_step.Screenshot) Shot($"{Short(scene)}-{i:D3}-{Ascii(_step.Segment)}");
                _died = false;
                _fell = false;
                var outcome = new Outcome();
                yield return Execute(_step, outcome);
                Move(0f);
                _keys.Release(Key.LeftCtrl);
                _keys.Release(Key.W);
                _keys.Release(Key.S);

                if (outcome.Result == Result.Done)
                {
                    run.StepsDone++;
                    i++;
                    continue;
                }

                if (outcome.Result == Result.Died)
                {
                    run.Deaths++;
                    _report.Note($"Mort la pasul {i} ({_step.Segment}, {_step.Action}).");
                    yield return WaitRespawn();
                    deathsHere = i == lastDeathStep ? deathsHere + 1 : 1;
                    lastDeathStep = i;
                    if (deathsHere >= 3)
                    {
                        yield return Assist(_step, "3 morți la rând în același loc");
                        i++;
                        continue;
                    }

                    i = Mathf.Min(_resumeIndex, i);
                    continue;
                }

                yield return Assist(_step, outcome.Reason);
                i++;
            }

            if (!_levelDone)
            {
                yield return WaitFor(() => _levelDone, 6f);
                if (!_levelDone) _report.Note("Ieșirea din nivel nu a fost atinsă la capătul traseului.");
            }

            run.Completed = _levelDone;
            if (session != null) run.LogsFound = session.Session.Lore.Count - logsAtStart;
            _report.End();
        }

        private IEnumerator Execute(RouteStep step, Outcome outcome)
        {
            switch (step.Action)
            {
                case RouteAction.Jump: return JumpTo(step.Position, outcome);
                case RouteAction.WallClimb: return WallClimb(step.Position, outcome);
                case RouteAction.Crawl: return WalkTo(step.Position, 15f, outcome, crawl: true);
                case RouteAction.Interact: return InteractAt(step, outcome);
                case RouteAction.Ride: return Ride(step, outcome);
                case RouteAction.Swim: return SwimTo(step.Position, outcome);
                case RouteAction.Updraft: return UpdraftTo(step, outcome);
                case RouteAction.Hold: return HoldAt(step, outcome);
                default: return WalkTo(step.Position, 25f, outcome);
            }
        }

        /// <summary>Cannot pass: note it, photograph it, teleport to the waypoint and go on.</summary>
        private IEnumerator Assist(RouteStep step, string reason)
        {
            string shot = Shot($"{Short(_report.Current?.Scene)}-{_stepIndex:D3}-PROBLEMA");
            _report.Note($"**Blocat** la pasul {_stepIndex} ({step.Segment}, {step.Action}): {reason}. {Diagnostics(step)} Mutat mai departe. Captură: {shot}");
            DumpTrace(step);
            yield return null;
            yield return new WaitForEndOfFrame();
            if (_player != null && _player.IsAlive)
            {
                _player.Respawn(step.Position + Vector2.up * 0.3f);
                _fell = false;
            }
        }

        // ---------------------------------------------------------------- moves

        private bool Interrupted(Outcome outcome)
        {
            if (_levelDone)
            {
                outcome.Done();
                return true;
            }

            if (_died || _player == null || !_player.IsAlive)
            {
                outcome.Died();
                return true;
            }

            return false;
        }

        private IEnumerator WalkTo(Vector2 target, float timeout, Outcome outcome, bool crawl = false)
        {
            float until = Time.time + timeout;
            float lastX = _player != null ? _player.Position.x : 0f;
            float stuckSince = Time.time;
            bool airJumpUsed = false;
            int falls = 0;
            while (true)
            {
                if (Interrupted(outcome)) yield break;
                if (_fell)
                {
                    _fell = false;
                    if (++falls >= 3)
                    {
                        outcome.Fail("căzut de 3 ori");
                        yield break;
                    }
                }

                Vector2 p = _player.Position;
                float dx = target.x - p.x;
                bool settled = _player.IsGrounded || _player.InWater;
                if (Mathf.Abs(dx) < 0.35f && (settled || crawl))
                {
                    Move(0f);
                    outcome.Done();
                    yield break;
                }

                Move(Mathf.Sign(dx));
                if (crawl) _keys.Hold(Key.LeftCtrl);
                if (Mathf.Abs(p.x - lastX) > 0.15f)
                {
                    lastX = p.x;
                    stuckSince = Time.time;
                }

                bool blocked = Time.time - stuckSince > 0.3f;
                if (!crawl && _player.IsGrounded && (blocked || (target.y > p.y + 0.6f && Mathf.Abs(dx) < 2.2f)))
                {
                    StartJump(0.4f);
                    airJumpUsed = false;
                    stuckSince = Time.time;
                }
                else if (!crawl && !_player.IsGrounded && !_player.InWater && !airJumpUsed && _player.Velocity.y < 1.5f && (blocked || target.y > p.y + 0.4f))
                {
                    AirJump();
                    airJumpUsed = true;
                }

                if (Time.time > until)
                {
                    outcome.Fail(crawl ? "nu a trecut prin tubul de ventilație" : "nu a ajuns la punct (blocat)");
                    yield break;
                }

                yield return null;
            }
        }

        private IEnumerator JumpTo(Vector2 target, Outcome outcome)
        {
            Vector2 takeoff = _player.Position;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                yield return WaitFor(() => _player.IsGrounded || _died, 3f);
                if (Interrupted(outcome)) yield break;
                float dx0 = target.x - _player.Position.x;
                bool runUp = Mathf.Abs(dx0) > 2.5f || target.y <= _player.Position.y + 0.5f;
                StartJump(0.45f);
                bool left = false;
                bool airJumped = false;
                bool dashed = false;
                bool landedWrong = false;
                float until = Time.time + 4f;
                while (Time.time < until)
                {
                    if (Interrupted(outcome)) yield break;
                    if (_fell)
                    {
                        _fell = false;
                        break;
                    }

                    Vector2 p = _player.Position;
                    float dx = target.x - p.x;
                    if (!_player.IsGrounded) left = true;
                    bool high = p.y > target.y - 0.35f;
                    float dir = Mathf.Abs(dx) < 0.25f ? 0f : Mathf.Sign(dx);
                    Move(runUp || high ? dir : 0f);
                    if (left && !airJumped && _player.Velocity.y < 2f && (p.y < target.y + 0.6f || Mathf.Abs(dx) > 2.2f))
                    {
                        AirJump();
                        airJumped = true;
                    }

                    if (left && airJumped && !dashed && _player.Velocity.y < -1f && Mathf.Abs(dx) > 2.2f && p.y > target.y - 0.6f)
                    {
                        _keys.Tap(Key.LeftShift);
                        dashed = true;
                    }

                    if (left && _player.IsGrounded)
                    {
                        if (p.y > target.y - 0.6f && p.y < target.y + 1.6f && Mathf.Abs(dx) < 3f)
                        {
                            Move(0f);
                            outcome.Done();
                            yield break;
                        }

                        landedWrong = true;
                        break;
                    }

                    yield return null;
                }

                if (landedWrong) _report.Note($"Salt ratat la pasul {_stepIndex} ({_step.Segment}): aterizat la ({_player.Position.x:0.0}, {_player.Position.y:0.0}), ținta ({target.x:0.0}, {target.y:0.0}).");
                var back = new Outcome();
                yield return WalkTo(takeoff, 8f, back);
                if (back.Result == Result.Died)
                {
                    outcome.Died();
                    yield break;
                }
            }

            outcome.Fail("saltul nu a reușit de 3 ori");
        }

        private IEnumerator WallClimb(Vector2 top, Outcome outcome)
        {
            float until = Time.time + 20f;
            float hold = 1f;
            float nextJumpAt = 0f;
            while (Time.time < until)
            {
                if (Interrupted(outcome)) yield break;
                Vector2 p = _player.Position;
                if (_player.IsGrounded && Mathf.Abs(p.y - top.y) < 0.6f && Mathf.Abs(top.x - p.x) < 2f)
                {
                    Move(0f);
                    outcome.Done();
                    yield break;
                }

                if (p.y > top.y + 0.3f)
                {
                    Move(Mathf.Sign(top.x - p.x));
                }
                else if (_player.StateId == PlayerStateId.WallSlide)
                {
                    hold = _player.Facing;
                    if (Time.time >= nextJumpAt)
                    {
                        StartJump(0.3f);
                        nextJumpAt = Time.time + 0.2f;
                    }

                    Move(hold);
                }
                else if (_player.IsGrounded)
                {
                    hold = 1f;
                    Move(hold);
                    if (Time.time >= nextJumpAt)
                    {
                        StartJump(0.45f);
                        nextJumpAt = Time.time + 0.3f;
                    }
                }
                else
                {
                    Move(hold);
                }

                yield return null;
            }

            outcome.Fail("nu a urcat hornul prin wall-jump");
        }

        private IEnumerator InteractAt(RouteStep step, Outcome outcome)
        {
            if (step.Subject is Terminal hacked && hacked.IsHacked)
            {
                outcome.Done();
                yield break;
            }

            var approach = new Outcome();
            yield return WalkTo(step.Position, 25f, approach);
            if (approach.Result != Result.Done)
            {
                if (approach.Result == Result.Died) outcome.Died();
                else outcome.Fail(approach.Reason);
                yield break;
            }

            for (int attempt = 0; attempt < 3; attempt++)
            {
                Move(0f);
                yield return WaitFor(() => _promptVisible, 2f);
                if (!_promptVisible) _report.Note($"Fără prompt de interacțiune la pasul {_stepIndex} ({step.Segment}). {Diagnostics(step)}");
                _keys.Tap(Key.E);
                switch (step.Subject)
                {
                    case Terminal terminal:
                        yield return WaitFor(() => CurrentState == GameState.Hacking, 2f);
                        yield return WaitFor(() => CurrentState != GameState.Hacking, 90f);
                        yield return new WaitForSecondsRealtime(0.4f);
                        if (terminal.IsHacked)
                        {
                            outcome.Done();
                            yield break;
                        }

                        break;
                    case NetworkNode node:
                        Vector2 from = node.transform.position;
                        yield return WaitFor(() => _player != null && Vector2.Distance(_player.Position, from) > 3f, 3f);
                        if (_player != null && Vector2.Distance(_player.Position, from) > 3f)
                        {
                            outcome.Done();
                            yield break;
                        }

                        break;
                    case Elevator elevator:
                        yield return WaitFor(() => elevator.IsMoving || elevator.IsAtB, 2f);
                        if (elevator.IsMoving || elevator.IsAtB)
                        {
                            outcome.Done();
                            yield break;
                        }

                        break;
                    default:
                        yield return new WaitForSecondsRealtime(0.5f);
                        outcome.Done();
                        yield break;
                }

                if (Interrupted(outcome)) yield break;
            }

            outcome.Fail(step.Subject is Terminal ? $"spargerea ({_solver.LastMinigame}) nu a reușit" : "interacțiunea nu a reușit");
        }

        private IEnumerator Ride(RouteStep step, Outcome outcome)
        {
            if (step.Subject is Elevator elevator)
            {
                Move(0f);
                yield return WaitFor(() => elevator.IsAtB || _died, 25f);
                if (Interrupted(outcome)) yield break;
                if (elevator.IsAtB) outcome.Done();
                else outcome.Fail("liftul nu a urcat");
                yield break;
            }

            if (step.Subject is not MovingPlatform gondola)
            {
                yield return WalkTo(step.Position, 20f, outcome);
                yield break;
            }

            for (int attempt = 0; attempt < 3; attempt++)
            {
                Move(0f);
                yield return WaitFor(() => Mathf.Abs(gondola.transform.position.x - gondola.PointA.x) < 0.2f || _died, 30f);
                if (Interrupted(outcome)) yield break;
                float boardUntil = Time.time + 3f;
                while (Mathf.Abs(_player.Position.x - gondola.transform.position.x) > 0.4f && Time.time < boardUntil && !_fell)
                {
                    if (Interrupted(outcome)) yield break;
                    Move(Mathf.Sign(gondola.transform.position.x - _player.Position.x));
                    yield return null;
                }

                Move(0f);
                float rideUntil = Time.time + 20f;
                while (Mathf.Abs(gondola.transform.position.x - gondola.PointB.x) > 0.2f && Time.time < rideUntil && !_fell)
                {
                    if (Interrupted(outcome)) yield break;
                    Move(0f);
                    yield return null;
                }

                if (_fell)
                {
                    _fell = false;
                    _report.Note($"Căzut de pe nacelă la pasul {_stepIndex} ({step.Segment}).");
                    continue;
                }

                var off = new Outcome();
                yield return WalkTo(step.Position, 8f, off);
                if (off.Result == Result.Done)
                {
                    outcome.Done();
                    yield break;
                }

                if (off.Result == Result.Died)
                {
                    outcome.Died();
                    yield break;
                }
            }

            outcome.Fail("traversarea cu nacela nu a reușit");
        }

        private IEnumerator SwimTo(Vector2 target, Outcome outcome)
        {
            float until = Time.time + 75f;
            float lastJump = 0f;
            bool airJumped = false;
            while (Time.time < until)
            {
                if (Interrupted(outcome)) yield break;
                _fell = false;
                Vector2 p = _player.Position;
                float dx = target.x - p.x;
                Vertical(0);
                if (!_player.InWater)
                {
                    if (_player.IsGrounded && Mathf.Abs(dx) < 1.2f && Mathf.Abs(p.y - target.y) < 0.7f)
                    {
                        Move(0f);
                        outcome.Done();
                        yield break;
                    }

                    Move(Mathf.Sign(dx));
                    if (_player.IsGrounded) airJumped = false;
                    else if (!airJumped && p.y < target.y && _player.Velocity.y < 1f)
                    {
                        AirJump();
                        airJumped = true;
                    }
                }
                else
                {
                    bool under = target.y < _player.WaterSurfaceY - 0.4f;
                    Move(Mathf.Abs(dx) < 0.3f ? 0f : Mathf.Sign(dx));
                    if (under)
                    {
                        Vertical(p.y > target.y + 0.3f ? -1 : 0);
                        if (Mathf.Abs(dx) < 0.8f && Mathf.Abs(p.y - target.y) < 1.1f)
                        {
                            outcome.Done();
                            yield break;
                        }
                    }
                    else
                    {
                        Vertical(1);
                        if (p.y > _player.WaterSurfaceY - 0.9f && Mathf.Abs(dx) < 3.5f && Time.time - lastJump > 0.5f)
                        {
                            _keys.Tap(Key.Space);
                            lastJump = Time.time;
                            airJumped = false;
                        }
                    }
                }

                yield return null;
            }

            outcome.Fail("nu a ieșit din apă la punct");
        }

        private IEnumerator UpdraftTo(RouteStep step, Outcome outcome)
        {
            float fanX = step.Subject != null ? step.Subject.transform.position.x : step.Position.x - 3f;
            Vector2 target = step.Position;
            float until = Time.time + 40f;
            while (Time.time < until)
            {
                if (Interrupted(outcome)) yield break;
                Vector2 p = _player.Position;
                if (_player.IsGrounded && Mathf.Abs(target.x - p.x) < 2f && Mathf.Abs(p.y - target.y) < 0.6f)
                {
                    Move(0f);
                    outcome.Done();
                    yield break;
                }

                if (p.y > target.y + 0.6f) Move(Mathf.Sign(target.x - p.x));
                else Move(Mathf.Abs(fanX - p.x) > 0.25f ? Mathf.Sign(fanX - p.x) : 0f);
                yield return null;
            }

            outcome.Fail("curentul de aer nu l-a urcat pe stâncă");
        }

        private IEnumerator HoldAt(RouteStep step, Outcome outcome)
        {
            var station = step.Subject as UplinkStation;
            float until = Time.time + 120f;
            while (Time.time < until)
            {
                if (Interrupted(outcome)) yield break;
                if (station == null || station.Transfer.Completed)
                {
                    Move(0f);
                    outcome.Done();
                    yield break;
                }

                float dx = step.Position.x - _player.Position.x;
                Move(Mathf.Abs(dx) > 1.5f ? Mathf.Sign(dx) : 0f);
                yield return null;
            }

            outcome.Fail("transmisia nu a ajuns la 100%");
        }

        private void Move(float direction)
        {
            _keys.Release(Key.A);
            _keys.Release(Key.D);
            if (direction > 0.1f) _keys.Hold(Key.D);
            else if (direction < -0.1f) _keys.Hold(Key.A);
        }

        private void Vertical(int direction)
        {
            _keys.Release(Key.W);
            _keys.Release(Key.S);
            if (direction > 0) _keys.Hold(Key.W);
            else if (direction < 0) _keys.Hold(Key.S);
        }

        private void StartJump(float holdSeconds)
        {
            _keys.Hold(Key.Space);
            _jumpReleaseAt = Time.time + holdSeconds;
        }

        /// <summary>A second press needs the key released first: release this frame, press the next.</summary>
        private void AirJump()
        {
            if (_airJumpStage == 0) _airJumpStage = 1;
        }

        // ---------------------------------------------------------------- waits

        private static GameState CurrentState => ServiceLocator.TryGet(out GameManager game) ? game.State : GameState.Boot;

        private IEnumerator WaitFor(Func<bool> condition, float seconds)
        {
            float until = Time.unscaledTime + seconds;
            while (!condition() && Time.unscaledTime < until) yield return null;
        }

        private IEnumerator WaitScene(string scene, float seconds, bool tapToSkip = false)
        {
            float until = Time.unscaledTime + seconds;
            float nextTap = Time.unscaledTime + 1f;
            while (Time.unscaledTime < until)
            {
                bool loading = ServiceLocator.TryGet(out SceneLoader loader) && loader.IsLoading;
                if (!loading && SceneManager.GetActiveScene().name == scene)
                {
                    _loadedScene = scene;
                    yield break;
                }

                if (tapToSkip && Time.unscaledTime >= nextTap)
                {
                    _keys.Tap(Key.Enter);
                    nextTap = Time.unscaledTime + 1.5f;
                }

                yield return null;
            }

            _loadedScene = SceneManager.GetActiveScene().name;
        }

        private IEnumerator WaitPlayer()
        {
            float until = Time.unscaledTime + 10f;
            _player = null;
            while (_player == null && Time.unscaledTime < until)
            {
                if (ServiceLocator.TryGet(out IPlayerTarget target) && target.Transform != null)
                    _player = target.Transform.GetComponent<PlayerController>();
                yield return null;
            }

            _lastHealth = -1;
            yield return new WaitForSecondsRealtime(0.5f);
        }

        private IEnumerator WaitPlaying(float seconds) => WaitFor(() => CurrentState == GameState.Playing, seconds);

        private IEnumerator WaitRespawn()
        {
            yield return WaitFor(() => !_died, 15f);
            yield return WaitPlayer();
            yield return WaitPlaying(10f);
            _died = false;
            _fell = false;
        }

        // ---------------------------------------------------------------- events

        private void OnDied(PlayerDied _) => _died = true;

        private void OnRespawned(PlayerRespawned _)
        {
            if (_died)
            {
                _died = false;
            }
            else
            {
                _fell = true;
                LevelRun run = _report.Current;
                if (run != null)
                {
                    run.Falls++;
                    // A pit fall costs a hit point too; report it as a fall, not as a hit.
                    if (_lastHitFrame == Time.frameCount && run.Hits > 0)
                    {
                        run.Hits--;
                        if (_lastHitSegment != null && run.HitsBySegment.TryGetValue(_lastHitSegment, out int n))
                        {
                            if (n <= 1) run.HitsBySegment.Remove(_lastHitSegment);
                            else run.HitsBySegment[_lastHitSegment] = n - 1;
                        }
                    }
                }
            }

            _lastHealth = -1;
        }

        private void OnHealth(PlayerHealthChanged evt)
        {
            if (_lastHealth >= 0 && evt.Current < _lastHealth) CountHit();
            _lastHealth = evt.Current;
        }

        private void CountHit()
        {
            LevelRun run = _report.Current;
            if (run == null) return;
            run.Hits++;
            string segment = _step != null ? _step.Segment : "?";
            run.HitsBySegment[segment] = run.HitsBySegment.TryGetValue(segment, out int n) ? n + 1 : 1;
            _lastHitFrame = Time.frameCount;
            _lastHitSegment = segment;
        }

        private void OnCheckpoint(CheckpointReached _) => _resumeIndex = _stepIndex;

        private void OnLevelCompleted(LevelCompleted _) => _levelDone = true;

        private void OnHackStarted(HackSessionStarted _) => _solver.Reset();

        private void OnHackEnded(HackSessionEnded evt)
        {
            LevelRun run = _report.Current;
            if (run == null) return;
            run.Hacks++;
            if (!evt.Success)
            {
                run.HacksFailed++;
                _report.Note($"Spargere eșuată ({_solver.LastMinigame}) la pasul {_stepIndex}.");
            }
        }

        private void OnTraceLevel(TraceLevelChanged evt)
        {
            if (_report.Current != null && evt.Current >= TraceLevel.Alert && evt.Previous < TraceLevel.Alert)
            {
                _report.Current.Alarms++;
                _report.Note($"Alarmă la pasul {_stepIndex} ({_step?.Segment}).");
            }
        }

        private void OnPrompt(InteractionPromptChanged evt) => _promptVisible = evt.Visible;

        private void OnSceneLoaded(SceneLoadCompleted evt)
        {
            _loadedScene = evt.SceneName;
            _overlay = null;
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (message.Contains(IgnoredHitLog))
            {
                CountHit();
                return;
            }

            if (type is LogType.Error or LogType.Exception or LogType.Assert) _report.Error(message);
        }

        // ---------------------------------------------------------------- diagnostics

        private string Diagnostics(RouteStep step)
        {
            if (_player == null) return "(fără jucător)";
            var interactor = _player.GetComponent<PlayerInteractor>();
            Collider2D[] near = Physics2D.OverlapCircleAll(_player.transform.position, 1.5f, Layers.InteractableMask);
            string nearby = near.Length == 0 ? "nimic" : string.Join(", ", near.Select(c => $"{c.name}@{c.transform.position.x:0.0}"));
            return $"[jucător ({_player.Position.x:0.0}, {_player.Position.y:0.0}), stare {_player.StateId}, sol {_player.IsGrounded}, joc {CurrentState}, " +
                   $"ținta ({step.Position.x:0.0}, {step.Position.y:0.0}), interactiv curent: {(interactor != null && interactor.Current != null ? interactor.Current.Prompt : "niciunul")}, " +
                   $"interactive la 1.5 m: {nearby}]";
        }

        private void DumpTrace(RouteStep step)
        {
            var text = new StringBuilder($"[Playthrough] Trace before step {_stepIndex} ({step.Segment}, {step.Action}):\n");
            foreach (string line in _trace) text.AppendLine(line);
            Debug.Log(text.ToString());
            _trace.Clear();
        }

        private string Keys()
        {
            var keys = new StringBuilder();
            foreach (Key key in new[] { Key.A, Key.D, Key.W, Key.S, Key.Space, Key.LeftShift, Key.LeftCtrl, Key.E })
                if (_keys.IsHeld(key)) keys.Append(key).Append(' ');
            if (_airJumpStage > 0) keys.Append("air").Append(_airJumpStage);
            return keys.ToString();
        }

        // ---------------------------------------------------------------- helpers

        private string Shot(string name)
        {
            string file = $"{++_shotIndex:D3}-{Ascii(name)}.png";
            ScreenCapture.CaptureScreenshot(Path.Combine(_report.Folder, file));
            _report.Current?.Shots.Add(file);
            return file;
        }

        private static void ClickButton(Component root, string name)
        {
            if (root == null) return;
            Button button = root.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == name);
            if (button != null && button.interactable) button.onClick.Invoke();
        }

        private static Button[] Entries(TerminalScreenView view) =>
            view.GetComponentsInChildren<Button>(false)
                .Where(b => b.transform.parent != null && b.transform.parent.name == "List" && b.name != "EntryTemplate").ToArray();

        private static string Label(Button button)
        {
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            return text != null ? text.text : string.Empty;
        }

        private static string LevelName(SessionService session, string scene) =>
            session.Database.Levels.FirstOrDefault(l => l != null && l.SceneName == scene)?.DisplayName ?? scene;

        private static string Short(string scene) => string.IsNullOrEmpty(scene) ? "x" : scene.Split('_')[0];

        private static string Ascii(string text)
        {
            if (string.IsNullOrEmpty(text)) return "x";
            string normalized = text.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            foreach (char c in normalized)
            {
                if (char.IsLetterOrDigit(c) && c < 128) builder.Append(char.ToLowerInvariant(c));
                else if (c == ' ' || c == '-' || c == '_') builder.Append('-');
            }

            return builder.Length > 0 ? builder.ToString() : "x";
        }
    }
}
