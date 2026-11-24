# Root Access – progres sesiunea 1 + prompt pentru sesiunea următoare

Data: 2026-11-24 · Proiect Unity: `D:\ProiectSPV\RootAccess` · Repo git: `D:\ProiectSPV` (branch `main`)

---

## 1. Ce e GATA (verificat)

### Faza A – Mediu ✅
- Unity Hub 4.0.1 (MSIX, winget) + **Unity CLI 1.0.1** (`%LOCALAPPDATA%\Microsoft\WindowsApps\unity.exe`).
- **Unity 6000.3.29f1 (6.3 LTS)** → `C:\Program Files\Unity\Hub\Editor\6000.3.29f1\Editor\Unity.exe` (Windows Mono inclus).
- Cont Unity conectat + **licență Personal activă**.
- git 2.55, git-lfs 3.7.1, .NET SDK 10.0.300, VS Code 1.145 (fără extensia Unity: `code --install-extension visualstudiotoolsforunity.vstuc`), Visual Studio 2026 Community.

### Faza B – Proiect + configurare ✅ (commit `7c9f372`)
- Proiect creat cu `-createProject`; `Packages/manifest.json`: URP 17.3.0, 2D Animation 13.0.6, 2D Sprite, 2D Tilemap + Extras 6.0.3, Pixel Perfect 5.1.1, Shader Graph 17.3.0, Input System 1.20.1, **Cinemachine 3.1.8**, Timeline 1.8.13, UGUI 2.0 (TMP), Netcode 2.14.0, Transport 2.7.4, Multiplayer Services 2.4.1, Test Framework 1.6.0, IDE VS 2.0.26.
- `Editor/ProjectSetup.cs`: URP asset + **2 renderere** (index 0 = Renderer2D, index 1 = UniversalRenderer, folosit doar pentru skybox), player settings (CerneaBogdanAndrei / Root Access, Linear, 1920×1080, Input System only, Win64 Mono), Force Text + Visible Meta, tag-uri, sorting layers, layere fizice (Water = layer built-in 4), matrice de coliziuni 2D, generare wrapper input; TMP Essentials importat prin `-importPackage`.

### Fazele C + D – Arhitectură + sisteme ✅ (compilează fără erori și fără warning-uri)
- 13 asmdef-uri: Core, Input, Systems, Audio, VFX, Player, AI, Gameplay, Hacking, UI, Cinematics, Net, Editor (graful e în comentariile asmdef-urilor; de documentat în ARCHITECTURE.md).
- Core: EventBus<T>, ServiceLocator, FSM, PoolService, SaveSystem (JSON versionat + migrări), SettingsService (PlayerPrefs), SceneLoader async, GameManager, SystemsBootstrapper (`Resources/SystemsRoot`), `Strings` (toate textele RO), `CommandLineAutomation` (argumente `-ra-*`).
- Input: `RootAccessInputActions.inputactions` (Player/UI/Hacking, KBM + gamepad) + wrapper generat, `InputReader` SO, `InputModeController`, `RebindService`.
- Player: controller cu FSM (Grounded/Airborne/WallSlide/Dash/Swim/Hack/Hurt/Dead/Teleport): coyote, buffer, săritură variabilă, dublu salt, dash, wall-jump, ghemuire, înot (stub), Health, Interactor, Visibility (layer Hidden), AnimatorBridge.
- AI: A* propriu (`AStarGrid`, `AStarPathfinder` cu heap, fără tăiat colțuri), `PathfindingGrid`, `PathRequestQueue`, `VisionCone` (raze + mesh), Guard (Patrol→Suspicious→Chase→Search→Return) + config-uri Base/Heavy/Sniper, SecurityCamera, Drone, Turret, Projectile.
- Gameplay: Terminal (+variante Door/Camera/Data), Door, Elevator, Teleporter/NetworkNode/TeleportPad, Pickup, Hazard, Checkpoint, LevelExit, HideZone, WaterBody2D (arcuri) + Buoyancy, WindZone2D, AlarmZone, DialogueZone, SpawnPoint, LevelContext, CameraZone, console Hub (JobBoard, FastTravel, Shop, SkillTerminal).
- Systems: Inventory, TraceMeter, Wallet, SkillTree, QuestLog, Dialogue, GameSession/SaveData, SessionService, GameDatabase.
- Hacking: modele + view-uri PatternMemory / WireConnect / PortScan + `HackingOverlay`.
- UI: MainMenu, Settings (volume, rezoluție, fullscreen, VSync, rebind Jump/Interact), Pause, HUD, Loading, GameOver, LevelComplete, DialogueBox, Inventory/QuestLog/SkillTree (schelete), DebugRubricPanel (F1), Credits, UITween, ButtonSfx.
- Audio: AudioManager (surse din pool), MusicDirector (stealth/alert), MixerSnapshots, FootstepPlayer, SfxLibrary.
- VFX: VfxSpawner, ScreenShake (impulse), CameraController (zoom la hack), CameraRoom, FlickerLight2D, DissolveController, ParallaxLayer, FoliageWind, SkyboxBackdrop, plus helperi de demo.
- Cinematics/Net: CutsceneController, SkipCutscene, SignalReceiver; IInfiltratorRole/IOperatorRole, RelayServiceStub, NetworkBootstrap.

### Pipeline-ul de conținut (Editor) ✅
- `PlaceholderArtGenerator.GenerateAll`: 74 sprite-uri, 45 normal maps, skybox cu 6 fețe, 62 de fișiere audio (SFX + muzică + ambianță, toate procedurale).
- `SceneBuilder.BuildAll`: materiale, 7 shadere HLSL proprii (Dissolve lit, GrassWind lit, Hologram, NeonPulse, Water, ParticlesAdditive, Backdrop), **AudioMixer prin reflection** (4 grupuri, 3 snapshot-uri, 4 parametri expuși), Animator Controllers + clipuri cu Animation Events, 12 prefab-uri VFX, UIRoot, toate prefab-urile + variantele, SystemsRoot și scenele Bootstrap, MainMenu, Hub, Level01_Rooftops, RubricShowcase (14 camere) și Credits, plus Build Settings.
- `BuildScript.BuildWindows` → `RootAccess/Builds/Windows/RootAccess.exe` (~108 MB) ✅.

### Scripturi CLI ✅
- `scripts/common.ps1`, `scripts/unity-batch.ps1`, `scripts/build.ps1 [-RebuildContent] [-Development]`, `scripts/smoke-player.ps1` (pornește exe-ul în fereastră, face capturi în `RootAccess/Logs/smoke/*.png` și verifică `Player.log`).
- Ultimul smoke test: MainMenu, Hub, Level01, RubricShowcase și Credits pornesc **fără excepții**.

---

## 2. Probleme cunoscute (de rezolvat PRIMELE)

1. **Jucătorul nu se vede în Hub/Level01, iar HUD-ul arată „Viață 0/5”** (deci a murit sau nu a fost inițializat). Tilemap-urile sunt acum reparate: în batchmode `SetTile` nu merge, așa că folosim `SetTiles`. Pași: rulează din nou smoke-ul. Dacă tot apare 0/5, adaugă loguri `[Player]` cu poziția, verifică `LevelContext` (spawn) și dacă CompositeCollider-ul tilemap-ului există în build, apoi kill plane-ul și hazard-urile. Verifică și că `CM_Follow` urmărește jucătorul.
2. Barele HUD (viață/urmărire) nu par să afișeze fill-ul; verifică `Image.type = Filled` cu sprite alb.
3. Titlul „ASCUNZĂTOAREA” din Hub se suprapune cu panoul HUD (mută-l mai jos sau mai la dreapta).
4. Skybox-ul din Level01 (SkyCamera → RenderTexture → quad) trebuie verificat vizual; în MainMenu și Credits camera folosește UniversalRenderer (index 1).
5. De verificat în joc: hacking-ul (overlay + minijocuri), teleportul, urmărirea dronei, pauza, volumul și rebind-ul. Folosește `smoke-player.ps1 -Scenes Level01_Rooftops -Spawn l1_drone` etc. Spawn-uri: `l1_park`, `l1_movement`, `l1_security`, `l1_nodes`, `l1_drone`, `hub_center`, `room_01`..`room_14`.

### Abateri de documentat în RUBRIC.md / ROADMAP
- Shaderele sunt **HLSL**, nu Shader Graph. Rubrica cere ≥3 materiale Shader Graph → **P5** (sau acum, dacă e timp).
- Copacii se leagănă prin transform (FoliageWind), nu cu oase 2D Animation/SpriteSkin → P5.
- Apa nu are încă shader de distorsiune (are doar shimmer) → P4.
- Nu există încă asset-uri Timeline (intro/finale) → P5.
- DTO-urile JSON (SaveData, GameSettings, SaveEnvelope etc.) și `BuildContext` (editor) au câmpuri publice, ca excepție intenționată.
- `ReflectionProbe.cs` (Editor) e o unealtă de diagnostic pentru API-ul intern al mixerului.

---

## 3. Ce a RĂMAS din SETUP_PROMPT.md

- **Faza D (finalizare):** rezolvă problemele din secțiunea 2 și verifică manual criteriul de acceptare (meniu → Hub → Level01 → hack terminal → ușă → teleport → drona te urmărește → pauză → volum → meniu → RubricShowcase).
- **Faza E – Teste:** asmdef-uri `Tests/EditMode` + `Tests/PlayMode` (referințe: nunit, TestRunner, `UNITY_INCLUDE_TESTS`). EditMode: A* (drum drept, ocolire, fără drum, reguli diagonale), Inventory (add/stack/remove/overflow), TraceMeter (praguri, decay, lockdown), SaveSystem (round-trip, version mismatch, migrare), SkillTree (prerechizite, cost XP), QuestLog (finalizare obiective, AvoidDetection), PatternMemory/WireConnect/PortScan (modele). PlayMode: încarcă fiecare scenă din Build Settings și verifică că nu apar erori în log. Rulează cu `-runTests -testPlatform EditMode -testResults <xml>` (fără `-quit`) și parsează XML-ul.
- **Faza F:** `scripts/test.ps1` (EditMode + PlayMode + sumar), `scripts/package-for-drive.ps1` (`dist/RootAccess_Project.zip` fără Library/Temp/Obj/Logs/UserSettings/Builds/dist + `dist/RootAccess_Windows.zip`), `Editor/RubricValidator.cs` (meniu + batch: variante de prefab, skybox setat, BuoyancyEffector2D, snapshot-uri în mixer, Animation Events etc., cu tabel pass/fail).
- **Faza G:** commit pentru fiecare fază (există deja commitul A+B și un commit WIP) + tag-ul final `v0.1-setup`.
- **Faza H – Documentație:** `CLAUDE.md`, `README.md`, `ARCHITECTURE.md` (graful asmdef, fluxul de evenimente, formatul de salvare), `RUBRIC.md` (scenă, prefab-uri, scripturi, status, demo în 30 s), `ROADMAP.md` (P1–P8 cu livrabile, itemii de barem închiși și estimări), `ASSETS.md` (Kenney, itch.io, Asset Store, licențe, `Assets/ThirdParty`).
- **Faza I:** deschidere curată în batchmode, teste verzi, build OK, output-ul RubricValidator și `SETUP_REPORT.md`, apoi răspuns scurt.

---

## 4. Comenzi utile

```powershell
# compilare/rulare metodă editor (proiectul NU trebuie să fie deschis în Unity)
./scripts/unity-batch.ps1 -Step 99-test -ExecuteMethod RootAccess.EditorTools.SceneBuilder.BuildAll
./scripts/build.ps1 -RebuildContent          # regenerează conținutul + build-ul exe
./scripts/smoke-player.ps1 -Scenes Hub,Level01_Rooftops -Spawn l1_security
# re-generare artă (doar dacă se schimbă PlaceholderArtGenerator)
./scripts/unity-batch.ps1 -Step 30-art -ExecuteMethod RootAccess.EditorTools.PlaceholderArtGenerator.GenerateAll
```
Ordinea completă pe o mașină curată: `ProjectSetup.Run` → `-importPackage "<ugui>/Package Resources/TMP Essential Resources.unitypackage"` → `PlaceholderArtGenerator.GenerateAll` → `SceneBuilder.BuildAll` → `BuildScript.BuildWindows`.
Logurile batch sunt în `RootAccess/Logs/<step>.log`.

Lecții tehnice (Unity 6000.3):
- `Tilemap.SetTile(Vector3Int, TileBase)` nu are efect în batchmode; folosește `SetTiles`.
- `AssetDatabase.ImportPackage` nu se execută în batchmode cu `-quit`; folosește `-importPackage`.
- Renderer2D nu desenează skybox și nu se poate stivui cu UniversalRenderer.
- `RequireComponent` adaugă automat componente; adaugă componenta principală ÎNAINTE de cele care o cer.
- `GetComponent<T>() ?? ...` nu funcționează pe obiecte Unity (fake null); folosește `TryGetComponent`.
- Enum-uri cu valori ne-secvențiale (`Light2D.NormalMapQuality`): setează cu `intValue`, nu cu `enumValueIndex`.

---

## 5. PROMPT pentru sesiunea următoare (copiază tot blocul)

```
Continuăm setup-ul proiectului Unity „Root Access” din D:\ProiectSPV.
Citește ÎNTÂI integral D:\ProiectSPV\SETUP_PROMPT.md (specificația completă) și D:\ProiectSPV\PROGRES_NEXTPROMPT.md
(progresul sesiunii 1, problemele cunoscute, ce a rămas, comenzile și lecțiile tehnice).
Unity 6000.3.29f1 e instalat și licențiat; proiectul e în D:\ProiectSPV\RootAccess, scripturile CLI în D:\ProiectSPV\scripts.
Respectă regulile din secțiunea 0 a SETUP_PROMPT.md (batchmode, fără YAML scris de mână, stil de cod, commit pe fază).

Ordinea de lucru:
1. Rezolvă problemele din secțiunea 2 a PROGRES_NEXTPROMPT.md (jucător invizibil / „Viață 0/5”, barele HUD,
   titlul din Hub, skybox-ul din Level01). Verifică prin build + scripts/smoke-player.ps1 (cu capturi) și citește Player.log.
   Apoi verifică fluxul de acceptare al Fazei D cât se poate automat. Commit „feat(slice): phase C+D …”.
2. Faza E (teste EditMode + PlayMode, toate verzi) → commit.
3. Faza F (test.ps1, package-for-drive.ps1, RubricValidator.cs; build + zip-uri create) → commit.
4. Faza H (CLAUDE.md, README.md, ARCHITECTURE.md, RUBRIC.md, ROADMAP.md, ASSETS.md) → commit.
5. Faza I (verificare finală + SETUP_REPORT.md) → commit + tag v0.1-setup.
La final răspunde-mi scurt, în română, cu calea către SETUP_REPORT.md.
```
