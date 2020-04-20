using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(MachineChecker.Startup))]

namespace MachineChecker
{
    public class Startup : FunctionsStartup 
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
        }
    }
}
