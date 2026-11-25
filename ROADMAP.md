# Roadmap Root Access

Plan de la vertical slice (tag `v0.1-setup`) până la livrarea finală. Fiecare etapă are livrabile, punctele din barem pe
care le închide și o estimare de efort pentru un singur dezvoltator, în ore de lucru efectiv. Regula pentru fiecare etapă:
se termină cu teste verzi (`scripts/test.ps1`), build OK, `RubricValidator` fără eșecuri și un commit/tag.

## Punctul de plecare (v0.1-setup)

- Toate sistemele de bază există și sunt testate: mișcare completă, A*, hacking (3 modele, PatternMemory legat în nivel),
  inventar, trace, credite, skill tree, quest-uri, save/load versionat, meniuri, mixer, animatori, pool-uri.
- Scene: Bootstrap, MainMenu, Hub, secțiunea de test din Level01, RubricShowcase (14 camere), Credits.
- 100 de teste (92 EditMode + 8 PlayMode), inclusiv fluxul complet de acceptare.
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

## P2: hacking, terminale, inventar (16–20 h)

**Livrabile**
- Minijocurile WireConnect și PortScan legate de tipurile de terminal (Camera → WireConnect, Data → PortScan). Modelele
  și view-urile există deja.
- Dificultate pe terminal, skill-urile de hacking aplicate (`FasterHacking`, `ExtraHackAttempt`).
- Inventar UI complet (grilă, descriere, folosirea itemelor), cheile USB deschid uși, exploit-urile sar peste un minijoc.
- Grenada EMP: aruncare (acțiunea `Throw` e deja în input), traiectorie, explozie (`VFX_EmpExplosion`), dezactivează
  camere, drone și turele (`IHackable`).
- Shop-ul din Hub funcțional (cumpărare cu credite).

**Barem închis:** mecanicile avansate „interacțiune cu obiecte” și „inventar”; 7 (particula EMP folosită în gameplay).
**Teste noi:** EditMode pentru shop (credite insuficiente, stack), PlayMode pentru WireConnect/PortScan prin terminal.

## P3: AI, pathfinding, Nivelurile 1–2 (24–30 h)

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

## P4: skill tree, quest-uri, Nivelurile 3–5 cu apă (30–40 h)

**Livrabile**
- Skill tree UI (3 ramuri: Stealth / Hacking / Hardware, XP, prerechizite) și toate efectele aplicate în gameplay.
- Jurnal de misiuni UI, misiuni secundare din job board, recompense, bonus pentru stealth (`AvoidDetection`).
- **Nivelul 3, Server Farm**: drone multe, lasere, teleportare prin noduri ca puzzle.
- **Nivelul 4, Sewers**: apă reală pe tot nivelul (WaterBody2D + BuoyancyEffector2D), înot, lăzi plutitoare, puzzle de
  inundare, snapshot Underwater, **shader de distorsiune subacvatică** (deschide verificarea „planificat” din validator).
- **Nivelul 5, Corporate HQ**: final cu toate mecanicile.

**Barem închis:** 4 (complet), mecanicile avansate „skill tree” și „quest-uri”.
**Teste noi:** EditMode pentru efectele skill-urilor, PlayMode pentru plutirea în apă și pentru finalizarea fiecărui nivel.

## P5: VFX, audio, mediu, Shader Graph, cutscene-uri (20–26 h)

**Livrabile**
- **Shader Graph:** hologramă, glitch/dissolve și neon pulse refăcute ca `.shadergraph` (create din Editor, nu scrise de
  mână), plus distorsiunea apei. Materialele `M_Hologram`, `M_Dissolve`, `M_NeonCyan/Magenta` trec pe ele.
- **Copaci cu oase:** sprite-uri cu rig 2D Animation (`SpriteSkin`, 3–4 oase), legănați de `FoliageWind`.
- Audio real: muzică stealth/alertă pe straturi, ambianță per nivel, pași pe fiecare suprafață, din pachetele din ASSETS.md.
- Artă reală pentru tileset-uri, inamici și UI, plus fonturi.
- **Timeline:** intro (după Joc nou) și final (după Nivelul 5), sărite cu Enter/Esc (`CutsceneController` există).
- Rafinarea skybox-ului și a parallax-ului per nivel.

**Barem închis:** 2 (Shader Graph), 3 (oase), 9 și 10 cu asset-uri reale, bonusul de cinematice. După P5,
`RubricValidator` nu mai are itemi „planificat”, cu excepția co-op-ului.

## P6: polish și playtest cronometrat (14–18 h)

**Livrabile**
- Playtest complet cronometrat: Hub + 5 niveluri ≥ 30 de minute la prima trecere. Durata pe nivel se notează în
  `SETUP_REPORT`/`ROADMAP` și dificultatea se ajustează.
- Profilare pe hardware țintă (Iris Xe, 1080p): 60 FPS stabil, fără alocări per cadru în gameplay (Profiler + Memory Profiler).
- Juice: hit-stop, tranziții, feedback UI, accesibilitate (subtitrări la dialoguri, mărimea textului).
- Rezolvarea bug-urilor din playtest; toate textele revizuite în `Strings.cs`.

**Barem închis:** verificarea finală a tuturor celor 12 puncte în build.

## P7: co-op asimetric online, bonus (20–28 h)

**Livrabile**
- Netcode for GameObjects + Unity Relay: host/join cu cod.
- **P1 Infiltrator** joacă platformerul. **P2 Operator** are o hartă de ansamblu, deschide uși, pune camerele în buclă și
  marchează gardienii.
- Stare sincronizată: modelele (`TraceMeter`, uși, terminale) oglindite în `NetworkVariable`/RPC prin
  `IInfiltratorRole`/`IOperatorRole`.
- Meniul „Co-op” cu creare și intrare în sesiune.

**Barem închis:** bonusul multiplayer.

## P8: build final și Google Drive (4–6 h)

**Livrabile**
- `./scripts/test.ps1`, `RubricValidator`, `./scripts/package-for-drive.ps1 -Build`.
- Smoke pe un PC curat (dezarhivare, rulare, 30 de minute de joc).
- Urcarea pe Google Drive a celor două arhive și a unui README de prezentare; tag `v1.0`.

## Datorie tehnică urmărită

- Regenerarea conținutului rescrie fileID-urile sub-asset-urilor (controllere, profile de volum), ceea ce produce diff-uri
  mari. Opțional: ID-uri deterministe sau regenerare doar la schimbare.
- `AlarmRaised` nu are încă abonați (P3).
- `RubricValidator` numără dovezi, nu calitate: demonstrațiile din RUBRIC.md rămân verificarea finală.
