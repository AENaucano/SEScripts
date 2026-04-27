        public string ScriptTag = "VSC"; // name of this script
        public string ShipTag = "VSCShip"; // name of the ship (grid) -> Program
        public string PAMTag = "PAM"; // to be set to correct Name
        public string SpecialTag = "IIM"; // in accordance with Isy's inventory manager system 
        public string HomeTag = "HQ"; // to be set to what the HomeBase uses and its connector
        public string DepotTag = "Depot"; // to be set to what the depot uses and its connector
        public string SorterIn = "SorterIn"; // Name the sorter for loading
        public string SorterOut = "SorterOut"; // Name the sorter for delivery

        public void ScanCustomData()
        {
            if (String.IsNullOrWhiteSpace(Me.CustomData)) { SetDefCustomData(); return; }
            ShipTag = BaSe.GetCustomDataTag(Me, "ShipTag");
            if(ShipTag != Me.CubeGrid.CustomName) { Message += "ShipTag does not match grid name\n"; SetDefCustomData(); return; }
            PAMTag = BaSe.GetCustomDataTag(Me, "PAMTag");
            SpecialTag = BaSe.GetCustomDataTag(Me, "SpecialTag"); 
            HomeTag = BaSe.GetCustomDataTag(Me, "HomeTag");
            DepotTag = BaSe.GetCustomDataTag(Me, "DepotTag");
            SorterIn = BaSe.GetCustomDataTag(Me, "SorterIn");
            SorterOut = BaSe.GetCustomDataTag(Me, "SorterOut");
            // parameters
            string IntIn = BaSe.GetCustomDataTag(Me, "MaxTime");
            Int32.TryParse(IntIn, out MaxTime);
            IntIn = BaSe.GetCustomDataTag(Me, "WaitForCargo");
            Int32.TryParse(IntIn, out WaitForCargo);
            IntIn = BaSe.GetCustomDataTag(Me, "ConsolePrn");
            int prnConsole = -1;
            Int32.TryParse(IntIn, out prnConsole);
            Screen.ConsolePrn = prnConsole;
            // IMyCockpit
            string CockpitName = BaSe.GetCustomDataTag(Me, "Console");
            Screen.Console = Grid.GetBlockWithName(CockpitName) as IMyCockpit;
            // screen
            string _font = BaSe.GetCustomDataTag(Me, "Font");
            Screen.myFont = _font;
            float InFloat = 1.0f;
            if (!Single.TryParse(BaSe.GetCustomDataTag(Me, "SizeFont"), out InFloat)) InFloat = 1.0f;
            Screen.myFontSize = InFloat;

            return;
        }
        public void SetDefCustomData()
        {
            string newData = "";
            Me.CustomData = "";
            newData += "//ShipTag=" + Me.CubeGrid.CustomName + "\n";
            newData += "//PAMTag=" + PAMTag + "\n";
            newData += "//SpecialTag=" + SpecialTag + "\n";
            newData += "//HomeTag=" + HomeTag + "\n";
            newData += "//DepotTag=" + DepotTag + "\n";
            newData += "//SorterIn=" + SorterIn + "\n";
            newData += "//SorterOut=" + SorterOut + "\n";
            // parameters
            newData += "//MaxTime=" + MaxTime + "\n";
            newData += "//WaitForCargo=" + WaitForCargo.ToString() + "\n";
            newData += "//ConsolePrn=" + Screen.ConsolePrn.ToString() + "\n";
            // cockpit
            if (Screen.Console != null) newData += "//Console=" + Screen.Console.CustomName + "\n";
            else newData += "//Console=" + ShipTag + " Main Cockpit\n";
            newData += "//Font = Monospace\n";
            newData += "//FontSize = 1.0\n";

            Me.CustomData = newData;
            return;
        }


public string GetCustomDataTag(IMyTerminalBlock thisBlock, string _thisTag, string _setThis = "")
          {
                if (String.IsNullOrWhiteSpace(thisBlock.CustomData)) return "";
                string _CustomData = thisBlock.CustomData.Trim();
                string _newCD = "";
                bool found = false;

                string[] _cdlines = _CustomData.Split('\n');
                // for each line
                for (int cdidx = 0; cdidx < _cdlines.Length; cdidx++)
                {
                    // if it does not start with // it is not mine !
                    if (_cdlines[cdidx].StartsWith(EchoChars))
                    {
                        string _cdline = _cdlines[cdidx].Replace(EchoChars, "");
                        string[] _cdwords = _cdline.Split('=');
                        if (_cdwords[0].Trim().ToLower() == _thisTag.Trim().ToLower())
                        {
                            found = true;
                            // replace
                            if (_setThis != "") { _newCD += EchoChars + _thisTag + "=" + _setThis + "\n"; }
                            else { return _cdwords[1]; } // just return current data
                        }
                    }
                    else
                    {
                        _newCD += _cdlines[cdidx] + "\n";
                    }
                }
