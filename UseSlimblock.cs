        IMySlimBlock GetSlimBlockFromFat(IMyTerminalBlock block)
        {
            return block.CubeGrid.GetCubeBlock(block.Position);
        }
