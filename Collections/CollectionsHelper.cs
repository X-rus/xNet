using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace xNet.Collections
{
    /// <summary>
    /// Представляет статический класс, предназначенный для помощи в работе с коллекциями.
    /// </summary>
    public static class CollectionsHelper
    {
        /// <summary>
        /// Проверяет, пустая ли последовательность.
        /// </summary>
        /// <typeparam name="T">Тип элементов последовательности <paramref name="source"/>.</typeparam>
        /// <param name="source">Последовательность, проверяемая на наличие элементов.</param>
        /// <returns>Значение <see langword="true"/>, если последовательность пуста, иначе значение <see langword="false"/>.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="source"/> равно <see langword="null"/>.</exception>
        public static bool IsEmpty<T>(this IEnumerable<T> source)
        {
            #region Проверка параметров

            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            #endregion

            return !source.Any();
        }

        /// <summary>
        /// Преобразует последовательность в строку, где строковые значения всех записей соединяются, а между ними устанавливается заданный разделитель.
        /// </summary>
        /// <typeparam name="T">Тип элементов последовательности <paramref name="source"/>.</typeparam>
        /// <param name="source">Преобразуемая последовательность.</param>
        /// <param name="separator">Разделитель между значениями последовательности.</param>
        /// <param name="letterCase">Регистр букв, применяемый к значениям последовательности.</param>
        /// <returns>Строковое представление последовательности.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="source"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="separator"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="separator"/> является пустой строкой.</exception>
        public static string ToString<T>(this IEnumerable<T> source,
            string separator, LetterCase letterCase = LetterCase.None)
        {
            #region Проверка параметров

            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (separator == null)
            {
                throw new ArgumentNullException("separator");
            }

            if (separator.Length == 0)
            {
                throw ExceptionHelper.EmptyString("separator");
            }

            #endregion

            var strBuilder = new StringBuilder();

            foreach (T value in source)
            {
                string valueInStr = value.ToString();

                switch (letterCase)
                {
                    case LetterCase.Upper:
                        valueInStr = valueInStr.ToUpper();
                        break;

                    case LetterCase.Lower:
                        valueInStr = valueInStr.ToLower();
                        break;
                }

                strBuilder.Append(valueInStr);
                strBuilder.Append(separator);
            }

            if (strBuilder.Length != 0)
            {
                strBuilder.Remove(strBuilder.Length - separator.Length, separator.Length);
            }

            return strBuilder.ToString();
        }
    }
}