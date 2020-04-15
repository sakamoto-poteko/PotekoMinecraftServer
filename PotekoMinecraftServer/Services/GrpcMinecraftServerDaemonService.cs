using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PotekoProtos;

namespace PotekoMinecraftServer.Services
{
    public class GrpcMinecraftServerDaemonService : IMinecraftServerDaemonService
    {
        private readonly ILogger<GrpcMinecraftServerDaemonService> _logger;
        private readonly Dictionary<string, MinecraftServerSerivce.MinecraftServerSerivceClient> _clients = new Dictionary<string, MinecraftServerSerivce.MinecraftServerSerivceClient>();

        public GrpcMinecraftServerDaemonService(IOptions<Settings.MinecraftServer> options,
            ILogger<GrpcMinecraftServerDaemonService> logger)
        {
            _logger = logger;
            var settings = options.Value;

            foreach (var srv in settings.Endpoints)
            {
                // BUG: add settings accepts any cert
                var httpClientHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                // Return `true` to allow certificates that are untrusted/invalid
                var httpClient = new HttpClient(httpClientHandler);

                var channel = GrpcChannel.ForAddress(srv.ServerAddress, new GrpcChannelOptions
                {
                     HttpClient = httpClient
                });

                var client = new MinecraftServerSerivce.MinecraftServerSerivceClient(channel);
                _clients.Add(srv.Name, client);
            }
        }

        public Task<bool> StartServerAsync(string name)
        {
            return DaemonOperationAsync(name, MinecraftServerOperation.Start);
        }

        public Task<bool> StopServerAsync(string name)
        {
            return DaemonOperationAsync(name, MinecraftServerOperation.Stop);
        }

        private MinecraftServerSerivce.MinecraftServerSerivceClient GetClient(string name)
        {
            if (!_clients.TryGetValue(name, out var client))
            {
                throw new ArgumentOutOfRangeException(nameof(name), "Name does not exist");
            }

            return client;
        }

        private async Task<bool> DaemonOperationAsync(string name, MinecraftServerOperation operation)
        {
            try
            {
                var result = await GetClient(name).OperationAsync(new MinecraftServerOperationRequest
                {
                    Operation = operation
                });

                if (result.Completed)
                {
                    return true;
                }
                else
                {
                    _logger.LogError($"MC server operation `{operation}' failed: {result.ErrorMessage}");
                    return false;
                }
            }
            catch (RpcException e)
            {
                _logger.LogError($"OperationAsync error for server {name}: {e.Message}");
                return false;
            }
        }

        public async Task<OnlinePlayers> ListPlayersAsync(string name)
        {
            try
            {
                var users = await GetClient(name).ListUserAsync(new MinecraftServerListUserRequest());
                return new OnlinePlayers
                {
                    Max = users.Max,
                    Online = users.Online,
                    Players = users.Users.ToList(),
                };
            }
            catch (RpcException e)
            {
                _logger.LogError($"ListUserAsync error for server {name}: {e.Message}");
                return new OnlinePlayers
                {
                    Max = 0,
                    Online = 0,
                };
            }
        }

        public async Task<MinecraftBdsStatus> GetServerStatusAsync(string name)
        {
            try
            {
                var result = await GetClient(name).GetStatusAsync(new MinecraftServerGetStatusRequest());
                return result.Status switch
                {
                    PotekoProtos.MinecraftServerStatus.Stopped => MinecraftBdsStatus.Stopped,
                    PotekoProtos.MinecraftServerStatus.Running => MinecraftBdsStatus.Running,
                    PotekoProtos.MinecraftServerStatus.Error => MinecraftBdsStatus.Error,
                    _ => throw new ArgumentOutOfRangeException(),
                };
            }
            catch (RpcException e)
            {
                _logger.LogError($"GetStatusAsyncError for server {name}: {e.Message}");
                return MinecraftBdsStatus.NetworkError;
            }
        }
    }

}
