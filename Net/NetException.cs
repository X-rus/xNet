using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace xNet.Net
{
    /// <summary>
    /// Исключение, которое выбрасывается, в случае возникновения ошибки при работе с сетью.
    /// </summary>
    public class NetException : Exception, ISerializable
    {
        #region Конструкторы (открытые)

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="NetException"/>.
        /// </summary>
        public NetException() : this(Resources.NetException_Default) { }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="NetException"/> заданным сообщением об ошибке.
        /// </summary>
        /// <param name="message">Сообщение об ошибке с объяснением причины исключения.</param>
        /// <param name="innerException">Исключение, вызвавшее текущие исключение, или значение <see langword="null"/>.</param>
        public NetException(string message, Exception innerException = null)
            : base(message, innerException) { }

        #endregion


        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="NetException"/> заданными экземплярами <see cref="SerializationInfo"/> и <see cref="StreamingContext"/>.
        /// </summary>
        /// <param name="serializationInfo">Экземпляр класса <see cref="SerializationInfo"/>, который содержит сведения, требуемые для сериализации нового экземпляра класса <see cref="NetException"/>.</param>
        /// <param name="streamingContext">Экземпляр класса <see cref="StreamingContext"/>, содержащий источник сериализованного потока, связанного с новым экземпляром класса <see cref="NetException"/>.</param>
        protected NetException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext) { }


        /// <summary>
        /// Заполняет экземпляр <see cref="SerializationInfo"/> данными, необходимыми для сериализации исключения <see cref="NetException"/>.
        /// </summary>
        /// <param name="serializationInfo">Данные о сериализации, <see cref="SerializationInfo"/>, которые должны использоваться.</param>
        /// <param name="streamingContext">Данные о сериализации, <see cref="StreamingContext"/>, которые должны использоваться.</param>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);
        }
    }
}