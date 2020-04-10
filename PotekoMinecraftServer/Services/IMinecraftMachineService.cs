using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PotekoMinecraftServer.Services
{
    public interface IMinecraftMachineService
    {
        public Task StartAsync(string name);
        public Task StopAsync(string name);
        public Task DeallocateAsync(string name);
        public Task<MachinePowerState> GetStatusAsync(string name);
    }
}
