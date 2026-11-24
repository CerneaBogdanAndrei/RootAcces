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
            public const string MainQuest = "main_first_contract";
            public const string SideQuest = "side_data_courier";
            public const string Level01 = "level01";
            public const string Hub = "hub";
            public const string TerminalLevel01Door = "terminal_l1_door";
            public const string TerminalLevel01Camera = "terminal_l1_camera";
            public const string TerminalLevel01Data = "terminal_l1_data";
            public const string ExitLevel01 = "exit_l1";
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
            public const string SilentStepsDesc = "Pașii tăi nu mai fac zgomot pe metal.";
            public const string Overclock = "Overclock neural";
            public const string OverclockDesc = "Minijocurile de spargere sunt mai lente.";
            public const string ServoLegs = "Servo-picioare";
            public const string ServoLegsDesc = "Deblochează săritura dublă (necesită Pași silențioși).";
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
        }

        public static class Rubric
        {
            public const string Movement = "Controller Rigidbody2D: accelerație, săritură variabilă, coyote time, buffer, dublu salt, dash, wall-jump, ghemuire, înot.";
            public const string Materials = "Materiale pe suprafețe (metal, sticlă, neon, beton), shadere dizolvare/hologramă/neon, normal maps.";
            public const string Environment = "Copaci și iarbă cu vânt din script și shader care se îndoaie lângă jucător.";
            public const string Water = "Apă cu arcuri dinamice, BuoyancyEffector2D, înot, snapshot subacvatic; skybox de noapte.";
            public const string Lighting = "Lumini 2D Global/Point/Spot/Freeform, neon pâlpâitor, umbre, Cinemachine cu confiner și shake.";
            public const string Prefabs = "Prefab-uri și variante (Guard_Heavy/Sniper, Terminal_Door/Camera/Data), obiecte din pool.";
            public const string Particles = "Particule (scântei, ploaie, abur, date, praf, stropi) și teleportare prin noduri de rețea.";
            public const string Menu = "Meniu principal, setări cu rebinding, pauză, game over, nivel complet, loading asincron.";
            public const string Sound = "AudioMixer cu snapshot-uri, muzică adaptivă stealth/alertă, pași pe suprafețe, surse din pool.";
            public const string Animation = "Animator Controllers cu blend trees, Animation Events pentru pași și lovituri.";
            public const string Collisions = "Matrice de coliziuni, OnCollisionEnter2D (daune, aterizare), triggere, raycast-uri.";
            public const string Scripts = "Arhitectură pe asmdef-uri, EventBus, A* propriu pentru drone, teste EditMode/PlayMode.";
            public const string Cinematics = "Cutscene-uri Timeline + Cinemachine, sărite cu Enter/Esc (P5).";
            public const string Multiplayer = "Co-op asimetric Netcode + Relay: interfețe și bootstrap (P7).";
        }

        public static class Labels
        {
            public const string ShowcaseHint = "[E] pe platforma de teleport → camera următoare   ·   F1 = panou barem";
            public const string MenuRoomHint = "Apasă Esc pentru meniul de pauză (setări, volum, taste).";
            public const string HubWelcome = "ASCUNZĂTOAREA";
        }
    }
}
