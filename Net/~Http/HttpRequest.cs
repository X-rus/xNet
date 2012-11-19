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
        private int _sendBufferSize;

        private EventHandler<UploadProgressChangedEventArgs> _uploadProgressChangedHandler;
        private EventHandler<DownloadProgressChangedEventArgs> _downloadProgressChangedHandler;

        private HttpContent _content;
        private readonly StringDictionary _headers = new StringDictionary(StringComparer.OrdinalIgnoreCase);

        // Временные данные, которые задаются через специальные методы.
        // Удаляются после первого запроса.
        private StringDictionary _addedParams;
        private StringDictionary _addedHeaders;
        private StringDictionary _addedUrlParams;
        private MultipartDataCollection _addedMultipartData;

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


        #region Свойства (закрытые)

        private MultipartDataCollection AddedMultipartData
        {
            get
            {
                if (_addedMultipartData == null)
                {
                    _addedMultipartData = new MultipartDataCollection();
                }

                return _addedMultipartData;
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
        /// <param name="urlParams">Параметры URL-адреса, или значение <see langword="null"/>.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public HttpResponse Get(string address, StringDictionary urlParams = null)
        {
            return Raw(HttpMethod.GET, address, urlParams);
        }

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="urlParams">Параметры URL-адреса, или значение <see langword="null"/>.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public HttpResponse Get(Uri address, StringDictionary urlParams = null)
        {
            return Raw(HttpMethod.GET, address, urlParams);
        }

        #endregion

        #region Post

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        /// <remarks>Если значение заголовка 'Content-Type' не задано, то отправляется значение 'Content-Type: application/x-www-form-urlencoded'.</remarks>
        public HttpResponse Post(string address)
        {
            return Raw(HttpMethod.POST, address);
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        /// <remarks>Если значение заголовка 'Content-Type' не задано, то отправляется значение 'Content-Type: application/x-www-form-urlencoded'.</remarks>
        public HttpResponse Post(Uri address)
        {
            return Raw(HttpMethod.POST, address);
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
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="data"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        /// <remarks>Если значение заголовка 'Content-Type' не задано, то отправляется значение 'Content-Type: application/x-www-form-urlencoded'.</remarks>
        public HttpResponse Post<TData>(string address, TData data)
        {
            return Raw(HttpMethod.POST, address, null, data);
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
            return Raw(HttpMethod.POST, address, null, data);
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
            return Raw(HttpMethod.POST, address, null, reqParams);
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
            return Raw(HttpMethod.POST, address, null, reqParams);
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
            return Raw(HttpMethod.POST, address, null, messageBody);
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
            return Raw(HttpMethod.POST, address, null, messageBody);
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="messageBodyStream">Тело сообщения в виде потока данных, отправляемое HTTP-серверу.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="messageBodyStream"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        /// <remarks>Если значение заголовка 'Content-Type' не задано, то отправляется значение 'Content-Type: application/x-www-form-urlencoded'.
        /// 
        /// Поток заданный в <paramref name="messageBodyStream"/> освобождается после запроса, либо если произойдёт исключение.</remarks>
        public HttpResponse Post(string address, Stream messageBodyStream)
        {
            return Raw(HttpMethod.POST, address, null, messageBodyStream);
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="messageBodyStream">Тело сообщения в виде потока данных, отправляемое HTTP-серверу.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="messageBodyStream"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        /// <remarks>Если значение заголовка 'Content-Type' не задано, то отправляется значение 'Content-Type: application/x-www-form-urlencoded'.
        /// 
        /// Поток заданный в <paramref name="messageBodyStream"/> освобождается после запроса, либо если произойдёт исключение.</remarks>
        public HttpResponse Post(Uri address, Stream messageBodyStream)
        {
            return Raw(HttpMethod.POST, address, null, messageBodyStream);
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
            return Raw(HttpMethod.POST, address, null, multipartData);
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
            return Raw(HttpMethod.POST, address, null, multipartData);
        }

        #endregion

        #endregion

        #region Raw

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу.
        /// </summary>
        /// <param name="method">HTTP-метод запроса.</param>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="urlParams">Параметры URL-адреса, или значение <see langword="null"/>.</param>
        /// <param name="messageBody">Тело сообщения, отправляемое HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public HttpResponse Raw(HttpMethod method,
            string address, StringDictionary urlParams = null, object messageBody = null)
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

            var uri = new Uri(address, UriKind.RelativeOrAbsolute);

            return Raw(method, uri, urlParams, messageBody);
        }

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу.
        /// </summary>
        /// <param name="method">HTTP-метод запроса.</param>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="urlParams">Параметры URL-адреса, или значение <see langword="null"/>.</param>
        /// <param name="messageBody">Тело сообщения, отправляемое HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <returns>Объект, предназначенный для приёма ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="multipartData"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public HttpResponse Raw(HttpMethod method,
            Uri address, StringDictionary urlParams = null, object messageBody = null)
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

            if (urlParams == null)
            {
                urlParams = _addedUrlParams;
            }

            if (urlParams != null)
            {
                var uriBuilder = new UriBuilder(address);
                uriBuilder.Query = ToQueryString(urlParams, false);

                address = uriBuilder.Uri;
            }

            if (messageBody == null)
            {
                if (_addedParams != null)
                {
                    messageBody = _addedParams;
                }
                else if (_addedMultipartData != null)
                {
                    messageBody = _addedMultipartData;
                }
            }

            HttpContent content = null;

            if (messageBody != null)
            {
                if (messageBody is StringDictionary)
                {
                    content = new StringHttpContent(
                        ToQueryString(messageBody as StringDictionary, EnableAutoUrlEncode));
                }
                else if (messageBody is string)
                {
                    content = new StringHttpContent(messageBody as string);
                }
                else if (messageBody is MultipartDataCollection)
                {
                    content = new MultipartHttpContent(messageBody as MultipartDataCollection);
                }
                else if (messageBody is Stream)
                {
                    content = new StreamHttpContent(messageBody as Stream);
                }
                else if (messageBody is HttpContent)
                {
                    content = messageBody as HttpContent;
                }
                else
                {
                    content = new StringHttpContent(ToQueryString(messageBody));
                }
            }

            try
            {
                return SendRequest(method, address, content);
            }
            finally
            {
                if (_content != null)
                {
                    _content.Dispose();
                }
            }
        } 

        #endregion

        #region Добавление временных данных запроса

        /// <summary>
        /// Добавляет временный параметр запроса.
        /// </summary>
        /// <param name="name">Имя параметра.</param>
        /// <param name="value">Значение параметра.</param>
        /// <remarks>Данный параметр будет стёрт после первого запроса.</remarks>
        public HttpRequest AddParam(string name, string value)
        {
            #region Проверка параметра

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            #endregion

            if (_addedParams == null)
            {
                _addedParams = new StringDictionary();
            }

            _addedParams[name] = value;

            return this;
        }

        /// <summary>
        /// Добавляет временный параметр URL-адреса.
        /// </summary>
        /// <param name="name">Имя параметра.</param>
        /// <param name="value">Значение параметра.</param>
        /// <remarks>Данный параметр будет стёрт после первого запроса.</remarks>
        public HttpRequest AddUrlParam(string name, string value)
        {
            #region Проверка параметра

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            #endregion

            if (_addedUrlParams == null)
            {
                _addedUrlParams = new StringDictionary();
            }

            _addedUrlParams[name] = value;

            return this;
        }

        /// <summary>
        /// Добавляет временный заголовок запроса.
        /// </summary>
        /// <param name="name">Имя заголовка.</param>
        /// <param name="value">Значение заголовка.</param>
        /// <remarks>Данный заголовок будет стёрт после первого запроса.</remarks>
        public HttpRequest AddHeader(string name, string value)
        {
            #region Проверка параметра

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (CheckHeader(name))
            {
                throw new ArgumentException(string.Format(
                    Resources.ArgumentException_HttpRequest_SetNotAvailableHeader, name), "name");
            }

            #endregion

            if (_addedHeaders == null)
            {
                _addedHeaders = new StringDictionary();
            }

            _addedHeaders[name] = value;

            return this;
        }

        /// <summary>
        /// Добавляет временный элемент Multipart/form данных.
        /// </summary>
        /// <param name="name">Имя элемента.</param>
        /// <param name="value">Значение элемента.</param>
        /// <remarks>Данный элемент будет стёрт после первого запроса.</remarks>
        public HttpRequest AddField(string name, string value)
        {
            AddedMultipartData.AddField(name, value);

            return this;
        }

        /// <summary>
        /// Добавляет временный элемент Multipart/form данных.
        /// </summary>
        /// <param name="name">Имя элемента.</param>
        /// <param name="value">Значение элемента.</param>
        /// <remarks>Данный элемент будет стёрт после первого запроса.</remarks>
        public HttpRequest AddField(string name, byte[] value)
        {
            AddedMultipartData.AddField(name, value);

            return this;
        }

        /// <summary>
        /// Добавляет временный элемент Multipart/form данных, представляющий файл.
        /// </summary>
        /// <param name="name">Имя элемента.</param>
        /// <param name="fileName">Имя передаваемого файла.</param>
        /// <param name="contentType">Тип передаваемых данных.</param>
        /// <param name="value">Значение элемента.</param>
        /// <remarks>Данный элемент будет стёрт после первого запроса.</remarks>
        public HttpRequest AddFile(string name, string fileName, string contentType, byte[] value)
        {
            AddedMultipartData.AddFile(name, fileName, contentType, value);

            return this;
        }

        /// <summary>
        /// Добавляет временный элемент Multipart/form данных, представляющий файл.
        /// </summary>
        /// <param name="name">Имя элемента.</param>
        /// <param name="path">Путь к файлу.</param>
        /// <param name="doPreLoading">Указывает, нужно ли делать предварительную загрузку файла.</param>
        /// <param name="contentType">Тип передаваемых данных, или значение <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="path"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="path"/> является пустой строкой, содержит только пробелы или содержит недопустимые символы.</exception>
        /// <exception cref="System.IO.PathTooLongException">Указанный путь, имя файла или и то и другое превышает наибольшую возможную длину, определенную системой. Например, для платформ на основе Windows длина пути не должна превышать 248 знаков, а имена файлов не должны содержать более 260 знаков.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Значение параметра <paramref name="path"/> указывает на несуществующий файл.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">Значение параметра <paramref name="path"/> указывает на недопустимый путь.</exception>
        /// <exception cref="System.IO.IOException">Ошибка ввода-вывода при работе с файлом.</exception>
        /// <exception cref="System.Security.SecurityException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Операция чтения файла не поддерживается на текущей платформе.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Значение параметра <paramref name="path"/> определяет каталог.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <remarks>Данный элемент будет стёрт после первого запроса.
        /// 
        /// Если использовать предварительную загрузку файла, то файл будет сразу загружен в память. Если файл имеет большой размер, либо нет необходимости, чтобы файл находился в памяти, то не используйте предварительную загрузку. В этом случае, файл будет загружаться блоками во время записи в поток.
        /// 
        /// Если не задать тип передаваемых данных, то он будет определяться по расширению файла. Если тип не удастся определить, то будет использовано значение ‘application/unknown‘.</remarks>
        public void AddFile(string name, string path, bool doPreLoading = false, string contentType = null)
        {
            AddedMultipartData.AddFile(name, path, doPreLoading, contentType);
        }

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
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Очищает все HTTP-заголовки, кроме тех, которые устанавливаются через специальные свойства.
        /// </summary>
        public void ClearAllHeaders()
        {
            _headers.Clear();
        }

        #endregion


        internal void ReportBytesReceived(int bytesReceived, int totalBytesToReceive)
        {
            if (_downloadProgressChangedHandler != null)
            {
                _bytesReceived += bytesReceived;

                OnDownloadProgressChanged(
                    new DownloadProgressChangedEventArgs(_bytesReceived, totalBytesToReceive));
            }
        }


        #region Методы (защищённые)

        /// Освобождает неуправляемые (а при необходимости и управляемые) ресурсы, используемые объектом <see cref="HttpRequest"/>.
        /// </summary>
        /// <param name="disposing">Значение <see langword="true"/> позволяет освободить управляемые и неуправляемые ресурсы; значение <see langword="false"/> позволяет освободить только неуправляемые ресурсы.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _tcpClient != null)
            {
                _tcpClient.Close();
                _tcpClient = null;
                _clientStream = null;
                _clientNetworkStream = null;
            }
        }

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
            HttpContent content, bool reconnection = false)
        {
            _content = content;

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

            if (_content != null)
            {
                _content.Init(this, WriteBytes);
            }

            try
            {
                // Отправляем начальную линию.
                byte[] buffer = Encoding.ASCII.GetBytes(GenerateStartingLine(method));
                _clientStream.Write(buffer, 0, buffer.Length);

                // Отправляем заголовки.
                buffer = Encoding.ASCII.GetBytes(GenerateHeaders(method));
                _clientStream.Write(buffer, 0, buffer.Length);

                // Отправляем тело сообщения, если оно не пустое.
                if (_content != null && _content.GetLength() > 0)
                {
                    _bytesSent = 0;
                    _content.Send();
                }
            }
            catch (SecurityException ex)
            {
                throw NewHttpException(Resources.HttpException_FailedSendRequest, ex);
            }
            catch (IOException ex)
            {
                // Если это не первый запрос и включены постоянные соединения и до этого не было переподключения,
                // то пробуем заново отправить запрос.
                if (!createdNewConnection && KeepAlive && !reconnection)
                {
                    Dispose();
                    return SendRequest(method, address, content, true);
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
                // Если был получен пустой ответ и до этого не было переподключения или
                // если это не первый запрос и включены постоянные соединения и до этого не было переподключения,
                // то пробуем заново отправить запрос.
                if ((ex.EmptyMessageBody && !reconnection) ||
                    (!createdNewConnection && KeepAlive && !reconnection))
                {
                    Dispose();
                    return SendRequest(method, address, content, true);
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

            ClearRequestData();

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

                return SendRequest(HttpMethod.GET, redirectAddress, null);
            }
            else
            {
                _redirectionsCount = 0;
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

        private string GenerateHeaders(HttpMethod method)
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

            if (!string.IsNullOrEmpty(ContentType))
            {
                headersBuilder.AppendFormat("Content-Type: {0}\r\n", ContentType);
            }
            else if (_content != null)
            {
                headersBuilder.AppendFormat("Content-Type: {0}\r\n", _content.GetType());
            }

            if (_content != null && _content.GetLength() > 0)
            {
                headersBuilder.AppendFormat("Content-Length: {0}\r\n", _content.GetLength());
            }

            #endregion

            headersBuilder.Append(ToHeadersString(_headers));

            if (_addedHeaders != null)
            {
                headersBuilder.Append(ToHeadersString(_addedHeaders));
            }

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

        private void WriteBytes(byte[] bytes, int length)
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
                        new UploadProgressChangedEventArgs(_bytesSent, _content.GetLength()));
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

        private string ToQueryString(object obj)
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

        private void ClearRequestData()
        {
            _addedParams = null;
            _addedHeaders = null;
            _addedUrlParams = null;
            _addedMultipartData = null;
        }

        private HttpException NewHttpException(string message,
            Exception innerException = null)
        {
            return new HttpException(string.Format(message, Address.Host), innerException);
        }

        #endregion
    }
}