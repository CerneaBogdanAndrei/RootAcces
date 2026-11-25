# Asset-uri externe: recomandări, licențe, import

Disciplina permite asset-uri și pachete importate, dar **tot codul trebuie scris de noi**. Acum toată grafica și tot
sunetul sunt placeholdere generate procedural (`PlaceholderArtGenerator`), deci proiectul nu are nicio dependență de
licență. Lista de mai jos e un punct de plecare: **verifică licența pe pagina fiecărui pachet înainte de import**
(licențele se pot schimba) și trece-o în tabelul de la final.

## Reguli

1. Totul intră în `RootAccess/Assets/ThirdParty/<Autor>_<Pachet>/`, alături de un `LICENSE.txt` (sau `LICENSE.url`)
   copiat de pe pagina pachetului.
2. Nu modifica direct fișierele importate. Variantele (recolorări, slicing) se fac prin scripturi de Editor sau în
   `Assets/_Project/Art`, ca reimportul să rămână posibil.
3. Pixel art: Pixels Per Unit **32** (ca placeholderele, `GeneratedArt.PixelsPerUnit`), Filter Mode **Point**,
   Compression **None**, Sprite Mode Multiple + Sprite Editor pentru sheet-uri.
4. Pentru lumini 2D, adaugă normal maps ca *Secondary Textures* cu numele `_NormalMap`. Generatorul face la fel, iar
   `RubricValidator` le numără. Pentru pachetele fără normal maps se pot genera cu Laigter (gratuit) sau SpriteIlluminator.
5. **Preferă CC0** pentru tot ce ajunge în `RootAccess_Project.zip`. Arhiva urcată pe Drive redistribuie fișierele sursă,
   iar unele licențe interzic asta (vezi nota despre Asset Store).
6. **Diacritice:** UI-ul e în română. Un font nou trebuie să conțină `ă â î ș ț Ă Â Î Ș Ț`, cu ș/ț cu virgulă (U+0219,
   U+021B), nu cu sedilă. La crearea Font Asset-ului TMP, include aceste caractere în setul de caractere.
7. Conectarea în joc (P1/P5): generatoarele încarcă sprite-urile după nume, prin `GeneratedArt`. Planul e un
   ScriptableObject `ArtOverrides` (nume placeholder → sprite importat), citit de `GeneratedArt.Sprite()`. Rulezi din nou
   `SceneBuilder.BuildAll`, iar prefab-urile și scenele preiau arta reală fără modificări manuale.

## Grafică: oraș cyberpunk, tileset-uri, personaje

| Sursă | Pachet (căutare) | Ce acoperă | Licență (de verificat) |
|---|---|---|---|
| itch.io, ansimuz | **Warped City** (și Warped City 2) | oraș cyberpunk pixel art: tileset, fundaluri parallax, personaj, inamici, props | autorul permite uz personal și comercial; creditarea e apreciată |
| itch.io, ansimuz | **Cyberpunk Street Environment** | stradă cu neoane, fundaluri pe straturi | ca mai sus |
| Kenney.nl | **Pixel Platformer** + **Industrial Expansion** | tile-uri de platformer, metal, țevi, uși | CC0 (domeniu public) |
| Kenney.nl | **Sci-Fi RTS** / **Space Kit** (sprite-uri 2D) | props tehnologice, terminale, drone | CC0 |
| itch.io | căutare „cyberpunk character pixel art free”, „hacker sprite” | jucătorul (idle/run/jump/wall-slide/hack) | variază: alege CC0 sau licențe care permit modificarea |
| itch.io | căutare „pixel art trees wind” / „foliage 2D” | copaci pentru rigging cu 2D Animation (P5) | variază |
| Unity Asset Store | căutare „2D Cyberpunk”, „Sci-Fi Platformer Tileset” (filtru Free) | tileset-uri și personaje alternative | Standard Unity Asset Store EULA |

**Notă despre Asset Store:** EULA standard permite folosirea asset-urilor în jocul construit, dar nu redistribuirea lor ca
fișiere sursă. Dacă folosești pachete de pe Asset Store, verifică dacă arhiva de proiect de pe Drive (privată, pentru
evaluare) e acceptabilă, sau scoate-le din arhivă și documentează aici cum se reinstalează.

## UI și fonturi

| Sursă | Pachet | Licență |
|---|---|---|
| Kenney.nl | **UI Pack (Sci-Fi)**, **UI Pack** | CC0 |
| Google Fonts | **Rajdhani**, **Orbitron**, **Share Tech Mono**, **VT323**, **Press Start 2P** | SIL Open Font License 1.1 (permite includerea în joc). Verifică diacriticele (regula 6): nu toate acoperă Latin Extended |
| Unity (inclus) | LiberationSans SDF (TMP Essentials, folosit acum) | inclus cu TextMesh Pro; are diacriticele românești |

## Sunet: SFX

| Sursă | Pachet | Licență |
|---|---|---|
| Kenney.nl | **Interface Sounds**, **UI Audio**, **Sci-Fi Sounds**, **Impact Sounds**, **Digital Audio** | CC0 |
| Sonniss | **GDC Game Audio Bundle** (anual) | royalty-free, comercial, fără atribuire (vezi licența fiecărui an) |
| freesound.org | pași pe metal/sticlă/iarbă, ploaie, abur, alarme | per sunet: CC0 / CC-BY / CC-BY-NC. Folosește doar CC0 sau CC-BY și notează atribuirea |

## Sunet: muzică și ambianță

| Sursă | Ce | Licență |
|---|---|---|
| Eric Matyas, soundimage.org | muzică „Sci-Fi”, „Puzzle”, „Urban”, bucle de ambianță | gratuită cu atribuire (vezi condițiile site-ului) |
| Kevin MacLeod, incompetech.com | piese electronice/ambient | CC-BY 4.0 (atribuire în Credite) |
| OpenGameArt.org | căutare „cyberpunk”, „synthwave loop”, „stealth” | variază (CC0, CC-BY, CC-BY-SA, GPL). Evită SA/GPL pentru asset-uri amestecate cu cod |

Muzica adaptivă (`MusicDirector`) are nevoie de două straturi pe nivel, *stealth* și *alert*, cu același tempo și aceeași
lungime, ca trecerea dintre ele să fie fluidă. Caută piese livrate cu variante sau stem-uri.

## Evidența asset-urilor importate

Completează la fiecare import. Creditele din joc (`Credits.unity`, textele din `Strings.cs`) se actualizează din acest tabel.

| Folder în `Assets/ThirdParty` | Autor | Sursă (URL) | Licență | Atribuire cerută | Folosit pentru |
|---|---|---|---|---|---|
| *(niciunul încă: toate asset-urile sunt generate procedural)* | | | | | |
