# Root Access: progres sesiunea 3 (final)

Data: 25.11.2026 · Repo: https://github.com/CerneaBogdanAndrei/RootAcces · tag `v1.0`

## Ce s-a făcut în sesiunea 3 (câte un PR pe etapă, fiecare integrat doar cu teste, build și validator verzi)

| PR | Etapă | Rezumat |
|---|---|---|
| #3 | P3 | `LevelLayout` (niveluri pe segmente), Level01 refăcut, Level02_Office, auzul gardienilor (`PlayerNoise`, Investigate), răspunsul la alarmă, lunetistul cu proiectile, gropile (`FallZone`), lanțul de misiuni, tab-ul Călătorie. Bug-uri vechi reparate: gardienii nu patrulau; referințele la ScriptableObject-uri din scene se salvau goale. |
| #4 | P4 | Level03_ServerFarm (lanț de turnuri cu noduri), Level04_Sewers (bazine, scufundare, stavila care inundă puțul), Level05_HQ (final), `SG_WaterDistortion` (refracție din `_CameraSortingLayerTexture`). |
| #5 | P5 | Copaci cu oase (`SpriteRigFactory` + `SpriteSkin`), cutscene-uri Timeline `Intro` și `Finale` (Animation, Cinemachine, Activation, Signal; se pot sări). |
| #6 | P6 | Timpul pe nivel + recorduri salvate, XP în HUD, briefing în Hub, `Logs/PlaytimeEstimate.md` (≈52 min). |
| #7 | P7 | Co-op asimetric: ecran împărțit local + LAN (Netcode, IP direct), cod comun de Operator (`IOperatorLink`), consola Operatorului, meniul Co-op, `scripts/coop-lan-smoke.ps1`. |
| #8 | P8 | `Documentatie/DOCUMENTATIE.md` + capturi, README cu galerie, `RELEASE_REPORT.md`, tag `v1.0`, arhivele din `dist/`. |

Stare finală: **EditMode 114/114, PlayMode 38/38**. `RubricValidator`: toate cele 12 puncte + ambele bonusuri PASS.
Build OK, `coop-lan-smoke` 8/8.

## Ce rămâne de făcut de utilizator

1. Urcarea pe Google Drive a celor două arhive din `D:\ProiectSPV\dist\`.
2. O trecere completă cronometrată (timpii apar pe ecranul „Misiune îndeplinită”).
3. Testul co-op pe două PC-uri: Windows Firewall cere permisiunea pentru `RootAccess.exe` (portul 7777) la prima găzduire.
4. Opțional: pachete reale de artă și sunet (ASSETS.md) și documentația oficială pornind de la `Documentatie/DOCUMENTATIE.md`.

## Lecții tehnice noi (trecute și în CLAUDE.md)

- `EditorSceneManager.NewScene` poate descărca ScriptableObject-urile ținute în context: se folosește `BuildContext.Fresh(...)`.
- Raycast-urile care pornesc exact de pe suprafața unui collider nu-l detectează: sondele pornesc cu ~0,5 mai sus.
- `_CameraSortingLayerTexture` cere `m_UseCameraSortingLayersTexture` și `m_CameraSortingLayersTextureBound` (ID-ul unic al
  layer-ului) pe Renderer2DData; proprietatea din Shader Graph trebuie să fie neexpusă.
- Rig-uri de sprite: oasele și greutățile trec prin `ISpriteBoneDataProvider` + `ISpriteMeshDataProvider` (pixeli).
- Netcode: NetworkManager pe un obiect rădăcină, `EnableSceneManagement = false` când peer-urile nu au aceleași scene;
  asmdef-ul Net are nevoie de `Unity.Networking.Transport`.
- Heredoc-urile lungi se rup în Bash: scripturile Python se scriu în scratchpad și se rulează cu `python -I`.
