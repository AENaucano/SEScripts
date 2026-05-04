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

IMyCockpit

GetTotalGravity
CalculateShipMass
