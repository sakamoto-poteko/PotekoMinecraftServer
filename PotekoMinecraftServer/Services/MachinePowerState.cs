namespace PotekoMinecraftServer.Services
{
    public enum MachinePowerState
    {
        Starting = 0,
        Running = 1,
        Stopping = 2,
        Stopped = 3,
        Deallocating = 4,
        Deallocated = 5,
        NetworkError = 0xFD,
        LocalError = 0xFE,
        Error = 0xFF,
    }

    public static class MachinePowerStateExtensions
    {
        public static bool IsError(this MachinePowerState state)
        {
            return state switch
            {
                MachinePowerState.NetworkError => true,
                MachinePowerState.LocalError => true,
                MachinePowerState.Error => true,
                _ => false,
            };
        }
    }
}