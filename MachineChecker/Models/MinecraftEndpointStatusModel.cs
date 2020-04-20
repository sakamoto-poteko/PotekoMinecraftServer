using System;
using System.Collections.Generic;
using System.Text;
using PotekoMinecraftServerData.Data;

namespace MachineChecker.Models
{
    public class MinecraftEndpointStatusModel : MinecraftEndpointStatus
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
    }
}
