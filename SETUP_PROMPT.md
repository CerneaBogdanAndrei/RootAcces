# Root Access — Unity project setup (Claude Code task)

You are setting up the complete development environment and a playable vertical-slice skeleton for a graded university Unity game. Work autonomously through the phases below, verify each phase before moving on, track progress with your todo list, and stop only for decisions that cannot be undone or for missing software I must install myself.

---

## 0. Execution rules (read first)

- OS is Windows. Detect the shell (PowerShell or Git Bash) and use matching syntax. Prefer PowerShell scripts for anything I will rerun.
- Never hand-write Unity YAML assets (`.unity`, `.prefab`, `.asset`, `.controller`, `.mat`, `.meta`). Create them through Editor scripts executed in batchmode (`-executeMethod`). Hand-written C#, `.asmdef`, `.inputactions`, `manifest.json`, docs and scripts are fine.
- Batchmode invocation pattern: `"<UnityEditorPath>\Unity.exe" -batchmode -nographics -quit -projectPath "<path>" -logFile "<path>\Logs\<step>.log" [-executeMethod Namespace.Class.Method]`. After every run, parse the log for `error CS`, `Exception`, `Compilation failed` and exit code; fix and rerun until clean. The project must not be open in the Editor during batchmode runs; if `Temp/UnityLockfile` exists, ask me to close Unity.
- Target Unity 6 APIs: `Rigidbody2D.linearVelocity` (not `velocity`), `FindFirstObjectByType`/`FindObjectsByType`, `Object.InstantiateAsync` where useful, new Input System only.
- Code style: C# 9+, `[SerializeField] private` fields, no public mutable fields, no magic strings (constants/enums), guard clauses with clear log messages, no `FindObjectOfType` in gameplay code (use EventBus / ServiceLocator / serialized references). No inline comments unless the logic is non-obvious; XML doc comments on public APIs are fine.
- Every phase ends green: zero compile errors, tests pass, scenes open. Commit after each phase with a conventional commit message.
- Do not download assets from the internet. Generate placeholder art procedurally; I will import real packs into `Assets/ThirdParty` later.
- Ask before deleting or modifying anything outside the project folder.

---

## 1. Context

- Developer: experienced full-stack dev (TypeScript, Python, Java, C#-capable), new-ish to Unity. Explain Unity-specific decisions briefly in docs, not in chat.
- Machine: Lenovo ThinkPad, Intel i5, Intel Iris Xe (integrated GPU), 16 GB RAM. Performance target: stable 60 FPS at 1080p. URP 2D, no heavy post-processing (Bloom only, low quality), pooled objects, no per-frame allocations in hot paths.
- Course topic: "Joc 2D de tip singleplayer/multiplayer în Unity cu C#".
- Course requirements: complex and complete game, minimum 30 minutes of gameplay to finish; start menu (settings, play etc.); in-game GUI (health, score, inventory etc.); keyboard + mouse input (gamepad too); animated characters and enemies; scripted objects and collisions; advanced mechanics (object interaction, inventory, skill tree, quests, pathfinding). Assets/packages may be imported, but all programming must be ours. Deliverables: full project uploaded to Google Drive + published Windows executable.

---

## 2. Game concept — "Root Access"

2D side-scrolling stealth-platformer in a cyberpunk city. The player is a hacker who earns a living by infiltrating buildings, hacking terminals and stealing data.

- **Structure (≥30 min):** Hub (hideout with a "darknet job board", shop, skill terminal, fast-travel) + 5 levels of ~6–8 min each:
  1. Rooftops & Park (tutorial; trees/grass, skybox)
  2. Office Tower (cameras, guards, first hacking minigames)
  3. Server Farm (drones with pathfinding, lasers, network-node teleports)
  4. Sewers (water level: swimming, buoyancy, flooding puzzle)
  5. Corporate HQ (finale, all mechanics combined, finale cutscene)
- **Player:** run, variable-height jump, coyote time, jump buffer, double jump (skill unlock), dash, wall-slide/wall-jump, crouch, hide in shadow zones, swim, interact, throw EMP grenade.
- **Hacking:** terminals open a hacking overlay with 3 minigames (wire-connect puzzle, pattern/sequence memory, port-scan timing). Hacked terminals control doors, cameras, turrets, lights, elevators, network nodes.
- **Enemies:** Guards (state machine: Patrol → Suspicious → Chase → Search → Return), Security Cameras (vision cone via raycasts, trigger alarm), Drones (custom A* grid pathfinding we write ourselves, no plugin), Turrets.
- **Systems:** Health; Trace meter (alert level, triggers lockdown at 100%); Credits economy; Inventory (USB keys, exploits, EMP grenades, data shards; stackable, ScriptableObject item definitions); Skill tree (3 branches: Stealth / Hacking / Hardware, XP-based, prerequisites); Quest system (main story + side contracts, objectives: reach, hack, collect, avoid detection; ScriptableObject-driven); Dialogue box (simple, data-driven); Checkpoints; Save/Load (JSON in `Application.persistentDataPath`, versioned save format).
- **Teleportation:** hacked Network Nodes teleport the player between linked nodes (dissolve out → particle trail → dissolve in); Hub fast-travel to unlocked levels.
- **Bonus:** Timeline cutscenes (intro + finale, skippable); online 2-player asymmetric co-op (P1 Infiltrator = platforming, P2 Operator = map view, opens doors, loops cameras) via Netcode for GameObjects + Unity Relay join code — implemented LAST; this session only installs packages and writes interfaces/stubs.
- **UI language:** Romanian, all strings centralized in one place (e.g. a `Strings` ScriptableObject or static class) so they can be changed or localized later.

---

## 3. Grading rubric — target ALL 12, each undeniable and demonstrable

Mandatory items (must be perfect):

1. **Movement / jump** — our Rigidbody2D controller: run with acceleration/deceleration, variable-height jump, coyote time, jump buffer, double jump, dash, wall-slide/wall-jump, crouch, swim.
2. **Material / texture** — custom materials per surface type (metal, glass, neon emissive, concrete); at least 3 custom Shader Graph materials (hologram, glitch/dissolve for hacked objects and teleport, emissive neon pulse); normal-mapped sprites reacting to 2D lights.
5. **Light / camera** — URP 2D Global, Point, Spot and Freeform lights; scripted flickering neon; Shadow Caster 2D; per-level color grading/tint; Cinemachine follow camera with confiner, look-ahead, zoom on hacking, impulse screen shake, blends between rooms.
6. **Prefabs** — every reusable object is a prefab; prefab variants (Guard → Guard_Heavy / Guard_Sniper; Terminal → Terminal_Door / Terminal_Camera / Terminal_Data); levels assembled from prefabs; runtime spawning through object pools.
8. **Menu** — Main menu (Continue, New Game, Settings, Rubric Showcase, Credits, Quit), Settings (master/music/SFX/UI volume, resolution, fullscreen, VSync, key rebinding via Input System rebinding UI), Pause menu, Game Over, Level Complete, Loading screen with async progress bar.
10. **Animations / Animator** — Animator Controllers with parameters and blend trees for the player (idle/run/jump/fall/dash/wall-slide/swim/hack/hurt/death), guards, drones, cameras, doors, elevators; Animation Events drive footstep sounds and hit frames; animated UI transitions.
11. **Collisions / triggers** — configured layer collision matrix; `OnCollisionEnter2D` (damage, landing impact), `OnTriggerEnter2D/Exit2D` (checkpoints, pickups, hide zones, alarm zones, level exits, water entry, dialogue zones, teleport pads); raycasts for vision cones and ground/wall checks.
12. **Script** — all gameplay systems written by us, clean architecture with assembly definitions, EditMode + PlayMode tests for core logic (A*, inventory, quests, skill tree, save/load, trace meter).

Optional items — implement ALL for extra points:

3. **Environment: trees / grass** — Park/rooftop garden with trees rigged with 2D Animation bones (Sprite Skin) swaying in script-driven wind; grass using a wind-sway Shader Graph that also bends around the player (player position fed to the shader); foreground/background foliage parallax layers.
4. **Water / skybox** — Sewers level with real water: `BuoyancyEffector2D` (floating crates, player float), our spring-based dynamic water surface with waves on entry, splash particles, distortion Shader Graph, swim state, underwater audio snapshot. Skybox: a real Skybox material (procedural or 6-sided night city) set as camera background in MainMenu and the Rooftops level, behind parallax layers.
7. **Particle system / teleportation** — particles for sparks, EMP explosion, rain, steam, hacking data stream, landing dust, splash, death; Network Node teleportation and Hub fast-travel (see section 2).
9. **Sound** — AudioMixer (Master/Music/SFX/UI/Ambience) with snapshots (Normal, Underwater, Alarm); adaptive music switching stealth ↔ alert; footsteps per surface; UI sounds; ambient loops per level; pooled audio sources.

Bonus: Cinematics (Timeline + Cinemachine, skippable) and Multiplayer co-op (last phase).

### Rubric Showcase (grading aid)

- Scene `RubricShowcase`, reachable from the main menu: one compact room per rubric item (1–12 + bonuses) with an on-screen label of what is demonstrated, connected by teleport pads.
- In-game debug panel (F1) listing rubric items with a "Go to" button that loads the real level section where the feature appears.
- `RUBRIC.md` maps each item to: scene, prefab(s), script file(s), status (planned / skeleton / done), and how to demonstrate it in under 30 seconds.

---

## 4. Phases for THIS session

Scope of this session: environment + architecture + a playable vertical slice (MainMenu → Hub → small test section of Level 1 → RubricShowcase skeleton). Full content comes in later sessions following ROADMAP.md.

### Phase A — Environment check
- Locate Unity Hub and editors (default `C:\Program Files\Unity\Hub\Editor\`). Use the newest installed Unity 6 LTS. If none exists, stop and tell me the exact version and modules to install (Windows Build Support Mono is enough).
- Check `git`, `git lfs`, .NET SDK, and an IDE (VS Code or Rider). Report anything missing with the exact `winget` command.
- Acceptance: a short environment summary with paths and versions.

### Phase B — Project creation and configuration
- Create `./RootAccess` via `-createProject` in batchmode.
- Write `Packages/manifest.json` with: Universal RP, 2D Animation, 2D Sprite, 2D Tilemap, 2D Tilemap Extras, 2D Pixel Perfect, Shader Graph, Input System, Cinemachine (3.x), Timeline, TextMeshPro (or the Unity 6 equivalent UGUI package), Netcode for GameObjects, Unity Transport, Multiplayer Services, Test Framework, the IDE integration package. Choose versions compatible with the installed editor; open in batchmode to resolve, then confirm in `packages-lock.json` and the log.
- Editor setup script (`Editor/ProjectSetup.cs`, run via `-executeMethod`):
  - create URP Asset + Renderer 2D, assign in Graphics and all Quality levels;
  - Player Settings: company "CerneaBogdanAndrei", product "Root Access", Active Input Handling = Input System Package (New), Linear color space, default 1920×1080, resizable window, Windows x64;
  - Editor Settings: Force Text serialization, Visible Meta Files;
  - tags, sorting layers (Background, Parallax, Environment, Interactables, Characters, Foreground, VFX, UI) and physics layers (Player, Enemy, Ground, Wall, Interactable, Trigger, Water, Projectile, Hidden) with a sensible 2D collision matrix;
  - import TMP Essentials.
- Acceptance: project opens in batchmode with zero errors; settings verified by reading back `ProjectSettings/*.asset`.

### Phase C — Folder structure and assembly definitions
```
Assets/_Project/
  Art/{Sprites,Tilesets,Materials,Shaders,Animations,VFX,Skybox}
  Audio/{Music,SFX,Ambience,Mixers}
  Prefabs/{Player,Enemies,Interactables,Environment,UI,VFX,Systems}
  Scenes/{Bootstrap,MainMenu,Hub,Level01_Rooftops,Level02_Office,Level03_ServerFarm,Level04_Sewers,Level05_HQ,RubricShowcase,Credits}
  ScriptableObjects/{Items,Quests,Skills,Levels,Audio,Dialogue,Strings}
  Settings/{URP,Input,Mixer}
  Scripts/
    Core        (GameManager, SceneLoader + loading screen, EventBus, ServiceLocator, SaveSystem, SettingsService, ObjectPool, Constants)
    Input       (InputActions asset with maps Player/UI/Hacking, generated C# wrapper, InputReader ScriptableObject, rebinding)
    Player      (PlayerController, PlayerStateMachine, PlayerHealth, PlayerAnimatorBridge, PlayerInteractor, Swim)
    AI          (StateMachine base, Guard, SecurityCamera, Drone, Turret, VisionCone, Pathfinding/AStarGrid + AStarPathfinder + PathRequestQueue)
    Gameplay    (IInteractable, Terminal, Door, Elevator, NetworkNode, Pickup, Hazard, Checkpoint, LevelExit, HideZone, WaterBody2D, WindZone2D)
    Hacking     (HackingMinigame base, WireConnect, PatternMemory, PortScan, HackingOverlay)
    Systems     (Inventory, SkillTree, QuestSystem, Economy, TraceMeter, DialogueSystem)
    UI          (MainMenu, SettingsMenu, PauseMenu, HUD, InventoryUI, SkillTreeUI, QuestLogUI, DialogueBox, GameOver, LevelComplete, DebugRubricPanel, UITween)
    Audio       (AudioManager, MusicDirector, FootstepPlayer, MixerSnapshots)
    VFX         (VfxSpawner, ScreenShake, FlickerLight2D, DissolveController, ParallaxLayer, FoliageWind)
    Cinematics  (CutsceneController, SkipCutscene, Timeline signal receivers)
    Net         (NetworkBootstrap, RelayService, IOperatorRole, IInfiltratorRole — stubs only)
    Editor      (ProjectSetup, SceneBuilder, PlaceholderArtGenerator, BuildScript, RubricValidator)
  Tests/{EditMode,PlayMode}
Assets/ThirdParty/
```
- One `.asmdef` per Scripts subfolder with explicit references (no cycles), Editor asmdef Editor-only, test asmdefs referencing the test framework.
- Gameplay state kept separate from presentation (plain C# models + MonoBehaviour views) so it can later be mirrored to NetworkVariables for co-op.
- Acceptance: compiles clean; asmdef dependency graph documented in `ARCHITECTURE.md`.

### Phase D — Skeleton systems (real, minimal, compiling)
- Core: EventBus, ServiceLocator, SceneLoader (async, loading screen), SaveSystem (JSON, versioned), SettingsService (PlayerPrefs), ObjectPool.
- Input: `.inputactions` with keyboard+mouse+gamepad bindings, generated wrapper, InputReader SO exposing events.
- Player: full movement set from rubric item 1 (except swim, which comes as a stub), health, interaction.
- AI: A* grid pathfinder + one Drone following the player in the test section; one Guard with a working patrol/chase state machine; one SecurityCamera with a vision cone.
- Gameplay: one Terminal (PatternMemory minigame working) opening one Door; one Network Node teleport pair with dissolve + particles; one Checkpoint; one Pickup into Inventory.
- Systems: Inventory, TraceMeter, Credits with working logic; SkillTree and QuestSystem data models + one sample quest and 3 sample skills.
- UI: Main menu, Settings (volume sliders wired to the mixer, resolution, fullscreen, rebinding for Jump/Interact), Pause, HUD (health, trace, credits, active quest), Loading screen.
- Audio: AudioMixer with groups + snapshots, AudioManager with pooled sources, placeholder generated beeps (procedural AudioClip) so sound is audible.
- VFX: URP 2D lights in Hub, one FlickerLight2D, Cinemachine camera with confiner and impulse shake.
- Placeholder art: `PlaceholderArtGenerator` creates colored sprites, a basic tileset and a normal map so every scene is playable and readable.
- Scenes built by `SceneBuilder` (Editor script): Bootstrap, MainMenu (with Skybox material as camera background), Hub, Level01_Rooftops test section (tilemap, parallax, a few placeholder trees/grass with the wind shader), RubricShowcase with 12 labeled empty rooms + teleport pads; all added to Build Settings in order.
- Acceptance: from MainMenu I can start, reach Hub, enter the Level 1 test section, hack the terminal, open the door, teleport, get chased by the drone, pause, change volume, return to menu, and open RubricShowcase.

### Phase E — Tests
- EditMode tests: A* (straight path, obstacle detour, no path, diagonal rules), Inventory (add/stack/remove/overflow), TraceMeter (thresholds, decay), SaveSystem (round-trip, version mismatch), SkillTree (prerequisites, XP cost), QuestSystem (objective completion).
- PlayMode smoke test: load each scene in Build Settings and assert no errors in the log.
- Run via `-runTests -testPlatform EditMode -testResults <path>` (and PlayMode); parse the XML; all green.

### Phase F — Build and delivery tooling
- `Editor/BuildScript.cs`: menu item + batchmode entry point; builds Windows x64 to `Builds/Windows/RootAccess.exe` with scenes from Build Settings; fails with non-zero exit on errors.
- `scripts/build.ps1`: runs the build, tails the log on failure, prints the output path and size.
- `scripts/test.ps1`: runs EditMode + PlayMode tests and summarizes results.
- `scripts/package-for-drive.ps1`: creates `dist/RootAccess_Project.zip` (whole project excluding Library, Temp, Obj, Logs, UserSettings, Builds, dist) and `dist/RootAccess_Windows.zip` (the build folder).
- `Editor/RubricValidator.cs`: menu item that checks the project for evidence of each rubric item (e.g. at least N prefab variants, Skybox material assigned, BuoyancyEffector2D present in Sewers, AudioMixer snapshots exist, Animator Controllers with Animation Events) and prints a pass/fail table.
- Acceptance: build succeeds, `.exe` exists and launches to the main menu (run it briefly, then close it), both zips are created.

### Phase G — Version control
- `git init`, Unity `.gitignore`, `.gitattributes` with Git LFS for png, psd, jpg, wav, mp3, ogg, fbx, ttf, otf, exr, mp4; `git lfs install`.
- Commits per phase; final tag `v0.1-setup`.

### Phase H — Documentation
- `CLAUDE.md` (for future Claude Code sessions): project summary, folder map, architecture rules, coding conventions, how to run setup/build/tests/package from the CLI, the batchmode rules from section 0, and a pointer to RUBRIC.md and ROADMAP.md.
- `README.md`: open, play, build, test, package, controls.
- `ARCHITECTURE.md`: systems overview, asmdef graph, event flow, save format.
- `RUBRIC.md`: as described in section 3, with current status per item.
- `ROADMAP.md`: P1 core movement + Hub polish; P2 hacking (3 minigames) + terminals + inventory UI; P3 AI + pathfinding + Levels 1–2; P4 skill tree + quests + Levels 3–5 (incl. water); P5 VFX, audio, trees/grass, skybox, cutscenes; P6 polish + timed 30-minute playtest pass; P7 co-op (bonus); P8 final build + Drive upload. Each phase lists deliverables, rubric items closed, and a rough effort estimate.
- `ASSETS.md`: recommended free packs (cyberpunk tilesets and characters, SFX, music, fonts) from Kenney, itch.io and the Unity Asset Store, with license notes and where to import them.

### Phase I — Final verification and report
- Clean batchmode open (zero errors), all tests green, build OK, RubricValidator output included.
- Write `SETUP_REPORT.md`: versions installed, what was configured, phase-by-phase results, known limitations, and the exact next 5 tasks for the next session.
- Reply to me with a short summary and the path to SETUP_REPORT.md.
