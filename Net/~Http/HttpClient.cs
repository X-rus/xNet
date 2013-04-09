using System;
using xNet.Collections;

namespace xNet.Net
{
    /// <summary>
    /// Представляет статические класс, предназначенный для работы с HTTP-сервером.
    /// </summary>
    public static class HttpClient
    {
        #region Статические свойства (открытые)

        /// <summary>
        /// Возвращает или задаёт значение, указывающие, нужно ли использовать прокси-клиент Internet Explorer'a, если нет прямого подключения к интернету и не задан прокси-клиент.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="false"/>.</value>
        public static bool UseIeProxy
        {
            get
            {
                return HttpRequest.UseIeProxy;
            }
            set
            {
                HttpRequest.UseIeProxy = value;
            }
        }

        /// <summary>
        /// Возвращает или задаёт значение, указывающие, нужно ли отключать прокси-клиент для локальных адресов.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="false"/>.</value>
        public static bool DisableProxyForLocalAddress
        {
            get
            {
                return HttpRequest.DisableProxyForLocalAddress;
            }
            set
            {
                HttpRequest.DisableProxyForLocalAddress = value;
            }
        }

        /// <summary>
        /// Возвращает или задаёт глобальный прокси-клиент.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        public static ProxyClient GlobalProxy
        {
            get
            {
                return HttpRequest.GlobalProxy;
            }
            set
            {
                HttpRequest.GlobalProxy = value;
            }
        }

        #endregion


        #region Статические методы (открытые)

        #region Get

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="urlParams">Параметры URL-адреса, или значение <see langword="null"/>.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public static void Get(string address, RequestParams urlParams = null, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (var request = new HttpRequest())
            {
                if (cookies == null)
                {
                    request.Cookies = new CookieDictionary();
                }
                else
                {
                    request.Cookies = cookies;
                }

                request.Proxy = proxy;
                request.KeepAlive = false;
                request.UserAgent = HttpHelper.ChromeUserAgent();

                request.Get(address, urlParams).None();
            }
        }

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="urlParams">Параметры URL-адреса, или значение <see langword="null"/>.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public static void Get(Uri address, RequestParams urlParams = null, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (var request = new HttpRequest())
            {
                if (cookies == null)
                {
                    request.Cookies = new CookieDictionary();
                }
                else
                {
                    request.Cookies = cookies;
                }

                request.Proxy = proxy;
                request.KeepAlive = false;
                request.UserAgent = HttpHelper.ChromeUserAgent();

                request.Get(address, urlParams).None();
            }
        }

        #endregion

        #region GetString

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу. Загружает тело сообщения и возвращает его в виде строки.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="urlParams">Параметры URL-адреса, или значение <see langword="null"/>.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <returns>Если тело сообщения отсутствует, то будет возвращена пустая строка.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public static string GetString(string address, RequestParams urlParams = null, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (var request = new HttpRequest())
            {
                if (cookies == null)
                {
                    request.Cookies = new CookieDictionary();
                }
                else
                {
                    request.Cookies = cookies;
                }

                request.Proxy = proxy;
                request.KeepAlive = false;
                request.UserAgent = HttpHelper.ChromeUserAgent();

                return request.Get(address, urlParams).ToString();
            }
        }

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу. Загружает тело сообщения и возвращает его в виде строки.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="urlParams">Параметры URL-адреса, или значение <see langword="null"/>.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <returns>Если тело сообщения отсутствует, то будет возвращена пустая строка.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public static string GetString(Uri address, RequestParams urlParams = null, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (var request = new HttpRequest())
            {
                if (cookies == null)
                {
                    request.Cookies = new CookieDictionary();
                }
                else
                {
                    request.Cookies = cookies;
                }

                request.Proxy = proxy;
                request.KeepAlive = false;
                request.UserAgent = HttpHelper.ChromeUserAgent();

                return request.Get(address, urlParams).ToString();
            }
        }

        #endregion

        #region GetBytes

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу. Загружает тело сообщения и возвращает его в виде текста.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="urlParams">Параметры URL-адреса, или значение <see langword="null"/>.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <returns>Если тело сообщения отсутствует, то будет возвращён пустой массив байтов.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public static byte[] GetBytes(string address, RequestParams urlParams = null, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (var request = new HttpRequest())
            {
                if (cookies == null)
                {
                    request.Cookies = new CookieDictionary();
                }
                else
                {
                    request.Cookies = cookies;
                }

                request.Proxy = proxy;
                request.KeepAlive = false;
                request.UserAgent = HttpHelper.ChromeUserAgent();

                return request.Get(address, urlParams).ToBytes();
            }
        }

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу. Загружает тело сообщения и возвращает его в виде текста.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="urlParams">Параметры URL-адреса, или значение <see langword="null"/>.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <returns>Если тело сообщения отсутствует, то будет возвращён пустой массив байтов.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public static byte[] GetBytes(Uri address, RequestParams urlParams = null, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (var request = new HttpRequest())
            {
                if (cookies == null)
                {
                    request.Cookies = new CookieDictionary();
                }
                else
                {
                    request.Cookies = cookies;
                }

                request.Proxy = proxy;
                request.KeepAlive = false;
                request.UserAgent = HttpHelper.ChromeUserAgent();

                return request.Get(address, urlParams).ToBytes();
            }
        }

        #endregion

        #region GetFile

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу. Загружает тело сообщения и сохраняет его в новый файл по указанному пути. Если файл уже существует, то он будет перезаписан.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="path">Путь к файлу, в котором будет сохранено тело сообщения.</param>
        /// <param name="urlParams">Параметры URL-адреса, или значение <see langword="null"/>.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="address"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="path"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="path"/> является пустой строкой, содержит только пробелы или содержит недопустимые символы.</exception>
        /// <exception cref="System.IO.PathTooLongException">Указанный путь, имя файла или и то и другое превышает наибольшую возможную длину, определенную системой. Например, для платформ на основе Windows длина пути не должна превышать 248 знаков, а имена файлов не должны содержать более 260 знаков.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Значение параметра <paramref name="path"/> указывает на несуществующий файл.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">Значение параметра <paramref name="path"/> указывает на недопустимый путь.</exception>
        /// <exception cref="System.IO.IOException">При открытии файла возникла ошибка ввода-вывода.</exception>
        /// <exception cref="System.Security.SecurityException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <exception cref="System.UnauthorizedAccessException">
        /// Операция чтения файла не поддерживается на текущей платформе.
        /// -или-
        /// Значение параметра <paramref name="path"/> определяет каталог.
        /// -или-
        /// Вызывающий оператор не имеет необходимого разрешения.
        /// </exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public static void GetFile(string address, string path, RequestParams urlParams = null, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (var request = new HttpRequest())
            {
                if (cookies == null)
                {
                    request.Cookies = new CookieDictionary();
                }
                else
                {
                    request.Cookies = cookies;
                }

                request.Proxy = proxy;
                request.KeepAlive = false;
                request.UserAgent = HttpHelper.ChromeUserAgent();

                request.Get(address, urlParams).ToFile(path);
            }
        }

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу. Загружает тело сообщения и сохраняет его в новый файл по указанному пути. Если файл уже существует, то он будет перезаписан.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="path">Путь к файлу, в котором будет сохранено тело сообщения.</param>
        /// <param name="urlParams">Параметры URL-адреса, или значение <see langword="null"/>.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="address"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="path"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="path"/> является пустой строкой, содержит только пробелы или содержит недопустимые символы.</exception>
        /// <exception cref="System.IO.PathTooLongException">Указанный путь, имя файла или и то и другое превышает наибольшую возможную длину, определенную системой. Например, для платформ на основе Windows длина пути не должна превышать 248 знаков, а имена файлов не должны содержать более 260 знаков.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Значение параметра <paramref name="path"/> указывает на несуществующий файл.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">Значение параметра <paramref name="path"/> указывает на недопустимый путь.</exception>
        /// <exception cref="System.IO.IOException">При открытии файла возникла ошибка ввода-вывода.</exception>
        /// <exception cref="System.Security.SecurityException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <exception cref="System.UnauthorizedAccessException">
        /// Операция чтения файла не поддерживается на текущей платформе.
        /// -или-
        /// Значение параметра <paramref name="path"/> определяет каталог.
        /// -или-
        /// Вызывающий оператор не имеет необходимого разрешения.
        /// </exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public static void GetFile(Uri address, string path, RequestParams urlParams = null, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (var request = new HttpRequest())
            {
                if (cookies == null)
                {
                    request.Cookies = new CookieDictionary();
                }
                else
                {
                    request.Cookies = cookies;
                }

                request.Proxy = proxy;
                request.KeepAlive = false;
                request.UserAgent = HttpHelper.ChromeUserAgent();

                request.Get(address, urlParams).ToFile(path);
            }
        }

        #endregion

        #region Post

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу. Загружает тело сообщения и возвращает его в виде строки.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="reqParams">Параметры запроса, отправляемые HTTP-серверу.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <returns>Если тело сообщения отсутствует, то будет возвращена пустая строка.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="address"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="reqParams"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public static string Post(string address, RequestParams reqParams, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (var request = new HttpRequest())
            {
                if (cookies == null)
                {
                    request.Cookies = new CookieDictionary();
                }
                else
                {
                    request.Cookies = cookies;
                }

                request.Proxy = proxy;
                request.KeepAlive = false;
                request.UserAgent = HttpHelper.ChromeUserAgent();

                return request.Post(address, reqParams).ToString();
            }
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу. Загружает тело сообщения и возвращает его в виде строки.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="reqParams">Параметры запроса, отправляемые HTTP-серверу.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <returns>Если тело сообщения отсутствует, то будет возвращена пустая строка.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="address"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="reqParams"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public static string Post(Uri address, RequestParams reqParams, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (var request = new HttpRequest())
            {
                if (cookies == null)
                {
                    request.Cookies = new CookieDictionary();
                }
                else
                {
                    request.Cookies = cookies;
                }

                request.Proxy = proxy;
                request.KeepAlive = false;
                request.UserAgent = HttpHelper.ChromeUserAgent();

                return request.Post(address, reqParams).ToString();
            }
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу. Загружает тело сообщения и возвращает его в виде строки.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="content">Контент, отправляемый HTTP-серверу.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <returns>Если тело сообщения отсутствует, то будет возвращена пустая строка.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="address"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="content"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public static string Post(string address, HttpContent content, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (var request = new HttpRequest())
            {
                if (cookies == null)
                {
                    request.Cookies = new CookieDictionary();
                }
                else
                {
                    request.Cookies = cookies;
                }

                request.Proxy = proxy;
                request.KeepAlive = false;
                request.UserAgent = HttpHelper.ChromeUserAgent();

                return request.Post(address, content).ToString();
            }
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу. Загружает тело сообщения и возвращает его в виде строки.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="content">Контент, отправляемый HTTP-серверу.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <returns>Если тело сообщения отсутствует, то будет возвращена пустая строка.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="address"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="content"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        public static string Post(Uri address, HttpContent content, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (var request = new HttpRequest())
            {
                if (cookies == null)
                {
                    request.Cookies = new CookieDictionary();
                }
                else
                {
                    request.Cookies = cookies;
                }

                request.Proxy = proxy;
                request.KeepAlive = false;
                request.UserAgent = HttpHelper.ChromeUserAgent();

                return request.Post(address, content).ToString();
            }
        }

        #endregion

        #endregion
    }
}