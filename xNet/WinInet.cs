using System;
using System.IO;
using System.Security;
using Microsoft.Win32;

namespace xNet
{
    /// <summary>
    /// Представляет класс для взаимодействия с настройками сети операционной системы Windows.
    /// </summary>
    public static class WinInet
    {
        private const string PathToInternetOptions = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";


        #region Статические свойства (открытые)

        /// <summary>
        /// Возвращает значение, указывающие, установлено ли подключение к интернету.
        /// </summary>
        public static bool InternetConnected
        {
            get
            {
                SafeNativeMethods.InternetConnectionState state = 0;
                return SafeNativeMethods.InternetGetConnectedState(ref state, 0);
            }
        }

        /// <summary>
        /// Возвращает значение, указывающие, установлено ли подключение к интернету через модем.
        /// </summary>
        public static bool InternetThroughModem
        {
            get
            {
                return EqualConnectedState(
                    SafeNativeMethods.InternetConnectionState.INTERNET_CONNECTION_MODEM);
            }
        }

        /// <summary>
        /// Возвращает значение, указывающие, установлено ли подключение к интернету через локальную сеть.
        /// </summary>
        public static bool InternetThroughLan
        {
            get
            {
                return EqualConnectedState(
                    SafeNativeMethods.InternetConnectionState.INTERNET_CONNECTION_LAN);
            }
        }

        /// <summary>
        /// Возвращает значение, указывающие, установлено ли подключение к интернету через прокси-сервер.
        /// </summary>
        public static bool InternetThroughProxy
        {
            get
            {
                return EqualConnectedState(
                    SafeNativeMethods.InternetConnectionState.INTERNET_CONNECTION_PROXY);
            }
        }

        /// <summary>
        /// Возвращает значение, указывающее, используется ли прокси-сервер в Internet Explorer.
        /// </summary>
        public static bool IEProxyEnable
        {
            get
            {
                try
                {
                    return GetIEProxyEnable();
                }
                catch (IOException) { return false; }
                catch (SecurityException) { return false; }
                catch (ObjectDisposedException) { return false; }
                catch (UnauthorizedAccessException) { return false; }
            }
            set
            {
                try
                {
                    SetIEProxyEnable(value);
                }
                catch (IOException) { }
                catch (SecurityException) { }
                catch (ObjectDisposedException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        /// <summary>
        /// Возвращает или задаёт прокси-сервер Internet Explorer'а.
        /// </summary>
        /// <value>Если прокси-сервер Internet Explorer'а не задан или ошибочен, то будет возвращён <see langword="null"/>. Если задать <see langword="null"/>, то прокси-сервер Internet Explorer'а будет стёрт.</value>
        public static HttpProxyClient IEProxy
        {
            get
            {
                string proxy;

                try
                {
                    proxy = GetIEProxy();
                }
                catch (IOException) { return null; }
                catch (SecurityException) { return null; }
                catch (ObjectDisposedException) { return null; }
                catch (UnauthorizedAccessException) { return null; }

                HttpProxyClient ieProxy;
                HttpProxyClient.TryParse(proxy, out ieProxy);

                return ieProxy;
            }
            set
            {
                try
                {
                    if (value != null)
                    {
                        SetIEProxy(value.ToString());
                    }
                    else
                    {
                        SetIEProxy(string.Empty);
                    }
                }
                catch (SecurityException) { }
                catch (ObjectDisposedException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        #endregion


        #region Статические методы (открытые)

        /// <summary>
        /// Возвращает значение, указывающее, используется ли прокси-сервер в Internet Explorer. Значение берется из реестра.
        /// </summary>
        /// <returns>Значение, указывающее, используется ли прокси-сервер в Internet Explorer.</returns>
        /// <exception cref="System.Security.SecurityException">У пользователя отсутствуют разрешения, необходимые для чтения раздела реестра.</exception>
        /// <exception cref="System.ObjectDisposedException">Объект <see cref="Microsoft.Win32.RegistryKey"/>, для которого вызывается этот метод, закрыт (доступ к закрытым разделам невозможен).</exception>
        /// <exception cref="System.UnauthorizedAccessException">У пользователя отсутствуют необходимые права доступа к реестру.</exception>
        /// <exception cref="System.IO.IOException">Раздел <see cref="Microsoft.Win32.RegistryKey"/>, содержащий заданное значение, был помечен для удаления.</exception>
        public static bool GetIEProxyEnable()
        {
            using (RegistryKey regKey = Registry.CurrentUser.OpenSubKey(PathToInternetOptions))
            {
                object value = regKey.GetValue("ProxyEnable");

                if (value == null)
                {
                    return false;
                }
                else
                {
                    return ((int)value == 0) ? false : true;
                }
            }
        }

        /// <summary>
        /// Задаёт значение, указывающее, используется ли прокси-сервер в Internet Explorer. Значение задаётся в реестре.
        /// </summary>
        /// <param name="enabled">Указывает, используется ли прокси-сервер в Internet Explorer.</param>
        /// <exception cref="System.Security.SecurityException">У пользователя отсутствуют разрешения, необходимые для создания или открытия раздела реестра.</exception>
        /// <exception cref="System.ObjectDisposedException">Объект <see cref="Microsoft.Win32.RegistryKey"/>, для которого вызывается этот метод, закрыт (доступ к закрытым разделам невозможен).</exception>
        /// <exception cref="System.UnauthorizedAccessException">Запись в объект <see cref="Microsoft.Win32.RegistryKey"/> невозможна, например, он не может быть открыт как раздел, доступный для записи, или у пользователя нет необходимых прав доступа.</exception>
        public static void SetIEProxyEnable(bool enabled)
        {
            using (RegistryKey regKey = Registry.CurrentUser.CreateSubKey(PathToInternetOptions))
            {
                regKey.SetValue("ProxyEnable", (enabled) ? 1 : 0);
            }
        }

        /// <summary>
        /// Возвращает значение прокси-сервера Internet Explorer'а. Значение берется из реестра.
        /// </summary>
        /// <returns>Значение прокси-сервера Internet Explorer'а, иначе пустая строка.</returns>
        /// <exception cref="System.Security.SecurityException">У пользователя отсутствуют разрешения, необходимые для чтения раздела реестра.</exception>
        /// <exception cref="System.ObjectDisposedException">Объект <see cref="Microsoft.Win32.RegistryKey"/>, для которого вызывается этот метод, закрыт (доступ к закрытым разделам невозможен).</exception>
        /// <exception cref="System.UnauthorizedAccessException">У пользователя отсутствуют необходимые права доступа к реестру.</exception>
        /// <exception cref="System.IO.IOException">Раздел <see cref="Microsoft.Win32.RegistryKey"/>, содержащий заданное значение, был помечен для удаления.</exception>
        public static string GetIEProxy()
        {
            using (RegistryKey regKey = Registry.CurrentUser.OpenSubKey(PathToInternetOptions))
            {
                return (regKey.GetValue("ProxyServer") as string) ?? string.Empty;
            }
        }

        /// <summary>
        /// Задаёт значение прокси-сервера Internet Explorer'а. Значение задаётся в реестре.
        /// </summary>
        /// <param name="host">Хост прокси-сервера.</param>
        /// <param name="port">Порт прокси-сервера.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="host"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="host"/> является пустой строкой.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра <paramref name="port"/> меньше 1 или больше 65535.</exception>
        /// <exception cref="System.Security.SecurityException">У пользователя отсутствуют разрешения, необходимые для создания или открытия раздела реестра.</exception>
        /// <exception cref="System.ObjectDisposedException">Объект <see cref="Microsoft.Win32.RegistryKey"/>, для которого вызывается этот метод, закрыт (доступ к закрытым разделам невозможен).</exception>
        /// <exception cref="System.UnauthorizedAccessException">Запись в объект <see cref="Microsoft.Win32.RegistryKey"/> невозможна, например, он не может быть открыт как раздел, доступный для записи, или у пользователя нет необходимых прав доступа.</exception>
        public static void SetIEProxy(string host, int port)
        {
            #region Проверка параметров

            if (host == null)
            {
                throw new ArgumentNullException("host");
            }

            if (host.Length == 0)
            {
                throw ExceptionHelper.EmptyString("host");
            }

            if (!ExceptionHelper.ValidateTcpPort(port))
            {
                throw ExceptionHelper.WrongTcpPort("port");
            }

            #endregion

            SetIEProxy(host + ":" + port.ToString());
        }

        /// <summary>
        /// Задаёт значение прокси-сервера Internet Explorer'а. Значение задаётся в реестре.
        /// </summary>
        /// <param name="hostAndPort">Хост и порт прокси-сервера, в формате - хост:порт, либо только хост.</param>
        /// <exception cref="System.Security.SecurityException">У пользователя отсутствуют разрешения, необходимые для создания или открытия раздела реестра.</exception>
        /// <exception cref="System.ObjectDisposedException">Объект <see cref="Microsoft.Win32.RegistryKey"/>, для которого вызывается этот метод, закрыт (доступ к закрытым разделам невозможен).</exception>
        /// <exception cref="System.UnauthorizedAccessException">Запись в объект <see cref="Microsoft.Win32.RegistryKey"/> невозможна, например, он не может быть открыт как раздел, доступный для записи, или у пользователя нет необходимых прав доступа.</exception>
        public static void SetIEProxy(string hostAndPort)
        {
            using (RegistryKey regKey = Registry.CurrentUser.CreateSubKey(PathToInternetOptions))
            {
                regKey.SetValue("ProxyServer", hostAndPort ?? string.Empty);
            }
        }

        #endregion


        private static bool EqualConnectedState(SafeNativeMethods.InternetConnectionState expected)
        {
            SafeNativeMethods.InternetConnectionState state = 0;
            SafeNativeMethods.InternetGetConnectedState(ref state, 0);

            return (state & expected) != 0;
        }
    }
}