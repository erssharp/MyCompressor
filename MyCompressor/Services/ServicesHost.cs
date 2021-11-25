using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyCompressor.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCompressor.Services
{
    internal static class ServicesHost
    {
        readonly static IHost host = Host.CreateDefaultBuilder()
                             .ConfigureServices((_, services) =>
                                       services.AddSingleton<IMultiThreadWriter, MultiThreadWriter>()
                                       .AddSingleton<IReaderAsync, ReaderAsync>())
                             .Build();

        readonly static CancellationTokenSource cts = new();

        public static bool IsActive { get; private set; }

        public static IServiceProvider ServiceProvider => host.Services;

        public static void AbortHostWork()
        {
            if (!IsActive) return;

            cts.Cancel();
            host.Dispose();

            IsActive = false;
        }

        public static void StartHost()
        {
            if (IsActive) return;

            IsActive = true;

            host.RunAsync(cts.Token);
        }
    }
}
