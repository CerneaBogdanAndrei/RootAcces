# Root Access: ce a rămas de făcut (prompt pentru sesiunea următoare)

Data: 26.11.2026 · Repo: `D:\ProiectSPV` · GitHub: https://github.com/CerneaBogdanAndrei/RootAcces · Unity 6000.3.29f1
Citește întâi `CLAUDE.md` (reguli: fără YAML scris de mână, batchmode cu Editorul închis, texte în `Strings.cs`/`SeedContent.cs`
în română + engleză, un branch + PR în română pe etapă, merge doar după teste + build + `RubricValidator`).

## Starea de acum

- `main` = `3390a9b` (P13 merge-uit), versiunea afișată **1.4**. Pe `main`: 136 teste EditMode și 51 PlayMode verzi,
  validatorul 16/16, 65 de capturi refăcute.
- Etape terminate în această sesiune:
  - **P11**: nivelul 6, Antena Halden.
  - **P12**: limba română/engleză din meniu (`Core/Localization/Loc.cs`, `LocalizedLabel`, câmpuri `...En` în asset-uri).
  - **P13**: co-op pe ecran împărțit fără suprapuneri (consola sub meniuri, `SplitScreenRegion`) și lista de obiective
    din HUD (`ObjectiveChecklist`, indiciul spre terminalul de călătorie).
- **Lucru neterminat pe branch-ul `feat/p14-playthrough`, necomis.** Nu am rulat testele pe el; doar build-ul trece.

## P14 în lucru: playthrough automat (botul care joacă jocul cap-coadă)

Scopul (cerut de utilizator): un bot care joacă tot jocul în build-ul real și raportează ce e în regulă și ce trebuie
modificat pe fiecare nivel, cu capturi.

### Ce e deja scris pe branch

- **Traseul fiecărui nivel**, generat de editor:
  - `Gameplay/World/RouteStep.cs` și `LevelRoute.cs`, date pure;
  - `LevelLayout.Route(...)`, apelat în fiecare segment (61 de apeluri), plus pasul pentru terminalul de date din
    nivelul 1 în `SceneBuilder`;
  - scenele sunt deja regenerate cu traseul.
- **Botul**, în `Scripts/Playthrough/` (assembly `RootAccess.Playthrough`, activ doar cu `-ra-playthrough`):
  - `BotKeyboard`: tastatură virtuală prin Input System;
  - `HackSolver`: rezolvă cele 3 minijocuri prin overlay; are `InternalsVisibleTo` în `Hacking/AssemblyInfo.cs`;
  - `PlaythroughReport`: scrie `Playthrough.md`;
  - `PlaythroughBot`: meniu → intro → ascunzătoare (contracte, călătorie) → nivelurile 1–6 → final → credite. Pune
    capturi, notează diagnostice și un istoric de mișcare (trace) în `Player.log`; unde nu trece, îl mută mai departe
    („assist”).
- **Comutatoare noi**:
  - `-ra-playthrough-level <Scenă>`, `-ra-playthrough-out <dir>`;
  - `-ra-invulnerable` (`CommandLineAutomation.Invulnerable`; `PlayerHealth` ignoră daunele și le scrie în log).
- `scripts/playthrough.ps1 [-Level Level01_Rooftops] [-Invulnerable]` → `RootAccess/Logs/Playthrough/...`.
- `Tests/PlayMode/InteractionTests.cs`: 2 teste noi, **încă nerulate**. Verifică faptul că jucătorul care stă pe un
  terminal sau pe un nod chiar îl selectează (`PlayerInteractor.Current`) și vede promptul. Testele existente apelează
  `Interact()` direct și sar peste calea asta.

### Ce a arătat botul pe nivelul 1 (3 rulări, invulnerabil)

- **Botul se mișcă și sare** (36 din 46 de pași singur), după ce am pus `InputSystem.settings.backgroundBehavior = IgnoreFocus`.
- **Posibil bug real**: stând pe terminalul camerei, pe terminalul laserelor sau pe nodul de rețea, `PlayerInteractor.Current`
  rămâne `null`, nu apare promptul „[E] …” și nu pornește nicio spargere.
  - Prima verificare: rulează `./scripts/test.ps1 -Platform PlayMode -Filter InteractionTests`.
  - Context: `ProjectSetup` pune `Physics2D.queriesHitTriggers = false` și `queriesStartInColliders = false`.
    `PlayerInteractor` folosește `ContactFilter2D { useLayerMask, layerMask, useTriggers = true }`.
  - Dacă testul pică, repară detecția și păstrează testul.
- **Dialogul rămâne blocat**: în nivelul 1, starea `Dialogue` a ținut ~50 s, deși botul apasă Enter (UI Submit) la 0,6 s.
  - Ipoteză: apăsările scurte ale botului (`Tap`, un singur cadru) nu ajung la acțiuni, în timp ce tastele ținute
    (A/D, Space) merg. Asta ar explica și E-ul.
  - Verifică abonându-te la evenimentele `InputReader` (`Resources.FindObjectsOfTypeAll<InputReader>()`) și numărând
    `UISubmitEvent` / `InteractEvent`.
  - Dacă problema e la tap, ține tasta 2–3 cadre.
  - Dacă nu e de la bot, e bug real în `DialogueBoxView.OnSubmit`, care cere `_group.alpha > 0`.
- **De calibrat la bot** (nu sunt bug-uri ale jocului):
  - diagnosticul cu `Physics2D.OverlapCircleAll` nu vede trigger-ele (din cauza `queriesHitTriggers = false`);
    folosește `ContactFilter2D` cu `useTriggers`;
  - urcarea în hornul de wall-jump (pasul 14) eșuează: botul rămâne jos;
  - săritura pe pasarela din patrulă (pasul 19) eșuează;
  - săritura peste zidul de 4 tile-uri din arena cu drone eșuează.
  - Istoricul de mișcare e în `RootAccess/Logs/Playthrough/Level01_Rooftops/Player.log`
    (`[Playthrough] Trace before step …`).
  - Aterizarea pe recuzită (AC, lăzi) e deja acceptată.

### Pașii rămași pentru P14

1. Rezolvă cele două probleme de mai sus: interacțiunea și dialogul / tap-urile.
2. Calibrează saltul, hornul și pasarela, până când nivelul 1 trece fără „Blocat” cu `-Invulnerable`.
3. Rulează fiecare nivel separat (`-Level Level02_Office` … `Level06_Spire`), întâi invulnerabil, apoi normal. La final
   rulează toată campania (`./scripts/playthrough.ps1`) și o trecere în engleză. Pentru engleză adaugă `-ra-language en`
   la argumentele din `playthrough.ps1`; e nevoie de un parametru nou.
4. Verifică vizual capturile (texte suprapuse sau tăiate, netraduse, zone prea întunecate, HUD aglomerat).
5. Scrie pentru utilizator lista „ce e OK / ce trebuie modificat” pe fiecare nivel, cu capturi. Repară ce e clar bug.
6. Încheie etapa:
   - regenerează (`./scripts/build.ps1 -RebuildContent`);
   - rulează testele (EditMode 136, PlayMode **53** cu `InteractionTests`), validatorul și capturile;
   - versiunea **1.5** în `BuildScript.GameVersion`;
   - documentează: ROADMAP (P14), CLAUDE.md (comutatoarele noi, `playthrough.ps1`, numărul de teste), ARCHITECTURE
     (assembly-ul `RootAccess.Playthrough`, traseul nivelurilor), DOCUMENTATIE (testare);
   - PR în română, merge.

## Alte lucruri rămase (în afara P14)

- **Google Drive**: nu urca nimic până nu cere utilizatorul. Arhivele din `dist/` sunt încă cele vechi (v1.0); le
  refaci cu `./scripts/package-for-drive.ps1 -Build` doar când le cere.
- O trecere cronometrată jucată de om (estimarea e ≈ 66 de minute; timpii pe nivel apar pe ecranul „Misiune îndeplinită”).
- `SETUP_REPORT.md` încă pomenește `SETUP_PROMPT.md`, care a fost șters (doar o mențiune istorică).
