using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace xNet.Net
{
    /// <summary>
    /// Представляет клиент для Socks4 прокси-сервера.
    /// </summary>
    public class Socks4ProxyClient : ProxyClient
    {
        #region Константы (внутренние защищённые)

        internal protected const int DefaultPort = 1080;

        internal protected const byte VersionNumber = 4;
        internal protected const byte CommandConnect = 0x01;
        internal protected const byte CommandBind = 0x02;
        internal protected const byte CommandReplyRequestGranted = 0x5a;
        internal protected const byte CommandReplyRequestRejectedOrFailed = 0x5b;
        internal protected const byte CommandReplyRequestRejectedCannotConnectToIdentd = 0x5c;
        internal protected const byte CommandReplyRequestRejectedDifferentIdentd = 0x5d;

        #endregion


        #region Конструкторы (открытые)

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Socks4ProxyClient"/>.
        /// </summary>
        public Socks4ProxyClient()
            : this(null) { }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Socks4ProxyClient"/> заданным хостом прокси-сервера, и устанавливает порт равным - 1080.
        /// </summary>
        /// <param name="host">Хост прокси-сервера.</param>
        public Socks4ProxyClient(string host)
            : this(host, DefaultPort) { }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Socks4ProxyClient"/> заданными данными о прокси-сервере.
        /// </summary>
        /// <param name="host">Хост прокси-сервера.</param>
        /// <param name="port">Порт прокси-сервера.</param>
        public Socks4ProxyClient(string host, int port)
            : this(host, port, string.Empty) { }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Socks4ProxyClient"/> заданными данными о прокси-сервере.
        /// </summary>
        /// <param name="host">Хост прокси-сервера.</param>
        /// <param name="port">Порт прокси-сервера.</param>
        /// <param name="username">Имя пользователя для авторизации на прокси-сервере.</param>
        public Socks4ProxyClient(string host, int port, string username)
            : base(ProxyType.Socks4, host, port, username, null) { }

        #endregion


        #region Статические методы (закрытые)

        /// <summary>
        /// Преобразует строку в экземпляр класса <see cref="Socks4ProxyClient"/>.
        /// </summary>
        /// <param name="proxyAddress">Строка вида - хост:порт:имя_пользователя:пароль. Три последних параметра являются необязательными.</param>
        /// <param name="proxyType">Тип прокси-сервера.</param>
        /// <returns>Экземпляр класса <see cref="Socks4ProxyClient"/>.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="proxyAddress"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="proxyAddress"/> является пустой строкой.</exception>
        /// <exception cref="System.FormatException">Формат порта является неправильным.</exception>
        /// <exception cref="System.InvalidOperationException">Получен неподдерживаемый тип прокси-сервера.</exception>
        public static Socks4ProxyClient Parse(string proxyAddress)
        {
            return ProxyClient.Parse(ProxyType.Socks4, proxyAddress) as Socks4ProxyClient;
        }

        /// <summary>
        /// Преобразует строку в экземпляр класса <see cref="Socks4ProxyClient"/>. Возвращает значение, указывающее, успешно ли выполнено преобразование.
        /// </summary>
        /// <param name="proxyAddress">Строка вида - хост:порт:имя_пользователя:пароль. Три последних параметра являются необязательными.</param>
        /// <param name="proxyType">Тип прокси-сервера.</param>
        /// <param name="result">Если преобразование выполнено успешно, то содержит экземпляр класса <see cref="Socks4ProxyClient"/>, иначе <see langword="null"/>.</param>
        /// <returns>Значение <see langword="true"/>, если параметр <paramref name="proxyAddress"/> преобразован успешно, иначе <see langword="false"/>.</returns>
        public static bool TryParse(string s, out Socks4ProxyClient result)
        {
            ProxyClient proxy;

            if (ProxyClient.TryParse(ProxyType.Socks4, s, out proxy))
            {
                result = proxy as Socks4ProxyClient;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        #endregion


        /// <summary>
        /// Создаёт соединение с прокси-сервером.
        /// </summary>
        /// <param name="destinationHost">Хост пункта назначения, с которым нужно связаться через прокси-сервер.</param>
        /// <param name="destinationPort">Порт пункта назначения, с которым нужно связаться через прокси-сервер.</param>
        /// <returns>Соединение с прокси-сервером.</returns>
        /// <exception cref="System.InvalidOperationException">Значение свойства <see cref="xNet.Net.ProxyClient.Host"/> равно <see langword="null"/> или имеет нулевую длину.</exception>
        /// <exception cref="System.InvalidOperationException">Значение свойства <see cref="xNet.Net.ProxyClient.Port"/> меньше 1 или больше 65535.</exception>
        /// <exception cref="System.InvalidOperationException">Значение свойства <see cref="xNet.Net.ProxyClient.Username"/> имеет длину более 255 символов.</exception>
        /// <exception cref="System.InvalidOperationException">Значение свойства <see cref="xNet.Net.ProxyClient.Password"/> имеет длину более 255 символов.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="destinationHost"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="destinationHost"/> является пустой строкой.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра <paramref name="destinationPort"/> меньше 1 или больше 65535.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public override TcpClient CreateConnection(string destinationHost, int destinationPort)
        {
            CheckState();

            #region Проверка параметров

            if (destinationHost == null)
            {
                throw new ArgumentNullException("destinationHost");
            }

            if (destinationHost.Length == 0)
            {
                throw ExceptionHelper.EmptyString("destinationHost");
            }

            if (!ExceptionHelper.ValidateTcpPort(destinationPort))
            {
                throw ExceptionHelper.WrongTcpPort("destinationPort");
            }

            #endregion

            TcpClient tcpClient = CreateConnectionWithProxy();

            try
            {
                SendCommand(tcpClient.GetStream(), CommandConnect, destinationHost, destinationPort);
            }
            catch (Exception ex)
            {
                tcpClient.Close();

                if (ex is IOException || ex is SocketException)
                {
                    throw NewProxyException(Resources.ProxyException_Error, ex);
                }

                throw;
            }

            return tcpClient;
        }


        #region Методы (внутренние защищённые)

        internal protected virtual void SendCommand(NetworkStream nStream, byte command, string destinationHost, int destinationPort)
        {
            byte[] dstPort = GetIPAddressBytes(destinationHost);
            byte[] dstIp = GetPortBytes(destinationPort);

            byte[] userId = string.IsNullOrEmpty(_username) ?
                new byte[0] : Encoding.ASCII.GetBytes(_username);

            // +----+----+----+----+----+----+----+----+----+----+....+----+
            // | VN | CD | DSTPORT |      DSTIP        | USERID       |NULL|
            // +----+----+----+----+----+----+----+----+----+----+....+----+
            //    1    1      2              4           variable       1
            byte[] request = new byte[9 + userId.Length];

            request[0] = VersionNumber;
            request[1] = command;
            dstIp.CopyTo(request, 2);
            dstPort.CopyTo(request, 4);
            userId.CopyTo(request, 8);
            request[8 + userId.Length] = 0x00;

            nStream.Write(request, 0, request.Length);

            // +----+----+----+----+----+----+----+----+
            // | VN | CD | DSTPORT |      DSTIP        |
            // +----+----+----+----+----+----+----+----+
            //   1    1       2              4
            byte[] response = new byte[8];

            nStream.Read(response, 0, response.Length);

            byte reply = response[1];

            // Если запрос не выполнен.
            if (reply != CommandReplyRequestGranted)
            {
                HandleCommandError(reply);
            }
        }

        internal protected byte[] GetIPAddressBytes(string destinationHost)
        {
            IPAddress ipAddr = null;

            if (!IPAddress.TryParse(destinationHost, out ipAddr))
            {
                try
                {
                    IPAddress[] ips = Dns.GetHostAddresses(destinationHost);

                    if (ips.Length > 0)
                    {
                        ipAddr = ips[0];
                    }
                }
                catch (Exception ex)
                {
                    if (ex is SocketException || ex is ArgumentException)
                    {
                        throw new ProxyException(string.Format(
                            Resources.ProxyException_FailedGetHostAddresses, destinationHost), this, ex);
                    }

                    throw;
                }
            }

            return ipAddr.GetAddressBytes();
        }

        internal protected byte[] GetPortBytes(int port)
        {
            byte[] array = new byte[2];

            array[0] = (byte)(port / 256);
            array[1] = (byte)(port % 256);

            return array;
        }

        internal protected void HandleCommandError(byte command)
        {
            string errorMessage;

            switch (command)
            {
                case CommandReplyRequestRejectedOrFailed:
                    errorMessage = Resources.Socks4_CommandReplyRequestRejectedOrFailed;
                    break;

                case CommandReplyRequestRejectedCannotConnectToIdentd:
                    errorMessage = Resources.Socks4_CommandReplyRequestRejectedCannotConnectToIdentd;
                    break;

                case CommandReplyRequestRejectedDifferentIdentd:
                    errorMessage = Resources.Socks4_CommandReplyRequestRejectedDifferentIdentd;
                    break;

                default:
                    errorMessage = Resources.Socks_UnknownError;
                    break;
            }

            string exceptionMsg = string.Format(
                Resources.ProxyException_CommandError, errorMessage, ToString());

            throw new ProxyException(exceptionMsg, this);
        }

        #endregion
    }
}