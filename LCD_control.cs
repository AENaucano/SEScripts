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
