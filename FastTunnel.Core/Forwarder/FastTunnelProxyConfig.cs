using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace FastTunnel.Core.Forwarder
{
    public class FastTunnelProxyConfig : IProxyConfig
    {
        public FastTunnelProxyConfig()
            : this([], [])
        {
        }

        public FastTunnelProxyConfig(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
        {
            Routes = routes;
            Clusters = clusters;
            ChangeToken = new CancellationChangeToken(cancellationToken.Token);
        }

        public IReadOnlyList<RouteConfig> Routes { get; }

        public IReadOnlyList<ClusterConfig> Clusters { get; }

        public IChangeToken ChangeToken { get; }

        private readonly CancellationTokenSource cancellationToken = new();
    }
}
