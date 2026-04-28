        public string ShipTag = "VSCShip"; // name of the ship (grid) -> Program
        public string PAMTag = "PAM"; // to be set to correct Name
        public string SpecialTag = "IIM"; // in accordance with Isy's inventory manager system 
        public string HomeTag = "HQ"; // to be set to what the HomeBase uses and its connector
        public string DepotTag = "Depot"; // to be set to what the depot uses and its connector
        public string SorterIn = "SorterIn"; // Name the sorter for loading
        public string SorterOut = "SorterOut"; // Name the sorter for delivery

        public bool DoScan()
        {        
            if (String.IsNullOrWhiteSpace(Me.CustomData)) { SetDefCustomData(); }
            else {ScanCustomData(); }
            ...
        }

        public void ScanCustomData()
        {
            ShipTag = GetCustomDataTag(Me, "ShipTag",  "VSCShip" );
            if(ShipTag != Me.CubeGrid.CustomName) { Message += "ShipTag does not match grid name\n"; SetDefCustomData(); return; }
            PAMTag = GetCustomDataTag(Me, "PAMTag", "PAM");
            SpecialTag = GetCustomDataTag(Me, "SpecialTag","IIM"); 
            HomeTag = GetCustomDataTag(Me, "HomeTag", "HQ");
            DepotTag = GetCustomDataTag(Me, "DepotTag", "Depot");
            SorterIn = GetCustomDataTag(Me, "SorterIn", "SorterIn");
            SorterOut = GetCustomDataTag(Me, "SorterOut", "SorterOut");
            // parameters
            string IntIn = GetCustomDataTag(Me, "MaxTime", "1");
            Int32.TryParse(IntIn, out MaxTime);
            IntIn = GetCustomDataTag(Me, "WaitForCargo", "100");
            Int32.TryParse(IntIn, out WaitForCargo);
            IntIn = GetCustomDataTag(Me, "ConsolePrn","-1");
            int prnConsole = -1;
            Int32.TryParse(IntIn, out prnConsole);
            Screen.ConsolePrn = prnConsole;
            // screen
            string _font = GetCustomDataTag(Me, "Font","Monospace");
            Screen.myFont = _font;
            float InFloat = 1.0f;
            if (!Single.TryParse(GetCustomDataTag(Me, "SizeFont","1.0"), out InFloat)) InFloat = 1.0f;
            Screen.myFontSize = InFloat;

            return;
        }
        public void SetDefCustomData()
        {
            string newData = "";
            Me.CustomData = "";
            newData += "//ShipTag=" + Me.CubeGrid.CustomName + "\n";
            newData += "//PAMTag=VSCShip\n";
            newData += "//SpecialTag= IIM\n";
            newData += "//HomeTag=HQ\n";
            newData += "//DepotTag=Depot\n";
            newData += "//SorterIn=SorterIn\n";
            newData += "//SorterOut=SorterOut\n";
            // parameters
            newData += "//MaxTime=1\n";
            newData += "//WaitForCargo=100\n";
            newData += "//ConsolePrn=0\n";
            // cockpit
            newData += "//Font = Monospace\n";
            newData += "//FontSize = 1.0\n";

            Me.CustomData = newData;
            return;
        }

        public string EchoChars = "//"; // space gives problems

        public string GetCustomDataTag(IMyTerminalBlock thisBlock, string _thisTag, string _Default)
        {
            string _CustomData = thisBlock.CustomData.Trim();
            string _newCD = "";
            string found = "";

            string[] _cdlines = _CustomData.Split('\n');
            
            for (int cdidx = 0; cdidx < _cdlines.Length; cdidx++)
            {
                // if it does not start with // it is not mine !
                if (!_cdlines[cdidx].StartsWith(EchoChars))
                {
                    _newCD += _cdlines[cdidx] + "\n";
                }
                else
                {
                    string _cdline = _cdlines[cdidx].Replace(EchoChars, "");
                    string[] _cdwords = _cdline.Split('=');
                    if (_cdwords[0].Trim().ToLower() == _thisTag.Trim().ToLower())
                    {
                        if ( _cdwords[1].Trim() == "") { found=_Default;}
                        else { found = _cdwords[1]; }
                        _newCD += EchoChars + _thisTag + "=" + found + "\n";
                    }
                    else
                    {
                        _newCD += EchoChars + _cdwords[0] + "=" + _cdwords[1] + "\n";
                    }
                }
            }
            // insert new entry
            if(found == "" ) { _newCD += EchoChars + _thisTag + "=" + _Default + "\n"; found=_Default;}
            Me.CustomData = _newCD;

            return found;
        }
