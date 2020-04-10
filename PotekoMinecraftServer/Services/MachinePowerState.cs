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
        Error = 0xFF,
    }
}