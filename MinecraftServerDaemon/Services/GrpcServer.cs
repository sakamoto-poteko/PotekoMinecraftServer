using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinecraftServerDaemon.Settings;
using PotekoProtos;

namespace MinecraftServerDaemon.Services
{
    public class GrpcServer : IHostedService
    {
        private readonly MinecraftServerServiceImpl _minecraftServerServiceImpl;
        private readonly ILogger<GrpcServer> _logger;
        private readonly string _certificatePem;
        private readonly string _keyPem;
        private Server _server;
        private readonly string _host;
        private readonly int _port;
        private readonly string _rootCertificatePem;
        private bool _enforceClientVerification;

        public GrpcServer(IOptions<GrpcServerInfo> grpcServerInfo,
            MinecraftServerServiceImpl minecraftServerServiceImpl,
            ILogger<GrpcServer> logger)
        {
            _logger = logger;
            var settings = grpcServerInfo.Value;
            _host = settings.Host;
            _port = settings.Port;
            _rootCertificatePem = settings.RootClientCertificate;
            _enforceClientVerification = settings.Enforce;
            _minecraftServerServiceImpl = minecraftServerServiceImpl;
            _certificatePem = Encoding.UTF8.GetString(Convert.FromBase64String(settings.Certificate));
            _keyPem = Encoding.UTF8.GetString(Convert.FromBase64String(settings.Key));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            ServerCredentials cred;
            if (_enforceClientVerification)
            {
                cred = new SslServerCredentials(
                    new List<KeyCertificatePair>
                    {
                        new KeyCertificatePair(_certificatePem, _keyPem)
                    }, _rootCertificatePem, SslClientCertificateRequestType.RequestAndRequireAndVerify);
            }
            else
            {
                cred = new SslServerCredentials(
                    new List<KeyCertificatePair>
                    {
                        new KeyCertificatePair(_certificatePem, _keyPem)
                    });
            }

            _server = new Server
            {
                Services = { MinecraftServerSerivce.BindService(_minecraftServerServiceImpl) },
                Ports = { new ServerPort(_host, _port, cred) }
            };

            try
            {
                _server.Start();
            }
            catch (IOException ex)
            {
                _logger.LogError($"Failed to start GRPC server: {ex.Message}");
                throw;
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _server.ShutdownAsync();
        }
    }
}

