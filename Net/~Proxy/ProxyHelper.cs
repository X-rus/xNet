using System;

namespace xNet.Net
{
    /// <summary>
    /// Представляет статический класс, предназначенный для помощи в работе с прокси.
    /// </summary>
    public static class ProxyHelper
    {
        /// <summary>
        /// Создаёт клиент для заданного типа прокси-сервера.
        /// </summary>
        /// <param name="proxyType">Тип прокси-сервера.</param>
        /// <param name="host">Хост прокси-сервера, или значение <see langword="null"/>.</param>
        /// <param name="port">Порт прокси-сервера.</param>
        /// <param name="username">Имя пользователя для авторизации на прокси-сервере, или значение <see langword="null"/>.</param>
        /// <param name="password">Пароль для авторизации на прокси-сервере, или значение <see langword="null"/>.</param>
        /// <returns>Экземпляр класса прокси-клиента, унаследованный от <see cref="xNet.Net.ProxyClient"/>.</returns>
        /// <exception cref="System.InvalidOperationException">Получен неподдерживаемый тип прокси-сервера.</exception>
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
                    throw new InvalidOperationException(string.Format(
                        Resources.InvalidOperationException_ProxyHelper_NotSupportedProxyType, proxyType));
            }
        }
    }
}