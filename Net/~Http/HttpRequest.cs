using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Threading;
using xNet.Collections;

namespace xNet.Net
{
    /// <summary>
    /// Представляет класс, предназначеннный для отправки запросов HTTP-серверу.
    /// </summary>
    public class HttpRequest : IDisposable
    {
        /// <summary>
        /// Версия HTTP-протокола, используемая в запросе.
        /// </summary>
        public static readonly Version ProtocolVersion = new Version(1, 1);


        private static ProxyClient _currentProxy;


        #region Статические свойства (открытые)

        /// <summary>
        /// Возвращает или задаёт значение, указывающие, нужно ли использовать прокси-клиент Internet Explorer'a, если нет прямого подключения к интернету и не задан прокси-клиент.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="false"/>.</value>
        public static bool UseIeProxy { get; set; }

        /// <summary>
        /// Возвращает или задаёт значение, указывающие, нужно ли отключать прокси-клиент для локальных адресов.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="false"/>.</value>
        public static bool DisableProxyForLocalAddress { get; set; }

        /// <summary>
        /// Возвращает или задаёт глобальный прокси-клиент.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        public static ProxyClient GlobalProxy { get; set; }

        /// <summary>
        /// Возвращает или задаёт глобальное значение HTTP-заголовка 'User-Agent'.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        public static string GlobalUserAgent { get; set; }

        #endregion


        #region Поля (закрытые)

        private HttpResponse _response;

        private TcpClient _tcpClient;
        private Stream _clientStream;
        private NetworkStream _clientNetworkStream;

        private int _connectTimeout = 60000;
        private int _readWriteTimeout = 60000;

        private int _redirectionsCount = 0;
        private int _maximumAutomaticRedirections = 5;

        private int _bytesSent;
        private int _bytesReceived;
        private int _messageBodyLength;
        private int _sendBufferSize;

        private EventHandler<UploadProgressChangedEventArgs> _uploadProgressChangedHandler;
        private EventHandler<DownloadProgressChangedEventArgs> _downloadProgressChangedHandler;

        private MultipartDataCollection _multipartData;
        private readonly StringDictionary _headers = new StringDictionary(StringComparer.OrdinalIgnoreCase);

        #endregion


        #region События (открытые)

        /// <summary>
        /// Возникает каждый раз при продвижении хода выгрузки данных.
        /// </summary>
        public event EventHandler<UploadProgressChangedEventArgs> UploadProgressChanged
        {
            add
            {
                _uploadProgressChangedHandler += value;
            }
            remove
            {
                _uploadProgressChangedHandler -= value;
            }
        }

        /// <summary>
        /// Возникает каждый раз при продвижении хода загрузки данных.
        /// </summary>
        public event EventHandler<DownloadProgressChangedEventArgs> DownloadProgressChanged
        {
            add
            {
                _downloadProgressChangedHandler += value;
            }
            remove
            {
                _downloadProgressChangedHandler -= value;
            }
        }

        #endregion


        #region Свойства (открытые)

        /// <summary>
        /// Возвращает URI интернет-ресурса, который фактически отвечает на запрос.
        /// </summary>
        public Uri Address { get; private set; }

        /// <summary>
        /// Возвращает последний ответ от HTTP-сервера, полученный данным экземпляром класса.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        public HttpResponse Response
        {
            get
            {
                return _response;
            }
        }

        /// <summary>
        /// Возвращает или задает прокси-клиент.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        public ProxyClient Proxy { get; set; }

        /// <summary>
        /// Возвращает или задает метод делегата, вызываемый при проверки сертификата SSL, используемый для проверки подлинности.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        public RemoteCertificateValidationCallback SslCertificateValidatorCallback;

        #region Поведение

        /// <summary>
        /// Возвращает или задает значение, указывающие, нужно ли кодировать параметры POST-запросов.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="true"/>.</value>
        /// <remarks>Параметры кодируются с помощью метода <see cref="Uri.EscapeDataString"/>.</remarks>
        public bool EnableAutoUrlEncode { get; set; }

        /// <summary>
        /// Возвращает или задает значение, указывающие, должен ли запрос следовать ответам переадресации.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="true"/>.</value>
        public bool AllowAutoRedirect { get; set; }

        /// <summary>
        /// Возвращает или задает максимальное количество последовательных переадресаций.
        /// </summary>
        /// <value>Значение по умолчанию - 5.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра меньше 1.</exception>
        public int MaximumAutomaticRedirections
        {
            get
            {
                return _maximumAutomaticRedirections;
            }
            set
            {
                #region Проверка параметра

                if (value < 1)
                {
                    throw ExceptionHelper.CanNotBeLess("MaximumAutomaticRedirections", 1);
                }

                #endregion

                _maximumAutomaticRedirections = value;
            }
        }

        /// <summary>
        /// Возвращает или задаёт время ожидания в миллисекундах при подключении к HTTP-серверу.
        /// </summary>
        /// <value>Значение по умолчанию - 60.000, что равняется одной минуте.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра меньше 1.</exception>
        public int ConnectTimeout
        {
            get
            {
                return _connectTimeout;
            }
            set
            {
                #region Проверка параметра

                if (value < 1)
                {
                    throw ExceptionHelper.CanNotBeLess("ConnectTimeout", 1);
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
        public int ReadWriteTimeout
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
                    throw ExceptionHelper.CanNotBeLess("ConnectTimeout", 0);
                }

                #endregion
                
                _readWriteTimeout = value;
            }
        }

        #endregion

        #region HTTP-заголовки

        /// <summary>
        /// Возвращает или задаёт кодировку, применяемую для преобразования исходящих и входящих данных.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        /// <remarks>Если кодировка установлена, то дополнительно отправляется заголовок 'Accept-Charset' с названием этой кодировки. Кодировка ответа определяется автоматически, но, если её не удастся определить, то будет использовано значение данного свойства. Если значение данного свойства не задано, то будет использовано значение <see cref="System.Text.Encoding.Default"/>.</remarks>
        public Encoding CharacterSet { get; set; }

        /// <summary>
        /// Возвращает или задает значение, указывающее, необходимо ли устанавливать постоянное подключение к интернет-ресурсу.
        /// </summary>
        /// <value>Значение по умолчанию - <see langword="true"/>.</value>
        /// <remarks>Если значение равно <see langword="true"/>, то дополнительно отправляется заголовок 'Connection: Keep-Alive', иначе отправляется заголовок 'Connection: Close'. Если для подключения используется HTTP-прокси, то за место заголовка - 'Connection', устанавливается заголовок - 'Proxy-Connection'.</remarks>
        public bool KeepAlive { get; set; }

        /// <summary>
        /// Возвращает или задает значение, указывающее, нужно ли кодировать содержимое ответа. Это используется, прежде всего, для сжатия данных.
        /// </summary>
        /// <value>Значение по умолчанию - <see langword="true"/>.</value>
        /// <remarks>Если значение равно <see langword="true"/>, то дополнительно отправляется заголовок 'Accept-Encoding: gzip, deflate'.</remarks>
        public bool EnableEncodingContent { get; set; }

        /// <summary>
        /// Возвращает или задаёт имя пользователя для авторизации на HTTP-сервере.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        /// <remarks>Если значение установлено, то дополнительно отправляется заголовок 'Authorization'.</remarks>
        public string Username { get; set; }

        /// <summary>
        /// Возвращает или задаёт пароль для авторизации на HTTP-сервере.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        /// <remarks>Если значение установлено, то дополнительно отправляется заголовок 'Authorization'.</remarks>
        public string Password { get; set; }

        /// <summary>
        /// Возвращает или задает значение HTTP-заголовка 'Referer'.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        public string Referer { get; set; }

        /// <summary>
        /// Возвращает или задает значение HTTP-заголовка 'User-Agent'.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        public string UserAgent { get; set; }

        /// <summary>
        /// Возвращает или задает значение HTTP-заголовка 'Content-Type'.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        public string ContentType { get; set; }

        /// <summary>
        /// Возвращает или задает кукисы, связанные с запросом.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        /// <remarks>Кукиксы могут изменяться ответом от HTTP-сервера. Чтобы не допустить этого, нужно установить свойство <see cref="xNet.Net.CookieDictionary.IsLocked"/> равным <see langword="true"/>.</remarks>
        public CookieDictionary Cookies { get; set; }

        #endregion

        #endregion


        #region Свойства (внутренние)

        internal TcpClient TcpClient
        {
            get
            {
                return _tcpClient;
            }
        }

        internal Stream ClientStream
        {
            get
            {
                return _clientStream;
            }
        }

        internal NetworkStream ClientNetworkStream
        {
            get
            {
                return _clientNetworkStream;
            }
        }

        #endregion


        /// <summary>
        /// Возвращает или задаёт значение HTTP-заголовка.
        /// </summary>
        /// <param name="headerName">Название заголовка.</param>
        /// <value>Значение заголовка, если такой заголок задан, иначе пустая строка. Если задать значение <see langword="null"/> или пустую строку, то заголовок будет удалён из списка.</value>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="headerName"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="headerName"/> является пустой строкой.</exception>
        /// <exception cref="System.ArgumentException">Получение, либо установка значения заголовка, который должен задаваться с помощью специального свойства.</exception>
        /// <remarks>Список заголовков, которые должны задаваться только с помощью специальных свойств:
        /// <list type="table">
        ///     <item>
        ///        <description>Accept-Charset</description>
        ///     </item>
        ///     <item>
        ///        <description>Accept-Encoding</description>
        ///     </item>
        ///     <item>
        ///        <description>Authorization</description>
        ///     </item>
        ///     <item>
        ///        <description>Content-Type</description>
        ///     </item>
        ///     <item>
        ///        <description>Content-Length</description>
        ///     </item>
        ///     <item>
        ///        <description>Cookie</description>
        ///     </item>
        ///     <item>
        ///        <description>Connection</description>
        ///     </item>
        ///     <item>
        ///        <description>Proxy-Connection</description>
        ///     </item>
        ///     <item>
        ///        <description>Referer</description>
        ///     </item>
        ///     <item>
        ///        <description>User-Agent</description>
        ///     </item>
        ///     <item>
        ///        <description>Host</description>
        ///     </item>
        /// </list>
        /// </remarks>
        public string this[string headerName]
        {
            get
            {
                #region Проверка параметра

                if (headerName == null)
                {
                    throw new ArgumentNullException("headerName");
                }

                if (headerName.Length == 0)
                {
                    throw ExceptionHelper.EmptyString("headerName");
                }

                if (CheckHeader(headerName))
                {
                    throw new ArgumentException(string.Format(
                        Resources.ArgumentException_HttpRequest_GetNotAvailableHeader, headerName), "headerName");
                }

                #endregion

                if (_headers.ContainsKey(headerName))
                {
                    return _headers[headerName];
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                #region Проверка параметра

                if (headerName == null)
                {
                    throw new ArgumentNullException("headerName");
                }

                if (headerName.Length == 0)
                {
                    throw ExceptionHelper.EmptyString("headerName");
                }

                if (CheckHeader(headerName))
                {
                    throw new ArgumentException(string.Format(
                        Resources.ArgumentException_HttpRequest_SetNotAvailableHeader, headerName), "headerName");
                }

                #endregion

                if (string.IsNullOrEmpty(value))
                {
                    _headers.Remove(headerName);
                }
                else
                {
                    _headers[headerName] = value;
                }
            }
        }


        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="HttpRequest"/>.
        /// </summary>
        public HttpRequest()
        {
            KeepAlive = true;
            AllowAutoRedirect = true;
            EnableAutoUrlEncode = true;
            EnableEncodingContent = true;

            Address = new Uri("/", UriKind.Relative);
        }


        #region Методы (открытые)

        #region Get

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="data">Параметры запроса, в виде объекта или структуры, отправляемые HTTP-серверу.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <typeparam name="TData">Тип данных значения, которое представляет параметры запроса. Для данного типа должен быть задан атрибут <see cref="xNet.Net.HttpDataAttribute"/>, а для его полей и свойств, которые представляют параметры запроса, должен быть задан атрибут <see cref="xNet.Net.HttpDataMemberAttribute"/>.</typeparam>
        /// <exception cref="System.ArgumentException">Для типа <typeparamref name="TData"/> не задан обязательный атрибут <see cref="xNet.Net.HttpDataAttribute"/>.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="data"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public HttpResponse Get<TData>(string address, TData data)
        {
            #region Проверка параметров

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            if (address.Length == 0)
            {
                throw ExceptionHelper.EmptyString("address");
            }

            #endregion

            if (!address.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                address = address.Insert(0, "http://");
            }

            return Get(new Uri(address, UriKind.RelativeOrAbsolute), data);
        }

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="data">Параметры запроса, в виде объекта или структуры, отправляемые HTTP-серверу.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <typeparam name="TData">Тип данных значения, которое представляет параметры запроса. Для данного типа должен быть задан атрибут <see cref="xNet.Net.HttpDataAttribute"/>, а для его полей и свойств, которые представляют параметры запроса, должен быть задан атрибут <see cref="xNet.Net.HttpDataMemberAttribute"/>.</typeparam>
        /// <exception cref="System.ArgumentException">Для типа <typeparamref name="TData"/> не задан обязательный атрибут <see cref="xNet.Net.HttpDataAttribute"/>.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="data"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public HttpResponse Get<TData>(Uri address, TData data)
        {
            #region Проверка параметров

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            if (!address.IsAbsoluteUri)
            {
                throw new ArgumentException(Resources.ArgumentException_OnlyAbsoluteUri, "address");
            }

            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            #endregion

            _multipartData = null;
            _messageBodyLength = 0;
            _redirectionsCount = 0;

            var uriBuilder = new UriBuilder(address);
            uriBuilder.Query = ToQueryString<TData>(data);

            address = uriBuilder.Uri;

            return SendRequest(HttpMethod.GET, address, null);
        }

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="reqParams">Параметры запроса, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public HttpResponse Get(string address, StringDictionary reqParams = null)
        {
            #region Проверка параметров

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            if (address.Length == 0)
            {
                throw ExceptionHelper.EmptyString("address");
            }

            #endregion

            if (!address.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                address = address.Insert(0, "http://");
            }

            return Get(new Uri(address, UriKind.RelativeOrAbsolute), reqParams);
        }

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="reqParams">Параметры запроса, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public HttpResponse Get(Uri address, StringDictionary reqParams = null)
        {
            #region Проверка параметров

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            if (!address.IsAbsoluteUri)
            {
                throw new ArgumentException(Resources.ArgumentException_OnlyAbsoluteUri, "address");
            }

            #endregion

            _multipartData = null;
            _messageBodyLength = 0;
            _redirectionsCount = 0;

            if (reqParams != null)
            {
                var uriBuilder = new UriBuilder(address);
                uriBuilder.Query = ToQueryString(reqParams, false);

                address = uriBuilder.Uri;
            }

            return SendRequest(HttpMethod.GET, address, null);
        }

        #endregion

        #region Head

        /// <summary>
        /// Отправляет HEAD-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="data">Параметры запроса, в виде объекта или структуры, отправляемые HTTP-серверу.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <typeparam name="TData">Тип данных значения, которое представляет параметры запроса. Для данного типа должен быть задан атрибут <see cref="xNet.Net.HttpDataAttribute"/>, а для его полей и свойств, которые представляют параметры запроса, должен быть задан атрибут <see cref="xNet.Net.HttpDataMemberAttribute"/>.</typeparam>
        /// <exception cref="System.ArgumentException">Для типа <typeparamref name="TData"/> не задан обязательный атрибут <see cref="xNet.Net.HttpDataAttribute"/>.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="data"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public HttpResponse Head<TData>(string address, TData data)
        {
            #region Проверка параметров

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            if (address.Length == 0)
            {
                throw ExceptionHelper.EmptyString("address");
            }

            #endregion

            if (!address.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                address = address.Insert(0, "http://");
            }

            return Head(new Uri(address, UriKind.RelativeOrAbsolute), data);
        }

        /// <summary>
        /// Отправляет HEAD-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="data">Параметры запроса, в виде объекта или структуры, отправляемые HTTP-серверу.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <typeparam name="TData">Тип данных значения, которое представляет параметры запроса. Для данного типа должен быть задан атрибут <see cref="xNet.Net.HttpDataAttribute"/>, а для его полей и свойств, которые представляют параметры запроса, должен быть задан атрибут <see cref="xNet.Net.HttpDataMemberAttribute"/>.</typeparam>
        /// <exception cref="System.ArgumentException">Для типа <typeparamref name="TData"/> не задан обязательный атрибут <see cref="xNet.Net.HttpDataAttribute"/>.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="data"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public HttpResponse Head<TData>(Uri address, TData data)
        {
            #region Проверка параметров

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            if (!address.IsAbsoluteUri)
            {
                throw new ArgumentException(Resources.ArgumentException_OnlyAbsoluteUri, "address");
            }

            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            #endregion

            _multipartData = null;
            _messageBodyLength = 0;
            _redirectionsCount = 0;

            var uriBuilder = new UriBuilder(address);
            uriBuilder.Query = ToQueryString<TData>(data);

            address = uriBuilder.Uri;

            return SendRequest(HttpMethod.HEAD, address, null);
        }

        /// <summary>
        /// Отправляет HEAD-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="reqParams">Параметры запроса, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public HttpResponse Head(string address, StringDictionary reqParams = null)
        {
            #region Проверка параметров

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            if (address.Length == 0)
            {
                throw ExceptionHelper.EmptyString("address");
            }

            #endregion

            if (!address.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                address = address.Insert(0, "http://");
            }

            return Head(new Uri(address, UriKind.RelativeOrAbsolute), reqParams);
        }

        /// <summary>
        /// Отправляет HEAD-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="reqParams">Параметры запроса, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public HttpResponse Head(Uri address, StringDictionary reqParams = null)
        {
            #region Проверка параметров

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            if (!address.IsAbsoluteUri)
            {
                throw new ArgumentException(Resources.ArgumentException_OnlyAbsoluteUri, "address");
            }

            #endregion

            _multipartData = null;
            _messageBodyLength = 0;
            _redirectionsCount = 0;

            if (reqParams != null)
            {
                var uriBuilder = new UriBuilder(address);
                uriBuilder.Query = ToQueryString(reqParams, false);

                address = uriBuilder.Uri;
            }

            return SendRequest(HttpMethod.HEAD, address, null);
        }

        #endregion

        #region Post

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="data">Параметры запроса, в виде объекта или структуры, отправляемые HTTP-серверу.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <typeparam name="TData">Тип данных значения, которое представляет параметры запроса. Для данного типа должен быть задан атрибут <see cref="xNet.Net.HttpDataAttribute"/>, а для его полей и свойств, которые представляют параметры запроса, должен быть задан атрибут <see cref="xNet.Net.HttpDataMemberAttribute"/>.</typeparam>
        /// <exception cref="System.ArgumentException">Для типа <typeparamref name="TData"/> не задан обязательный атрибут <see cref="xNet.Net.HttpDataAttribute"/>.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="data"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        /// <remarks>Если значение заголовка 'Content-Type' не задано, то отправляется значение 'Content-Type: application/x-www-form-urlencoded'.</remarks>
        public HttpResponse Post<TData>(string address, TData data)
        {
            #region Проверка параметров

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            if (address.Length == 0)
            {
                throw ExceptionHelper.EmptyString("address");
            }

            #endregion

            if (!address.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                address = address.Insert(0, "http://");
            }

            return Post(new Uri(address, UriKind.RelativeOrAbsolute), data);
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="data">Параметры запроса, в виде объекта или структуры, отправляемые HTTP-серверу.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <typeparam name="TData">Тип данных значения, которое представляет параметры запроса. Для данного типа должен быть задан атрибут <see cref="xNet.Net.HttpDataAttribute"/>, а для его полей и свойств, которые представляют параметры запроса, должен быть задан атрибут <see cref="xNet.Net.HttpDataMemberAttribute"/>.</typeparam>
        /// <exception cref="System.ArgumentException">Для типа <typeparamref name="TData"/> не задан обязательный атрибут <see cref="xNet.Net.HttpDataAttribute"/>.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="data"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        /// <remarks>Если значение заголовка 'Content-Type' не задано, то отправляется значение 'Content-Type: application/x-www-form-urlencoded'.</remarks>
        public HttpResponse Post<TData>(Uri address, TData data)
        {
            #region Проверка параметров

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            if (!address.IsAbsoluteUri)
            {
                throw new ArgumentException(Resources.ArgumentException_OnlyAbsoluteUri, "address");
            }

            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            #endregion

            _multipartData = null;
            _redirectionsCount = 0;

            byte[] buffer = Encoding.ASCII.GetBytes(ToQueryString<TData>(data));
            _messageBodyLength = buffer.Length;

            return SendRequest(HttpMethod.POST, address, buffer);
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="reqParams">Параметры запроса, отправляемые HTTP-серверу.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="reqParams"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        /// <remarks>Если значение заголовка 'Content-Type' не задано, то отправляется значение 'Content-Type: application/x-www-form-urlencoded'.</remarks>
        public HttpResponse Post(string address, StringDictionary reqParams)
        {
            #region Проверка параметров

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            if (address.Length == 0)
            {
                throw ExceptionHelper.EmptyString("address");
            }

            #endregion

            if (!address.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                address = address.Insert(0, "http://");
            }

            return Post(new Uri(address, UriKind.RelativeOrAbsolute), reqParams);
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="reqParams">Параметры запроса, отправляемые HTTP-серверу.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="reqParams"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        /// <remarks>Если значение заголовка 'Content-Type' не задано, то отправляется значение 'Content-Type: application/x-www-form-urlencoded'.</remarks>
        public HttpResponse Post(Uri address, StringDictionary reqParams)
        {
            #region Проверка параметров

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            if (!address.IsAbsoluteUri)
            {
                throw new ArgumentException(Resources.ArgumentException_OnlyAbsoluteUri, "address");
            }

            if (reqParams == null)
            {
                throw new ArgumentNullException("reqParams");
            }

            #endregion

            _multipartData = null;
            _redirectionsCount = 0;

            byte[] buffer = Encoding.ASCII.GetBytes(ToQueryString(reqParams, EnableAutoUrlEncode));
            _messageBodyLength = buffer.Length;

            return SendRequest(HttpMethod.POST, address, buffer);
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="messageBody">Тело сообщения, отправляемое HTTP-серверу.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="messageBody"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="messageBody"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        /// <remarks>Если значение заголовка 'Content-Type' не задано, то отправляется значение 'Content-Type: application/x-www-form-urlencoded'.</remarks>
        public HttpResponse Post(string address, string messageBody)
        {
            #region Проверка параметров

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            if (address.Length == 0)
            {
                throw ExceptionHelper.EmptyString("address");
            }

            #endregion

            if (!address.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                address = address.Insert(0, "http://");
            }

            return Post(new Uri(address, UriKind.RelativeOrAbsolute), messageBody);
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="messageBody">Тело сообщения, отправляемое HTTP-серверу.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="messageBody"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="messageBody"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        /// <remarks>Если значение заголовка 'Content-Type' не задано, то отправляется значение 'Content-Type: application/x-www-form-urlencoded'.</remarks>
        public HttpResponse Post(Uri address, string messageBody)
        {
            #region Проверка параметров

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            if (!address.IsAbsoluteUri)
            {
                throw new ArgumentException(Resources.ArgumentException_OnlyAbsoluteUri, "address");
            }

            if (messageBody == null)
            {
                throw new ArgumentNullException("messageBody");
            }

            if (messageBody.Length == 0)
            {
                throw ExceptionHelper.EmptyString("messageBody");
            }

            #endregion

            _multipartData = null;
            _redirectionsCount = 0;

            Encoding encoding = CharacterSet ?? Encoding.Default;

            byte[] buffer = encoding.GetBytes(messageBody ?? string.Empty);
            _messageBodyLength = buffer.Length;

            return SendRequest(HttpMethod.POST, address, buffer);
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="messageBody">Тело сообщения, отправляемое HTTP-серверу.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="messageBody"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        /// <remarks>Если значение заголовка 'Content-Type' не задано, то отправляется значение 'Content-Type: application/x-www-form-urlencoded'.</remarks>
        public HttpResponse Post(string address, byte[] messageBody)
        {
            #region Проверка параметров

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            if (address.Length == 0)
            {
                throw ExceptionHelper.EmptyString("address");
            }

            #endregion

            if (!address.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                address = address.Insert(0, "http://");
            }

            return Post(new Uri(address, UriKind.RelativeOrAbsolute), messageBody);
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="messageBody">Тело сообщения, отправляемое HTTP-серверу.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="messageBody"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        /// <remarks>Если значение заголовка 'Content-Type' не задано, то отправляется значение 'Content-Type: application/x-www-form-urlencoded'.</remarks>
        public HttpResponse Post(Uri address, byte[] messageBody)
        {
            #region Проверка параметров

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            if (!address.IsAbsoluteUri)
            {
                throw new ArgumentException(Resources.ArgumentException_OnlyAbsoluteUri, "address");
            }

            if (messageBody == null)
            {
                throw new ArgumentNullException("messageBody");
            }

            #endregion

            _multipartData = null;
            _redirectionsCount = 0;
            _messageBodyLength = messageBody.Length;

            return SendRequest(HttpMethod.POST, address, messageBody);
        }

        #region MultipartFormData

        /// <summary>
        /// Отправляет POST-запрос с Multipart/form данными.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="multipartData">Multipart/form данные, отправляемые HTTP-серверу.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="multipartData"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public HttpResponse Post(string address, MultipartDataCollection multipartData)
        {
            #region Проверка параметров

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            if (address.Length == 0)
            {
                throw ExceptionHelper.EmptyString("address");
            }

            #endregion

            if (!address.StartsWith("http"))
            {
                address = address.Insert(0, "http://");
            }

            return Post(new Uri(address, UriKind.RelativeOrAbsolute), multipartData);
        }

        /// <summary>
        /// Отправляет POST-запрос с Multipart/form данными.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="multipartData">Multipart/form данные, отправляемые HTTP-серверу.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="multipartData"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public HttpResponse Post(Uri address, MultipartDataCollection multipartData)
        {
            #region Проверка параметров

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            if (!address.IsAbsoluteUri)
            {
                throw new ArgumentException(Resources.ArgumentException_OnlyAbsoluteUri, "address");
            }

            if (multipartData == null)
            {
                throw new ArgumentNullException("multipartData");
            }

            #endregion

            _redirectionsCount = 0;
            _multipartData = multipartData;

            _messageBodyLength = multipartData.
                CalculateLength(CharacterSet ?? Encoding.Default);

            return SendRequest(HttpMethod.POST, address, null);
        }

        #endregion

        #endregion

        /// <summary>
        /// Закрывает соединение с HTTP-сервером.
        /// </summary>
        /// <remarks>Вызов данного метода равносилен вызову метода <see cref="Dispose"/>.</remarks>
        public void Close()
        {
            Dispose();
        }

        /// <summary>
        /// Освобождает все ресурсы, используемые текущим экземпляром класса <see cref="HttpRequest"/>.
        /// </summary>
        public virtual void Dispose()
        {
            if (_tcpClient != null)
            {
                _tcpClient.Close();
                _tcpClient = null;
                _clientStream = null;
                _clientNetworkStream = null;
            }
        }

        /// <summary>
        /// Очищает все HTTP-заголовки, кроме тех, которые устанавливаются через специальные свойства.
        /// </summary>
        public void ClearAllHeaders()
        {
            _headers.Clear();
        }

        #endregion


        internal void ReportBytesReceived(
            int bytesReceived, int totalBytesToReceive)
        {
            if (_downloadProgressChangedHandler != null)
            {
                _bytesReceived += bytesReceived;

                OnDownloadProgressChanged(
                    new DownloadProgressChangedEventArgs(_bytesReceived, totalBytesToReceive));
            }
        }


        #region Методы (защищённые)

        /// <summary>
        /// Вызывает событие <see cref="UploadProgressChanged"/>.
        /// </summary>
        /// <param name="e">Аргументы события.</param>
        protected virtual void OnUploadProgressChanged(UploadProgressChangedEventArgs e)
        {
            EventHandler<UploadProgressChangedEventArgs> eventHandler = _uploadProgressChangedHandler;

            if (eventHandler != null)
            {
                eventHandler(this, e);
            }
        }

        /// <summary>
        /// Вызывает событие <see cref="DownloadProgressChanged"/>.
        /// </summary>
        /// <param name="e">Аргументы события.</param>
        protected virtual void OnDownloadProgressChanged(DownloadProgressChangedEventArgs e)
        {
            EventHandler<DownloadProgressChangedEventArgs> eventHandler = _downloadProgressChangedHandler;

            if (eventHandler != null)
            {
                eventHandler(this, e);
            }
        }

        #endregion


        #region Методы (закрытые)

        private HttpResponse SendRequest(HttpMethod method, Uri address,
            byte[] messageBody, bool reconnection = false)
        {
            bool createdNewConnection = false;

            // Если это первый запрос данного экземпляра класса.
            if (_response == null)
            {
                _response = new HttpResponse(this);
            }
            else if (_tcpClient != null &&
                !_response.MessageBodyLoaded && !_response.HasError)
            {
                try
                {
                    _response.None();
                }
                catch (HttpException)
                {
                    Dispose();
                }
            }

            // Если нужно создать новое подключение.
            if (_tcpClient == null || Address.Port != address.Port ||
                !Address.Host.Equals(address.Host, StringComparison.OrdinalIgnoreCase) ||
                !Address.Scheme.Equals(address.Scheme, StringComparison.OrdinalIgnoreCase) ||
                _response.HasError)
            {
                Address = address;

                Dispose();
                CreateConnection();

                createdNewConnection = true;
            }
            else
            {
                Address = address;
            }

            #region Отправка запроса

            try
            {
                // Отправляем начальную линию.
                byte[] buffer = Encoding.ASCII.GetBytes(GenerateStartingLine(method));
                _clientStream.Write(buffer, 0, buffer.Length);

                // Отправляем заголовки.
                buffer = Encoding.ASCII.GetBytes(GenerateHeaders(method, _messageBodyLength));
                _clientStream.Write(buffer, 0, buffer.Length);

                // Отправляем тело сообщения, если оно не пустое.
                if (_messageBodyLength != 0)
                {
                    _bytesSent = 0;

                    if (messageBody == null)
                    {
                        _multipartData.SendBytes(SendBytes, CharacterSet ?? Encoding.Default);
                    }
                    else
                    {
                        SendBytes(messageBody, _messageBodyLength);
                    }
                }
            }
            catch (SecurityException ex)
            {
                throw NewHttpException(Resources.HttpException_FailedSendRequest, ex);
            }
            catch (IOException ex)
            {
                // Если нужно, делаем переподключение.
                if (!createdNewConnection && KeepAlive && !reconnection)
                {
                    Dispose();
                    return SendRequest(method, address, messageBody, true);
                }
                else
                {
                    throw NewHttpException(Resources.HttpException_FailedSendRequest, ex);
                }
            }

            #endregion

            #region Загрузка ответа

            try
            {
                _bytesReceived = 0;
                _response.LoadResponse(method);
            }
            catch (HttpException ex)
            {
                // Если нужно, делаем переподключение.
                if (!createdNewConnection && KeepAlive && !reconnection && ex.EmptyMessageBody)
                {
                    Dispose();
                    return SendRequest(method, address, messageBody, true);
                }
                else
                {
                    throw;
                }
            }

            #endregion

            #region Проверка кода ответа

            int statusCodeNum = (int)_response.StatusCode;

            if (statusCodeNum >= 400 && statusCodeNum < 500)
            {
                throw new HttpException(string.Format(
                    Resources.HttpException_ClientError, statusCodeNum), _response.StatusCode);
            }
            else if (statusCodeNum >= 500)
            {
                throw new HttpException(string.Format(
                    Resources.HttpException_SeverError, statusCodeNum), _response.StatusCode);
            }

            #endregion

            #region Переадресация

            if (AllowAutoRedirect && _response.HasRedirect)
            {
                if (++_redirectionsCount > _maximumAutomaticRedirections)
                {
                    throw NewHttpException(Resources.HttpException_LimitRedirections);
                }

                Uri redirectAddress;

                if (_response.Location.StartsWith("http", StringComparison.Ordinal))
                {
                    redirectAddress = new Uri(_response.Location);
                }
                else
                {
                    string newAddress;

                    if (address.IsDefaultPort)
                    {
                        newAddress = string.Format(
                            "http://{0}{1}", address.Host, _response.Location);
                    }
                    else
                    {
                        newAddress = string.Format(
                            "http://{0}:{1}{2}", address.Host, address.Port, _response.Location);
                    }

                    redirectAddress = new Uri(newAddress);
                }

                _multipartData = null;
                _messageBodyLength = 0;

                return SendRequest(HttpMethod.GET, redirectAddress, null);
            }

            #endregion

            return _response;
        }

        #region Создание подключения

        private void CreateConnection()
        {
            _tcpClient = CreateTcpConnection(Address.Host, Address.Port);
            _clientNetworkStream = _tcpClient.GetStream();
            _sendBufferSize = _tcpClient.SendBufferSize;

            // Если требуется безопасное соединение.
            if (Address.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    SslStream sslStream;

                    if (SslCertificateValidatorCallback == null)
                    {
                        sslStream = new SslStream(_clientNetworkStream, false);
                    }
                    else
                    {
                        sslStream = new SslStream(_clientNetworkStream, false, SslCertificateValidatorCallback);
                    }

                    sslStream.AuthenticateAsClient(Address.Host);

                    _clientStream = sslStream;
                }
                catch (Exception ex)
                {
                    if (ex is IOException || ex is System.Security.Authentication.AuthenticationException)
                    {
                        throw NewHttpException(Resources.HttpException_FailedSslConnect, ex);
                    }

                    throw;
                }
            }
            else
            {
                _clientStream = _clientNetworkStream;
            }
        }

        private TcpClient CreateTcpConnection(string host, int port)
        {
            TcpClient tcpClient;
            _currentProxy = GetProxy();

            if (_currentProxy == null)
            {
                #region Создание подключения

                tcpClient = new TcpClient();
                Exception connectException = null;
                var connectDoneEvent = new ManualResetEventSlim();

                try
                {
                    tcpClient.BeginConnect(host, port, new AsyncCallback(
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
                        throw NewHttpException(Resources.HttpException_FailedConnect, ex);
                    }

                    throw;
                }

                #endregion

                if (!connectDoneEvent.Wait(_connectTimeout))
                {
                    tcpClient.Close();
                    throw NewHttpException(Resources.HttpException_ConnectTimeout);
                }

                if (connectException != null)
                {
                    tcpClient.Close();

                    if (connectException is SocketException)
                    {
                        throw NewHttpException(Resources.HttpException_FailedConnect, connectException);
                    }
                    else
                    {
                        throw connectException;
                    }
                }

                if (!tcpClient.Connected)
                {
                    tcpClient.Close();
                    throw NewHttpException(Resources.HttpException_FailedConnect);
                }

                #endregion

                tcpClient.SendTimeout = _readWriteTimeout;
                tcpClient.ReceiveTimeout = _readWriteTimeout;
            }
            else
            {
                tcpClient = _currentProxy.CreateConnection(host, port);
            }

            return tcpClient;
        }

        private ProxyClient GetProxy()
        {
            if (DisableProxyForLocalAddress)
            {
                try
                {
                    IPAddress checkIp = IPAddress.Parse("127.0.0.1");
                    IPAddress[] ips = Dns.GetHostAddresses(Address.Host);

                    foreach (IPAddress ip in ips)
                    {
                        if (ip.Equals(checkIp))
                        {
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex is SocketException || ex is ArgumentException)
                    {
                        throw NewHttpException(
                            Resources.HttpException_FailedGetHostAddresses, ex);
                    }

                    throw;
                }
            }

            ProxyClient proxy = Proxy ?? GlobalProxy;

            if (proxy == null && UseIeProxy && !WinInet.InternetConnected)
            {
                proxy = WinInet.IEProxy;
            }

            return proxy;
        }

        #endregion

        #region Формирование данных запроса

        private string GenerateStartingLine(HttpMethod method)
        {
            string query;

            if (_currentProxy != null && _currentProxy.ProxyType == ProxyType.Http)
            {
                query = Address.AbsoluteUri;
            }
            else
            {
                query = Address.PathAndQuery;
            }

            return string.Format("{0} {1} HTTP/{2}\r\n",
                method, query, ProtocolVersion);
        }

        private string GenerateHeaders(HttpMethod method, int messageBodyLength)
        {
            var headersBuilder = new StringBuilder();

            if (Address.IsDefaultPort)
            {
                headersBuilder.AppendFormat("Host: {0}\r\n", Address.Host);
            }
            else
            {
                headersBuilder.AppendFormat("Host: {0}:{1}\r\n", Address.Host, Address.Port);
            }

            #region Первая часть

            if (_currentProxy != null && _currentProxy.ProxyType == ProxyType.Http)
            {
                if (KeepAlive)
                {
                    headersBuilder.Append("Proxy-Connection: keep-alive\r\n");
                }
                else
                {
                    headersBuilder.Append("Proxy-Connection: close\r\n");
                }

                HttpProxyClient httpProxy = _currentProxy as HttpProxyClient;
                headersBuilder.Append(httpProxy.GenerateAuthorizationHeader());
            }
            else
            {
                if (KeepAlive)
                {
                    headersBuilder.Append("Connection: keep-alive\r\n");
                }
                else
                {
                    headersBuilder.Append("Connection: close\r\n");
                }
            }

            headersBuilder.Append(GenerateAuthorizationHeader());

            #endregion

            #region Вторая часть

            if (CharacterSet != null)
            {
                headersBuilder.AppendFormat("Accept-Charset: {0}\r\n", CharacterSet.WebName);
            }

            if (EnableEncodingContent)
            {
                headersBuilder.AppendFormat("Accept-Encoding: gzip, deflate\r\n");
            }

            if (!string.IsNullOrEmpty(Referer))
            {
                headersBuilder.AppendFormat("Referer: {0}\r\n", Referer);
            }

            if (!string.IsNullOrEmpty(UserAgent))
            {
                headersBuilder.AppendFormat("User-Agent: {0}\r\n", UserAgent);
            }
            else if (!string.IsNullOrEmpty(GlobalUserAgent))
            {
                headersBuilder.AppendFormat("User-Agent: {0}\r\n", GlobalUserAgent);
            }

            if (_multipartData != null)
            {
                headersBuilder.AppendFormat("Content-Type: {0}\r\n", _multipartData.GenerateContentType());
            }
            else if (!string.IsNullOrEmpty(ContentType))
            {
                headersBuilder.AppendFormat("Content-Type: {0}\r\n", ContentType);
            }
            else if (method == HttpMethod.POST)
            {
                headersBuilder.AppendFormat("Content-Type: application/x-www-form-urlencoded\r\n");
            }

            if (messageBodyLength > 0)
            {
                headersBuilder.AppendFormat("Content-Length: {0}\r\n", messageBodyLength);
            }

            #endregion

            headersBuilder.Append(ToHeadersString(_headers));

            if (Cookies != null && Cookies.Count != 0)
            {
                headersBuilder.AppendFormat("Cookie: {0}\r\n", Cookies.ToString());
            }

            headersBuilder.AppendLine();

            return headersBuilder.ToString();
        }

        private string GenerateAuthorizationHeader()
        {
            if (!string.IsNullOrEmpty(Username) || !string.IsNullOrEmpty(Password))
            {
                string data = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                    string.Format("{0}:{1}", Username, Password)));

                return string.Format("Authorization: Basic {0}\r\n", data);
            }

            return string.Empty;
        }

        #endregion

        private void SendBytes(byte[] bytes, int length)
        {
            if (_uploadProgressChangedHandler == null)
            {
                _clientStream.Write(bytes, 0, length);
            }
            else
            {
                int index = 0;

                while (length > 0)
                {
                    if (length >= _sendBufferSize)
                    {
                        _bytesSent += _sendBufferSize;
                        _clientStream.Write(bytes, index, _sendBufferSize);

                        index += _sendBufferSize;
                        length -= _sendBufferSize;
                    }
                    else
                    {
                        _bytesSent += length;
                        _clientStream.Write(bytes, index, length);

                        length = 0;
                    }

                    OnUploadProgressChanged(
                        new UploadProgressChangedEventArgs(_bytesSent, _messageBodyLength));
                }
            }
        }

        private bool CheckHeader(string headerName)
        {
            if (headerName.Equals("Accept-Charset", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (headerName.Equals("Accept-Encoding", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (headerName.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (headerName.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (headerName.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (headerName.Equals("Cookie", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            
            if (headerName.Equals("Connection", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            
            if (headerName.Equals("Proxy-Connection", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            
            if (headerName.Equals("Referer", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (headerName.Equals("User-Agent", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (headerName.Equals("Host", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }        

            return false;
        }

        private string ToHeadersString(StringDictionary headers)
        {
            var headersBuilder = new StringBuilder();

            foreach (KeyValuePair<string, string> header in headers)
            {
                headersBuilder.AppendFormat("{0}: {1}\r\n", header.Key, header.Value);
            }

            return headersBuilder.ToString();
        }

        private string ToQueryString<T>(T obj)
        {
            Type objType = obj.GetType();

            if (!objType.IsDefined(typeof(HttpDataAttribute), false))
            {
                throw NewHttpException(string.Format(
                    Resources.ArgumentException_NotContainAttribute, "HttpDataAttribute"));
            }

            var queryBuilder = new StringBuilder();
            Type dataMemberType = typeof(HttpDataMemberAttribute);

            foreach (var field in objType.GetFields())
            {
                var dataMember = Attribute.GetCustomAttribute(
                    field, dataMemberType) as HttpDataMemberAttribute;

                if (dataMember == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(dataMember.Name))
                {
                    queryBuilder.Append(dataMember.Name);
                }
                else
                {
                    queryBuilder.Append(field.Name);
                }

                queryBuilder.Append('=');
                queryBuilder.Append(field.GetValue(obj).ToString());
                queryBuilder.Append('&');
            }

            foreach (var property in objType.GetProperties())
            {
                var dataMember = Attribute.GetCustomAttribute(
                    property, dataMemberType) as HttpDataMemberAttribute;

                if (dataMember == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(dataMember.Name))
                {
                    queryBuilder.Append(dataMember.Name);
                }
                else
                {
                    queryBuilder.Append(property.Name);
                }

                queryBuilder.Append('=');
                queryBuilder.Append(property.GetValue(obj, null).ToString());
                queryBuilder.Append('&');
            }

            if (queryBuilder.Length != 0)
            {
                queryBuilder.Remove(queryBuilder.Length - 1, 1);
            }

            return queryBuilder.ToString();
        }

        private string ToQueryString(StringDictionary parameters, bool needEscapeData)
        {
            var queryBuilder = new StringBuilder();

            foreach (KeyValuePair<string, string> param in parameters)
            {
                queryBuilder.Append(param.Key);
                queryBuilder.Append('=');

                if (needEscapeData)
                {
                    queryBuilder.Append(Uri.EscapeDataString(param.Value));
                }
                else
                {
                    queryBuilder.Append(param.Value);
                }

                queryBuilder.Append('&');
            }

            if (queryBuilder.Length != 0)
            {
                queryBuilder.Remove(queryBuilder.Length - 1, 1);
            }

            return queryBuilder.ToString();
        }

        private HttpException NewHttpException(string message,
            Exception innerException = null)
        {
            return new HttpException(string.Format(message, Address.Host), innerException);
        }

        #endregion
    }
}