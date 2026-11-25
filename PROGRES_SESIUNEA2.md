# Root Access: progres sesiunea 2 + prompt pentru sesiunea 3

Data: 25.11.2026 · Repo local: `D:\ProiectSPV` · GitHub: https://github.com/CerneaBogdanAndrei/RootAcces (public, scris „RootAcces”)
Proiect Unity: `D:\ProiectSPV\RootAccess` (Unity 6000.3.29f1). `main` = `cb4f163` + acest fișier.

---

## 1. Reguli stabilite cu utilizatorul (de respectat)

- **Câte un PR pe etapă.** Pentru fiecare etapă: branch `feat/...` din `main`, apoi `scripts/test.ps1` + build + `RubricValidator`, apoi
  `gh pr create`, apoi `gh pr merge --squash --delete-branch`, apoi `git checkout main && git pull`. Merge doar după ce totul e verde.
  Descrierile PR-urilor se scriu în română.
- **Co-op:** ambele variante, **split-screen local** (P2 pe gamepad, același PC) și **LAN** (Netcode for GameObjects, host + join
  pe IP direct, fără Relay), cu roluri asimetrice: Infiltrator (platforming) și Operator (hartă, deschide uși, pune camere în buclă).
  Cele două moduri partajează același cod de Operator, în spatele unei abstracții de „link” (local / rețea).
- **La final** (după co-op):
  - folderul `Documentatie/` cu capturile necesare (`scripts/capture.ps1` le salvează implicit în `Documentatie/screenshots`);
  - `Documentatie/DOCUMENTATIE.md`, completă de la cap la coadă: descrierea jocului, poveste, controale, niveluri, mecanici,
    arhitectură, maparea pe barem, teste, build, livrare; utilizatorul o folosește ca bază pentru documentația oficială a jocului;
  - README-ul repo-ului actualizat, cu capturi.
- Fără YAML Unity scris de mână; totul se generează prin scripturi de Editor rulate în batchmode, cu proiectul închis în Unity.
- Interfața jocului e în română (textele stau în `Core/Strings.cs`); documentația e tot în română (CLAUDE.md în engleză).

## 2. Ce s-a făcut în sesiunea 2

### Partea 1: finalizarea setup-ului (tag `v0.1-setup`)
- **Bug major reparat:** în batchmode, podelele (tilemap) se salvau fără geometrie de coliziune, așa că jucătorul cădea și murea
  („Viață 0/5”). Fix: `SceneBuilder.BakeTilemapColliders`, care oprește build-ul dacă un tilemap solid n-are coliziune.
- Faza E: teste EditMode și PlayMode, inclusiv testul de acceptare pentru tot fluxul (meniu → Hub → L1 → hack → ușă → teleport →
  drona → pauză → volum → meniu → showcase).
- Faza F: `scripts/test.ps1`, `scripts/package-for-drive.ps1` (`dist/RootAccess_Project.zip`, `dist/RootAccess_Windows.zip`),
  `Editor/RubricValidator.cs` (meniul Root Access → Validate Rubric; raportul în `RootAccess/Logs/RubricReport.md`).
- Faza H: CLAUDE.md, README.md, ARCHITECTURE.md, RUBRIC.md, ROADMAP.md, ASSETS.md. Faza I: SETUP_REPORT.md.
- Alte fix-uri: linia de vedere a dronei, AudioListener în Bootstrap, creditele care apăreau după 20 s, etichetele care
  se suprapuneau cu HUD-ul.
- Deschiderea proiectului de la zero din zip a fost verificată. **Atenție la calea lungă:** dezarhivează aproape de rădăcina
  discului, altfel apar erori MAX_PATH la importul pachetelor.

### Partea 2: GitHub + etape
- Repo-ul a fost urcat (main + tag `v0.1-setup` + LFS), cu descriere și topic-uri setate.
- **PR #1 (merged): Shader Graph.** `Editor/Generation/ShaderGraphWriter.cs` + `ShaderGraphFactory.cs` generează `SG_Hologram`,
  `SG_SpriteDissolve`, `SG_NeonPulse` și `SG_GrassWind` (`Art/Shaders/Graphs`) din noduri reale. Fiecare graf e verificat la
  import cu `ShaderUtil.ShaderHasError`. Cele 4 HLSL-uri vechi au fost șterse. Punctul 2 din barem e acum PASS.
- **PR #2 (merged): P2.**
  - `UI/Screens/TerminalScreenView.cs`: terminal personal cu tab-urile Inventar, Abilități, Misiuni, Magazin, Contracte, plus
    `GameState.Terminal`, care oprește timpul. Se deschide cu Tab/I sau din consolele din Hub (`ShopTerminal`, `SkillTerminal`,
    `JobBoard` publică `TerminalScreenRequested`).
  - `Systems/Economy/Shop.cs` (ShopOffer / PurchaseResult), `ItemGrant` (2 EMP-uri la start), evenimentul `XpChanged`.
  - 8 skill-uri cu efecte reale: pași silențioși, camuflaj optic, overclock, a doua șansă, acces de la distanță, servo-picioare
    (dash), blindaj (+1 viață), condensatori EMP.
  - Grenada EMP: `Player/PlayerGrenadeThrower.cs` (G / LMB / RT) + `Gameplay/World/EmpGrenade.cs` (din pool) + interfața
    `IEmpTarget` pe cameră, dronă, turelă și gardian (stun).
  - Automatizare: `-ra-interact <Obiect>`, `-ra-command inventory,skills,quests,pause,rubric,settings,gameover`,
    `-ra-default-settings` (setări în memorie; scripturile nu ating PlayerPrefs). Scriptul nou `scripts/capture.ps1` are o
    listă de 33 de cadre pentru documentație.
- Stare verificată: **EditMode 98/98, PlayMode 18/18**, RubricValidator **47/52** (toate obligatoriile PASS), build OK, smoke 5/5.

### De știut (lecții tehnice noi)
- Pe PC-ul utilizatorului, PlayerPrefs-ul build-ului are volum general ~1%, muzică 0% și rezoluție 1280×960. Nu e un bug și
  nu trebuie modificat; utilizatorul poate reseta din Setări. Capturile folosesc `-ra-default-settings`.
- Heredoc-urile bash lungi cu Python se rup în Git Bash. Scriptul Python se scrie într-un fișier în scratchpad și se rulează cu `python -I fișier.py`.
- Avertismentele „CRLF will be replaced by LF” de la `git add` sunt inofensive.
- Shader Graph se scrie cu nodurile fără sloturi (Shader Graph le recreează la încărcare). Explicite rămân doar sloturile de
  Custom Function și constantele. Obiectele JSON sunt pe o singură linie, separate prin linie goală.
- În PlayMode, test runner-ul adaugă `InitTestScene…` în Build Settings; scenele se filtrează după `Assets/_Project/Scenes/`.

## 3. Ce a rămas (în ordine, câte un PR pentru fiecare)

### PR 3: P3, niveluri + AI (branch `feat/p3-levels`, deja creat local, gol)
1. **Constructor de niveluri pe segmente** (`Editor/Generation/LevelLayout.cs`). Helper-ele private din `SceneBuilder` (Gameplay,
   Ground, Fill, Place, Spawn, ConfigureTerminal, LinkNodes, Parallax, GameplayScene) devin `internal`. Segmente: Start, Parkour,
   WallJump (horn de 3 tile-uri), GuardPatrol (+HideZone, crate), CameraGate (cameră + Terminal_Camera + ușă + Terminal_Door,
   cu zid deasupra ușii), LaserCorridor (`Hazard_Laser` e IHackable, dezactivat de terminal), NodeHop (noduri spre un etaj
   superior, zid care blochează drumul), DroneArena (stâlpi + drone), Elevator (blocat; un terminal îl deblochează), DataVault
   (Terminal_Data + fragment de date), TurretHall (+pickup EMP), WaterPool (WaterBody + lăzi), Checkpoint, Exit.
   Un `PathfindingGrid` pe tot nivelul; limitele camerei cât nivelul.
2. **Level01 extins** la 6–8 minute și **Level02_Office** (interior, fără skybox: gardieni, camere, lift, lasere, sniper, seif
   de date, turelă). Ambele se adaugă în `SceneBuilder.BuildOrder` / Build Settings.
3. **Progresie:** `QuestDefinition.followUpQuest` (la finalizarea misiunii principale pornește următoarea), misiune principală
   pe fiecare nivel, `LevelExit.unlocksLevel` setat pentru fiecare nivel, plus tab-ul nou `TerminalTab.Travel` (FastTravelTerminal
   publică lista de `LevelDefinition`; acțiunea „Călătorește” face `GameManager.LoadScene`).
4. **AI:** gardienii aud zgomotul. Evenimentul nou `PlayerNoise(pos, radius)` e publicat de `FootstepPlayer` (doar pentru jucător,
   cu rază × volum, deci pașii silențioși contează), de aterizare și de EMP; gardianul intră în Suspicious spre poziție.
   Abonează gardienii la `AlarmRaised` (merg în Search la poziția alarmei dacă e în rază). Opțional: drone cu waypoint-uri.
5. Teste: PlayMode „nivelul se poate termina” (traseu scriptat sau teleport la ieșire și verificarea LevelComplete și a deblocării),
   EditMode pentru lanțul de misiuni, testele existente actualizate dacă se schimbă spawn-urile din L1 (`l1_nodes`, `l1_drone`,
   `l1_security` sunt folosite în teste și în `capture.ps1`).

### PR 4: P4, Nivelurile 3–5
- Level03_ServerFarm (drone, lanțuri de noduri, lasere, nucleul de servere), Level04_Sewers (apă pe tot nivelul, înot,
  lăzi plutitoare, puzzle cu stavila sau inundația, snapshot Underwater, **Shader Graph pentru distorsiunea apei** `SG_WaterDistortion`,
  numit „RootAccess/Water Distortion”, pentru că validatorul caută acest nume) și Level05_HQ (finalul, toate mecanicile; ieșirea
  duce la credite sau la cutscene).
- Sprite-uri placeholder noi, dacă e nevoie (rack de server, birou, țeavă), în `PlaceholderArtGenerator`.

### PR 5: P5
- Copaci cu oase 2D Animation (`SpriteSkin`, 3–4 oase; date setate prin `ISpriteEditorDataProvider` / bones + weights;
  `SpriteSkin.SetRootBone` / `SetBoneTransforms`), legănați de `FoliageWind`.
- Timeline: intro (după Joc nou) și final (după L5), create prin API (`TimelineAsset`, track-uri Animation/Cinemachine/Signal/Activation),
  cu `CutsceneController` + `SkipCutscene`. Validatorul cere ≥2 asset-uri Timeline.

### PR 6: P6, polish
- Estimarea duratei (tabel minute/nivel; utilizatorul trebuie să cronometreze o trecere completă), feedback, bug-uri,
  textele revizuite, poate HUD pentru XP.

### PR 7: co-op (split-screen + LAN)
- `Net/`: interfața `IOperatorLink` cu două implementări, `LocalOperatorLink` (split-screen: a doua cameră sau panou UI pe
  jumătatea dreaptă, input de pe gamepad prin `PlayerInputManager` sau un al doilea `InputReader`) și `NetworkOperatorLink`
  (NGO + UnityTransport `SetConnectionData(ip, 7777)`: host = Infiltrator, client = Operator; host-ul trimite snapshot-uri ale
  hărții prin CustomMessagingManager sau NetworkVariables, iar clientul trimite comenzi: deschide ușa X, pune camera Y în buclă).
- UI pentru Operator (hartă schematică: jucător, gardieni, camere, uși, terminale; butoane de acțiune) și un meniu Co-op
  (Local / Găzduiește LAN / Alătură-te pe IP). Validatorul: verificarea „Co-op asimetric funcțional” devine obligatorie.
- Teste: PlayMode pentru link-ul local (o comandă de Operator deschide ușa). LAN: test cu host + client în același proces, dacă
  e fezabil, altfel manual cu două instanțe ale exe-ului.

### PR 8: documentație finală + livrare
- `scripts/capture.ps1` → `Documentatie/screenshots/*.png` (eventual și cadre noi pentru nivelurile 2–5 și co-op).
- `Documentatie/DOCUMENTATIE.md` completă (vezi secțiunea 1), RUBRIC/ROADMAP/ARCHITECTURE/README actualizate (README cu
  capturi pentru pagina GitHub), un nou SETUP_REPORT (sau RELEASE_REPORT), tag `v1.0`, `package-for-drive.ps1 -Build`.

## 4. Comenzi utile
```powershell
./scripts/test.ps1                      # EditMode + PlayMode
./scripts/build.ps1 -RebuildContent     # regenerează conținutul + exe
./scripts/smoke-player.ps1              # 5 scene, capturi în RootAccess/Logs/smoke
./scripts/capture.ps1 -Only '0*' -OutDir RootAccess/Logs/capture/png -Width 1280 -Height 720
./scripts/unity-batch.ps1 -Step rubric -ExecuteMethod RootAccess.EditorTools.RubricValidator.RunBatch
gh pr create --repo CerneaBogdanAndrei/RootAcces --base main --head <branch> --title "..." --body-file <fișier>
gh pr merge <n> --repo CerneaBogdanAndrei/RootAcces --squash --delete-branch
```

---

## 5. PROMPT pentru sesiunea următoare (copiază tot blocul)

```
Continuăm proiectul Unity „Root Access” din D:\ProiectSPV (repo GitHub CerneaBogdanAndrei/RootAcces).
Citește ÎNTÂI integral: D:\ProiectSPV\PROGRES_SESIUNEA2.md (starea curentă, regulile stabilite cu mine, ce a rămas și
designul pentru fiecare etapă), apoi CLAUDE.md, ROADMAP.md și RUBRIC.md. SETUP_PROMPT.md e specificația originală.
Unity 6000.3.29f1; închide proiectul în Editor înainte de scripturi (rulează în batchmode).

Reguli: câte un PR pe etapă (branch din main → test.ps1 + build + RubricValidator verzi → gh pr create → merge squash →
pull main). Fără YAML scris de mână. Interfața și documentația sunt în română.

Continuă, în ordine:
1. PR 3 (P3): constructor de niveluri pe segmente, Level01 extins, Level02_Office, lanț de misiuni + tab-ul Călătorie,
   gardieni care aud zgomotul și reacționează la AlarmRaised, teste.
2. PR 4 (P4): Level03_ServerFarm, Level04_Sewers (apă + Shader Graph de distorsiune), Level05_HQ (final).
3. PR 5 (P5): copaci cu oase (SpriteSkin) și cutscene-uri Timeline (intro + final, care se pot sări).
4. PR 6 (P6): polish + tabel cu durata estimată pe nivel (țintă: ≥30 de minute în total).
5. PR 7: co-op asimetric, split-screen local ȘI LAN (Netcode, host/join pe IP), cu cod comun pentru Operator.
6. PR 8: folderul Documentatie/ cu capturi (scripts/capture.ps1) + Documentatie/DOCUMENTATIE.md completă de la cap la coadă,
   README-ul repo-ului cu capturi, tag v1.0, arhivele pentru Drive.
Dacă limita sesiunii se apropie, oprește-te după un PR integrat și actualizează PROGRES_SESIUNEA2.md (sau creează
PROGRES_SESIUNEA3.md) cu starea și promptul pentru sesiunea următoare.
La final răspunde-mi scurt, în română.
```
