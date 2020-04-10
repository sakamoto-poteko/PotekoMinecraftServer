using System;
using System.Collections.Generic;

namespace PotekoMinecraftServer.Services
{
    public class MinecraftServerStatus
    {
        public MinecraftBdsStatus MinecraftBdsStatus { get; set; }
        public int Online { get; set; }
        public int Max { get; set; }
        public List<string> Players { get; set; }
        public DateTime Timestamp { get; set; }
    }
}