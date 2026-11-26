namespace RootAccess.Core
{
    /// <summary>
    /// Every player-facing UI string, in Romanian and English side by side. Each property returns the text for the
    /// current <see cref="Loc"/> language, so code reads <c>Strings.Menu.NewGame</c> and never inlines UI text.
    /// Generators bake the Romanian text and its English twin into prefabs and scenes (see <see cref="TextCatalog"/>).
    /// </summary>
    public static class Strings
    {
        private static string T(string romanian, string english) => Loc.T(romanian, english);

        public static string GameTitle => T("ROOT ACCESS", "ROOT ACCESS");
        public static string GameSubtitle => T("infiltrare · hacking · evadare", "infiltration · hacking · escape");

        public static class Menu
        {
            public static string Continue => T("Continuă", "Continue");
            public static string NewGame => T("Joc nou", "New game");
            public static string Settings => T("Setări", "Settings");
            public static string RubricShowcase => T("Prezentare barem", "Rubric showcase");
            public static string Credits => T("Credite", "Credits");
            public static string Quit => T("Ieșire", "Quit");
            public static string Back => T("Înapoi", "Back");
            public static string Resume => T("Reia", "Resume");
            public static string MainMenu => T("Meniu principal", "Main menu");
            public static string Paused => T("PAUZĂ", "PAUSED");
            public static string Retry => T("Reîncearcă", "Retry");
            public static string NextLevel => T("Continuă", "Continue");
            public static string GameOver => T("CONEXIUNE PIERDUTĂ", "CONNECTION LOST");
            public static string GameOverHint => T("Ai fost prins. Reia de la ultimul checkpoint.", "You got caught. Resume from the last checkpoint.");
            public static string LevelComplete => T("MISIUNE ÎNDEPLINITĂ", "MISSION COMPLETE");
            public static string Loading => T("Se încarcă...", "Loading...");
            public static string Version => T("versiunea", "version");
            public static string Language => T("Limba: {0}", "Language: {0}");
        }

        public static class Settings
        {
            public static string Title => T("SETĂRI", "SETTINGS");
            public static string Audio => T("Audio", "Audio");
            public static string Video => T("Video", "Video");
            public static string Controls => T("Controale", "Controls");
            public static string MasterVolume => T("Volum general", "Master volume");
            public static string MusicVolume => T("Muzică", "Music");
            public static string SfxVolume => T("Efecte sonore", "Sound effects");
            public static string UiVolume => T("Interfață", "Interface");
            public static string Resolution => T("Rezoluție", "Resolution");
            public static string Fullscreen => T("Ecran complet", "Fullscreen");
            public static string VSync => T("VSync", "VSync");
            public static string Language => T("Limbă", "Language");
            public static string Rebind => T("Apasă o tastă...", "Press a key...");
            public static string Jump => T("Săritură", "Jump");
            public static string Interact => T("Interacțiune", "Interact");
            public static string ResetBindings => T("Resetează tastele", "Reset key bindings");
        }

        public static class Hud
        {
            public static string Health => T("Viață", "Health");
            public static string Trace => T("Urmărire", "Trace");
            public static string Credits => T("Credite", "Credits");
            public static string NoActiveQuest => T("Niciun contract activ", "No active contract");
            public static string Hidden => T("ASCUNS", "HIDDEN");
            public static string Lockdown => T("BLOCARE TOTALĂ", "FULL LOCKDOWN");
            public static string CheckpointSaved => T("Checkpoint salvat", "Checkpoint saved");
            public static string ItemPickedUp => T("Ai obținut: {0}", "Picked up: {0}");
            public static string InventoryFull => T("Inventar plin", "Inventory full");
            public static string EmpCount => T("EMP ×{0}", "EMP ×{0}");
            public static string NoEmp => T("Nu ai grenade EMP", "No EMP grenades left");
            public static string EmpDetonated => T("EMP: {0} sisteme dezactivate", "EMP: {0} systems disabled");
            public static string ExtraAttempt => T("Încercare suplimentară (abilitate)", "Extra attempt (skill)");
            public static string SluiceOpened => T("Stavila e deschisă: apa urcă", "The sluice is open: the water is rising");
            public static string FanStarted => T("Ventilatorul a pornit: curentul de aer te ridică", "The fan is running: the updraft lifts you up");
            public static string Uplink => T("TRANSMISIE {0}%", "BROADCAST {0}%");
            public static string UplinkPaused => T("TRANSMISIE ÎNTRERUPTĂ {0}%: întoarce-te la emițător", "BROADCAST PAUSED {0}%: get back to the transmitter");
            public static string UplinkStarted => T("Transmisia a pornit: rămâi lângă emițător!", "The broadcast has started: stay by the transmitter!");
            public static string UplinkDone => T("Transmisie completă: tot orașul vede datele Halden", "Broadcast complete: the whole city sees the Halden data");
            public static string LevelTime => T("Timp în nivel", "Level time");
            public static string BestTime => T("record", "best");
            public static string TotalTime => T("Timp total de joc", "Total play time");
            public static string CreditsAndXp => T("{0} Credite  ·  {1} XP", "{0} Credits  ·  {1} XP");
            public static string ObjectiveCurrent => T("> ", "> ");
            public static string TravelHint => T("Mergi la terminalul de călătorie [E] și alege „{0}”", "Go to the travel terminal [E] and pick “{0}”");
            public static string SideContract => T("+ {0}: {1}", "+ {0}: {1}");
        }

        public static class Panels
        {
            public static string Inventory => T("INVENTAR", "INVENTORY");
            public static string Quests => T("MISIUNI", "MISSIONS");
            public static string Skills => T("ABILITĂȚI", "SKILLS");
            public static string Shop => T("MAGAZIN", "SHOP");
            public static string Contracts => T("CONTRACTE", "CONTRACTS");
            public static string Travel => T("CĂLĂTORIE", "TRAVEL");
            public static string Lore => T("JURNALE", "DATA LOGS");
            public static string Title => T("TERMINAL PERSONAL", "PERSONAL TERMINAL");
            public static string InventoryHint => T("[Tab] Închide", "[Tab] Close");
            public static string CloseHint => T("[Esc] Închide", "[Esc] Close");
            public static string Continue => T("[Enter] Continuă", "[Enter] Continue");
            public static string Header => T("{0} credite   ·   {1} XP", "{0} credits   ·   {1} XP");
            public static string Empty => T("Nimic aici încă.", "Nothing here yet.");
            public static string Close => T("Închide", "Close");
            public static string Buy => T("Cumpără", "Buy");
            public static string Unlock => T("Deblochează", "Unlock");
            public static string Accept => T("Acceptă", "Accept");
            public static string Quantity => T("Cantitate: {0}", "Quantity: {0}");
            public static string Value => T("Valoare: {0} credite", "Value: {0} credits");
            public static string Price => T("Preț: {0} credite", "Price: {0} credits");
            public static string Owned => T("Deții: {0}", "Owned: {0}");
            public static string Cost => T("Cost: {0} XP", "Cost: {0} XP");
            public static string Requires => T("Necesită: {0}", "Requires: {0}");
            public static string Rewards => T("Recompensă: {0} credite, {1} XP", "Reward: {0} credits, {1} XP");
            public static string SkillUnlocked => T("DEBLOCATĂ", "UNLOCKED");
            public static string SkillAvailable => T("Disponibilă", "Available");
            public static string SkillMissingPrerequisite => T("Blocată: lipsește o abilitate anterioară", "Locked: a previous skill is missing");
            public static string SkillNotEnoughXp => T("XP insuficient", "Not enough XP");
            public static string SkillOnlyAtTerminal => T("Deblocarea se face la terminalul de abilități din ascunzătoare.", "Skills are unlocked at the skill terminal in the hideout.");
            public static string QuestActive => T("în desfășurare", "in progress");
            public static string QuestCompleted => T("finalizată", "completed");
            public static string QuestFailed => T("eșuată", "failed");
            public static string ContractAvailable => T("disponibil", "available");
            public static string ObjectiveDone => T("[x] ", "[x] ");
            public static string ObjectiveOpen => T("[ ] ", "[ ] ");
            public static string ObjectiveFailed => T("[!] ", "[!] ");
            public static string Optional => T(" (opțional)", " (optional)");
            public static string Stealth => T("Rămâi nedetectat", "Stay undetected");
            public static string NotEnoughCredits => T("Credite insuficiente", "Not enough credits");
            public static string TravelAction => T("Călătorește", "Travel");
            public static string LevelOpen => T("deblocat", "unlocked");
            public static string LevelLocked => T("blocat", "locked");
            public static string LevelHere => T("ești aici", "you are here");
            public static string TravelLockedHint => T("Termină nivelul anterior ca să deblochezi această destinație.", "Finish the previous level to unlock this destination.");
            public static string TravelReadyHint => T("Gata de plecare. Progresul se salvează înainte de călătorie.", "Ready to go. Your progress is saved before the trip.");
            public static string TravelHereHint => T("Te afli deja aici.", "You are already here.");
            public static string TravelStarted => T("Călătorie spre: {0}", "Travelling to: {0}");
        }

        /// <summary>Data logs hidden in the levels (terminal "Jurnale" tab, pickup toast).</summary>
        public static class Lore
        {
            public static string Found => T("Jurnal găsit: {0}  ({1}/{2})", "Data log found: {0}  ({1}/{2})");
            public static string MissingEntry => T("??? · {0}", "??? · {0}");
            public static string MissingTitle => T("Jurnal negăsit", "Log not found");
            public static string MissingHint => T("Jurnalul e ascuns undeva în „{0}”. Caută pe pasarele, pe ziduri înalte, în tuburi de ventilație și sub apă.",
                "This log is hidden somewhere in “{0}”. Look on catwalks, on high walls, in air vents and under water.");
            public static string Progress => T("Jurnale găsite: {0}/{1}", "Data logs found: {0}/{1}");
        }

        public static class Branches
        {
            public static string Stealth => T("Stealth", "Stealth");
            public static string Hacking => T("Hacking", "Hacking");
            public static string Hardware => T("Hardware", "Hardware");
        }

        public static class Prompts
        {
            public static string Hack => T("[E] Spargere terminal", "[E] Hack terminal");
            public static string Teleport => T("[E] Teleportare prin nod", "[E] Teleport through the node");
            public static string NodeLocked => T("Nod blocat – sparge terminalul", "Node locked – hack the terminal");
            public static string UseElevator => T("[E] Folosește liftul", "[E] Use the elevator");
            public static string Talk => T("[E] Vorbește", "[E] Talk");
            public static string FastTravel => T("[E] Călătorie rapidă", "[E] Fast travel");
            public static string JobBoard => T("[E] Panou de contracte (darknet)", "[E] Contract board (darknet)");
            public static string Shop => T("[E] Magazin", "[E] Shop");
            public static string SkillTerminal => T("[E] Terminal abilități", "[E] Skill terminal");
            public static string Exit => T("[E] Ieșire din nivel", "[E] Leave the level");
            public static string TeleportPad => T("[E] Teleportare", "[E] Teleport");
            public static string AlreadyHacked => T("Terminal deja spart", "Terminal already hacked");
        }

        public static class Hub
        {
            public static string ContractAccepted => T("Contract acceptat: {0}", "Contract accepted: {0}");
            public static string ContractActive => T("Contract în desfășurare: {0}", "Contract in progress: {0}");
            public static string LevelLocked => T("Nivel blocat: {0}", "Level locked: {0}");
            public static string SkillUnlocked => T("Abilitate deblocată: {0}", "Skill unlocked: {0}");
            public static string NoSkillAvailable => T("XP insuficient ({0}) sau nicio abilitate disponibilă.", "Not enough XP ({0}) or no skill available.");
            public static string Bought => T("Cumpărat: {0} (-{1} credite)", "Bought: {0} (-{1} credits)");
            public static string TooExpensive => T("Credite insuficiente ({0}/{1})", "Not enough credits ({0}/{1})");
            public static string PriceSuffix => T("{0}: {1} ({2} cr)", "{0}: {1} ({2} cr)");
            public static string TargetSuffix => T("{0}: {1}", "{0}: {1}");
        }

        public static class HackTargets
        {
            public static string Door => T("Ușă de securitate", "Security door");
            public static string Elevator => T("Lift", "Elevator");
            public static string NetworkNode => T("Nod de rețea", "Network node");
            public static string Camera => T("Cameră de securitate", "Security camera");
            public static string Drone => T("Dronă", "Drone");
            public static string Turret => T("Turelă", "Turret");
            public static string Laser => T("Laser", "Laser");
            public static string MotionSensor => T("Senzor de mișcare", "Motion sensor");
            public static string Sluice => T("Stavilă", "Sluice gate");
            public static string Fan => T("Ventilator", "Fan");
            public static string Uplink => T("Emițător", "Transmitter");
        }

        public static class Hacking
        {
            public static string Title => T("SPARGERE ÎN CURS", "HACK IN PROGRESS");
            public static string PatternMemoryHint => T("Memorează secvența, apoi repet-o.", "Memorise the sequence, then repeat it.");
            public static string WireConnectHint => T("Conectează firele de aceeași culoare.", "Connect the wires of the same colour.");
            public static string PortScanHint => T("Apasă când cursorul trece prin portul deschis.", "Press when the cursor passes the open port.");
            public static string Watch => T("Urmărește...", "Watch...");
            public static string YourTurn => T("Rândul tău!", "Your turn!");
            public static string Success => T("ACCES PERMIS", "ACCESS GRANTED");
            public static string Failure => T("ACCES REFUZAT", "ACCESS DENIED");
            public static string CancelHint => T("[Esc] Abandonează", "[Esc] Abort");
        }

        public static class Rubric
        {
            public static string PanelTitle => T("BAREM – PREZENTARE (F1)", "RUBRIC – SHOWCASE (F1)");
            public static string GoTo => T("Mergi", "Go");
            public static string Movement => T("1. Mișcare / săritură", "1. Movement / jumping");
            public static string Materials => T("2. Materiale / texturi", "2. Materials / textures");
            public static string Environment => T("3. Mediu: copaci / iarbă", "3. Environment: trees / grass");
            public static string Water => T("4. Apă / skybox", "4. Water / skybox");
            public static string Lighting => T("5. Lumini / cameră", "5. Lights / camera");
            public static string Prefabs => T("6. Prefab-uri", "6. Prefabs");
            public static string Particles => T("7. Particule / teleportare", "7. Particles / teleporting");
            public static string Menu => T("8. Meniu", "8. Menu");
            public static string Sound => T("9. Sunet", "9. Sound");
            public static string Animation => T("10. Animații / Animator", "10. Animations / Animator");
            public static string Collisions => T("11. Coliziuni / triggere", "11. Collisions / triggers");
            public static string Scripts => T("12. Scripturi", "12. Scripts");
            public static string Cinematics => T("Bonus: Cinematice", "Bonus: Cinematics");
            public static string Multiplayer => T("Bonus: Multiplayer co-op", "Bonus: Co-op multiplayer");
        }

        public static class Levels
        {
            public static string Hub => T("Ascunzătoarea", "The Hideout");
            public static string Level01 => T("Acoperișuri și parc", "Rooftops and Park");
            public static string Level02 => T("Turnul de birouri", "The Office Tower");
            public static string Level03 => T("Ferma de servere", "The Server Farm");
            public static string Level04 => T("Canalizarea", "The Sewers");
            public static string Level05 => T("Sediul corporației", "Corporate Headquarters");
            public static string Level06 => T("Antena Halden", "The Halden Antenna");
            public static string RubricShowcase => T("Prezentare barem", "Rubric showcase");
        }

        public static class Coop
        {
            public static string MenuButton => T("Co-op", "Co-op");
            public static string Title => T("CO-OP ASIMETRIC", "ASYMMETRIC CO-OP");
            public static string Explain => T("Infiltratorul joacă nivelul. Operatorul vede harta, deschide uși, buclează camere și distrage gardieni.",
                "The Infiltrator plays the level. The Operator sees the map, opens doors, loops cameras and distracts guards.");
            public static string Local => T("Local (ecran împărțit)", "Local (split screen)");
            public static string LocalHint => T("Jucătorul 1: tastatură + mouse · Operatorul: gamepad (D-pad sus/jos, A = execută) sau Numpad 8/2/5",
                "Player 1: keyboard + mouse · Operator: gamepad (D-pad up/down, A = execute) or Numpad 8/2/5");
            public static string Host => T("Găzduiește în LAN (Infiltrator)", "Host on LAN (Infiltrator)");
            public static string Join => T("Alătură-te (Operator)", "Join (Operator)");
            public static string AddressPlaceholder => T("IP-ul gazdei, ex. 192.168.1.20", "Host IP, e.g. 192.168.1.20");
            public static string Back => T("Înapoi", "Back");
            public static string LocalStatus => T("Co-op local: Operatorul e pe jumătatea dreaptă a ecranului", "Local co-op: the Operator is on the right half of the screen");
            public static string HostWaiting => T("Găzduiești pe portul {0}. Așteptăm Operatorul...", "Hosting on port {0}. Waiting for the Operator...");
            public static string HostFailed => T("Nu am putut porni gazda (portul e ocupat?)", "Could not start the host (is the port in use?)");
            public static string JoinFailed => T("Nu am putut porni conexiunea", "Could not start the connection");
            public static string Connecting => T("Conectare la {0}:{1}...", "Connecting to {0}:{1}...");
            public static string WaitingForMap => T("Conectat. Așteptăm harta nivelului de la Infiltrator...", "Connected. Waiting for the level map from the Infiltrator...");
            public static string Connected => T("Conectat · {0}", "Connected · {0}");
            public static string OperatorJoined => T("Operatorul s-a conectat", "The Operator has connected");
            public static string Disconnected => T("Conexiune pierdută", "Connection lost");
            public static string NoEnergy => T("Operator: energie insuficientă", "Operator: not enough energy");
            public static string OperatorActed => T("Operator: {0}", "Operator: {0}");
            public static string Door => T("ușă comutată", "door toggled");
            public static string Camera => T("cameră buclată 8 s", "camera looped for 8 s");
            public static string Guard => T("gardian distras", "guard distracted");
            public static string ConsoleTitle => T("OPERATOR", "OPERATOR");
            public static string Energy => T("Energie {0}/{1}", "Energy {0}/{1}");
            public static string NoTargets => T("Nicio țintă în nivel.", "No targets in this level.");
            public static string NoSignal => T("Fără semnal de la Infiltrator.", "No signal from the Infiltrator.");
            public static string TargetDoor => T("Ușă", "Door");
            public static string TargetCamera => T("Cameră", "Camera");
            public static string TargetGuard => T("Gardian", "Guard");
            public static string ActionDoor => T("comută ({0})", "toggle ({0})");
            public static string ActionCamera => T("buclează ({0})", "loop ({0})");
            public static string ActionGuard => T("distrage ({0})", "distract ({0})");
            public static string StateOpen => T("deschisă", "open");
            public static string StateClosed => T("închisă", "closed");
            public static string StateLooped => T("oarbă", "blind");
            public static string StateWatching => T("activă", "watching");
            public static string StateAlert => T("alertă", "alert");
            public static string StateCalm => T("patrulează", "patrolling");
            public static string Distance => T("{0:0} m", "{0:0} m");
            public static string LeaveConsole => T("Deconectare", "Disconnect");
        }

        public static class Cutscenes
        {
            public static string Skip => T("[Enter / Esc] Sari peste", "[Enter / Esc] Skip");
            public static string IntroTitle => T("ROOT ACCESS", "ROOT ACCESS");
            public static string Intro1 => T("Sectorul 7. Orașul nu doarme niciodată, iar rețeaua lui nu uită nimic.",
                "Sector 7. The city never sleeps, and its network never forgets.");
            public static string Intro2 => T("Corporațiile dețin serverele. Eu dețin cheile.", "The corporations own the servers. I own the keys.");
            public static string Intro3 => T("Un contract nou așteaptă în ascunzătoare. E timpul să intru.", "A new contract is waiting at the hideout. Time to go in.");
            public static string FinaleTitle => T("ROOT ACCESS: CONFIRMAT", "ROOT ACCESS: CONFIRMED");
            public static string Finale1 => T("Fiecare ecran din oraș arată acum datele Halden. Proiectul OCHI s-a terminat în direct.",
                "Every screen in the city now shows the Halden data. Project EYE ended live on air.");
            public static string Finale2 => T("Alarma sună degeaba. Până ajung gardienii pe antenă, eu sunt doar un semnal în rețea.",
                "The alarm rings for nothing. By the time the guards reach the antenna, I am just a signal in the network.");
            public static string Finale3 => T("Sfârșit. Mulțumim că ai jucat.", "The end. Thank you for playing.");
        }

        public static class Credits
        {
            public static string Title => T("CREDITE", "CREDITS");
            public static string Body => T(
                "ROOT ACCESS\n\nProiect universitar – Joc 2D singleplayer/multiplayer în Unity cu C#\n\n" +
                "Programare, design și integrare\nCernea Bogdan-Andrei\n\n" +
                "Realizat cu Unity 6 LTS · URP 2D · Cinemachine · Input System\n\n" +
                "Grafică și sunete: placeholder generate procedural\n(pachetele finale sunt listate în ASSETS.md)\n\n" +
                "Mulțumiri speciale profesorilor și colegilor de laborator.",
                "ROOT ACCESS\n\nUniversity project – 2D singleplayer/multiplayer game in Unity with C#\n\n" +
                "Programming, design and integration\nCernea Bogdan-Andrei\n\n" +
                "Made with Unity 6 LTS · URP 2D · Cinemachine · Input System\n\n" +
                "Art and sound: procedurally generated placeholders\n(the final packs are listed in ASSETS.md)\n\n" +
                "Special thanks to our teachers and lab colleagues.");
            public static string BackHint => T("[Esc] Înapoi la meniu", "[Esc] Back to the menu");
        }
    }
}
