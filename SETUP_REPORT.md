# SETUP_REPORT: Root Access v0.1-setup

Data: 25.11.2026 · Repo: `D:\ProiectSPV` (branch `main`, tag `v0.1-setup`) · Proiect Unity: `D:\ProiectSPV\RootAccess`

## Rezumat

Mediul, arhitectura și un vertical slice jucabil sunt gata. Am verificat totul automat:

| Verificare | Rezultat |
|---|---|
| Deschidere în batchmode, proiectul de lucru | ✅ exit 0, 0 erori, 0 warning-uri de compilare (`Logs/90-clean-open.log`) |
| Deschidere de la zero din `RootAccess_Project.zip` (fără Library, import complet) | ✅ exit 0, 0 erori de compilare sau import (`Logs/91-clean-open-from-zip.log`) |
| Teste EditMode | ✅ 92/92 |
| Teste PlayMode | ✅ 10/10, inclusiv fluxul complet de acceptare al Fazei D |
| Build Windows x64 | ✅ `RootAccess/Builds/Windows/RootAccess.exe`, 108 MB |
| Smoke pe exe (5 scene, capturi + Player.log) | ✅ MainMenu, Hub, Level01_Rooftops, RubricShowcase, Credits: fără excepții |
| RubricValidator | ✅ 46/46 verificări obligatorii; 6 itemi de roadmap deschiși (mai jos) |
| Arhive pentru Drive | ✅ `dist/RootAccess_Project.zip` (~4,8 MB) și `dist/RootAccess_Windows.zip` (~39 MB) |

## 1. Versiuni instalate

| Componentă | Versiune / cale |
|---|---|
| Unity Editor | **6000.3.29f1 (6.3 LTS)**, `C:\Program Files\Unity\Hub\Editor\6000.3.29f1\Editor\Unity.exe`, cu Windows Build Support (Mono) |
| Unity Hub / Unity CLI | 4.0.1 (MSIX, winget) / 1.0.1 |
| Licență | Unity Personal, activă |
| git / git-lfs | 2.55.0 / 3.7.1 (LFS pentru png, psd, jpg, wav, mp3, ogg, fbx, ttf, otf, exr, mp4; 162 de fișiere în LFS) |
| .NET SDK | 10.0.300 |
| IDE | VS Code 1.145.0 (extensia Unity nu e instalată: `code --install-extension visualstudiotoolsforunity.vstuc`), Visual Studio 2026 Community |

Pachete (`Packages/manifest.json`): URP 17.3.0, 2D Animation 13.0.6, 2D Sprite 1.0.0, 2D Tilemap 1.0.0 + Extras 6.0.3,
2D Pixel Perfect 5.1.1, Shader Graph 17.3.0, Input System 1.20.1, Cinemachine 3.1.8, Timeline 1.8.13, UGUI 2.0.0 (TMP),
Netcode for GameObjects 2.14.0, Transport 2.7.4, Multiplayer Services 2.4.1, Test Framework 1.6.0, IDE Visual Studio 2.0.26.

## 2. Ce s-a configurat

- **Proiect** (`Editor/ProjectSetup.cs`): URP cu 2 renderere (0 = Renderer2D pentru gameplay, 1 = UniversalRenderer pentru
  meniu, credite și camera de cer), asignat în Graphics și în toate nivelurile de Quality; company „CerneaBogdanAndrei”,
  product „Root Access”, Linear, 1920×1080 redimensionabil, Windows x64 Mono, doar Input System; Force Text + Visible
  Meta; tag-uri, 8 sorting layers, 9 layere fizice, matrice de coliziuni 2D (36 de perechi ignorate); TMP Essentials.
- **Cod:** 13 asmdef-uri (Core, Input, Systems, Audio, VFX, Player, AI, Gameplay, Hacking, UI, Cinematics, Net, Editor)
  + 2 de test, fără cicluri. Graful e în [ARCHITECTURE.md](ARCHITECTURE.md).
- **Pipeline de conținut** (batchmode, fără YAML scris de mână): 74 de sprite-uri, 45 de normal maps, skybox cu 6 fețe,
  35 de clipuri audio procedurale; 16 materiale, 7 shadere HLSL, AudioMixer (5 grupuri, 3 snapshot-uri, 4 parametri
  expuși), 7 Animator Controllers / 24 de clipuri cu Animation Events, 57 de prefab-uri (9 variante), 6 scene în Build Settings.
- **Scripturi CLI** (`scripts/`): `build.ps1`, `test.ps1`, `smoke-player.ps1`, `package-for-drive.ps1`, `unity-batch.ps1`, `common.ps1`.
- **Documentație:** CLAUDE.md, README.md, ARCHITECTURE.md, RUBRIC.md, ROADMAP.md, ASSETS.md, acest raport.

## 3. Rezultate pe faze

| Fază | Rezultat | Commit |
|---|---|---|
| A: mediu | ✅ Unity 6.3 LTS instalat și licențiat, unelte verificate | `7c9f372` |
| B: proiect + configurare | ✅ proiect creat în batchmode, pachete rezolvate, setări verificate | `7c9f372` |
| C: structură + asmdef | ✅ structura de foldere din specificație, 13 asmdef-uri, compilare curată | `4668aa4` |
| D: sisteme schelet + slice | ✅ toate sistemele cerute; fluxul de acceptare verificat automat (vezi E) | `4668aa4`, `32f5929` |
| E: teste | ✅ 92 EditMode + 10 PlayMode, toate verzi | `7eb5cac` + commitul final |
| F: build + livrare | ✅ BuildScript, build.ps1, test.ps1, package-for-drive.ps1, RubricValidator | `48a9743` |
| G: control de versiune | ✅ git + LFS, commit pe fiecare fază, tag `v0.1-setup` | toate |
| H: documentație | ✅ 6 documente | `2339e9c` |
| I: verificare finală | ✅ acest raport | `v0.1-setup` |

### Probleme găsite și rezolvate în sesiunea a doua

1. **Jucătorul invizibil și „Viață 0/5” în Hub/Level01.** Cauza: în batchmode, `CompositeCollider2D`-ul tilemap-urilor era
   salvat fără geometrie (`m_CompositePaths: []`). Podeaua se desena, dar n-avea coliziune, așa că jucătorul cădea sub
   kill-plane și murea în timpul ecranului de încărcare. Fix: `SceneBuilder.BakeTilemapColliders` generează explicit
   geometria înainte de salvare și oprește build-ul dacă un tilemap solid rămâne fără căi. Testul PlayMode
   `Scene_LoadsAndRunsWithoutErrors` verifică acum, pe fiecare scenă, că jucătorul stă pe podea.
2. Barele HUD erau corecte: arătau goale pentru că viața era 0. Acum se umplu normal.
3. Titlul „ASCUNZĂTOAREA” din Hub și etichetele din RubricShowcase se suprapuneau cu HUD-ul. Le-am mutat.
4. Skybox-ul din Level01 (camera de cer → RenderTexture → quad) se vede în spatele parallax-ului. MainMenu/Credits sunt OK.
5. Drona verifica linia de vedere spre tălpile jucătorului, adică pe suprafața podelei. Acum ținește pieptul.
6. Scena Bootstrap n-avea `AudioListener` (mii de warning-uri la pornire). Am adăugat unul.
7. Creditele începeau cu 900 px sub ecran (ecran gol ~20 s). Acum pornesc imediat.
8. `smoke-player.ps1` pierdea capturile cu `-OutDir` relativ. Am rezolvat.

### Fluxul de acceptare al Fazei D (verificat automat)

`VerticalSliceAcceptanceTests.MenuToShowcase_FullVerticalSliceFlow` rulează pe scenele, UI-ul și sistemele reale. Doar
input-ul jucătorului e simulat. Pașii: MainMenu → buton **Joc nou** → Hub (jucătorul pe podea) → Level01 → terminalul de
ușă → overlay → **Pattern Memory** rezolvat → ușa se deschide, starea revine la Playing → **Network Node** → teleport
pe acoperiș → **drona** te vede și primește un drum **A*** și se apropie → **pauză** (timeScale 0, meniul de pauză vizibil)
→ **volumul muzicii** scade în mixer → butonul **Meniu principal** din pauză → MainMenu → butonul **Prezentare barem** →
RubricShowcase. `HackingTerminalTests` acoperă celelalte două minijocuri: PortScan dezactivează camera de securitate,
iar WireConnect dă credite și un data shard.

## 4. RubricValidator (25.11.2026)

`./scripts/unity-batch.ps1 -Step rubric -ExecuteMethod RootAccess.EditorTools.RubricValidator.RunBatch`, cu raportul complet
în `RootAccess/Logs/RubricReport.md`.

| Punct din barem | Status | Verificări OK | Planificate |
|---|---|---|---|
| 1. Mișcare / săritură | **PASS** | 3/3 | 0 deschise |
| 2. Materiale / texturi | **PARTIAL** | 3/3 | 1 deschise |
| 3. Mediu: copaci / iarbă | **PARTIAL** | 3/3 | 1 deschise |
| 4. Apă / skybox | **PARTIAL** | 3/3 | 2 deschise |
| 5. Lumini / cameră | **PASS** | 6/6 | 0 deschise |
| 6. Prefab-uri | **PASS** | 3/3 | 0 deschise |
| 7. Particule / teleportare | **PASS** | 3/3 | 0 deschise |
| 8. Meniu | **PASS** | 3/3 | 0 deschise |
| 9. Sunet | **PASS** | 5/5 | 0 deschise |
| 10. Animații / Animator | **PASS** | 4/4 | 0 deschise |
| 11. Coliziuni / triggere | **PASS** | 4/4 | 0 deschise |
| 12. Scripturi | **PASS** | 3/3 | 0 deschise |
| Bonus: Cinematice | **PARTIAL** | 1/1 | 1 deschise |
| Bonus: Multiplayer co-op | **PARTIAL** | 1/1 | 1 deschise |
| Prezentare barem | **PASS** | 1/1 | 0 deschise |

Itemi deschiși (planificați, vezi ROADMAP.md):

| Punct | Ce lipsește | Etapă |
|---|---|---|
| 2 (obligatoriu) | ≥3 materiale **Shader Graph**: efectele există ca shadere HLSL scrise de mână | P5, recomandat mai devreme (sarcina 1) |
| 3 | copaci cu oase 2D Animation (SpriteSkin): acum se leagănă prin transform | P5 |
| 4 | shader de distorsiune subacvatică; nivelul Sewers (Level04) | P4 |
| Bonus cinematice | asset-uri Timeline intro + final (scripturile există) | P5 |
| Bonus co-op | co-op asimetric funcțional prin Relay (pachete + interfețe + bootstrap există) | P7 |

## 5. Limitări cunoscute

- **Conținut:** doar Hub + secțiunea de test din Nivelul 1, câteva minute de joc. Cele 30 de minute cerute vin în P3–P6.
- **Grafică și sunet placeholder**, generate procedural. Pachetele recomandate și regulile de import sunt în ASSETS.md.
- **Punctul 2 din barem e obligatoriu și încă parțial**, din cauza Shader Graph. Asta e cea mai importantă datorie față de barem.
- Grenada EMP nu se aruncă încă: acțiunea `Throw` și VFX-ul există, logica vine în P2. Inventarul, skill tree-ul și jurnalul
  de misiuni au UI schelet (text), deși logica e completă și testată.
- `AlarmRaised` e publicat, dar nu are încă abonați: gardienii care converg spre alarmă vin în P3.
- Performanța pe ținta din specificație (Intel Iris Xe, 1080p, 60 FPS) **nu e măsurată**: verificarea a rulat pe un PC cu
  RTX 3070. Profilarea pe ThinkPad e în P6.
- Regenerarea conținutului rescrie fileID-urile sub-asset-urilor (controllere, profile de volum), deci diff-uri mari dar
  inofensive după fiecare `BuildAll`.
- Scripturile cer ca proiectul să fie închis în Editor. Testele PlayMode rulează implicit cu device grafic (`-NoGraphics` opțional).
- Dezarhivarea proiectului într-o cale adâncă depășește limita Windows de 260 de caractere la importul pachetelor. Detalii în README.
- Switch-urile de automatizare `-ra-*` sunt active și în build-ul release. Sunt inofensive, dar pot fi limitate la build-urile de development.
- Urcarea efectivă pe Google Drive nu s-a făcut: arhivele sunt gata în `dist/`, iar livrarea finală e în P8.

## 6. Următoarele 5 sarcini (sesiunea următoare)

1. **Închide golul din punctul obligatoriu 2: Shader Graph.** Creează în Shader Graph Editor trei grafuri (Sprite Lit pentru
   Dissolve/Glitch, Sprite Unlit pentru Hologram și Neon Pulse), cu Custom Function nodes care apelează funcțiile din
   `Art/Shaders/RootAccessCommon.hlsl`. Apoi trece `AssetFactory` pe aceste shadere, rulează `build.ps1 -RebuildContent`
   și verifică cu `RubricValidator` că punctul 2 devine PASS. Criteriu: 3 fișiere `.shadergraph`, testele verzi și camera 2
   din showcase arată la fel.
2. **Pipeline pentru artă reală.** Adaugă `ArtOverrides` (ScriptableObject nume → sprite) citit de `GeneratedArt.Sprite()`,
   importă primul pachet (jucător + tileset de acoperiș) în `Assets/ThirdParty` conform ASSETS.md, regenerează conținutul
   și compară capturile din `smoke-player.ps1`.
3. **Grenada EMP (P2).** Consumă acțiunea `Throw`: proiectil din pool cu traiectorie, `VFX_EmpExplosion`, `ApplyHack(Disable)`
   pe toate `IHackable` din rază și decrementarea din inventar. Teste EditMode pentru rază și consum, plus un test PlayMode
   „EMP dezactivează camera și drona”.
4. **Inventar UI + Shop în Hub (P2).** Grilă cu sloturi, descriere și folosire. Shop-ul vinde EMP și chei USB pe credite
   (`Wallet.TrySpend`). Teste EditMode pentru cumpărare (fonduri insuficiente, inventar plin).
5. **Nivelul 1 complet (P3).** Extinde `SceneBuilder.BuildLevel01` peste secțiunea de test, la 6–8 minute, cu indicii de
   tutorial prin `DialogueZone`. Abonează gardienii la `AlarmRaised` și adaugă un test PlayMode „nivelul se poate termina”
   (traseu scriptat până la `LevelExit` → LevelComplete).

## 7. Cum reproduci verificarea

```powershell
# din D:\ProiectSPV, cu Unity închis
./scripts/test.ps1                                   # 92 + 10 teste
./scripts/unity-batch.ps1 -Step rubric -ExecuteMethod RootAccess.EditorTools.RubricValidator.RunBatch
./scripts/build.ps1                                  # RootAccess/Builds/Windows/RootAccess.exe
./scripts/smoke-player.ps1                           # capturi în RootAccess/Logs/smoke
./scripts/package-for-drive.ps1                      # dist/*.zip
```

`PROGRES_NEXTPROMPT.md` descrie starea de la finalul primei sesiuni și e păstrat doar ca istoric. Starea curentă e în
acest raport.
