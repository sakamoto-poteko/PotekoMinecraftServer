using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PotekoMinecraftServerData.Data;

namespace PotekoMinecraftServer.Services
{
    public interface IMinecraftServerDaemonService
    {
        public Task<bool> StartServerAsync(string name);
        public Task<bool> StopServerAsync(string name);
        public Task<OnlinePlayers> ListPlayersAsync(string name);
        public Task<MinecraftBdsStatus> GetServerStatusAsync(string name);
    }


    public class OnlinePlayers
    {
        public int Online { get; set; }
        public int Max { get; set; }
        public List<string> Players { get; set; }
    }
}
