# Barem: unde se vede fiecare punct

Pentru fiecare punct din barem găsești scena, prefab-urile, scripturile (căi relative la `RootAccess/Assets/_Project/Scripts`),
starea curentă și o demonstrație de cel mult 30 de secunde.

**Status:** ✅ gata (funcțional în build) · 🟡 parțial (funcțional, cu o cerință din barem încă planificată) ·
🧩 schelet (cod/pachete prezente, fără conținut) · ⏳ planificat (etapa din [ROADMAP.md](ROADMAP.md)).

Dovezile se verifică automat cu `RubricValidator`: meniul **Root Access → Validate Rubric** sau
`./scripts/unity-batch.ps1 -Step rubric -ExecuteMethod RootAccess.EditorTools.RubricValidator.RunBatch`, cu raportul în
`RootAccess/Logs/RubricReport.md`. Ultima rulare (25.11.2026) a trecut toate cele 46 de verificări obligatorii; 6 itemi
de roadmap sunt încă deschiși.

## Cum ajungi rapid la fiecare punct

- **Meniu → Prezentare barem**: scena `RubricShowcase` are 14 camere, câte una per punct, fiecare cu titlu și explicație.
  Apasă **E** pe platforma de teleport din dreapta ca să treci la camera următoare.
- **F1 (în joc)**: panoul de barem. Butonul **Mergi** încarcă secțiunea din nivelul real unde apare funcționalitatea.
- Din linia de comandă: `./scripts/smoke-player.ps1 -Scenes RubricShowcase -Spawn room_04` (sau `Level01_Rooftops -Spawn l1_drone`).

| # | Punct | Status | Unde (F1 → Mergi) | Camera din showcase |
|---|---|---|---|---|
| 1 | Mișcare / săritură | ✅ (înot: 🟡) | Level01 `l1_movement` | `room_01` |
| 2 | Materiale / texturi | 🟡 | RubricShowcase `room_02` | `room_02` |
| 3 | Mediu: copaci / iarbă | 🟡 | Level01 `l1_park` | `room_03` |
| 4 | Apă / skybox | 🟡 | RubricShowcase `room_04` | `room_04` |
| 5 | Lumini / cameră | ✅ | Hub `hub_center` | `room_05` |
| 6 | Prefab-uri | ✅ | RubricShowcase `room_06` | `room_06` |
| 7 | Particule / teleportare | ✅ | Level01 `l1_nodes` | `room_07` |
| 8 | Meniu | ✅ | MainMenu | `room_08` |
| 9 | Sunet | ✅ | RubricShowcase `room_09` | `room_09` |
| 10 | Animații / Animator | ✅ | RubricShowcase `room_10` | `room_10` |
| 11 | Coliziuni / triggere | ✅ | Level01 `l1_security` | `room_11` |
| 12 | Scripturi | ✅ | Level01 `l1_drone` | `room_12` |
| B1 | Cinematice (Timeline) | 🧩 | RubricShowcase `room_13` | `room_13` |
| B2 | Multiplayer co-op | 🧩 | RubricShowcase `room_14` | `room_14` |

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

## 2. Materiale / texturi (obligatoriu) 🟡

- **Scene:** `RubricShowcase` camera 2 (blocuri metal/beton/sticlă, neon, nod hologramă, terminal care se dizolvă în buclă,
  lumină mobilă pe normal maps), plus toate nivelurile.
- **Asset-uri:** `Art/Materials/M_Metal`, `M_Concrete`, `M_Glass`, `M_NeonCyan`, `M_NeonMagenta`, `M_Hologram`,
  `M_Dissolve`, `M_GrassWind`, `M_Water`; shadere în `Art/Shaders/*.shader`; 45 de normal maps generate (secondary texture
  `_NormalMap` pe sprite-uri).
- **Scripturi:** `VFX/DissolveController.cs`, `VFX/DissolveDemoLoop.cs`, `Editor/PlaceholderArtGenerator.cs` (normal maps),
  `Editor/Generation/AssetFactory.cs` (materiale).
- **Ce lipsește:** baremul cere ≥3 materiale **Shader Graph**. Hologram, dissolve/glitch și neon pulse sunt acum shadere HLSL
  scrise de mână, cu același efect. Conversia în Shader Graph e în **P5** (validatorul o marchează „planificat”).
- **Demonstrație:** Prezentare barem → camera 2. Lumina punctiformă se mișcă peste sprite-uri cu normal map, terminalul
  din dreapta se dizolvă și reapare, nodul hologramă pâlpâie, neonul pulsează (cu Bloom).

## 3. Mediu: copaci / iarbă (opțional) 🟡

- **Scene:** `Level01_Rooftops` (parcul de pe acoperiș, spawn `l1_park`), `RubricShowcase` camera 3.
- **Prefab-uri:** `Environment/Tree.prefab`, `Environment/GrassPatch.prefab`, `Environment/WindZone.prefab`.
- **Scripturi:** `VFX/FoliageWind.cs` (vânt scriptat, trimite poziția jucătorului ca vector global), `Gameplay/World/WindZone2D.cs`,
  `VFX/ParallaxLayer.cs`, shaderul `RootAccess_SpriteGrassWind.shader` (iarba se apleacă în jurul jucătorului).
- **Ce lipsește:** copacii se leagănă prin transform, nu cu oase 2D Animation/SpriteSkin (**P5**).
- **Demonstrație:** F1 → 3 → Mergi. Copacii se leagănă, iar iarba se îndoaie când alergi prin ea. Intră în WindZone (rafală)
  și urmărește straturile parallax din fundal.

## 4. Apă / skybox (opțional) 🟡

- **Scene:** `RubricShowcase` camera 4 (bazin cu lăzi care plutesc), `MainMenu` și `Level01_Rooftops` (skybox de noapte).
- **Prefab-uri:** `Environment/WaterBody.prefab` (WaterBody2D + BuoyancyEffector2D + trigger), `VFX/VFX_Splash.prefab`.
- **Scripturi:** `Gameplay/World/WaterBody2D.cs` (suprafață cu arcuri, valuri la intrare), `Player/States/PlayerSwimState.cs`,
  `Audio/MixerSnapshots.cs` (snapshot Underwater), `VFX/SkyboxBackdrop.cs` (skybox real randat în spatele scenei 2D).
- **Ce lipsește:** shaderul de distorsiune subacvatică și nivelul Sewers (**P4**). Skybox-ul e gata.
- **Demonstrație:** Prezentare barem → camera 4. Sari în apă (stropi, valuri, sunet înăbușit), înoată, lăzile plutesc și se
  balansează. Apoi Esc → meniu principal pentru skybox-ul de noapte (cerul cu stele din spatele meniului).

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
- **Scripturi:** `OnTriggerEnter2D/Exit2D` în Checkpoint, Pickup, HideZone, AlarmZone, LevelExit, WaterBody2D (intrarea în
  apă), DialogueZone, CameraZone, CameraRoom, Hazard, Projectile; `OnTriggerStay2D` în WindZone2D; `OnCollisionEnter2D` în
  PlayerController (impact la aterizare), Drone și Hazard (daune); raycast-uri în `AI/Perception/VisionCone.cs`,
  `Player/PlayerSensors.cs` și `AI/Pathfinding/PathfindingGrid.cs`. Platformele de teleport și terminalele se folosesc cu E
  (`PlayerInteractor` caută cel mai apropiat `IInteractable`).
- **Demonstrație:** F1 → 11 → Mergi. Checkpoint-ul se aprinde, intri în umbră („ASCUNS”), gardianul te vede (conul se
  înroșește, Urmărirea crește), podeaua electrică și laserul iau viață, pickup-ul de credite intră în portofel.

## 12. Scripturi (obligatoriu) ✅

- **Cod:** 13 asmdef-uri runtime/editor + 2 de test, fără cicluri (graful e în [ARCHITECTURE.md](ARCHITECTURE.md)).
  Tot codul e scris de noi: EventBus, ServiceLocator, FSM, A*, sisteme, generatoare.
- **Teste:** `Tests/EditMode` (92: A*, inventar, trace meter, save/load + migrări, skill tree, quest-uri, hacking, EventBus…)
  și `Tests/PlayMode` (8: fiecare scenă pornește fără erori, plus fluxul complet de acceptare). Rulare: `./scripts/test.ps1`.
- **Demonstrație:** F1 → 12 → Mergi (`l1_drone`). Drona te vede și te urmărește pe un drum A* în jurul stâlpilor (în Editor,
  selectează drona ca să vezi drumul ca gizmo). Apoi deschide *Window → General → Test Runner* și rulează testele: 100 verzi.

## Bonus: cinematice 🧩

- **Ce există:** `Cinematics/CutsceneController.cs` (PlayableDirector + blocarea input-ului), `SkipCutscene.cs`
  (Enter/Esc), `SignalReceiver`, pachetele Timeline + Cinemachine. Camera 13 din showcase e rezervată.
- **Ce lipsește:** asset-urile Timeline pentru intro și final (**P5**).

## Bonus: multiplayer co-op 🧩

- **Ce există:** pachetele Netcode for GameObjects 2.14, Unity Transport, Multiplayer Services; `Net/IInfiltratorRole.cs`,
  `IOperatorRole.cs`, `NetworkBootstrap.cs`, `RelayServiceStub.cs`; `NetworkBootstrap` e plasat în camera 14.
  Modelele de gameplay sunt clase C# simple, ușor de oglindit în `NetworkVariable`.
- **Ce lipsește:** co-op-ul asimetric funcțional (Infiltrator + Operator, cod de join Relay), în **P7**.

## Alte cerințe ale disciplinei

| Cerință | Stare |
|---|---|
| ≥30 de minute de joc (Hub + 5 niveluri × 6–8 min) | ⏳ P3–P6 (acum: Hub + secțiunea de test din Nivelul 1) |
| Meniu de start, setări, play | ✅ |
| GUI în joc: viață, scor/credite, inventar, misiune activă | ✅ HUD; inventarul are UI schelet (P2) |
| Input tastatură + mouse (și gamepad) | ✅ toate hărțile au binding-uri KBM și gamepad |
| Personaje și inamici animați | ✅ (sprite-uri placeholder) |
| Obiecte scriptate, coliziuni | ✅ |
| Mecanici avansate: interacțiune, inventar, skill tree, quest-uri, pathfinding | ✅ logică + teste; UI pentru skill tree/jurnal în P2–P4 |
| Livrare: proiect + executabil pe Google Drive | ✅ `scripts/package-for-drive.ps1` (urcarea finală în P8) |
