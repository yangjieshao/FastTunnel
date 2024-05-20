using Yarp.ReverseProxy.Configuration;

namespace FastTunnel.Core.Forwarder
{
    public class FastTunnelProxyConfigProvider : IProxyConfigProvider
    {
        public IProxyConfig GetConfig()
        {
            return new FastTunnelProxyConfig();
        }
    }
}
