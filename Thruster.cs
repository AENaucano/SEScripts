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
Public float EffThrust=0;
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

