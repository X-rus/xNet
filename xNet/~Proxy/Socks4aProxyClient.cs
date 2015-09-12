using System.Net.Sockets;
using System.Text;

namespace xNet
{
    /// <summary>
    /// Представляет клиент для Socks4a прокси-сервера.
    /// </summary>
    public class Socks4aProxyClient : Socks4ProxyClient 
    {
        #region Конструкторы (открытые)

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Socks4aProxyClient"/>.
        /// </summary>
        public Socks4aProxyClient()
            : this(null) { }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Socks4aProxyClient"/> заданным хостом прокси-сервера, и устанавливает порт равным - 1080.
        /// </summary>
        /// <param name="host">Хост прокси-сервера.</param>
        public Socks4aProxyClient(string host)
            : this(host, DefaultPort) { }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Socks4aProxyClient"/> заданными данными о прокси-сервере.
        /// </summary>
        /// <param name="host">Хост прокси-сервера.</param>
        /// <param name="port">Порт прокси-сервера.</param>
        public Socks4aProxyClient(string host, int port)
            : this(host, port, string.Empty) { }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Socks4aProxyClient"/> заданными данными о прокси-сервере.
        /// </summary>
        /// <param name="host">Хост прокси-сервера.</param>
        /// <param name="port">Порт прокси-сервера.</param>
        /// <param name="username">Имя пользователя для авторизации на прокси-сервере.</param>
        public Socks4aProxyClient(string host, int port, string username)
            : base(host, port, username)
        {
            _type = ProxyType.Socks4a;
        }

        #endregion


        #region Методы (открытые)

        /// <summary>
        /// Преобразует строку в экземпляр класса <see cref="Socks4aProxyClient"/>.
        /// </summary>
        /// <param name="proxyAddress">Строка вида - хост:порт:имя_пользователя:пароль. Три последних параметра являются необязательными.</param>
        /// <returns>Экземпляр класса <see cref="Socks4aProxyClient"/>.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="proxyAddress"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="proxyAddress"/> является пустой строкой.</exception>
        /// <exception cref="System.FormatException">Формат порта является неправильным.</exception>
        public static Socks4aProxyClient Parse(string proxyAddress)
        {
            return ProxyClient.Parse(ProxyType.Socks4a, proxyAddress) as Socks4aProxyClient;
        }

        /// <summary>
        /// Преобразует строку в экземпляр класса <see cref="Socks4aProxyClient"/>. Возвращает значение, указывающее, успешно ли выполнено преобразование.
        /// </summary>
        /// <param name="proxyAddress">Строка вида - хост:порт:имя_пользователя:пароль. Три последних параметра являются необязательными.</param>
        /// <param name="result">Если преобразование выполнено успешно, то содержит экземпляр класса <see cref="Socks4aProxyClient"/>, иначе <see langword="null"/>.</param>
        /// <returns>Значение <see langword="true"/>, если параметр <paramref name="proxyAddress"/> преобразован успешно, иначе <see langword="false"/>.</returns>
        public static bool TryParse(string proxyAddress, out Socks4aProxyClient result)
        {
            ProxyClient proxy;

            if (ProxyClient.TryParse(ProxyType.Socks4a, proxyAddress, out proxy))
            {
                result = proxy as Socks4aProxyClient;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        #endregion


        internal protected override void SendCommand(NetworkStream nStream, byte command, string destinationHost, int destinationPort)
        {
            byte[] dstPort = GetPortBytes(destinationPort);
            byte[] dstIp = { 0, 0, 0, 1 };

            byte[] userId = string.IsNullOrEmpty(_username) ?
                new byte[0] : Encoding.ASCII.GetBytes(_username);

            byte[] dstAddr = ASCIIEncoding.ASCII.GetBytes(destinationHost);

            // +----+----+----+----+----+----+----+----+----+----+....+----+----+----+....+----+
            // | VN | CD | DSTPORT |      DSTIP        | USERID       |NULL| DSTADDR      |NULL|
            // +----+----+----+----+----+----+----+----+----+----+....+----+----+----+....+----+
            //    1    1      2              4           variable       1    variable        1 
            byte[] request = new byte[10 + userId.Length + dstAddr.Length];

            request[0] = VersionNumber;
            request[1] = command;
            dstPort.CopyTo(request, 2);
            dstIp.CopyTo(request, 4);
            userId.CopyTo(request, 8);
            request[8 + userId.Length] = 0x00;
            dstAddr.CopyTo(request, 9 + userId.Length);
            request[9 + userId.Length + dstAddr.Length] = 0x00;

            nStream.Write(request, 0, request.Length);

            // +----+----+----+----+----+----+----+----+
            // | VN | CD | DSTPORT |      DSTIP        |
            // +----+----+----+----+----+----+----+----+
            //    1    1      2              4
            byte[] response = new byte[8];

            nStream.Read(response, 0, 8);

            byte reply = response[1];

            // Если запрос не выполнен.
            if (reply != CommandReplyRequestGranted)
            {
                HandleCommandError(reply);
            }
        }
    }
}