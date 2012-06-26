using System;
using System.Security.Cryptography;
using System.Text;

namespace xNet.Security.Cryptography
{
    /// <summary>
    /// Представляет статический класс, предназначенный для помощи в работе с криптографией.
    /// </summary>
    public static class CryptographyHelper
    {
        #region Статические методы (открытые)

        /// <summary>
        /// Вычесляет MD5 хэш-значение для заданного массива байтов.
        /// </summary>
        /// <param name="data">Входные данные, для которых вычисляется MD5 хэш-значение.</param>
        /// <returns>Вычисляемое MD5 хэш-значение.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="data"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.InvalidOperationException">Политика FIPS-совместимого алгоритма не задействована.</exception>
        public static string GetMd5Hash(byte[] data)
        {
            #region Проверка параметров

            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            #endregion

            if (data.Length == 0)
            {
                return string.Empty;
            }
            
            using (HashAlgorithm hashProvider = new MD5CryptoServiceProvider())
            {
                var strBuilder = new StringBuilder(32);
                byte[] hashData = hashProvider.ComputeHash(data);
 
                for (int i = 0; i < hashData.Length; ++i)
                {
                    strBuilder.Append(hashData[i].ToString("x2"));
                }

                return strBuilder.ToString();
            }
        }

        /// <summary>
        /// Вычесляет MD5 хэш-значение для заданной строки.
        /// </summary>
        /// <param name="data">Входные данные, для которых вычисляется MD5 хэш-значение.</param>
        /// <param name="encoding">Кодировка, применяемая для преобразования данных в последовательность байтов. Если значение параметра равно <see langword="null"/>, то будет использоваться <see cref="System.Text.Encoding.Default"/>.</param>
        /// <returns>Вычисляемое MD5 хэш-значение.</returns>
        /// <exception cref="System.InvalidOperationException">Политика FIPS-совместимого алгоритма не задействована.</exception>
        public static string GetMd5Hash(string data, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(data))
            {
                return string.Empty;
            }

            encoding = encoding ?? Encoding.Default;

            return GetMd5Hash(encoding.GetBytes(data));
        }

        #endregion
    }
}