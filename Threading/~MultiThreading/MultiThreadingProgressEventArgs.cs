using System;

namespace xNet.Threading
{
    /// <summary>
    /// Предоставляет данные для события, сообщающим о прогрессе выполнения асинхронной операции.
    /// </summary>
    /// <typeparam name="T">Тип данных передаваемого значения.</typeparam>
    public sealed class MultiThreadingProgressEventArgs<T> : EventArgs
    {
        /// <summary>
        /// Возвращает передаваемое значение.
        /// </summary>
        public T Result { get; private set; }


        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="MultiThreadingProgressEventArgs&lt;T&gt;"/> заданным значением.
        /// </summary>
        /// <param name="result">Передаваемое значение.</param>
        public MultiThreadingProgressEventArgs(T result)
        {
            Result = result;
        }
    }
}