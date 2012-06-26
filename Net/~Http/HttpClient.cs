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

        /// <summary>
        /// Возвращает или задаёт глобальное значение HTTP-заголовка 'User-Agent'.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        public static string GlobalUserAgent
        {
            get
            {
                return HttpRequest.GlobalUserAgent;
            }
            set
            {
                HttpRequest.GlobalUserAgent = value;
            }
        }

        #endregion


        #region Статические методы (открытые)

        #region Get

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public static void Get(string address, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            Get(address, null, cookies, proxy);
        }

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="reqParams">Параметры запроса, отправляемые HTTP-серверу.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public static void Get(string address, StringDictionary reqParams, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (HttpRequest request = new HttpRequest())
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

                if (string.IsNullOrEmpty(HttpRequest.GlobalUserAgent))
                {
                    request.UserAgent = HttpHelper.RandomUserAgent();
                }

                request.Get(address, reqParams).None();
            }
        }

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public static void Get(Uri address, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            Get(address, null, cookies, proxy);
        }

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="reqParams">Параметры запроса, отправляемые HTTP-серверу.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public static void Get(Uri address, StringDictionary reqParams, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (HttpRequest request = new HttpRequest())
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

                if (string.IsNullOrEmpty(HttpRequest.GlobalUserAgent))
                {
                    request.UserAgent = HttpHelper.RandomUserAgent();
                }

                request.Get(address, reqParams).None();
            }
        }

        #endregion

        #region GetText

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу. Загружает тело сообщения и возвращает его в виде байтов.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <returns>Если тело сообщения отсутствует, то будет возвращена пустая строка.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public static string GetText(string address, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            return GetText(address, null, cookies, proxy);
        }

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу. Загружает тело сообщения и возвращает его в виде байтов.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="reqParams">Параметры запроса, отправляемые HTTP-серверу.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <returns>Если тело сообщения отсутствует, то будет возвращена пустая строка.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public static string GetText(string address, StringDictionary reqParams, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (HttpRequest request = new HttpRequest())
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

                if (string.IsNullOrEmpty(HttpRequest.GlobalUserAgent))
                {
                    request.UserAgent = HttpHelper.RandomUserAgent();
                }

                return request.Get(address, reqParams).ToText();
            }
        }

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу. Загружает тело сообщения и возвращает его в виде байтов.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <returns>Если тело сообщения отсутствует, то будет возвращена пустая строка.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public static string GetText(Uri address, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            return GetText(address, null, cookies, proxy);
        }

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу. Загружает тело сообщения и возвращает его в виде байтов.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="reqParams">Параметры запроса, отправляемые HTTP-серверу.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <returns>Если тело сообщения отсутствует, то будет возвращена пустая строка.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public static string GetText(Uri address, StringDictionary reqParams, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (HttpRequest request = new HttpRequest())
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

                if (string.IsNullOrEmpty(HttpRequest.GlobalUserAgent))
                {
                    request.UserAgent = HttpHelper.RandomUserAgent();
                }

                return request.Get(address, reqParams).ToText();
            }
        }

        #endregion

        #region GetBytes

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу. Загружает тело сообщения и возвращает его в виде текста.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <returns>Если тело сообщения отсутствует, то будет возвращён пустой массив байтов.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public static byte[] GetBytes(string address, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            return GetBytes(address, null, cookies, proxy);
        }

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу. Загружает тело сообщения и возвращает его в виде текста.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="reqParams">Параметры запроса, отправляемые HTTP-серверу.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <returns>Если тело сообщения отсутствует, то будет возвращён пустой массив байтов.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public static byte[] GetBytes(string address, StringDictionary reqParams, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (HttpRequest request = new HttpRequest())
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

                if (string.IsNullOrEmpty(HttpRequest.GlobalUserAgent))
                {
                    request.UserAgent = HttpHelper.RandomUserAgent();
                }

                return request.Get(address, reqParams).ToBytes();
            }
        }

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу. Загружает тело сообщения и возвращает его в виде текста.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <returns>Если тело сообщения отсутствует, то будет возвращён пустой массив байтов.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public static byte[] GetBytes(Uri address, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            return GetBytes(address, null, cookies, proxy);
        }

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу. Загружает тело сообщения и возвращает его в виде текста.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="reqParams">Параметры запроса, отправляемые HTTP-серверу.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <returns>Если тело сообщения отсутствует, то будет возвращён пустой массив байтов.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public static byte[] GetBytes(Uri address, StringDictionary reqParams, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (HttpRequest request = new HttpRequest())
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

                if (string.IsNullOrEmpty(HttpRequest.GlobalUserAgent))
                {
                    request.UserAgent = HttpHelper.RandomUserAgent();
                }

                return request.Get(address, reqParams).ToBytes();
            }
        }

        #endregion

        #region GetFile

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу. Загружает тело сообщения и сохраняет его в новый файл по указанному пути. Если файл уже существует, то он будет перезаписан.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="path">Путь к файлу, в котором будет сохранено тело сообщения.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
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
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public static void GetFile(string address, string path, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            GetFile(address, path, null, cookies, proxy);
        }

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу. Загружает тело сообщения и сохраняет его в новый файл по указанному пути. Если файл уже существует, то он будет перезаписан.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="path">Путь к файлу, в котором будет сохранено тело сообщения.</param>
        /// <param name="reqParams">Параметры запроса, отправляемые HTTP-серверу.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
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
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public static void GetFile(string address, string path, StringDictionary reqParams, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (HttpRequest request = new HttpRequest())
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

                if (string.IsNullOrEmpty(HttpRequest.GlobalUserAgent))
                {
                    request.UserAgent = HttpHelper.RandomUserAgent();
                }

                request.Get(address, reqParams).ToFile(path);
            }
        }

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу. Загружает тело сообщения и сохраняет его в новый файл по указанному пути. Если файл уже существует, то он будет перезаписан.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="path">Путь к файлу, в котором будет сохранено тело сообщения.</param>
        /// <param name="reqParams">Параметры запроса, отправляемые HTTP-серверу.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
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
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public static void GetFile(Uri address, string path, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            GetFile(address, path, null, cookies, proxy);
        }

        /// <summary>
        /// Отправляет GET-запрос HTTP-серверу. Загружает тело сообщения и сохраняет его в новый файл по указанному пути. Если файл уже существует, то он будет перезаписан.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="path">Путь к файлу, в котором будет сохранено тело сообщения.</param>
        /// <param name="reqParams">Параметры запроса, отправляемые HTTP-серверу.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
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
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public static void GetFile(Uri address, string path, StringDictionary reqParams, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (HttpRequest request = new HttpRequest())
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

                if (string.IsNullOrEmpty(HttpRequest.GlobalUserAgent))
                {
                    request.UserAgent = HttpHelper.RandomUserAgent();
                }

                request.Get(address, reqParams).ToFile(path);
            }
        }

        #endregion

        #region Head

        /// <summary>
        /// Отправляет HEAD-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public static void Head(string address, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            Head(address, null, cookies, proxy);
        }

        /// <summary>
        /// Отправляет HEAD-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="reqParams">Параметры запроса, отправляемые HTTP-серверу.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public static void Head(string address, StringDictionary reqParams, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (HttpRequest request = new HttpRequest())
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

                if (string.IsNullOrEmpty(HttpRequest.GlobalUserAgent))
                {
                    request.UserAgent = HttpHelper.RandomUserAgent();
                }

                request.Head(address, reqParams).None();
            }
        }

        /// <summary>
        /// Отправляет HEAD-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public static void Head(Uri address, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            Head(address, null, cookies, proxy);
        }

        /// <summary>
        /// Отправляет HEAD-запрос HTTP-серверу.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="reqParams">Параметры запроса, отправляемые HTTP-серверу.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public static void Head(Uri address, StringDictionary reqParams, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (HttpRequest request = new HttpRequest())
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

                if (string.IsNullOrEmpty(HttpRequest.GlobalUserAgent))
                {
                    request.UserAgent = HttpHelper.RandomUserAgent();
                }

                request.Head(address, reqParams).None();
            }
        }

        #endregion

        #region Post

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу. Загружает тело сообщения и возвращает его в виде текста.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="reqParams">Параметры запроса, отправляемые HTTP-серверу.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <returns>Если тело сообщения отсутствует, то будет возвращена пустая строка.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="reqParams"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        /// <remarks>Если значение заголовка 'Content-Type' не задано, то отправляется значение 'Content-Type: application/x-www-form-urlencoded'.</remarks>
        public static string Post(string address, StringDictionary reqParams, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (HttpRequest request = new HttpRequest())
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

                if (string.IsNullOrEmpty(HttpRequest.GlobalUserAgent))
                {
                    request.UserAgent = HttpHelper.RandomUserAgent();
                }

                return request.Post(address, reqParams).ToText();
            }
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу. Загружает тело сообщения и возвращает его в виде текста.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="messageBody">Тело сообщения, отправляемое HTTP-серверу.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <returns>Если тело сообщения отсутствует, то будет возвращена пустая строка.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="messageBody"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="messageBody"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        /// <remarks>Если значение заголовка 'Content-Type' не задано, то отправляется значение 'Content-Type: application/x-www-form-urlencoded'.</remarks>
        public static string Post(string address, string messageBody, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (HttpRequest request = new HttpRequest())
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

                if (string.IsNullOrEmpty(HttpRequest.GlobalUserAgent))
                {
                    request.UserAgent = HttpHelper.RandomUserAgent();
                }

                return request.Post(address, messageBody).ToText();
            }
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу. Загружает тело сообщения и возвращает его в виде текста.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="messageBody">Тело сообщения, отправляемое HTTP-серверу.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <returns>Если тело сообщения отсутствует, то будет возвращена пустая строка.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="messageBody"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        /// <remarks>Если значение заголовка 'Content-Type' не задано, то отправляется значение 'Content-Type: application/x-www-form-urlencoded'.</remarks>
        public static string Post(string address, byte[] messageBody, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (HttpRequest request = new HttpRequest())
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

                if (string.IsNullOrEmpty(HttpRequest.GlobalUserAgent))
                {
                    request.UserAgent = HttpHelper.RandomUserAgent();
                }

                return request.Post(address, messageBody).ToText();
            }
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу. Загружает тело сообщения и возвращает его в виде текста.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="reqParams">Параметры запроса, отправляемые HTTP-серверу.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <returns>Если тело сообщения отсутствует, то будет возвращена пустая строка.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="reqParams"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        /// <remarks>Если значение заголовка 'Content-Type' не задано, то отправляется значение 'Content-Type: application/x-www-form-urlencoded'.</remarks>
        public static string Post(Uri address, StringDictionary reqParams, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (HttpRequest request = new HttpRequest())
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

                if (string.IsNullOrEmpty(HttpRequest.GlobalUserAgent))
                {
                    request.UserAgent = HttpHelper.RandomUserAgent();
                }

                return request.Post(address, reqParams).ToText();
            }
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу. Загружает тело сообщения и возвращает его в виде текста.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="messageBody">Тело сообщения, отправляемое HTTP-серверу.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <returns>Если тело сообщения отсутствует, то будет возвращена пустая строка.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="messageBody"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="messageBody"/> является пустой строкой.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        /// <remarks>Если значение заголовка 'Content-Type' не задано, то отправляется значение 'Content-Type: application/x-www-form-urlencoded'.</remarks>
        public static string Post(Uri address, string messageBody, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (HttpRequest request = new HttpRequest())
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

                if (string.IsNullOrEmpty(HttpRequest.GlobalUserAgent))
                {
                    request.UserAgent = HttpHelper.RandomUserAgent();
                }

                return request.Post(address, messageBody).ToText();
            }
        }

        /// <summary>
        /// Отправляет POST-запрос HTTP-серверу. Загружает тело сообщения и возвращает его в виде текста.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="messageBody">Тело сообщения, отправляемое HTTP-серверу.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <returns>Если тело сообщения отсутствует, то будет возвращена пустая строка.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="messageBody"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        /// <remarks>Если значение заголовка 'Content-Type' не задано, то отправляется значение 'Content-Type: application/x-www-form-urlencoded'.</remarks>
        public static string Post(Uri address, byte[] messageBody, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (HttpRequest request = new HttpRequest())
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

                if (string.IsNullOrEmpty(HttpRequest.GlobalUserAgent))
                {
                    request.UserAgent = HttpHelper.RandomUserAgent();
                }

                return request.Post(address, messageBody).ToText();
            }
        }

        #region MultipartFormData

        /// <summary>
        /// Отправляет POST-запрос с Multipart/form данными. Загружает тело сообщения и возвращает его в виде текста.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="multipartData">Multipart/form данные, отправляемые HTTP-серверу.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <returns>Если тело сообщения отсутствует, то будет возвращена пустая строка.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> является пустой строкой.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="multipartData"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public static string Post(string address, MultipartDataCollection multipartData, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (HttpRequest request = new HttpRequest())
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

                if (string.IsNullOrEmpty(HttpRequest.GlobalUserAgent))
                {
                    request.UserAgent = HttpHelper.RandomUserAgent();
                }

                return request.Post(address, multipartData).ToText();
            }
        }

        /// <summary>
        /// Отправляет POST-запрос с Multipart/form данными. Загружает тело сообщения и возвращает его в виде текста.
        /// </summary>
        /// <param name="address">Адрес интернет-ресурса.</param>
        /// <param name="multipartData">Multipart/form данные, отправляемые HTTP-серверу.</param>
        /// <param name="cookies">Кукисы, отправляемые HTTP-серверу, или значение <see langword="null"/>.</param>
        /// <param name="proxy">Прокси-клиент, используемый для запроса, или значение <see langword="null"/>.</param>
        /// <returns>Если тело сообщения отсутствует, то будет возвращена пустая строка.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="address"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="address"/> не является абсолютным URI.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="multipartData"/> равно <see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">Ошибка при работе с HTTP-протоколом.</exception>
        /// <exception cref="xNet.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public static string Post(Uri address, MultipartDataCollection multipartData, CookieDictionary cookies = null, ProxyClient proxy = null)
        {
            using (HttpRequest request = new HttpRequest())
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

                if (string.IsNullOrEmpty(HttpRequest.GlobalUserAgent))
                {
                    request.UserAgent = HttpHelper.RandomUserAgent();
                }

                return request.Post(address, multipartData).ToText();
            }
        }

        #endregion

        #endregion

        #endregion
    }
}