# Root Access: documentația jocului

Joc 2D side-scrolling de tip **stealth-platformer** într-un oraș cyberpunk, realizat în Unity 6 cu C#, pentru disciplina
„Joc 2D de tip singleplayer/multiplayer în Unity cu C#”.

| | |
|---|---|
| **Gen** | stealth-platformer 2D cu hacking, single-player + co-op asimetric pentru doi jucători |
| **Motor** | Unity 6000.3.29f1 (6.3 LTS), Universal Render Pipeline 2D, Input System, Cinemachine 3, Timeline, 2D Animation, Netcode for GameObjects |
| **Platformă** | Windows x64; tastatură + mouse sau gamepad |
| **Durată** | Ascunzătoarea + 5 niveluri + cutscene-uri, aproximativ 52 de minute la prima trecere (estimare, vezi §11) |
| **Limbă** | interfața, dialogurile și documentația sunt în română |
| **Autor** | Cernea Bogdan-Andrei |
| **Cod sursă** | https://github.com/CerneaBogdanAndrei/RootAcces (tag `v1.0`) |

![Meniul principal](screenshots/01-meniu-principal.png)

## Cuprins

1. [Povestea](#1-povestea)
2. [Cum pornești jocul](#2-cum-pornești-jocul)
3. [Controale](#3-controale)
4. [Structura jocului](#4-structura-jocului)
5. [Nivelurile](#5-nivelurile)
6. [Mecanici](#6-mecanici)
7. [Co-op asimetric](#7-co-op-asimetric)
8. [Grafică, animație, sunet](#8-grafică-animație-sunet)
9. [Arhitectura tehnică](#9-arhitectura-tehnică)
10. [Maparea pe barem](#10-maparea-pe-barem)
11. [Durata de joc](#11-durata-de-joc)
12. [Testare și calitate](#12-testare-și-calitate)
13. [Build și livrare](#13-build-și-livrare)
14. [Limitări și pași rămași](#14-limitări-și-pași-rămași)
15. [Anexă: capturi de ecran](#15-anexă-capturi-de-ecran)

---

## 1. Povestea

Sectorul 7 e un oraș care nu doarme niciodată, iar rețeaua lui nu uită nimic. Corporațiile dețin serverele; tu ești un
hacker care deține cheile. Lucrezi din **ascunzătoare** (Hub) împreună cu **Operatorul**, o voce în cască care urmărește
camerele orașului și te ghidează prin fiecare misiune.

Povestea urmează un singur fir, misiune după misiune (fiecare deblochează nivelul următor):

1. **Primul contract**: un client anonim vrea datele unui laborator de pe acoperișurile Sectorului 7. Spargi ușa de
   serviciu, recuperezi fragmentul de date și ajungi la punctul de extracție.
2. **Turnul de birouri**: fragmentul conține o adresă, seiful de date al firmei **Halden Systems**, la etajul 2 al
   turnului. Repornești liftul de serviciu și spargi seiful.
3. **Ferma de servere**: seiful trimitea totul către ferma de servere a corporației, unde se află cheia de acces la
   sediu. Treci prin hale păzite de drone și spargi nucleul de servere.
4. **Sub oraș**: sediul e sigilat. Singura intrare nesupravegheată trece prin canalizare, pe sub ziduri, prin apă.
5. **Root Access**: ultimul pas, mainframe-ul din vârful sediului. Cu el, orașul întreg răspunde la comanda ta.

Jocul începe cu o scenă cinematică (intro) și se încheie cu un final cinematic urmat de credite.

| ![Intro](screenshots/34-cutscene-intro-titlu.png) | ![Finalul](screenshots/36-cutscene-final.png) |
|---|---|

## 2. Cum pornești jocul

### Build-ul gata făcut

1. Dezarhivează `RootAccess_Windows.zip`.
2. Pornește `RootAccess\RootAccess.exe`.
3. Din meniu: **Joc nou** (intro → ascunzătoarea), **Continuă** (ultima salvare), **Co-op**, **Setări**,
   **Prezentare barem**, **Credite**, **Ieșire**.

### Din Unity Editor

1. Unity Hub + editorul **6000.3.29f1** cu modulul *Windows Build Support (Mono)*.
2. *Add project from disk* → folderul `RootAccess`. Dacă pornești din `RootAccess_Project.zip`, dezarhivează-l aproape
   de rădăcina discului (de exemplu `C:\Proiecte\`), ca să eviți căile mai lungi de 260 de caractere.
3. Deschide `Assets/_Project/Scenes/Bootstrap/Bootstrap.unity` și apasă Play (orice altă scenă pornește și direct).

## 3. Controale

| Acțiune | Tastatură + mouse | Gamepad |
|---|---|---|
| Mișcare | A / D (sau săgeți) | stick stâng / D-pad |
| Săritură (ținută = mai sus), dublu salt, wall-jump | Space | A / Cross |
| Dash | Shift stânga / click dreapta | RB / R1 |
| Ghemuire (mers fără zgomot) | Ctrl stânga / C / S | B / Circle |
| Interacțiune (terminal, nod, lift, consolă) | E | X / Square |
| Grenadă EMP (spre cursor / stick-ul drept) | G / click stânga | RT / R2 |
| Terminal personal | Tab / I | Select |
| Pauză | Esc | Start |
| Panoul de barem | F1 | — |
| Minijocuri de hacking | săgeți / mouse + Enter, Esc = abandonează | D-pad + A, B = abandonează |
| Cutscene | Enter / Esc = sari peste | A / B |
| **Operator în co-op local** | Numpad 8 / 2 = alege ținta, Numpad 5 = execută | D-pad sus / jos, A = execută |

Tastele pentru Săritură și Interacțiune se pot schimba din **Setări → Controale**. Tot acolo se reglează volumul
(general, muzică, efecte, interfață), rezoluția, fullscreen-ul și VSync.

## 4. Structura jocului

```
Meniu principal ──Joc nou──► Intro (Timeline) ──► Ascunzătoarea (Hub)
       │                                              │  briefing, magazin, abilități, contracte, călătorie
       ├─Continuă──► ultima salvare                   ▼
       ├─Co-op──► Local / Găzduiește LAN / Alătură-te   Nivelul 1 ──► Nivelul 2 ──► … ──► Nivelul 5 ──► Final (Timeline) ──► Credite
       └─Prezentare barem (14 camere)                 (fiecare ieșire: „Misiune îndeplinită”, deblocare, salvare)
```

### Ascunzătoarea (Hub)

Bază între misiuni, cu un briefing la intrare și cinci console:

- **Panoul de contracte** (darknet): misiuni secundare, „Curier de date” și „Fantoma de pe acoperiș”.
- **Magazinul**: grenadă EMP (60 credite), cheie USB (90), exploit zero-day (250).
- **Terminalul de abilități**: singurul loc unde se cheltuie XP.
- **Consola de călătorie**: tab-ul *Călătorie* listează nivelurile 1–5. Cele blocate apar gri; ieșirea dintr-un nivel îl
  deblochează pe următorul.
- **Teleportul spre Prezentarea baremului.**

| ![Ascunzătoarea](screenshots/03-hub.png) | ![Călătorie](screenshots/22-terminal-calatorie.png) |
|---|---|

### Terminalul personal (Tab)

Oprește timpul și are tab-urile **Inventar**, **Abilități**, **Misiuni**, plus **Magazin**, **Contracte** sau
**Călătorie** când e deschis de la o consolă din Hub.

| ![Inventar](screenshots/04-terminal-inventar.png) | ![Abilități](screenshots/05-terminal-abilitati.png) |
|---|---|
| ![Magazin](screenshots/06-terminal-magazin.png) | ![Misiuni](screenshots/08-terminal-misiuni.png) |

### Progresie și salvare

- **Misiunea principală** e un lanț: când se termină misiunea unui nivel, pornește automat cea a nivelului următor
  (`QuestDefinition.followUpQuest`). Fiecare misiune are un obiectiv bonus opțional: „nu declanșa alarma”.
- **Recompense:** credite (magazin) și XP (abilități), din misiuni, terminale sparte și pickup-uri.
- **Salvare** automată la checkpoint-uri, la ieșirea din nivel și înainte de călătorie (JSON versionat în
  `%USERPROFILE%\AppData\LocalLow\CerneaBogdanAndrei\Root Access\Saves`). Se salvează: scena, checkpoint-ul, creditele, XP-ul, abilitățile,
  inventarul, misiunile, nivelurile deblocate, terminalele sparte și **cel mai bun timp pe fiecare nivel**.
- **Ecranul „Misiune îndeplinită”** arată timpul petrecut în nivel, recordul, timpul total, creditele, XP-ul și
  nivelul de urmărire.

## 5. Nivelurile

Nivelurile sunt construite din **segmente** reutilizabile (`Editor/Generation/LevelLayout.cs`), puse unul după altul
de la stânga la dreapta. Fiecare segment își face propria podea, așa că ordinea lor se poate schimba ușor.

| Segment | Ce cere de la jucător |
|---|---|
| Acoperișuri | sărituri peste gropi între clădiri; căderea costă o viață și te întoarce pe marginea dinainte |
| Horn de wall-jump | intri sub un zid și urci sărind dintr-un perete în altul |
| Patrulă | gardian cu rută, umbră în care te ascunzi, lăzi, pasarelă pe deasupra |
| Poartă cu cameră | cameră care baleiază și ușă blocată: oprești camera de la terminal, apoi spargi ușa |
| Coridor cu lasere | tunel prea jos ca să sari peste lasere: treci în pauza lor sau oprește-le de la terminal |
| Salt prin noduri / lanț de turnuri | clădiri prea înalte; nodurile de rețea te teleportează pe acoperiș sau din turn în turn |
| Arena dronelor | drone care te urmăresc pe un drum A* în jurul stâlpilor |
| Lift | spargi terminalul liftului, urci la etajul următor |
| Seif de date | terminal de date (Wire Connect) care dă un fragment de date și obiectivul misiunii |
| Sala cu turele | turele care trag; te adăpostești după lăzi sau folosești EMP-ul |
| Bazin / scufundare | înot, lăzi care plutesc, ziduri coborâte în apă sub care treci pe dedesubt |
| Puțul cu stavilă | spargi stavila, apa umple puțul și te ridică până la marginea care altfel e prea sus |
| Checkpoint / ieșire | salvare și reapariție / finalul nivelului |

### Nivelul 1: Acoperișuri și parc (tutorial, exterior)

Start cu indicații de control și dialogul Operatorului, parc pe acoperiș (copaci cu oase legănați de vânt, iarbă care
se îndoaie în jurul jucătorului), patru gropi între clădiri, hornul de wall-jump, primul gardian (cu pasarelă pe
deasupra), poarta de securitate cu ușa din misiune, tunelul cu lasere și podea electrică, turnul urcat prin noduri de
rețea (cu fragmentul de date pe acoperiș), arena cu două drone și ultimele gropi până la extracție. Ploaie, skybox de noapte
și parallax.

| ![Parc](screenshots/09-nivel1-parc.png) | ![Acoperișuri](screenshots/21-nivel1-acoperisuri.png) |
|---|---|
| ![Securitate](screenshots/10-nivel1-securitate.png) | ![Drona](screenshots/15-nivel1-drona.png) |

### Nivelul 2: Turnul de birouri (interior, două etaje)

Hol cu camere, gardian cu pasarelă, poartă (ușă de dificultate 2), coridor cu trei lasere, liftul de serviciu blocat,
etajul doi cu un **lunetist** (trage de la distanță), sala cu turele (cu un EMP la intrare), seiful de date, un gardian
greu și o dronă înainte de ieșirea pe acoperiș.

| ![Hol](screenshots/23-nivel2-hol.png) | ![Lift](screenshots/25-nivel2-lift.png) |
|---|---|

### Nivelul 3: Ferma de servere

Hale cu rafturi de servere, arene cu drone, un coridor cu patru lasere, **lanțul de turnuri** traversat nod → nod → nod
peste gol, poartă de dificultate 3, gardian greu, încă o arenă cu trei drone, nucleul de servere (spargere de
dificultate 3) și sala cu turele.

| ![Servere](screenshots/28-nivel3-servere.png) | ![Noduri](screenshots/29-nivel3-noduri.png) |
|---|---|

### Nivelul 4: Canalizarea

Bazine cu lăzi care plutesc, ziduri coborâte în apă (te scufunzi pe sub ele), **puțul cu stavilă** (apa urcă după
spargere și te duce până la marginea de sus), lasere, drone, încă un bazin cu scufundare și o poartă păzită sub
fundația sediului. Apa folosește shaderul de refracție (§8).

| ![Apă](screenshots/30-nivel4-apa.png) | ![Stavila](screenshots/31-nivel4-stavila.png) |
|---|---|

### Nivelul 5: Sediul corporației (finalul)

Toate mecanicile în secvență: poartă de dificultate 3, gardian greu, patru lasere, liftul executiv, lunetist, trei drone,
turele, lanțul de noduri și mainframe-ul (spargere de dificultate 4). Ieșirea de pe acoperiș pornește finalul cinematic,
apoi creditele.

| ![Sediul](screenshots/32-nivel5-sediu.png) | ![Liftul executiv](screenshots/33-nivel5-lift.png) |
|---|---|

## 6. Mecanici

### Mișcare (Rigidbody2D + FSM)

Accelerație și frânare diferite pe sol și în aer, săritură cu înălțime variabilă (gravitație mai mare la eliberarea
tastei și la cădere), **coyote time** 0,1 s, **jump buffer** 0,12 s, **dublu salt**, **dash** cu cooldown, **wall-slide +
wall-jump**, **ghemuire** cu verificarea tavanului, **înot** (ieșire la suprafață cu săritură) și impact la aterizare
(shake, praf, sunet, zgomot). Stările: Grounded, Airborne, WallSlide, Dash, Swim, Hack, Hurt, Dead, Teleport.

### Stealth

- **Conuri de vedere** (raycast) pe gardieni, camere și turele. Cât timp te văd, crește conștientizarea gardianului și
  **nivelul de urmărire**. Pragurile 25 / 60 / 100 schimbă muzica (stealth → alertă), snapshot-ul de sunet și HUD-ul (la
  100: „BLOCARE TOTALĂ”).
- **Umbre** (HideZone) care te fac invizibil; ghemuit ești mai greu de observat.
- **Zgomot:** pașii (rază ~4,5 m, mai mare pe metal și sticlă, mai mică pe iarbă), aterizările dure și exploziile EMP se
  aud. Gardienii aflați în rază vin să verifice; un zid între tine și gardian înjumătățește raza. Mersul ghemuit nu face
  zgomot, iar abilitatea „Pași silențioși” reduce raza la 30%.
- **Alarme:** camerele și senzorii de mișcare declanșează alarma, iar gardienii din apropiere vin la locul ei.

### Hacking (3 minijocuri)

| Terminal | Minijoc | Ce faci | Efect |
|---|---|---|---|
| Ușă (`Terminal_Door`) | Pattern Memory | memorezi și repeți o secvență | deschide ușa |
| Cameră (`Terminal_Camera`) | Port Scan | oprești cursorul în fereastra portului deschis | dezactivează camera 20 s |
| Date (`Terminal_Data`) | Wire Connect | conectezi firele de aceeași culoare | fragment de date + credite |

Dificultatea e între 1 și 5. Un eșec adaugă +15 la urmărire. Terminalele generice opresc laserele, deblochează liftul,
nodurile sau stavila. Un terminal spart rămâne spart (salvat).

| ![Pattern Memory](screenshots/11-hacking-pattern-memory.png) | ![Port Scan](screenshots/12-hacking-port-scan.png) | ![Wire Connect](screenshots/13-hacking-wire-connect.png) |
|---|---|---|

### Abilități (3 ramuri, 8 abilități cu efecte reale)

| Ramură | Abilitate | Cost | Efect |
|---|---|---|---|
| Stealth | Pași silențioși | 100 XP | pașii se aud de trei ori mai încet (raza zgomotului × 0,3) |
| Stealth | Camuflaj optic | 180 XP | urmărirea crește cu 30% mai încet când ești văzut |
| Hacking | Overclock neural | 120 XP | fiecare terminal e cu un nivel mai ușor |
| Hacking | A doua șansă | 160 XP | o încercare suplimentară automată după un eșec |
| Hacking | Acces de la distanță | 240 XP | spargi terminale de la ~4 m |
| Hardware | Servo-picioare | 120 XP | dash-ul se reîncarcă cu 40% mai repede |
| Hardware | Blindaj subdermal | 180 XP | +1 punct de viață maxim |
| Hardware | Condensatori EMP | 220 XP | raza grenadelor EMP +50% |

### Obiecte

Grenadă EMP (se aruncă; dezactivează camere, drone și turele și amețește gardienii), cheie USB, fragment de date, exploit
zero-day. Jocul nou pornește cu 150 de credite și 2 grenade EMP.

### Inamici și AI

- **Gardieni** (FSM): Patrol → Suspicious → Chase → Search → Return, plus **Investigate** (merg la un zgomot). Gardianul
  greu e mai lent, dar lovește de două ori mai tare și aude mai bine; **lunetistul** vede de departe și trage proiectile. Toate cele trei
  variante sunt prefab variants ale lui `Guard`.
- **Camere de securitate**: baleiază, detectează și declanșează alarma; se pot dezactiva prin hacking, EMP sau de către
  Operator.
- **Drone**: zboară după jucător pe un drum calculat cu **A\*** propriu (grilă din layerele de coliziune, heap binar,
  coadă de cereri limitată pe cadru), cu netezirea drumului prin linie de vedere.
- **Turele**: trag proiectile din pool când te văd.

### Mediu

- **Apă**: suprafață din arcuri amortizate (valuri la intrare), `BuoyancyEffector2D` (lăzile plutesc), înot, snapshot
  audio subacvatic și **stavila** care ridică nivelul apei.
- **Gropi**: −1 viață și revenire pe marginea dinainte (`FallZone`).
- **Vânt**: rafale (`WindZone2D`), copaci cu oase care se leagănă și iarbă care se îndoaie în jurul jucătorului.
- **Pericole**: lasere care pornesc și se opresc ciclic, podea electrică.
- **Lift**, **uși**, **noduri de rețea** (teleport cu dizolvare și traseu de particule), **checkpoint-uri**.

## 7. Co-op asimetric

Doi jucători, două roluri:

- **Infiltratorul** joacă nivelul exact ca în single-player.
- **Operatorul** vede o **hartă schematică** a nivelului (zidurile, Infiltratorul, ușile, camerele, gardienii) și are
  un buget de **energie** care se reface (6/s, maximum 100). Poate să **comute o ușă** (35), să **bucleze o cameră** timp
  de 8 secunde (25) sau să **distragă un gardian** cu un apel radio fals (20).

Două moduri, cu același cod de Operator:

| Mod | Cum pornește | Detalii |
|---|---|---|
| **Local, ecran împărțit** | Meniu → Co-op → Local | Infiltratorul folosește jumătatea stângă (tastatură + mouse), consola Operatorului e pe jumătatea dreaptă (gamepad sau numpad). |
| **LAN** | PC 1: Co-op → Găzduiește în LAN · PC 2: scrie IP-ul gazdei → Alătură-te | Gazda joacă și e autoritară (port 7777). Clientul rulează doar consola Operatorului; harta, pozițiile și comenzile circulă prin Netcode for GameObjects. |

| ![Meniul Co-op](screenshots/37-meniu-coop.png) | ![Ecran împărțit](screenshots/38-coop-ecran-impartit.png) |
|---|---|

![Consola Operatorului în LAN](screenshots/39-coop-lan-operator.png)

Tehnic: țintele implementează `IOperatorTarget` (Core) și se înscriu în `OperatorRegistry`. `CoopSession` trimite
~10 snapshot-uri pe secundă prin `IOperatorLink` (`LocalOperatorLink` în același proces, `NetworkOperatorLink` prin
mesaje numite Netcode, cu payload binar `OperatorWire`) și aplică comenzile în bugetul `OperatorModel`.

## 8. Grafică, animație, sunet

- **URP 2D**: lumini Global, Point, Spot și Freeform, umbre (`ShadowCaster2D`), 45 de normal maps generate, neon pâlpâitor,
  Bloom + Color Adjustments + Vignette cu câte un profil per nivel, skybox real randat în spatele scenei 2D.
- **Shader Graph** (5 grafuri generate din noduri reale):
  - `SG_Hologram`: scanline-uri și pâlpâire;
  - `SG_SpriteDissolve`: dizolvare cu margine HDR, folosită la teleport și la terminale;
  - `SG_NeonPulse`: neon emisiv care pulsează;
  - `SG_GrassWind`: vânt și îndoire în jurul jucătorului, în stadiul de vertex;
  - `SG_WaterDistortion`: refracție, citește `_CameraSortingLayerTexture` prin UV-uri deformate de zgomot.
- **Animație**:
  - Animator Controllers cu blend trees pentru jucător, gardieni, drone, camere, uși și lifturi;
  - Animation Events pentru pași și cadrele de lovire;
  - **copaci cu oase 2D Animation** (`SpriteSkin`, 4 oase și un mesh cu greutăți, generate prin provider-ii Sprite Editor);
  - tranziții UI animate.
- **Particule**: scântei, explozie EMP, ploaie, abur, flux de date, praf la aterizare, stropi, moarte, teleportare.
  VFX-urile și proiectilele vin dintr-un object pool.
- **Cutscene-uri Timeline**: intro și final, cu track-uri Animation (păpușa hackerului), Cinemachine (cadre cu blend),
  Activation (titluri, subtitrări) și Signal (shake, alarmă, VFX). Se pot sări cu Enter sau Esc.
- **Sunet**: AudioMixer cu grupurile Master / Music / SFX / UI / Ambience, snapshot-urile Normal / Underwater / Alarm,
  muzică adaptivă (stealth ↔ alertă), pași diferiți pe fiecare suprafață și sunete de interfață. Volumele se reglează
  din setări.

Grafica și sunetele sunt deocamdată placeholdere generate procedural de cod (`PlaceholderArtGenerator`, `ProceduralAudio`).
Pachetele reale recomandate și licențele lor sunt în [ASSETS.md](../ASSETS.md).

## 9. Arhitectura tehnică

- **13 assembly definitions** fără cicluri: Core (EventBus, ServiceLocator, FSM, salvare, contracte, modelele de co-op),
  Input, Systems (inventar, urmărire, portofel, abilități, misiuni, sesiune), Audio, VFX, Player, AI, Gameplay, Hacking,
  UI, Cinematics, Net, Editor. Detalii în [ARCHITECTURE.md](../ARCHITECTURE.md).
- **Stare de gameplay în clase C# simple** (`Inventory`, `TraceMeter`, `QuestLog`, `SkillTree`, `Wallet`,
  `OperatorModel`, modelele de hacking), cu vederi MonoBehaviour, așa că se pot testa unitar și se pot sincroniza în rețea.
- **Comunicare prin `EventBus<T>`** (evenimente `readonly struct`) și `ServiceLocator`; codul de gameplay nu folosește `Find*`.
- **Conținut generat din cod**: toate scenele, prefab-urile, materialele, Shader Graph-urile, animatorii, mixerul,
  timeline-urile și arta placeholder sunt create de scripturi de Editor (`SceneBuilder.BuildAll`), rulate în batchmode.
  Proiectul se reconstruiește identic pe orice PC, iar nimic nu e editat de mână în YAML-ul Unity.
- **Nivelurile** sunt compuse din segmentele `LevelLayout`. `Finish()` dimensionează limitele camerei și grila A* după ce
  s-a construit nivelul. Fiecare segment declară și o estimare de timp (§11).

## 10. Maparea pe barem

Raportul automat `RubricValidator` (meniul **Root Access → Validate Rubric** sau scriptul din §13) verifică dovezile
pentru fiecare punct. **Toate cele 12 puncte trec, la fel și ambele bonusuri.** Detaliile complete (scripturi, prefab-uri,
demonstrații de 30 de secunde) sunt în [RUBRIC.md](../RUBRIC.md).

| # | Punct | Unde se vede | Pe scurt |
|---|---|---|---|
| 1 | Mișcare / săritură | Nivelul 1, `l1_movement` | controller complet: coyote, buffer, dublu salt, dash, wall-jump, înot |
| 2 | Materiale / texturi | showcase camera 2 | materiale pe suprafețe, normal maps, 5 Shader Graph-uri |
| 3 | Mediu: copaci / iarbă | Nivelul 1, parcul | copaci cu oase (SpriteSkin), iarbă cu shader, vânt, parallax |
| 4 | Apă / skybox | Nivelul 4, showcase camera 4 | apă cu arcuri, plutire, înot, refracție, stavilă; skybox de noapte |
| 5 | Lumini / cameră | Hub, toate nivelurile | lumini 2D de toate tipurile, umbre, Cinemachine (confiner, shake, zoom) |
| 6 | Prefab-uri | showcase camera 6 | 60 de prefab-uri, variante (Guard → Heavy/Sniper, Terminal → Door/Camera/Data), pool |
| 7 | Particule / teleportare | Nivelul 1, `l1_nodes` | 11 sisteme de particule, teleportare prin noduri cu dizolvare |
| 8 | Meniu | meniul principal | meniu, setări cu rebind, pauză, game over, nivel complet, loading, co-op |
| 9 | Sunet | showcase camera 9 | mixer, snapshot-uri, muzică adaptivă, pași pe suprafețe |
| 10 | Animații / Animator | showcase camera 10 | Animator Controllers, blend trees, Animation Events |
| 11 | Coliziuni / triggere | Nivelul 1, `l1_security` | matrice de coliziuni, triggere (gropi, ascunzători, alarme, apă…), raycast-uri |
| 12 | Scripturi | Nivelul 1, `l1_drone` | arhitectură pe asmdef, EventBus, A* propriu, AI cu auz, 152 de teste |
| B1 | Cinematice | Joc nou (intro), finalul | 2 cutscene-uri Timeline + Cinemachine, se pot sări |
| B2 | Multiplayer | Meniu → Co-op | co-op asimetric local (ecran împărțit) și LAN (Netcode) |

În joc, tasta **F1** deschide panoul de barem: butonul **Mergi** duce direct la locul unde se vede fiecare punct. Scena
**Prezentare barem** are câte o cameră pentru fiecare punct (capturile `barem-camera-01..14`).

![Panoul de barem](screenshots/17-panou-barem-f1.png)

## 11. Durata de joc

Estimarea pentru o primă trecere se calculează automat la fiecare build de conținut (`Logs/PlaytimeEstimate.md`): se
însumează timpii pe segment (mers, așteptarea patrulelor și a laserelor, câte o încercare pe minijoc) și se înmulțește
cu ×1,25 pentru morți și reîncercări.

| Parte | Estimare (min) |
|---|---|
| Nivelul 1: Acoperișuri și parc | 7,7 |
| Nivelul 2: Turnul de birouri | 9,5 |
| Nivelul 3: Ferma de servere | 9,3 |
| Nivelul 4: Canalizarea | 8,9 |
| Nivelul 5: Sediul corporației | 11,2 |
| Ascunzătoarea (5 vizite) | 3,3 |
| Cutscene-uri și dialoguri | 1,8 |
| **Total** | **≈ 52** |

Ținta din cerințe e de cel puțin 30 de minute. Timpul real al fiecărui nivel apare pe ecranul „Misiune îndeplinită” și
rămâne în salvare, așa că o trecere cronometrată confirmă estimarea.

## 12. Testare și calitate

- **114 teste EditMode**:
  - A*, inventar, urmărire, portofel, magazin, abilități;
  - misiuni (inclusiv lanțul principal), salvare/încărcare cu migrări și timpii pe nivel;
  - minijocurile de hacking, regula de auz, modelul și codec-ul Operatorului, EventBus, ServiceLocator, FSM.
- **38 de teste PlayMode**:
  - fiecare scenă pornește fără erori, cu jucătorul pe podea;
  - fluxul complet de acceptare: meniu → intro → Hub → Nivelul 1 → hack → ușă → teleport → dronă → pauză → volum → meniu → showcase;
  - toate cele trei minijocuri prin overlay-ul real, terminalul personal și magazinul;
  - ieșirile nivelurilor 1–5 cu deblocare, tab-ul Călătorie;
  - gardianul care aude, patrulează și răspunde la alarmă;
  - stavila care inundă puțul;
  - cutscene-urile (track-uri și skip), copacii cu oase (deformarea mesh-ului);
  - co-op-ul local (ușă, cameră, gardian, ecran împărțit) și gazda LAN.

  Framework-ul de test pică orice test care produce o eroare în log.
- **Scripturi de verificare**:
  - `smoke-player.ps1` (exe-ul pe fiecare scenă, cu capturi și scanarea log-ului);
  - `capture.ps1` (toate capturile din această documentație);
  - `coop-lan-smoke.ps1` (două exe-uri, gazdă + client pe 127.0.0.1, verifică ușa deschisă de la distanță);
  - `RubricValidator` (dovezile pentru barem, inclusiv ultima rulare de teste și durata estimată).

## 13. Build și livrare

Comenzi PowerShell din rădăcina repo-ului (proiectul trebuie să fie închis în Unity Editor):

```powershell
./scripts/build.ps1 -RebuildContent      # regenerează conținutul și face RootAccess.exe
./scripts/test.ps1                       # EditMode + PlayMode (XML în RootAccess/Logs/TestResults)
./scripts/unity-batch.ps1 -Step rubric -ExecuteMethod RootAccess.EditorTools.RubricValidator.RunBatch
./scripts/capture.ps1                    # capturile din Documentatie/screenshots
./scripts/coop-lan-smoke.ps1             # test LAN cu două instanțe
./scripts/package-for-drive.ps1 -Build   # dist/RootAccess_Project.zip + dist/RootAccess_Windows.zip
```

Pentru Google Drive se urcă cele două arhive din `dist/`:

- `RootAccess_Project.zip`: tot proiectul, fără folderele pe care Unity le regenerează (`Library`, `Temp`, `Logs`,
  `Builds`). Se deschide direct în Unity 6000.3.29f1.
- `RootAccess_Windows.zip`: jocul gata de rulat.

## 14. Limitări și pași rămași

- **Grafica și sunetele sunt placeholdere procedurale.** Arhitectura e pregătită pentru pachete reale: se importă în
  `Assets/ThirdParty` și se leagă în generatoare (vezi ASSETS.md).
- **Durata de joc e o estimare** (~52 de minute). O trecere completă cronometrată o confirmă; timpii se văd în joc.
- **Co-op-ul LAN** e testat automat pe același PC (127.0.0.1). Pe două PC-uri, firewall-ul Windows trebuie să permită
  `RootAccess.exe` (portul 7777) la prima pornire a gazdei. Nu există join prin internet (Relay).
- **Echilibrarea** (dificultatea terminalelor, costurile Operatorului, numărul de gardieni) poate fi ajustată după
  playtest; valorile stau în generatoare și în asset-urile de configurare (`GuardConfig`, `PlayerMovementConfig`).

## 15. Anexă: capturi de ecran

Toate capturile sunt în `Documentatie/screenshots` și se regenerează cu `./scripts/capture.ps1` (1920×1080), plus
`39-coop-lan-operator.png` din `coop-lan-smoke.ps1`.

| Fișier | Ce arată |
|---|---|
| `01-meniu-principal`, `02-setari` | meniul principal, setările |
| `03-hub`, `04`–`08` `terminal-*` | ascunzătoarea și tab-urile terminalului |
| `09`, `10`, `14`, `15`, `20`, `21` `nivel1-*` | nivelul 1: parc, securitate, noduri, dronă, start, acoperișuri |
| `11`–`13` `hacking-*` | cele trei minijocuri |
| `16-pauza`, `17-panou-barem-f1`, `18-game-over`, `19-credite` | ecranele de sistem |
| `22-terminal-calatorie` | tab-ul Călătorie |
| `23`–`27` `nivel2-*` | nivelul 2 |
| `28`, `29` `nivel3-*` · `30`, `31` `nivel4-*` · `32`, `33` `nivel5-*` | nivelurile 3–5 |
| `34`–`36` `cutscene-*` | intro și final |
| `37-meniu-coop`, `38-coop-ecran-impartit`, `39-coop-lan-operator` | co-op |
| `barem-camera-01`…`14` | camerele scenei Prezentare barem |
