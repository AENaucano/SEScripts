void ShowText(string LCDname, string Tekst)
{
    List<IMyTerminalBlock> MyLCDs = new List<IMyTerminalBlock>();
    GridTerminalSystem.SearchBlocksOfName(LCDname, MyLCDs);
    if ((MyLCDs == null) || (MyLCDs.Count == 0))
    {
		Echo( "|-0 No LCD-panel found with " + LCDname+ "\n\n" );
        Echo(	Tekst  );
    }
    else
    {
        for (int i = 0; i < MyLCDs.Count; i++)
        {
     		IMyTextPanel ThisLCD = GridTerminalSystem.GetBlockWithName(MyLCDs[i].CustomName) as IMyTextPanel;
			if ( ThisLCD == null)
			{
				Echo("°-X LCD not found? \n");
			}
			else
			{
                ThisLCDs.WritePublicText(Tekst, false);
                ThisLCDs.ShowPublicTextOnScreen();
            }
    	}
    }
}
///////////////////////////////////////////////////////////////////////////////////
// List to store all found surfaces
List<IMyTextSurface> allSurfaces = new List<IMyTextSurface>();

public void Main(string argument, UpdateType updateSource)
{
    // Clear the list for each run
    allSurfaces.Clear();
    
    // 1. Get every terminal block on the grid
    List<IMyTerminalBlock> allBlocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocks(allBlocks);

    foreach (var block in allBlocks)
    {
        // 2. Check if the block is a standalone LCD Panel
        if (block is IMyTextPanel)
        {
            allSurfaces.Add((IMyTextSurface)block);
        }
        // 3. Check if the block has internal surfaces (Cockpits, PB, etc.)
        else if (block is IMyTextSurfaceProvider)
        {
            var provider = (IMyTextSurfaceProvider)block;
            // Iterate through every surface index available on this block
            for (int i = 0; i < provider.SurfaceCount; i++)
            {
                allSurfaces.Add(provider.GetSurface(i));
            }
        }
    }
/////////////////////////////////////////////////////////////////
Standalone LCDs: Note that standard IMyTextPanel (LCD Panel) blocks often require a direct cast to IMyTextSurface to be treated as a screen in newer API versions.
1. Simple Direct Cast
IMyTextPanel panel = GridTerminalSystem.GetBlockWithName("My LCD") as IMyTextPanel;
if (panel != null)
{
    IMyTextSurface surface = (IMyTextSurface)panel;
    surface.WriteText("Direct Cast Successful!");
}

2. The as Operator (Safer)
IMyTextSurface surface = panel as IMyTextSurface;
if (surface != null)
{
    surface.WriteText("Safe Cast Successful!");
}
///////////////////////////////////////////////////////////////////////
