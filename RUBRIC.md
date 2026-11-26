# Barem: unde se vede fiecare punct

Pentru fiecare punct din barem găsești scena, prefab-urile, scripturile (căi relative la `RootAccess/Assets/_Project/Scripts`),
starea curentă și o demonstrație de cel mult 30 de secunde.

**Status:** ✅ gata (funcțional în build) · 🟡 parțial (funcțional, cu o cerință din barem încă planificată) ·
🧩 schelet (cod/pachete prezente, fără conținut) · ⏳ planificat (etapa din [ROADMAP.md](ROADMAP.md)).

Dovezile se verifică automat cu `RubricValidator`: meniul **Root Access → Validate Rubric** sau
`./scripts/unity-batch.ps1 -Step rubric -ExecuteMethod RootAccess.EditorTools.RubricValidator.RunBatch`, cu raportul în
`RootAccess/Logs/RubricReport.md`. Toate verificările obligatorii trec; itemii de roadmap încă deschiși sunt listați
în raport.

## Cum ajungi rapid la fiecare punct

- **Meniu → Prezentare barem**: scena `RubricShowcase` are 14 camere, câte una per punct, fiecare cu titlu și explicație.
  Apasă **E** pe platforma de teleport din dreapta ca să treci la camera următoare.
- **F1 (în joc)**: panoul de barem. Butonul **Mergi** încarcă secțiunea din nivelul real unde apare funcționalitatea.
- Din linia de comandă: `./scripts/smoke-player.ps1 -Scenes RubricShowcase -Spawn room_04` (sau `Level01_Rooftops -Spawn l1_drone`).

| # | Punct | Status | Unde (F1 → Mergi) | Camera din showcase |
|---|---|---|---|---|
| 1 | Mișcare / săritură | ✅ (înot: 🟡) | Level01 `l1_movement` | `room_01` |
| 2 | Materiale / texturi | ✅ | RubricShowcase `room_02` | `room_02` |
| 3 | Mediu: copaci / iarbă | ✅ | Level01 `l1_park` | `room_03` |
| 4 | Apă / skybox | ✅ | Level04 `l4_water` | `room_04` |
| 5 | Lumini / cameră | ✅ | Hub `hub_center` | `room_05` |
| 6 | Prefab-uri | ✅ | RubricShowcase `room_06` | `room_06` |
| 7 | Particule / teleportare | ✅ | Level01 `l1_nodes` | `room_07` |
| 8 | Meniu | ✅ | MainMenu | `room_08` |
| 9 | Sunet | ✅ | RubricShowcase `room_09` | `room_09` |
| 10 | Animații / Animator | ✅ | RubricShowcase `room_10` | `room_10` |
| 11 | Coliziuni / triggere | ✅ | Level01 `l1_security` | `room_11` |
| 12 | Scripturi | ✅ | Level01 `l1_drone` | `room_12` |
| B1 | Cinematice (Timeline) | ✅ | Intro (cutscene) | `room_13` |
| B2 | Multiplayer co-op | ✅ | Meniu → Co-op | `room_14` |

---

## 1. Mișcare / săritură (obligatoriu) ✅

- **Scene:** `Level01_Rooftops` (spawn `l1_movement`: platforme în trepte, pereți pentru wall-jump), `RubricShowcase` camera 1.
- **Prefab-uri:** `Prefabs/Player/Player.prefab` (Rigidbody2D, CapsuleCollider2D, PlayerController, PlayerHealth,
  PlayerInteractor), asset-ul `PlayerMovementConfig`.
- **Scripturi:** `Player/PlayerController.cs`, `Player/States/*.cs` (Grounded, Airborne, WallSlide, Dash, Swim, stările
  blocate Hack/Hurt/Dead/Teleport), `Player/PlayerSensors.cs`, `Player/PlayerMovementConfig.cs`, `Core/FSM/StateMachine.cs`.
- **Ce e implementat:** accelerație/decelerație diferite pe sol și în aer, săritură cu înălțime variabilă (gravitație mai mare
  când eliberezi tasta), coyote time 0,1 s, jump buffer 0,12 s, dublu salt (poate fi legat de skill-ul `DoubleJump`), dash
  cu buffer și cooldown, wall-slide + wall-jump cu blocare scurtă a controlului, ghemuire cu verificarea tavanului, impact
  la aterizare (shake + praf + SFX). Înotul există ca stare și funcționează în camera 4; nivelul de apă vine în P4.
- **Demonstrație (30 s):** F1 → 1 → Mergi. Aleargă și oprește (inerție), sari scurt/lung, sari imediat după marginea
  platformei (coyote), apasă Space chiar înainte de aterizare (buffer), dublu salt, Shift (dash), sari pe perete și din
  perete, ține Ctrl sub obstacol.

## 2. Materiale / texturi (obligatoriu) ✅

- **Scene:** `RubricShowcase` camera 2 (blocuri metal/beton/sticlă, neon, nod hologramă, terminal care se dizolvă în buclă,
  lumină mobilă pe normal maps), plus toate nivelurile.
- **Asset-uri:** `Art/Materials/M_Metal`, `M_Concrete`, `M_Glass`, `M_NeonCyan`, `M_NeonMagenta`, `M_Hologram`,
  `M_Dissolve`, `M_GrassWind`, `M_Water`; 45 de normal maps generate (secondary texture `_NormalMap` pe sprite-uri).
- **Shader Graph** (`Art/Shaders/Graphs`), construite din noduri obișnuite (Time, Sine, Simple Noise, Step, Lerp, Sample
  Texture 2D, Vertex Color, Position):
  - `SG_Hologram` (Sprite Unlit, aditiv): sprite colorat × scanline-uri în spațiul lumii × pâlpâire → `M_Hologram`;
  - `SG_SpriteDissolve` (Sprite Lit, cu mască și normal map): dizolvare pe zgomot cu margine HDR → `M_Dissolve`
    (jucător, terminale „glitch”, teleport);
  - `SG_NeonPulse` (Sprite Unlit, HDR): luminozitate care pulsează, decalată pe axa X → `M_NeonCyan`, `M_NeonMagenta`;
  - `SG_GrassWind` (Sprite Lit): vânt + îndoire lângă jucător în stadiul de vertex → `M_GrassWind` (punctul 3).
- **Scripturi:** `VFX/DissolveController.cs`, `VFX/DissolveDemoLoop.cs`, `Editor/PlaceholderArtGenerator.cs` (normal maps),
  `Editor/Generation/AssetFactory.cs` (materiale), `Editor/Generation/ShaderGraphFactory.cs` + `ShaderGraphWriter.cs`
  (generează grafurile și verifică la import că shaderul compilează fără erori).
- **Demonstrație:** Prezentare barem → camera 2. Lumina punctiformă se mișcă peste sprite-uri cu normal map, terminalul
  din dreapta se dizolvă și reapare, nodul hologramă pâlpâie, neonul pulsează (cu Bloom).

## 3. Mediu: copaci / iarbă (opțional) ✅

- **Scene:** `Level01_Rooftops` (parcul de pe acoperiș, spawn `l1_park`), `RubricShowcase` camera 3.
- **Prefab-uri:** `Environment/Tree.prefab` (sprite `tree_rigged` cu 4 oase și 45 de vârfuri cu greutăți + `SpriteSkin`),
  `Environment/GrassPatch.prefab`, `Environment/WindZone.prefab`.
- **Scripturi:** `VFX/FoliageWind.cs` (vânt scriptat, trimite poziția jucătorului ca vector global), `Gameplay/World/WindZone2D.cs`,
  `VFX/ParallaxLayer.cs`, Shader Graph-ul `SG_GrassWind` (iarba se apleacă în jurul jucătorului; matematica vântului e un
  nod Custom Function care apelează `Art/Shaders/RootAccessShaderGraph.hlsl`).
- **Oase 2D Animation:** `Editor/Generation/SpriteRigFactory.cs` scrie lanțul de oase (Trunk → Bough → Canopy → Crown) și un
  mesh-grilă cu greutăți prin provider-ii Sprite Editor (exact ce ar salva Skinning Editor-ul); `PrefabFactory` creează
  transformările oaselor și le leagă de `SpriteSkin`. `FoliageWind` rotește oasele de deasupra trunchiului, iar mesh-ul
  se îndoaie lin (verificat în testul PlayMode `ParkTrees_AreSpriteSkinRigsBentByTheWind`).
- **Demonstrație:** F1 → 3 → Mergi. Copacii se leagănă, iar iarba se îndoaie când alergi prin ea. Intră în WindZone (rafală)
  și urmărește straturile parallax din fundal.

## 4. Apă / skybox (opțional) ✅

- **Scene:** `Level04_Sewers` (bazine, ziduri sub care te scufunzi, puțul cu stavilă: spawn `l4_water`, `l4_sluice`),
  `RubricShowcase` camera 4 (bazin cu lăzi care plutesc), `MainMenu` și `Level01_Rooftops` (skybox de noapte).
- **Prefab-uri:** `Environment/WaterBody.prefab` (WaterBody2D + BuoyancyEffector2D + trigger, material `M_WaterDistortion`),
  `VFX/VFX_Splash.prefab`.
- **Shader Graph:** `SG_WaterDistortion` (Sprite Unlit): eșantionează `_CameraSortingLayerTexture` (tot ce a desenat
  rendererul 2D până la layer-ul Characters) la coordonate de ecran deformate de zgomot care curge în timp, apoi colorează
  cu gradientul suprafeței (refracție reală: lăzile și jucătorul se văd deformați prin apă). Rendererul 2D are
  „Camera Sorting Layer Texture” activat de `AssetFactory`.
- **Scripturi:** `Gameplay/World/WaterBody2D.cs` (suprafață cu arcuri, valuri la intrare, `SetDepth` pentru inundare),
  `Gameplay/World/FloodControl.cs` (stavila: apa urcă până sub marginea puțului), `Player/States/PlayerSwimState.cs`,
  `Audio/MixerSnapshots.cs` (snapshot Underwater), `VFX/SkyboxBackdrop.cs` (skybox real randat în spatele scenei 2D).
- **Demonstrație:** F1 → 4 → Mergi (canalizarea). Sari în apă (stropi, valuri, sunet înăbușit), privește lăzile deformate
  prin apă, scufundă-te pe sub zid; la `l4_sluice` sparge terminalul stavilei și urcă odată cu apa. Apoi Esc → meniu
  principal pentru skybox-ul de noapte.

## 5. Lumini / cameră (obligatoriu) ✅

- **Scene:** `Hub` (spot-uri de tavan, Freeform pe fereastră, Point, neon pâlpâitor), `RubricShowcase` camera 5
  (Global/Point/Spot/Freeform + umbre), `Level01_Rooftops`.
- **Prefab-uri:** `Systems/CameraRig.prefab` (CinemachineCamera + PositionComposer cu look-ahead + Confiner2D + ImpulseListener),
  `Environment/StreetLamp.prefab`, `NeonSign.prefab`, `Crate.prefab`/`RooftopAC.prefab` (ShadowCaster2D).
- **Scripturi:** `VFX/FlickerLight2D.cs`, `VFX/CameraController.cs` (zoom la hacking), `VFX/ScreenShake.cs` (impulse),
  `Gameplay/World/CameraZone.cs` + `VFX/CameraRoom.cs` (blend între camere), profilele `Settings/URP/VP_*.asset`
  (Bloom + Color Adjustments per nivel).
- **Demonstrație:** F1 → 5 → Mergi (Hub): lumini de toate tipurile și neonul care pâlpâie. În showcase, camera 5 arată
  umbrele lăzilor. Interacționează cu un terminal pentru zoom, cazi de sus pentru shake, iar trecerea dintre camerele din
  showcase arată blend-ul.

## 6. Prefab-uri (obligatoriu) ✅

- **Scene:** `RubricShowcase` camera 6 (Guard, Guard_Heavy, Guard_Sniper, Terminal_Door/Camera/Data, Elevator). Toate
  nivelurile sunt asamblate din prefab-uri (validatorul numără instanțele).
- **Prefab-uri:** 57 în `Prefabs/` (9 variante), printre care `Enemies/Guard_Heavy`, `Enemies/Guard_Sniper` (bază `Guard`) și
  `Interactables/Terminal_Door`, `Terminal_Camera`, `Terminal_Data` (bază `Terminal`).
- **Scripturi:** `Core/Pooling/PoolService.cs`, `PooledObject.cs`, `VFX/VfxSpawner.cs`, `Editor/Generation/PrefabFactory.cs`.
- **Demonstrație:** Prezentare barem → camera 6: trei gardieni cu aspect și parametri diferiți, trei terminale-variantă.
  În Editor: inspectorul unei variante arată prefab-ul de bază. VFX-urile și proiectilele vin din pool (PoolService în SystemsRoot).

## 7. Particule / teleportare (opțional) ✅

- **Scene:** `Level01_Rooftops` (spawn `l1_nodes`: nodul de jos te duce pe acoperișul cu data shard; ploaie), `Hub`
  (fast-travel), `RubricShowcase` camera 7 (emițător demo, abur, ploaie, pereche de noduri).
- **Prefab-uri:** `VFX/VFX_Sparks`, `VFX_EmpExplosion`, `VFX_Rain`, `VFX_Steam`, `VFX_DataStream`, `VFX_LandingDust`,
  `VFX_Splash`, `VFX_Death`, `VFX_TeleportIn/Out/Trail`; `Interactables/NetworkNode`, `TeleportPad`, `FastTravelTerminal`.
- **Scripturi:** `Gameplay/Interactables/Teleporter.cs`, `NetworkNode.cs`, `TeleportPad.cs`, `Gameplay/Hub/FastTravelTerminal.cs`,
  `VFX/DissolveController.cs`, `VFX/VfxDemoEmitter.cs`.
- **Demonstrație:** F1 → 7 → Mergi, apoi E pe nod: jucătorul se dizolvă, un traseu de particule zboară până la nodul de pe
  acoperiș și jucătorul reapare. Ploaia cade continuu. În Hub, terminalele de fast-travel duc la nivel și la showcase.

## 8. Meniu (obligatoriu) ✅

- **Scene:** `MainMenu`; restul ecranelor sunt în `UIRoot`, prezent în orice scenă.
- **Prefab-uri:** `UI/MainMenuCanvas.prefab`, `UI/UIRoot.prefab` (HUD, pauză, setări, game over, nivel complet, loading,
  overlay hacking, inventar, skill tree, jurnal, dialog, panoul F1), `UI/CreditsCanvas.prefab`.
- **Scripturi:** `UI/Menus/MainMenuView.cs`, `SettingsMenuView.cs`, `PauseMenuView.cs`, `RebindActionButton.cs`,
  `UI/Screens/*.cs`, `UI/Common/UIPanel.cs` + `UITween.cs`, `Core/Scenes/SceneLoader.cs` (încărcare asincronă),
  `Core/Settings/SettingsService.cs`, `Input/RebindService.cs`.
- **Demonstrație:** meniul are Continuă / Joc nou / Setări / Prezentare barem / Credite / Ieșire. În Setări: volume
  (general/muzică/efecte/UI) aplicate pe mixer, rezoluție, fullscreen, VSync, rebind pentru Săritură și Interacțiune.
  Joc nou arată ecranul de loading cu bara de progres. În joc: Esc = pauză. Moartea (de exemplu pe podeaua electrică sau
  în laser, din `l1_security`) duce la Game Over, iar ieșirea de la capătul Level01 duce la Nivel complet.

## Mecanici avansate (cerința disciplinei)

- **Terminal personal** (`UI/Screens/TerminalScreenView.cs`, tasta Tab/I): oprește timpul (`GameState.Terminal`) și are
  tab-urile Inventar, Abilități, Misiuni; din consolele din Hub se deschide pe Magazin (`ShopTerminal`), Contracte
  (`JobBoard`) sau Abilități cu deblocare permisă (`SkillTerminal`, singurul loc unde se cheltuie XP).
- **Skill tree** pe 3 ramuri, 8 abilități cu prerechizite și efecte reale: Stealth (pași silențioși, camuflaj optic),
  Hacking (overclock = dificultate −1, a doua șansă la spargere, acces de la distanță), Hardware (dash mai des, +1 viață,
  rază EMP +50%).
- **Magazin** (`Systems/Economy/Shop.cs`, testat în EditMode): cumpărare „totul sau nimic”, refuză dacă inventarul e plin.
- **Grenada EMP** (`Player/PlayerGrenadeThrower.cs` + `Gameplay/World/EmpGrenade.cs`, tasta G): consumă un item din
  inventar, se aruncă spre cursor sau stick, dezactivează tot ce implementează `IEmpTarget` (camere, drone, turele, vizorul
  gardienilor).
- **Demonstrație:** Joc nou → în Hub, E pe consola galbenă (magazin) → cumpără o grenadă; consola verde (abilități);
  consola roz (contracte) → acceptă un contract; Tab → inventar și misiuni. În showcase, camera 12: G spre dronă și turelă.

## 9. Sunet (opțional) ✅

- **Scene:** toate. `RubricShowcase` camera 9 (zonă de alarmă, podea de sticlă și de iarbă pentru pași diferiți).
- **Asset-uri:** `Audio/Mixers/RootAccessMixer.mixer` (grupuri Master/Music/SFX/UI/Ambience, snapshot-uri
  Normal/Underwater/Alarm, 4 parametri de volum expuși), 35 de clipuri procedurale în `Audio/` (SFX, muzică, ambianță),
  `ScriptableObjects/Audio/SfxLibrary`.
- **Scripturi:** `Audio/AudioManager.cs` (20 de surse în pool, 2D/3D), `MusicDirector.cs` (stealth ↔ alertă după
  TraceLevel), `MixerSnapshots.cs`, `FootstepPlayer.cs` (pas pe suprafață, prin `SurfaceTag`), `UI/Common/ButtonSfx.cs`.
- **Demonstrație:** Prezentare barem → camera 9. Mergi pe metal, sticlă și iarbă (pași diferiți), intră în zona de alarmă
  (muzica trece pe alertă, snapshot Alarm), treci peste butoanele din meniu (hover/click). Volumul din Setări se aude imediat.

## 10. Animații / Animator (obligatoriu) ✅

- **Scene:** toate. `RubricShowcase` camera 10 (dronă, cameră de securitate, ușă care se deschide periodic).
- **Asset-uri:** `Art/Animations/{Player,Guard,Drone,SecurityCamera,Door,Elevator,Pulse}/*.controller` + 24 de clipuri.
  Controllerul jucătorului are 10 parametri (Speed, VerticalSpeed, Grounded, Crouching, Dashing, WallSliding, Swimming,
  Hacking, Dead, Hurt) și un blend tree de locomoție (idle/run). Animation Events: pașii în clipul de alergare al
  jucătorului și în cel de mers al gardianului, cadrul de lovire în atacul gardianului.
- **Scripturi:** `Player/PlayerAnimatorBridge.cs`, `AI/Guard/GuardAnimationEvents.cs`, `Audio/FootstepPlayer.cs`,
  `Editor/Generation/AnimationFactory.cs`, `UI/Common/UITween.cs` (tranziții animate de UI).
- **Demonstrație:** aleargă, sari, fă dash și wall-slide (animații distincte, pași sincronizați cu cadrele). În camera 10
  ușa se deschide și se închide animat, camera de securitate se rotește, drona plutește. Panourile UI apar cu fade/scale.

## 11. Coliziuni / triggere (obligatoriu) ✅

- **Scene:** `Level01_Rooftops` (spawn `l1_security`: checkpoint, gardian, zonă de ascuns, podea electrică, laser, cameră,
  terminal, ușă), `RubricShowcase` camera 11.
- **Configurare:** matricea de coliziuni 2D din `ProjectSetup` (layerele Player, Enemy, Ground, Wall, Interactable, Trigger,
  Water, Projectile, Hidden). Validatorul a găsit 36 de perechi de layere care nu se ciocnesc.
- **Scripturi:** `OnTriggerEnter2D/Exit2D` în Checkpoint, Pickup, HideZone, AlarmZone, LevelExit, FallZone (gropile dintre
  acoperișuri), Elevator (pasagerii liftului), WaterBody2D (intrarea în apă), DialogueZone, CameraZone, CameraRoom, Hazard,
  Projectile; `OnTriggerStay2D` în WindZone2D; `OnCollisionEnter2D` în
  PlayerController (impact la aterizare), Drone și Hazard (daune); raycast-uri în `AI/Perception/VisionCone.cs`,
  `Player/PlayerSensors.cs` și `AI/Pathfinding/PathfindingGrid.cs`. Platformele de teleport și terminalele se folosesc cu E
  (`PlayerInteractor` caută cel mai apropiat `IInteractable`).
- **Demonstrație:** F1 → 11 → Mergi. Checkpoint-ul se aprinde, intri în umbră („ASCUNS”), gardianul te vede (conul se
  înroșește, Urmărirea crește), podeaua electrică și laserul iau viață, pickup-ul de credite intră în portofel.

## 12. Scripturi (obligatoriu) ✅

- **Cod:** 13 asmdef-uri runtime/editor + 2 de test, fără cicluri (graful e în [ARCHITECTURE.md](ARCHITECTURE.md)).
  Tot codul e scris de noi: EventBus, ServiceLocator, FSM, A*, sisteme, generatoare.
- **AI:** gardienii aud zgomotul (`PlayerNoise` → starea Investigate, `AI/Perception/Hearing.cs`), converg spre alarme
  (`AlarmRaised` → Search), lunetistul trage de la distanță; dronele folosesc A* pe grila `PathfindingGrid` care acoperă
  tot nivelul (Level01 și Level02).
- **Teste:** `Tests/EditMode` (103: A*, inventar, trace meter, save/load + migrări, skill tree, quest-uri + lanțul de
  misiuni, auz, hacking, EventBus…) și `Tests/PlayMode` (23: fiecare scenă pornește fără erori, fluxul complet de
  acceptare, minijocurile de hacking, terminalul personal, ieșirile nivelurilor 1–2 cu deblocare, tab-ul Călătorie,
  gardianul care aude zgomotul). Rulare: `./scripts/test.ps1`.
- **Demonstrație:** F1 → 12 → Mergi (`l1_drone`). Drona te vede și te urmărește pe un drum A* în jurul stâlpilor (în Editor,
  selectează drona ca să vezi drumul ca gizmo). În `l1_security`, mergi spre gardian: la ~4,5 m te aude și vine să
  verifice (semnul galben); ghemuit (Ctrl) nu faci zgomot. Apoi *Window → General → Test Runner*: 126 de teste verzi.

## Bonus: cinematice ✅

- **Scene:** `Intro` (după Joc nou, apoi ascunzătoarea) și `Finale` (după ieșirea din nivelul 6, apoi creditele).
- **Asset-uri:** `Timelines/TL_Intro.playable`, `TL_Finale.playable`, clipurile `AC_*_Puppet.anim`, semnalele `SIG_*.signal`,
  generate de `Editor/Generation/CutsceneFactory.cs`.
- **Track-uri:** Animation (păpușa hackerului merge pe acoperiș, cu cadrele de alergare), Cinemachine (cadru larg și cadru
  apropiat cu blend între ele), Activation (titlu, subtitrări, dispariția la teleport), Signal (shake, alarmă, VFX de
  teleport și flux de date prin `CutsceneSignalReceiver`).
- **Scripturi:** `Cinematics/CutsceneController.cs` (pornește la Start, starea `GameState.Cutscene`, încarcă scena următoare),
  `SkipCutscene.cs` (Enter/Esc).
- **Demonstrație:** Meniu → Joc nou (sau F1 → Cinematice → Mergi). Intro-ul rulează ~17 s; Enter îl sare direct în
  ascunzătoare.

## Bonus: multiplayer co-op ✅

- **Roluri asimetrice:** Infiltratorul joacă nivelul; Operatorul vede o hartă schematică (zidurile nivelului, jucătorul,
  ușile, camerele, gardienii) și, cu un buget de energie care se reface, comută uși (35), buclează camere 8 s (25) și
  distrage gardieni cu un apel radio fals (20).
- **Două moduri, același cod de Operator** (`Core/Coop`: `IOperatorTarget`, `OperatorSnapshot`, `OperatorModel`,
  `OperatorWire`, `IOperatorLink`; `UI/Coop/OperatorConsoleView.cs`):
  - **Local, ecran împărțit** (`LocalOperatorLink`): camera de joc și HUD-ul pe jumătatea stângă (`SplitViewport`), consola
    pe dreapta; jucătorul 1 rămâne pe tastatură + mouse (`InputReader.RestrictToKeyboardMouse`), Operatorul pe gamepad
    (D-pad sus/jos, A) sau Numpad 8/2/5.
  - **LAN** (`NetworkOperatorLink`, Netcode for GameObjects + Unity Transport, port 7777, fără Relay): gazda joacă și e
    autoritară; clientul se conectează pe IP și rulează doar consola (scena `OperatorConsole`). Snapshot-urile (~10/s),
    harta nivelului (biți, o dată per nivel) și comenzile circulă ca mesaje numite.
- **Scripturi:** `Net/CoopSession.cs` (moduri, snapshot-uri, aplicarea comenzilor), `UI/Menus/CoopMenuView.cs` (Local /
  Găzduiește / Alătură-te pe IP).
- **Teste:** EditMode `OperatorTests` (energie, codec, link local); PlayMode `CoopTests` (în split-screen Operatorul
  deschide ușa, buclează camera, distrage gardianul; gazda LAN pornește, rulează un nivel și se oprește curat);
  `scripts/coop-lan-smoke.ps1` pornește două exe-uri (gazdă + client pe 127.0.0.1) și verifică ușa deschisă de la distanță.
- **Demonstrație:** Meniu → Co-op → Local; în `l1_security` Operatorul (gamepad) buclează camera și deschide ușa porții.
  Pe două PC-uri: unul „Găzduiește în LAN”, celălalt scrie IP-ul gazdei și „Alătură-te”.

## Alte cerințe ale disciplinei

| Cerință | Stare |
|---|---|
| ≥30 de minute de joc (Hub + 5 niveluri × 6–8 min) | ⏳ P3–P6 (acum: Hub + secțiunea de test din Nivelul 1) |
| Meniu de start, setări, play | ✅ |
| GUI în joc: viață, scor/credite, inventar, misiune activă | ✅ HUD (viață, urmărire, credite, EMP, misiune activă) + terminal personal |
| Input tastatură + mouse (și gamepad) | ✅ toate hărțile au binding-uri KBM și gamepad |
| Personaje și inamici animați | ✅ (sprite-uri placeholder) |
| Obiecte scriptate, coliziuni | ✅ |
| Mecanici avansate: interacțiune, inventar, skill tree, quest-uri, pathfinding | ✅ logică + UI (terminal personal cu tab-uri: inventar, abilități pe 3 ramuri, misiuni, magazin, contracte) + teste |
| Livrare: proiect + executabil pe Google Drive | ✅ `scripts/package-for-drive.ps1` (urcarea finală în P8) |
