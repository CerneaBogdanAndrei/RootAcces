# Root Access: progres sesiunea 3 (în lucru, PR 3)

Data: 25.11.2026 · Branch local `feat/p3-levels` (commit WIP, **necompilat și netestat încă**, neîmpins).
Restul regulilor și al designului: vezi `PROGRES_SESIUNEA2.md` (secțiunile 1 și 3).

## Ce e scris deja în PR 3 (cod C#, încă nerulat în Unity)
- `Core/Events/GameEvents.cs`: evenimentul `PlayerNoise(Position, Radius)`.
- `Core/Contracts/Contracts.cs`: `IFallRecoverable.RecoverFromFall(damage, safePosition)`; implementat în `Player/PlayerHealth.cs`.
- `Gameplay/World/FallZone.cs` (nou): trigger în gropi; -1 viață și revenire pe marginea dinainte (`returnPoint`).
- Zgomot: `Audio/FootstepPlayer.cs` (câmp `noiseRadius`, doar jucătorul; rază × volum × suprafață),
  `Player/PlayerController.cs` (aterizare 4–9 u), `Gameplay/World/EmpGrenade.cs` (rază × 2,5).
- AI: `AI/Perception/Hearing.cs` (regulă pură, testabilă), `GuardConfig` (`hearingSensitivity`, `investigateSeconds`,
  `alarmResponseRadius`, `ranged`, `ConfigureSenses`), `Guard.cs` (abonat la `PlayerNoise` și `AlarmRaised`, starea nouă
  `Investigate`, tragere cu proiectile din pool pentru gardienii `ranged`, câmpuri `projectilePrefab`/`muzzle`),
  `GuardStates.cs` (`GuardInvestigateState`).
- Misiuni: `QuestDefinition.followUpQuest` + `SetFollowUp`; `QuestLog.TryComplete` pornește automat misiunea următoare.
- Călătorie: `TerminalTab.Travel`, `TerminalScreenRequested.Destinations`, `FastTravelTerminal.destinations` (deschide
  tab-ul), `TerminalScreenView` (listă niveluri, „Călătorește”, salvează înainte), `LevelDefinition.description`,
  texte noi în `Strings.Panels`, al 6-lea tab în `UIRootBuilder` (lățime 232).

## Ce urmează (în ordine)
1. `Editor/Generation/LevelLayout.cs`: constructorul pe segmente (design în PROGRES_SESIUNEA2 §3). Convenție: podeaua
   la y=0 (tile-uri −4..−1), salt 3,4 + 2,6, gropi ≤5 tile-uri cu `FallZone`, cameră ortho 6 (limite ≥13 înălțime).
   `SceneBuilder.GameplayScene` + helper-ele devin `internal`; `GameplayScene` primește `PolygonCollider2D Bounds`
   (actualizat în `Finish()` cu PathfindingGrid pe tot nivelul).
2. Prefab-uri: `noiseRadius` ≈ 5 pe FootstepPlayer-ul jucătorului; Guard_Sniper: `projectilePrefab` + `ConfigureSenses(…, ranged)`;
   Elevator cu întoarcere automată (opțional); consola de călătorie din Hub cu `destinations` = nivelurile 1–5.
3. Level01 reconstruit (păstrează spawn-urile l1_park/movement/security/nodes/drone și obiectele numite Terminal_Door,
   Terminal_Camera, Terminal_Data) + Level02_Office; quest-uri principale înlănțuite; `LevelExit.unlocksLevel`; BuildOrder.
4. Teste: actualizează `VerticalSliceAcceptanceTests` / `HackingTerminalTests` să caute după `TerminalId`/nume (nu `Single` pe Kind);
   adaugă Level02 în `SceneSmokeTests`; EditMode pentru `Hearing` și lanțul de misiuni; PlayMode pentru gardianul care aude
   zgomotul și pentru „nivelul se poate termina” (teleport la ieșire → LevelCompleted + deblocare).
5. test.ps1 + build + RubricValidator → PR 3 → merge; apoi PR 4–8 ca în PROGRES_SESIUNEA2.

## Prompt pentru sesiunea următoare
```
Continuăm „Root Access” din D:\ProiectSPV. Citește PROGRES_SESIUNEA3.md, apoi PROGRES_SESIUNEA2.md, CLAUDE.md.
Ești pe branch-ul feat/p3-levels cu un commit WIP necompilat. Termină PR 3 (pașii din PROGRES_SESIUNEA3.md),
apoi continuă cu PR 4–8. Când rămâi fără limită, oprește-te după un pas complet și actualizează acest fișier.
```
