using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Extreme.Net
{
    /// <summary>
    /// Представляет цепочку из различных прокси-серверов.
    /// </summary>
    public class ChainProxyClient : ProxyClient
    {
        #region Статические поля (закрытые)

        [ThreadStatic] private static Random _rand;
        private static Random Rand
        {
            get
            {
                if (_rand == null)
                    _rand = new Random();
                return _rand;
            }
        }

        #endregion


        #region Поля (закрытые)

        private List<ProxyClient> _proxies = new List<ProxyClient>();

        #endregion


        #region Свойства (открытые)

        /// <summary>
        /// Возвращает или задает значение, указывающие, нужно ли перемешивать список цепочки прокси-серверов, перед созданием нового подключения.
        /// </summary>
        public bool EnableShuffle { get; set; }

        /// <summary>
        /// Возвращает список цепочки прокси-серверов.
        /// </summary>
        public List<ProxyClient> Proxies
        {
            get
            {
                return _proxies;
            }
        }

        #region Переопределённые

        /// <summary>
        /// Данное свойство не поддерживается.
        /// </summary>
        /// <exception cref="System.NotSupportedException">При любом использовании этого свойства.</exception>
        override public string Host
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Данное свойство не поддерживается.
        /// </summary>
        /// <exception cref="System.NotSupportedException">При любом использовании этого свойства.</exception>
        override public int Port
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Данное свойство не поддерживается.
        /// </summary>
        /// <exception cref="System.NotSupportedException">При любом использовании этого свойства.</exception>
        override public string Username
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Данное свойство не поддерживается.
        /// </summary>
        /// <exception cref="System.NotSupportedException">При любом использовании этого свойства.</exception>
        override public string Password
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Данное свойство не поддерживается.
        /// </summary>
        /// <exception cref="System.NotSupportedException">При любом использовании этого свойства.</exception>
        override public int ConnectTimeout
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Данное свойство не поддерживается.
        /// </summary>
        /// <exception cref="System.NotSupportedException">При любом использовании этого свойства.</exception>
        override public int ReadWriteTimeout
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        #endregion

        #endregion


        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ChainProxyClient"/>.
        /// </summary>
        /// <param name="enableShuffle">Указывает, нужно ли перемешивать список цепочки прокси-серверов, перед созданием нового подключения.</param>
        public ChainProxyClient(bool enableShuffle = false)
            : base(ProxyType.Chain)
        {
            EnableShuffle = enableShuffle;
        }


        #region Методы (открытые)

        /// <summary>
        /// Создаёт соединение с сервером через цепочку прокси-серверов.
        /// </summary>
        /// <param name="destinationHost">Хост сервера, с которым нужно связаться через прокси-сервер.</param>
        /// <param name="destinationPort">Порт сервера, с которым нужно связаться через прокси-сервер.</param>
        /// <param name="tcpClient">Соединение, через которое нужно работать, или значение <see langword="null"/>.</param>
        /// <returns>Соединение с сервером через цепочку прокси-серверов.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// Количество прокси-серверов равно 0.
        /// -или-
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
        /// <exception cref="Extreme.Net.Net.ProxyException">Ошибка при работе с прокси-сервером.</exception>
        public override TcpClient CreateConnection(string destinationHost, int destinationPort, TcpClient tcpClient = null)
        {
            #region Проверка состояния

            if (_proxies.Count == 0)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_ChainProxyClient_NotProxies);
            }

            #endregion

            List<ProxyClient> proxies;

            if (EnableShuffle)
            {
                proxies = _proxies.ToList();

                // Перемешиваем прокси.
                for (int i = 0; i < proxies.Count; i++)
                {
                    int randI = Rand.Next(proxies.Count);

                    ProxyClient proxy = proxies[i];
                    proxies[i] = proxies[randI];
                    proxies[randI] = proxy;
                }
            }
            else
            {
                proxies = _proxies;
            }

            int length = proxies.Count - 1;
            TcpClient curTcpClient = tcpClient;

            for (int i = 0; i < length; i++)
            {
                curTcpClient = proxies[i].CreateConnection(
                    proxies[i + 1].Host, proxies[i + 1].Port, curTcpClient);
            }

            curTcpClient = proxies[length].CreateConnection(
                destinationHost, destinationPort, curTcpClient);

            return curTcpClient;
        }

        /// <summary>
        /// Формирует список строк вида - хост:порт, представляющую адрес прокси-сервера.
        /// </summary>
        /// <returns>Список строк вида - хост:порт, представляющая адрес прокси-сервера.</returns>
        public override string ToString()
        {
            var strBuilder = new StringBuilder();

            foreach (var proxy in _proxies)
            {
                strBuilder.AppendLine(proxy.ToString());
            }

            return strBuilder.ToString();
        }

        /// <summary>
        /// Формирует список строк вида - хост:порт:имя_пользователя:пароль. Последние два параметра добавляются, если они заданы.
        /// </summary>
        /// <returns>Список строк вида - хост:порт:имя_пользователя:пароль.</returns>
        public virtual string ToExtendedString()
        {
            var strBuilder = new StringBuilder();

            foreach (var proxy in _proxies)
            {
                strBuilder.AppendLine(proxy.ToExtendedString());
            }

            return strBuilder.ToString();
        }

        #region Добавление прокси-серверов

        /// <summary>
        /// Добавляет в цепочку новый прокси-клиент.
        /// </summary>
        /// <param name="proxy">Добавляемый прокси-клиент.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="proxy"/> равно <see langword="null"/>.</exception>
        public void AddProxy(ProxyClient proxy)
        {
            #region Проверка параметров

            if (proxy == null)
            {
                throw new ArgumentNullException("proxy");
            }

            #endregion

            _proxies.Add(proxy);
        }

        /// <summary>
        /// Добавляет в цепочку новый HTTP-прокси клиент.
        /// </summary>
        /// <param name="proxyAddress">Строка вида - хост:порт:имя_пользователя:пароль. Три последних параметра являются необязательными.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="proxyAddress"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="proxyAddress"/> является пустой строкой.</exception>
        /// <exception cref="System.FormatException">Формат порта является неправильным.</exception>
        public void AddHttpProxy(string proxyAddress)
        {
            _proxies.Add(HttpProxyClient.Parse(proxyAddress));
        }

        /// <summary>
        /// Добавляет в цепочку новый Socks4-прокси клиент.
        /// </summary>
        /// <param name="proxyAddress">Строка вида - хост:порт:имя_пользователя:пароль. Три последних параметра являются необязательными.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="proxyAddress"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="proxyAddress"/> является пустой строкой.</exception>
        /// <exception cref="System.FormatException">Формат порта является неправильным.</exception>
        public void AddSocks4Proxy(string proxyAddress)
        {
            _proxies.Add(Socks4ProxyClient.Parse(proxyAddress));
        }

        /// <summary>
        /// Добавляет в цепочку новый Socks4a-прокси клиент.
        /// </summary>
        /// <param name="proxyAddress">Строка вида - хост:порт:имя_пользователя:пароль. Три последних параметра являются необязательными.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="proxyAddress"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="proxyAddress"/> является пустой строкой.</exception>
        /// <exception cref="System.FormatException">Формат порта является неправильным.</exception>
        public void AddSocks4aProxy(string proxyAddress)
        {
            _proxies.Add(Socks4aProxyClient.Parse(proxyAddress));
        }

        /// <summary>
        /// Добавляет в цепочку новый Socks5-прокси клиент.
        /// </summary>
        /// <param name="proxyAddress">Строка вида - хост:порт:имя_пользователя:пароль. Три последних параметра являются необязательными.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="proxyAddress"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="proxyAddress"/> является пустой строкой.</exception>
        /// <exception cref="System.FormatException">Формат порта является неправильным.</exception>
        public void AddSocks5Proxy(string proxyAddress)
        {
            _proxies.Add(Socks5ProxyClient.Parse(proxyAddress));
        }

        #endregion

        #endregion
    }
}
