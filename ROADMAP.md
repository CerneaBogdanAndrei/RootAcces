# Roadmap Root Access

Plan de la vertical slice (tag `v0.1-setup`) până la livrarea finală. Fiecare etapă are livrabile, punctele din barem pe
care le închide și o estimare de efort pentru un singur dezvoltator, în ore de lucru efectiv. Regula pentru fiecare etapă:
se termină cu teste verzi (`scripts/test.ps1`), build OK, `RubricValidator` fără eșecuri și un commit/tag.

## Punctul de plecare (v0.1-setup)

- Toate sistemele de bază există și sunt testate: mișcare completă, A*, hacking (3 minijocuri legate de terminale: ușă →
  PatternMemory, cameră → PortScan, date → WireConnect), inventar, trace, credite, skill tree, quest-uri, save/load
  versionat, meniuri, mixer, animatori, pool-uri.
- Scene: Bootstrap, MainMenu, Hub, secțiunea de test din Level01, RubricShowcase (14 camere), Credits.
- 102 teste (92 EditMode + 10 PlayMode), inclusiv fluxul complet de acceptare și toate cele trei minijocuri.
- Artă și sunete placeholder procedurale.

| Etapă | Tema | Barem închis | Efort |
|---|---|---|---|
| P1 | Mișcare finală + Hub | 1 (finisare), 8 | 12–16 h |
| P2 | Hacking complet, terminale, inventar UI, EMP | mecanici avansate, 7 | 16–20 h |
| P3 | AI + pathfinding + Nivelurile 1–2 | 11, 12, 5 | 24–30 h |
| P4 | Skill tree + quest-uri + Nivelurile 3–5 (cu apă) | 4, mecanici avansate | 30–40 h |
| P5 | VFX, audio, copaci/iarbă, Shader Graph, skybox, cutscene-uri | 2, 3, 9, 10, bonus cinematice | 20–26 h |
| P6 | Polish + playtest cronometrat de 30 de minute | toate (verificare) | 14–18 h |
| P7 | Co-op asimetric online (bonus) | bonus multiplayer | 20–28 h |
| P8 | Build final + Google Drive | livrare | 4–6 h |

Total estimat: aproximativ 140–185 de ore.

---

## P1: mișcare finală și Hub (12–16 h)

**Livrabile**
- Reglajul fin al `PlayerMovementConfig` după playtest (accelerație, gravitație la cădere, coyote/buffer, wall-jump).
- Înotul complet (ieșirea la suprafață, oxigen opțional) testat în camera 4 din showcase.
- Hub-ul final: decor, NPC/terminal de briefing cu dialog, terminalele (job board, shop, skill, fast-travel) cu UI propriu
  în loc de toast-uri.
- Ecran de „Continuă” cu rezumatul salvării (nivel, timp de joc, credite).
- Sprite-urile jucătorului înlocuite cu un pachet real din `Assets/ThirdParty` (vezi ASSETS.md) și reconectate în `GeneratedArt`.

**Barem închis:** 1 (fără observații), 8 (meniuri complete).
**Teste noi:** PlayMode pentru coyote time, jump buffer, wall-jump și dash (input simulat cu `InputTestFixture`).

## P2: hacking, terminale, inventar (16–20 h) ✅ făcut (PR „P2”)

**Livrabile**
- Finisarea minijocurilor: WireConnect (terminale de date) și PortScan (terminale de cameră) funcționează deja și au teste
  PlayMode. Lipsesc firele desenate între socket-uri, animațiile de rezultat și dificultatea crescătoare pe nivel.
- Dificultate pe terminal, skill-urile de hacking aplicate (`FasterHacking`, `ExtraHackAttempt`).
- Inventar UI complet (grilă, descriere, folosirea itemelor), cheile USB deschid uși, exploit-urile sar peste un minijoc.
- Grenada EMP: aruncare (acțiunea `Throw` e deja în input), traiectorie, explozie (`VFX_EmpExplosion`), dezactivează
  camere, drone și turele (`IHackable`).
- Shop-ul din Hub funcțional (cumpărare cu credite).

**Barem închis:** mecanicile avansate „interacțiune cu obiecte” și „inventar”; 7 (particula EMP folosită în gameplay).
**Teste noi:** EditMode pentru shop (credite insuficiente, stack), PlayMode pentru grenada EMP (dezactivează camera și drona).

## P3: AI, pathfinding, Nivelurile 1–2 (24–30 h) ✅ făcut (PR „P3”)

Realizat: `LevelLayout` (constructor pe segmente), Level01 refăcut (~230 de tile-uri: parc, gropi, horn de wall-jump,
patrulă, poartă cu cameră, lasere, salt prin noduri, arena dronelor), `Level02_Office` (interior pe două etaje, lift,
lunetist, turele, seif), lanțul de misiuni (`followUpQuest`), tab-ul Călătorie, auzul gardienilor + răspunsul la alarmă,
lunetistul cu proiectile, gropile cu `FallZone`. Reparate pe drum: gardienii nu patrulau (sonda de podea pornea din
podea), referințele la ScriptableObject-uri din scene se salvau goale (dialogul din Level01, terminalul spre showcase).

**Livrabile**
- Gardienii: auz (zgomotul din `TraceNoise` atrage gardienii), convergența spre `AlarmRaised`, căutare din ultima poziție,
  atac la distanță pentru Sniper.
- Drone: patrulare pe waypoint-uri, păstrarea drumului A* la schimbarea țintei, evitarea altor drone.
- **Nivelul 1, Rooftops & Park** complet (6–8 min): tutorial contextual, parc, bloc de securitate, noduri, arenă cu drone.
- **Nivelul 2, Office Tower** (6–8 min): camere, gardieni, primele minijocuri, lift.
- Fiecare nivel cu `LevelDefinition`, ieșire, recompense și checkpoint-uri.

**Barem închis:** 11 (triggere și coliziuni folosite în tot conținutul), 12 (A* în joc real), 5 (confiner și blend-uri
per cameră de nivel).
**Teste noi:** EditMode pentru FSM-ul gardianului (tranziții), PlayMode „nivelul se poate termina” (traseu scriptat până la ieșire).

## P4: skill tree, quest-uri, Nivelurile 3–5 cu apă (30–40 h) ✅ făcut (PR „P4”)

Realizat: `Level03_ServerFarm` (hale cu rafturi, drone, coridor cu 4 lasere, lanț de turnuri traversat prin noduri,
nucleul de servere), `Level04_Sewers` (bazine cu lăzi plutitoare, ziduri sub care te scufunzi, puțul cu stavilă care se
inundă), `Level05_HQ` (finalul, toate mecanicile, ieșirea duce la credite), Shader Graph `SG_WaterDistortion` (refracție),
misiunile principale înlănțuite pe toate cele 5 niveluri. Skill tree-ul și jurnalul de misiuni erau deja făcute în P2.

**Livrabile**
- Skill tree UI (3 ramuri: Stealth / Hacking / Hardware, XP, prerechizite) și toate efectele aplicate în gameplay.
- Jurnal de misiuni UI, misiuni secundare din job board, recompense, bonus pentru stealth (`AvoidDetection`).
- **Nivelul 3, Server Farm**: drone multe, lasere, teleportare prin noduri ca puzzle.
- **Nivelul 4, Sewers**: apă reală pe tot nivelul (WaterBody2D + BuoyancyEffector2D), înot, lăzi plutitoare, puzzle de
  inundare, snapshot Underwater, **shader de distorsiune subacvatică** (deschide verificarea „planificat” din validator).
- **Nivelul 5, Corporate HQ**: final cu toate mecanicile.

**Barem închis:** 4 (complet), mecanicile avansate „skill tree” și „quest-uri”.
**Teste noi:** EditMode pentru efectele skill-urilor, PlayMode pentru plutirea în apă și pentru finalizarea fiecărui nivel.

## P5: VFX, audio, mediu, Shader Graph, cutscene-uri (20–26 h) ✅ făcut (PR „P5”)

Realizat: copacii cu oase (`SpriteRigFactory` + `SpriteSkin`, legănați de `FoliageWind`), cutscene-urile Timeline `Intro`
și `Finale` (Animation, Cinemachine, Activation, Signal; se pot sări). Rămân pentru polish: audio și artă din pachete reale
(ASSETS.md), care țin de dezvoltator.

**Livrabile**
- ✅ **Shader Graph** (făcut devreme, în etapa „Shader Graph”): hologramă, dissolve/glitch, neon pulse și iarba cu vânt
  sunt `.shadergraph` generate din Editor; distorsiunea apei vine cu nivelul Sewers (P4).
- **Copaci cu oase:** sprite-uri cu rig 2D Animation (`SpriteSkin`, 3–4 oase), legănați de `FoliageWind`.
- Audio real: muzică stealth/alertă pe straturi, ambianță per nivel, pași pe fiecare suprafață, din pachetele din ASSETS.md.
- Artă reală pentru tileset-uri, inamici și UI, plus fonturi.
- **Timeline:** intro (după Joc nou) și final (după Nivelul 5), sărite cu Enter/Esc (`CutsceneController` există).
- Rafinarea skybox-ului și a parallax-ului per nivel.

**Barem închis:** 2 (Shader Graph), 3 (oase), 9 și 10 cu asset-uri reale, bonusul de cinematice. După P5,
`RubricValidator` nu mai are itemi „planificat”, cu excepția co-op-ului.

## P6: polish și playtest cronometrat (14–18 h) ✅ făcut (PR „P6”)

Realizat: timpul pe nivel (cel mai bun timp se salvează; ecranul „Misiune îndeplinită” arată timpul nivelului, recordul
și timpul total), XP în HUD, briefing-ul din ascunzătoare, raportul de durată estimată (`Logs/PlaytimeEstimate.md`,
generat la fiecare `SceneBuilder.BuildAll`, verificat de `RubricValidator`).

| Nivel | Segmente | Estimare primă trecere (min) |
|---|---|---|
| 1. Acoperișuri și parc | 15 | 7,7 |
| 2. Turnul de birouri | 14 | 9,5 |
| 3. Ferma de servere | 12 | 9,3 |
| 4. Canalizarea | 13 | 8,9 |
| 5. Sediul corporației | 15 | 11,2 |
| Ascunzătoarea (5 vizite) | – | 3,3 |
| Cutscene-uri și dialoguri | – | 1,8 |
| **Total** | | **≈ 52** |

Estimarea însumează timpii pe segment (mers, așteptarea patrulelor și a laserelor, câte o încercare pe minijoc) × 1,25
pentru morți și reîncercări. **De făcut de dezvoltator:** o trecere cronometrată completă; timpii reali apar pe ecranul de
final al fiecărui nivel și rămân în salvare.

**Livrabile**
- Playtest complet cronometrat: Hub + 5 niveluri ≥ 30 de minute la prima trecere. Durata pe nivel se notează în
  `SETUP_REPORT`/`ROADMAP` și dificultatea se ajustează.
- Profilare pe hardware țintă (Iris Xe, 1080p): 60 FPS stabil, fără alocări per cadru în gameplay (Profiler + Memory Profiler).
- Juice: hit-stop, tranziții, feedback UI, accesibilitate (subtitrări la dialoguri, mărimea textului).
- Rezolvarea bug-urilor din playtest; toate textele revizuite în `Strings.cs`.

**Barem închis:** verificarea finală a tuturor celor 12 puncte în build.

## P7: co-op asimetric online, bonus (20–28 h) ✅ făcut (PR „P7”)

Realizat altfel decât planul inițial: în loc de Relay (cod de join prin servicii online), co-op-ul merge **local pe ecran
împărțit** și **în LAN pe IP direct**, cu același cod de Operator (`IOperatorLink`: `LocalOperatorLink` /
`NetworkOperatorLink`). Infiltrator + Operator (hartă, uși, camere, distragerea gardienilor, energie). Detalii în RUBRIC.md.

**Livrabile**
- Netcode for GameObjects + Unity Relay: host/join cu cod.
- **P1 Infiltrator** joacă platformerul. **P2 Operator** are o hartă de ansamblu, deschide uși, pune camerele în buclă și
  marchează gardienii.
- Stare sincronizată: modelele (`TraceMeter`, uși, terminale) oglindite în `NetworkVariable`/RPC prin
  `IInfiltratorRole`/`IOperatorRole`.
- Meniul „Co-op” cu creare și intrare în sesiune.

**Barem închis:** bonusul multiplayer.

## P8: build final și Google Drive (4–6 h) ✅ făcut (PR „P8”, tag `v1.0`)

Realizat: `Documentatie/DOCUMENTATIE.md` (documentația completă a jocului) cu capturile din `Documentatie/screenshots`,
README-ul cu galerie, [RELEASE_REPORT.md](RELEASE_REPORT.md), tag-ul `v1.0`, arhivele din `dist/`. Urcarea pe Google
Drive și testul pe un PC curat rămân la dezvoltator.

**Livrabile**
- `./scripts/test.ps1`, `RubricValidator`, `./scripts/package-for-drive.ps1 -Build`.
- Smoke pe un PC curat (dezarhivare, rulare, 30 de minute de joc).
- Urcarea pe Google Drive a celor două arhive și a unui README de prezentare; tag `v1.0`.

## P9: grafică nouă (v1.1) ✅ făcut (PR „P9”)

- Personaje desenate dintr-un schelet (șold, trunchi, picioare cu IK pe 2 oase, brațe, cap): hacker cu glugă, eșarfă și
  rucsac, gardian, gardian greu cu scut, lunetist, fixer-ul din ascunzătoare. Alergare pe 8 cadre, idle pe 4, înot,
  hacking, cădere și moarte pe mai multe cadre; fiecare cadru are hartă de normale reală pentru luminile 2D.
- 7 biomuri cu tile-uri proprii (acoperișuri cu fațade și ferestre, birouri, ferma de servere, canalizare, sediul
  corporației, ascunzătoarea, laboratorul cu baremul), lămpi și ambianță pe biom.
- 38 de piese de decor și 5 tipuri de lămpi (rezervor de apă, panou publicitar, birouri, automat de sucuri, răcire pentru servere, valve,
  statuie, recepție etc.) așezate automat între segmente; obiectele de gameplay redesenate la aceleași dimensiuni.
- Meniu principal cu ilustrație pixel-art (hackerul pe acoperiș, dronă, lună, ploaie); versiunea 1.1.

## P10: mai mult conținut ✅ făcut (PR „P10”)

- **15 jurnale de date** (3 pe nivel) ascunse în afara drumului principal: pe zidul hornului de wall-jump, pe pasarele,
  pe stâlpii din arena dronelor, pe marginea turelelor, între ultimele lasere, pe fundul bazinelor, în puțul stavilei,
  în tuburile de ventilație. Spun povestea Halden Systems (Proiectul OCHI, inginerul L. Varga, contul root).
- Tab-ul **Jurnale** în terminalul personal: jurnalele găsite se citesc, iar cele lipsă indică nivelul în care sunt.
  Fiecare jurnal dă 25 de credite și 20 XP; rămân găsite în salvare (`SaveData.loreLogs`, compatibil cu salvările vechi).
- Contractul **Arhivarul** pe avizierul din Hub: 10 jurnale pentru 500 de credite și 250 XP. Jurnalele găsite înainte de
  acceptare se numără (`QuestLog.ReportTo`).
- Segmentul nou **Tub de ventilație**: o trecere de un tile, prin care treci doar ghemuit, în fiecare nivel.

## Datorie tehnică urmărită

- Regenerarea conținutului rescrie fileID-urile sub-asset-urilor (controllere, profile de volum), ceea ce produce diff-uri
  mari. Opțional: ID-uri deterministe sau regenerare doar la schimbare.
- Dronele nu au încă waypoint-uri de patrulare (stau pe loc până văd jucătorul).
- `RubricValidator` numără dovezi, nu calitate: demonstrațiile din RUBRIC.md rămân verificarea finală.
