/********************************************
	Ore Transport Ship
*********************************************/

/********************************************
 ISI has a fabulous program wich let you choose by customdata
 What kind of item you want and how many... 
 Problem: both ISI Managers @base and @destiantion use
 the same Customdata, which means it does not unload.
 So it has to change "customdata" accordingly.
 efine the amounts of items you want to store in this container.
Negative amounts will act as a limiter and will remove any items
above the set value without putting items into the container.

Example: -100 will remove items when their quantity is above 100

put this in the customdata of the Programable block

Component/BulletproofGlass 0=0
Component/Computer 0=0
Component/Construction 0=0
Component/Detector 0=0
Component/Display 0=0
Component/Explosives 0=0
Component/Girder 0=0
Component/InteriorPlate 0=0
Component/LargeTube 0=0
Component/Medical 0=0
Component/MetalGrid 0=0
Component/Motor 0=0
Component/PowerCell 0=0
Component/RadioCommunication 0=0
Component/Reactor 0=0
Component/SmallTube 0=0
Component/SolarCell 0=0
Component/SteelPlate 0=0
Component/Thrust 0=0
Ore/Cobalt 0=0
Ore/Gold 0=0
Ore/Ice 0=0
Ore/Iron 0=0
Ore/Magnesium 0=0
Ore/Nickel 0=0
Ore/Scrap 0=0
Ore/Silicon 0=0
Ore/Silver 0=0
Ore/Stone 0=0
Ore/Uranium 0=0
Ingot/Cobalt 0=0
Ingot/Gold 0=0
Ingot/Iron 0=0
Ingot/Magnesium 0=0
Ingot/Nickel 0=0
Ingot/Silicon 0=0
Ingot/Silver 0=0
Ingot/Stone 0=0
Ingot/Uranium 0=0
AmmoMagazine/Missile200mm 0=0
AmmoMagazine/NATO_25x184mm 0=0
AmmoMagazine/NATO_5p56x45mm 0=0
OxygenContainerObject/OxygenBottle 0=0
GasContainerObject/HydrogenBottle 0=0
PhysicalGunObject/AngleGrinder2Item 0=0
PhysicalGunObject/AngleGrinder3Item 0=0
PhysicalGunObject/AngleGrinderItem 0=0
PhysicalGunObject/AutomaticRifleItem 0=0
PhysicalGunObject/HandDrill2Item 0=0
PhysicalGunObject/HandDrill3Item 0=0
PhysicalGunObject/HandDrillItem 0=0
PhysicalGunObject/RapidFireAutomaticRifleItem 0=0
PhysicalGunObject/Welder3Item 0=0
PhysicalGunObject/WelderItem 0=0


If this is in the customdata of the connector of each station ( ship ? )
We could read it.
Bassicaly what it should do is witching between !special and !locked

 *********************************************/

/********************************************
	Transport Ship
*********************************************/


public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    string[] storedData = Storage.Split(';');

    if(storedData.Length >= 1)
    {
        ProgramStatus = storedData[0];
    }
    
    if(storedData.Length >= 2)
    {
        Position = storedData[1];
    }

}

public void Save()
{
    Storage = string.Join(";",
        ProgramStatus ?? "GotoBase",
        Position ?? "@Base");
}

const string VERSION = "0.01";

const string LCDNAME = "DroneLCD";
const string FREIGHTCONTAINER = "MContainer Drone Remorque";
const string MINECONTAINER = "Container @Mine";
const string DRONECONNECTOR = "Drone Connector Remorque [SAM]";
const string DRONEBATTERY = "Drone Battery";
const string DRONESAM = "Programmable block [SAM]";
const string PBDRONE ="PB - AutoDrone";

// Keyword for the Connectors & Containers
const string lockedContainer = "Locked";
const string specialContainer = "Special";

// necessary definitions
bool SAMRunOK = false;
int RunNumber = 0;

string ProgramStatus = "Euh";

string Position = "Error";

double ContainerPercent = 99D;

public IMyCargoContainer OreContainer;
// there are only one of these
public IMyShipConnector DroneConnector;
public IMyProgrammableBlock PBDrone;

// Listings
List<IMyThrust> MyThrusters = new List<IMyThrust>();
List<IMyTerminalBlock> specialContainers = new List<IMyTerminalBlock>();

/* ISI */



string Message = "If this shows: something is wrong\n";

public void Main(string argument, UpdateType updateSource)
{
    Message = "Drone Control " + VERSION + " ... \n";

    // we need thrusters - .. or we don't fly
    

    // We need a connector & its inventory
    int ConnectorStatus = CheckConnector();
    switch (ConnectorStatus)
    {
        case -1:
            Message += " --> Connector not found or not usable\n";
            break;
        case 0:
            Message += " --> Connector can not connect to annything\n";
            break;
        case 1:
            // SAM Missed this or we are Manual
            Message += " °-] Connector is not connected\n --> but could connect\n";
            // leave it to SAM ;) ConnectConnector();
            break;
        case 2:
            Message += " --> Connector is connected\n";
            break;
        default:
            Message += " --> this is akward check Connector\n";
            break;
    }

    // We need container ( inventory and contenance)
    // this should be an amount and check it with a amount given as a double
    string ContainerStatus = CheckContainer();

    switch (ContainerStatus)
    {
        case "Full":
            Message += " ==> " + FREIGHTCONTAINER + " full\n";
            break;
        case "Empty":
            Message += " --> " + FREIGHTCONTAINER + " empty\n";
            break;
        case "Error":
        default:
            Message += " |-] " + FREIGHTCONTAINER + " error\n";
            break;    
    }

     
    // We need power -> Battery -> reactor needs Uranium Ingot - Solar panels don’t
    float PowerStatus = Checkbatteries();
    // E=P*t => E/p = t
    float TotalSeconds = ( PowerStatus * 3600)/ CurrentOut;
    int Secondstime = Convert.ToInt32(Math.Floor(TotalSeconds));
    TimeSpan BatterieTime = new TimeSpan(0,0,0,Secondstime);

    string BatterieTimeFormated = string.Format("{0:D2} {1:D2}:{2:D2}:{3:D2}",
                BatterieTime.Days, 
                BatterieTime.Hours, 
                BatterieTime.Minutes, 
                BatterieTime.Seconds
                );
    Message += " --> Energy until: " + BatterieTimeFormated + " \n";
    // how do we know there is enough power ? -> we don't ... until we fly !
    // We need enough Thrusters -> at least 2 large ones if you have medium container
    // float UsedToEnergy
    // float UsedFromEnergy
    //DEBUG
    Message += "Used Energy: " + UsedToEnergy.ToString("0.000") + " / " + UsedFromEnergy.ToString("0.000") + " \n";
 
    // We need a PC with SAM -> with connection ?
    if (!CheckSAM())
    {
        Message += " |-] No SAM available? \n";
        return;        
    }

    // DEBUG
    Message += "Position: " + Position + "\n";
    
    // DEBUG
    Message += "ProgramStatus: " + ProgramStatus + "\n";

    string MineContainerStatus = CheckMine();

    switch (ProgramStatus)
    {
        case "Wait":
            ProgramStatus = "GotoBase";
            break;
        case "WaitToFuel":
        case "WaitFromFuel":
            Message += " --> Texaco\n";
            break;
        case "@Mine":
           if(ContainerStatus != "Full")
            {
                ProgramStatus = "@Mine";
                Save();
                     
                //Check Mine Container
                switch(MineContainerStatus)
                {
                    case "Empty":
                        Message += " --> " + MINECONTAINER + " empty\n";
                        break;
                    case "Full":
                        Message += " ==> " + MINECONTAINER + " full\n";
                        // transfer
                        OreTransfer();
                        break;
                    default:
                        Message += " --> " + MINECONTAINER + " with ore\n";
                        // transfer
                        OreTransfer();
                        break;        
                }
            }

            if(MineContainerStatus == "Empty"){break;}
            if((UsedFromEnergy > 0)&&(UsedFromEnergy > StoredEnergy))
            {
                ProgramStatus = "WaitFromFuel";
            }            
            else
            {
		        ProgramStatus = "GotoBase";
                Save();
                // transfer to Ship Container(s) 
		        // if full
			        // stop transfer
			        // Set SAM to base
			        // Start SAM
            }
            break;
        case "GotoMine":
            GotoMine();
            ProgramStatus = "Arrived@Mine";
            Save();
            FuelUse("StartCounting", PowerStatus );
            Message += " --> Going to the Mine\n";
            break;
        case "Arrived@Mine":
            Message += "ConnectorStatus: " + ConnectorStatus.ToString() + "\n";
            if(ConnectorStatus == 2)
            {
                Position = "@Mine";
                ProgramStatus = "@Mine";
                FuelUse("StopCounting", PowerStatus );
                Message += " --> Arrived @ Mine\n";
                SAMRunOK = false;
                Save();                 
            }
            else
            {
                FuelUse("StopCounting", PowerStatus );                
                Message += " --> Flying °°°\n";
            }            
            break;
        case "@Base":
           if(ContainerStatus != "Empty")
            {
                ProgramStatus = "@Base";
                // Ore master should get all ore if containers/connectors have Remorque in the name
                Save();
                break;
            }
            
            if((UsedToEnergy > 0)&&(UsedToEnergy > StoredEnergy))
            {
                ProgramStatus = "WaitToFuel";
            }
            else
            {
		        // == 0 -> Fetch new ore
                ProgramStatus = "GotoMine";
                Save();
            }

            // -> Waiting
		    // Check fuel -> Amount in stock ? Conveyor ? 

            break;
        case "GotoBase":
            GotoBase();
            ProgramStatus = "Arrived@Base";
            FuelUse("StartBaseCounting", PowerStatus );
            Message += " --> Going Home\n";
            Save();
            break;
        case "Arrived@Base":
            if(ConnectorStatus == 2)
            {
                Position = "@Base";
                ProgramStatus = "@Base";
                FuelUse("StopBaseCounting", PowerStatus );
                Message += " --> Arrived @ Base\n";
                SAMRunOK = false;
                Save();                 
            }
            else
            {
                FuelUse("StopBaseCounting", PowerStatus );
                Message += " --> Flying Home\n";
            }            
            break;                
        
    }

    ShowText (Message, LCDNAME, true);
}

/**********************
    Programmable block
 **********************/
public bool CheckProgrammableBlock()
{
    IMyProgrammableBlock  PBDrone = GridTerminalSystem.GetBlockWithName(PBDRONE) as IMyProgrammableBlock;

    if (PBDrone == null)
    {
        Message += PBDRONE + " Not found ???\n";
        return false;
    }

    // We need to get the Customdata
    var BlockData = PBDrone.Customdata.split("/n");
    
    //checking the data


}


/**************
    Container
 **************/
 // this should better return an amount 

/* ISI
		var inventory = container.GetInventory(0);
		string percent = ((double)inventory.CurrentVolume).PercentOf((double)inventory.MaxVolume);

		if (showFillLevel) {
			newName += " (" + percent + ")";
			newName = newName.Replace("  ", " ");
		}

		if (newName != oldName) container.CustomName = newName;
 */

public double CheckContainer()
{
    double OreContainerStatus = 0D;
    // No there is only one container ... for the moment
    IMyCargoContainer OreContainer = GridTerminalSystem.GetBlockWithName(FREIGHTCONTAINER) as IMyCargoContainer;
    if(OreContainer == null)
    {
        Message += "WTF? \n";
        return OreContain;
    }

    IMyInventory ThisStock = OreContainer.GetInventory(0);

    if(ThisStock.IsFull){OreContainerStatus = 100D;}
    if(ThisStock.CurrentVolume == 0)
    {
        return OreContainerStatus;
    }
    else
    {

    }
 
   	string percent = ((double)ThisStock.CurrentVolume).PercentOf((double)ThisStock.MaxVolume);


    return OreContainerStatus;
}

// Check the inventory of the Mine connector
public string CheckMine()
{
    string MiningContainerStatus = "Error";

    IMyShipConnector ThisOreContainer = GridTerminalSystem.GetBlockWithName(MINECONTAINER) as IMyShipConnector;
    if(ThisOreContainer == null)
    {
        Message += "WTF? \n";
        return MiningContainerStatus;
    }

    IMyInventory ThisStock = ThisOreContainer.GetInventory(0);
    findOre(ThisStock);

    if(OreTypes.Count > 0)
    {
        if(ThisStock.IsFull){MiningContainerStatus = "Full";}
    }

    if(ThisStock.CurrentVolume == 0){MiningContainerStatus = "Empty";}
    
    return MiningContainerStatus;
}

void OreTransfer()
{
    IMyShipConnector MineOreContainer = GridTerminalSystem.GetBlockWithName(MINECONTAINER) as IMyShipConnector;
    IMyInventory MineStock = MineOreContainer.GetInventory(0);    
    IMyCargoContainer FreightContainer = GridTerminalSystem.GetBlockWithName(FREIGHTCONTAINER) as IMyCargoContainer;
    IMyInventory FreightStock = FreightContainer.GetInventory(0);

    findOre(MineStock);
    for(int i = 0; i < OreTypes.Count; i++)
    {
        float OreAmount = countItem(MineStock, OreTypes[i]);
        transfer(MineStock, FreightStock, "Ore", OreTypes[i], OreAmount);
    }
}

/**************
   Connector
**************/
public int CheckConnector()
{
	int Status = -1;
    // Unconnected	0	This connector is not connected to anything, nor is it near anything connectable.
    // Connectable	1	This connector is currently near something that it can connect to.
    // Connected	2	This connector is currently connected to something.
    IMyShipConnector DroneConnector = GridTerminalSystem.GetBlockWithName(DRONECONNECTOR) as IMyShipConnector;
   
    if(DroneConnector == null){return -1;}
    
    MyShipConnectorStatus DroneConnectorStatus = DroneConnector.Status;

    switch (DroneConnectorStatus)
    {
        case MyShipConnectorStatus.Connected:
            Status = 2;
            break;
        case MyShipConnectorStatus.Connectable:
            Status = 1;
            break;
        case MyShipConnectorStatus.Unconnected:
            Status = 0;
            break;
        default:
            Status = -1;
            break;
    }

    return Status;
}

public void ConnectConnector()
{
    IMyShipConnector DroneConnector = GridTerminalSystem.GetBlockWithName(DRONECONNECTOR) as IMyShipConnector;
    if(DroneConnector != null)
    {
        DroneConnector.Connect();
    }
    else
    {
        Message += " |-[ could not connect " + DRONECONNECTOR + " \n";
    }
}

/*****************
    batteries
*******************/
float MaxEnergy = 0.0f;
float StoredEnergy = 0.0f;
float CurrentIn = 0.0f;
float CurrentOut = 0.0f;

public float Checkbatteries()
{
  StoredEnergy  = 0.0f;
  MaxEnergy = 0.0f;  
  CurrentIn =  0.0f;  
  CurrentOut = 0.0f;  
  
  IMyBatteryBlock Battery = GridTerminalSystem.GetBlockWithName(DRONEBATTERY) as IMyBatteryBlock;
  if(Battery != null)
  {
    StoredEnergy = Battery.CurrentStoredPower;
    MaxEnergy = Battery.MaxStoredPower;  
    CurrentIn =  Battery.CurrentInput;  
    CurrentOut = Battery.CurrentOutput;
   }
   else
   {
       Echo("huh? No battery ? \n");
   }

  return StoredEnergy;
}

/*********
    SAM
**********/

public bool CheckSAM()
{
   IMyProgrammableBlock PCSam = GridTerminalSystem.GetBlockWithName(DRONESAM) as IMyProgrammableBlock;
   if(PCSam == null)
   {
       return false;
   }
   return true;   
}

public void GotoMine()
{

   IMyProgrammableBlock PCSam = GridTerminalSystem.GetBlockWithName(DRONESAM) as IMyProgrammableBlock;

   if(!SAMRunOK)
    {
        RunNumber++;
        Message += "Running Sam ..." + RunNumber + "\n";
        PCSam.TryRun("DOCK NEXT");
        PCSam.TryRun("NAV START");    
        SAMRunOK = true;
    }
}

public void GotoBase()
{

   IMyProgrammableBlock PCSam = GridTerminalSystem.GetBlockWithName(DRONESAM) as IMyProgrammableBlock;

   if(!SAMRunOK)
    {
        RunNumber++;
        Message += "Running Sam ..." + RunNumber + "\n";
        PCSam.TryRun("DOCK PREV");
        PCSam.TryRun("NAV START");    
        SAMRunOK = true;
    }
}

// between 2 Docks -> connected connectors
// ToMine -> DeltaEnergy
// FromMine - > DeltaEnergy
// Energy can only go down !

float StartEnergy = 0.0f;
float EndEnergy = 0.0f;
float DeltaEnergy = 0.0f;
float UsedToEnergy = 0.0f;
float UsedFromEnergy = 0.0f;

public void FuelUse(string Parameter, float EnergyReading)
{
    switch(Parameter)
    {
        case "StartCounting":
            StartEnergy = EnergyReading;
            EndEnergy = 0.0f;
            break;
        case "StopCounting":
            // if the current energy is higher then the startenergy
            // then we connected to a Grid with power and that does not count
            if (EnergyReading < StartEnergy)
            {
                EndEnergy = EnergyReading;
            }
            UsedToEnergy = StartEnergy - EndEnergy;
            break;
        case "StartBaseCounting":
            StartEnergy = EnergyReading;
            EndEnergy = 0.0f;
            break;
        case "StopBaseCounting":
            if (EnergyReading < StartEnergy)
            {
                EndEnergy = EnergyReading;
            }
            UsedFromEnergy = StartEnergy - EndEnergy;
            break;
        default:
            break;
    }
}

//-----------------------------------------------------------------------------

List<string> OreTypes = new List<string>();

public void findOre(IMyInventory inv)
{
    OreTypes.Clear();
    var items = inv.GetItems();
    for(int i = 0; i < items.Count; i++)
    {
        if(items[i].Content.TypeId.ToString().EndsWith("Ore"))
        {
            OreTypes.Add(items[i].Content.SubtypeId.ToString());
        }
    }

}

public float countItem(IMyInventory inv, string itemSubType)
{
    var items = inv.GetItems();
    float total = 0.0f;
    for(int i = 0; i < items.Count; i++)
    {
        if(items[i].Content.TypeId.ToString().EndsWith("Ore") && items[i].Content.SubtypeId.ToString() == itemSubType)
        {
            total += (float)items[i].Amount;
        }
    }
    return total;
}

void transfer(IMyInventory FromStock, IMyInventory ToStock, string type, string subType, float amount)
{
    var items = FromStock.GetItems();
    float left = amount;
    for(int i = items.Count - 1; i >= 0; i--)
    {
        if(left > 0 && items[i].Content.TypeId.ToString().EndsWith(type) && items[i].Content.SubtypeId.ToString() == subType)
        {
            if((float)items[i].Amount > left)
            {
                // transfer remaining and break
                FromStock.TransferItemTo(ToStock, i, null, true, (VRage.MyFixedPoint)amount);
                left = 0;
                break;
            }
            else
            {
                left -= (float)items[i].Amount;
                // transfer all
                FromStock.TransferItemTo(ToStock, i, null, true, null);
            }
        }
    }
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

///////////////////////////////////////////
// ISI Stuff - to adapt
//
// 
// Build on the autocraft system
///////////////////////////////////////////

