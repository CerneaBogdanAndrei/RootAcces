namespace RootAccess.EditorTools
{
    /// <summary>
    /// Seed content written into ScriptableObjects and scenes (items, skills, quests, logs, dialogue, rubric, level
    /// labels), in Romanian and English side by side like <see cref="RootAccess.Core.Strings"/>. Generators read the
    /// Romanian text (the source language) and bake its English twin next to it (<see cref="Translations"/>).
    /// </summary>
    public static class SeedContent
    {
        private static string T(string romanian, string english) => RootAccess.Core.Loc.T(romanian, english);

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
            public const string ArchivistQuest = "side_archivist";
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
            public const string Level06 = "level06";
            public const string MainQuestLevel06 = "main_broadcast";
            public const string TerminalLevel06Fan = "l6_fan";
            public const string TerminalLevel06Uplink = "l6_uplink";
            public const string BroadcastDone = "l6_broadcast";
            public const string ExitLevel06 = "exit_l6";
        }


        public static class Items
        {
            public static string UsbKey => T("Cheie USB", "USB Key");
            public static string UsbKeyDesc => T("Cheie de acces criptată. Deschide uși de serviciu.", "Encrypted access key. Opens service doors.");
            public static string DataShard => T("Fragment de date", "Data Shard");
            public static string DataShardDesc => T("Date corporative furate. Se vând pe darknet.", "Stolen corporate data. Sells well on the darknet.");
            public static string EmpGrenade => T("Grenadă EMP", "EMP Grenade");
            public static string EmpGrenadeDesc => T("Scurtcircuitează drone, camere și turele pentru câteva secunde.", "Shorts out drones, cameras and turrets for a few seconds.");
            public static string Exploit => T("Exploit zero-day", "Zero-day Exploit");
            public static string ExploitDesc => T("Sare peste un minijoc de spargere.", "Skips one hacking minigame.");
        }

        public static class Skills
        {
            public static string SilentSteps => T("Pași silențioși", "Silent Steps");
            public static string SilentStepsDesc => T("Tălpi amortizate: pașii tăi se aud de trei ori mai încet.", "Padded soles: your footsteps are three times quieter.");
            public static string Camouflage => T("Camuflaj optic", "Optical Camouflage");
            public static string CamouflageDesc => T("Când ești văzut, nivelul de urmărire crește cu 30% mai încet.", "When you are seen, the trace level rises 30% slower.");
            public static string Overclock => T("Overclock neural", "Neural Overclock");
            public static string OverclockDesc => T("Fiecare terminal e cu un nivel de dificultate mai ușor.", "Every terminal is one difficulty level easier.");
            public static string SecondChance => T("A doua șansă", "Second Chance");
            public static string SecondChanceDesc => T("Dacă o spargere eșuează, primești automat încă o încercare.", "If a hack fails, you automatically get one more attempt.");
            public static string RemoteHack => T("Acces de la distanță", "Remote Access");
            public static string RemoteHackDesc => T("Poți sparge terminalele de la aproximativ 4 metri.", "You can hack terminals from about 4 metres away.");
            public static string ServoLegs => T("Servo-picioare", "Servo Legs");
            public static string ServoLegsDesc => T("Dash-ul se reîncarcă cu 40% mai repede.", "Your dash recharges 40% faster.");
            public static string Armor => T("Blindaj subdermal", "Subdermal Armour");
            public static string ArmorDesc => T("+1 punct de viață maxim.", "+1 maximum health.");
            public static string EmpCapacitors => T("Condensatori EMP", "EMP Capacitors");
            public static string EmpCapacitorsDesc => T("Raza grenadelor EMP crește cu 50%.", "EMP grenade radius increased by 50%.");
        }

        public static class Quests
        {
            public static string MainTitle => T("Primul contract", "First Contract");
            public static string MainDesc => T("Un client anonim vrea datele unui laborator de pe acoperișurile Sectorului 7.",
                "An anonymous client wants the data of a lab on the rooftops of Sector 7.");
            public static string ReachLevel => T("Ajunge pe acoperișurile din Sectorul 7", "Reach the rooftops of Sector 7");
            public static string HackDoor => T("Sparge terminalul ușii de serviciu", "Hack the service door terminal");
            public static string CollectShard => T("Recuperează fragmentul de date", "Recover the data shard");
            public static string ReachExit => T("Ajunge la punctul de extracție", "Reach the extraction point");
            public static string Stealth => T("Bonus: nu declanșa alarma", "Bonus: don't trigger the alarm");
            public static string SideTitle => T("Curier de date", "Data Courier");
            public static string SideDesc => T("Adu o cheie USB și dezactivează camera de pe fațadă.", "Bring a USB key and disable the camera on the facade.");
            public static string CollectUsb => T("Găsește o cheie USB", "Find a USB key");
            public static string HackCamera => T("Dezactivează camera de securitate", "Disable the security camera");
            public static string GhostTitle => T("Fantoma de pe acoperiș", "Rooftop Ghost");
            public static string GhostDesc => T("Clientul vrea o operațiune curată: ajunge la extracție fără să declanșezi alarma.",
                "The client wants a clean job: reach the extraction point without triggering the alarm.");
            public static string GhostExit => T("Ajunge la punctul de extracție din Sectorul 7", "Reach the extraction point in Sector 7");
            public static string OfficeTitle => T("Turnul de birouri", "The Office Tower");
            public static string OfficeDesc => T("Fragmentul de pe acoperiș conține o adresă: seiful de date al firmei Halden Systems, la etajul 2 al turnului de birouri.",
                "The rooftop shard holds an address: the Halden Systems data vault, on floor 2 of the office tower.");
            public static string OfficeReach => T("Intră în turnul de birouri", "Get into the office tower");
            public static string OfficeElevator => T("Repornește liftul de serviciu", "Restart the service elevator");
            public static string OfficeVault => T("Sparge seiful de date de la etajul 2", "Hack the data vault on floor 2");
            public static string OfficeExit => T("Ieși prin acoperișul turnului", "Leave through the tower roof");
            public static string FarmTitle => T("Ferma de servere", "The Server Farm");
            public static string FarmDesc => T("Seiful Halden trimitea totul către ferma de servere a corporației. Acolo e cheia de acces la sediu.",
                "The Halden vault sent everything to the corporation's server farm. The key to the headquarters is there.");
            public static string FarmReach => T("Intră în ferma de servere", "Get into the server farm");
            public static string FarmCore => T("Sparge nucleul de servere", "Hack the server core");
            public static string FarmExit => T("Ieși prin tunelul de răcire", "Leave through the cooling tunnel");
            public static string SewersTitle => T("Sub oraș", "Under the City");
            public static string SewersDesc => T("Sediul e sigilat. Singura intrare nesupravegheată trece prin canalizare, pe sub ziduri.",
                "The headquarters is sealed. The only unwatched way in runs through the sewers, under the walls.");
            public static string SewersReach => T("Coboară în canalizare", "Go down into the sewers");
            public static string SewersSluice => T("Deschide stavila și urcă pe apă", "Open the sluice gate and ride the water up");
            public static string SewersExit => T("Ajunge sub fundația sediului", "Reach the foundations of the headquarters");
            public static string HqTitle => T("Root Access", "Root Access");
            public static string HqDesc => T("Mainframe-ul din vârful sediului. Cu el, orașul întreg e al tău.",
                "The mainframe at the top of the headquarters. With it, the whole city is yours.");
            public static string HqReach => T("Pătrunde în sediul corporației", "Break into the corporate headquarters");
            public static string HqElevator => T("Pornește liftul executiv", "Start the executive elevator");
            public static string HqMainframe => T("Obține root access pe mainframe", "Get root access on the mainframe");
            public static string HqExit => T("Fugi pe acoperiș", "Escape to the roof");
            public static string SpireTitle => T("Transmisia", "The Broadcast");
            public static string SpireDesc => T("Varga a cerut un singur lucru: datele înapoi la oameni. Antena din vârful sediului emite pe fiecare ecran din oraș.",
                "Varga asked for one thing: give the data back to the people. The antenna on top of the headquarters reaches every screen in the city.");
            public static string SpireReach => T("Urcă pe antena Halden", "Climb the Halden antenna");
            public static string SpireFan => T("Pornește ventilatorul de răcire", "Start the cooling fan");
            public static string SpireUplink => T("Sparge emițătorul de la vârf", "Hack the transmitter at the top");
            public static string SpireBroadcast => T("Ține transmisia până la 100%", "Hold the broadcast until 100%");
            public static string SpireExit => T("Dispari în noapte", "Vanish into the night");
        }

        /// <summary>
        /// The 18 data logs (3 per level): who Halden Systems is, what "Proiectul OCHI" (Project EYE) does with the city's
        /// cameras, the engineer L. Varga who leaked the first fragment, and the root account the hacker keeps using.
        /// </summary>
        public static class Lore
        {
            public static string ArchivistTitle => T("Arhivarul", "The Archivist");
            public static string ArchivistDesc => T("O jurnalistă din Sectorul 7 plătește pentru orice jurnal de date despre Halden Systems. Le găsești ascunse în niveluri: pe pasarele, pe ziduri, sub apă.",
                "A journalist from Sector 7 pays for any data log about Halden Systems. You will find them hidden in the levels: on catwalks, on walls, under water.");
            public static string ArchivistObjective => T("Găsește jurnale de date", "Find data logs");

            /// <summary>One hidden log: id, level, then title and body in Romanian and in English.</summary>
            public readonly struct Log
            {
                public readonly string Id;
                public readonly string Level;
                public readonly string Title;
                public readonly string TitleEn;
                public readonly string Body;
                public readonly string BodyEn;

                public Log(string id, string level, string title, string titleEn, string body, string bodyEn)
                {
                    Id = id;
                    Level = level;
                    Title = title;
                    TitleEn = titleEn;
                    Body = body;
                    BodyEn = bodyEn;
                }
            }

            public static readonly Log[] Entries =
            {
                new("lore_l1_patrol", Ids.Level01, "Jurnal de patrulă #114", "Patrol log #114",
                    "Acoperișurile din Sectorul 7 sunt ale noastre după miezul nopții. Halden plătește dublu pentru tura de noapte și nimeni nu întreabă de ce. Regula e simplă: dacă vezi pe cineva cu glugă, raportezi întâi, gândești după.",
                    "The Sector 7 rooftops are ours after midnight. Halden pays double for the night shift and nobody asks why. The rule is simple: if you see someone in a hood, report first, think later."),
                new("lore_l1_varga", Ids.Level01, "Mesaj șters: L. Varga", "Deleted message: L. Varga",
                    "Dacă citești asta, am reușit să scot fragmentul din clădire. Halden nu stochează date, le vinde. Fiecare cameră din oraș trimite imagini către ferma lor de servere, iar fiecare imagine are un preț.",
                    "If you are reading this, I managed to get the shard out of the building. Halden doesn't store data, it sells it. Every camera in the city sends footage to their server farm, and every frame has a price."),
                new("lore_l1_ad", Ids.Level01, "Reclamă interceptată", "Intercepted advert",
                    "HALDEN CLOUD: viața ta, salvată pentru totdeauna. Abonamentul gratuit include, implicit, consimțământul pentru analiza comportamentală. Dezabonarea se face personal, la sediul central, între orele 03:00 și 03:05.",
                    "HALDEN CLOUD: your life, saved forever. The free plan includes, by default, consent to behavioural analysis. To unsubscribe, come in person to our headquarters between 03:00 and 03:05."),

                new("lore_l2_memo", Ids.Level02, "Memo intern: Proiectul OCHI", "Internal memo: Project EYE",
                    "Toate holurile primesc camere noi până vineri. Căștile noi ale gardienilor aud pașii de la cinci metri, deci personalul e rugat să nu mai alerge după cafea. Departamentul juridic a aprobat. Departamentul juridic aprobă orice.",
                    "Every hallway gets new cameras by Friday. The guards' new headsets pick up footsteps from five metres away, so staff are asked to stop running for coffee. Legal has approved it. Legal approves anything."),
                new("lore_l2_hr", Ids.Level02, "Conversație HR", "HR conversation",
                    "— A mai dispărut cineva de la etajul doi.\n— Nu e treaba noastră. Șterge-i ecusonul și publică postul.\n— Și dosarul?\n— Ce dosar?",
                    "— Someone else has gone missing from the second floor.\n— Not our problem. Wipe their badge and post the job.\n— And the file?\n— What file?"),
                new("lore_l2_vault", Ids.Level02, "Seif: index de sincronizare", "Vault: sync index",
                    "Seiful trimite o copie de rezervă către ferma de servere în fiecare oră. Cheia de sincronizare n-a mai fost schimbată din ziua instalării. Cineva a fost leneș, iar acel cineva semnează cu inițialele L.V.",
                    "The vault sends a backup to the server farm every hour. The sync key hasn't been changed since the day it was installed. Someone was lazy, and that someone signs with the initials L.V."),

                new("lore_l3_cooling", Ids.Level03, "Jurnal tehnician: răcire", "Technician log: cooling",
                    "Unitatea CRAC-2 cade iar. Peste 40 de grade, dronele trec singure în patrulare agresivă. Nu te pune între ele și rafturi, nu discuta cu ele și, mai ales, nu le arăta ecusonul: îl scanează.",
                    "CRAC-2 is down again. Above 40 degrees the drones switch to aggressive patrol on their own. Don't get between them and the racks, don't argue with them and, above all, don't show them your badge: they scan it."),
                new("lore_l3_root", Ids.Level03, "Log de sistem: acces respins", "System log: access denied",
                    "Utilizator: root. Motiv: utilizatorul root nu există.\nUtilizator: root. Motiv: utilizatorul root nu există.\nUtilizator: root. Acces permis.\nAvertisment: cine a creat contul root?",
                    "User: root. Reason: user root does not exist.\nUser: root. Reason: user root does not exist.\nUser: root. Access granted.\nWarning: who created the root account?"),
                new("lore_l3_note", Ids.Level03, "Bilet lipit pe un raft", "Note stuck to a rack",
                    "Parola de la nucleu e numele câinelui directorului. Câinele a murit acum trei ani. Parola a rămas. Dacă tot citești bilete lipite pe rafturi, măcar pune-l la loc.",
                    "The core password is the director's dog's name. The dog died three years ago. The password stayed. If you're going to read notes stuck to racks, at least put this one back."),

                new("lore_l4_sluice", Ids.Level04, "Jurnal de întreținere: stavila 3", "Maintenance log: sluice 3",
                    "Halden a cumpărat și canalizarea. Oficial, pentru cabluri. Neoficial, ca nimeni să nu mai coboare aici. Stavila 3 se deschide doar din terminal; apa urcă repede, așa că nu sta în puț când o pornești.",
                    "Halden bought the sewers too. Officially, for the cables. Unofficially, so nobody comes down here any more. Sluice 3 only opens from the terminal; the water rises fast, so don't stand in the shaft when you start it."),
                new("lore_l4_fixer", Ids.Level04, "Mesaj de la fixer", "Message from the fixer",
                    "Dacă ai ajuns în canale, ferma a picat și ei știu. Te urmăresc acum pe trei rețele diferite. Nu te opri, nu răspunde la apeluri și nu bea apa, oricât de caldă ar fi.",
                    "If you made it to the sewers, the farm went down and they know it. They are tracking you on three different networks now. Don't stop, don't answer calls and don't drink the water, however warm it is."),
                new("lore_l4_wall", Ids.Level04, "Inscripție pe zid", "Writing on the wall",
                    "Am fost ingineri la Halden. Acum dormim lângă conducte. Ei au scos oamenii din oraș și au păstrat doar datele lor. Dacă treci pe aici, lasă o lanternă. Și închide stavila în urma ta.",
                    "We were engineers at Halden. Now we sleep next to the pipes. They pushed the people out of the city and kept only their data. If you pass through, leave a torch. And close the sluice behind you."),

                new("lore_l5_board", Ids.Level05, "Directiva Consiliului", "Board directive",
                    "Proiectul OCHI intră în faza a doua: fiecare ușă din oraș va răspunde numai mainframe-ului Halden. Cetățenii nu trebuie informați. Comunicatul de presă va vorbi despre „siguranța comunității”.",
                    "Project EYE enters phase two: every door in the city will answer only to the Halden mainframe. Citizens are not to be informed. The press release will talk about “community safety”."),
                new("lore_l5_director", Ids.Level05, "Jurnal personal: directorul", "Personal log: the director",
                    "Cineva umblă prin rețea cu contul root. Eu l-am creat, acum zece ani, ca ușă din spate pentru ziua în care consiliul m-ar fi dat afară. N-am crezut că-l va găsi altcineva. Acum el îmi deschide ușile mie.",
                    "Someone is walking around the network with the root account. I created it ten years ago, as a back door for the day the board would fire me. I never thought anyone else would find it. Now it opens my own doors."),
                new("lore_l5_last", Ids.Level05, "Ultimul mesaj al lui L. Varga", "L. Varga's last message",
                    "Dacă ai ajuns la mainframe, termină ce am început eu. Nu vinde datele, nu le șterge. Dă-le înapoi oamenilor cărora le aparțin. Și, te rog, schimbă cheia de sincronizare. Mi-e rușine de ea.",
                    "If you made it to the mainframe, finish what I started. Don't sell the data, don't delete it. Give it back to the people it belongs to. And please, change the sync key. I'm ashamed of it."),

                new("lore_l6_cleaners", Ids.Level06, "Jurnal: echipa de spălat geamuri", "Log: window-cleaning crew",
                    "Nacela 2 merge singură de la o vreme, dus-întors, fără să-i spună nimeni. Grilajele de pe pasarela de est sunt ruginite până la os. Halden a promis că le schimbă trimestrul viitor. Asta a fost acum trei ani.",
                    "Gondola 2 has been running on its own for a while, back and forth, with nobody telling it to. The grates on the east catwalk are rusted to the bone. Halden promised to replace them next quarter. That was three years ago."),
                new("lore_l6_spec", Ids.Level06, "Specificație: emițătorul OCHI", "Spec sheet: the EYE transmitter",
                    "Emițătorul din vârful antenei preia orice ecran din oraș în mai puțin de o secundă: panouri, telefoane, televizoare. Oficial, pentru anunțuri de urgență. Neoficial, n-a fost folosit niciodată pentru o urgență.",
                    "The transmitter at the top of the antenna takes over any screen in the city in under a second: billboards, phones, TVs. Officially, for emergency announcements. Unofficially, it has never been used for an emergency."),
                new("lore_l6_varga", Ids.Level06, "Mesaj programat: L. Varga", "Scheduled message: L. Varga",
                    "Dacă emițătorul pornește, cineva a ajuns unde eu n-am mai putut. Am lăsat ventilatoarele pe manual și cheia de sincronizare în modul de transmisie. Restul depinde de tine. Ține-te bine și nu te uita în jos.",
                    "If the transmitter starts, someone got where I never could. I left the fans on manual and the sync key in broadcast mode. The rest is up to you. Hold on tight and don't look down.")
            };
        }

        public static class Dialogue
        {
            public static string Operator => T("OPERATOR", "OPERATOR");
            public static string Hacker => T("TU", "YOU");
            public static string Briefing1 => T("Contract nou pe darknet. Laboratorul de pe acoperișurile Sectorului 7 ține ceva prețios.",
                "New contract on the darknet. The lab on the Sector 7 rooftops is holding something valuable.");
            public static string Briefing2 => T("Intri, spargi ușa de serviciu, iei fragmentul de date și ieși fără să te vadă nimeni.",
                "You go in, hack the service door, grab the data shard and get out without anyone seeing you.");
            public static string Briefing3 => T("Primit. Pornesc spre acoperișuri.", "Copy that. Heading for the rooftops.");
            public static string Intro1 => T("Te văd pe cameră. Mișcă-te cu A/D, sari cu Space, dash cu Shift.", "I've got you on camera. Move with A/D, jump with Space, dash with Shift.");
            public static string Intro2 => T("Gardienii văd doar ce e în conul lor de lumină. Stai ghemuit în umbră.", "Guards only see what is inside their cone of light. Stay crouched in the shadows.");
            public static string Intro3 => T("Și ai grijă la zgomot: pașii pe metal se aud. Ghemuit nu faci niciun zgomot.", "And watch the noise: footsteps on metal carry. Crouched, you make no sound at all.");
            public static string Office1 => T("Turnul Halden. Camere pe fiecare hol, gardieni care ascultă și un lunetist la etajul doi.",
                "The Halden tower. Cameras in every hallway, guards who listen and a sniper on the second floor.");
            public static string Office2 => T("Liftul de serviciu e blocat. Sparge-i terminalul și urcă la seif.", "The service elevator is locked. Hack its terminal and ride up to the vault.");
            public static string Office3 => T("Am înțeles. Intru pe ușa din spate.", "Understood. Going in through the back door.");
            public static string Farm1 => T("Ferma de servere: zeci de drone de pază și noduri de rețea între rafturi.", "The server farm: dozens of guard drones and network nodes between the racks.");
            public static string Farm2 => T("Sari din nod în nod peste rafturi. Nucleul e la capătul halei.", "Jump from node to node over the racks. The core is at the far end of the hall.");
            public static string Sewers1 => T("Canalizarea e inundată pe alocuri. Poți înota, dar ai grijă sub ziduri.", "The sewers are flooded in places. You can swim, but be careful under the walls.");
            public static string Sewers2 => T("Un puț e prea înalt: deschide stavila și lasă apa să te ridice.", "One shaft is too tall: open the sluice gate and let the water lift you.");
            public static string Hq1 => T("Sediul. Gardieni grei, lunetiști, turele și lasere peste tot.", "Headquarters. Heavy guards, snipers, turrets and lasers everywhere.");
            public static string Hq2 => T("Liftul executiv duce la mainframe. După asta, fugi pe acoperiș.", "The executive elevator leads to the mainframe. After that, run for the roof.");
            public static string Hq3 => T("Hai să terminăm ce am început.", "Let's finish what we started.");
            public static string Spire1 => T("Ești pe antena sediului. De aici, emițătorul ajunge pe fiecare ecran din oraș.",
                "You're on the headquarters antenna. From here, the transmitter reaches every screen in the city.");
            public static string Spire2 => T("Vântul bate tare. Nacelele și ventilatoarele de răcire te duc până sus.", "The wind is strong up here. The gondolas and cooling fans will carry you to the top.");
            public static string Spire3 => T("Atunci urc. Varga ar fi vrut asta.", "Then I'm going up. Varga would have wanted this.");
        }

        public static class Rubric
        {
            public static string Movement => T("Controller Rigidbody2D: accelerație, săritură variabilă, coyote time, buffer, dublu salt, dash, wall-jump, ghemuire, înot.",
                "Rigidbody2D controller: acceleration, variable jump, coyote time, buffering, double jump, dash, wall jump, crouching, swimming.");
            public static string Materials => T("Materiale pe suprafețe (metal, sticlă, neon, beton), shadere dizolvare/hologramă/neon, normal maps.",
                "Surface materials (metal, glass, neon, concrete), dissolve/hologram/neon shaders, normal maps.");
            public static string Environment => T("Copaci cu oase (SpriteSkin) legănați de vânt și iarbă cu shader care se îndoaie lângă jucător.",
                "Boned trees (SpriteSkin) swaying in the wind and shader grass that bends around the player.");
            public static string Water => T("Apă cu arcuri dinamice și refracție (Shader Graph), plutire, înot, stavilă care inundă puțul; skybox de noapte.",
                "Water with dynamic springs and refraction (Shader Graph), buoyancy, swimming, a sluice that floods the shaft; night skybox.");
            public static string Lighting => T("Lumini 2D Global/Point/Spot/Freeform, neon pâlpâitor, umbre, Cinemachine cu confiner și shake.",
                "2D Global/Point/Spot/Freeform lights, flickering neon, shadows, Cinemachine with confiner and shake.");
            public static string Prefabs => T("Prefab-uri și variante (Guard_Heavy/Sniper, Terminal_Door/Camera/Data), obiecte din pool.",
                "Prefabs and variants (Guard_Heavy/Sniper, Terminal_Door/Camera/Data), pooled objects.");
            public static string Particles => T("Particule (scântei, ploaie, abur, date, praf, stropi) și teleportare prin noduri de rețea.",
                "Particles (sparks, rain, steam, data, dust, splashes) and teleporting through network nodes.");
            public static string Menu => T("Meniu principal, limbă română/engleză, setări cu rebinding, pauză, game over, nivel complet, loading asincron.",
                "Main menu, Romanian/English language, settings with rebinding, pause, game over, level complete, asynchronous loading.");
            public static string Sound => T("AudioMixer cu snapshot-uri, muzică adaptivă stealth/alertă, pași pe suprafețe, surse din pool.",
                "AudioMixer with snapshots, adaptive stealth/alert music, per-surface footsteps, pooled sources.");
            public static string Animation => T("Animator Controllers cu blend trees, Animation Events pentru pași și lovituri.",
                "Animator Controllers with blend trees, Animation Events for footsteps and hits.");
            public static string Collisions => T("Matrice de coliziuni, OnCollisionEnter2D (daune, aterizare), triggere, raycast-uri.",
                "Collision matrix, OnCollisionEnter2D (damage, landing), triggers, raycasts.");
            public static string Scripts => T("Arhitectură pe asmdef-uri, EventBus, A* propriu pentru drone, teste EditMode/PlayMode.",
                "Architecture on asmdefs, EventBus, our own A* for drones, EditMode/PlayMode tests.");
            public static string Cinematics => T("Cutscene-uri Timeline + Cinemachine (intro după Joc nou, final după nivelul 6), sărite cu Enter/Esc.",
                "Timeline + Cinemachine cutscenes (intro after New game, finale after level 6), skippable with Enter/Esc.");
            public static string Multiplayer => T("Co-op asimetric: Infiltrator + Operator (hartă, uși, camere, gardieni), split-screen sau LAN.",
                "Asymmetric co-op: Infiltrator + Operator (map, doors, cameras, guards), split screen or LAN.");
        }

        public static class Labels
        {
            public static string ShowcaseHint => T("[E] pe platforma de teleport → camera următoare   ·   F1 = panou barem",
                "[E] on the teleport pad → next room   ·   F1 = rubric panel");
            public static string MenuRoomHint => T("Apasă Esc pentru meniul de pauză (setări, volum, taste).", "Press Esc for the pause menu (settings, volume, keys).");
            public static string HubWelcome => T("ASCUNZĂTOAREA", "THE HIDEOUT");
            public static string CoopRoomHint => T("Meniu → Co-op → Local: Operatorul (gamepad) deschide ușa și buclează camera de aici.",
                "Menu → Co-op → Local: the Operator (gamepad) opens the door and loops the camera here.");
            public static string L1Move => T("A/D mers · Space salt · Space în aer = dublu salt · Shift = dash", "A/D walk · Space jump · Space in the air = double jump · Shift = dash");
            public static string L1Gaps => T("Între acoperișuri e gol: o cădere te costă o viață", "There is nothing between the roofs: a fall costs you a life");
            public static string L1WallJump => T("Intră sub perete și sari dintr-un zid în altul", "Get under the wall and jump from one wall to the other");
            public static string Vent => T("Ctrl: te strecori ghemuit prin tubul de ventilație", "Ctrl: crouch to squeeze through the air vent");
            public static string L1Stealth => T("Ctrl = ghemuit (fără zgomot) · umbra te ascunde · pasarela ocolește gardianul",
                "Ctrl = crouch (silent) · shadows hide you · the catwalk goes around the guard");
            public static string L1Gate => T("Oprește camera de la terminal, apoi sparge ușa", "Shut the camera down at its terminal, then hack the door");
            public static string L1Lasers => T("Laserele pornesc și se opresc: treci în pauză sau sparge terminalul", "The lasers switch on and off: cross during the pause or hack the terminal");
            public static string L1Nodes => T("Clădirea e prea înaltă: folosește nodul de rețea", "The building is too tall: use the network node");
            public static string L1Drones => T("Dronele te urmăresc pe un traseu A*: rupe-le linia de vedere sau aruncă un EMP (G)",
                "Drones chase you along an A* path: break their line of sight or throw an EMP (G)");
            public static string L2Lobby => T("Holurile sunt pline de camere. Gardienii aud pașii și aterizările", "The hallways are full of cameras. Guards hear footsteps and landings");
            public static string L2Elevator => T("Liftul e blocat: sparge terminalul, urcă pe platformă și apasă E", "The elevator is locked: hack the terminal, step on the platform and press E");
            public static string L2Sniper => T("Lunetistul trage de departe: stai la adăpost", "The sniper fires from afar: stay in cover");
            public static string L2Turrets => T("Turelele: adăpostește-te după lăzi sau oprește-le cu EMP", "Turrets: take cover behind crates or shut them down with an EMP");
            public static string L3Start => T("Rafturi de servere cât vezi cu ochii. Dronele patrulează pe sus", "Server racks as far as the eye can see. Drones patrol up high");
            public static string L3Chain => T("Turnurile de servere: nod → nod → nod. Căderea te costă o viață", "The server towers: node → node → node. A fall costs you a life");
            public static string L4Start => T("Apa e caldă, dar adâncă: Space la suprafață te scoate afară", "The water is warm but deep: Space at the surface gets you out");
            public static string L4Dive => T("Zidul coboară în apă: scufundă-te (S) și treci pe dedesubt", "The wall goes down into the water: dive (S) and pass underneath");
            public static string L4Sluice => T("Sparge terminalul stavilei: apa umple puțul", "Hack the sluice terminal: the water fills the shaft");
            public static string L5Start => T("Sediul: toate sistemele de securitate, deodată", "Headquarters: every security system at once");
            public static string L5Mainframe => T("Mainframe-ul: root access e la un hack distanță", "The mainframe: root access is one hack away");
            public static string L6Start => T("Antena Halden: vântul bate puternic aici sus", "The Halden antenna: the wind blows hard up here");
            public static string L6Gondola => T("Nacela merge singură dintr-o parte în alta: urcă și lasă-te dus", "The gondola runs across on its own: climb aboard and ride it");
            public static string L6Fan => T("Zidul e prea înalt: sparge terminalul ventilatorului și lasă aerul să te ridice", "The wall is too tall: hack the fan terminal and let the air lift you");
            public static string L6Grates => T("Grilajele ruginite cad la scurt timp după ce calci pe ele: nu te opri", "The rusty grates fall shortly after you step on them: keep moving");
            public static string L6Arcs => T("Arcurile electrice pornesc și se opresc: treci în pauză sau sparge terminalul", "The electric arcs switch on and off: cross during the pause or hack the terminal");
            public static string L6Pulse => T("Ventilatorul suflă în rafale: intră în curent când pornește", "This fan blows in gusts: step into the stream when it starts");
            public static string L6Uplink => T("Sparge emițătorul și rămâi pe platformă până la 100%", "Hack the transmitter and stay on the platform until 100%");
        }

        public static class LevelInfo
        {
            public static string Level01 => T("Acoperișurile Sectorului 7: sărituri între clădiri, primul gardian, o poartă de securitate și arena dronelor.",
                "The Sector 7 rooftops: jumps between buildings, the first guard, a security gate and the drone arena.");
            public static string Level02 => T("Turnul Halden Systems: holuri supravegheate, lasere, lift de serviciu, lunetist și seiful de date.",
                "The Halden Systems tower: watched hallways, lasers, a service elevator, a sniper and the data vault.");
            public static string Level03 => T("Ferma de servere a corporației: roiuri de drone și lanțuri de noduri de rețea.",
                "The corporation's server farm: drone swarms and chains of network nodes.");
            public static string Level04 => T("Canalizarea de sub oraș: apă, curenți și o ecluză de deschis.",
                "The sewers under the city: water, currents and a lock to open.");
            public static string Level05 => T("Sediul corporației: toate sistemele de securitate, deodată, și mainframe-ul din vârf.",
                "Corporate headquarters: every security system at once, and the mainframe at the top.");
            public static string Level06 => T("Antena Halden: nacele, ventilatoare, grilaje care cad și transmisia finală către tot orașul.",
                "The Halden antenna: gondolas, fans, falling grates and the final broadcast to the whole city.");
        }
    }
}
