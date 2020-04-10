using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PotekoMinecraftServer.Models;

namespace PotekoMinecraftServer.Hubs
{
    public interface IMinecraftEndpointStatusClient
    {
        Task StatusUpdated(List<MinecraftEndpointStatusViewModel> status);
    }
}
