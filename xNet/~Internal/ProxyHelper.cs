using System;

namespace xNet
{
    internal static class ProxyHelper
    {
        public static ProxyClient CreateProxyClient(ProxyType proxyType, string host = null,
            int port = 0, string username = null, string password = null)
        {
            switch (proxyType)
            {
                case ProxyType.Http:
                    return (port == 0) ?
                        new HttpProxyClient(host) : new HttpProxyClient(host, port, username, password);

                case ProxyType.Socks4:
                    return (port == 0) ?
                        new Socks4ProxyClient(host) : new Socks4ProxyClient(host, port, username);

                case ProxyType.Socks4a:
                    return (port == 0) ?
                        new Socks4aProxyClient(host) : new Socks4aProxyClient(host, port, username);

                case ProxyType.Socks5:
                    return (port == 0) ?
                        new Socks5ProxyClient(host) : new Socks5ProxyClient(host, port, username, password);

                default:
                    throw new InvalidOperationException();
            }
        }
    }
}