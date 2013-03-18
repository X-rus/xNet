using System;
using System.Text;
using xNet.Collections;

namespace xNet.Net
{
    public class FormUrlEncodedContent : BytesContent
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="FormUrlEncodedContent"/>.
        /// </summary>
        /// <param name="content">Содержимое контента.</param>
        /// <param name="dontEscape">Указывает, нужно ли кодировать значения параметров.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="content"/> равно <see langword="null"/>.</exception>
        /// <remarks>По умолчанию используется тип контента - 'application/x-www-form-urlencoded'.</remarks>
        public FormUrlEncodedContent(StringDictionary content, bool dontEscape = false)
        {
            #region Проверка параметров

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            #endregion

            string str = HttpHelper.ToQueryString(content, dontEscape);

            _content = Encoding.ASCII.GetBytes(str);
            _offset = 0;
            _count = _content.Length;

            _contentType = "application/x-www-form-urlencoded";
        }
    }
}