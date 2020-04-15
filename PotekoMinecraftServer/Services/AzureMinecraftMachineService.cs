using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PotekoMinecraftServer.Services
{
    public class AzureMinecraftMachineService : IMinecraftMachineService
    {
        private readonly ILogger<AzureMinecraftMachineService> _logger;
        private readonly IAzure _azure;
        private readonly Dictionary<string, (string resourceGroup, string machineName)> _machines = new Dictionary<string, (string resourceGroup, string machineName)>();

        public AzureMinecraftMachineService(IOptions<Settings.MinecraftServer> options, ILogger<AzureMinecraftMachineService> logger)
        {
            _logger = logger;

            var settings = options.Value;

            foreach (var serverEntry in settings.Endpoints)
            {
                _machines.Add(serverEntry.Name, (serverEntry.ResourceGroup, serverEntry.MachineName));
            }

            AzureCredentials cred;
            if (settings.AzureUseMsi && settings.AzureMsiType != Settings.AzureMsiType.Unknown)
            {
                var msiResourceType = settings.AzureMsiType switch
                {
                    Settings.AzureMsiType.AppService => MSIResourceType.AppService,
                    Settings.AzureMsiType.VirtualMachine => MSIResourceType.VirtualMachine,
                    _ => throw new ArgumentOutOfRangeException()
                };
                cred = new AzureCredentialsFactory().FromMSI(new MSILoginInformation(msiResourceType), AzureEnvironment.AzureGlobalCloud);
            }
            else
            {
                cred = new AzureCredentialsFactory().FromServicePrincipal(settings.AzureClientId,
                    settings.AzureKey, settings.AzureTenantId, AzureEnvironment.AzureGlobalCloud);
            }
            _azure = Azure.Configure().Authenticate(cred).WithSubscription(settings.AzureSubscription);
        }

        private (string resourceGroup, string machineName) GetMachine(string name)
        {
            if (!_machines.TryGetValue(name, out var pair))
            {
                throw new ArgumentOutOfRangeException(name);
            }
            return pair;
        }

        public async Task StartAsync(string name)
        {
            var m = GetMachine(name);
            var vm = await _azure.VirtualMachines.GetByResourceGroupAsync(m.resourceGroup, m.machineName);
            await vm.StartAsync();
        }

        public async Task StopAsync(string name)
        {
            var m = GetMachine(name);
            var vm = await _azure.VirtualMachines.GetByResourceGroupAsync(m.resourceGroup, m.machineName);
            await vm.PowerOffAsync();
        }

        public async Task DeallocateAsync(string name)
        {
            var m = GetMachine(name);
            var vm = await _azure.VirtualMachines.GetByResourceGroupAsync(m.resourceGroup, m.machineName);
            await vm.DeallocateAsync();
        }

        public async Task<MachinePowerState> GetStatusAsync(string name)
        {
            var m = GetMachine(name);
            var vm = await _azure.VirtualMachines.GetByResourceGroupAsync(m.resourceGroup, m.machineName);
            return vm.PowerState?.Value switch
            {
                "PowerState/running" => MachinePowerState.Running,
                "PowerState/deallocating" => MachinePowerState.Deallocating,
                "PowerState/deallocated" => MachinePowerState.Deallocated,
                "PowerState/starting" => MachinePowerState.Starting,
                "PowerState/stopped" => MachinePowerState.Stopped,
                "PowerState/stopping" => MachinePowerState.Stopping,
                "PowerState/unknown" => MachinePowerState.Error,
                null => MachinePowerState.LocalError,
                _ => throw new NotImplementedException($"PowerState value `{vm.PowerState.Value}' not implemented"),
            };
        }
    }
}
