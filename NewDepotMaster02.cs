/****************************
    Depotmaster Helping out 
    Inventory Manager
******************************/

/*
    * first tests are good
    * Stone <-> gravel
    * Scrap -> does not have a Ingot -> iron -> TODO ?
    * Calculated the Ingot Equivalent
    TODO Building a history of the Subtypes -> Time -> ISI ?
        -> Seems to me timer block is not working properly, furthermore it uses start not TriggerNow
        -> maybe better to read the customdata of the pb of ISIs alignement script
        -> PB - ISI.CustomData(dayTimer=(int)Currentclicktime dayLength=(int)MaxClicksPerDay)
    * Implementing Antenna system
        -> we can not use the antenna system if we are on the same grid ?
    * I need a way of getting control again without the overide of DPM
    TODO get the Cariers Volume and calculate the minimumamount from it 60% van 33750 * 2.7 ratio Kg/L
        -> do we even have a Carier ? Or carier ...
        -> if a carier has to wait && other mines goto next mine ( even if we do not "need" that ore)
    * DP should not call the Antenna itself --> AntennaMaster
        -> except perhaps if we do not have a PB with AntennaSystem on ... ?
    * saving/loading ProgramTick & TicksPerDay
    TODO Uranium, Ice(?) and Magnesium are special cases ( NO not stone, stone is a special special case)
        -> uranium ingots are IN the reactors if you have any, they do not count as real "ingot" though.
    * We have to figure out if we have mines -> maybe call them by the Nato codes of SAM ?
        -> mines are just Cargo Constainers (ships?) designated as "Mine"
        -> How do we register a new mine ? -> mine asks DM gives a free code 
        -> not Alfa and not Zulu.
    * Established a report - system

*/

string version = "1.09";

// this only runs @the start ie. compile
public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update100;

    //Loading
    string[] storedData = Storage.Split(';');
    if(storedData.Length >= 1) {
        ProgramStatus = storedData[0];
    }
        
    // place holder
    if(storedData.Length >= 2) {
        version = storedData[1];
    }

    CheckPBs();

    CheckCustomData();


    FeStations.MyOreName("Iron");
    NiStations.MyOreName("Nickle");
    SiStations.MyOreName("Silicon");
    CoStations.MyOreName("Cobalt");    
    MgStations.MyOreName("Magnesium");
    UStations.MyOreName("Uranium");
    AgStations.MyOreName("Silver");
    AuStations.MyOreName("Gold");
    PtStations.MyOreName("Platinum");
    GrStations.MyOreName("Stone");
}

public void Save() {
    Storage = string.Join(";",
        ProgramStatus ?? "run",
        version ?? version);

    Me.CustomData = "ProgramTick=" + ProgramTick.ToString() 
        + "\nTicksPerDay=" + TicksPerDay.ToString() + "\nAntenna="
        + MyOwnAntenna + "\nCommand=" + LastReceived + "\n";
}

// static Names
const string LCDNAME="DepotLCD";
const string ISIPbName = "Solar Power";
   
    // constants
    const string OreType = "Ore";
    const string IngotType = "Ingot";

    // variables
    // string OreSubType = "Iron" ; // Default we need also the list
    const float RefineryBaseEfficency = 0.8f;
    float OreAmount = 0f;
    float MiniAmount = 54000f; // ~60% van 33750 * 2.7 ratio Kg/L 
    // At this point it makes sence to send the Carier

    // Antenna system names
    const string PB_Antenna = "PB - Antenna Master";
    string MyOwnAntenna = "Antenna @Base DepotMaster"; // Change this to the correct name -> Customdata
    const string AntennaMaster = "AntennaMaster";
    const string SendMessageHeader = "DepotMaster";
    string LastReceived = "";

    // Typical SE stuff
    public IMyProgrammableBlock PBMaster;

    /* Lists no! no Chopins! */
    // Basic lists
    List<IMyTerminalBlock> AllBlocks = new List<IMyTerminalBlock>();

    // Needed Lists
    static List<String> SubOreTypeList = new List<string> {  "Iron", "Nickel","Silicon", "Cobalt", "Magnesium", "Uranium", "Silver", "Gold", "Platinum", "Scrap", "Stone" };
    static List<String> SubIngotTypeList = new List<string> {  "Iron", "Nickel","Silicon", "Cobalt", "Magnesium", "Uranium", "Silver", "Gold", "Platinum", "ScrapIron", "Gravel" };
    // Alfa = Base ?
    static List<string> NATO_CODES = new List<string>(new string[] { "Alfa", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel", "India", "Juliett", "Kilo", "Lima", "Mike", "November", "Oscar", "Papa", "Quebec", "Romeo", "Sierra", "Tango", "Uniform", "Victor", "Whiskey", "Xray", "Yankee", "Zulu" }); 
    
    List<string> NeededOres = new List<string>(); // this is what we really need
    List<string> OreFetchings = new List<string>(); // this is what we ( forced ) fetch
    List<string> DefStations = new List<string>(); // these are the defined SAMCodes, the mines which exists -> No ore
    List<string> StatusStations = new List<string>(); // status of the mines or stations

    List<string> Cariers = new List<string>(); // Or drone or anything that can get ore ;)
    List<string> Waypoints = new List<string>(); // this were those Cariers should go.

    // report system
    List<String> ReportTexts = new List<string> {  
        " :-] Nothing to report\n", // 0
        " |-[ I have no Antenna\n",
        " °-[ Failed to send message\n",
        " :-] message send\n", 
        " :-> No Antenna PB\n", // obsolete
        " |-[ I have no mines\n", //5
        " |-[ I need cariers\n",
        " |-0 I need a Fe mine\n", //7
        " |-0 I need a Ni mine\n",
        " |-0 I need a Si mine\n",
        " |-0 I need a Co mine\n", // 10
        " |-0 I need a Mg mine\n",
        " |-0 I need a U mine\n",        
        " |-0 I need a Ag mine\n",
        " |-0 I need a Au mine\n",
        " |-0 I need a Pt mine\n", //15
        " |-0 I need a Gr mine\n",
        " :-] Waiting for reply\n", // not used for the moment
        " °-0 Orename does not exist\n",
        " |-[ failed station registration\n",
        " :-] registering new station\n", // 20
        " --> Testing\n"
        };

    int ReportCounter = 0;
    int ReportTimeCounter = 0;
    int ReportTime = 3;
    List<int> ReportIndexes = new List<int>();
    int CurrentIndex = 0;
    string ReportText = "";
    List<String> WhatsWrongs = new List<string> {  
        " :-] Everythings fine! \n", 
        " > Check the MyOwnAntenna definition\n",
        " > Is the Antenna active?\n",
        " °-> Antenna should receive\n something ... \n",
        " > Check name of PB\n",
        " > Go Ore hunting? \n", 
        " > Make a drone perhaps?\n",
        " > Build a Fe mine\n",
        " > Build a Ni mine\n",
        " > Build a Si mine\n",
        " > Build a Co mine\n",
        " > Build a Mg mine\n",
        " > Build a U mine\n",        
        " > Build a Ag mine\n",
        " > Build a Au mine\n",
        " > Build a Pt mine\n",
        " > Build a Gr mine\n",
        " > Do we have contact?\n",
        " > Iron, Nickle, ... \n",
        " > Check contact w station\n",
        " > should give a NATO code\n",
        " > DEBUG\n"    
        };

    string WhatsWrong = " °-| I dunno ...\n";
    bool ShowWhatsWrong = false;

    // The raw ratio converting Ore to Ingot per type %
    // formula: 50 kg of Nickel Ore x 0.4 (the ore's efficiency) x 0.8 (the refinery's base efficiency) = 16 kg of Nickel Ingots
    // Without modules
    List<float> OreIngotRatio = new List<float> { 0.7f, 0.4f, 0.7f, 0.3f, 0.07f, 0.07f, 0.1f, 0.01f, 0.005f, 0.8f, 0.9f };
    List<float> NumberOfIngots = new List<float> { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f }; // Stock of Ingots / EQ ingot

    // List<float> OldStock = new List<float>();
    List<float> NewOreStock = new List<float>();
    List<float> NewIngotStock = new List<float>();
    List<float> IngotEquivalentStock = new List<float>();
    
    Stationings FeStations = new Stationings();
    Stationings NiStations = new Stationings();
    Stationings SiStations = new Stationings();
    Stationings CoStations = new Stationings();   
    Stationings MgStations = new Stationings();
    Stationings UStations = new Stationings();
    Stationings AgStations = new Stationings();
    Stationings AuStations = new Stationings();
    Stationings PtStations = new Stationings();
    Stationings GrStations = new Stationings();    

    // History
    // public static int NumberOfData = (int)6;

    // int CurrentDayNumber = 0; // so the last will be indexed NumberOfData - 1 !
    // int NumberReadings = 0; // difference between init 0 and real 0

    // special stuff -> scrap -> gives Fe !
    // Gr(avel) -> stone

    // TimeCalculation - Aprox or with ISI
    int ProgramTick = 0;
    // int RunningTick = 0;
    // int MaxRunningTick = 5; // TODO claculate this
    int TicksPerDay = 0;
    int sunSet = 0; // ISI uses this to calculate time

    // Booleans and stuff
    bool ProgramRunning = false;
    bool HasISIPowerPB = false;

    // variables
    // TODO Actors should be marked differently
    string CarierStatus = "Error";

    string ProgramStatus = "Euh";
    string Message=" Huh ? \n";

    string NewStation = "Zulu";

    //bools
    bool ShowStatus = false;
    bool hasSend = false;
    bool hasContactWAntenna = false;
    bool ShowStations = false;
 
    // fancy stuff
    static List<char> SLIDER_ROTATOR = new List<char>(new char[] { '-', '\\', '|', '/'}); 
    DisplaySlider rotator = new DisplaySlider(Program.SLIDER_ROTATOR); 

    // ========================================= Main ================================================

public void Main(string argument, UpdateType updateSource) {
    Message="";

    if (argument.Length > 0) {
        /// This is still very much work in progress
        LastReceived = argument;

        var parts= argument.Split(':','>','/','=');
            
        // "Zulu=Test" means a new station is asking for a code
        if(parts[0].Contains("Zulu")) {
            AddReport(20);
            NewStation = "Zulu";
            // nevermind the second part ( Test )
            // Search a new unused code, but not "alfa"[0] -> base and not "Zulu"
            for(int i=1; i<NATO_CODES.Count - 1; i++) {
                if(DefStations.Count > 0){
                    if(DefStations.Contains(NATO_CODES[i])) { continue; }
                }
                NewStation = NATO_CODES[i];
                break;
            }

            // register the station
            if(NewStation != "Zulu" ) { 
                DefStations.Add(NewStation); 
                StatusStations.Add("New");
            }
            // send it to the station
            if(!SendMessage("Station=" + NewStation + "\n")) {
                AddReport(19);
            }else{
                RemoveReport(19);
                RemoveReport(20);
            }
        }
        
        // Stations=Mines will send their NatoCode -> Check in DefStations
        if(DefStations.Count > 0){

            bool StationFound = false;
            foreach( string Station in DefStations ){
                if(argument.Contains(Station)) {StationFound = true; break;}
            }

            if (StationFound) {
                // TODO checkout Ack system
                if (parts.Length == 4) {
                    string From = parts[0].Trim();
                    string OreName = parts[1].Trim();
                    AddOreStations(From, OreName);
                    // OreAmount = float.Parse(parts[2]); //Checkout what is been send is without chars !
                    if(StatusStations.Count > 0) {
                        int StationIdx = DefStations.IndexOf(From);
                        StatusStations[StationIdx]=parts[3].Trim();
                    }else{
                        Echo("StatusStations is wrong\n" );
                    }
                    SendMessage("Ack", From);
                }
            }
            // if mine=finished and Carier=Waiting -> reset Carier
        }

        if (argument.Contains("Carier")) {
            Message += " Intercepted Carier message: \n " + argument + "\n";
            var Carierparts = argument.Split(':','=');
            CarierStatus = Carierparts[1];
        }

        // Real commands
        switch (argument.ToLower()) {
            case "reset":
                ProgramRunning = false;
                ProgramTick = 0;
                TicksPerDay = 0; 
                ProgramStatus = "Stop";
                Save();                
                break;
            case "stop":
                ProgramRunning = false;
                break; 
            case "run":
                // for the moment it is running ;)
                ProgramRunning = true;
                break;
            case "time":
                TicksPerDay = ProgramTick;
                ProgramTick = 0;
                break;
            case "status":
                ShowStatus = !ShowStatus;
                break;
            case "list":
                ShowStations = !ShowStations;
                break;
            case "help":
                ShowWhatsWrong = !ShowWhatsWrong;
                break;
                default:
                // Message += " Commands:\n Run, Reset, Stop ... " + ProgramTick + "\n\n";
                break;
        }

        if (argument.ToLower().Contains("force")){
            string[] Lines=argument.Split(' ', '\n');
            toggleForced(Lines[1].Trim());
        }
    }

    
    //DEBUG
    // Echo("ProgramStatus: " + ProgramStatus + "\n");

    // check config Me.Customdata
    CheckCustomData();

    // clock
    ProgramTick++;

    // I need the ore and ingots containers on the base
    FindOreOnGrid();

    // right well a Scrap Ingot does not exist -> TODO
    FindIngotOnGrid();

    // calculating Ingot_Equivalent from Ore with formula
    // volume ore * OreIngotRatio * Refinery base Efficency
    // NumberOfIngots.Clear();
    Calculate_IE();

    // I need the average consumation of Ore and ingots
    // -> I need a history but not at every tick !
            
    for( int i=0; i<SubOreTypeList.Count; i++) {
        string TempText = "";

        NeededOres.Clear(); // Yep delete everything
            
        switch (SubOreTypeList[i]) {
            case "Iron":
                // a large armor block needs typically 25 Ingots
                // wiki says 21 Ingots, game says  7 ???

                int NumberOfPlates = (int) NumberOfIngots[0]/7;
                TempText += " Iron: ";
                if(NumberOfPlates > 5000) { TempText += " Abundance\n"; break; }
                if(NumberOfPlates > 2000) { TempText += " Good\n"; break; } 
                if(NumberOfPlates > 1000) { TempText += " Not exagerated\n"; break; }                  
                if(NumberOfPlates > 500) { TempText += " We need iron\n"; AddNeededOre("Iron"); break; }
                if(NumberOfPlates < 250) { TempText += " Critical! \n"; AddNeededOre("Iron"); break; }

                break;
            case "Nickel":
                // Message += " Nickle: " + NumberOfIngots[1] + " \n";
                // 1 large Atmo Thrusters asks for 1960 Nickle
                int NumberOfThrusters = (int) NumberOfIngots[1]/1960;
                TempText += " Nickle: ";
                if(NumberOfThrusters > 48) { TempText += " Abundance\n"; break; }
                if(NumberOfThrusters > 24) { TempText += " Good\n"; break; }
                if(NumberOfThrusters > 12) { TempText += " Not exagerated\n"; break; }               
                if(NumberOfThrusters > 6) { TempText += " We need Nickle\n"; AddNeededOre("Nickle"); break; }
                if(NumberOfThrusters < 2) { TempText += " Critical! \n"; AddNeededOre("Nickle"); break; }                            
    
                break;
            case "Silicon":
                // a solar cell asks for 170,80
                int NumberOfSolars = (int) NumberOfIngots[2]/171;
                TempText += " Silicon: "; 
                if(NumberOfSolars > 128) { TempText += " Abundance\n"; break; }
                if(NumberOfSolars > 64) { TempText += " Good\n"; break; }
                if(NumberOfSolars > 32) { TempText += " Not exagerated\n"; break; }               
                if(NumberOfSolars > 16) { TempText += " We need Silicon\n"; AddNeededOre("Silicon"); break; }
                if(NumberOfSolars < 4) { TempText += " Critical! \n";  AddNeededOre("Silicon"); break; }

                break;
            case "Cobalt":
                // Heavy armor needs 50 ingots
                int NumberOfHArmor = (int) NumberOfIngots[3]/50;
                TempText += " Cobalt: ";
                if(NumberOfHArmor > 2500) { TempText += " Abundance\n"; break; }
                if(NumberOfHArmor > 1000) { TempText += " Good\n"; break; }
                if(NumberOfHArmor > 500) { TempText += " Not exagerated\n"; break; }               
                if(NumberOfHArmor > 250) { TempText += " We need Cobalt\n"; AddNeededOre("Cobalt"); break; }
                if(NumberOfHArmor < 150) { TempText += " Critical! \n"; AddNeededOre("Cobalt"); break; } 

                break;                                
            case "Magnesium":
                // Mg is simple Nato 184 uses ... 1.00
                int NumberOfMunition = (int)NumberOfIngots[4];
                TempText += " Magnesium: ";
                if(NumberOfMunition > 2500) { TempText += " Abundance\n"; break; }
                if(NumberOfMunition > 1000) { TempText += " Good\n"; break; }
                if(NumberOfMunition > 500) { TempText += " Not exagerated\n"; break; }               
                if(NumberOfMunition > 250) { TempText += " We need Magnesium\n"; AddNeededOre("Magnesium"); break; }
                if(NumberOfMunition < 50) { TempText += " Critical! \n"; AddNeededOre("Magnesium"); break; }

                break;
            case "Uranium":
                // special case
                // at full demand a small reactor consumes 1 kg in 4min
                // a large reactor 1 kg in 12sec -> TODO do we check what reactors are on the grid ?
                int SecondsOfUranium = ( int )NumberOfIngots[5]*4;
                TimeSpan t = TimeSpan.FromSeconds( SecondsOfUranium );

                string UTime = string.Format("{0:D2}h:{1:D2}m:{2:D2}s", 
                                t.Hours, 
                                t.Minutes, 
                                t.Seconds);

                TempText += " Uranium for " + UTime + " \n"; 
                TempText += " Uranium ore: " + NewOreStock[i].ToString();
                if(NewOreStock[i] + NumberOfIngots[5] > 5) { TempText += " We are safe\n"; break; }
                if(NewOreStock[i] + NumberOfIngots[5] < 1) { TempText += " Uranium needed\n"; AddNeededOre("Uranium"); break; }                                  
                break;
            case "Silver":
                // a small reactor uses 167 a big one 3333.33
                int NumberOfSReactors = (int) NumberOfIngots[6] / 167;
                TempText += " Silver: ";
                if(NumberOfSReactors > 40) { TempText += " Abundance (Jeezes)\n"; break; }
                if(NumberOfSReactors > 20) { TempText += " Good(1 big reactor)\n"; break; }
                if(NumberOfSReactors > 10) { TempText += " No Large Reactor\n";  break; }               
                if(NumberOfSReactors > 5) { TempText += " We could use Silver\n"; AddNeededOre("Silver"); break; }
                if(NumberOfSReactors < 1) { TempText += " not even 1 small reactor \n"; AddNeededOre("Silver"); break; }                                 

                break;
            case "Gold":
                // Well only ion thrusters and Large reactor uses gold, so
                int NumberOfLReactors = (int)NumberOfIngots[7] / 67;
                TempText += " Gold: ";                   
                if(NumberOfLReactors > 6) { TempText += " A Miljonair\n"; break; }
                if(NumberOfLReactors > 4) { TempText += " Nice\n"; break; }
                if(NumberOfLReactors > 2) { TempText += " Double Power\n"; break; }               
                if(NumberOfLReactors > 1) { TempText += " You can build 1\n"; break; }
                if(NumberOfLReactors < 1) { TempText += " Can not build a large reactor \n"; AddNeededOre("Gold");; break; }  

                break;
            case "Platinum":
                // only on asteroids and moons so no wonder it is hard to find
                TempText += " Platinum: " + NumberOfIngots[8] + " \n";                

                break;
            case "Stone":
                // gravel is only used for reactor components so same thing as Silver
                int NumberOfReactors = (int) NumberOfIngots[9] / 667;
                TempText += " Gravel: "; 
                if(NumberOfReactors > 40) { TempText += " Heaps\n"; break; }
                if(NumberOfReactors > 20) { TempText += " Good(1 big reactor)\n"; break; }
                if(NumberOfReactors > 10) { TempText += " No Large Reactor\n"; break; }               
                if(NumberOfReactors > 5) { TempText += " We could use Gravel\n"; AddNeededOre("Stone"); break; }

                break;
            default:
                // scrap metal will give an error so leave it
                if(!ShowStatus) 
                {
                    Message += " Show stock: ";
                    Message += (ShowStatus) ? "[ON]\n" : "[OFF]\n"; // well he would not say ON would he ?
                }
                break;  
        }

        if(ShowStatus) Message += TempText;   
        
    }

    if(ShowStations){
        if(DefStations.Count > 0){
            Message += "\n List of " + DefStations.Count + " defined Station(s): \n";
            foreach( string Station in DefStations) {
                int StationIndex = DefStations.IndexOf(Station);
                Message += " >" + Station + ": " + StatusStations[StationIndex] + "\n";
            }
        }else{
            Message += "\n No stations defined\n";
        }
    }   

    // we could force an Uranium fetching ... ?
    // TODO to whom are we sending this ?
    if(!hasSend) { SendMessage("Idle"); }
        
    // we need something(s) to fetch the bl**dy things
    // NeededOres: what DM thinks we need
    // OreFetchings: forced ore gathering
    if (NeededOres.Count > 0 || OreFetchings.Count > 0) {
        OreFetching();
    }

    // Displaying the comments
    // Header
    string OnOff = (ProgramRunning) ? " [ON] " : " [Off] ";

    string MessageLine = " Time " + ProgramTick + rotator.GetString() + TicksPerDay + "\n " + Me.CustomName  + " " + OnOff + "\n";
        
    if(HasISIPowerPB) {
        MessageLine = " Time " + GetTimeString(ProgramTick) + "\n " + Me.CustomName  + " " + OnOff + "\n";
    }

    // Checking report system
    ReportText = CheckReport(ShowWhatsWrong);

    MessageLine += ReportText + Message;
    MessageLine += " Commands:\n Run, Reset, Stop ... \n";

    Save();

    ShowText(MessageLine,LCDNAME, true); 
}

// **************************************************************************************************************

public void toggleForced(string OreName) {
    //DEBUG
    // Echo("Toggle ore: " + OreName + "\n" );
    if(!SubOreTypeList.Contains(OreName)){
        AddReport(18); // " °-0 Orename does not exist\n"
        return;
    }
    
    RemoveReport(18);
    if (OreFetchings.Contains(OreName)) {
        OreFetchings.Remove(OreName);
    }else{
        OreFetchings.Add(OreName);
    }
    return;
}

public void CheckPBs() {
    List<IMyTerminalBlock> PB_blocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.SearchBlocksOfName( ISIPbName, PB_blocks, b => b is IMyProgrammableBlock);
        
    if (PB_blocks == null) return;
    if (PB_blocks.Count < 1) return;

    HasISIPowerPB = true;
 
    PBMaster = PB_blocks[0] as IMyProgrammableBlock;

    return;
}

public void AddNeededOre(string Ore) {
    if(NeededOres.Count == 0) NeededOres.Add(Ore); return;

    if(NeededOres.Contains(Ore)) return;

    NeededOres.Add(Ore);
    return;
}

// TODO this could become redundant ...
public void RemoveNeededOre(string Ore) {
    if(NeededOres.Contains(Ore)) NeededOres.Remove(Ore);
    return;
}

// there could be multiple mines for the same ore.
public void GetStations() {
    // What mines do we have ?
    // none
    if(DefStations.Count == 0) { AddReport(5); return; }
    RemoveReport(5);

    Waypoints.Clear();
    // ok, So what do we need ?
    for(int i=0; i<NeededOres.Count; i++) {
        // NOPE !
        int lindex = SubOreTypeList.IndexOf(NeededOres[i]) + 7;
        // that mine is not yet made !
        if (CountStations(NeededOres[i]) < 1) {
            AddReport(lindex); 
            continue; 
        }else {
            RemoveReport(lindex);
            // make a new waypoint
            string LString = FindStation(NeededOres[i]);
            if (LString != "Zulu") {
                if(!Waypoints.Contains(LString))Waypoints.Add(LString);
            }
        }
    }

   for(int i=0; i<OreFetchings.Count; i++) {
        // find index by subOretype index
        int lindex = SubOreTypeList.IndexOf(OreFetchings[i]) + 7;
        // that mine is not yet made !
        if (CountStations(OreFetchings[i]) < 1) {
            AddReport(lindex); 
            continue; 
        }else {
            RemoveReport(lindex);
            // make a new waypoint
            string LString = FindStation(OreFetchings[i]);
            if (LString != "Zulu") {
                if(!Waypoints.Contains(LString))Waypoints.Add(LString);
            }
        }
    }

    return;
}
    
public void GetCariers()
{
    if(Cariers.Count == 0) AddReport(6); return;

    RemoveReport(6);

    // TODO Exclusive brilliant code !

    return;
}

public void CheckCustomData()
{
    // If we have ISI Solar power script put it in our own
    if (HasISIPowerPB)
    {
        // hook into the PBs Customdata 
        // normaly we should have a IMyProgrammableBlock PBMaster
        // -> PB - ISI.CustomData(dayTimer = (int)Currentclicktime dayLength = (int)MaxClicksPerDay)
        string[] _ISIsData = PBMaster.CustomData.Split('\n');

        if (_ISIsData.Length >= 1)
        {
            foreach (var line in _ISIsData)
            {
                if (!line.Contains("=")) continue;
                var lineContent = line.Split('=');
                switch (lineContent[0].ToLower())
                {
                    case "daytimer":
                        if (!(Int32.TryParse(lineContent[1], out ProgramTick))) continue;
                        break;
                    case "daylength":
                        if (!(Int32.TryParse(lineContent[1], out TicksPerDay))) continue;
                        break;
                    case "sunset":
                        if (!(Int32.TryParse(lineContent[1], out sunSet))) continue;
                        break;                    
                    default:
                        break;
                }
            }
        }
        // Save puts it in Me.CustomData
        Save();
    }

    // My own, my precious ...
    if (Me.CustomData.Length < 1)
    {
        Me.CustomData = "ProgramTick=" + ProgramTick.ToString() + "\nTicksPerDay=" + TicksPerDay.ToString() + "\nAntenna=" + MyOwnAntenna + "\n";
    }
    else
    {
        string[] MyCustomData = Me.CustomData.Split('\n');
        if (MyCustomData.Length >= 1)
        {
            foreach (var line in MyCustomData)
            {
                if (!line.Contains("=")) continue;
                var lineContent = line.Split('=');

                    // DEBUG
                    // Message += "Customdata[0]: " + lineContent[0] + "\n";
                    // Message += "Customdata[1]: " + lineContent[1] + "\n";      

                switch (lineContent[0].ToLower())
                {
                    case "programtick":
                        if (!(Int32.TryParse(lineContent[1], out ProgramTick))) ProgramTick = 0;
                        break;
                    case "ticksPerday":
                        if (!(Int32.TryParse(lineContent[1], out TicksPerDay))) TicksPerDay = 0;
                        break;
                    case "antenna":
                        MyOwnAntenna = (string)lineContent[1];
                        break;
                    default:
                        break;
                }
            }
        }
    }
}

public void AddReport(int Indeks)
{
    if( ReportIndexes.Count == 0 ) 
    {
        ReportIndexes.Add(Indeks); 
        return;
    }
    if(ReportIndexes.Contains(Indeks))
    {
        return;
    }
    else 
    {
        ReportIndexes.Add(Indeks);
    } 
    return;
}

public void RemoveReport(int Indeks)
{
    if(ReportIndexes.Contains(Indeks)) ReportIndexes.Remove(Indeks);
    return;
}

public string CheckReport(bool HelpMe) {
 
    string newMessage="WTF ?";

    // check the loop timer
    ReportTimeCounter++;
    if(ReportTimeCounter > ReportTime) {
        ReportTimeCounter = 0;
        ReportCounter++;
        if(ReportCounter > ReportIndexes.Count - 1) ReportCounter = 0;
    }

    // Read the list indexes
    if(ReportIndexes.Count == 0) {
        CurrentIndex = 0;
    }else {
        CurrentIndex = ReportIndexes[ReportCounter];
    }

    //Now read the text
    if(!HelpMe) { 
        newMessage = ReportTexts[CurrentIndex];
    } else {
        WhatsWrong = WhatsWrongs[CurrentIndex];
        newMessage = ReportTexts[CurrentIndex] + WhatsWrong;
    }

    return newMessage;
}

public void FindOreOnGrid() {
    float AmountItems=0;
    GridTerminalSystem.SearchBlocksOfName("", AllBlocks, i => i.HasInventory);

    if(AllBlocks.Count == 0) return; // as unlikely as it is ...

    NewOreStock.Clear();
    // iterate through all Subtypes of Ore -> SubOreTypeList
    for(int j=0; j<SubOreTypeList.Count; j++) {
        AmountItems=0;
        // Search in all Inventories
        for (int i=0; i<AllBlocks.Count; i++) {
            var ThisContainer = AllBlocks[i];
            IMyInventory ThisStock = ThisContainer.GetInventory(0);
    
            AmountItems += countItem(ThisStock, OreType , SubOreTypeList[j]);
        } 

        NewOreStock.Add(AmountItems);
    }

    return;
}

public void FindIngotOnGrid()
    {
        float AmountItems=0;
        GridTerminalSystem.SearchBlocksOfName("", AllBlocks, i => i.HasInventory);

        if(AllBlocks.Count == 0) return; // as unlikely as it is ...

        NewIngotStock.Clear();
        // iterate through all Subtypes of Ore -> SubIngotTypeList
        for(int j=0; j<SubIngotTypeList.Count; j++)
        {
            AmountItems=0;
            // Search in all Inventories
            for (int i=0; i<AllBlocks.Count; i++)
            {
                var ThisContainer = AllBlocks[i];
                IMyInventory ThisStock = ThisContainer.GetInventory(0);
    
                AmountItems += countItem(ThisStock, IngotType , SubIngotTypeList[j]);
            } 

            NewIngotStock.Add(AmountItems);
        }

        return;
    }

    /*
        Ore has an Ingot Equivalent -> easier to keep track
    */
void Calculate_IE() {
    IngotEquivalentStock.Clear();

    for( int i=0; i<SubOreTypeList.Count; i++)
    {

        // volume ore * OreIngotRatio * Refinery base Efficency
        float IngotEquivalent=0f;
        float IngotTotal=0f;

        // Scrap gives iron so -> that goes wrong
        // Read ore the index should be good ?
        IngotEquivalent = (float)NewOreStock[i] * (float)OreIngotRatio[i] * RefineryBaseEfficency;
        // Add the Stock of real Ingot
        IngotTotal = IngotEquivalent + (float)NewIngotStock[i];

        IngotEquivalentStock.Add(IngotTotal);

        // we have a problem with scrap again 
        // Both lists should be synchronised   
        string StockName=SubIngotTypeList[i];

        switch (StockName) {
            case "Iron":
                NumberOfIngots.Insert(0, IngotTotal);
                break;
            case "Nickel":
                NumberOfIngots.Insert(1, IngotTotal);                
                break;
            case "Silicon":
                NumberOfIngots.Insert(2, IngotTotal);                
                break;
            case "Cobalt":
                NumberOfIngots.Insert(3, IngotTotal);                
                break;
            case "Magnesium":
                NumberOfIngots.Insert(4, IngotTotal);                
                break;
            case "Uranium":
                // right, now U-ingots are put IN the reactors.
                // So what about them ?
                NumberOfIngots.Insert(5, IngotTotal);                
                break;
            case "Silver":
                NumberOfIngots.Insert(6, IngotTotal);                
                break;
            case "Gold":
                NumberOfIngots.Insert(7, IngotTotal);                
                break;
            case "Platinum":
                NumberOfIngots.Insert(8, IngotTotal);                
                break;
            case "Scrap":
                break;
            case "Stone":
                NumberOfIngots.Insert(9, IngotTotal);                
                break;
            default:
                break;  
        }
    }

    return;
}

public void AddOreStations(string FromCode, string OreCode) {
    switch (OreCode) {
        case "Iron":
            FeStations.load(FromCode);
            break;
        case "Nickel":
            NiStations.load(FromCode);               
            break;
        case "Silicon":
            SiStations.load(FromCode);     
            break;
        case "Cobalt":
            CoStations.load(FromCode);                   
            break;
        case "Magnesium":
            MgStations.load(FromCode);                     
            break;
        case "Uranium":
            UStations.load(FromCode);                     
            break;
        case "Silver":
            AgStations.load(FromCode);                     
            break;
        case "Gold":
            AuStations.load(FromCode);                    
            break;
        case "Platinum":
            PtStations.load(FromCode);                    
            break;
        case "Stone":
            GrStations.load(FromCode);                    
            break;
        default:
            break;  
    }

    return;
}

public int CountStations(string OreCode) {
    int NumberOfStations=0;
    switch (OreCode) {
        case "Iron":
            NumberOfStations = FeStations.Count();
            break;
        case "Nickel":
            NumberOfStations = NiStations.Count();               
            break;
        case "Silicon":
            NumberOfStations = SiStations.Count();    
            break;
        case "Cobalt":
            NumberOfStations = CoStations.Count();                   
            break;
        case "Magnesium":
            NumberOfStations = MgStations.Count();                     
            break;
        case "Uranium":
            NumberOfStations = UStations.Count();                     
            break;
        case "Silver":
            NumberOfStations = AgStations.Count();                    
            break;
        case "Gold":
            NumberOfStations = AuStations.Count();                    
            break;
        case "Platinum":
            NumberOfStations = PtStations.Count();                   
            break;
        case "Stone":
            NumberOfStations = GrStations.Count();                   
            break;
        default:
            break;  
    }

    return NumberOfStations;
}

public string FindStation(string OreCode) {
    string FoundStation="Zulu";
    switch (OreCode) {
        case "Iron":
            FoundStation = FeStations.FindFirstStation();
            break;
        case "Nickel":
            FoundStation = NiStations.FindFirstStation();              
            break;
        case "Silicon":
            FoundStation = SiStations.FindFirstStation();   
            break;
        case "Cobalt":
            FoundStation = CoStations.FindFirstStation();                  
            break;
        case "Magnesium":
            FoundStation = MgStations.FindFirstStation();                  
            break;
        case "Uranium":
            FoundStation = UStations.FindFirstStation();                   
            break;
        case "Silver":
            FoundStation = AgStations.FindFirstStation();              
            break;
        case "Gold":
            FoundStation = AuStations.FindFirstStation();               
            break;
        case "Platinum":
            FoundStation = PtStations.FindFirstStation();                 
            break;
        case "Stone":
            FoundStation = GrStations.FindFirstStation();               
            break;
        default:
            break;  
    }

    return FoundStation;
}

float countItem(IMyInventory inv, string Type,string itemSubType) {
    var items = inv.GetItems();
    float total = 0.0f;
    for(int i = 0; i < items.Count; i++) {
        if(items[i].Content.TypeId.ToString().EndsWith(Type) && items[i].Content.SubtypeId.ToString() == itemSubType)
        {
            total += (float)items[i].Amount;
        }
    }
    return total;
}

/********************
    Ore fetching
*********************/
public void OreFetching() {
    // So I need ore ?
    // Do we have a Mine of the particular ores ?  -> Waypoints                  
    GetStations();

    // -> a mine is a container with an antenna a a drillscript (maybe not configured but with ore ?)
    // what is the Mine doing ? -> MineCar -> MineShip ?
    // Is there any Ore ?
    if (OreAmount >= MiniAmount){
        // What is the Carier doing ?
        GetCariers(); // Cariers and the like ...

        if(CarierStatus == "Idle") {
            Message += "Action: Sending Carier\n";
            if(!SendMessage("Fly Carier")) Echo("Houston we have a problem\n");   
        }
    }

    if((OreAmount == 0)&&(CarierStatus == "Waiting")) {
        Message += "Action: Calling Carier Home\n";
        if(!SendMessage("Fly Home")) Echo("Houston we have another problem\n");                 
    }

    return;
}

/****************   
    LCD   
******************/   

    public void ShowText(string Tekst, string LCDName = "LCD ComponentControl",  bool RepeatEcho = false)     
    {     
        List<IMyTerminalBlock> MyLCDs = new List<IMyTerminalBlock>();     
        GridTerminalSystem.SearchBlocksOfName(LCDName, MyLCDs);   
    
        if ((MyLCDs == null) || (MyLCDs.Count == 0))     
        {     
            Echo( "|-0 No LCD-panel found with " + LCDName + "\n" );     
            Echo(Tekst);     
        }     
        else     
        {     
            if (RepeatEcho)
            {
                Echo(Tekst);  // Control
            }

            for (int i = 0; i < MyLCDs.Count; i++)     
            {     
                IMyTextPanel ThisLCD = GridTerminalSystem.GetBlockWithName(MyLCDs[i].CustomName) as IMyTextPanel;     
                if (ThisLCD == null)     
                {     
                    Echo("°-X LCD not found? \n");     
                }     
                else     
                {     
                    // test -> Echo("Using " + MyLCDs.Count + " LCDs\n" );
    
                    ThisLCD.WritePublicText(Tekst, false);     
                    ThisLCD.ShowPublicTextOnScreen();    
                }    
            }     
        }     
    }

    /***************
    antenna system
    ***************/
List<IMyTerminalBlock> Antennas = new List<IMyTerminalBlock>();
public bool SendMessage(string SendMessage="AntennaTest", string Header = SendMessageHeader )
{
    hasSend = false;
    GridTerminalSystem.SearchBlocksOfName(MyOwnAntenna, Antennas, block => block is IMyRadioAntenna);

    if ((Antennas == null)||(Antennas.Count == 0))
    {
        AddReport(1);
        RemoveReport(4);        
        return hasSend;
    }
    else 
    {
        RemoveReport(1);
    }

    IMyRadioAntenna Antenna = Antennas[0] as IMyRadioAntenna;
    // Echo("Antenna name: " + Antenna.CustomName + "\n");
    hasSend = Antenna.TransmitMessage(Header + "=" + SendMessage);
    if(!hasSend) { 
        // Echo ("-> Error: message " + Header + "=" + SendMessage + " not send\n");
        AddReport(3);
    }else{
        RemoveReport(3); 
    }

    return hasSend;
}

public static double PercentOf(double numerator, double denominator)
{
    double percentage = Math.Round(numerator / denominator * 100, 1);
    if (denominator == 0) {
        return 0;
    } else {
        return percentage;
    }
}

    /*********************
        Fancy stuff
    ***********************/
public class DisplaySlider 
{ 
    public List<char> displayList; 
    public DisplaySlider(List<char> l) 
    { 
        displayList = new List<char>(l); 
    } 
    public string GetString() 
    { 
        displayList.Move(this.displayList.Count() - 1, 0); 
        return displayList.First().ToString(); 
    } 
} 

/* ISIs time calculation */
// do NOT use this if there is not PB !
/// <summary>
/// Create a time string based on a double value
/// </summary>
/// <param name="timeToEvaluate">Any double value</param>
/// <param name="returnHour">Optional: true only returns the hour</param>
/// <returns>String like "16:30"</returns>
string GetTimeString(double timeToEvaluate, bool returnHour = false)
{
	string timeString = "";

	// Mod the timeToEvaluate by dayLength in order to avoid unrealistic times
	timeToEvaluate = timeToEvaluate % TicksPerDay;

	// Calculate Midnight
	double midNight = sunSet + (TicksPerDay - sunSet) / 2D;

	// Calculate Time
	double hourLength = TicksPerDay / 24D;
	double time;
	if (timeToEvaluate < midNight) {
		time = (timeToEvaluate + (TicksPerDay - midNight)) / hourLength;
	} else {
		time = (timeToEvaluate - midNight) / hourLength;
	}

	double timeHour = Math.Floor(time);
	double timeMinute = Math.Floor((time % 1 * 100) * 0.6);
	string timeHourStr = timeHour.ToString("00");
	string timeMinuteStr = timeMinute.ToString("00");

	timeString = timeHourStr + ":" + timeMinuteStr;

	if (returnHour) {
		return timeHour.ToString();
	} else {
		return timeString;
	}
}

public class Stationings {

    string OreName;
    List<string> Stations = new List<string>();
    
    public Stationings() {
    }

    public void MyOreName (string Name){
        OreName = Name;
    }

    public void load(string NCode) {
        if(Stations.Count < 1) { Stations.Add(NCode); return; }
        if(!Stations.Contains(NCode)) Stations.Add(NCode);
    }

    public void Unload(string NCode) {
        if(Stations.Count < 1) { return; }   
        if(Stations.Contains(NCode)) Stations.Remove(NCode);        
    }

    public int Count() {
        return Stations.Count;
    }

    public string FindFirstStation(){
        string FoundStation = "Zulu"; // zulu can not be = error
        if(Stations.Count > 0) { FoundStation = Stations[0]; }
        return FoundStation;
    }
}