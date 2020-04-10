using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using PotekoMinecraftServer.Models;
using PotekoMinecraftServer.Services;

namespace PotekoMinecraftServer.Hubs
{
    public class MinecraftEndpointStatusHub : Hub<IMinecraftEndpointStatusClient>
    {
        private readonly IMinecraftMonitorService _monitorService;

        public MinecraftEndpointStatusHub(Services.IMinecraftMonitorService monitorService)
        {
            _monitorService = monitorService;
        }

        public Task RequestUpdate()
        {
            var status = _monitorService.GetEndpointStatusCachedAll();
            return Clients.Caller.StatusUpdated(status);
        }

        public override async Task OnConnectedAsync()
        {
            await RequestUpdate();

            await base.OnConnectedAsync();
        }
    }
}
