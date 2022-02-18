using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MinecraftServerDaemon.Services;

namespace MinecraftServerDaemon
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(ConfigureServices);

        public static void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
        {
            var configuration = ctx.Configuration;
            services.Configure<Settings.MinecraftServerInfo>(configuration.GetSection("MinecraftServerInfo"));
            services.Configure<Settings.GrpcServerInfo>(configuration.GetSection("GrpcServerInfo"));

            services.AddSingleton<IMinecraftServerProcessService, MinecraftServerProcessService>();
            services.AddSingleton<MinecraftServerServiceImpl>();
            services.AddHostedService<GrpcServer>();
            services.AddHostedService<MinecraftServerStarter>();
        }
    }
}
