using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PotekoMinecraftServer.Models
{
    public class MinecraftEndpointStatusViewModel
    {
        public string Name { get; set; }

        public Services.MinecraftMachineStatus MachineStatus { get; set; }
        public Services.MinecraftServerStatus ServerStatus { get; set; }
    }
}
