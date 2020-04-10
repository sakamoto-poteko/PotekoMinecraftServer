namespace PotekoMinecraftServer.Services
{
    public enum MinecraftBdsStatus
    {
        Stopped = 0,
        Running = 1,
        LocalError = 0xFE,
        Error = 0xFF,
    }
}