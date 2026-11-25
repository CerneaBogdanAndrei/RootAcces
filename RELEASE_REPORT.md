# Root Access v1.0: raport de livrare

Data: 25.11.2026 · Tag: `v1.0` · Unity 6000.3.29f1 · Repo: https://github.com/CerneaBogdanAndrei/RootAcces

Raportul descrie starea proiectului la livrare. Setup-ul inițial e documentat în [SETUP_REPORT.md](SETUP_REPORT.md)
(`v0.1-setup`), iar jocul complet în [Documentatie/DOCUMENTATIE.md](Documentatie/DOCUMENTATIE.md).

## Ce s-a livrat, pe etape (PR-uri integrate în `main`)

| PR | Etapă | Conținut |
|---|---|---|
| #1 | Shader Graph | grafurile pentru hologramă, dizolvare, neon pulse și iarba cu vânt, generate din noduri reale |
| #2 | P2 | terminalul personal (inventar, abilități, misiuni, magazin, contracte), 8 abilități cu efecte, grenada EMP |
| #3 | P3 | constructorul de niveluri pe segmente, Nivelul 1 refăcut, Nivelul 2, auzul gardienilor, alarme, lunetist, gropi, lanțul de misiuni, tab-ul Călătorie |
| #4 | P4 | Nivelurile 3–5 (ferma de servere, canalizarea cu stavilă, sediul), apa cu refracție (Shader Graph) |
| #5 | P5 | copaci cu oase (2D Animation, SpriteSkin), cutscene-uri Timeline (intro și final) |
| #6 | P6 | timpul pe nivel și recordurile, XP în HUD, briefing în Hub, raportul de durată estimată |
| #7 | P7 | co-op asimetric: ecran împărțit local + LAN (Netcode), consola Operatorului |
| #8 | P8 | documentația finală cu capturi, README, acest raport, tag-ul `v1.0`, arhivele |

## Verificări la livrare

| Verificare | Rezultat |
|---|---|
| `scripts/test.ps1` | EditMode **114/114**, PlayMode **38/38** |
| `scripts/build.ps1 -RebuildContent` | conținutul se regenerează fără erori; `RootAccess.exe` (~110 MB folder de build) |
| `RubricValidator` | **toate cele 12 puncte PASS**, plus **Cinematice** și **Multiplayer**, plus „Durată ≥ 30 min” și „Prezentare barem”; nicio verificare planificată rămasă |
| `scripts/coop-lan-smoke.ps1` | **8/8**: gazda ascultă, clientul se conectează, primește harta și țintele, ușa se deschide la comanda Operatorului, fără excepții |
| `scripts/capture.ps1` | toate capturile din `Documentatie/screenshots` (1920×1080), fără excepții în log-uri |
| Durata estimată | ≈ 52 de minute (Hub + 5 niveluri + cutscene-uri), din `Logs/PlaytimeEstimate.md` |

## Arhive pentru Google Drive

Generate cu `./scripts/package-for-drive.ps1 -Build` în `dist/` (în afara git):

| Fișier | Conținut | Mărime |
|---|---|---|
| `RootAccess_Project.zip` | proiectul Unity complet, fără `Library`/`Temp`/`Logs`/`Builds` | 40,1 MB (1477 fișiere) |
| `RootAccess_Windows.zip` | jocul gata de rulat (`RootAccess\RootAccess.exe`) | 40,5 MB (241 fișiere) |

## Ce rămâne de făcut de dezvoltator

1. **Urcarea arhivelor pe Google Drive** (cele două fișiere din `dist/`) și partajarea link-ului.
2. **O trecere completă cronometrată**: timpii reali apar pe ecranul „Misiune îndeplinită” al fiecărui nivel.
3. **Co-op pe două PC-uri**: la prima pornire a gazdei, Windows Firewall cere permisiunea pentru `RootAccess.exe`
   (portul 7777). Testul automat rulează pe același PC.
4. Opțional: pachetele reale de grafică și sunet din [ASSETS.md](ASSETS.md), în locul placeholderelor procedurale.
5. Pe PC-ul de dezvoltare, setările salvate au volumul general la ~1% și muzica la 0%. Nu e un bug; se resetează din Setări.

## Limitări cunoscute

- Grafica și sunetele sunt generate procedural (placeholdere coerente, dar simple).
- Co-op-ul LAN folosește IP direct (fără Relay/join prin internet).
- Dificultatea și echilibrarea nu au trecut printr-un playtest cu alți jucători.
