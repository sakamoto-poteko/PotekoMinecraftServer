using System;
using System.Collections.Generic;
using MachineChecker.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using PotekoMinecraftServerData.Data;

namespace MachineChecker.Functions
{
    public static class StoreMinecraftEndpointStatus
    {
        [FunctionName("StoreMinecraftEndpointStatus")]
        public static void Run(
            [ActivityTrigger] MinecraftEndpointStatus status,
            [Table(Constants.StorageTableName)] ICollector<MinecraftEndpointStatusModel> statusTableCollector,
            ILogger log)
        {
            log.LogInformation($"Store minecraft endpoint status triggered");
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            statusTableCollector.Add(new MinecraftEndpointStatusModel
            {
                PartitionKey = now.ToString(),
                RowKey = $"{now}:{Guid.NewGuid()}",
                Name = status.Name,
                MachineStatus = status.MachineStatus,
                ServerStatus = status.ServerStatus,
            });
        }
    }
}
