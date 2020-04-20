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
using Newtonsoft.Json;
using PotekoMinecraftServer.Settings;
using PotekoMinecraftServerData.Data;

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
            //_logger.LogInformation("Debug: settings {json}", JsonConvert.SerializeObject(settings));
            if (settings.AzureUseMsi && settings.AzureMsiType != Settings.AzureMsiType.Unknown)
            {
                _logger.LogInformation("Azure login with MSI");
                var msiResourceType = settings.AzureMsiType switch
                {
                    Settings.AzureMsiType.AppService => MSIResourceType.AppService,
                    Settings.AzureMsiType.VirtualMachine => MSIResourceType.VirtualMachine,
                    AzureMsiType.Unknown => throw new ArgumentOutOfRangeException(),
                    _ => throw new ArgumentOutOfRangeException()
                };
                cred = new AzureCredentialsFactory().FromMSI(new MSILoginInformation(msiResourceType), AzureEnvironment.AzureGlobalCloud);
            }
            else
            {
                _logger.LogInformation("Azure login with service principal");
                if (settings.AzureClientId == null)
                {
                    throw new ArgumentNullException(nameof(settings.AzureClientId));
                }

                if (settings.AzureKey == null)
                {
                    throw new ArgumentNullException(nameof(settings.AzureKey));
                }

                if (settings.AzureTenantId == null)
                {
                    throw new ArgumentNullException(nameof(settings.AzureTenantId));
                }

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
            _logger.LogDebug("Starting machine {name}", name);
            var m = GetMachine(name);
            var vm = await _azure.VirtualMachines.GetByResourceGroupAsync(m.resourceGroup, m.machineName);
            await vm.StartAsync();
        }

        public async Task StopAsync(string name)
        {
            _logger.LogDebug("Stopping machine {name}", name);
            var m = GetMachine(name);
            var vm = await _azure.VirtualMachines.GetByResourceGroupAsync(m.resourceGroup, m.machineName);
            await vm.PowerOffAsync();
        }

        public async Task DeallocateAsync(string name)
        {
            _logger.LogDebug("Deallocating machine {name}", name);
            var m = GetMachine(name);
            var vm = await _azure.VirtualMachines.GetByResourceGroupAsync(m.resourceGroup, m.machineName);
            await vm.DeallocateAsync();
        }

        public async Task<MachinePowerState> GetStatusAsync(string name)
        {
            _logger.LogDebug("Getting machine {name} status", name);
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
