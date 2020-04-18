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
            _serverInactiveFirstTimestamp = new Dictionary<string, DateTime>();

        private readonly Dictionary<string, DateTime>
            _serverStoppedFirstTimestamp = new Dictionary<string, DateTime>();

        private readonly Dictionary<string, DateTime>
            _machineInactiveFirstTimestamp = new Dictionary<string, DateTime>();

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
                _serverInactiveFirstTimestamp.Add(name, DateTime.UtcNow);
                _serverStoppedFirstTimestamp.Add(name, DateTime.UtcNow);
                _machineInactiveFirstTimestamp.Add(name, DateTime.UtcNow);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("monitor started");
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var tasks = _machines.Select(async name =>
                    {
                        if (stoppingToken.IsCancellationRequested)
                            return;

                        await QueryServerAsync(name);

                        await CheckShutdownNeededAsync(name);
                    }).ToArray();

                    await Task.WhenAll(tasks);

                    await BroadcastStatusAsync();

                    await Task.Delay(TimeSpan.FromSeconds(_refreshInterval), stoppingToken);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("monitor encountered exception: {message}", e.Message);
                throw;
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
            var status = _serverStatus[name].MinecraftBdsStatus;
            if (DateTime.UtcNow - _serverInactiveFirstTimestamp[name] >
                    TimeSpan.FromMinutes(_idleServerShutdownInterval) &&
                    !status.IsError())
            {
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
            if (DateTime.UtcNow - _serverStoppedFirstTimestamp[name] >
                        TimeSpan.FromMinutes(_serverPowerOffInterval)
                        && !mStatus.IsError())
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
            if (DateTime.UtcNow - _machineInactiveFirstTimestamp[name] >
                        TimeSpan.FromMinutes(_serverDeallocateInterval)
                        && !mStatus.IsError())
            {
                if (mStatus == MachinePowerState.Stopped)
                {
                    await _minecraftMachineService.DeallocateAsync(name);
                }
            }
        }

        private void SetAsNever(IDictionary<string, DateTime> dict, string name)
        {
            dict[name] = DateTime.MaxValue;
        }

        private void SetAsNow(IDictionary<string, DateTime> dict, string name)
        {
            dict[name] = DateTime.UtcNow;
        }

        private bool IsNever(IDictionary<string, DateTime> dict, string name)
        {
            return dict[name] == DateTime.MaxValue;
        }

        private void SetAsNowIfIsNever(IDictionary<string, DateTime> dict, string name)
        {
            if (IsNever(dict, name))
            {
                SetAsNow(dict, name);
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
                // if just turned on, set server stopped to now
                if (!IsNever(_machineInactiveFirstTimestamp, name))
                {
                    SetAsNow(_serverStoppedFirstTimestamp, name);
                }

                SetAsNever(_machineInactiveFirstTimestamp, name);
                var sStatus = await _minecraftServerDaemonService.GetServerStatusAsync(name);
                s.MinecraftBdsStatus = sStatus;

                if (sStatus == MinecraftBdsStatus.Running)
                {
                    if (!IsNever(_serverStoppedFirstTimestamp, name))
                    {
                        SetAsNow(_serverInactiveFirstTimestamp, name);
                    }

                    SetAsNever(_serverStoppedFirstTimestamp, name);

                    var sPlayers = await _minecraftServerDaemonService.ListPlayersAsync(name);
                    s.Max = sPlayers.Max;
                    s.Online = sPlayers.Online;
                    s.Players = sPlayers.Players;

                    if (sPlayers.Online > 0)
                    {
                        SetAsNever(_serverInactiveFirstTimestamp, name);
                    }
                    else
                    {
                        SetAsNowIfIsNever(_serverInactiveFirstTimestamp, name);
                    }
                }
                else
                {
                    SetAsNowIfIsNever(_serverStoppedFirstTimestamp, name);
                }
            }
            else
            {
                SetAsNowIfIsNever(_machineInactiveFirstTimestamp, name);
            }

            _machineStatus[name] = m;
            _serverStatus[name] = s;
            _logger.LogDebug("Status of {name} updated", name);
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
