using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace xNet
{
    /// <summary>
    /// Исключение, которое выбрасывается, в случае возникновения ошибки при работе с прокси.
    /// </summary>
    [Serializable]
    public sealed class ProxyException : NetException
    {
        /// <summary>
        /// Возвращает прокси-клиент, в котором произошла ошибка.
        /// </summary>
        public ProxyClient ProxyClient { get; private set; }

        
        #region Конструкторы (открытые)

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ProxyException"/>.
        /// </summary>
        public ProxyException() : this(Resources.ProxyException_Default) { }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ProxyException"/> заданным сообщением об ошибке.
        /// </summary>
        /// <param name="message">Сообщение об ошибке с объяснением причины исключения.</param>
        /// <param name="innerException">Исключение, вызвавшее текущие исключение, или значение <see langword="null"/>.</param>
        public ProxyException(string message, Exception innerException = null)
            : base(message, innerException) { }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="xNet.Net.ProxyException"/> заданным сообщением об ошибке и прокси-клиентом.
        /// </summary>
        /// <param name="message">Сообщение об ошибке с объяснением причины исключения.</param>
        /// <param name="proxyClient">Прокси-клиент, в котором произошла ошибка.</param>
        /// <param name="innerException">Исключение, вызвавшее текущие исключение, или значение <see langword="null"/>.</param>
        public ProxyException(string message, ProxyClient proxyClient, Exception innerException = null)
            : base(message, innerException)
        {
            ProxyClient = proxyClient;
        }

        #endregion


        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ProxyException"/> заданными экземплярами <see cref="SerializationInfo"/> и <see cref="StreamingContext"/>.
        /// </summary>
        /// <param name="serializationInfo">Экземпляр класса <see cref="SerializationInfo"/>, который содержит сведения, требуемые для сериализации нового экземпляра класса <see cref="ProxyException"/>.</param>
        /// <param name="streamingContext">Экземпляр класса <see cref="StreamingContext"/>, содержащий источник сериализованного потока, связанного с новым экземпляром класса <see cref="ProxyException"/>.</param>
        protected ProxyException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext) { }
    }
}