namespace PotekoMinecraftServer.Services
{
    public enum MinecraftBdsStatus
    {
        Stopped = 0,
        Running = 1,
        NetworkError = 0xFD,
        LocalError = 0xFE,
        Error = 0xFF,
    }

    public static class MinecraftBdsStatusExtensions
    {
        public static bool IsError(this MinecraftBdsStatus status)
        {
            return status switch
            {
                MinecraftBdsStatus.LocalError => true,
                MinecraftBdsStatus.NetworkError => true,
                _ => false,
            };
        }
    }
}