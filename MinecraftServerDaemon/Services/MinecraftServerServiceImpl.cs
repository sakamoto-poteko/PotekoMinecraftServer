using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using PotekoProtos;

namespace MinecraftServerDaemon.Services
{
    public class MinecraftServerServiceImpl : MinecraftServerSerivce.MinecraftServerSerivceBase
    {
        private readonly MinecraftServerProcessService _service;
        private readonly ILogger<MinecraftServerServiceImpl> _logger;

        public MinecraftServerServiceImpl(MinecraftServerProcessService service, ILogger<MinecraftServerServiceImpl> logger)
        {
            _service = service;
            _logger = logger;
        }

        public override Task<MinecraftServerGetStatusReply> GetStatus(MinecraftServerGetStatusRequest request, ServerCallContext context)
        {
            _logger.LogInformation("GetStatus");
            var status = _service.ServerStatus switch
            {
                MinecraftServerProcessService.MinecraftServerStatus.Stopped => MinecraftServerStatus.Stopped,
                MinecraftServerProcessService.MinecraftServerStatus.Starting => MinecraftServerStatus.Stopped,
                MinecraftServerProcessService.MinecraftServerStatus.Running => MinecraftServerStatus.Running,
                MinecraftServerProcessService.MinecraftServerStatus.Stopping => MinecraftServerStatus.Running,
                MinecraftServerProcessService.MinecraftServerStatus.Error => MinecraftServerStatus.Error,
                _ => throw new ArgumentOutOfRangeException(),
            };
            return Task.FromResult(new MinecraftServerGetStatusReply { Status = status });
        }

        public override Task<MinecraftServerListUserReply> ListUser(MinecraftServerListUserRequest request, ServerCallContext context)
        {
            _logger.LogInformation("ListUser");
            return Task.FromResult(new MinecraftServerListUserReply
            {
                Max = _service.MaxPlayers,
                Online = _service.OnlinePlayers,
            });
        }

        public override async Task<MinecraftServerOperationReply> Operation(MinecraftServerOperationRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Operation");
            switch (request.Operation)
            {
                case MinecraftServerOperation.Start:
                    _service.Start();
                    break;
                case MinecraftServerOperation.Stop:
                    _service.Stop(true);
                    await _service.WaitForProcessExitAsync();
                    // TODO: error handling required
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return new MinecraftServerOperationReply { Completed = true };
        }
    }
}
