        public class Logging
        {
            
            private string Text = "";
            private string Key = "Undef";
            private string Type = "Debug";
            
            private Dictionary<string, string> Emojis = new Dictionary<string, string>()
            {
                {"Info", ":->"},
                {"Good", ":-)" },
                {"Found", ":-]"},
                {"NotFound", ":-["},
                {"Error", "|-X"},
                {"Warning", ":-O"},
                {"Debug", ":-D"},
                {"WTF", ":-/"},
                {"Bad", ":-("},
                {"Undef", "°-o"},
                {"LOL", "^_^" }
            };

            private const int MAXLINES = 100;
            private List<Logging> generalLog = new List<Logging>();
            private string MyFooter = BasicSystem.GetTimeString();

            public Logging()
            {
                Text = "If you see this, something is Wrong";
                Key = "New";
                Type = "Error";
            }

            public Logging(string _key, string _txt, string _type = "Debug")
            {
                Text = _txt;
                Key = _key;
                Type = _type;
            }

            public void clearLog()
            {
                if (generalLog != null) generalLog.Clear();
            }

            public void addLog(string ThisText, string _Type = "Undef")
            {
                int OldInt = -1;
                if (String.IsNullOrEmpty(ThisText)) return;

                string newKey = "";
                string newText = "";
                string[] _splitText = ThisText.Split(':');
                if (String.IsNullOrEmpty(_splitText[1]))
                {
                    newKey = BasicSystem.GetTimeString();
                    newText = ThisText;
                }
                else
                {
                    newKey = _splitText[0];
                    newText = _splitText[1];
                }

                OldInt = FindLog(_splitText[0]); // -1 nothing found
                Logging _newLog = new Logging(newKey, newText, _Type);

                if (OldInt == -1) generalLog.Add(_newLog);
                else generalLog[OldInt] = _newLog;

                if (generalLog.Count() > MAXLINES) generalLog.RemoveAt(0);

                return;
            }

            private int FindLog(string _key)
            {
                for (int fidx = 0; fidx < generalLog.Count(); fidx++)
                {
                    if (generalLog[fidx].Key == _key) return fidx;
                }
                return -1;
            }

            /// <summary>
            /// Returns a string with a maxNumberOfLines to put on a LCD
            /// </summary>
            /// <returns></returns>
            public string printLog(bool _debugonly = false)
            {
                string newPrintlog = "";
                if (!_debugonly) newPrintlog = " === " + _prog.ScriptTag + " " + VERSION + " =Log=\n";
                else newPrintlog = " =D= Debug === " + VERSION + " =D=\n";

                if (generalLog != null || generalLog.Count != 0)
                {
                    // int startLine = generalLog.Count - maxNumberOfLines - 1;
                    // if (startLine < 0) startLine = 0;
                    //for (int i = startLine; i < generalLog.Count; i++)
                    for (int i = 0; i < generalLog.Count; i++)
                    {
                        Logging _printLog = generalLog[i];
                        if (_printLog.Type == "Debug" & !_debugonly) continue;
                        if (_printLog.Type != "Debug" & _debugonly) continue;
                        newPrintlog += " " + Emojis[_printLog.Type] + " " + _printLog.Text + "\n";
                    }
                    Logging _printLog = generalLog[generalLog.Count];
                    setfooter(Emojis[_printLog.Type] + " " + _printLog.Text + "\n");
                }
                else
                {
                    newPrintlog += "... Empty";
                }

                return newPrintlog;
            }
            public List<string> ListDebug()
            {
                List<string> _Debug = new List<string>();

                if (generalLog != null || generalLog.Count() != 0)
                {
                    for (int fi = 0; fi < generalLog.Count(); fi++)
                    {
                        string _text = "";
                        Logging _printLog = generalLog[fi];
                        if (_printLog.Type != "Debug") continue;
                        _text = " " + Emojis[_printLog.Type] + " " + _printLog.Text;
                        _Debug.Add(_text);
                    }

                }
                return _Debug;
            }
            public string getfooter()
            {
                return MyFooter;
            }
            public string setfooter(string Newfooter)
            {
                if (Newfooter == "")
                {
                    MyFooter = BasicSystem.GetTimeString();
                }
                else
                {
                    MyFooter = Newfooter;
                }
                return MyFooter;
            }

        }
