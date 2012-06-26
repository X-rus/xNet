using System.Collections.Generic;
using System.Text;
using xNet.Collections;

namespace xNet.Net
{
    /// <summary>
    /// Представляет коллекцию HTTP-кукиксов.
    /// </summary>
    public class CookieDictionary : StringDictionary
    {
        /// <summary>
        /// Возвращает или задает значение, указывающие, закрыты ли кукисы для редактирования
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="false"/>.</value>
        public bool IsLocked { get; set; }


        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="CookieDictionary"/>.
        /// </summary>
        /// <param name="isLocked">Указывает, закрыты ли кукисы для редактирования.</param>
        public CookieDictionary(bool isLocked = false)
        {
            IsLocked = isLocked;
        }


        internal string ToString()
        {
            StringBuilder strBuilder = new StringBuilder();        

            foreach (KeyValuePair<string, string> cookie in this)
            {
                strBuilder.AppendFormat("{0}={1}; ", cookie.Key, cookie.Value);
            }

            if (strBuilder.Length > 0)
            {
                strBuilder.Remove(strBuilder.Length - 2, 2);
            }

            return strBuilder.ToString();
        }
    }
}