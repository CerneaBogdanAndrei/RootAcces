# Pentru Claude: curățenie înainte de livrare

Utilizatorul vrea proiectul gata de încărcat (Google Drive / predare). Rămâne doar **jocul de bază** și documentația lui.
Se șterge tot ce ține de **asistentul AI** (Claude) și unealta neterminată din etapa P14. La final, șterge și acest folder.

Branch-ul curent e `feat/p14-playthrough`: are toate lucrările comise, inclusiv botul neterminat. `main` e la v1.4,
curat, cu P13. Unity Editor trebuie să fie închis; scripturile batch refuză să ruleze altfel.

## Atenție: „AI” din joc NU se șterge

`RootAccess/Assets/_Project/Scripts/AI/`, assembly-ul `RootAccess.AI`, `RootAccess.AI.csproj` și mențiunile de „AI” din
`ARCHITECTURE.md`, `RUBRIC.md`, `ROADMAP.md` și `Documentatie/DOCUMENTATIE.md` sunt **inteligența artificială a
inamicilor**: gardieni, drone, A*. Fac parte din joc și din barem. **Nu le atinge.**

## 0. Pregătire

1. Citește `CLAUDE.md` înainte să-l ștergi: are regulile proiectului (fără YAML scris de mână, comenzi, ce verifici în
   log-uri).
2. Fă un branch nou din cel curent: `git checkout -b chore/livrare`.

## 1. Șterge ce ține de asistentul AI

| Ce | Cum |
|---|---|
| `CLAUDE.md` | șterge fișierul |
| `NEXT_PROMPT.md` | șterge fișierul, după ce citești „Probleme deschise” de mai jos |
| `SETUP_REPORT.md` | șterge fișierul (raportul sesiunii de setup făcute de Claude) |
| `README.md` | scoate rândul de tabel `\| [CLAUDE.md](CLAUDE.md) \| reguli pentru sesiunile Claude Code \|` |
| `RELEASE_REPORT.md` | linia 5 trimite la `SETUP_REPORT.md`: reformulează fraza fără link |
| `ROADMAP.md` | linia ~137 pomenește `SETUP_REPORT`: reformulează fără el |
| `.gitignore` | scoate linia `.claude/settings.local.json` și comentariul de deasupra ei, dacă există |
| `.claude/` | dacă apare un astfel de folder în repo, șterge-l |
| `TO_DELETE/` | șterge-l la final, cu tot cu acest fișier |

Verificare: căutarea `grep -rIil "claude\|anthropic" --exclude-dir=.git --exclude-dir=Library --exclude-dir=Logs
--exclude-dir=Temp --exclude-dir=Builds --exclude-dir=dist .` nu trebuie să mai găsească nimic.

## 2. Șterge unealta neterminată (botul de playthrough din P14)

Nu face parte din joc. Toate modificările ei față de `main` sunt doar ale botului, așa că le poți anula direct:

```bash
git checkout main -- \
  RootAccess/Assets/_Project/Scripts/Editor/Generation/LevelLayout.cs \
  RootAccess/Assets/_Project/Scripts/Editor/SceneBuilder.cs \
  RootAccess/Assets/_Project/Scripts/Core/Dev/CommandLineAutomation.cs \
  RootAccess/Assets/_Project/Scripts/Player/PlayerHealth.cs \
  RootAccess/Assets/_Project/Scripts/Hacking/AssemblyInfo.cs
git rm -r RootAccess/Assets/_Project/Scripts/Playthrough RootAccess/Assets/_Project/Scripts/Playthrough.meta \
  RootAccess/Assets/_Project/Scripts/Gameplay/World/RouteStep.cs RootAccess/Assets/_Project/Scripts/Gameplay/World/RouteStep.cs.meta \
  RootAccess/Assets/_Project/Scripts/Gameplay/World/LevelRoute.cs RootAccess/Assets/_Project/Scripts/Gameplay/World/LevelRoute.cs.meta \
  scripts/playthrough.ps1
```

- Șterge și `RootAccess/Logs/Playthrough/`, care e ignorat de git.
- `RootAccess/RootAccess.Playthrough.csproj` e generat de IDE: dispare singur sau îl ștergi.
- Apoi regenerează conținutul, ca scenele să piardă obiectele `Route`: `./scripts/build.ps1 -RebuildContent`.
- Citește log-urile (`RootAccess/Logs/*.log`): fără `error CS`, `Exception`, `Compilation failed` sau `[RootAccess][FAIL]`.

## 3. Ce PĂSTREZI

- Tot proiectul Unity: `RootAccess/Assets` (fără `Playthrough`), `Packages`, `ProjectSettings`.
- Generatoarele din `Scripts/Editor`: conținutul e generat din cod și fără ele proiectul nu se poate reface.
- Testele EditMode și PlayMode (sunt dovadă la punctul 12 din barem) și `RubricValidator`.
- Scripturile din `scripts/` (build, test, capture, smoke, package, co-op), mai puțin `playthrough.ps1`.
- Comutatoarele `-ra-*` din `CommandLineAutomation`: sunt pentru capturi și smoke-test, nu țin de AI.
- `Documentatie/`, `README.md`, `ARCHITECTURE.md`, `RUBRIC.md`, `ROADMAP.md`, `ASSETS.md`, `RELEASE_REPORT.md`.
- `Tests/PlayMode/InteractionTests.cs`: testează jocul, nu botul. Vezi punctul 4.

## 4. Probleme deschise (din `NEXT_PROMPT.md`; spune-le utilizatorului)

- **Posibil bug real.** Botul a stat pe terminalele și pe nodul din nivelul 1, dar `PlayerInteractor.Current` a rămas
  `null`: nu a apărut promptul „[E] …” și nu a pornit nicio spargere.
  - `InteractionTests` (2 teste, încă nerulate) verifică exact asta.
  - Rulează `./scripts/test.ps1 -Platform PlayMode -Filter InteractionTests`.
  - Dacă trec, problema era a botului: păstrează testele.
  - Dacă pică, e bug în joc: **nu șterge testele**. Spune-i utilizatorului și repară detecția înainte de livrare.
  - Context: `ProjectSetup` pune `Physics2D.queriesHitTriggers = false`, iar `PlayerInteractor` folosește
    `ContactFilter2D` cu `useTriggers = true`.
- **Dialogul.** În rularea botului, dialogul Operatorului nu a avansat la Enter. Cel mai probabil era de la bot (apăsări
  de un singur cadru), dar verifică repede în joc că Enter avansează dialogul de la începutul nivelului 1.

## 5. Verificare finală

1. `./scripts/build.ps1 -RebuildContent`.
2. `./scripts/test.ps1`. Așteptat: EditMode 136; PlayMode 53 dacă păstrezi `InteractionTests`, altfel 51. Totul verde.
3. `./scripts/unity-batch.ps1 -Step rubric -ExecuteMethod RootAccess.EditorTools.RubricValidator.RunBatch` → 16/16 PASS.
4. Actualizează numărul de teste în `README.md` și `Documentatie/DOCUMENTATIE.md` (secțiunea „Testare și calitate”),
   dacă s-a schimbat.
5. Commit („chore: pregătire pentru livrare, fără fișierele asistentului AI și fără botul de playthrough”), PR în
   română, merge în `main` după verificări.

## 6. Pachetul pentru încărcare

- `./scripts/package-for-drive.ps1 -Build` face `dist/RootAccess_Project.zip` și `dist/RootAccess_Windows.zip`.
- **Fără `-IncludeGit`**: istoria git rămâne în afara arhivei (două mesaje vechi de commit pomenesc Claude).
- **Nu urca nimic pe Google Drive până nu cere utilizatorul explicit.** Spune-i doar unde sunt arhivele și cât de mari
  sunt.
- Nu rescrie istoria git și nu șterge nimic de pe GitHub (commit-uri, descrieri de PR) fără acordul explicit al
  utilizatorului.
- Folderul de memorie al lui Claude, din afara repo-ului (`C:\Users\...\.claude\`), nu face parte din proiect: nu-l
  atinge.
