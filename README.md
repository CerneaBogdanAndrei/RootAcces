# Root Access

Joc 2D side-scrolling de tip **stealth-platformer** într-un oraș cyberpunk. Ești un hacker care se strecoară pe
acoperișuri, în birouri, ferme de servere și canalizări, sparge terminale și fură date. Poți juca singur sau în
**co-op asimetric**: un Infiltrator și un Operator care vede harta și îl ajută de la distanță. Proiect pentru
disciplina „Joc 2D de tip singleplayer/multiplayer în Unity cu C#”.

![Root Access](Documentatie/screenshots/01-meniu-principal.png)

- **Motor:** Unity 6000.3.29f1 (6.3 LTS), URP 2D, Input System, Cinemachine 3, Timeline, 2D Animation, Netcode for GameObjects.
- **Platformă:** Windows x64 (tastatură + mouse sau gamepad).
- **Conținut:** meniu, intro cinematic, ascunzătoarea (Hub), 6 niveluri, final cinematic, credite, co-op local + LAN,
  scena „Prezentare barem”, 18 jurnale de date ascunse în niveluri. Estimarea pentru o primă trecere: ≈ 66 de minute.
- **Stare:** `v1.2` (nivelul 6, antena Halden: nacele, ventilatoare, grilaje care cad, transmisia finală; grafică pixel
  art pictată din cod, jurnale de date). Toate cele 12 puncte din barem și ambele bonusuri (cinematice, multiplayer)
  trec în `RubricValidator`; 124 de teste EditMode și 46 PlayMode sunt verzi.

**Documentația completă a jocului:** [Documentatie/DOCUMENTATIE.md](Documentatie/DOCUMENTATIE.md).

| ![Nivelul 1](Documentatie/screenshots/09-nivel1-parc.png) | ![Hacking](Documentatie/screenshots/11-hacking-pattern-memory.png) |
|---|---|
| ![Nivelul 4: apă cu refracție](Documentatie/screenshots/30-nivel4-apa.png) | ![Co-op pe ecran împărțit](Documentatie/screenshots/38-coop-ecran-impartit.png) |
| ![Terminalul personal](Documentatie/screenshots/05-terminal-abilitati.png) | ![Intro](Documentatie/screenshots/34-cutscene-intro-titlu.png) |

| Document | Conținut |
|---|---|
| [Documentatie/DOCUMENTATIE.md](Documentatie/DOCUMENTATIE.md) | povestea, controalele, nivelurile, mecanicile, co-op-ul, arhitectura, baremul, testele, livrarea |
| [RUBRIC.md](RUBRIC.md) | fiecare punct din barem: unde e, ce scripturi, cum se demonstrează în 30 s |
| [ARCHITECTURE.md](ARCHITECTURE.md) | module, asmdef-uri, fluxul de evenimente, formatul de salvare, pipeline-ul de conținut |
| [ROADMAP.md](ROADMAP.md) | etapele P1–P11 și ce s-a făcut în fiecare |
| [RELEASE_REPORT.md](RELEASE_REPORT.md) | starea la livrare: teste, barem, build, arhive, limitări |
| [ASSETS.md](ASSETS.md) | pachete de grafică/sunet recomandate și licențele lor |
| [CLAUDE.md](CLAUDE.md) | reguli pentru sesiunile Claude Code |

## Cum joci

### Build-ul gata făcut

1. Dezarhivează `RootAccess_Windows.zip`.
2. Pornește `RootAccess\RootAccess.exe`.
3. Din meniu:
   - **Joc nou**: intro, apoi ascunzătoarea;
   - **Co-op**: Local / Găzduiește în LAN / Alătură-te pe IP;
   - **Prezentare barem**: o cameră pentru fiecare punct.

### Din Unity Editor

1. Instalează Unity Hub și editorul **6000.3.29f1**, cu modulul *Windows Build Support (Mono)*.
2. În Unity Hub: *Add project from disk* → folderul `RootAccess` (nu rădăcina repo-ului). Dacă pornești din
   `RootAccess_Project.zip`, dezarhivează-l aproape de rădăcina discului (de exemplu `C:\Proiecte\`), ca să eviți căile
   mai lungi de 260 de caractere.
3. Deschide `Assets/_Project/Scenes/Bootstrap/Bootstrap.unity` și apasă Play (orice scenă pornește și direct).

## Controale

| Acțiune | Tastatură + mouse | Gamepad |
|---|---|---|
| Mișcare | A / D (sau săgeți) | stick stâng / D-pad |
| Săritură (ținută = mai sus), dublu salt, wall-jump | Space | A / Cross |
| Dash | Shift stânga / click dreapta | RB / R1 |
| Ghemuire (fără zgomot) | Ctrl stânga / C / S | B / Circle |
| Interacțiune (terminal, nod, lift, consolă) | E | X / Square |
| Grenadă EMP (spre cursor / stick-ul drept) | G / click stânga | RT / R2 |
| Terminal personal | Tab / I | Select |
| Pauză | Esc | Start |
| Panoul de barem | F1 | — |
| Hacking (în overlay) | mouse sau săgeți + Enter, Esc = renunță | D-pad + A, B = renunță |
| Sari peste cutscene | Enter / Esc | A / B |
| Operator (co-op local) | Numpad 8 / 2 / 5 | D-pad sus / jos, A |

Tastele pentru Săritură și Interacțiune se pot schimba din **Setări → Controale**.

## Linia de comandă (PowerShell, din rădăcina repo-ului)

Închide proiectul în Unity Editor înainte să rulezi scripturile, pentru că rulează Unity în batchmode.

```powershell
./scripts/build.ps1 -RebuildContent      # regenerează asset-urile și scenele, apoi face build-ul .exe
./scripts/test.ps1                       # teste EditMode + PlayMode (XML în RootAccess/Logs/TestResults)
./scripts/smoke-player.ps1               # pornește exe-ul pe scene, face capturi și verifică Player.log
./scripts/capture.ps1                    # capturile din Documentatie/screenshots
./scripts/coop-lan-smoke.ps1             # co-op LAN: gazdă + client pe 127.0.0.1, verifică ușa deschisă de Operator
./scripts/package-for-drive.ps1 -Build   # dist/RootAccess_Project.zip + dist/RootAccess_Windows.zip
./scripts/unity-batch.ps1 -Step rubric -ExecuteMethod RootAccess.EditorTools.RubricValidator.RunBatch
```

Din Editor, aceleași operații sunt în meniul **Root Access** (Build All Content, Build Windows x64, Validate Rubric).

## Livrare pe Google Drive

Rulează `./scripts/package-for-drive.ps1 -Build` și urcă ambele fișiere din `dist/`:

- `RootAccess_Project.zip`: tot proiectul (cod, asset-uri, setări, scripturi, documentație), fără folderele pe care
  Unity le regenerează (`Library`, `Temp`, `Logs`, `UserSettings`, `Builds`). Se deschide direct în Unity 6000.3.29f1.
- `RootAccess_Windows.zip`: jocul gata de rulat.

## Autor

Cernea Bogdan-Andrei. Tot codul este scris de noi. Grafica (pixel art cu hărți de normale) și sunetele sunt generate
din cod (`Editor/Generation/Art`, `ProceduralAudio`); pachetele externe opționale se importă în `Assets/ThirdParty`
(vezi [ASSETS.md](ASSETS.md)).
