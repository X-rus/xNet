using System;

namespace xNet.Threading
{
    /// <summary>
    /// Предоставляет данные для события, сообщающим о прогрессе выполнения асинхронной операции.
    /// </summary>
    public sealed class MultiThreadingProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Возвращает передаваемое значение.
        /// </summary>
        public object Result { get; private set; }


        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="MultiThreadingProgressEventArgs"/> заданным значением.
        /// </summary>
        /// <param name="result">Передаваемое значение.</param>
        public MultiThreadingProgressEventArgs(object result)
        {
            Result = result;
        }
    }
}