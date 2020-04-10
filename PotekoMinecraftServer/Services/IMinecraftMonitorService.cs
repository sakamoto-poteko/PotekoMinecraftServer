using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PotekoMinecraftServer.Services
{
    public interface IMinecraftMonitorService
    {
        public List<string> ListServerNames();
        public MinecraftServerStatus GetMinecraftServerStatusCached(string name);
        public MinecraftMachineStatus GetMachineStatusCached(string name);
        public List<Models.MinecraftEndpointStatusViewModel> GetEndpointStatusCachedAll();
    }
}
