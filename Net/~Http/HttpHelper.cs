using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using Microsoft.Win32;

namespace xNet.Net
{
    /// <summary>
    /// Представляет статический класс, предназначенный для помощи в работе с HTTP-протоколом.
    /// </summary>
    public static class HttpHelper
    {
        #region HttpHeaders

        internal static readonly Dictionary<HttpHeader, string> HttpHeaders = new Dictionary<HttpHeader, string>()
        {
            { HttpHeader.Accept, "Accept" },
            { HttpHeader.AcceptCharset, "Accept-Charset" },
            { HttpHeader.AcceptLanguage, "Accept-Language" },
            { HttpHeader.AcceptDatetime, "Accept-Datetime" },
            { HttpHeader.CacheControl, "Cache-Control" },
            { HttpHeader.ContentType, "Content-Type" },
            { HttpHeader.Date, "Date" },
            { HttpHeader.Expect, "Expect" },
            { HttpHeader.From, "From" },
            { HttpHeader.IfMatch, "If-Match" },
            { HttpHeader.IfModifiedSince, "If-Modified-Since" },
            { HttpHeader.IfNoneMatch, "If-None-Match" },
            { HttpHeader.IfRange, "If-Range" },
            { HttpHeader.IfUnmodifiedSince, "If-Unmodified-Since" },
            { HttpHeader.MaxForwards, "Max-Forwards" },
            { HttpHeader.Pragma, "Pragma" },
            { HttpHeader.Range, "Range" },
            { HttpHeader.Referer, "Referer" },
            { HttpHeader.Upgrade, "Upgrade" },
            { HttpHeader.UserAgent, "User-Agent" },
            { HttpHeader.Via, "Via" },
            { HttpHeader.Warning, "Warning" },
            { HttpHeader.DNT, "DNT" },
            { HttpHeader.AccessControlAllowOrigin, "Access-Control-Allow-Origin" },
            { HttpHeader.AcceptRanges, "Accept-Ranges" },
            { HttpHeader.Age, "Age" },
            { HttpHeader.Allow, "Allow" },
            { HttpHeader.ContentEncoding, "Content-Encoding" },
            { HttpHeader.ContentLanguage, "Content-Language" },
            { HttpHeader.ContentLength, "Content-Length" },
            { HttpHeader.ContentLocation, "Content-Location" },
            { HttpHeader.ContentMD5, "Content-MD5" },
            { HttpHeader.ContentDisposition, "Content-Disposition" },
            { HttpHeader.ContentRange, "Content-Range" },
            { HttpHeader.ETag, "ETag" },
            { HttpHeader.Expires, "Expires" },
            { HttpHeader.LastModified, "Last-Modified" },
            { HttpHeader.Link, "Link" },
            { HttpHeader.Location, "Location" },
            { HttpHeader.P3P, "P3P" },
            { HttpHeader.Refresh, "Refresh" },
            { HttpHeader.RetryAfter, "Retry-After" },
            { HttpHeader.Server, "Server" },
            { HttpHeader.TransferEncoding, "Transfer-Encoding" }
        };

        #endregion


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
        /// Преобразует параметры в строку запроса.
        /// </summary>
        /// <param name="parameters">Параметры.</param>
        /// <param name="dontEscape">Указывает, нужно ли кодировать значения параметров.</param>
        /// <returns>Строка запроса.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="parameters"/> равно <see langword="null"/>.</exception>
        public static string ToQueryString(IEnumerable<KeyValuePair<string, string>> parameters, bool dontEscape)
        {
            #region Проверка параметров

            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }

            #endregion

            var queryBuilder = new StringBuilder();

            foreach (var param in parameters)
            {
                queryBuilder.Append(param.Key);
                queryBuilder.Append('=');

                if (dontEscape)
                {
                    queryBuilder.Append(param.Value);
                }
                else
                {
                    queryBuilder.Append(Uri.EscapeDataString(param.Value));
                }

                queryBuilder.Append('&');
            }

            if (queryBuilder.Length != 0)
            {
                // Удаляем '&' в конце.
                queryBuilder.Remove(queryBuilder.Length - 1, 1);
            }

            return queryBuilder.ToString();
        }

        /// <summary>
        /// Определяет и возвращает MIME-тип на основе расширения файла.
        /// </summary>
        /// <param name="extension">Расширение файла.</param>
        /// <returns>MIME-тип.</returns>
        public static string DetermineMediaType(string extension)
        {
            string mediaType = "application/octet-stream";

            try
            {
                using (var regKey = Registry.ClassesRoot.OpenSubKey(extension))
                {
                    if (regKey != null)
                    {
                        object keyValue = regKey.GetValue("Content Type");

                        if (keyValue != null)
                        {
                            mediaType = keyValue.ToString();
                        }
                    }
                }
            }
            #region Catch's

            catch (IOException) { }
            catch (ObjectDisposedException) { }
            catch (UnauthorizedAccessException) { }
            catch (SecurityException) { }

            #endregion

            return mediaType;
        }

        #region User Agent

        /// <summary>
        /// Генерирует случайный User-Agent от браузера IE.
        /// </summary>
        /// <returns>Случайный User-Agent от браузера IE.</returns>
        public static string IEUserAgent()
        {
            string version = null;
            string mozillaVersion = null;

            #region Генерация случайной версии

            switch (Rand.Next(3))
            {
                case 0:
                    version = "9.0";
                    mozillaVersion = "5.0";
                    break;

                case 1:
                    version = "10.0";
                    mozillaVersion = "5.0";
                    break;

                case 2:
                    version = "10.6";
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
                "Mozilla/{0} (compatible; MSIE {1}; {2}; Trident/5.0; {3})",
                mozillaVersion, version, windowsVersion, otherParams);
        }

        /// <summary>
        /// Генерирует случайный User-Agent от браузера Opera.
        /// </summary>
        /// <returns>Случайный User-Agent от браузера Opera.</returns>
        public static string OperaUserAgent()
        {
            string version = null;
            string presto = null;

            #region Генерация случайной версии

            switch (Rand.Next(6))
            {
                case 0:
                    version = "11.00";
                    presto = "2.7.62";
                    break;

                case 1:
                    version = "11.10";
                    presto = "2.8.131";
                    break;

                case 2:
                    version = "11.50";
                    presto = "2.9.168";
                    break;

                case 3:
                    version = "11.62";
                    presto = "2.10.229";
                    break;

                case 4:
                    version = "12.00";
                    presto = "2.10.181";
                    break;

                case 5:
                    version = "12.02";
                    presto = "2.10.289";
                    break;
            }

            #endregion

            return string.Format(
                "Opera/9.80 ({0}); U) Presto/{1} Version/{2}",
                RandomWindowsVersion(), presto, version);
        }

        /// <summary>
        /// Генерирует случайный User-Agent от браузера Chrome.
        /// </summary>
        /// <returns>Случайный User-Agent от браузера Chrome.</returns>
        public static string ChromeUserAgent()
        {
            string version = null;
            string safari = null;

            #region Генерация случайной версии

            switch (Rand.Next(5))
            {
                case 0:
                    version = "22.0.1229.79";
                    safari = "537.4";
                    break;

                case 1:
                    version = "23.0.1271.17";
                    safari = "537.11";
                    break;

                case 2:
                    version = "23.0.1271.6";
                    safari = "537.11";
                    break;

                case 3:
                    version = "24.0.1290.1";
                    safari = "537.13";
                    break;

                case 4:
                    version = "24.0.1309.0";
                    safari = "537.17";
                    break;
            }

            #endregion

            return string.Format(
                "Mozilla/5.0 ({0}) AppleWebKit/537.1 (KHTML, like Gecko) Chrome/{1} Safari/{2}",
                RandomWindowsVersion(), version, safari);
        }

        /// <summary>
        /// Генерирует случайный User-Agent от браузера Firefox.
        /// </summary>
        /// <returns>Случайный User-Agent от браузера Firefox.</returns>
        public static string FirefoxUserAgent()
        {
            string version = null;
            string revision = null;
            
            #region Генерация случайной версии

            switch (Rand.Next(3))
            {
                case 0:
                    version = "15.0.1";
                    revision = "15.0";
                    break;

                case 1:
                    version = "15.0.2";
                    revision = "15.0";
                    break;

                case 2:
                    version = "16.0.1";
                    revision = "16.0.1";
                    break;
            }

            #endregion

            return string.Format(
                "Mozilla/5.0 (Windows; U; {0}; rv:{1}) Gecko/20121011 Firefox/{2}",
                RandomWindowsVersion(), revision, version);
        }

        /// <summary>
        /// Генерирует случайный User-Agent от мобильного браузера Opera.
        /// </summary>
        /// <returns>Случайный User-Agent от мобильного браузера Opera.</returns>
        public static string OperaMiniUserAgent()
        {
            string os = null;
            string miniVersion = null;
            string version = null;
            string presto = null;

            #region Генерация случайной версии

            switch (Rand.Next(3))
            {
                case 0:
                    os = "iOS";
                    miniVersion = "7.0.73345";
                    version = "11.62";
                    presto = "2.10.229";
                    break;

                case 1:
                    os = "J2ME/MIDP";
                    miniVersion = "7.1.23511";
                    version = "12.00";
                    presto = "2.10.181";
                    break;

                case 2:
                    os = "Android";
                    miniVersion = "7.5.54678";
                    version = "12.02";
                    presto = "2.10.289";
                    break;
            }

            #endregion

            return string.Format(
                "Opera/9.80 ({0}; Opera Mini/{1}/28.2555; U; ru) Presto/{2} Version/{3}",
                os, miniVersion, presto, version);
        }

        #endregion

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
                    windowsVersion += "5.1"; // Windows XP.
                    break;

                case 1:
                    windowsVersion += "6.0"; // Windows Vista.
                    break;

                case 2:
                    windowsVersion += "6.1"; // Windows 7.
                    break;
            }

            if (Rand.NextDouble() < 0.2)
            {
                windowsVersion += "; WOW64"; // 64-битная версия.
            }

            return windowsVersion;
        }

        #endregion
    }
}