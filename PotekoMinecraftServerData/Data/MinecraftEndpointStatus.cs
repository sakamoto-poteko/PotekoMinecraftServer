using System;
using System.Collections.Generic;
using System.Text;

namespace PotekoMinecraftServerData.Data
{
    public class MinecraftEndpointStatus
    {
        public string Name { get; set; }

        public MinecraftMachineStatus MachineStatus { get; set; }
        public MinecraftServerStatus ServerStatus { get; set; }
    }
}
