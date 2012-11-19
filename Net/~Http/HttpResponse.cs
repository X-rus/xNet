using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using xNet.Text;

namespace xNet.Net
{
    /// <summary>
    /// Представляет класс, предназначеннный для приёма ответа от HTTP-сервера.
    /// </summary>
    public sealed class HttpResponse
    {
        #region Классы (закрытые)

        private sealed class BytesWraper
        {
            public int Length { get; set; }

            public byte[] Value { get; set; }
        }

        // Данный класс используется для загрузки начальных данных.
        // Но он также используется и для загрузки тела сообщения, точнее, из него просто выгружается остаток данных, полученный при загрузки начальных данных.
        private sealed class ReceiverHelper
        {
            private const int InitialLineSize = 1000;


            #region Поля (закрытые)

            private Stream _stream;

            private byte[] _buffer;
            private int _bufferSize;

            private int _linePosition;
            private byte[] _lineBuffer = new byte[InitialLineSize];

            #endregion


            #region Свойства (открытые)

            public bool HasData
            {
                get
                {
                    return (Length - Position) != 0;
                }
            }

            public int Length { get; private set; }

            public int Position { get; private set; }

            #endregion


            public ReceiverHelper(Stream stream, int bufferSize)
            {
                _stream = stream;
                _bufferSize = bufferSize;

                _buffer = new byte[_bufferSize];
            }


            #region Методы (открытые)

            public string ReadLine()
            {
                _linePosition = 0;

                while (true)
                {
                    if (Position == Length)
                    {
                        Position = 0;
                        Length = _stream.Read(_buffer, 0, _bufferSize);

                        if (Length == 0)
                        {
                            break;
                        }
                    }

                    byte b = _buffer[Position++];

                    _lineBuffer[_linePosition++] = b;

                    // Если считан символ '\n'.
                    if (b == 10)
                    {
                        break;
                    }

                    // Если достигнут максимальный предел размера буфера линии.
                    if (_linePosition == _lineBuffer.Length)
                    {
                        // Увеличиваем размер буфера линии в два раза.
                        byte[] newLineBuffer = new byte[_lineBuffer.Length * 2];

                        _lineBuffer.CopyTo(newLineBuffer, 0);
                        _lineBuffer = newLineBuffer;
                    }
                }

                return Encoding.ASCII.GetString(_lineBuffer, 0, _linePosition);
            }

            public int Read(byte[] buffer, int index, int length)
            {
                int curLength = Length - Position;

                if (curLength > length)
                {
                    curLength = length;
                }

                Array.Copy(_buffer, Position, buffer, index, curLength);

                Position += curLength;

                return curLength;
            }

            #endregion
        }

        // Данный класс используется при загрузки сжатых данных.
        // Он позволяет определить точное количество считаных байт. А потоки для считывания сжатых данных сообщают количество байт уже преобразованных данных.
        private sealed class StreamWrapper : Stream
        {
            #region Поля (закрытые)

            private Stream _baseStream;
            private ReceiverHelper _receiverHelper;

            #endregion


            #region Свойства (открытые)

            public int BytesRead { get; private set; }

            public int TotalBytesRead { get; set; }

            public int LimitBytesRead { get; set; }

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

            public StreamWrapper(Stream baseStream, ReceiverHelper receiverHelper)
            {
                _baseStream = baseStream;
                _receiverHelper = receiverHelper;
            }


            #region Методы (открытые)

            public override void Flush()
            {
                _baseStream.Flush();
            }

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
                // Если установлен лимит на количество считанных байт.
                if (LimitBytesRead != 0)
                {
                    int length = LimitBytesRead - TotalBytesRead;

                    // Если лимит достигнут.
                    if (length == 0)
                    {
                        return 0;
                    }

                    if (length > buffer.Length)
                    {
                        length = buffer.Length;
                    }

                    if (_receiverHelper.HasData)
                    {
                        BytesRead = _receiverHelper.Read(buffer, offset, length);
                    }
                    else
                    {
                        BytesRead = _baseStream.Read(buffer, offset, length);
                    }
                }
                else
                {
                    if (_receiverHelper.HasData)
                    {
                        BytesRead = _receiverHelper.Read(buffer, offset, count);
                    }
                    else
                    {
                        BytesRead = _baseStream.Read(buffer, offset, count);
                    }
                }

                TotalBytesRead += BytesRead;

                return BytesRead;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _baseStream.Write(buffer, offset, count);
            }

            #endregion
        }

        #endregion


        private readonly byte[] _signatureBytes = Encoding.ASCII.GetBytes("</html>");


        #region Поля (закрытые)

        private readonly HttpRequest _request;
        private ReceiverHelper _receiverHelper;

        private readonly Dictionary<string, string> _headers =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        #endregion


        #region Свойства (открытые)

        /// <summary>
        /// Возвращает значение, указывающие, произошла ли ошибка во время получения ответа от HTTP-сервера.
        /// </summary>
        public bool HasError { get; private set; }

        /// <summary>
        /// Возвращает значение, указывающие, загружено ли тело сообщения.
        /// </summary>
        public bool MessageBodyLoaded { get; private set; }

        /// <summary>
        /// Возвращает значение, указывающие, успешно ли выполнен запрос (код ответа = 200 OK). 
        /// </summary>
        public bool IsOK
        {
            get
            {
                return (StatusCode == HttpStatusCode.OK);
            }
        }

        /// <summary>
        /// Возвращает значение, указывающие, имеется ли переадресация (код ответа = 3xx).
        /// </summary>
        public bool HasRedirect
        {
            get
            {
                int numStatusCode = (int)StatusCode;

                return (numStatusCode >= 300 && numStatusCode < 400);
            }
        }

        #region Основные данные

        /// <summary>
        /// Возвращает URI интернет-ресурса, который фактически отвечал на запрос.
        /// </summary>
        public Uri Address { get; private set; }

        /// <summary>
        /// Возвращает HTTP-метод, используемый для получения ответа.
        /// </summary>
        public HttpMethod Method { get; private set; }

        /// <summary>
        /// Возвращает версию HTTP-протокола, используемую в ответе.
        /// </summary>
        public Version ProtocolVersion { get; private set; }

        /// <summary>
        /// Возвращает код состояния ответа.
        /// </summary>
        public HttpStatusCode StatusCode { get; private set; }

        #endregion

        #region HTTP-заголовки

        /// <summary>
        /// Возвращает кодировку тела сообщения.
        /// </summary>
        /// <value>Значение заголовка, если такой заголок задан, иначе значение заданное в <see cref="xNet.Net.HttpRequest"/>. Если и оно не задано, то значение <see cref="System.Text.Encoding.Default"/>.</value>
        public Encoding CharacterSet { get; private set; }

        /// <summary>
        /// Возвращает длину тела сообщения.
        /// </summary>
        /// <value>Значение заголовка, если такой заголок задан, иначе -1.</value>
        public int ContentLength { get; private set; }

        /// <summary>
        /// Возвращает тип содержимого ответа.
        /// </summary>
        /// <value>Значение заголовка, если такой заголок задан, иначе пустая строка.</value>
        public string ContentType { get; private set; }

        /// <summary>
        /// Возвращает значение HTTP-заголовка 'Location'.
        /// </summary>
        /// <returns>Значение заголовка, если такой заголок задан, иначе пустая строка.</returns>
        public string Location
        {
            get
            {
                return this["Location"];
            }
        }

        /// <summary>
        /// Возвращает кукисы, образовавшиеся в результате запроса, или установленные в <see cref="xNet.Net.HttpRequest"/>.
        /// </summary>
        /// <remarks>Если кукиксы были установлены в <see cref="xNet.Net.HttpRequest"/> и значение свойства <see cref="xNet.Net.CookieDictionary.IsLocked"/> равно <see langword="true"/>, то будут созданы новые кукиксы.</remarks>
        public CookieDictionary Cookies { get; private set; }

        #endregion

        #endregion


        /// <summary>
        /// Возвращает значение HTTP-заголовка.
        /// </summary>
        /// <param name="headerName">Название заголовка.</param>
        /// <returns>Значение заголовка, если такой заголок задан, иначе пустая строка.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="headerName"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="headerName"/> является пустой строкой.</exception>
        public string this[string headerName]
        {
            get
            {
                #region Проверка параметра

                if (headerName == null)
                {
                    throw new ArgumentNullException("headerName");
                }

                if (string.IsNullOrEmpty(headerName))
                {
                    throw ExceptionHelper.EmptyString("headerName");
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
        }


        internal HttpResponse(HttpRequest request)
        {
            _request = request;

            ContentLength = -1;
            ContentType = string.Empty;
        }


        #region Методы (открытые)

        /// <summary>
        /// Загружает тело сообщения и возвращает его в виде байтов.
        /// </summary>
        /// <returns>Если тело сообщения отсутствует, или оно уже было загружено, то будет возвращён пустой массив байтов.</returns>
        /// <exception cref="System.InvalidOperationException">Вызов метода из ошибочного ответа.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public byte[] ToBytes()
        {
            #region Проверка состояния

            if (HasError)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_HttpResponse_HasError);
            }

            #endregion

            if (MessageBodyLoaded)
            {
                return new byte[0];
            }

            var memoryStream = new MemoryStream(
                (ContentLength == -1) ? 0 : ContentLength);

            try
            {
                IEnumerable<BytesWraper> source = GetMessageBodySource();

                foreach (BytesWraper bytes in source)
                {
                    memoryStream.Write(bytes.Value, 0, bytes.Length);
                    _request.ReportBytesReceived(bytes.Length, ContentLength);
                }
            }
            catch (Exception ex)
            {
                HasError = true;

                if (ex is IOException || ex is InvalidOperationException)
                {
                    throw NewHttpException(Resources.HttpException_FailedReceiveMessageBody, ex);
                }

                throw;
            }
            finally
            {
                _receiverHelper = null;
            }

            if (ConnectionClosed())
            {
                _request.Dispose();
            }

            MessageBodyLoaded = true;

            return memoryStream.ToArray();
        }

        /// <summary>
        /// Загружает тело сообщения и возвращает его в виде текста.
        /// </summary>
        /// <returns>Если тело сообщения отсутствует, или оно уже было загружено, то будет возвращена пустая строка.</returns>
        /// <exception cref="System.InvalidOperationException">Вызов метода из ошибочного ответа.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        override public string ToString()
        {
            #region Проверка состояния

            if (HasError)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_HttpResponse_HasError);
            }

            #endregion

            if (MessageBodyLoaded)
            {
                return string.Empty;
            }

            var memoryStream = new MemoryStream(
                (ContentLength == -1) ? 0 : ContentLength);

            try
            {
                IEnumerable<BytesWraper> source = GetMessageBodySource();

                foreach (BytesWraper bytes in source)
                {
                    memoryStream.Write(bytes.Value, 0, bytes.Length);
                    _request.ReportBytesReceived(bytes.Length, ContentLength);
                }
            }
            catch (Exception ex)
            {
                HasError = true;

                if (ex is IOException || ex is InvalidOperationException)
                {
                    throw NewHttpException(Resources.HttpException_FailedReceiveMessageBody, ex);
                }

                throw;
            }
            finally
            {
                _receiverHelper = null;
            }

            if (ConnectionClosed())
            {
                _request.Dispose();
            }

            MessageBodyLoaded = true;

            string text = CharacterSet.GetString(
                memoryStream.GetBuffer(), 0, (int)memoryStream.Length);

            return text;
        }

        /// <summary>
        /// Загружает тело сообщения и сохраняет его в новый файл по указанному пути. Если файл уже существует, то он будет перезаписан.
        /// </summary>
        /// <param name="path">Путь к файлу, в котором будет сохранено тело сообщения.</param>
        /// <exception cref="System.InvalidOperationException">Вызов метода из ошибочного ответа.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="path"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="path"/> является пустой строкой, содержит только пробелы или содержит недопустимые символы.</exception>
        /// <exception cref="System.IO.PathTooLongException">Указанный путь, имя файла или и то и другое превышает наибольшую возможную длину, определенную системой. Например, для платформ на основе Windows длина пути не должна превышать 248 знаков, а имена файлов не должны содержать более 260 знаков.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Значение параметра <paramref name="path"/> указывает на несуществующий файл.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">Значение параметра <paramref name="path"/> указывает на недопустимый путь.</exception>
        /// <exception cref="System.IO.IOException">При открытии файла возникла ошибка ввода-вывода.</exception>
        /// <exception cref="System.Security.SecurityException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Операция чтения файла не поддерживается на текущей платформе.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Значение параметра <paramref name="path"/> определяет каталог.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public void ToFile(string path)
        {
            #region Проверка состояния

            if (HasError)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_HttpResponse_HasError);
            }

            #endregion

            #region Проверка параметров

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            #endregion

            if (MessageBodyLoaded)
            {
                return;
            }

            try
            {
                using (var fStream = new FileStream(path, FileMode.Create))
                {
                    IEnumerable<BytesWraper> source = GetMessageBodySource();

                    foreach (BytesWraper bytes in source)
                    {
                        fStream.Write(bytes.Value, 0, bytes.Length);
                        _request.ReportBytesReceived(bytes.Length, ContentLength);
                    }
                }
            }
            #region Catch's

            catch (ArgumentException ex)
            {
                throw ExceptionHelper.WrongPath("path", ex);
            }
            catch (NotSupportedException ex)
            {
                throw ExceptionHelper.WrongPath("path", ex);
            }
            catch (Exception ex)
            {
                HasError = true;

                if (ex is IOException || ex is InvalidOperationException)
                {
                    throw NewHttpException(Resources.HttpException_FailedReceiveMessageBody, ex);
                }

                throw;
            }

            #endregion
            finally
            {
                _receiverHelper = null;
            }

            if (ConnectionClosed())
            {
                _request.Dispose();
            }

            MessageBodyLoaded = true;
        }

        /// <summary>
        /// Пропускает тело сообщения. Данный метод следует вызвать, если не требуется тело сообщения.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Вызов метода из ошибочного ответа.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public void None()
        {
            #region Проверка состояния

            if (HasError)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_HttpResponse_HasError);
            }

            #endregion

            if (MessageBodyLoaded)
            {
                return;
            }

            if (ConnectionClosed())
            {
                _request.Dispose();
                _receiverHelper = null;
            }
            else
            {
                try
                {
                    IEnumerable<BytesWraper> source = GetMessageBodySource();

                    foreach (BytesWraper bytes in source)
                    {
                        _request.ReportBytesReceived(bytes.Length, ContentLength);
                    }
                }
                catch (Exception ex)
                {
                    HasError = true;

                    if (ex is IOException || ex is InvalidOperationException)
                    {
                        throw NewHttpException(Resources.HttpException_FailedReceiveMessageBody, ex);
                    }

                    throw;
                }
                finally
                {
                    _receiverHelper = null;
                }
            }

            MessageBodyLoaded = true;
        }

        #endregion


        internal void LoadResponse(HttpMethod method)
        {
            HasError = false;
            Address = _request.Address;
            Method = method;

            MessageBodyLoaded = false;

            _receiverHelper = new ReceiverHelper(
                _request.ClientStream, _request.TcpClient.ReceiveBufferSize);

            try
            {
                WaitData();

                ReceiveStartingLine();

                _headers.Clear();

                if (_request.Cookies != null && !_request.Cookies.IsLocked)
                {
                    Cookies = _request.Cookies;
                }
                else
                {
                    Cookies = new CookieDictionary();
                }

                ReceiveHeaders();

                CharacterSet = GetCharacterSet();
                ContentLength = GetContentLength();
                ContentType = GetContentType();
            }
            catch (Exception ex)
            {
                HasError = true;

                if (ex is IOException)
                {
                    throw NewHttpException(
                        Resources.HttpException_FailedReceiveResponse, ex);
                }

                throw;
            }

            // Если пришёл ответ без тела сообщения.
            if (Method == HttpMethod.HEAD || Method == HttpMethod.DELETE ||
                StatusCode == HttpStatusCode.Continue || StatusCode == HttpStatusCode.NoContent ||
                StatusCode == HttpStatusCode.NotModified)
            {
                MessageBodyLoaded = true;
            }
        }


        #region Методы (закрытые)

        #region Загрузка начальных данных

        private void ReceiveStartingLine()
        {
            string startingLine;

            while (true)
            {
                startingLine = _receiverHelper.ReadLine();

                if (startingLine.Length == 0)
                {
                    HttpException exception =
                        NewHttpException(Resources.HttpException_ReceivedEmptyResponse);

                    exception.EmptyMessageBody = true;

                    throw exception;
                }
                else if (startingLine.Equals(
                    HttpHelper.NewLine, StringComparison.Ordinal))
                {
                    continue;
                }
                else
                {
                    break;
                }
            }

            string version = startingLine.Substring("HTTP/", " ");
            string statusCode = startingLine.Substring(" ", " ");

            if (version.Length == 0 || statusCode.Length == 0)
            {
                throw NewHttpException(Resources.HttpException_ReceivedEmptyResponse);
            }

            ProtocolVersion = Version.Parse(version);

            StatusCode = (HttpStatusCode)Enum.Parse(
                typeof(HttpStatusCode), statusCode);
        }

        private void ReceiveHeaders()
        {
            DateTime dateTimeNow = DateTime.Now;

            while (true)
            {
                string header = _receiverHelper.ReadLine();

                // Если достигнут конец заголовков.
                if (header.Equals(
                    HttpHelper.NewLine, StringComparison.Ordinal))
                {
                    return;
                }

                // Ищем позицию между именем и значением заголовка.
                int separatorPos = header.IndexOf(':');

                if (separatorPos == -1)
                {
                    string message = string.Format(
                        Resources.HttpException_WrongHeader, header, Address.Host);

                    throw NewHttpException(message);
                }

                string headerName = header.Substring(0, separatorPos);
                string headerValue = header.Substring(separatorPos + 1).Trim(' ', '\t', '\r', '\n');

                if (headerName.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
                {
                    #region Устанавливаем кукикс

                    // Ищем позицию, где заканчивается кукис и начинается описание его параметров.
                    int endCookiePos = headerValue.IndexOf(';');

                    // Ищем позицию между именем и значением кукикса.
                    separatorPos = headerValue.IndexOf('=');

                    if (separatorPos == -1)
                    {
                        string message = string.Format(
                            Resources.HttpException_WrongCookie, headerValue, Address.Host);

                        throw NewHttpException(message);
                    }

                    string cookieValue;
                    string cookieName = headerValue.Substring(0, separatorPos);

                    if (endCookiePos == -1)
                    {
                        cookieValue = headerValue.Substring(separatorPos + 1);
                    }
                    else
                    {
                        cookieValue = headerValue.Substring(separatorPos + 1,
                            (endCookiePos - separatorPos) - 1);

                        #region Получаем время, которое кукис будет действителен и проверяем его

                        int expiresPos = headerValue.IndexOf("expires=");

                        if (expiresPos != -1)
                        {
                            string expiresStr;
                            int endExpiresPos = headerValue.IndexOf(';', expiresPos);

                            expiresPos += 8;

                            if (endExpiresPos == -1)
                            {
                                expiresStr = headerValue.Substring(expiresPos);
                            }
                            else
                            {
                                expiresStr = headerValue.Substring(expiresPos, endExpiresPos - expiresPos);
                            }

                            DateTime expires;

                            if (DateTime.TryParse(expiresStr, out expires) &&
                                expires < dateTimeNow)
                            {
                                Cookies.Remove(cookieName);
                                continue;
                            }
                        }

                        #endregion
                    }

                    // Если кукис нужно удалить.
                    if (cookieValue.Length == 0 ||
                        cookieValue.Equals("deleted", StringComparison.OrdinalIgnoreCase))
                    {
                        Cookies.Remove(cookieName);
                    }
                    else
                    {
                        Cookies[cookieName] = cookieValue;
                    }

                    #endregion
                }
                else
                {
                    _headers[headerName] = headerValue;
                }
            }
        }

        #endregion

        #region Загрузка тела сообщения

        private IEnumerable<BytesWraper> GetMessageBodySource()
        {
            if (_headers.ContainsKey("Content-Encoding"))
            {
                return GetMessageBodySourceZip();
            }

            return GetMessageBodySourceStd();
        }

        // Приём обычных данных.
        private IEnumerable<BytesWraper> GetMessageBodySourceStd()
        {
            if (_headers.ContainsKey("Transfer-Encoding"))
            {
                return ReceiveMessageBodyChunked();
            }

            if (ContentLength != -1)
            {
                return ReceiveMessageBody(ContentLength);
            }

            bool isHtml;

            if (ContentType.Equals("text/html", StringComparison.OrdinalIgnoreCase))
            {
                isHtml = true;
            }
            else
            {
                isHtml = false;
            }

            return ReceiveMessageBody(_request.ClientStream, isHtml);
        }

        // Приём сжатых данных.
        private IEnumerable<BytesWraper> GetMessageBodySourceZip()
        {
            if (_headers.ContainsKey("Transfer-Encoding"))
            {
                return ReceiveMessageBodyChunkedZip();
            }

            if (ContentLength != -1)
            {
                return ReceiveMessageBodyZip(ContentLength);
            }

            bool isHtml;

            if (ContentType.Equals("text/html", StringComparison.OrdinalIgnoreCase))
            {
                isHtml = true;
            }
            else
            {
                isHtml = false;
            }

            var streamWrapper = new StreamWrapper(
                _request.ClientStream, _receiverHelper);

            return ReceiveMessageBody(GetZipStream(streamWrapper), isHtml);
        }

        // Считывания тела сообщения неизвестной длины.
        private IEnumerable<BytesWraper> ReceiveMessageBody(Stream stream, bool isHtml)
        {
            var bytesWraper = new BytesWraper();

            int bufferSize = _request.TcpClient.ReceiveBufferSize;
            byte[] buffer = new byte[bufferSize];

            bytesWraper.Value = buffer;

            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, bufferSize);

                #region Проверка на конец тела сообщения

                // Если тело сообщения содержит HTML.
                if (isHtml)
                {
                    if (bytesRead == 0)
                    {
                        WaitData();

                        continue;
                    }

                    bool found = false;
                    int newBufferLength = (buffer.Length - _signatureBytes.Length) + 1;

                    #region Поиск сигнатуры

                    for (int bufferIndex = 0; bufferIndex < newBufferLength && !found; ++bufferIndex)
                    {
                        if (buffer[bufferIndex] == 0)
                        {
                            continue;
                        }

                        for (int signatureIndex = 0; signatureIndex < _signatureBytes.Length; ++signatureIndex)
                        {
                            byte b = buffer[bufferIndex + signatureIndex];

                            // Если достигнуты буквы сигнатуры.
                            if (signatureIndex >= 2)
                            {
                                char c = (char)b;

                                // Приводим символ к нижнему регистру для правильного сравнения.
                                b = (byte)char.ToLower(c);
                            }

                            if (_signatureBytes[signatureIndex] == b)
                            {
                                found = true;
                            }
                            else
                            {
                                found = false;
                                break;
                            }
                        }
                    }

                    #endregion

                    if (found)
                    {
                        bytesWraper.Length = bytesRead;
                        yield return bytesWraper;

                        yield break;
                    }
                }
                else
                {
                    if (bytesRead == 0)
                    {
                        yield break;
                    }
                }

                #endregion

                bytesWraper.Length = bytesRead;
                yield return bytesWraper;
            }
        }

        // Считывание тела сообщения известной длины.
        private IEnumerable<BytesWraper> ReceiveMessageBody(int contentLength)
        {
            Stream stream = _request.ClientStream;
            var bytesWraper = new BytesWraper();

            int bufferSize = _request.TcpClient.ReceiveBufferSize;
            byte[] buffer = new byte[bufferSize];

            bytesWraper.Value = buffer;

            int totalBytesRead = 0;

            while (totalBytesRead != contentLength)
            {
                int bytesRead;

                if (_receiverHelper.HasData)
                {
                    bytesRead = _receiverHelper.Read(buffer, 0, bufferSize);
                }
                else
                {
                    bytesRead = stream.Read(buffer, 0, bufferSize);
                }

                if (bytesRead == 0)
                {
                    WaitData();
                }
                else
                {
                    totalBytesRead += bytesRead;

                    bytesWraper.Length = bytesRead;
                    yield return bytesWraper;
                }
            }
        }

        private IEnumerable<BytesWraper> ReceiveMessageBodyChunked()
        {
            Stream stream = _request.ClientStream;
            var bytesWraper = new BytesWraper();

            int bufferSize = _request.TcpClient.ReceiveBufferSize;
            byte[] buffer = new byte[bufferSize];

            bytesWraper.Value = buffer;

            while (true)
            {
                string line = _receiverHelper.ReadLine();

                // Если достигнут конец блока.
                if (line.Equals(
                    HttpHelper.NewLine, StringComparison.Ordinal))
                {
                    continue;
                }

                line = line.Trim(' ', '\r', '\n');

                // Если достигнут конец тела сообщения.
                if (line.Equals("0", StringComparison.Ordinal))
                {
                    yield break;
                }

                int blockLength;
                int totalBytesRead = 0;

                #region Задаём длину блока

                try
                {
                    blockLength = Convert.ToInt32(line, 16);
                }
                catch (Exception ex)
                {
                    if (ex is FormatException || ex is OverflowException)
                    {
                        throw NewHttpException(string.Format(
                            Resources.HttpException_WrongChunkedBlockLength, line), ex);
                    }

                    throw;
                }

                #endregion

                while (totalBytesRead != blockLength)
                {
                    int length = blockLength - totalBytesRead;

                    if (length > bufferSize)
                    {
                        length = bufferSize;
                    }

                    int bytesRead;

                    if (_receiverHelper.HasData)
                    {
                        bytesRead = _receiverHelper.Read(buffer, 0, length);
                    }
                    else
                    {
                        bytesRead = stream.Read(buffer, 0, length);
                    }

                    if (bytesRead == 0)
                    {
                        WaitData();
                    }
                    else
                    {
                        totalBytesRead += bytesRead;

                        bytesWraper.Length = bytesRead;
                        yield return bytesWraper;
                    }
                }
            }
        }

        private IEnumerable<BytesWraper> ReceiveMessageBodyZip(int contentLength)
        {
            var bytesWraper = new BytesWraper();
            var streamWrapper = new StreamWrapper(
                _request.ClientStream, _receiverHelper);

            using (Stream stream = GetZipStream(streamWrapper))
            {
                int bufferSize = _request.TcpClient.ReceiveBufferSize;
                byte[] buffer = new byte[bufferSize];

                bytesWraper.Value = buffer;

                while (true)
                {
                    int bytesRead = stream.Read(buffer, 0, bufferSize);

                    if (bytesRead == 0)
                    {
                        if (streamWrapper.TotalBytesRead == contentLength)
                        {
                            yield break;
                        }
                        else
                        {
                            WaitData();

                            continue;
                        }
                    }

                    bytesWraper.Length = bytesRead;
                    yield return bytesWraper;
                }
            }
        }

        private IEnumerable<BytesWraper> ReceiveMessageBodyChunkedZip()
        {
            var bytesWraper = new BytesWraper();
            var streamWrapper = new StreamWrapper
                (_request.ClientStream, _receiverHelper);

            using (Stream stream = GetZipStream(streamWrapper))
            {
                int bufferSize = _request.TcpClient.ReceiveBufferSize;
                byte[] buffer = new byte[bufferSize];

                bytesWraper.Value = buffer;

                while (true)
                {
                    string line = _receiverHelper.ReadLine();

                    // Если достигнут конец блока.
                    if (line.Equals(
                        HttpHelper.NewLine, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    line = line.Trim(' ', '\r', '\n');

                    // Если достигнут конец данных.
                    if (line.Equals("0", StringComparison.Ordinal))
                    {
                        yield break;
                    }

                    int blockLength;

                    #region Задаём длину блока

                    try
                    {
                        blockLength = Convert.ToInt32(line, 16);
                    }
                    catch (Exception ex)
                    {
                        if (ex is FormatException || ex is OverflowException)
                        {
                            throw NewHttpException(string.Format(
                                Resources.HttpException_WrongChunkedBlockLength, line), ex);
                        }

                        throw;
                    }

                    #endregion

                    streamWrapper.TotalBytesRead = 0;
                    streamWrapper.LimitBytesRead = blockLength;

                    while (true)
                    {
                        int bytesRead = stream.Read(buffer, 0, bufferSize);

                        if (bytesRead == 0)
                        {
                            if (streamWrapper.TotalBytesRead == blockLength)
                            {
                                break;
                            }
                            else
                            {
                                WaitData();

                                continue;
                            }
                        }

                        bytesWraper.Length = bytesRead;
                        yield return bytesWraper;
                    }
                }
            }
        }

        #endregion

        #region Получение значения HTTP-заголовков

        private bool ConnectionClosed()
        {
            if (_headers.ContainsKey("Connection") &&
                _headers["Connection"].Equals("close", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (_headers.ContainsKey("Proxy-Connection") &&
                _headers["Proxy-Connection"].Equals("close", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private Encoding GetCharacterSet()
        {
            if (!_headers.ContainsKey("Content-Type"))
            {
                return (_request.CharacterSet == null ? Encoding.Default : _request.CharacterSet);
            }

            string contentType = _headers["Content-Type"];

            // Пример текста, где ищется позиция символа: text/html; charset=UTF-8
            int charsetPos = contentType.IndexOf('=');

            if (charsetPos == -1)
            {
                return (_request.CharacterSet == null ? Encoding.Default : _request.CharacterSet);
            }

            contentType = contentType.Substring(charsetPos + 1);

            try
            {
                return Encoding.GetEncoding(contentType);
            }
            catch (ArgumentException)
            {
                return (_request.CharacterSet == null ? Encoding.Default : _request.CharacterSet);
            }
        }

        private int GetContentLength()
        {
            if (_headers.ContainsKey("Content-Length"))
            {
                int contentLength;
                int.TryParse(_headers["Content-Length"], out contentLength);

                return contentLength;
            }

            return -1;
        }

        private string GetContentType()
        {
            if (_headers.ContainsKey("Content-Type"))
            {
                string contentType = _headers["Content-Type"];

                // Ищем позицию, где заканчивается описание типа контента и начинается описание его параметров.
                int endTypePos = contentType.IndexOf(';');

                if (endTypePos != -1)
                {
                    contentType = contentType.Substring(0, endTypePos);
                }

                return contentType;
            }

            return string.Empty;
        }

        #endregion

        private void WaitData()
        {
            int sleepTime = 0;
            int delay = (_request.TcpClient.ReceiveTimeout < 10) ?
                10 : _request.TcpClient.ReceiveTimeout;

            while (!_request.ClientNetworkStream.DataAvailable)
            {
                if (sleepTime >= delay)
                {
                    throw NewHttpException(Resources.HttpException_WaitDataTimeout);
                }

                sleepTime += 10;
                System.Threading.Thread.Sleep(10);
            }
        }

        private Stream GetZipStream(Stream stream)
        {
            string contentEncoding = _headers["Content-Encoding"].ToLower();

            switch (contentEncoding)
            {
                case "gzip":
                    return new GZipStream(stream, CompressionMode.Decompress, true);

                case "deflate":
                    return new DeflateStream(stream, CompressionMode.Decompress, true);

                default:
                    throw new InvalidOperationException(string.Format(
                        Resources.InvalidOperationException_NotSupportedEncodingFormat, contentEncoding));
            }
        }

        private HttpException NewHttpException(string message,
            Exception innerException = null)
        {
            return new HttpException(string.Format(message, Address.Host), innerException);
        }

        #endregion
    }
}