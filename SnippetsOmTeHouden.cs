   // We need power -> Battery -> reactor needs Uranium Ingot - Solar panels don’t
    int PowerStatus = Checkbatteries();
    // Echo ("Powerstatus : " + PowerStatus + "\n");
    if (PowerStatus >= MinimumPower ) PowerOk = true;
    string ShowPowerLevel = PowerStatus.ToString();
    firstline = new String('I', PowerStatus/5);
    secondline = new String('.', 20-(PowerStatus/5));
    fillline = "[" + firstline + secondline + "] ";
    Message += " " + fillline + ShowPowerLevel + "%\n";

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
    // Message += "Used Energy: " + UsedToEnergy.ToString("0.000") + " / " + UsedFromEnergy.ToString("0.000") + " \n";


    // ------------------------------------------------------------------------------------------------------------------
    // between 2 Docks -> connected connectors
// ToMine -> DeltaEnergy
// FromMine - > DeltaEnergy
// Energy can only go down !

/**************
    How do we know we have enough power to fly home ?    
 **************/


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

// Accesing inventory of connectors
           if (RemorqueConnectors.Count > 0)
           {

                for (int i = 0; i < RemorqueConnectors.Count; i++)
                {
                    IMyShipConnector ThisContainer= GridTerminalSystem.GetBlockWithName(RemorqueConnectors[i]) as IMyShipConnector;
                    IMyInventory OreStock = ThisContainer.GetInventory(0);
                    OreCount = countItem(OreStock, CurrentOre); 

                    if (OreCount > 0)
                    {
                        tekst += " --> transferring " + OreCount + " from " + RemorqueConnectors[i] + "\n";
                        tekst += " --- to " + OreContainers[0] + "\n";
 
                        TransferTo(OreStock, CurrentOre, OreCount);
                    }
                }
           }


// Connector status - new system
public int CheckConnector()
{
	int Status = -1;
    // Unconnected	0	This connector is not connected to anything, nor is it near anything connectable.
    // Connectable	1	This connector is currently near something that it can connect to.
    // Connected	2	This connector is currently connected to something.
    // IMyShipConnector DroneConnector = GridTerminalSystem.GetBlockWithName(DRONECONNECTOR) as IMyShipConnector;
   
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

// LD scrolling as to ISI
string CreateScrollingText(float fontSize, string text, IMyTextPanel lcd, int headingHeight = 3)
{
	long id = lcd.EntityId;

	if (!scroll.ContainsKey(id)) {
		scroll[id] = new List<int> { 1, 3, headingHeight, 0 };
	}

	int scrollDirection = scroll[id][0];
	int scrollWait = scroll[id][1];
	int lineStart = scroll[id][2];
	int scrollSecond = scroll[id][3];

	var linesTemp = text.TrimEnd('\n').Split('\n');
	List<string> lines = new List<string>();
	int lcdHeight = (int)Math.Floor(17 / fontSize);
	int lcdWidth = (int)(26 / fontSize);
	string lcdText = "";

	if (lcd.BlockDefinition.SubtypeName.Contains("Corner")) {
		if (lcd.CubeGrid.GridSize == 0.5) {
			lcdHeight = (int)Math.Floor(5 / fontSize);
		} else {
			lcdHeight = (int)Math.Floor(3 / fontSize);
		}
	}

	if (lcd.BlockDefinition.SubtypeName.Contains("Wide")) {
		lcdWidth = (int)(52 / fontSize);
	}

	foreach (var line in linesTemp) {
		if (line.Length <= lcdWidth) {
			lines.Add(line);
		} else {
			try {
				string currentLine = "";
				var words = line.Split(' ');
				string number = System.Text.RegularExpressions.Regex.Match(line, @".+\:\ ").Value;
				string tab = ' '.Repeat(number.Length);

				foreach (var word in words) {
					if ((currentLine + " " + word).Length > lcdWidth) {
						lines.Add(currentLine);
						currentLine = tab + word + " ";
					} else {
						currentLine += word + " ";
					}
				}

				lines.Add(currentLine);
			} catch {
				lines.Add(line);
			}
		}
	}

	if (lines.Count > lcdHeight) {
		if (DateTime.Now.Second != scrollSecond) {
			scrollSecond = DateTime.Now.Second;
			if (scrollWait > 0) scrollWait--;
			if (scrollWait <= 0) lineStart += scrollDirection;

			if (lineStart + lcdHeight - headingHeight >= lines.Count && scrollWait <= 0) {
				scrollDirection = -1;
				scrollWait = 3;
			}
			if (lineStart <= headingHeight && scrollWait <= 0) {
				scrollDirection = 1;
				scrollWait = 3;
			}
		}
	} else {
		lineStart = headingHeight;
		scrollDirection = 1;
		scrollWait = 3;
	}

	scroll[id][0] = scrollDirection;
	scroll[id][1] = scrollWait;
	scroll[id][2] = lineStart;
	scroll[id][3] = scrollSecond;

	for (var line = 0; line < headingHeight; line++) {
		lcdText += lines[line] + "\n";
	}

	for (var line = lineStart; line < lines.Count; line++) {
		lcdText += lines[line] + "\n";
	}

	return lcdText.TrimEnd('\n');
}

// Scan PBs

public void ScanProgramableBlocks() { 
    programableBlocks.Clear(); 
    List<IMyProgrammableBlock> blocks = new List<IMyProgrammableBlock>(); 
    GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(blocks, b => b.CubeGrid == Me.CubeGrid); 
    foreach (IMyProgrammableBlock block in blocks) { 
        var match = Program.tagRegex.Match(block.CustomName); 
        if (!match.Success) continue; 
        switch (match.Groups[2].Value.ToUpper()) { 
            case "NOTIFY": 
                Program.FixNameTag(block, match.Groups[1].Value, " NOTIFY"); 
                programableBlocks.Add(block); 
                break; 
            default: 
                Program.FixNameTag(block, match.Groups[1].Value, ""); 
                break; 
        } 
    } 
} 