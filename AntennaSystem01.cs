/*****************************************
  Grid connection system -- AntennaMaster
******************************************/

/*
    * 1.0 Establishing first connection -- we are still very much debugging this
    * Building a usable Log
    * Refresh of the LCD goes to fast
    * Implementing ISI time keeping -> PB has a dayTimer=in / dayLength = int in CustomData
    * this should find Depotmaster
    * this should find Stationmaster (or backwards ? Or both ?)
    * Using List<string> Actors --> set them in CustomData 
        --> "Actorname"
    * It actually does not matter if it comes through the antenna or internally by a PB.Tryrun()
    * TODO should be handschake(d)
        --> so what should we do when a message arrives?
    * used ISI's time calculation & if available
     TODO Antennas will not work if the PB is on the grid -> changing
        --> every PB on the grid is potentially an Actor -> Actors list ?
 */

public Program()    
{    
    Runtime.UpdateFrequency = UpdateFrequency.Update10;

    CheckPBs();
}    

public void Save()
{
}

// This is persistent !
const string version = "1.07";

// Definitions
const string LCDNAME = "AntennaLCD";
string MyOwnAntenna = "Antenna @Base Transmitter";
const string SendMessageHeader = "AntennaMaster";

// some fancy stuff
public static List<char> SLIDER_ROTATOR = new List<char>(new char[] { '-', '\\', '|', '/'}); 
public DisplaySlider rotator = new DisplaySlider(Program.SLIDER_ROTATOR); 

// Text
string Header = " Error Header\n";     
string Message = "If this shows: something is wrong\n";
string LastReceived = "";


// Timing stuff
int ProgramTick = 0;
int TicksPerDay = 0;
const string ISIPbName = "Solar Power"; // correct ?
int sunSet = 0; // ISI uses this to calculate time

// lists
// List<IMyTerminalBlock> AllBlocks = new List<IMyTerminalBlock>();
List<string> WarningText = new List<string>();
List<string> ReceiveLog = new List<string>();
List<string> SpamLog = new List<string>();
// List<string> Actions = new List<string>();
        
List<string> Actors = new List<string>();
List<string> ReceivedNames = new List<string>();
List<string> PB_Names = new List<string>();
List<string> PBOnGrids = new List<string>();

string Actor = "Me";

// Typical SE stuff
public IMyProgrammableBlock PBMaster;

// anything in Main is subject to change
// int ProgramRunner = 0;
bool Showspam = true;
bool FoundActor = false;
bool MeCommand = false;
bool HasISIPowerPB = false;


/*****************
    Main loop
 ****************/

public void Main(string argument, UpdateType updateSource)    
{

    // Display setup
    Message="";
    MeCommand = false;

    Header = " " + rotator.GetString() + Me.CustomName + " " + version + "\n";
    if(HasISIPowerPB)
    {
        ReadTime();
        Header = " " + GetTimeString(ProgramTick) + " " + Me.CustomName + " " + version + "\n";
    }

    // Config
    ReadCustomData();
    AddWarning(" 01 :-] ", "Configured " + Actors.Count + " Actors" );

    // Command center
    if (argument.Length <= 0)
    {
        // You get ADHD from this --
        // AddWarning(" 01 --> ", "Nothing new" );            
    }
    else
    {
        AddWarning(" 02 --> ", argument );
        RemoveWarning(" 01 --> ");
       //Manual arguments
        switch (argument.ToLower())
        {
            case "list":
                ListActors();
                AddItemInLog("Me: ", argument);
                MeCommand = true;
                break;
            case "spam":
                MeCommand = true;                            
                Showspam = !Showspam;
                AddItemInLog("Me: ", argument + ((Showspam) ? " On" : " Off" ));                    
                break;
            case "clear":
                ClearMe();
                MeCommand = true;
                break;               
            default:
                MeCommand = false;
                break;
        }

        //Received stuff
        if(!MeCommand)
        {
            string ReceivedMessage=argument;

            //DEBUG
            AddSpam("Spam", ReceivedMessage);

            FoundActor = false;

            if((Actors != null) || (Actors.Count > 0))
            {
                CheckActors(ReceivedMessage);
                RemoveWarning(" 01 |-[ ");   
            }
            else
            {
                AddWarning(" 01 |-[ ", " No Actors defined -> Check CustomData\n");             
            }

            // DEBUG
            // Echo("Found Actor:" + FoundActor + "\n" );
            if(!FoundActor)
            {
                // DEBUG
                Echo("Actor NOT Found:" + FoundActor + "\n" );                
                AddSpam("Spam", ReceivedMessage);
            }

        }
     }

    Display();

}

// ========================================================================================================================

public void ListActors()
{
    for(int i=0; i<Actors.Count; i++)
    {
        AddItemInLog("Me" + i + ": ", Actors[i]);
    }   
}

public void ClearMe()
{
    RemoveWarning("Me: ");
    RemoveItemInLog("Me: ");
    Actors.Clear();
    ReceivedNames.Clear();
    PB_Names.Clear();
}

// Always the same: Look for a PB and send the WhoWhat
public void CallActor(string ActorsName, string Who, string What )
{
    IMyProgrammableBlock Master;
    List<IMyTerminalBlock> PB_blocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.SearchBlocksOfName(ActorsName, PB_blocks, b => b is IMyProgrammableBlock);
    
    // IMyProgrammableBlock Master = PB_blocks[0] as IMyProgrammableBlock;
    if(PB_blocks.Count < 1) { AddWarning(" 08 |-[ ", "No PBs found with name: " + ActorsName); return; }

    Master = PB_blocks[0] as IMyProgrammableBlock;

    if (Master == null)
    {
        AddWarning(" 02 |-[ ", "Can not find " + ActorsName);        
    }

    if(Master.TryRun(Who + ": " + What + "\n")) 
    {
        RemoveWarning(" 02 |-[ "); 
        RemoveWarning(" 05 |-[ ");
    }
    else
    {
        AddWarning(" 05 |-[ ", "Could not notify " + Master.CustomName);
    }
    return;
}

public string PBOnGrid(string SearchPB){
    
    string PBfound="";
    CheckPBs();
    // if it exists it is on PBOnGrid list
    if(PBOnGrids.Count>0){
        for(int i=0; i<PBOnGrids.Count; i++){
            if(SearchPB == PBOnGrids[i]) { PBfound=PBOnGrids[i]; }
        }
    }

    return PBfound;
}

// Get all the PB on the grid
public void CheckPBs()
{
    List<IMyTerminalBlock> PB_blocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.SearchBlocksOfName( ISIPbName, PB_blocks, b => b is IMyProgrammableBlock);
    // GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(PB_blocks);
        
    if (PB_blocks == null) return;
    if (PB_blocks.Count < 1) return;

    PBMaster = PB_blocks[0] as IMyProgrammableBlock;

    return;
}

public void ReadTime()
{
   // If we have ISI Solar power script put it in our own script
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
    }
    return;
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

public void ReadCustomData()
{
    string MyCustomData = Me.CustomData;

    if(MyCustomData == "")
    {
        AddWarning(" 03 |-[ ", "CustomData is Empty\n");
        return;       
    }
    else
    {
        RemoveWarning(" 03 |-[ ");
        Actors.Clear();
        ReceivedNames.Clear();
        PB_Names.Clear();

        //Actor should be ActorName = "Received name"
        // ex: Drone:DroneStatus=
        var Lineparts = MyCustomData.Split('\n');
        foreach(string Line in Lineparts)
        {
            var parts = Line.Split(':','=');
            string Actor = parts[0];
            string ReceivedName = parts[1];
            string PB_Name = parts[2];
 
            // Saving Data
            AddActor(Actor, ReceivedName, PB_Name);
        }
    }
}

public void CheckActors(string GotMail)
{
    for(int i=0; i<ReceivedNames.Count; i++)
    {
       if (GotMail.Contains(ReceivedNames[i]))
        {
            // format should be Name=Message ( Message can not have '=' huh !)
            var ReceivedMessage = GotMail.Split('=');
            AddItemInLog(Actors[i], ReceivedMessage[1]);
            FoundActor = true;
            /*
            if(PbOnGrid(PB_Names[i]) != ""){
                // Send its own name back not the Antennas' !
                CallActor(PB_Names[i], ReceivedNames[i] ,"Ack");
            }
            else {
                SendMessage(PB_Names[i] + ":Ack");
            }
            */
        }
    }
}

// Setting up the LCD
public void Display()
{
    string DisplayString ="";
    string WarningString ="";
    string SpamString = "";

    IEnumerable<string> sortAscendingWarning =
            from Warning in WarningText
            orderby Warning //"ascending" is default
            select Warning;

    // foreach(string Line1 in WarningText )
    foreach(string Line1 in sortAscendingWarning )
    {
        WarningString += " " + Line1;       
    }

    IEnumerable<string> sortAscendingLog=
            from Log in ReceiveLog
            orderby Log //"ascending" is default
            select Log;

    // foreach(string Line2 in ReceiveLog )
     foreach(string Line2 in sortAscendingLog )   
    {
        DisplayString += " " + Line2;       
    }

    if (Showspam)
    {
        foreach(string Line3 in SpamLog )
        {
            SpamString += " " + Line3;       
        }
        ShowText(Header + " ---Warnings------------\n" + WarningString + " ----Log--------------------\n" + DisplayString +  " --Spam-------------------\n" + SpamString + "\n" , LCDNAME, true);
    }
    else
    {
        ShowText(Header + " ---Warnings------------\n" + WarningString + " ----Log--------------------\n" + DisplayString + "\n" , LCDNAME, true);
    }

    return;
}

/****************   
    LCD   
******************/   
    
public void ShowText(string Tekst, string LCDName = "Testing",  bool RepeatEcho = false)     
{     
    List<IMyTerminalBlock> MyLCDs = new List<IMyTerminalBlock>();     
    GridTerminalSystem.SearchBlocksOfName(LCDName, MyLCDs, block => block is IMyTextPanel);
   
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

/********************
  Receivelog control
*********************/
public void AddItemInLog(string SearchString, string Text)
{
    // string CurrentTime=DateTime.Now.ToString("hh:mm:ss");
    // Message = SearchString + Text + "(" + CurrentTime + ")" + "\n";
    if(HasISIPowerPB)
    {
        Message = GetTimeString(ProgramTick) + " " + SearchString + ": " + Text + "\n";
    }
    else
    {
        Message = SearchString + ": " + Text + "\n";        
    }

    for ( int i = 0; i < ReceiveLog.Count; i++)
    {
        if(ReceiveLog[i].Contains(SearchString)) ReceiveLog.RemoveAt(i);
    }

    ReceiveLog.Add(Message);

    return;
}

public void RemoveItemInLog(string SearchString)
{
    for ( int i = 0; i < ReceiveLog.Count; i++)
    {
        if(ReceiveLog[i].Contains(SearchString)) ReceiveLog.RemoveAt(i);
    }

    return;
}

public void AddSpam(string SearchString, string Text)
{
    Message = SearchString + ": " + Text + "\n";

    for ( int i = 0; i < SpamLog.Count; i++)
    {
        if(SpamLog[i].Contains(SearchString)) SpamLog.RemoveAt(i);
    }

    SpamLog.Add(Message);

    return;
}

public void AddWarning(string SearchString, string Text)
{
    // string CurrentTime=DateTime.Now.ToString("hh:mm:ss");

    Message = SearchString + Text + "\n";

    for ( int i = 0; i < WarningText.Count; i++)
    {
        if(WarningText[i].Contains(SearchString)) WarningText.RemoveAt(i);
    }

    WarningText.Add(Message);

    return;
}

public void RemoveWarning(string SearchString)
{
    for ( int i = 0; i < WarningText.Count; i++)
    {
        if(WarningText[i].Contains(SearchString)) WarningText.RemoveAt(i);
    }

}

public void AddActor(string ActorName, string CallName, string RealName)
{
    //IMyProgrammableBlock Master;
    // The realName Should have a PB
    List<IMyTerminalBlock> PB_blocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.SearchBlocksOfName(RealName, PB_blocks, b => b is IMyProgrammableBlock);

    if (PB_blocks == null)
    {
       AddWarning(" 04 |-[ ", "Can not find any " + RealName + "\n");
    }

    // Master = PB_blocks[0] as IMyProgrammableBlock;

    if (Actors.Count > 0)
    {
        for ( int i = 0; i < Actors.Count; i++)
        {
            if(Actors[i].Contains(ActorName)) Actors.RemoveAt(i);
        }
    }
    Actors.Add(ActorName);

    if (ReceivedNames.Count > 0)
    {
        for ( int i = 0; i < ReceivedNames.Count; i++)
        {
            if(ReceivedNames[i].Contains(CallName)) ReceivedNames.RemoveAt(i);
        }
    }
    ReceivedNames.Add(CallName); 

    // You Can have double PB_names !
    PB_Names.Add(RealName); 

    RemoveWarning(" 04 |-[ ");

    return;
}

// Send procedure
List<IMyTerminalBlock> Antennas = new List<IMyTerminalBlock>();
public void SendMessage(string SendMessage="Ack")
{
    bool hasSend = false;
    // Ok I will try it myself
    GridTerminalSystem.SearchBlocksOfName(MyOwnAntenna, Antennas, block => block is IMyRadioAntenna);

    if ((Antennas == null)||(Antennas.Count == 0))
    {
        AddWarning(" 06 |-[ ", "Can not find any " + MyOwnAntenna + "\n");
        return;
    }
    else 
    {
        RemoveWarning(" 06 |-[ ");
        RemoveWarning(" 08 |-[ ");
    }

    IMyRadioAntenna Antenna = Antennas[0] as IMyRadioAntenna;

    hasSend = Antenna.TransmitMessage(SendMessage);
    if(!hasSend){ AddWarning(" 07 |-[ ", "Can NOT send " + SendMessage + "\n"); }

    RemoveWarning(" 07 |-[ ");   
    return;
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

/**************
  Fancy stuff 
 **************/

public class DisplaySlider 
{ 
    public List<char> displayList; 
    public DisplaySlider(List<char> l) 
    { 
        this.displayList = new List<char>(l); 
    } 
    public string GetString() 
    { 
        this.displayList.Move(this.displayList.Count() - 1, 0); 
        return this.displayList.First().ToString(); 
    } 
} 