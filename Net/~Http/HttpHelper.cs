using System;
using System.Text;

namespace xNet.Net
{
    /// <summary>
    /// Представляет статический класс, предназначенный для помощи в работе с HTTP-протоколом.
    /// </summary>
    public static class HttpHelper
    {
        /// <summary>
        /// Обозначает новую строку в HTTP-протоколе.
        /// </summary>
        public const string NewLine = "\r\n";


        #region Статические методы (открытые)

        /// <summary>
        /// Кодирует строку для надёжной передачи HTTP-серверу.
        /// </summary>
        /// <param name="str">Строка, которая будет закодирована.</param>
        /// <param name="encoding">Кодировка, применяемая для преобразования данных в последовательность байтов. Если значение параметра равно <see langword="null"/>, то будет использовано значение <see cref="System.Text.Encoding.UTF8"/>.</param>
        /// <returns>Закодированная строка.</returns>
        public static string UrlEncode(string str, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            encoding = encoding ?? Encoding.UTF8;

            byte[] bytes = encoding.GetBytes(str);

            int spaceCount = 0;
            int otherCharCount = 0;

            #region Подсчёт символов

            for (int i = 0; i < bytes.Length; i++)
            {
                char c = (char)bytes[i];

                if (c == ' ')
                {
                    ++spaceCount;
                }
                else if (!IsUrlSafeChar(c))
                {
                    ++otherCharCount;
                }
            }

            #endregion

            // Если в строке не присутствуют символы, которые нужно закодировать.
            if ((spaceCount == 0) && (otherCharCount == 0))
            {
                return str;
            }

            int bufferIndex = 0;
            byte[] buffer = new byte[bytes.Length + (otherCharCount * 2)];

            for (int i = 0; i < bytes.Length; i++)
            {
                char c = (char)bytes[i];

                if (IsUrlSafeChar(c))
                {
                    buffer[bufferIndex++] = bytes[i];
                }
                else if (c == ' ')
                {
                    buffer[bufferIndex++] = (byte)'+';
                }
                else
                {
                    buffer[bufferIndex++] = (byte)'%';
                    buffer[bufferIndex++] = (byte)IntToHex((bytes[i] >> 4) & 15);
                    buffer[bufferIndex++] = (byte)IntToHex(bytes[i] & 15);
                }
            }

            return Encoding.ASCII.GetString(buffer);
        }

        /// <summary>
        /// Генерирует случайный User-Agent от браузера: IE, Opera, Chrome или Firefox.
        /// </summary>
        /// <returns>Случайный User-Agent.</returns>
        public static string RandomUserAgent()
        {
            switch (Rand.Next(4))
            {
                case 0:
                    return RandomIEUserAgent();

                case 1:
                    return RandomOperaUserAgent();

                case 2:
                    return RandomChromeUserAgent();

                case 3:
                    return RandomFirefoxUserAgent();
            }

            return string.Empty;
        }

        /// <summary>
        /// Генерирует случайный User-Agent от браузера IE.
        /// </summary>
        /// <returns>Случайный User-Agent от браузера IE.</returns>
        public static string RandomIEUserAgent()
        {
            string version = null;
            string mozillaVersion = null;

            #region Генерация случайной версии

            switch (Rand.Next(2))
            {
                case 0:
                    version = "8.0";
                    mozillaVersion = "4.0";
                    break;

                case 1:
                    version = "9.0";
                    mozillaVersion = "5.0";
                    break;
            }

            #endregion

            string otherParams = null;
            string windowsVersion = RandomWindowsVersion();

            #region Генерация дополнительных параметров

            switch (windowsVersion)
            {
                case "Windows NT 5.1":
                    otherParams = ".NET CLR 2.0.50727; .NET CLR 3.5.30729";
                    break;

                case "Windows NT 6.0":
                    otherParams = ".NET CLR 2.0.50727; Media Center PC 5.0; .NET CLR 3.5.30729";
                    break;

                case "Windows NT 6.1":
                    otherParams = ".NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; .NET4.0C; .NET4.0E";
                    break;
            }

            #endregion

            return string.Format(
                "Mozilla/{0} (compatible; MSIE {1}; {2}; Trident/4.0; {3})",
                mozillaVersion, version, windowsVersion, otherParams);
        }

        /// <summary>
        /// Генерирует случайный User-Agent от браузера Opera.
        /// </summary>
        /// <returns>Случайный User-Agent от браузера Opera.</returns>
        public static string RandomOperaUserAgent()
        {
                string version = null;
                string presto = null;

                #region Генерация случайной версии

                switch (Rand.Next(6))
                {
                    case 0:
                        version = "10.00";
                        presto = "2.2.15";
                        break;

                    case 1:
                        version = "10.54";
                        presto = "2.5.25";
                        break;

                    case 2:
                        version = "10.63";
                        presto = "2.6.30";
                        break;

                    case 3:
                        version = "11.00";
                        presto = "2.7.62";
                        break;

                    case 4:
                        version = "11.10";
                        presto = "2.8.131";
                        break;

                    case 5:
                        version = "11.50";
                        presto = "2.9.168";
                        break;
                }

                #endregion

                return string.Format(
                    "Opera/9.80 ({0}); U; en) Presto/{1} Version/{2}",
                    RandomWindowsVersion(), presto, version);
        }

        /// <summary>
        /// Генерирует случайный User-Agent от браузера Chrome.
        /// </summary>
        /// <returns>Случайный User-Agent от браузера Chrome.</returns>
        public static string RandomChromeUserAgent()
        {
                string version = null;

                #region Генерация случайной версии

                switch (Rand.Next(3))
                {
                    case 0:
                        version = "13.0.782";
                        break;

                    case 1:
                        version = "14.0.835";
                        break;

                    case 2:
                        version = "15.0.874";
                        break;
                }

                #endregion

                return string.Format(
                    "Mozilla/5.0 ({0}) AppleWebKit/535.1 (KHTML, like Gecko) Chrome/{1}.{2} Safari/535.1",
                    RandomWindowsVersion(), version, Rand.Next(1000));
        }

        /// <summary>
        /// Генерирует случайный User-Agent от браузера Firefox.
        /// </summary>
        /// <returns>Случайный User-Agent от браузера Firefox.</returns>
        public static string RandomFirefoxUserAgent()
        {
            string version = null;
            string revision = null;

            #region Генерация случайной версии

            switch (Rand.Next(5))
            {
                case 0:
                    version = "4.0.1";
                    revision = "2.0.1";
                    break;

                case 1:
                    version = "5.0";
                    revision = "5.0";
                    break;

                case 2:
                    version = "5.0.1";
                    revision = "5.0.1";
                    break;

                case 3:
                    version = "6.0";
                    revision = "6.0";
                    break;

                case 4:
                    version = "6.0.1";
                    revision = "6.0.1";
                    break;
            }

            #endregion

            return string.Format(
                "Mozilla/5.0 (Windows; U; {0}; en; rv:{1}) Gecko/20100101 Firefox/{2}",
                RandomWindowsVersion(), revision, version);
        }

        #endregion


        #region Статические методы (закрытые)

        private static bool IsUrlSafeChar(char c)
        {
            if ((((c >= 'a') && (c <= 'z')) ||
                ((c >= 'A') && (c <= 'Z'))) ||
                ((c >= '0') && (c <= '9')))
            {
                return true;
            }

            switch (c)
            {
                case '(':
                case ')':
                case '*':
                case '-':
                case '.':
                case '_':
                case '!':
                    return true;
            }

            return false;
        }

        private static char IntToHex(int i)
        {
            if (i <= 9)
            {
                return (char)(i + 48);
            }

            return (char)((i - 10) + 65);
        }

        private static string RandomWindowsVersion()
        {
            string windowsVersion = "Windows NT ";

            switch (Rand.Next(3))
            {
                case 0:
                    windowsVersion += "5.1";
                    break;

                case 1:
                    windowsVersion += "6.0";
                    break;

                case 2:
                    windowsVersion += "6.1";
                    break;
            }

            return windowsVersion;
        }

        #endregion
    }
}