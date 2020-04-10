using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MinecraftServerDaemon.Settings;
using PotekoProtos;

namespace MinecraftServerDaemon.Services
{
    public class GrpcServer : IHostedService
    {
        private readonly MinecraftServerServiceImpl _minecraftServerServiceImpl;
        private Server _server;
        private readonly string _host;
        private readonly int _port;

        public GrpcServer(IOptions<GrpcServerInfo> grpcServerInfo,
            MinecraftServerServiceImpl minecraftServerServiceImpl)
        {
            var settings = grpcServerInfo.Value;
            _host = settings.Host;
            _port = settings.Port;
            _minecraftServerServiceImpl = minecraftServerServiceImpl;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _server = new Server
            {
                Services = { MinecraftServerSerivce.BindService(_minecraftServerServiceImpl) },
                Ports = { new ServerPort(_host, _port, ServerCredentials.Insecure) }
            };
            _server.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _server.ShutdownAsync();
        }
    }
}

