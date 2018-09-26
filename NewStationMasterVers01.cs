public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update100;

    GridTerminalSystem.GetBlocks(allBlocks);
    GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(All_PBs);
}

public Save() {

}

const string version = "0.01";


string Header = " " + Me.CustomName + " " + version + "\n";

// some general list
List<string> Warnings = new List<string>();
List<string> Errors = new List<string>();
List<string> Messages  = new List<string>();

// usefull SE lists
List<IMyTerminalBlock> allBlocks = new List<IMyTerminalBlock>();
List<IMyProgrammableBlock> All_PBs = New List<IMyProgrammableBlock>();

public void Main(string argument, UpdateType updateSource) {




}

// Display
//LCD
