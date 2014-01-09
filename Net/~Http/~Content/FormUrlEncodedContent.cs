using System;
using System.Collections.Generic;
using System.Text;

namespace xNet.Net
{
    public class FormUrlEncodedContent : BytesContent
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="FormUrlEncodedContent"/>.
        /// </summary>
        /// <param name="content">Содержимое контента в виде параметров запроса.</param>
        /// <param name="dontEscape">Указывает, нужно ли кодировать значения параметров.</param>
        /// <param name="encoding">Кодировка, применяемая для преобразования параметров запроса. Если значение параметра равно <see langword="null"/>, то будет использовано значение <see cref="System.Text.Encoding.UTF8"/>.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="content"/> равно <see langword="null"/>.</exception>
        /// <remarks>По умолчанию используется тип контента - 'application/x-www-form-urlencoded'.</remarks>
        public FormUrlEncodedContent(IEnumerable<KeyValuePair<string, string>> content, bool dontEscape = false, Encoding encoding = null)
        {
            #region Проверка параметров

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            #endregion

            string queryString = HttpHelper.ToPostQueryString(content, dontEscape, encoding);

            _content = Encoding.ASCII.GetBytes(queryString);
            _offset = 0;
            _count = _content.Length;

            _contentType = "application/x-www-form-urlencoded";
        }
    }
}