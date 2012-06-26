using System;
using System.Text;

namespace xNet
{
    /// <summary>
    /// Представляет статический класс, предназначенный для помощи в работе с перечислениями.
    /// </summary>
    public static class EnumHelper
    {
        /// <summary>
        /// Преобразует перечисление в строку, где строковые значения всех записей соединяются, а между ними устанавливается заданный разделитель. Для перечисления должен быть задан атрибут <see cref="System.FlagsAttribute"/>.
        /// </summary>
        /// <param name="enum">Преобразумое перечисление.</param>
        /// <param name="separator">Разделитель между строковыми значениями перечисления.</param>
        /// <param name="letterCase">Регистр букв, применяемый к строковым значениям перечисления.</param>
        /// <returns>Строковое представление перечисления.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="enum"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="separator"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="separator"/> является пустой строкой.</exception>
        public static string ToString(this Enum @enum, string separator, LetterCase letterCase)
        {
            #region Проверка параметров

            if (@enum == null)
            {
                throw new ArgumentNullException("enum");
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
            Array values = Enum.GetValues(@enum.GetType());

            foreach (Enum value in values)
            {
                if (@enum.HasFlag(value))
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
            }

            if (strBuilder.Length != 0)
            {
                strBuilder.Remove(strBuilder.Length - separator.Length, separator.Length);
            }

            return strBuilder.ToString();
        }
    }
}