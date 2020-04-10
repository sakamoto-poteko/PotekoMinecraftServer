using System;

namespace PotekoMinecraftServer.Services
{
    public class MinecraftMachineStatus
    {
        public MachinePowerState PowerState { get; set; }
        public DateTime Timestamp { get; set; }
    }
}