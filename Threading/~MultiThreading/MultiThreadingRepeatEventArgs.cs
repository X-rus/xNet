using System;

namespace xNet.Threading
{
    /// <summary>
    /// Предоставляет данные для события, сообщающим о завершение одного из повторов выполнения асинхронной операции.
    /// </summary>
    public sealed class MultiThreadingRepeatEventArgs : EventArgs
    {
        /// <summary>
        /// Возвращает число выполненных повторов.
        /// </summary>
        public ulong RepeatCount { get; private set; }


        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="MultiThreadingRepeatEventArgs"/>.
        /// </summary>
        /// <param name="repeatCount">Число выполненных повторов.</param>
        public MultiThreadingRepeatEventArgs(ulong repeatCount)
        {
            RepeatCount = repeatCount;
        }
    }
}