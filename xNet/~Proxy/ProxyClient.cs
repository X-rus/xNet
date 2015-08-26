using System;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Threading;

namespace xNet
{
    /// <summary>
    /// Представляет базовую реализацию класса для работы с прокси-сервером.
    /// </summary>
    public abstract class ProxyClient : IEquatable<ProxyClient>
    {
        #region Поля (защищённые)

        /// <summary>Тип прокси-сервера.</summary>
        protected ProxyType _type;

        /// <summary>Хост прокси-сервера.</summary>
        protected string _host;
        /// <summary>Порт прокси-сервера.</summary>
        protected int _port = 1;
        /// <summary>Имя пользователя для авторизации на прокси-сервере.</summary>
        protected string _username;
        /// <summary>Пароль для авторизации на прокси-сервере.</summary>
        protected string _password;

        /// <summary>Время ожидания в миллисекундах при подключении к прокси-серверу.</summary>
        protected int _connectTimeout = 60000;
        /// <summary>Время ожидания в миллисекундах при записи в поток или при чтении из него.</summary>
        protected int _readWriteTimeout = 60000;

        #endregion


        #region Свойства (открытые)

        /// <summary>
        /// Возвращает тип прокси-сервера.
        /// </summary>
        public virtual ProxyType Type
        {
            get
            {
                return _type;
            }
        }

        /// <summary>
        /// Возвращает или задаёт хост прокси-сервера.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        /// <exception cref="System.ArgumentNullException">Значение параметра равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра является пустой строкой.</exception>
        public virtual string Host
        {
            get
            {
                return _host;
            }
            set
            {
                #region Проверка параметра

                if (value == null)
                {
                    throw new ArgumentNullException("Host");
                }

                if (value.Length == 0)
                {
                    throw ExceptionHelper.EmptyString("Host");
                }

                #endregion

                _host = value;
            }
        }

        /// <summary>
        /// Возвращает или задаёт порт прокси-сервера.
        /// </summary>
        /// <value>Значение по умолчанию — 1.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра меньше 1 или больше 65535.</exception>
        public virtual int Port
        {
            get
            {
                return _port;
            }
            set
            {
                #region Проверка параметра

                if (!ExceptionHelper.ValidateTcpPort(value))
                {
                    throw ExceptionHelper.WrongTcpPort("Port");
                }

                #endregion

                _port = value;
            }
        }

        /// <summary>
        /// Возвращает или задаёт имя пользователя для авторизации на прокси-сервере.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра имеет длину более 255 символов.</exception>
        public virtual string Username
        {
            get
            {
                return _username;
            }
            set
            {   
                #region Проверка параметра

                if (value != null && value.Length > 255)
                {
                    throw new ArgumentOutOfRangeException("Username", string.Format(
                        Resources.ArgumentOutOfRangeException_StringLengthCanNotBeMore, 255));
                }

                #endregion

                _username = value;
            }
        }

        /// <summary>
        /// Возвращает или задаёт пароль для авторизации на прокси-сервере.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра имеет длину более 255 символов.</exception>
        public virtual string Password
        {
            get
            {
                return _password;
            }
            set
            {
                #region Проверка параметра

                if (value != null && value.Length > 255)
                {
                    throw new ArgumentOutOfRangeException("Password", string.Format(
                        Resources.ArgumentOutOfRangeException_StringLengthCanNotBeMore, 255));
                }

                #endregion

                _password = value;
            }
        }

        /// <summary>
        /// Возвращает или задаёт время ожидания в миллисекундах при подключении к прокси-серверу.
        /// </summary>
        /// <value>Значение по умолчанию - 60.000, что равняется одной минуте.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра меньше 0.</exception>
        public virtual int ConnectTimeout
        {
            get
            {
                return _connectTimeout;
            }
            set
            {
                #region Проверка параметра

                if (value < 0)
                {
                    throw ExceptionHelper.CanNotBeLess("ConnectTimeout", 0);
                }

                #endregion

                _connectTimeout = value;
            }
        }

        /// <summary>
        /// Возвращает или задает время ожидания в миллисекундах при записи в поток или при чтении из него.
        /// </summary>
        /// <value>Значение по умолчанию - 60.000, что равняется одной минуте.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра меньше 0.</exception>
        public virtual int ReadWriteTimeout
        {
            get
            {
                return _readWriteTimeout;
            }
            set
            {
                #region Проверка параметра

                if (value < 0)
                {
                    throw ExceptionHelper.CanNotBeLess("ReadWriteTimeout", 0);
                }

                #endregion

                _readWriteTimeout = value;
            }
        }

        #endregion


        #region Конструкторы (защищённые)

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ProxyClient"/>.
        /// </summary>
        /// <param name="proxyType">Тип прокси-сервера.</param>
        internal protected ProxyClient(ProxyType proxyType)
        {
            _type = proxyType;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ProxyClient"/>.
        /// </summary>
        /// <param name="proxyType">Тип прокси-сервера.</param>
        /// <param name="address">Хост прокси-сервера.</param>
        /// <param name="port">Порт прокси-сервера.</param>
        internal protected ProxyClient(ProxyType proxyType, string address, int port)
        {
            _type = proxyType;
            _host = address;
            _port = port;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ProxyClient"/>.
        /// </summary>
        /// <param name="proxyType">Тип прокси-сервера.</param>
        /// <param name="address">Хост прокси-сервера.</param>
        /// <param name="port">Порт прокси-сервера.</param>
        /// <param name="username">Имя пользователя для авторизации на прокси-сервере.</param>
        /// <param name="password">Пароль для авторизации на прокси-сервере.</param>
        internal protected ProxyClient(ProxyType proxyType, string address, int port, string username, string password)
        {
            _type = proxyType;
            _host = address;
            _port = port;
            _username = username;
            _password = password;
        }

        #endregion


        #region Статические методы (открытые)

        /// <summary>
        /// Преобразует строку в экземпляр класса прокси-клиента, унаследованный от <see cref="ProxyClient"/>.
        /// </summary>
        /// <param name="proxyType">Тип прокси-сервера.</param>
        /// <param name="proxyAddress">Строка вида - хост:порт:имя_пользователя:пароль. Три последних параметра являются необязательными.</param>
        /// <returns>Экземпляр класса прокси-клиента, унаследованный от <see cref="ProxyClient"/>.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="proxyAddress"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="proxyAddress"/> является пустой строкой.</exception>
        /// <exception cref="System.FormatException">Формат порта является неправильным.</exception>
        /// <exception cref="System.InvalidOperationException">Получен неподдерживаемый тип прокси-сервера.</exception>
        public static ProxyClient Parse(ProxyType proxyType, string proxyAddress)
        {
            #region Проверка параметров

            if (proxyAddress == null)
            {
                throw new ArgumentNullException("proxyAddress");
            }

            if (proxyAddress.Length == 0)
            {
                throw ExceptionHelper.EmptyString("proxyAddress");
            }

            #endregion

            string[] values = proxyAddress.Split(':');

            int port = 0;
            string host = values[0];

            if (values.Length >= 2)
            {
                #region Получение порта

                try
                {
                    port = int.Parse(values[1]);
                }
                catch (Exception ex)
                {
                    if (ex is FormatException || ex is OverflowException)
                    {
                        throw new FormatException(
                            Resources.InvalidOperationException_ProxyClient_WrongPort, ex);
                    }

                    throw;
                }

                if (!ExceptionHelper.ValidateTcpPort(port))
                {
                    throw new FormatException(
                        Resources.InvalidOperationException_ProxyClient_WrongPort);
                }

                #endregion
            }

            string username = null;
            string password = null;

            if (values.Length >= 3)
            {
                username = values[2];
            }

            if (values.Length >= 4)
            {
                password = values[3];
            }

            return ProxyHelper.CreateProxyClient(proxyType, host, port, username, password);
        }

        /// <summary>
        /// Преобразует строку в экземпляр класса прокси-клиента, унаследованный от <see cref="ProxyClient"/>. Возвращает значение, указывающее, успешно ли выполнено преобразование.
        /// </summary>
        /// <param name="proxyType">Тип прокси-сервера.</param>
        /// <param name="proxyAddress">Строка вида - хост:порт:имя_пользователя:пароль. Три последних параметра являются необязательными.</param>
        /// <param name="result">Если преобразование выполнено успешно, то содержит экземпляр класса прокси-клиента, унаследованный от <see cref="ProxyClient"/>, иначе <see langword="null"/>.</param>
        /// <returns>Значение <see langword="true"/>, если параметр <paramref name="proxyAddress"/> преобразован успешно, иначе <see langword="false"/>.</returns>
        public static bool TryParse(ProxyType proxyType, string proxyAddress, out ProxyClient result)
        {
            result = null;

            #region Проверка параметров

            if (string.IsNullOrEmpty(proxyAddress))
            {
                return false;
            }

            #endregion

            string[] values = proxyAddress.Split(':');

            int port = 0;
            string host = values[0];

            if (values.Length >= 2)
            {
                if (!int.TryParse(values[1], out port) || !ExceptionHelper.ValidateTcpPort(port))
                {
                    return false;
                }
            }

            string username = null;
            string password = null;

            if (values.Length >= 3)
            {
                username = values[2];
            }

            if (values.Length >= 4)
            {
                password = values[3];
            }

            try
            {
                result = ProxyHelper.CreateProxyClient(proxyType, host, port, username, password);
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            return true;
        }

        #endregion


        /// <summary>
        /// Создаёт соединение с сервером через прокси-сервер.
        /// </summary>
        /// <param name="destinationHost">Хост пункта назначения, с которым нужно связаться через прокси-сервер.</param>
        /// <param name="destinationPort">Порт пункта назначения, с которым нужно связаться через прокси-сервер.</param>
        /// <param name="tcpClient">Соединение, через которое нужно работать, или значение <see langword="null"/>.</param>
        /// <returns>Соединение с прокси-сервером.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// Значение свойства <see cref="Host"/> равно <see langword="null"/> или имеет нулевую длину.
        /// -или-
        /// Значение свойства <see cref="Port"/> меньше 1 или больше 65535.
        /// -или-
        /// Значение свойства <see cref="Username"/> имеет длину более 255 символов.
        /// -или-
        /// Значение свойства <see cref="Password"/> имеет длину более 255 символов.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="destinationHost"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="destinationHost"/> является пустой строкой.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра <paramref name="destinationPort"/> меньше 1 или больше 65535.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public abstract TcpClient CreateConnection(string destinationHost, int destinationPort, TcpClient tcpClient = null);


        #region Методы (открытые)

        /// <summary>
        /// Формирует строку вида - хост:порт, представляющую адрес прокси-сервера.
        /// </summary>
        /// <returns>Строка вида - хост:порт, представляющая адрес прокси-сервера.</returns>
        public override string ToString()
        {
            return string.Format("{0}:{1}", _host, _port);
        }

        /// <summary>
        /// Формирует строку вида - хост:порт:имя_пользователя:пароль. Последние два параметра добавляются, если они заданы.
        /// </summary>
        /// <returns>Строка вида - хост:порт:имя_пользователя:пароль.</returns>
        public virtual string ToExtendedString()
        {
            var strBuilder = new StringBuilder();

            strBuilder.AppendFormat("{0}:{1}", _host, _port);

            if (!string.IsNullOrEmpty(_username))
            {
                strBuilder.AppendFormat(":{0}", _username);

                if (!string.IsNullOrEmpty(_password))
                {
                    strBuilder.AppendFormat(":{0}", _password);
                }
            }

            return strBuilder.ToString();
        }

        /// <summary>
        /// Возвращает хэш-код для этого прокси-клиента.
        /// </summary>
        /// <returns>Хэш-код в виде 32-битового целого числа со знаком.</returns>
        public override int GetHashCode()
        {
            if (string.IsNullOrEmpty(_host))
            {
                return 0;
            }

            return (_host.GetHashCode() ^ _port);
        }

        /// <summary>
        /// Определяет, равны ли два прокси-клиента.
        /// </summary>
        /// <param name="proxy">Прокси-клиент для сравнения с данным экземпляром.</param>
        /// <returns>Значение <see langword="true"/>, если два прокси-клиента равны, иначе значение <see langword="false"/>.</returns>
        public bool Equals(ProxyClient proxy)
        {
            if (proxy == null || _host == null)
            {
                return false;
            }

            if (_host.Equals(proxy._host,
                StringComparison.OrdinalIgnoreCase) && _port == proxy._port)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Определяет, равны ли два прокси-клиента.
        /// </summary>
        /// <param name="obj">Прокси-клиент для сравнения с данным экземпляром.</param>
        /// <returns>Значение <see langword="true"/>, если два прокси-клиента равны, иначе значение <see langword="false"/>.</returns>
        public override bool Equals(object obj)
        {
            var proxy = obj as ProxyClient;

            if (proxy == null)
            {
                return false;
            }

            return Equals(proxy);
        }

        #endregion


        #region Методы (защищённые)

        /// <summary>
        /// Создаёт соединение с прокси-сервером.
        /// </summary>
        /// <returns>Соединение с прокси-сервером.</returns>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        protected TcpClient CreateConnectionToProxy()
        {
            TcpClient tcpClient = null;

            #region Создание подключения

            tcpClient = new TcpClient();
            Exception connectException = null;
            ManualResetEventSlim connectDoneEvent = new ManualResetEventSlim();

            try
            {
                tcpClient.BeginConnect(_host, _port, new AsyncCallback(
                    (ar) =>
                    {
                        if (tcpClient.Client != null)
                        {
                            try
                            {
                                tcpClient.EndConnect(ar);
                            }
                            catch (Exception ex)
                            {
                                connectException = ex;
                            }

                            connectDoneEvent.Set();
                        }
                    }), tcpClient
                );
            }
            #region Catch's

            catch (Exception ex)
            {
                tcpClient.Close();

                if (ex is SocketException || ex is SecurityException)
                {
                    throw NewProxyException(Resources.ProxyException_FailedConnect, ex);
                }

                throw;
            }

            #endregion

            if (!connectDoneEvent.Wait(_connectTimeout))
            {
                tcpClient.Close();
                throw NewProxyException(Resources.ProxyException_ConnectTimeout);
            }

            if (connectException != null)
            {
                tcpClient.Close();

                if (connectException is SocketException)
                {
                    throw NewProxyException(Resources.ProxyException_FailedConnect, connectException);
                }
                else
                {
                    throw connectException;
                }
            }

            if (!tcpClient.Connected)
            {
                tcpClient.Close();
                throw NewProxyException(Resources.ProxyException_FailedConnect);
            }

            #endregion

            tcpClient.SendTimeout = _readWriteTimeout;
            tcpClient.ReceiveTimeout = _readWriteTimeout;

            return tcpClient;
        }

        /// <summary>
        /// Проверяет различные параметры прокси-клиента на ошибочные значения.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Значение свойства <see cref="Host"/> равно <see langword="null"/> или имеет нулевую длину.</exception>
        /// <exception cref="System.InvalidOperationException">Значение свойства <see cref="Port"/> меньше 1 или больше 65535.</exception>
        /// <exception cref="System.InvalidOperationException">Значение свойства <see cref="Username"/> имеет длину более 255 символов.</exception>
        /// <exception cref="System.InvalidOperationException">Значение свойства <see cref="Password"/> имеет длину более 255 символов.</exception>
        protected void CheckState()
        {
            if (string.IsNullOrEmpty(_host))
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_ProxyClient_WrongHost);
            }

            if (!ExceptionHelper.ValidateTcpPort(_port))
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_ProxyClient_WrongPort);
            }

            if (_username != null && _username.Length > 255)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_ProxyClient_WrongUsername);
            }

            if (_password != null && _password.Length > 255)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_ProxyClient_WrongPassword);
            }
        }

        /// <summary>
        /// Создаёт объект исключения прокси.
        /// </summary>
        /// <param name="message">Сообщение об ошибке с объяснением причины исключения.</param>
        /// <param name="innerException">Исключение, вызвавшее текущие исключение, или значение <see langword="null"/>.</param>
        /// <returns>Объект исключения прокси.</returns>
        protected ProxyException NewProxyException(
            string message, Exception innerException = null)
        {
            return new ProxyException(string.Format(
                message, ToString()), this, innerException);
        }

        #endregion
    }
}