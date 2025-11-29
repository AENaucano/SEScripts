        IMySlimBlock GetSlimBlockFromFat(IMyTerminalBlock block)
        {
            return block.CubeGrid.GetCubeBlock(block.Position);
        }

Code:
 IMySlimBlock GetSlimBlockFromFat(IMyTerminalBlock block) { return block.CubeGrid.GetCubeBlock(block.Position); } 
