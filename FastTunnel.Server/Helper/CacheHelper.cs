using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reactive.Linq;
using Akavache;
using FastTunnel.Core.Client;
using FastTunnel.Core.Config;
using Microsoft.Extensions.Options;

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
        private ILogger<CacheHelper> Logger { get; }

        private List<int> AllCanPort { get; }

        public CacheHelper(FastTunnelServer fastTunnelServer
            , IOptionsMonitor<DefaultServerConfig> config, ILogger<CacheHelper> logger)
        {
            FastTunnelServer = fastTunnelServer;
            Config = config;
            Logger = logger;
            FastTunnelServer.ForwardAdded += FastTunnelServer_ForwardAdded;
            FastTunnelServer.ForwardRemoved += FastTunnelServer_ForwardRemoved;

            var minPort = 4000;
            var maxPort = 65535;
            if ((Config?.CurrentValue?.PortPool) != null
                && Config.CurrentValue.PortPool.Min > 0
                && Config.CurrentValue.PortPool.Min <= 65535
                && Config.CurrentValue.PortPool.Max > 0
                && Config.CurrentValue.PortPool.Max <= 65535
                && Config.CurrentValue.PortPool.Min <= Config.CurrentValue.PortPool.Max)
            {
                minPort = Config.CurrentValue.PortPool.Min;
                maxPort = Config.CurrentValue.PortPool.Max;
            }

            AllCanPort = Enumerable.Range(minPort, maxPort - minPort + 1).ToList();

            CheckPortsCanUse();
        }

        private void CheckPortsCanUse()
        {
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpEndPoints = ipGlobalProperties.GetActiveTcpListeners();

            var deletePorts = new List<int>();
            foreach (var endPoint in tcpEndPoints)
            {
                deletePorts.Add(endPoint.Port);
            }
            deletePorts = deletePorts.Distinct().ToList();
            AllCanPort.RemoveAll(deletePorts.Contains);

            var deletePortsBag = new ConcurrentBag<int>();
            AllCanPort.AsParallel().WithCancellation(CancellationToken.None)
                                      .WithDegreeOfParallelism(AllCanPort.Count > 512 ? 512 : AllCanPort.Count)
                                      .ForAll(port =>
                                      {
                                          if (!CanPortUse(port))
                                          {
                                              deletePortsBag.Add(port);
                                          }
                                      });
            deletePorts.AddRange(deletePortsBag);
            deletePorts = deletePorts.Distinct().ToList();
            AllCanPort.RemoveAll(deletePorts.Contains);

            if (deletePorts.Count > 0)
            {
                for (var i = deletePorts.Count - 1; i >= 0; i--)
                {
                    var deletePort = deletePorts[i];
                    if (deletePort > AllCanPort[^1]
                        || deletePort < AllCanPort[0])
                    {
                        deletePorts.RemoveAt(i);
                    }
                }
            }

            if (deletePorts.Count == 0)
            {
                Logger.LogInformation("可用端口池范围：{min}~{max}", AllCanPort[0], AllCanPort[^1]);
            }
            else
            {
                deletePorts.Sort();
                Logger.LogInformation("可用端口池范围：{min}~{max} 其中 [{deletePorts}] 不可用", AllCanPort[0], AllCanPort[^1], string.Join(',', deletePorts));
            }
        }

        /// <summary>
        /// 判断端口是否可用
        /// </summary>
        private bool CanPortUse(int port)
        {
            var canUse = false;
            TcpListener listener = null;
            try
            {
                // 尝试在指定端口上创建TcpListener
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                // 如果能够启动，则端口可用
                canUse = true;
            }
            catch (SocketException)
            {
                // 如果抛出异常，则端口不可用
                canUse = false;
            }
            finally
            {
                // 无论成功与否，都确保关闭TcpListener
                if (listener != null)
                {
                    listener.Stop();
                    listener.Dispose();
                }
            }
            return canUse;
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
            var allCanPortArray = new int[AllCanPort.Count];
            Array.Copy(AllCanPort.ToArray(), allCanPortArray, AllCanPort.Count);
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
