using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace MachineChecker
{
    public static class CheckMachineStatus
    {
        [FunctionName("CheckMachineStatus")]
        public static void Run([TimerTrigger("*/5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"CheckMachineStatus triggered at: {DateTime.Now}");


        }
    }
}
