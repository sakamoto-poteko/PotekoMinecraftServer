using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace MinecraftServerDaemon.Services
{
    public class MinecraftServerStarter : IHostedService
    {
        private readonly IMinecraftServerProcessService _service;

        public MinecraftServerStarter(IMinecraftServerProcessService service)
        {
            _service = service;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _service.Start();
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _service.Stop(true);
            var stopped = await _service.WaitForProcessExitAsync(5);
            if (!stopped)
            {
                _service.Stop(false);
            }
        }
    }
}
