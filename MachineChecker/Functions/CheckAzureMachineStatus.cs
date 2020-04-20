using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MachineChecker.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using PotekoMinecraftServerData.Data;

namespace MachineChecker.Functions
{
    public class CheckAzureMachineStatus
    {
        [FunctionName("CheckAzureMachineStatusTriggerByTime")]
        public async Task CheckMachineStatusTriggerByTime([TimerTrigger("*/5 * * * *")]TimerInfo _,
            ILogger log)
        {
            log.LogInformation($"CheckAzureMachineStatus triggered at: {DateTime.Now}");

            await CheckAzureMachineStatusAsync(statusTableCollector);
        }

        [FunctionName("CheckAzureMachineStatusTriggerByHttp")]
        public async Task<IActionResult> CheckMachineStatusTriggerByHttp(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "CheckAzureMachineStatus")] HttpRequest _,
            [Table(Constants.StorageTableName)] ICollector<MinecraftEndpointStatusModel> statusTableCollector,
            ILogger log)
        {
            log.LogInformation("CheckAzureMachineStatus triggered by HTTP");

            await CheckAzureMachineStatusAsync(statusTableCollector);

            return new OkResult();
        }

        private async Task CheckAzureMachineStatusAsync(ICollector<MinecraftEndpointStatusModel> statusTableCollector)
        {

        }
    }
}
