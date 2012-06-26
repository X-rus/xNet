using System;

namespace xNet.Threading
{
    /// <summary>
    /// Предоставляет данные для события, используемом в целях протоколирования выполнения асинхронной операции.
    /// </summary>
    public sealed class MultiThreadingLoggingEventArgs : EventArgs
    {
        #region Свойства (открытые)

        /// <summary>
        /// Возвращает сообщение.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Возвращает тип сообщения.
        /// </summary>
        public MessageType MessageType { get; private set; }

        #endregion


        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="MultiThreadingLoggingEventArgs"/> заданным сообщением.
        /// </summary>
        /// <param name="message">Сообщение.</param>
        /// <param name="messageType">Тип сообщения.</param>
        public MultiThreadingLoggingEventArgs(string message, MessageType messageType)
        {
            Message = message;
            MessageType = messageType;
        }
    }
}