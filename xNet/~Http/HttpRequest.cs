using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security;
using System.Security.Authentication;
using System.Text;
using System.Threading;

namespace xNet
{
    /// <summary>
    /// Представляет класс, предназначеннный для отправки запросов HTTP-серверу.
    /// </summary>
    public class HttpRequest : IDisposable
    {
        // Используется для определения того, сколько байт было отправлено/считано.
        private sealed class HttpWraperStream : Stream
        {
            #region Поля (закрытые)

            private Stream _baseStream;
            private int _sendBufferSize;

            #endregion


            #region Свойства (открытые)

            public Action<int> BytesReadCallback { get; set; }

            public Action<int> BytesWriteCallback { get; set; }

            #region Переопределённые

            public override bool CanRead
            {
                get
                {
                    return _baseStream.CanRead;
                }
            }

            public override bool CanSeek
            {
                get
                {
                    return _baseStream.CanSeek;
                }
            }

            public override bool CanTimeout
            {
                get
                {
                    return _baseStream.CanTimeout;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    return _baseStream.CanWrite;
                }
            }

            public override long Length
            {
                get
                {
                    return _baseStream.Length;
                }
            }

            public override long Position
            {
                get
                {
                    return _baseStream.Position;
                }
                set
                {
                    _baseStream.Position = value;
                }
            }

            #endregion

            #endregion


            public HttpWraperStream(Stream baseStream, int sendBufferSize)
            {
                _baseStream = baseStream;
                _sendBufferSize = sendBufferSize;
            }


            #region Методы (открытые)

            public override void Flush() { }

            public override void SetLength(long value)
            {
                _baseStream.SetLength(value);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _baseStream.Seek(offset, origin);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int bytesRead = _baseStream.Read(buffer, offset, count);

                if (BytesReadCallback != null)
                {
                    BytesReadCallback(bytesRead);
                }

                return bytesRead;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (BytesWriteCallback == null)
                {
                    _baseStream.Write(buffer, offset, count);
                }
                else
                {
                    int index = 0;

                    while (count > 0)
                    {
                        int bytesWrite = 0;

                        if (count >= _sendBufferSize)
                        {
                            bytesWrite = _sendBufferSize;
                            _baseStream.Write(buffer, index, bytesWrite);

                            index += _sendBufferSize;
                            count -= _sendBufferSize;
                        }
                        else
                        {
                            bytesWrite = count;
                            _baseStream.Write(buffer, index, bytesWrite);

                            count = 0;
                        }

                        BytesWriteCallback(bytesWrite);
                    }
                }
            }

            #endregion
        }


        /// <summary>
        /// Версия HTTP-протокола, используемая в запросе.
        /// </summary>
        public static readonly Version ProtocolVersion = new Version(1, 1);


        #region Статические поля (закрытые)

        // Заголовки, которые можно задать только с помощью специального свойства/метода.
        private static readonly List<string> _closedHeaders = new List<string>()
        {
            "Accept-Encoding",
            "Content-Length",
            "Content-Type",
            "Connection",
            "Proxy-Connection",
            "Host"
        };

        #endregion


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

        #endregion


        #region Поля (закрытые)

        private HttpResponse _response;

        private TcpClient _connection;
        private Stream _connectionCommonStream;
        private NetworkStream _connectionNetworkStream;

        private ProxyClient _currentProxy;

        private int _redirectionCount = 0;
        private int _maximumAutomaticRedirections = 5;

        private int _connectTimeout = 60 * 1000;
        private int _readWriteTimeout = 60 * 1000;

        private DateTime _whenConnectionIdle;
        private int _keepAliveTimeout = 30 * 1000;
        private int _maximumKeepAliveRequests = 100;
        private int _keepAliveRequestCount;
        private bool _keepAliveReconnected;

        private int _reconnectLimit = 3;
        private int _reconnectDelay = 100;
        private int _reconnectCount;

        private HttpMethod _method;
        private HttpContent _content; // Тело запроса.

        private readonly Dictionary<string, string> _permanentHeaders =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Временные данные, которые задаются через специальные методы.
        // Удаляются после первого запроса.
        private RequestParams _temporaryParams;
        private RequestParams _temporaryUrlParams;
        private Dictionary<string, string> _temporaryHeaders;
        private MultipartContent _temporaryMultipartContent;

        // Количество отправленных и принятых байт.
        // Используются для событий UploadProgressChanged и DownloadProgressChanged.
        private long _bytesSent;
        private long _totalBytesSent;
        private long _bytesReceived;
        private long _totalBytesReceived;
        private bool _canReportBytesReceived;

        private EventHandler<UploadProgressChangedEventArgs> _uploadProgressChangedHandler;
        private EventHandler<DownloadProgressChangedEventArgs> _downloadProgressChangedHandler;


        #endregion


        #region События (открытые)

        /// <summary>
        /// Возникает каждый раз при продвижении хода выгрузки данных тела сообщения.
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
        /// Возникает каждый раз при продвижении хода загрузки данных тела сообщения.
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
        /// Возвращает или задаёт URI интернет-ресурса, который используется, если в запросе указан относительный адрес.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        public Uri BaseAddress { get; set; }

        /// <summary>
        /// Возвращает URI интернет-ресурса, который фактически отвечает на запрос.
        /// </summary>
        public Uri Address { get; private set; }

        /// <summary>
        /// Возвращает последний ответ от HTTP-сервера, полученный данным экземпляром класса.
        /// </summary>
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
        /// <value>Значение по умолчанию — <see langword="null"/>. Если установлено значение по умолчанию, то используется метод, который принимает все сертификаты SSL.</value>
        public RemoteCertificateValidationCallback SslCertificateValidatorCallback;

        #region Поведение

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
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра меньше 0.</exception>
        public int ConnectTimeout
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
                    throw ExceptionHelper.CanNotBeLess("ReadWriteTimeout", 0);
                }

                #endregion

                _readWriteTimeout = value;
            }
        }

        /// <summary>
        /// Возвращает или задает значение, указывающие, нужно ли игнорировать ошибки протокола и не генерировать исключения.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="false"/>.</value>
        /// <remarks>Если установить значение <see langword="true"/>, то в случае получения ошибочного ответа с кодом состояния 4xx или 5xx, не будет сгенерировано исключение. Вы можете узнать код состояния ответа с помощью свойства <see cref="HttpResponse.StatusCode"/>.</remarks>
        public bool IgnoreProtocolErrors { get; set; }

        /// <summary>
        /// Возвращает или задает значение, указывающее, необходимо ли устанавливать постоянное подключение к интернет-ресурсу.
        /// </summary>
        /// <value>Значение по умолчанию - <see langword="true"/>.</value>
        /// <remarks>Если значение равно <see langword="true"/>, то дополнительно отправляется заголовок 'Connection: Keep-Alive', иначе отправляется заголовок 'Connection: Close'. Если для подключения используется HTTP-прокси, то вместо заголовка - 'Connection', устанавливается заголовок - 'Proxy-Connection'. В случае, если сервер оборвёт постоянное соединение, <see cref="HttpResponse"/> попытается подключиться заново, но это работает только, если подключение идёт напрямую с HTTP-сервером, либо с HTTP-прокси.</remarks>
        public bool KeepAlive { get; set; }

        /// <summary>
        /// Возвращает или задает время простаивания постоянного соединения в миллисекундах, которое используется по умолчанию.
        /// </summary>
        /// <value>Значение по умолчанию - 30.000, что равняется 30 секундам.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра меньше 0.</exception>
        /// <remarks>Если время вышло, то будет создано новое подключение. Если сервер вернёт своё значение таймаута <see cref="HttpResponse.KeepAliveTimeout"/>, тогда будет использовано именно оно.</remarks>
        public int KeepAliveTimeout
        {
            get
            {
                return _keepAliveTimeout;
            }
            set
            {
                #region Проверка параметра

                if (value < 0)
                {
                    throw ExceptionHelper.CanNotBeLess("KeepAliveTimeout", 0);
                }

                #endregion

                _keepAliveTimeout = value;
            }
        }

        /// <summary>
        /// Возвращает или задает максимально допустимое количество запросов для одного соединения, которое используется по умолчанию.
        /// </summary>
        /// <value>Значение по умолчанию - 100.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра меньше 1.</exception>
        /// <remarks>Если количество запросов превысило максимальное, то будет создано новое подключение. Если сервер вернёт своё значение максимального кол-ва запросов <see cref="HttpResponse.MaximumKeepAliveRequests"/>, тогда будет использовано именно оно.</remarks>
        public int MaximumKeepAliveRequests
        {
            get
            {
                return _maximumKeepAliveRequests;
            }
            set
            {
                #region Проверка параметра

                if (value < 1)
                {
                    throw ExceptionHelper.CanNotBeLess("MaximumKeepAliveRequests", 1);
                }

                #endregion

                _maximumKeepAliveRequests = value;
            }
        }

        /// <summary>
        /// Возвращает или задает значение, указывающее, нужно ли пробовать переподключаться через n-миллисекунд, если произошла ошибка во время подключения или отправки/загрузки данных.
        /// </summary>
        /// <value>Значение по умолчанию - <see langword="false"/>.</value>
        public bool Reconnect { get; set; }

        /// <summary>
        /// Возвращает или задает максимальное количество попыток переподключения.
        /// </summary>
        /// <value>Значение по умолчанию - 3.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра меньше 1.</exception>
        public int ReconnectLimit
        {
            get
            {
                return _reconnectLimit;
            }
            set
            {
                #region Проверка параметра

                if (value < 1)
                {
                    throw ExceptionHelper.CanNotBeLess("ReconnectLimit", 1);
                }

                #endregion

                _reconnectLimit = value;
            }
        }

        /// <summary>
        /// Возвращает или задает задержку в миллисекундах, которая возникает перед тем, как выполнить переподключение.
        /// </summary>
        /// <value>Значение по умолчанию - 100 миллисекунд.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра меньше 0.</exception>
        public int ReconnectDelay
        {
            get
            {
                return _reconnectDelay;
            }
            set
            {
                #region Проверка параметра

                if (value < 0)
                {
                    throw ExceptionHelper.CanNotBeLess("ReconnectDelay", 0);
                }

                #endregion

                _reconnectDelay = value;
            }
        }

        #endregion

        #region HTTP-заголовки

        /// <summary>
        /// Язык, используемый текущим запросом.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        /// <remarks>Если язык установлен, то дополнительно отправляется заголовок 'Accept-Language' с названием этого языка.</remarks>
        public CultureInfo Culture { get; set; }

        /// <summary>
        /// Возвращает или задаёт кодировку, применяемую для преобразования исходящих и входящих данных.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        /// <remarks>Если кодировка установлена, то дополнительно отправляется заголовок 'Accept-Charset' с названием этой кодировки, но только если этот заголовок уже не задан напрямую. Кодировка ответа определяется автоматически, но, если её не удастся определить, то будет использовано значение данного свойства. Если значение данного свойства не задано, то будет использовано значение <see cref="System.Text.Encoding.Default"/>.</remarks>
        public Encoding CharacterSet { get; set; }

        /// <summary>
        /// Возвращает или задает значение, указывающее, нужно ли кодировать содержимое ответа. Это используется, прежде всего, для сжатия данных.
        /// </summary>
        /// <value>Значение по умолчанию - <see langword="true"/>.</value>
        /// <remarks>Если значение равно <see langword="true"/>, то дополнительно отправляется заголовок 'Accept-Encoding: gzip, deflate'.</remarks>
        public bool EnableEncodingContent { get; set; }

        /// <summary>
        /// Возвращает или задаёт имя пользователя для базовой авторизации на HTTP-сервере.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        /// <remarks>Если значение установлено, то дополнительно отправляется заголовок 'Authorization'.</remarks>
        public string Username { get; set; }

        /// <summary>
        /// Возвращает или задаёт пароль для базовой авторизации на HTTP-сервере.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        /// <remarks>Если значение установлено, то дополнительно отправляется заголовок 'Authorization'.</remarks>
        public string Password { get; set; }

        /// <summary>
        /// Возвращает или задает значение HTTP-заголовка 'User-Agent'.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        public string UserAgent
        {
            get
            {
                return this["User-Agent"];
            }
            set
            {
                this["User-Agent"] = value;
            }
        }

        /// <summary>
        /// Возвращает или задает значение HTTP-заголовка 'Referer'.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        public string Referer
        {
            get
            {
                return this["Referer"];
            }
            set
            {
                this["Referer"] = value;
            }
        }

        /// <summary>
        /// Возвращает или задает значение HTTP-заголовка 'Authorization'.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        public string Authorization
        {
            get
            {
                return this["Authorization"];
            }
            set
            {
                this["Authorization"] = value;
            }
        }

        /// <summary>
        /// Возвращает или задает куки, связанные с запросом.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        /// <remarks>Куки могут изменяться ответом от HTTP-сервера. Чтобы не допустить этого, нужно установить свойство <see cref="xNet.Net.CookieDictionary.IsLocked"/> равным <see langword="true"/>.</remarks>
        public CookieDictionary Cookies { get; set; }

        #endregion

        #endregion


        #region Свойства (внутренние)

        internal TcpClient TcpClient
        {
            get
            {
                return _connection;
            }
        }

        internal Stream ClientStream
        {
            get
            {
                return _connectionCommonStream;
            }
        }

        internal NetworkStream ClientNetworkStream
        {
            get
            {
                return _connectionNetworkStream;
            }
        }

        #endregion


        private MultipartContent AddedMultipartData
        {
            get
            {
                if (_temporaryMultipartContent == null)
                {
                    _temporaryMultipartContent = new MultipartContent();
                }

                return _temporaryMultipartContent;
            }
        }


        #region Индексаторы (открытые)

        /// <summary>
        /// Возвращает или задаёт значение HTTP-заголовка.
        /// </summary>
        /// <param name="headerName">Название HTTP-заголовка.</param>
        /// <value>Значение HTTP-заголовка, если он задан, иначе пустая строка. Если задать значение <see langword="null"/> или пустую строку, то HTTP-заголовок будет удалён из списка.</value>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="headerName"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">
        /// Значение параметра <paramref name="headerName"/> является пустой строкой.
        /// -или-
        /// Установка значения HTTP-заголовка, который должен задаваться с помощью специального свойства/метода.
        /// </exception>
        /// <remarks>Список HTTP-заголовков, которые должны задаваться только с помощью специальных свойств/методов:
        /// <list type="table">
        ///     <item>
        ///        <description>Accept-Encoding</description>
        ///     </item>
        ///     <item>
        ///        <description>Content-Length</description>
        ///     </item>
        ///     <item>
        ///         <description>Content-Type</description>
        ///     </item>
        ///     <item>
        ///        <description>Connection</description>
        ///     </item>
        ///     <item>
        ///        <description>Proxy-Connection</description>
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

                #endregion

                string value;

                if (!_permanentHeaders.TryGetValue(headerName, out value))
                {
                    value = string.Empty;
                }

                return value;
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

                if (IsClosedHeader(headerName))
                {
                    throw new ArgumentException(string.Format(
                        Resources.ArgumentException_HttpRequest_SetNotAvailableHeader, headerName), "headerName");
                }

                #endregion

                if (string.IsNullOrEmpty(value))
                {
                    _permanentHeaders.Remove(headerName);
                }
                else
                {
                    _permanentHeaders[headerName] = value;
                }
            }
        }

        /// <summary>
        /// Возвращает или задаёт значение HTTP-заголовка.
        /// </summary>
        /// <param name="header">HTTP-заголовок.</param>
        /// <value>Значение HTTP-заголовка, если он задан, иначе пустая строка. Если задать значение <see langword="null"/> или пустую строку, то HTTP-заголовок будет удалён из списка.</value>
        /// <exception cref="System.ArgumentException">Установка значения HTTP-заголовка, который должен задаваться с помощью специального свойства/метода.</exception>
        /// <remarks>Список HTTP-заголовков, которые должны задаваться только с помощью специальных свойств/методов:
        /// <list type="table">
        ///     <item>
        ///        <description>Accept-Encoding</description>
        ///     </item>
        ///     <item>
        ///        <description>Content-Length</description>
        ///     </item>
        ///     <item>
        ///         <description>Content-Type</description>
        ///     </item>
        ///     <item>
        ///        <description>Connection</description>
        ///     </item>
        ///     <item>
        ///        <description>Proxy-Connection</description>
        ///     </item>
        ///     <item>
        ///        <description>Host</description>
        ///     </item>
        /// </list>
        /// </remarks>
        public string this[HttpHeader header]
        {
            get
            {
                return this[Http.Headers[header]];
            }
            set
            {
                this[Http.Headers[header]] = value;
            }
        }

        #endregion


        #region Конструкторы (открытые)

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="HttpRequest"/>.
        /// </summary>
        public HttpRequest()
        {
            Init();
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="HttpRequest"/>.
        /// </summary>
        /// <param name="baseAddress">Адрес интернет-ресурса, который используется, если в запросе указан относительный адрес.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="baseAddress"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">
        /// Значение параметра <paramref name="baseAddress"/> является пустой строкой.
        /// -или-
        /// Значение параметра <paramref name="baseAddress"/> не является абсолютным URI.
        /// </exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="baseAddress"/> не является абсолютным URI.</exception>
        public HttpRequest(string baseAddress)
        {
            #region Проверка параметров

            if (baseAddress == null)
            {
                throw new ArgumentNullException("baseAddress");
            }

            if (baseAddress.Length == 0)
            {
                throw ExceptionHelper.EmptyString("baseAddress");
            }

            #endregion

            if (!baseAddress.StartsWith("http"))
            {
                baseAddress = "http://" + baseAddress;
            }

            var uri = new Uri(baseAddress);

            if (!uri.IsAbsoluteUri)
            {
                throw new ArgumentException(Resources.ArgumentException_OnlyAbsoluteUri, "baseAddress");
            }

            BaseAddress = uri;

            Init();
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="HttpRequest"/>.
        /// </summary>
        /// <param name="baseAddress">Адрес интернет-ресурса, который используется, если в запросе указан относительный адрес.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="baseAddress"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="baseAddress"/> не является абсолютным URI.</exception>
        public HttpRequest(Uri baseAddress)
        {
            #region Проверка параметров

            if (baseAddress == null)
            {
                throw new ArgumentNullException("baseAddress");
            }

            if (!baseAddress.IsAbsoluteUri)
            {
                throw new ArgumentException(Resources.ArgumentException_OnlyAbsoluteUri, "baseAddress");
            }

            #endregion

            BaseAddress = baseAddress;

            Init();
        }

        #endregion


        #region Методы (открытые)

        #region Get

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="urlParams">Параметры URL-адреса, или значение <see langword="null"/>.</param>
        /// <returns>Объект, предназначенный для загрузки ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public HttpResponse Get(string address, RequestParams urlParams = null)
        {
            if (urlParams != null)
            {
                _temporaryUrlParams = urlParams;
            }

            return Raw(HttpMethod.GET, address);
        }

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="urlParams">Параметры URL-адреса, или значение <see langword="null"/>.</param>
        /// <returns>Объект, предназначенный для загрузки ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public HttpResponse Get(Uri address, RequestParams urlParams = null)
        {
            if (urlParams != null)
            {
                _temporaryUrlParams = urlParams;
            }

            return Raw(HttpMethod.GET, address);
        }

        #endregion

        #region Post

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <returns>Объект, предназначенный для загрузки ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public HttpResponse Post(string address)
        {
            return Raw(HttpMethod.POST, address);
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <returns>Объект, предназначенный для загрузки ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public HttpResponse Post(Uri address)
        {
            return Raw(HttpMethod.POST, address);
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="reqParams">Параметры запроса, отправляемые HTTP-серверу.</param>
        /// <param name="dontEscape">Указывает, нужно ли кодировать параметры запроса.</param>
        /// <returns>Объект, предназначенный для загрузки ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="address"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="reqParams"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public HttpResponse Post(string address, RequestParams reqParams, bool dontEscape = false)
        {
            #region Проверка параметров

            if (reqParams == null)
            {
                throw new ArgumentNullException("reqParams");
            }

            #endregion

            return Raw(HttpMethod.POST, address, new FormUrlEncodedContent(reqParams, dontEscape, CharacterSet));
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="reqParams">Параметры запроса, отправляемые HTTP-серверу.</param>
        /// <param name="dontEscape">Указывает, нужно ли кодировать параметры запроса.</param>
        /// <returns>Объект, предназначенный для загрузки ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="address"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="reqParams"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public HttpResponse Post(Uri address, RequestParams reqParams, bool dontEscape = false)
        {
            #region Проверка параметров

            if (reqParams == null)
            {
                throw new ArgumentNullException("reqParams");
            }

            #endregion

            return Raw(HttpMethod.POST, address, new FormUrlEncodedContent(reqParams, dontEscape, CharacterSet));
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="str">Строка, отправляемая HTTP-серверу.</param>
        /// <param name="contentType">Тип отправляемых данных.</param>
        /// <returns>Объект, предназначенный для загрузки ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="address"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="str"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="contentType"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Значение параметра <paramref name="address"/> является пустой строкой.
        /// -или-
        /// Значение параметра <paramref name="str"/> является пустой строкой.
        /// -или
        /// Значение параметра <paramref name="contentType"/> является пустой строкой.
        /// </exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public HttpResponse Post(string address, string str, string contentType)
        {
            #region Проверка параметров

            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

            if (str.Length == 0)
            {
                throw new ArgumentNullException("str");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            if (contentType.Length == 0)
            {
                throw new ArgumentNullException("contentType");
            }

            #endregion

            var content = new StringContent(str)
            {
                ContentType = contentType
            };

            return Raw(HttpMethod.POST, address, content);
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="str">Строка, отправляемая HTTP-серверу.</param>
        /// <param name="contentType">Тип отправляемых данных.</param>
        /// <returns>Объект, предназначенный для загрузки ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="address"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="str"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="contentType"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Значение параметра <paramref name="str"/> является пустой строкой.
        /// -или-
        /// Значение параметра <paramref name="contentType"/> является пустой строкой.
        /// </exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public HttpResponse Post(Uri address, string str, string contentType)
        {
            #region Проверка параметров

            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

            if (str.Length == 0)
            {
                throw new ArgumentNullException("str");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            if (contentType.Length == 0)
            {
                throw new ArgumentNullException("contentType");
            }

            #endregion

            var content = new StringContent(str)
            {
                ContentType = contentType
            };

            return Raw(HttpMethod.POST, address, content);
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="bytes">Массив байтов, отправляемый HTTP-серверу.</param>
        /// <param name="contentType">Тип отправляемых данных.</param>
        /// <returns>Объект, предназначенный для загрузки ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="address"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="bytes"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="contentType"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Значение параметра <paramref name="address"/> является пустой строкой.
        /// -или-
        /// Значение параметра <paramref name="contentType"/> является пустой строкой.
        /// </exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public HttpResponse Post(string address, byte[] bytes, string contentType = "application/octet-stream")
        {
            #region Проверка параметров

            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            if (contentType.Length == 0)
            {
                throw new ArgumentNullException("contentType");
            }

            #endregion

            var content = new BytesContent(bytes)
            {
                ContentType = contentType
            };

            return Raw(HttpMethod.POST, address, content);
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="bytes">Массив байтов, отправляемый HTTP-серверу.</param>
        /// <param name="contentType">Тип отправляемых данных.</param>
        /// <returns>Объект, предназначенный для загрузки ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="address"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="bytes"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="contentType"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="contentType"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public HttpResponse Post(Uri address, byte[] bytes, string contentType = "application/octet-stream")
        {
            #region Проверка параметров

            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            if (contentType.Length == 0)
            {
                throw new ArgumentNullException("contentType");
            }

            #endregion

            var content = new BytesContent(bytes)
            {
                ContentType = contentType
            };

            return Raw(HttpMethod.POST, address, content);
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="stream">Поток данных, отправляемый HTTP-серверу.</param>
        /// <param name="contentType">Тип отправляемых данных.</param>
        /// <returns>Объект, предназначенный для загрузки ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="address"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="stream"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="contentType"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Значение параметра <paramref name="address"/> является пустой строкой.
        /// -или-
        /// Значение параметра <paramref name="contentType"/> является пустой строкой.
        /// </exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public HttpResponse Post(string address, Stream stream, string contentType = "application/octet-stream")
        {
            #region Проверка параметров

            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            if (contentType.Length == 0)
            {
                throw new ArgumentNullException("contentType");
            }

            #endregion

            var content = new StreamContent(stream)
            {
                ContentType = contentType
            };

            return Raw(HttpMethod.POST, address, content);
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="stream">Поток данных, отправляемый HTTP-серверу.</param>
        /// <param name="contentType">Тип отправляемых данных.</param>
        /// <returns>Объект, предназначенный для загрузки ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="address"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="stream"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="contentType"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="contentType"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public HttpResponse Post(Uri address, Stream stream, string contentType = "application/octet-stream")
        {
            #region Проверка параметров

            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            if (contentType.Length == 0)
            {
                throw new ArgumentNullException("contentType");
            }

            #endregion

            var content = new StreamContent(stream)
            {
                ContentType = contentType
            };

            return Raw(HttpMethod.POST, address, content);
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="path">Путь к файлу, данные которого будут отправлены HTTP-серверу.</param>
        /// <returns>Объект, предназначенный для загрузки ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="address"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="path"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Значение параметра <paramref name="address"/> является пустой строкой.
        /// -или-
        /// Значение параметра <paramref name="path"/> является пустой строкой.
        /// </exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public HttpResponse Post(string address, string path)
        {
            #region Проверка параметров

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (path.Length == 0)
            {
                throw new ArgumentNullException("path");
            }

            #endregion

            return Raw(HttpMethod.POST, address, new FileContent(path));
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="path">Путь к файлу, данные которого будут отправлены HTTP-серверу.</param>
        /// <returns>Объект, предназначенный для загрузки ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="address"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="path"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="path"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public HttpResponse Post(Uri address, string path)
        {
            #region Проверка параметров

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (path.Length == 0)
            {
                throw new ArgumentNullException("path");
            }

            #endregion

            return Raw(HttpMethod.POST, address, new FileContent(path));
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="content">Контент, отправляемый HTTP-серверу.</param>
        /// <returns>Объект, предназначенный для загрузки ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="address"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="content"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public HttpResponse Post(string address, HttpContent content)
        {
            #region Проверка параметров

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            #endregion

            return Raw(HttpMethod.POST, address, content);
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="content">Контент, отправляемый HTTP-серверу.</param>
        /// <returns>Объект, предназначенный для загрузки ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="address"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="content"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public HttpResponse Post(Uri address, HttpContent content)
        {
            #region Проверка параметров

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            #endregion

            return Raw(HttpMethod.POST, address, content);
        }

        #endregion

        #region Raw

        /// <summary>
        /// Отправляет запрос HTTP-серверу.
        /// </summary>
        /// <param name="method">HTTP-метод запроса.</param>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="content">Контент, отправляемый HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <returns>Объект, предназначенный для загрузки ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public HttpResponse Raw(HttpMethod method, string address, HttpContent content = null)
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

            var uri = new Uri(address, UriKind.RelativeOrAbsolute);
            return Raw(method, uri, content);
        }

        /// <summary>
        /// Отправляет запрос HTTP-серверу.
        /// </summary>
        /// <param name="method">HTTP-метод запроса.</param>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="content">Контент, отправляемый HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <returns>Объект, предназначенный для загрузки ответа от HTTP-сервера.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public HttpResponse Raw(HttpMethod method, Uri address, HttpContent content = null)
        {
            #region Проверка параметров

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            #endregion

            if (!address.IsAbsoluteUri)
                address = GetRequestAddress(BaseAddress, address);

            if (_temporaryUrlParams != null)
            {
                var uriBuilder = new UriBuilder(address);
                uriBuilder.Query = Http.ToQueryString(_temporaryUrlParams, true);

                address = uriBuilder.Uri;
            }

            if (content == null)
            {
                if (_temporaryParams != null)
                {
                    content = new FormUrlEncodedContent(_temporaryParams, false, CharacterSet);
                }
                else if (_temporaryMultipartContent != null)
                {
                    content = _temporaryMultipartContent;
                }
            }

            try
            {
                return Request(method, address, content);
            }
            finally
            {
                if (content != null)
                    content.Dispose();

                ClearRequestData();
            }
        }

        #endregion

        #region Добавление временных данных запроса

        /// <summary>
        /// Добавляет временный параметр URL-адреса.
        /// </summary>
        /// <param name="name">Имя параметра.</param>
        /// <param name="value">Значение параметра, или значение <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="name"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="name"/> является пустой строкой.</exception>
        /// <remarks>Данный параметр будет стёрт после первого запроса.</remarks>
        public HttpRequest AddUrlParam(string name, object value = null)
        {
            #region Проверка параметров

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            #endregion

            if (_temporaryUrlParams == null)
            {
                _temporaryUrlParams = new RequestParams();
            }

            _temporaryUrlParams[name] = value;

            return this;
        }

        /// <summary>
        /// Добавляет временный параметр запроса.
        /// </summary>
        /// <param name="name">Имя параметра.</param>
        /// <param name="value">Значение параметра, или значение <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="name"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="name"/> является пустой строкой.</exception>
        /// <remarks>Данный параметр будет стёрт после первого запроса.</remarks>
        public HttpRequest AddParam(string name, object value = null)
        {
            #region Проверка параметров

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            #endregion

            if (_temporaryParams == null)
            {
                _temporaryParams = new RequestParams();
            }

            _temporaryParams[name] = value;

            return this;
        }

        /// <summary>
        /// Добавляет временный элемент Multipart/form данных.
        /// </summary>
        /// <param name="name">Имя элемента.</param>
        /// <param name="value">Значение элемента, или значение <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="name"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="name"/> является пустой строкой.</exception>
        /// <remarks>Данный элемент будет стёрт после первого запроса.</remarks>
        public HttpRequest AddField(string name, object value = null)
        {
            return AddField(name, value, CharacterSet ?? Encoding.UTF8);
        }

        /// <summary>
        /// Добавляет временный элемент Multipart/form данных.
        /// </summary>
        /// <param name="name">Имя элемента.</param>
        /// <param name="value">Значение элемента, или значение <see langword="null"/>.</param>
        /// <param name="encoding">Кодировка, применяемая для преобразования значения в последовательность байтов.</param>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="name"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="encoding"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="name"/> является пустой строкой.</exception>
        /// <remarks>Данный элемент будет стёрт после первого запроса.</remarks>
        public HttpRequest AddField(string name, object value, Encoding encoding)
        {
            #region Проверка параметров

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            #endregion

            string contentValue = (value == null ? string.Empty : value.ToString());

            AddedMultipartData.Add(new StringContent(contentValue, encoding), name);

            return this;
        }

        /// <summary>
        /// Добавляет временный элемент Multipart/form данных.
        /// </summary>
        /// <param name="name">Имя элемента.</param>
        /// <param name="value">Значение элемента.</param>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="name"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="value"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="name"/> является пустой строкой.</exception>
        /// <remarks>Данный элемент будет стёрт после первого запроса.</remarks>
        public HttpRequest AddField(string name, byte[] value)
        {
            #region Проверка параметров

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            #endregion

            AddedMultipartData.Add(new BytesContent(value), name);

            return this;
        }

        /// <summary>
        /// Добавляет временный элемент Multipart/form данных, представляющий файл.
        /// </summary>
        /// <param name="name">Имя элемента.</param>
        /// <param name="fileName">Имя передаваемого файла.</param>
        /// <param name="value">Данные файла.</param>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="name"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="fileName"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="value"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="name"/> является пустой строкой.</exception>
        /// <remarks>Данный элемент будет стёрт после первого запроса.</remarks>
        public HttpRequest AddFile(string name, string fileName, byte[] value)
        {
            #region Проверка параметров

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            #endregion

            AddedMultipartData.Add(new BytesContent(value), name, fileName);

            return this;
        }

        /// <summary>
        /// Добавляет временный элемент Multipart/form данных, представляющий файл.
        /// </summary>
        /// <param name="name">Имя элемента.</param>
        /// <param name="fileName">Имя передаваемого файла.</param>
        /// <param name="contentType">MIME-тип контента.</param>
        /// <param name="value">Данные файла.</param>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="name"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="fileName"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="contentType"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="value"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="name"/> является пустой строкой.</exception>
        /// <remarks>Данный элемент будет стёрт после первого запроса.</remarks>
        public HttpRequest AddFile(string name, string fileName, string contentType, byte[] value)
        {
            #region Проверка параметров

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            #endregion

            AddedMultipartData.Add(new BytesContent(value), name, fileName, contentType);

            return this;
        }

        /// <summary>
        /// Добавляет временный элемент Multipart/form данных, представляющий файл.
        /// </summary>
        /// <param name="name">Имя элемента.</param>
        /// <param name="fileName">Имя передаваемого файла.</param>
        /// <param name="stream">Поток данных файла.</param>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="name"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="fileName"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="stream"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="name"/> является пустой строкой.</exception>
        /// <remarks>Данный элемент будет стёрт после первого запроса.</remarks>
        public HttpRequest AddFile(string name, string fileName, Stream stream)
        {
            #region Проверка параметров

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            #endregion

            AddedMultipartData.Add(new StreamContent(stream), name, fileName);

            return this;
        }

        /// <summary>
        /// Добавляет временный элемент Multipart/form данных, представляющий файл.
        /// </summary>
        /// <param name="name">Имя элемента.</param>
        /// <param name="fileName">Имя передаваемого файла.</param>
        /// <param name="path">Путь к загружаемому файлу.</param>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="name"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="fileName"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="path"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Значение параметра <paramref name="name"/> является пустой строкой.
        /// -или-
        /// Значение параметра <paramref name="path"/> является пустой строкой.
        /// </exception>
        /// <remarks>Данный элемент будет стёрт после первого запроса.</remarks>
        public HttpRequest AddFile(string name, string fileName, string path)
        {
            #region Проверка параметров

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (path.Length == 0)
            {
                throw ExceptionHelper.EmptyString("path");
            }

            #endregion

            AddedMultipartData.Add(new FileContent(path), name, fileName);

            return this;
        }

        /// <summary>
        /// Добавляет временный элемент Multipart/form данных, представляющий файл.
        /// </summary>
        /// <param name="name">Имя элемента.</param>
        /// <param name="path">Путь к загружаемому файлу.</param>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="name"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="path"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Значение параметра <paramref name="name"/> является пустой строкой.
        /// -или-
        /// Значение параметра <paramref name="path"/> является пустой строкой.
        /// </exception>
        /// <remarks>Данный элемент будет стёрт после первого запроса.</remarks>
        public HttpRequest AddFile(string name, string path)
        {
            #region Проверка параметров

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (path.Length == 0)
            {
                throw ExceptionHelper.EmptyString("path");
            }

            #endregion

            AddedMultipartData.Add(new FileContent(path),
                name, Path.GetFileName(path));

            return this;
        }

        /// <summary>
        /// Добавляет временный HTTP-заголовок запроса. Такой заголовок перекрывает заголовок установленный через индексатор.
        /// </summary>
        /// <param name="name">Имя HTTP-заголовка.</param>
        /// <param name="value">Значение HTTP-заголовка.</param>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="name"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="value"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Значение параметра <paramref name="name"/> является пустой строкой.
        /// -или-
        /// Значение параметра <paramref name="value"/> является пустой строкой.
        /// -или-
        /// Установка значения HTTP-заголовка, который должен задаваться с помощью специального свойства/метода.
        /// </exception>
        /// <remarks>Данный HTTP-заголовок будет стёрт после первого запроса.</remarks>
        public HttpRequest AddHeader(string name, string value)
        {
            #region Проверка параметров

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (value.Length == 0)
            {
                throw ExceptionHelper.EmptyString("value");
            }

            if (IsClosedHeader(name))
            {
                throw new ArgumentException(string.Format(
                    Resources.ArgumentException_HttpRequest_SetNotAvailableHeader, name), "name");
            }

            #endregion

            if (_temporaryHeaders == null)
            {
                _temporaryHeaders = new Dictionary<string, string>();
            }

            _temporaryHeaders[name] = value;

            return this;
        }

        /// <summary>
        /// Добавляет временный HTTP-заголовок запроса. Такой заголовок перекрывает заголовок установленный через индексатор.
        /// </summary>
        /// <param name="header">HTTP-заголовок.</param>
        /// <param name="value">Значение HTTP-заголовка.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="value"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">
        /// Значение параметра <paramref name="value"/> является пустой строкой.
        /// -или-
        /// Установка значения HTTP-заголовка, который должен задаваться с помощью специального свойства/метода.
        /// </exception>
        /// <remarks>Данный HTTP-заголовок будет стёрт после первого запроса.</remarks>
        public HttpRequest AddHeader(HttpHeader header, string value)
        {
            AddHeader(Http.Headers[header], value);

            return this;
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
        /// Определяет, содержатся ли указанные куки.
        /// </summary>
        /// <param name="name">Название куки.</param>
        /// <returns>Значение <see langword="true"/>, если указанные куки содержатся, иначе значение <see langword="false"/>.</returns>
        public bool ContainsCookie(string name)
        {
            if (Cookies == null)
                return false;

            return Cookies.ContainsKey(name);
        }

        #region Работа с заголовками

        /// <summary>
        /// Определяет, содержится ли указанный HTTP-заголовок.
        /// </summary>
        /// <param name="headerName">Название HTTP-заголовка.</param>
        /// <returns>Значение <see langword="true"/>, если указанный HTTP-заголовок содержится, иначе значение <see langword="false"/>.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="headerName"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="headerName"/> является пустой строкой.</exception>
        public bool ContainsHeader(string headerName)
        {
            #region Проверка параметров

            if (headerName == null)
            {
                throw new ArgumentNullException("headerName");
            }

            if (headerName.Length == 0)
            {
                throw ExceptionHelper.EmptyString("headerName");
            }

            #endregion

            return _permanentHeaders.ContainsKey(headerName);
        }

        /// <summary>
        /// Определяет, содержится ли указанный HTTP-заголовок.
        /// </summary>
        /// <param name="header">HTTP-заголовок.</param>
        /// <returns>Значение <see langword="true"/>, если указанный HTTP-заголовок содержится, иначе значение <see langword="false"/>.</returns>
        public bool ContainsHeader(HttpHeader header)
        {
            return ContainsHeader(Http.Headers[header]);
        }

        /// <summary>
        /// Возвращает перечисляемую коллекцию HTTP-заголовков.
        /// </summary>
        /// <returns>Коллекция HTTP-заголовков.</returns>
        public Dictionary<string, string>.Enumerator EnumerateHeaders()
        {
            return _permanentHeaders.GetEnumerator();
        }

        /// <summary>
        /// Очищает все HTTP-заголовки.
        /// </summary>
        public void ClearAllHeaders()
        {
            _permanentHeaders.Clear();
        }

        #endregion

        #endregion


        #region Методы (защищённые)

        /// Освобождает неуправляемые (а при необходимости и управляемые) ресурсы, используемые объектом <see cref="HttpRequest"/>.
        /// </summary>
        /// <param name="disposing">Значение <see langword="true"/> позволяет освободить управляемые и неуправляемые ресурсы; значение <see langword="false"/> позволяет освободить только неуправляемые ресурсы.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _connection != null)
            {
                _connection.Close();
                _connection = null;
                _connectionCommonStream = null;
                _connectionNetworkStream = null;

                _keepAliveRequestCount = 0;
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

        private void Init()
        {
            KeepAlive = true;
            AllowAutoRedirect = true;
            EnableEncodingContent = true;

            _response = new HttpResponse(this);
        }

        private Uri GetRequestAddress(Uri baseAddress, Uri address)
        {
            var requestAddress = address;

            if (baseAddress == null)
            {
                var uriBuilder = new UriBuilder(address.OriginalString);
                requestAddress = uriBuilder.Uri;
            }
            else
            {
                Uri.TryCreate(baseAddress, address, out requestAddress);
            }

            return requestAddress;
        }

        #region Отправка запроса

        private HttpResponse Request(HttpMethod method, Uri address, HttpContent content)
        {
            _method = method;
            _content = content;

            CloseConnectionIfNeeded();

            var previousAddress = Address;
            Address = address;

            var createdNewConnection = false;
            try
            {
                createdNewConnection = TryCreateConnectionOrUseExisting(address, previousAddress);
            }
            catch (HttpException ex)
            {
                if (CanReconnect())
                    return ReconnectAfterFail();

                throw;
            }

            if (createdNewConnection)
                _keepAliveRequestCount = 1;
            else
                _keepAliveRequestCount++;

            #region Отправка запроса

            try
            {
                SendRequestData(method);
            }
            catch (SecurityException ex)
            {
                throw NewHttpException(Resources.HttpException_FailedSendRequest, ex, HttpExceptionStatus.SendFailure);
            }
            catch (IOException ex)
            {
                if (CanReconnect())
                    return ReconnectAfterFail();

                throw NewHttpException(Resources.HttpException_FailedSendRequest, ex, HttpExceptionStatus.SendFailure);
            }

            #endregion

            #region Загрузка заголовков ответа

            try
            {
                ReceiveResponseHeaders(method);
            }
            catch (HttpException ex)
            {
                if (CanReconnect())
                    return ReconnectAfterFail();

                // Если сервер оборвал постоянное соединение вернув пустой ответ, то пробуем подключиться заново.
                // Он мог оборвать соединение потому, что достигнуто максимально допустимое кол-во запросов или вышло время простоя.
                if (KeepAlive && !_keepAliveReconnected && !createdNewConnection && ex.EmptyMessageBody)
                    return KeepAliveReconect();

                throw;
            }

            #endregion

            _response.ReconnectCount = _reconnectCount;

            _reconnectCount = 0;
            _keepAliveReconnected = false;
            _whenConnectionIdle = DateTime.Now;

            if (!IgnoreProtocolErrors)
                CheckStatusCode(_response.StatusCode);

            #region Переадресация

            if (AllowAutoRedirect && _response.HasRedirect)
            {
                if (++_redirectionCount > _maximumAutomaticRedirections)
                    throw NewHttpException(Resources.HttpException_LimitRedirections);

                ClearRequestData();
                return Request(HttpMethod.GET, _response.RedirectAddress, null);
            }

            _redirectionCount = 0;

            #endregion

            return _response;
        }

        private void CloseConnectionIfNeeded()
        {
            var hasConnection = (_connection != null);

            if (hasConnection && !_response.HasError &&
                !_response.MessageBodyLoaded)
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
        }

        private bool TryCreateConnectionOrUseExisting(Uri address, Uri previousAddress)
        {
            ProxyClient proxy = GetProxy();

            var hasConnection = (_connection != null);
            var proxyChanged = (_currentProxy != proxy);

            var addressChanged =
                (previousAddress == null) ||
                (previousAddress.Port != address.Port) ||
                (previousAddress.Host != address.Host) ||
                (previousAddress.Scheme != address.Scheme);

            // Если нужно создать новое подключение.
            if (!hasConnection || proxyChanged ||
                addressChanged || _response.HasError ||
                KeepAliveLimitIsReached())
            {
                _currentProxy = proxy;

                Dispose();
                CreateConnection(address);
                return true;
            }

            return false;
        }

        private bool KeepAliveLimitIsReached()
        {
            if (!KeepAlive)
                return false;

            var maximumKeepAliveRequests =
                _response.MaximumKeepAliveRequests ?? _maximumKeepAliveRequests;

            if (_keepAliveRequestCount >= maximumKeepAliveRequests)
                return true;

            var keepAliveTimeout =
                _response.KeepAliveTimeout ?? _keepAliveTimeout;

            var timeLimit = _whenConnectionIdle.AddMilliseconds(keepAliveTimeout);
            if (timeLimit < DateTime.Now)
                return true;

            return false;
        }

        private void SendRequestData(HttpMethod method)
        {
            var contentLength = 0L;
            var contentType = string.Empty;

            if (CanContainsRequestBody(method) && (_content != null))
            {
                contentType = _content.ContentType;
                contentLength = _content.CalculateContentLength();
            }

            var startingLine = GenerateStartingLine(method);
            var headers = GenerateHeaders(method, contentLength, contentType);

            var startingLineBytes = Encoding.ASCII.GetBytes(startingLine);
            var headersBytes = Encoding.ASCII.GetBytes(headers);

            _bytesSent = 0;
            _totalBytesSent = startingLineBytes.Length + headersBytes.Length + contentLength;

            _connectionCommonStream.Write(startingLineBytes, 0, startingLineBytes.Length);
            _connectionCommonStream.Write(headersBytes, 0, headersBytes.Length);

            var hasRequestBody = (_content != null) && (contentLength > 0);

            // Отправляем тело запроса, если оно не присутствует.
            if (hasRequestBody)
                _content.WriteTo(_connectionCommonStream);
        }

        private void ReceiveResponseHeaders(HttpMethod method)
        {
            _canReportBytesReceived = false;

            _bytesReceived = 0;
            _totalBytesReceived = _response.LoadResponse(method);

            _canReportBytesReceived = true;
        }

        private bool CanReconnect()
        {
            return Reconnect && (_reconnectCount < _reconnectLimit);
        }

        private HttpResponse ReconnectAfterFail()
        {
            Dispose();
            Thread.Sleep(_reconnectDelay);

            _reconnectCount++;
            return Request(_method, Address, _content);
        }

        private HttpResponse KeepAliveReconect()
        {
            Dispose();
            _keepAliveReconnected = true;
            return Request(_method, Address, _content);
        }

        private void CheckStatusCode(HttpStatusCode statusCode)
        {
            var statusCodeNum = (int)statusCode;

            if ((statusCodeNum >= 400) && (statusCodeNum < 500))
            {
                throw new HttpException(string.Format(
                    Resources.HttpException_ClientError, statusCodeNum),
                    HttpExceptionStatus.ProtocolError, _response.StatusCode);
            }

            if (statusCodeNum >= 500)
            {
                throw new HttpException(string.Format(
                    Resources.HttpException_SeverError, statusCodeNum),
                    HttpExceptionStatus.ProtocolError, _response.StatusCode);
            }
        }

        private bool CanContainsRequestBody(HttpMethod method)
        {
            return
                (method == HttpMethod.PUT) ||
                (method == HttpMethod.POST) ||
                (method == HttpMethod.DELETE);
        }

        #endregion

        #region Создание подключения

        private ProxyClient GetProxy()
        {
            if (DisableProxyForLocalAddress)
            {
                try
                {
                    var checkIp = IPAddress.Parse("127.0.0.1");
                    IPAddress[] ips = Dns.GetHostAddresses(Address.Host);

                    foreach (var ip in ips)
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

        private TcpClient CreateTcpConnection(string host, int port)
        {
            TcpClient tcpClient;

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
                            try
                            {
                                tcpClient.EndConnect(ar);
                            }
                            catch (Exception ex)
                            {
                                connectException = ex;
                            }

                            connectDoneEvent.Set();
                        }), tcpClient
                    );
                }
                #region Catch's

                catch (Exception ex)
                {
                    tcpClient.Close();

                    if (ex is SocketException || ex is SecurityException)
                    {
                        throw NewHttpException(Resources.HttpException_FailedConnect, ex, HttpExceptionStatus.ConnectFailure);
                    }

                    throw;
                }

                #endregion

                if (!connectDoneEvent.Wait(_connectTimeout))
                {
                    tcpClient.Close();
                    throw NewHttpException(Resources.HttpException_ConnectTimeout, null, HttpExceptionStatus.ConnectFailure);
                }

                if (connectException != null)
                {
                    tcpClient.Close();

                    if (connectException is SocketException)
                    {
                        throw NewHttpException(Resources.HttpException_FailedConnect, connectException, HttpExceptionStatus.ConnectFailure);
                    }

                    throw connectException;
                }

                if (!tcpClient.Connected)
                {
                    tcpClient.Close();
                    throw NewHttpException(Resources.HttpException_FailedConnect, null, HttpExceptionStatus.ConnectFailure);
                }

                #endregion

                tcpClient.SendTimeout = _readWriteTimeout;
                tcpClient.ReceiveTimeout = _readWriteTimeout;
            }
            else
            {
                try
                {
                    tcpClient = _currentProxy.CreateConnection(host, port);
                }
                catch (ProxyException ex)
                {
                    throw NewHttpException(Resources.HttpException_FailedConnect, ex, HttpExceptionStatus.ConnectFailure);
                }
            }

            return tcpClient;
        }

        private void CreateConnection(Uri address)
        {
            _connection = CreateTcpConnection(address.Host, address.Port);
            _connectionNetworkStream = _connection.GetStream();

            // Если требуется безопасное соединение.
            if (address.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    SslStream sslStream;

                    if (SslCertificateValidatorCallback == null)
                    {
                        sslStream = new SslStream(_connectionNetworkStream, false, Http.AcceptAllCertificationsCallback);
                    }
                    else
                    {
                        sslStream = new SslStream(_connectionNetworkStream, false, SslCertificateValidatorCallback);
                    }

                    sslStream.AuthenticateAsClient(address.Host);
                    _connectionCommonStream = sslStream;
                }
                catch (Exception ex)
                {
                    if (ex is IOException || ex is AuthenticationException)
                    {
                        throw NewHttpException(Resources.HttpException_FailedSslConnect, ex, HttpExceptionStatus.ConnectFailure);
                    }

                    throw;
                }
            }
            else
            {
                _connectionCommonStream = _connectionNetworkStream;
            }

            if (_uploadProgressChangedHandler != null ||
                _downloadProgressChangedHandler != null)
            {
                var httpWraperStream = new HttpWraperStream(
                    _connectionCommonStream, _connection.SendBufferSize);

                if (_uploadProgressChangedHandler != null)
                {
                    httpWraperStream.BytesWriteCallback = ReportBytesSent;
                }

                if (_downloadProgressChangedHandler != null)
                {
                    httpWraperStream.BytesReadCallback = ReportBytesReceived;
                }

                _connectionCommonStream = httpWraperStream;
            }
        }

        #endregion

        #region Формирование данных запроса

        private string GenerateStartingLine(HttpMethod method)
        {
            string query;

            if (_currentProxy != null &&
                (_currentProxy.Type == ProxyType.Http || _currentProxy.Type == ProxyType.Chain))
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

        // Есть 3 типа заголовков, которые могут перекрываться другими. Вот порядок их установки:
        // - заголовки, которы задаются через специальные свойства, либо автоматически
        // - заголовки, которые задаются через индексатор
        // - временные заголовки, которые задаются через метод AddHeader
        private string GenerateHeaders(HttpMethod method, long contentLength = 0, string contentType = null)
        {
            var headers = GenerateCommonHeaders(method, contentLength, contentType);

            MergeHeaders(headers, _permanentHeaders);

            if (_temporaryHeaders != null && _temporaryHeaders.Count > 0)
                MergeHeaders(headers, _temporaryHeaders);

            if (Cookies != null && Cookies.Count != 0 && !headers.ContainsKey("Cookie"))
                headers["Cookie"] = Cookies.ToString();

            return ToHeadersString(headers);
        }

        private Dictionary<string, string> GenerateCommonHeaders(HttpMethod method, long contentLength = 0, string contentType = null)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            #region Host

            if (Address.IsDefaultPort)
                headers["Host"] = Address.Host;
            else
                headers["Host"] = string.Format("{0}:{1}", Address.Host, Address.Port);

            #endregion

            #region Connection и Authorization

            HttpProxyClient httpProxy = null;

            if (_currentProxy != null && _currentProxy.Type == ProxyType.Http)
            {
                httpProxy = _currentProxy as HttpProxyClient;
            }
            else if (_currentProxy != null && _currentProxy.Type == ProxyType.Chain)
            {
                httpProxy = FindHttpProxyInChain(_currentProxy as ChainProxyClient);
            }

            if (httpProxy != null)
            {
                if (KeepAlive)
                    headers["Proxy-Connection"] = "keep-alive";
                else
                    headers["Proxy-Connection"] = "close";

                if (!string.IsNullOrEmpty(httpProxy.Username) ||
                    !string.IsNullOrEmpty(httpProxy.Password))
                {
                    headers["Proxy-Authorization"] = GetProxyAuthorizationHeader(httpProxy);
                }
            }
            else
            {
                if (KeepAlive)
                    headers["Connection"] = "keep-alive";
                else
                    headers["Connection"] = "close";
            }

            if (!string.IsNullOrEmpty(Username) || !string.IsNullOrEmpty(Password))
            {
                headers["Authorization"] = GetAuthorizationHeader();
            }

            #endregion

            #region Content

            if (EnableEncodingContent)
                headers["Accept-Encoding"] = "gzip,deflate";

            if (Culture != null)
                headers["Accept-Language"] = GetLanguageHeader();

            if (CharacterSet != null)
                headers["Accept-Charset"] = GetCharsetHeader();

            if (CanContainsRequestBody(method))
            {
                if (contentLength > 0)
                {
                    headers["Content-Type"] = contentType;
                }

                headers["Content-Length"] = contentLength.ToString();
            }

            #endregion

            return headers;
        }

        #region Работа с заголовками

        private string GetAuthorizationHeader()
        {
            string data = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                string.Format("{0}:{1}", Username, Password)));

            return string.Format("Basic {0}", data);
        }

        private string GetProxyAuthorizationHeader(HttpProxyClient httpProxy)
        {
            string data = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                string.Format("{0}:{1}", httpProxy.Username, httpProxy.Password)));

            return string.Format("Basic {0}", data);
        }

        private string GetLanguageHeader()
        {
            string cultureName;

            if (Culture != null)
                cultureName = Culture.Name;
            else
                cultureName = CultureInfo.CurrentCulture.Name;

            if (cultureName.StartsWith("en"))
                return cultureName;

            return string.Format("{0},{1};q=0.8,en-US;q=0.6,en;q=0.4",
                cultureName, cultureName.Substring(0, 2));
        }

        private string GetCharsetHeader()
        {
            if (CharacterSet == Encoding.UTF8)
            {
                return "utf-8;q=0.7,*;q=0.3";
            }

            string charsetName;

            if (CharacterSet == null)
            {
                charsetName = Encoding.Default.WebName;
            }
            else
            {
                charsetName = CharacterSet.WebName;
            }

            return string.Format("{0},utf-8;q=0.7,*;q=0.3", charsetName);
        }

        private void MergeHeaders(Dictionary<string, string> destination, Dictionary<string, string> source)
        {
            foreach (var sourceItem in source)
            {
                destination[sourceItem.Key] = sourceItem.Value;
            }
        }

        #endregion

        private HttpProxyClient FindHttpProxyInChain(ChainProxyClient chainProxy)
        {
            HttpProxyClient foundProxy = null;

            // Ищем HTTP-прокси во всех цепочках прокси.
            // В приоритете найти прокси, который требует авторизацию.
            foreach (var proxy in chainProxy.Proxies)
            {
                if (proxy.Type == ProxyType.Http)
                {
                    foundProxy = proxy as HttpProxyClient;

                    if (!string.IsNullOrEmpty(foundProxy.Username) ||
                        !string.IsNullOrEmpty(foundProxy.Password))
                    {
                        return foundProxy;
                    }
                }
                else if (proxy.Type == ProxyType.Chain)
                {
                    HttpProxyClient foundDeepProxy =
                        FindHttpProxyInChain(proxy as ChainProxyClient);

                    if (foundDeepProxy != null &&
                        (!string.IsNullOrEmpty(foundDeepProxy.Username) ||
                        !string.IsNullOrEmpty(foundDeepProxy.Password)))
                    {
                        return foundDeepProxy;
                    }
                }
            }

            return foundProxy;
        }

        private string ToHeadersString(Dictionary<string, string> headers)
        {
            var headersBuilder = new StringBuilder();
            foreach (var header in headers)
            {
                headersBuilder.AppendFormat("{0}: {1}\r\n", header.Key, header.Value);
            }

            headersBuilder.AppendLine();
            return headersBuilder.ToString();
        }

        #endregion

        // Сообщает о том, сколько байт было отправлено HTTP-серверу.
        private void ReportBytesSent(int bytesSent)
        {
            _bytesSent += bytesSent;

            OnUploadProgressChanged(
                new UploadProgressChangedEventArgs(_bytesSent, _totalBytesSent));
        }

        // Сообщает о том, сколько байт было принято от HTTP-сервера.
        private void ReportBytesReceived(int bytesReceived)
        {
            _bytesReceived += bytesReceived;

            if (_canReportBytesReceived)
            {
                OnDownloadProgressChanged(
                    new DownloadProgressChangedEventArgs(_bytesReceived, _totalBytesReceived));
            }
        }

        // Проверяет, можно ли задавать этот заголовок.
        private bool IsClosedHeader(string name)
        {
            return _closedHeaders.Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        private void ClearRequestData()
        {
            _content = null;

            _temporaryUrlParams = null;
            _temporaryParams = null;
            _temporaryMultipartContent = null;
            _temporaryHeaders = null;
        }

        private HttpException NewHttpException(string message,
            Exception innerException = null, HttpExceptionStatus status = HttpExceptionStatus.Other)
        {
            return new HttpException(string.Format(message, Address.Host), status, HttpStatusCode.None, innerException);
        }

        #endregion
    }
}