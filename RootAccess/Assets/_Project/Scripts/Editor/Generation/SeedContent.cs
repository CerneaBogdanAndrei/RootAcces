namespace RootAccess.EditorTools
{
    /// <summary>
    /// Romanian seed content written into ScriptableObjects (items, skills, quests, dialogue, rubric). Runtime UI
    /// strings live in RootAccess.Core.Strings; this is authoring data only.
    /// </summary>
    public static class SeedContent
    {
        public static class Ids
        {
            public const string UsbKey = "usb_key";
            public const string DataShard = "data_shard";
            public const string EmpGrenade = "emp_grenade";
            public const string Exploit = "exploit_zero_day";
            public const string SkillSilentSteps = "stealth_silent_steps";
            public const string SkillOverclock = "hacking_overclock";
            public const string SkillServoLegs = "hardware_servo_legs";
            public const string SkillCamouflage = "stealth_optical_camouflage";
            public const string SkillSecondChance = "hacking_second_chance";
            public const string SkillRemoteHack = "hacking_remote_access";
            public const string SkillArmor = "hardware_armor_plating";
            public const string SkillEmpCapacitors = "hardware_emp_capacitors";
            public const string GhostQuest = "side_ghost_run";
            public const string MainQuest = "main_first_contract";
            public const string SideQuest = "side_data_courier";
            public const string Level01 = "level01";
            public const string Hub = "hub";
            public const string TerminalLevel01Door = "terminal_l1_door";
            public const string TerminalLevel01Camera = "terminal_l1_camera";
            public const string TerminalLevel01Data = "terminal_l1_data";
            public const string ExitLevel01 = "exit_l1";
            public const string TerminalLevel01Lasers = "terminal_l1_lasers";
            public const string Level02 = "level02";
            public const string Level03 = "level03";
            public const string Level04 = "level04";
            public const string Level05 = "level05";
            public const string MainQuestLevel02 = "main_office_tower";
            public const string TerminalLevel02Camera = "l2_gate_camera";
            public const string TerminalLevel02Door = "l2_gate_door";
            public const string TerminalLevel02Lasers = "l2_lasers";
            public const string TerminalLevel02Elevator = "l2_elevator";
            public const string TerminalLevel02Vault = "l2_vault";
            public const string ExitLevel02 = "exit_l2";
            public const string MainQuestLevel03 = "main_server_farm";
            public const string MainQuestLevel04 = "main_sewers";
            public const string MainQuestLevel05 = "main_root_access";
            public const string TerminalLevel03Core = "l3_core";
            public const string TerminalLevel04Sluice = "l4_sluice";
            public const string TerminalLevel05Elevator = "l5_elevator";
            public const string TerminalLevel05Mainframe = "l5_mainframe";
            public const string ExitLevel03 = "exit_l3";
            public const string ExitLevel04 = "exit_l4";
            public const string ExitLevel05 = "exit_l5";
        }

        public static class Items
        {
            public const string UsbKey = "Cheie USB";
            public const string UsbKeyDesc = "Cheie de acces criptată. Deschide uși de serviciu.";
            public const string DataShard = "Fragment de date";
            public const string DataShardDesc = "Date corporative furate. Se vând pe darknet.";
            public const string EmpGrenade = "Grenadă EMP";
            public const string EmpGrenadeDesc = "Scurtcircuitează drone, camere și turele pentru câteva secunde.";
            public const string Exploit = "Exploit zero-day";
            public const string ExploitDesc = "Sare peste un minijoc de spargere.";
        }

        public static class Skills
        {
            public const string SilentSteps = "Pași silențioși";
            public const string SilentStepsDesc = "Tălpi amortizate: pașii tăi se aud de trei ori mai încet.";
            public const string Camouflage = "Camuflaj optic";
            public const string CamouflageDesc = "Când ești văzut, nivelul de urmărire crește cu 30% mai încet.";
            public const string Overclock = "Overclock neural";
            public const string OverclockDesc = "Fiecare terminal e cu un nivel de dificultate mai ușor.";
            public const string SecondChance = "A doua șansă";
            public const string SecondChanceDesc = "Dacă o spargere eșuează, primești automat încă o încercare.";
            public const string RemoteHack = "Acces de la distanță";
            public const string RemoteHackDesc = "Poți sparge terminalele de la aproximativ 4 metri.";
            public const string ServoLegs = "Servo-picioare";
            public const string ServoLegsDesc = "Dash-ul se reîncarcă cu 40% mai repede.";
            public const string Armor = "Blindaj subdermal";
            public const string ArmorDesc = "+1 punct de viață maxim.";
            public const string EmpCapacitors = "Condensatori EMP";
            public const string EmpCapacitorsDesc = "Raza grenadelor EMP crește cu 50%.";
        }

        public static class Quests
        {
            public const string MainTitle = "Primul contract";
            public const string MainDesc = "Un client anonim vrea datele unui laborator de pe acoperișurile Sectorului 7.";
            public const string ReachLevel = "Ajunge pe acoperișurile din Sectorul 7";
            public const string HackDoor = "Sparge terminalul ușii de serviciu";
            public const string CollectShard = "Recuperează fragmentul de date";
            public const string ReachExit = "Ajunge la punctul de extracție";
            public const string Stealth = "Bonus: nu declanșa alarma";
            public const string SideTitle = "Curier de date";
            public const string SideDesc = "Adu o cheie USB și dezactivează camera de pe fațadă.";
            public const string CollectUsb = "Găsește o cheie USB";
            public const string HackCamera = "Dezactivează camera de securitate";
            public const string GhostTitle = "Fantoma de pe acoperiș";
            public const string GhostDesc = "Clientul vrea o operațiune curată: ajunge la extracție fără să declanșezi alarma.";
            public const string GhostExit = "Ajunge la punctul de extracție din Sectorul 7";
            public const string OfficeTitle = "Turnul de birouri";
            public const string OfficeDesc = "Fragmentul de pe acoperiș conține o adresă: seiful de date al firmei Halden Systems, la etajul 2 al turnului de birouri.";
            public const string OfficeReach = "Intră în turnul de birouri";
            public const string OfficeElevator = "Repornește liftul de serviciu";
            public const string OfficeVault = "Sparge seiful de date de la etajul 2";
            public const string OfficeExit = "Ieși prin acoperișul turnului";
            public const string FarmTitle = "Ferma de servere";
            public const string FarmDesc = "Seiful Halden trimitea totul către ferma de servere a corporației. Acolo e cheia de acces la sediu.";
            public const string FarmReach = "Intră în ferma de servere";
            public const string FarmCore = "Sparge nucleul de servere";
            public const string FarmExit = "Ieși prin tunelul de răcire";
            public const string SewersTitle = "Sub oraș";
            public const string SewersDesc = "Sediul e sigilat. Singura intrare nesupravegheată trece prin canalizare, pe sub ziduri.";
            public const string SewersReach = "Coboară în canalizare";
            public const string SewersSluice = "Deschide stavila și urcă pe apă";
            public const string SewersExit = "Ajunge sub fundația sediului";
            public const string HqTitle = "Root Access";
            public const string HqDesc = "Ultimul pas: mainframe-ul din vârful sediului. Cu el, orașul întreg e al tău.";
            public const string HqReach = "Pătrunde în sediul corporației";
            public const string HqElevator = "Pornește liftul executiv";
            public const string HqMainframe = "Obține root access pe mainframe";
            public const string HqExit = "Fugi pe acoperiș";
        }

        public static class Dialogue
        {
            public const string Operator = "OPERATOR";
            public const string Hacker = "TU";
            public const string Briefing1 = "Contract nou pe darknet. Laboratorul de pe acoperișurile Sectorului 7 ține ceva prețios.";
            public const string Briefing2 = "Intri, spargi ușa de serviciu, iei fragmentul de date și ieși fără să te vadă nimeni.";
            public const string Briefing3 = "Primit. Pornesc spre acoperișuri.";
            public const string Intro1 = "Te văd pe cameră. Mișcă-te cu A/D, sari cu Space, dash cu Shift.";
            public const string Intro2 = "Gardienii văd doar ce e în conul lor de lumină. Stai ghemuit în umbră.";
            public const string Intro3 = "Și ai grijă la zgomot: pașii pe metal se aud. Ghemuit nu faci niciun zgomot.";
            public const string Office1 = "Turnul Halden. Camere pe fiecare hol, gardieni care ascultă și un lunetist la etajul doi.";
            public const string Office2 = "Liftul de serviciu e blocat. Sparge-i terminalul și urcă la seif.";
            public const string Office3 = "Am înțeles. Intru pe ușa din spate.";
            public const string Farm1 = "Ferma de servere: zeci de drone de pază și noduri de rețea între rafturi.";
            public const string Farm2 = "Sari din nod în nod peste rafturi. Nucleul e la capătul halei.";
            public const string Sewers1 = "Canalizarea e inundată pe alocuri. Poți înota, dar ai grijă sub ziduri.";
            public const string Sewers2 = "Un puț e prea înalt: deschide stavila și lasă apa să te ridice.";
            public const string Hq1 = "Sediul. Gardieni grei, lunetiști, turele și lasere peste tot.";
            public const string Hq2 = "Liftul executiv duce la mainframe. După asta, fugi pe acoperiș.";
            public const string Hq3 = "Hai să terminăm ce am început.";
        }

        public static class Rubric
        {
            public const string Movement = "Controller Rigidbody2D: accelerație, săritură variabilă, coyote time, buffer, dublu salt, dash, wall-jump, ghemuire, înot.";
            public const string Materials = "Materiale pe suprafețe (metal, sticlă, neon, beton), shadere dizolvare/hologramă/neon, normal maps.";
            public const string Environment = "Copaci cu oase (SpriteSkin) legănați de vânt și iarbă cu shader care se îndoaie lângă jucător.";
            public const string Water = "Apă cu arcuri dinamice și refracție (Shader Graph), plutire, înot, stavilă care inundă puțul; skybox de noapte.";
            public const string Lighting = "Lumini 2D Global/Point/Spot/Freeform, neon pâlpâitor, umbre, Cinemachine cu confiner și shake.";
            public const string Prefabs = "Prefab-uri și variante (Guard_Heavy/Sniper, Terminal_Door/Camera/Data), obiecte din pool.";
            public const string Particles = "Particule (scântei, ploaie, abur, date, praf, stropi) și teleportare prin noduri de rețea.";
            public const string Menu = "Meniu principal, setări cu rebinding, pauză, game over, nivel complet, loading asincron.";
            public const string Sound = "AudioMixer cu snapshot-uri, muzică adaptivă stealth/alertă, pași pe suprafețe, surse din pool.";
            public const string Animation = "Animator Controllers cu blend trees, Animation Events pentru pași și lovituri.";
            public const string Collisions = "Matrice de coliziuni, OnCollisionEnter2D (daune, aterizare), triggere, raycast-uri.";
            public const string Scripts = "Arhitectură pe asmdef-uri, EventBus, A* propriu pentru drone, teste EditMode/PlayMode.";
            public const string Cinematics = "Cutscene-uri Timeline + Cinemachine (intro după Joc nou, final după nivelul 5), sărite cu Enter/Esc.";
            public const string Multiplayer = "Co-op asimetric Netcode + Relay: interfețe și bootstrap (P7).";
        }

        public static class Labels
        {
            public const string ShowcaseHint = "[E] pe platforma de teleport → camera următoare   ·   F1 = panou barem";
            public const string MenuRoomHint = "Apasă Esc pentru meniul de pauză (setări, volum, taste).";
            public const string HubWelcome = "ASCUNZĂTOAREA";
            public const string L1Move = "A/D mers · Space salt · Space în aer = dublu salt · Shift = dash";
            public const string L1Gaps = "Între acoperișuri e gol: o cădere te costă o viață";
            public const string L1WallJump = "Intră sub perete și sari dintr-un zid în altul";
            public const string L1Stealth = "Ctrl = ghemuit (fără zgomot) · umbra te ascunde · pasarela ocolește gardianul";
            public const string L1Gate = "Oprește camera de la terminal, apoi sparge ușa";
            public const string L1Lasers = "Laserele pornesc și se opresc: treci în pauză sau sparge terminalul";
            public const string L1Nodes = "Clădirea e prea înaltă: folosește nodul de rețea";
            public const string L1Drones = "Dronele te urmăresc pe un traseu A*: rupe-le linia de vedere sau aruncă un EMP (G)";
            public const string L2Lobby = "Holurile sunt pline de camere. Gardienii aud pașii și aterizările";
            public const string L2Elevator = "Liftul e blocat: sparge terminalul, urcă pe platformă și apasă E";
            public const string L2Sniper = "Lunetistul trage de departe: stai la adăpost";
            public const string L2Turrets = "Turelele: adăpostește-te după lăzi sau oprește-le cu EMP";
            public const string L3Start = "Rafturi de servere cât vezi cu ochii. Dronele patrulează pe sus";
            public const string L3Chain = "Turnurile de servere: nod → nod → nod. Căderea te costă o viață";
            public const string L4Start = "Apa e caldă, dar adâncă: Space la suprafață te scoate afară";
            public const string L4Dive = "Zidul coboară în apă: scufundă-te (S) și treci pe dedesubt";
            public const string L4Sluice = "Sparge terminalul stavilei: apa umple puțul";
            public const string L5Start = "Ultimul nivel: toate sistemele de securitate, deodată";
            public const string L5Mainframe = "Mainframe-ul: root access e la un hack distanță";
        }

        public static class LevelInfo
        {
            public const string Level01 = "Acoperișurile Sectorului 7: sărituri între clădiri, primul gardian, o poartă de securitate și arena dronelor.";
            public const string Level02 = "Turnul Halden Systems: holuri supravegheate, lasere, lift de serviciu, lunetist și seiful de date.";
            public const string Level03 = "Ferma de servere a corporației: roiuri de drone și lanțuri de noduri de rețea.";
            public const string Level04 = "Canalizarea de sub oraș: apă, curenți și o ecluză de deschis.";
            public const string Level05 = "Sediul corporației: finalul. Toate sistemele de securitate, deodată.";
        }
    }
}
