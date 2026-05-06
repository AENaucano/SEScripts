IMyThrust thruster = GridTerminalSystem.GetBlockWithName("My Thruster") as IMyThrust;
Vector3I direction = thruster.GridThrustDirection;

if (direction == Vector3I.Forward) {
    // This thruster pushes the ship forward relative to the cockpit
}
if (direction == Vector3I.Up) {
    // This thruster pushes the ship Up relative to the cockpit
}

IMyThrust
public float CurrentThrust { get; }
public float MaxEffectiveThrust { get; }

MaxEffectiveThrust: This is the maximum possible force (in Newtons) the thruster can generate in its current environment. 
Unlike MaxThrust (the raw design limit), this value accounts for environmental penalties, such as an Ion Thruster's reduced efficiency in an atmosphere or an Atmospheric Thruster's loss of power at high altitudes.

CurrentThrust: This is the actual force (in Newtons) the thruster is producing right now.
It changes dynamically based on player input (WASD), inertial dampener corrections, or a specific ThrustOverride setting.

F>m*g
Where:
F: Total MaxEffectiveThrust of all downward-facing thrusters (in Newtons).
m: Total grid mass (in kilograms), found in the Info tab of your terminal.
g: Local gravity (in m/s^2).

On an Earth-like planet, this is approximately 9.81 m/s^2\ (often rounded to \(10\) for a safety buffer)

IMyCockpit

GetTotalGravity
CalculateShipMass

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
List<IMyThrust> UpThrusters = new list<IMyThrust>();

list<IMyThrust> Thrusters =  new list<IMyThrust>();
Grid.GetBlocksOfType(Thrusters, ThatsMe_Grid);
if (!Thrusters.Any()) { Message += "Thrusters missing\n"; return false; }
for (int tidx = 0; tidx < Thrusters.Count(); tidx++)
{
    Vector3I direction = Thruster[tidx].GridThrustDirection;
    if (direction == Vector3I.Up) UpThrusters.Add(Thruster[tidx]);
}

public float AllThrust=0;
public float EffThrust=0;
public float Checkthrust()
{
    AllThrust=0;
    EffThrust=0;
    for (int tidx = 0; tidx < UpThrusters.Count(); tidx++)
    {
        AllThrust += UpThrusters.CurrentThrust;
        EffThrust += UpThrusters.MaxEffectiveThrust;
    }
    return AllThrust;
}
/////////////////////////////////////////////////////////// AI
public void Main() {
    var lcd = GridTerminalSystem.GetBlockWithName("FlightLCD") as IMyTextSurface;
    double mass = Me.CubeGrid.CalculateMass().PhysicalMass;
    double gravity = RemoteControl.GetNaturalGravity().Length(); // Requires a Remote Control block named 'RemoteControl'
    double requiredThrust = mass * gravity;

    lcd.WriteText($"Total Mass: {mass:N0} kg\n");
    lcd.WriteText($"Req. Lift: {requiredThrust:N0} N", true);
}

/////////////////////////////////////////////////////////////////
// Setup: Name your LCD or Cockpit "[FlightLCD]" 
// This script assumes you have a Cockpit or Remote Control on your ship.

public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update10; // Runs 6 times per second
}

public void Main() {
    var lcd = GridTerminalSystem.GetBlockWithName("[FlightLCD]") as IMyTextSurfaceProvider;
    IMyShipController controller = null;
    
    // 1. Find a reference cockpit or remote control
    List<IMyShipController> controllers = new List<IMyShipController>();
    GridTerminalSystem.GetBlocksOfType(controllers);
    if (controllers.Count > 0) controller = controllers[0];

    if (controller == null || lcd == null) {
        Echo("Error: Missing [FlightLCD] or Cockpit.");
        return;
    }

    // 2. Get Ship Data
    double mass = controller.CalculateShipMass().PhysicalMass;
    double gravity = controller.GetNaturalGravity().Length();
    double requiredThrust = mass * gravity; // Force in Newtons to hover

    // 3. Detect Upward Thrust (Automatic Direction Detection)
    List<IMyThrust> allThrusters = new List<IMyThrust>();
    GridTerminalSystem.GetBlocksOfType(allThrusters);
    
    double totalUpwardThrust = 0;
    Matrix cockpitMatrix;
    controller.Orientation.GetMatrix(out cockpitMatrix);
    Vector3D downDirection = cockpitMatrix.Down; // Grid-relative "Down"

    foreach (var t in allThrusters) {
        if (!t.IsWorking) continue;

        // If the thruster's exhaust points 'Down', it pushes the ship 'Up'
        if (t.Orientation.Forward == controller.Orientation.Down) {
            totalUpwardThrust += t.MaxEffectiveThrust;
        }
    }

    // 4. Calculate Ratios
    double twr = totalUpwardThrust / (requiredThrust > 0 ? requiredThrust : 1);
    double cargoRoom = (totalUpwardThrust / gravity) - mass;

    // 5. Output to LCD
    var surface = (lcd as IMyTextSurface) ?? lcd.GetSurface(0);
    surface.ContentType = ContentType.TEXT_AND_IMAGE;
    surface.WriteText(
        $"--- FLIGHT STATUS ---\n" +
        $"Mass: {mass/1000:F1} tonnes\n" +
        $"Local Gravity: {gravity/9.81:F2} g\n\n" +
        $"Upward Thrust: {totalUpwardThrust/1000:F0} kN\n" +
        $"Req. to Hover: {requiredThrust/1000:F0} kN\n\n" +
        $"TWR: {twr:F2} {(twr < 1.1 ? "!! DANGER !!" : "(Safe)")}\n" +
        $"Cargo Capacity: {cargoRoom/1000:F1} tonnes left"
    );
}

