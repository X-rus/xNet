using System;

namespace xNet
{
    /// <summary>
    /// Представляет статический класс, предназначенный для помощи в работе с битами.
    /// </summary>
    public static class BitHelper
    {
        #region Статические методы (открытые)

        /// <summary>
        /// Инвертирует заданный бит в числе.
        /// </summary>
        /// <param name="number">Число, в котором будет инвертирован бит.</param>
        /// <param name="bit">Индекс инвертируемого бита.</param>
        /// <returns>Число с инвертированным битом.</returns>
        public static int InvertBit(int number, int bit)
        {
            if (GetBit(number, bit))
            {
                number &= ~(1 << bit);
            }
            else
            {
                number |= (1 << bit);
            }

            return number;
        }

        /// <summary>
        /// Инвертирует заданные биты в числе.
        /// </summary>
        /// <param name="number">Число, в котором будут инвертированы биты.</param>
        /// <param name="bits">Список индексов битов, которые будут инвертированы.</param>
        /// <returns>Число с инвертированными битами.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="bits"/> равно <see langword="null"/>.</exception>
        public static int InvertBits(int number, params int[] bits)
        {
            #region Проверка параметров

            if (bits == null)
            {
                throw new ArgumentNullException("bits");
            }

            #endregion

            foreach (int bit in bits)
            {
                number = InvertBit(number, bit);
            }

            return number;
        }

        /// <summary>
        /// Возвращает значение заданного бита числа.
        /// </summary>
        /// <param name="number">Число, из которого будет взято значение заданного бита.</param>
        /// <param name="bit">Индекс бита, значение которого будет возвращено.</param>
        /// <returns>Значение заданного бита числа.</returns>
        public static bool GetBit(int number, int bit)
        {
            return ((number & (1 << bit)) != 0);
        }

        /// <summary>
        /// Устанавливет значение заданного бита числа.
        /// </summary>
        /// <param name="value">Значение устанавливаемого бита.</param>
        /// <param name="number">Число, в котором будет установлено значение заданного бита.</param>
        /// <param name="bit">Индекс бита, значение которого будет установлено.</param>
        /// <returns>Число с заданным битом.</returns>
        public static int SetBit(int number, bool value, int bit)
        {
            if (value)
            {
                number |= (1 << bit);
            }
            else
            {
                number &= ~(1 << bit);
            }

            return number;
        }

        /// <summary>
        /// Устанавливет значения заданных битов в числе.
        /// </summary>
        /// <param name="value">Значение устанавливаемых битов.</param>
        /// <param name="number">Число, в котором будет установлено значение заданных битов.</param>
        /// <param name="bits">Список индексов битов, значение которых будет установлено.</param>
        /// <returns>Число с заданными битами.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="bits"/> равно <see langword="null"/>.</exception>
        public static int SetBits(int number, bool value, params int[] bits)
        {
            #region Проверка параметров

            if (bits == null)
            {
                throw new ArgumentNullException("bits");
            }

            #endregion

            foreach (int bit in bits)
            {
                number = SetBit(number, value, bit);
            }

            return number;
        }

        #endregion
    }
}