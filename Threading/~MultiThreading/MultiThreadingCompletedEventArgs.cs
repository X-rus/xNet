using System;
using System.Collections.ObjectModel;
using xNet.Collections;

namespace xNet.Threading
{
    /// <summary>
    /// Предоставляет данные для события, сообщающего о завершение выполнения асинхронной операции.
    /// </summary>
    public sealed class MultiThreadingCompletedEventArgs : EventArgs
    {
        #region Свойства (открытые)

        /// <summary>
        /// Возвращает исключения, которые произошли во время выполнения асинхронного действия и которые могли повлечь преждевременное завершение работы.
        /// </summary>
        public ReadOnlyCollection<Exception> Errors { get; private set; }

        /// <summary>
        /// Возвращает значение, указывающие, имеются ли исключения, которые произошли во время выполнения асинхронного действия.
        /// </summary>
        public bool HasErrors
        {
            get
            {
                return !Errors.IsEmpty();
            }
        }

        #endregion


        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="MultiThreadingCompletedEventArgs"/>.
        /// </summary>
        /// <param name="exceptions">Исключения, которые произошли во время выполнения асинхронного действия и которые могли повлечь преждевременное завершение работы.</param>
        public MultiThreadingCompletedEventArgs(Exception[] exceptions)
        {
            Errors = new ReadOnlyCollection<Exception>(exceptions);
        }
    }
}