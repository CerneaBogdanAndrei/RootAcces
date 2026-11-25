# Root Access

Joc 2D side-scrolling de tip stealth-platformer, într-un oraș cyberpunk. Ești un hacker care se strecoară în clădiri,
sparge terminale și fură date. Proiect pentru disciplina „Joc 2D de tip singleplayer/multiplayer în Unity cu C#”.

- **Motor:** Unity 6000.3.29f1 (6.3 LTS), Universal Render Pipeline 2D, Input System, Cinemachine 3.
- **Platformă:** Windows x64 (tastatură + mouse sau gamepad).
- **Stare:** vertical slice jucabil (meniu → Hub → secțiunea de test din Nivelul 1 → scena de prezentare a baremului).
  Conținutul complet (5 niveluri, minimum 30 de minute de joc) urmează etapele din [ROADMAP.md](ROADMAP.md).

| Document | Conținut |
|---|---|
| [RUBRIC.md](RUBRIC.md) | fiecare punct din barem: unde e, ce scripturi, cum se demonstrează în 30 s |
| [ARCHITECTURE.md](ARCHITECTURE.md) | module, asmdef-uri, fluxul de evenimente, formatul de salvare |
| [ROADMAP.md](ROADMAP.md) | etapele P1–P8 până la livrarea finală |
| [ASSETS.md](ASSETS.md) | pachete de grafică/sunet recomandate și licențele lor |
| [SETUP_REPORT.md](SETUP_REPORT.md) | ce s-a instalat și configurat, rezultate, limitări |
| [CLAUDE.md](CLAUDE.md) | reguli pentru sesiunile Claude Code |

## Cum joci

### Varianta rapidă (build-ul gata făcut)

1. Dezarhivează `RootAccess_Windows.zip`.
2. Pornește `RootAccess\RootAccess.exe`.
3. Din meniu: **Joc nou** pornește din Hub. **Prezentare barem** deschide scena cu câte o cameră pentru fiecare punct din barem.

### Din Unity Editor

1. Instalează Unity Hub și editorul **6000.3.29f1**, cu modulul *Windows Build Support (Mono)*.
2. În Unity Hub: *Add project from disk* → folderul `RootAccess` (nu rădăcina repo-ului). Dacă pornești din
   `RootAccess_Project.zip`, dezarhivează-l aproape de rădăcina discului (de exemplu `C:\Proiecte\`). La primul import
   Unity creează `Library\PackageCache` (circa 2 GB), iar căile mai lungi de 260 de caractere dau erori de import pe Windows.
3. Deschide `Assets/_Project/Scenes/Bootstrap/Bootstrap.unity` și apasă Play. Orice altă scenă pornește și direct, pentru că
   sistemele persistente (`SystemsRoot`) se încarcă automat.

## Controale

| Acțiune | Tastatură + mouse | Gamepad |
|---|---|---|
| Mișcare | A / D (sau săgeți) | stick stâng / D-pad |
| Săritură (ținută = mai sus), dublu salt, wall-jump | Space | A / Cross |
| Dash | Shift stânga / click dreapta | RB / R1 |
| Ghemuire | Ctrl stânga / C / S | B / Circle |
| Interacțiune (terminal, nod de rețea, platformă de teleport) | E | X / Square |
| Aruncă grenadă EMP (spre cursor / stick-ul drept) | G / click stânga | RT / R2 |
| Terminal personal: inventar, abilități, misiuni | Tab / I | Select |
| Pauză | Esc | Start |
| Panoul de barem | F1 | — |
| Hacking (în overlay) | mouse sau săgeți + Enter, Esc = renunță | D-pad + A, B = renunță |

Tastele pentru Săritură și Interacțiune se pot schimba din **Setări → Controale**. Tot din Setări se reglează volumul
(general, muzică, efecte, interfață), rezoluția, fullscreen-ul și VSync.

## Linia de comandă (PowerShell, din rădăcina repo-ului)

Închide proiectul în Unity Editor înainte să rulezi scripturile, pentru că rulează Unity în batchmode.

```powershell
./scripts/build.ps1 -RebuildContent    # regenerează asset-urile și scenele, apoi face build-ul .exe
./scripts/build.ps1                    # doar build-ul .exe (RootAccess/Builds/Windows/RootAccess.exe)
./scripts/test.ps1                     # teste EditMode + PlayMode, cu sumar (XML în RootAccess/Logs/TestResults)
./scripts/smoke-player.ps1             # pornește exe-ul pe fiecare scenă, face capturi și verifică Player.log
./scripts/package-for-drive.ps1 -Build # dist/RootAccess_Project.zip + dist/RootAccess_Windows.zip
./scripts/unity-batch.ps1 -Step rubric -ExecuteMethod RootAccess.EditorTools.RubricValidator.RunBatch
```

Din Editor, aceleași operații sunt în meniul **Root Access** (Build All Content, Build Windows x64, Validate Rubric).
Testele rulează și din *Window → General → Test Runner*.

## Livrare pe Google Drive

Rulează `./scripts/package-for-drive.ps1 -Build` și urcă ambele fișiere din `dist/`:

- `RootAccess_Project.zip`: tot proiectul (cod, asset-uri, setări, scripturi, documentație), fără folderele pe care
  Unity le regenerează (`Library`, `Temp`, `Logs`, `UserSettings`, `Builds`). Se deschide direct în Unity 6000.3.29f1.
- `RootAccess_Windows.zip`: jocul gata de rulat.

## Autor

Cernea Bogdan-Andrei. Tot codul este scris de noi. Grafica și sunetele actuale sunt placeholdere generate procedural de
`PlaceholderArtGenerator`; pachetele externe se importă în `Assets/ThirdParty` (vezi [ASSETS.md](ASSETS.md)).
