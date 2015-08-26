using System;
using System.Collections.Generic;

namespace xNet
{
    /// <summary>
    /// Представляет коллекцию строк, представляющих параметры запроса.
    /// </summary>
    public class RequestParams : List<KeyValuePair<string,string>>
    {
        /// <summary>
        /// Задаёт новый параметр запроса.
        /// </summary>
        /// <param name="paramName">Название параметра запроса.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="paramName"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="paramName"/> является пустой строкой.</exception>
        public object this[string paramName]
        {
            set
            {
                #region Проверка параметра

                if (paramName == null)
                {
                    throw new ArgumentNullException("paramName");
                }

                if (paramName.Length == 0)
                {
                    throw ExceptionHelper.EmptyString("paramName");
                }

                #endregion

                string str = (value == null ? string.Empty : value.ToString());

                Add(new KeyValuePair<string, string>(paramName, str));
            }
        }
    }
}