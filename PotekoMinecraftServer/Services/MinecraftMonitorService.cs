using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PotekoMinecraftServer.Hubs;
using PotekoMinecraftServer.Models;

namespace PotekoMinecraftServer.Services
{
    public class MinecraftMonitorService : BackgroundService, IMinecraftMonitorService
    {
        private readonly ILogger<MinecraftMonitorService> _logger;
        private readonly IMinecraftMachineService _minecraftMachineService;
        private readonly IMinecraftServerDaemonService _minecraftServerDaemonService;
        private readonly IHubContext<MinecraftEndpointStatusHub, IMinecraftEndpointStatusClient> _mcstatusHub;

        private readonly Dictionary<string, MinecraftServerStatus> _serverStatus =
            new Dictionary<string, MinecraftServerStatus>();

        private readonly Dictionary<string, MinecraftMachineStatus> _machineStatus =
            new Dictionary<string, MinecraftMachineStatus>();

        private readonly Dictionary<string, DateTime>
            _serverHasPlayerLastTimestamp = new Dictionary<string, DateTime>();

        private readonly Dictionary<string, DateTime>
            _serverRunningLastTimestamp = new Dictionary<string, DateTime>();

        private readonly Dictionary<string, DateTime>
            _machineRunningLastTimestamp = new Dictionary<string, DateTime>();

        private readonly List<string> _machines;
        private readonly int _refreshInterval;
        private readonly int _idleServerShutdownInterval;
        private readonly int _serverPowerOffInterval;
        private readonly int _serverDeallocateInterval;

        public MinecraftMonitorService(IOptions<Settings.MinecraftServer> options, ILogger<MinecraftMonitorService> logger,
            IMinecraftMachineService minecraftMachineService, IMinecraftServerDaemonService minecraftServerDaemonService,
            IHubContext<MinecraftEndpointStatusHub, IMinecraftEndpointStatusClient> mcstatusHub)
        {
            _logger = logger;
            _minecraftMachineService = minecraftMachineService;
            _minecraftServerDaemonService = minecraftServerDaemonService;
            _mcstatusHub = mcstatusHub;

            var settings = options.Value;
            _refreshInterval = settings.RefreshInterval;
            _idleServerShutdownInterval = settings.IdleServerShutdownInterval;
            _serverPowerOffInterval = settings.ServerPowerOffInterval;
            _serverDeallocateInterval = settings.ServerDeallocateInterval;

            _machines = settings.Endpoints.Select(e => e.Name).ToList();
            foreach (var name in _machines)
            {
                _serverStatus.Add(name, new MinecraftServerStatus());
                _machineStatus.Add(name, new MinecraftMachineStatus());
                _serverHasPlayerLastTimestamp.Add(name, DateTime.MaxValue);
                _serverRunningLastTimestamp.Add(name, DateTime.MaxValue);
                _machineRunningLastTimestamp.Add(name, DateTime.MaxValue);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var name in _machines)
                {
                    if (stoppingToken.IsCancellationRequested)
                        return;

                    await QueryServerAsync(name);

                    await CheckShutdownNeededAsync(name);
                }

                await BroadcastStatusAsync();

                await Task.Delay(TimeSpan.FromSeconds(_refreshInterval), stoppingToken);
            }
        }

        private Task BroadcastStatusAsync()
        {
            var list = GetEndpointStatusCachedAll();
            return _mcstatusHub.Clients.All.StatusUpdated(list);
        }

        public List<MinecraftEndpointStatusViewModel> GetEndpointStatusCachedAll()
        {
            var list = _machines.Select(name => new Models.MinecraftEndpointStatusViewModel
            {
                Name = name,
                MachineStatus = _machineStatus[name],
                ServerStatus = _serverStatus[name]
            }).ToList();
            return list;
        }

        private async Task CheckShutdownNeededAsync(string name)
        {
            if (DateTime.UtcNow - _serverHasPlayerLastTimestamp[name] >
                TimeSpan.FromMinutes(_idleServerShutdownInterval))
            {
                var status = _serverStatus[name].MinecraftBdsStatus;
                if (status == MinecraftBdsStatus.Running)
                {
                    await _minecraftServerDaemonService.StopServerAsync(name);
                }
                else
                {
                    await CheckMachineStopNeededAsync(name);
                }
            }
        }

        private async Task CheckMachineStopNeededAsync(string name)
        {
            var mStatus = _machineStatus[name].PowerState;
            if (DateTime.UtcNow - _serverRunningLastTimestamp[name] >
                        TimeSpan.FromMinutes(_serverPowerOffInterval))
            {
                if (mStatus == MachinePowerState.Running)
                {
                    await _minecraftMachineService.StopAsync(name);
                }
                else
                {
                    await CheckMachineDeallocateNeededAsync(name);
                }
            }
        }

        private async Task CheckMachineDeallocateNeededAsync(string name)
        {
            var mStatus = _machineStatus[name].PowerState;
            if (DateTime.UtcNow - _machineRunningLastTimestamp[name] >
                        TimeSpan.FromMinutes(_serverDeallocateInterval))
            {
                if (mStatus == MachinePowerState.Stopped)
                {
                    await _minecraftMachineService.DeallocateAsync(name);
                }
            }
        }

        private async Task QueryServerAsync(string name)
        {
            var m = new MinecraftMachineStatus
            {
                Timestamp = DateTime.UtcNow
            };

            var s = new MinecraftServerStatus
            {
                Timestamp = DateTime.UtcNow
            };

            var mStatus = await _minecraftMachineService.GetStatusAsync(name);
            m.PowerState = mStatus;
            if (mStatus == MachinePowerState.Running)
            {
                var sStatus = await _minecraftServerDaemonService.GetServerStatusAsync(name);
                s.MinecraftBdsStatus = sStatus;

                if (sStatus == MinecraftBdsStatus.Running)
                {
                    var sPlayers = await _minecraftServerDaemonService.ListPlayersAsync(name);
                    s.Max = sPlayers.Max;
                    s.Online = sPlayers.Online;
                    s.Players = sPlayers.Players;

                    if (sPlayers.Online > 0)
                    {
                        _serverHasPlayerLastTimestamp[name] = DateTime.UtcNow;
                    }
                }
            }
            else
            {
                s.MinecraftBdsStatus = MinecraftBdsStatus.Stopped;
            }

            _machineStatus[name] = m;
            _serverStatus[name] = s;
        }

        public List<string> ListServerNames()
        {
            return _machines.ToList();
        }

        public MinecraftServerStatus GetMinecraftServerStatusCached(string name)
        {
            return _serverStatus.TryGetValue(name, out var status) ? status : null;
        }

        public MinecraftMachineStatus GetMachineStatusCached(string name)
        {
            return _machineStatus.TryGetValue(name, out var status) ? status : null;
        }
    }
}
