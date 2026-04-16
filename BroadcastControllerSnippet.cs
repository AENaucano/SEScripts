// Example snippet
var bc = GridTerminalSystem.GetBlockWithName("Broadcast Controller") as IMyBroadcastController;
if (bc != null)
{
    // Change the message
    bc.Message1 = "Automated Message: " + DateTime.Now.ToString();
    // Trigger the broadcast
    bc.TransmitMessage1();
}
