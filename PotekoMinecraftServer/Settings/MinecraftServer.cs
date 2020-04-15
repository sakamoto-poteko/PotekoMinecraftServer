using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PotekoMinecraftServer.Settings
{
    public class MinecraftServerEntry
    {
        public string Name { get; set; }
        public Uri ServerAddress { get; set; }
        public string ResourceGroup { get; set; }
        public string MachineName { get; set; }
    }

    public class MinecraftServer
    {
        public List<MinecraftServerEntry> Endpoints { get; set; }
        public int RefreshInterval { get; set; } = 5;
        public bool AzureUseMsi { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public AzureMsiType AzureMsiType { get; set; }
        public string AzureTenantId { get; set; }
        public string AzureClientId { get; set; }
        public string AzureKey { get; set; }
        public string AzureSubscription { get; set; }
        public int IdleServerShutdownInterval { get; set; }
        public int ServerPowerOffInterval { get; set; }
        public int ServerDeallocateInterval { get; set; }
    }

    public enum AzureMsiType
    {
        Unknown,
        AppService,
        VirtualMachine,
    }
}
