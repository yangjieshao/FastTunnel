using System.Reactive.Linq;
using Akavache;
using System.Linq;
using FastTunnel.Core.Client;
using FastTunnel.Core.Config;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace FastTunnel.Api.Helper
{
    public static class CacheHelperEx
    {
        public static IServiceCollection UseAkavache(this IServiceCollection services)
        {
            Registrations.Start("FastTunnel.Server");
            return services;
        }
        public static void StopAkavache()
        {
            BlobCache.Shutdown().WaitAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
    }
    public class CacheHelper
    {
        private FastTunnelServer FastTunnelServer { get; }
        private IOptionsMonitor<DefaultServerConfig> Config { get; }

        private Random Random { get; } = new Random(DateTime.Now.Millisecond);

        private int[] AllCanPort { get; }

        public CacheHelper(FastTunnelServer fastTunnelServer, IOptionsMonitor<DefaultServerConfig> config)
        {
            FastTunnelServer = fastTunnelServer;
            Config = config;
            fastTunnelServer.ForwardAdded += FastTunnelServer_ForwardAdded;
            fastTunnelServer.ForwardRemoved += FastTunnelServer_ForwardRemoved;

            if (Config?.CurrentValue?.PortPool == null
                || Config.CurrentValue.PortPool.Min <= 0
                || Config.CurrentValue.PortPool.Min > 65535
                || Config.CurrentValue.PortPool.Max <= 0
                || Config.CurrentValue.PortPool.Max > 65535
                || Config.CurrentValue.PortPool.Min > Config.CurrentValue.PortPool.Max)
            {
                AllCanPort = Enumerable.Range(1000, 65535).ToArray();
            }
            else
            {
                AllCanPort = Enumerable.Range(Config.CurrentValue.PortPool.Min, Config.CurrentValue.PortPool.Max).ToArray();
            }
        }

        private async void FastTunnelServer_ForwardRemoved(object sender, (string Name, int port) e)
        {
            await RemovePort(e.Name);
        }

        private async void FastTunnelServer_ForwardAdded(object sender, (string Name, int port) e)
        {
            await SetLongPort(e.Name, e.port);
        }

        /// <summary>
        /// 临时 port 
        /// </summary>

        public async ValueTask<int> GetPort(string token, int defaultVal = -1)
        {
            return await BlobCache.LocalMachine.GetObject<int>(token)
                                                             .Catch(Observable.Return(defaultVal));
        }

        public async ValueTask<int> CreatePort()
        {
            var allCanPortArray = new int[AllCanPort.Length];
            Array.Copy(AllCanPort, allCanPortArray, AllCanPort.Length);
            var usedPorts = await BlobCache.LocalMachine.GetAllObjects<int>();
            var allCanPort = allCanPortArray.ToList();
            allCanPort.RemoveAll(r => usedPorts.Contains(r));
            if (allCanPort.Count <= 0)
            {
                return -1;
            }
            var index = Random.Next(0, allCanPort.Count - 1);
            return allCanPort[index];
        }

        public async ValueTask SetLongPort(string token, int val)
        {
            await BlobCache.LocalMachine.InsertObject(token, val);
        }

        public async ValueTask SetTempPort(string token, int val)
        {
            await BlobCache.LocalMachine.InsertObject(token, val, new DateTimeOffset(DateTime.Now.AddMinutes(15)));
        }
        public async ValueTask RemovePort(string token)
        {
            await BlobCache.LocalMachine.Invalidate(token);
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public async Task CleanCache()
        {
            await BlobCache.LocalMachine.InvalidateAll();
            await BlobCache.LocalMachine.Vacuum();
        }

        /// <summary>
        /// 清空过期数据
        /// </summary>
        public async Task CleanInvalidateCache()
        {
            await BlobCache.LocalMachine.Vacuum();
        }
    }
}
