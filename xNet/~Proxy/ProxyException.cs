using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace xNet
{
    /// <summary>
    /// »сключение, которое выбрасываетс€, в случае возникновени€ ошибки при работе с прокси.
    /// </summary>
    public sealed class ProxyException : NetException, ISerializable
    {
        /// <summary>
        /// ¬озвращает прокси-клиент, в котором произошла ошибка.
        /// </summary>
        public ProxyClient ProxyClient { get; private set; }

        
        #region  онструкторы (открытые)

        /// <summary>
        /// »нициализирует новый экземпл€р класса <see cref="ProxyException"/>.
        /// </summary>
        public ProxyException() : this(Resources.ProxyException_Default) { }

        /// <summary>
        /// »нициализирует новый экземпл€р класса <see cref="ProxyException"/> заданным сообщением об ошибке.
        /// </summary>
        /// <param name="message">—ообщение об ошибке с объ€снением причины исключени€.</param>
        /// <param name="innerException">»сключение, вызвавшее текущие исключение, или значение <see langword="null"/>.</param>
        public ProxyException(string message, Exception innerException = null)
            : base(message, innerException) { }

        /// <summary>
        /// »нициализирует новый экземпл€р класса <see cref="xNet.Net.ProxyException"/> заданным сообщением об ошибке и прокси-клиентом.
        /// </summary>
        /// <param name="message">—ообщение об ошибке с объ€снением причины исключени€.</param>
        /// <param name="proxyClient">ѕрокси-клиент, в котором произошла ошибка.</param>
        /// <param name="innerException">»сключение, вызвавшее текущие исключение, или значение <see langword="null"/>.</param>
        public ProxyException(string message, ProxyClient proxyClient, Exception innerException = null)
            : base(message, innerException)
        {
            ProxyClient = proxyClient;
        }

        #endregion


        /// <summary>
        /// »нициализирует новый экземпл€р класса <see cref="ProxyException"/> заданными экземпл€рами <see cref="SerializationInfo"/> и <see cref="StreamingContext"/>.
        /// </summary>
        /// <param name="serializationInfo">Ёкземпл€р класса <see cref="SerializationInfo"/>, который содержит сведени€, требуемые дл€ сериализации нового экземпл€ра класса <see cref="ProxyException"/>.</param>
        /// <param name="streamingContext">Ёкземпл€р класса <see cref="StreamingContext"/>, содержащий источник сериализованного потока, св€занного с новым экземпл€ром класса <see cref="ProxyException"/>.</param>
        protected ProxyException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext) { }


        /// <summary>
        /// «аполн€ет экземпл€р <see cref="SerializationInfo"/> данными, необходимыми дл€ сериализации исключени€ <see cref="ProxyException"/>.
        /// </summary>
        /// <param name="serializationInfo">ƒанные о сериализации, <see cref="SerializationInfo"/>, которые должны использоватьс€.</param>
        /// <param name="streamingContext">ƒанные о сериализации, <see cref="StreamingContext"/>, которые должны использоватьс€.</param>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);
        }
    }
}