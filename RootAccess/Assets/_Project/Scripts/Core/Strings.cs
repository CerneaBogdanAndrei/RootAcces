namespace RootAccess.Core
{
    /// <summary>
    /// Every player-facing string, in Romanian. Swap this class (or back it with a localization table)
    /// to translate the game; gameplay code must never inline UI text.
    /// </summary>
    public static class Strings
    {
        public const string GameTitle = "ROOT ACCESS";
        public const string GameSubtitle = "infiltrare · hacking · evadare";

        public static class Menu
        {
            public const string Continue = "Continuă";
            public const string NewGame = "Joc nou";
            public const string Settings = "Setări";
            public const string RubricShowcase = "Prezentare barem";
            public const string Credits = "Credite";
            public const string Quit = "Ieșire";
            public const string Back = "Înapoi";
            public const string Resume = "Reia";
            public const string MainMenu = "Meniu principal";
            public const string Paused = "PAUZĂ";
            public const string Retry = "Reîncearcă";
            public const string NextLevel = "Continuă";
            public const string GameOver = "CONEXIUNE PIERDUTĂ";
            public const string GameOverHint = "Ai fost prins. Reia de la ultimul checkpoint.";
            public const string LevelComplete = "MISIUNE ÎNDEPLINITĂ";
            public const string Loading = "Se încarcă...";
            public const string Version = "v0.1 · vertical slice";
        }

        public static class Settings
        {
            public const string Title = "SETĂRI";
            public const string Audio = "Audio";
            public const string Video = "Video";
            public const string Controls = "Controale";
            public const string MasterVolume = "Volum general";
            public const string MusicVolume = "Muzică";
            public const string SfxVolume = "Efecte sonore";
            public const string UiVolume = "Interfață";
            public const string Resolution = "Rezoluție";
            public const string Fullscreen = "Ecran complet";
            public const string VSync = "VSync";
            public const string Rebind = "Apasă o tastă...";
            public const string Jump = "Săritură";
            public const string Interact = "Interacțiune";
            public const string ResetBindings = "Resetează tastele";
        }

        public static class Hud
        {
            public const string Health = "Viață";
            public const string Trace = "Urmărire";
            public const string Credits = "Credite";
            public const string NoActiveQuest = "Niciun contract activ";
            public const string Hidden = "ASCUNS";
            public const string Lockdown = "BLOCARE TOTALĂ";
            public const string CheckpointSaved = "Checkpoint salvat";
            public const string ItemPickedUp = "Ai obținut: {0}";
            public const string InventoryFull = "Inventar plin";
            public const string EmpCount = "EMP ×{0}";
            public const string NoEmp = "Nu ai grenade EMP";
            public const string EmpDetonated = "EMP: {0} sisteme dezactivate";
            public const string ExtraAttempt = "Încercare suplimentară (abilitate)";
            public const string SluiceOpened = "Stavila e deschisă: apa urcă";
            public const string LevelTime = "Timp în nivel";
            public const string BestTime = "record";
            public const string TotalTime = "Timp total de joc";
            public const string CreditsAndXp = "{0} Credite  ·  {1} XP";
        }

        public static class Panels
        {
            public const string Inventory = "INVENTAR";
            public const string Quests = "MISIUNI";
            public const string Skills = "ABILITĂȚI";
            public const string Shop = "MAGAZIN";
            public const string Contracts = "CONTRACTE";
            public const string Travel = "CĂLĂTORIE";
            public const string Title = "TERMINAL PERSONAL";
            public const string InventoryHint = "[Tab] Închide";
            public const string CloseHint = "[Esc] Închide";
            public const string Continue = "[Enter] Continuă";
            public const string Header = "{0} credite   ·   {1} XP";
            public const string Empty = "Nimic aici încă.";
            public const string Close = "Închide";
            public const string Buy = "Cumpără";
            public const string Unlock = "Deblochează";
            public const string Accept = "Acceptă";
            public const string Quantity = "Cantitate: {0}";
            public const string Value = "Valoare: {0} credite";
            public const string Price = "Preț: {0} credite";
            public const string Owned = "Deții: {0}";
            public const string Cost = "Cost: {0} XP";
            public const string Requires = "Necesită: {0}";
            public const string Rewards = "Recompensă: {0} credite, {1} XP";
            public const string SkillUnlocked = "DEBLOCATĂ";
            public const string SkillAvailable = "Disponibilă";
            public const string SkillMissingPrerequisite = "Blocată: lipsește o abilitate anterioară";
            public const string SkillNotEnoughXp = "XP insuficient";
            public const string SkillOnlyAtTerminal = "Deblocarea se face la terminalul de abilități din ascunzătoare.";
            public const string QuestActive = "în desfășurare";
            public const string QuestCompleted = "finalizată";
            public const string QuestFailed = "eșuată";
            public const string ContractAvailable = "disponibil";
            public const string ObjectiveDone = "[x] ";
            public const string ObjectiveOpen = "[ ] ";
            public const string ObjectiveFailed = "[!] ";
            public const string Optional = " (opțional)";
            public const string Stealth = "Rămâi nedetectat";
            public const string NotEnoughCredits = "Credite insuficiente";
            public const string TravelAction = "Călătorește";
            public const string LevelOpen = "deblocat";
            public const string LevelLocked = "blocat";
            public const string LevelHere = "ești aici";
            public const string TravelLockedHint = "Termină nivelul anterior ca să deblochezi această destinație.";
            public const string TravelReadyHint = "Gata de plecare. Progresul se salvează înainte de călătorie.";
            public const string TravelHereHint = "Te afli deja aici.";
            public const string TravelStarted = "Călătorie spre: {0}";
        }

        public static class Branches
        {
            public const string Stealth = "Stealth";
            public const string Hacking = "Hacking";
            public const string Hardware = "Hardware";
        }

        public static class Prompts
        {
            public const string Hack = "[E] Spargere terminal";
            public const string Teleport = "[E] Teleportare prin nod";
            public const string NodeLocked = "Nod blocat – sparge terminalul";
            public const string UseElevator = "[E] Folosește liftul";
            public const string Talk = "[E] Vorbește";
            public const string FastTravel = "[E] Călătorie rapidă";
            public const string JobBoard = "[E] Panou de contracte (darknet)";
            public const string Shop = "[E] Magazin";
            public const string SkillTerminal = "[E] Terminal abilități";
            public const string Exit = "[E] Ieșire din nivel";
            public const string TeleportPad = "[E] Teleportare";
            public const string AlreadyHacked = "Terminal deja spart";
        }

        public static class Hub
        {
            public const string ContractAccepted = "Contract acceptat: {0}";
            public const string ContractActive = "Contract în desfășurare: {0}";
            public const string LevelLocked = "Nivel blocat: {0}";
            public const string SkillUnlocked = "Abilitate deblocată: {0}";
            public const string NoSkillAvailable = "XP insuficient ({0}) sau nicio abilitate disponibilă.";
            public const string Bought = "Cumpărat: {0} (-{1} credite)";
            public const string TooExpensive = "Credite insuficiente ({0}/{1})";
            public const string PriceSuffix = "{0}: {1} ({2} cr)";
            public const string TargetSuffix = "{0}: {1}";
        }

        public static class HackTargets
        {
            public const string Door = "Ușă de securitate";
            public const string Elevator = "Lift";
            public const string NetworkNode = "Nod de rețea";
            public const string Camera = "Cameră de securitate";
            public const string Drone = "Dronă";
            public const string Turret = "Turelă";
            public const string Laser = "Laser";
            public const string MotionSensor = "Senzor de mișcare";
            public const string Sluice = "Stavilă";
        }

        public static class Hacking
        {
            public const string Title = "SPARGERE ÎN CURS";
            public const string PatternMemoryHint = "Memorează secvența, apoi repet-o.";
            public const string WireConnectHint = "Conectează firele de aceeași culoare.";
            public const string PortScanHint = "Apasă când cursorul trece prin portul deschis.";
            public const string Watch = "Urmărește...";
            public const string YourTurn = "Rândul tău!";
            public const string Success = "ACCES PERMIS";
            public const string Failure = "ACCES REFUZAT";
            public const string CancelHint = "[Esc] Abandonează";
        }

        public static class Rubric
        {
            public const string PanelTitle = "BAREM – PREZENTARE (F1)";
            public const string GoTo = "Mergi";
            public const string Movement = "1. Mișcare / săritură";
            public const string Materials = "2. Materiale / texturi";
            public const string Environment = "3. Mediu: copaci / iarbă";
            public const string Water = "4. Apă / skybox";
            public const string Lighting = "5. Lumini / cameră";
            public const string Prefabs = "6. Prefab-uri";
            public const string Particles = "7. Particule / teleportare";
            public const string Menu = "8. Meniu";
            public const string Sound = "9. Sunet";
            public const string Animation = "10. Animații / Animator";
            public const string Collisions = "11. Coliziuni / triggere";
            public const string Scripts = "12. Scripturi";
            public const string Cinematics = "Bonus: Cinematice";
            public const string Multiplayer = "Bonus: Multiplayer co-op";
        }

        public static class Levels
        {
            public const string Hub = "Ascunzătoarea";
            public const string Level01 = "Acoperișuri și parc";
            public const string Level02 = "Turnul de birouri";
            public const string Level03 = "Ferma de servere";
            public const string Level04 = "Canalizarea";
            public const string Level05 = "Sediul corporației";
            public const string RubricShowcase = "Prezentare barem";
        }

        public static class Cutscenes
        {
            public const string Skip = "[Enter / Esc] Sari peste";
            public const string IntroTitle = "ROOT ACCESS";
            public const string Intro1 = "Sectorul 7. Orașul nu doarme niciodată, iar rețeaua lui nu uită nimic.";
            public const string Intro2 = "Corporațiile dețin serverele. Eu dețin cheile.";
            public const string Intro3 = "Un contract nou așteaptă în ascunzătoare. E timpul să intru.";
            public const string FinaleTitle = "ROOT ACCESS: CONFIRMAT";
            public const string Finale1 = "Mainframe-ul Halden e al nostru. Fiecare ușă din oraș răspunde acum la comanda mea.";
            public const string Finale2 = "Alarma sună degeaba. Până ajung gardienii pe acoperiș, eu sunt doar un semnal în rețea.";
            public const string Finale3 = "Sfârșit. Mulțumim că ai jucat.";
        }

        public static class Credits
        {
            public const string Title = "CREDITE";
            public const string Body =
                "ROOT ACCESS\n\nProiect universitar – Joc 2D singleplayer/multiplayer în Unity cu C#\n\n" +
                "Programare, design și integrare\nCernea Bogdan-Andrei\n\n" +
                "Realizat cu Unity 6 LTS · URP 2D · Cinemachine · Input System\n\n" +
                "Grafică și sunete: placeholder generate procedural\n(pachetele finale sunt listate în ASSETS.md)\n\n" +
                "Mulțumiri speciale profesorilor și colegilor de laborator.";
            public const string BackHint = "[Esc] Înapoi la meniu";
        }
    }
}
