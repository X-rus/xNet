using System;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace xNet.Net
{
    /// <summary>
    /// Исключение, которое выбрасывается, в случае возникновения ошибки при работе с HTTP-протоколом.
    /// </summary>
    public sealed class HttpException : NetException, ISerializable
    {
        /// <summary>
        /// Возвращает код состояния ответа.
        /// </summary>
        public HttpStatusCode StatusCode { get; private set; }


        internal bool EmptyMessageBody { get; set; }


        #region Конструкторы (открытые)

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="HttpException"/>.
        /// </summary>
        public HttpException() : this(Resources.HttpException_Default) { }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="HttpException"/> заданным сообщением об ошибке.
        /// </summary>
        /// <param name="message">Сообщение об ошибке с объяснением причины исключения.</param>
        /// <param name="innerException">Исключение, вызвавшее текущие исключение, или значение <see langword="null"/>.</param>
        public HttpException(string message, Exception innerException = null)
            : base(message, innerException) { }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="HttpException"/> заданным сообщением об ошибке и кодом состояния ответа.
        /// </summary>
        /// <param name="message">Сообщение об ошибке с объяснением причины исключения.</param>
        /// <param name="statusCode">Код состояния ответа.</param>
        /// <param name="innerException">Исключение, вызвавшее текущие исключение, или значение <see langword="null"/>.</param>
        public HttpException(string message, HttpStatusCode statusCode, Exception innerException = null)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        #endregion


        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="HttpException"/> заданными экземплярами <see cref="SerializationInfo"/> и <see cref="StreamingContext"/>.
        /// </summary>
        /// <param name="serializationInfo">Экземпляр класса <see cref="SerializationInfo"/>, который содержит сведения, требуемые для сериализации нового экземпляра класса <see cref="HttpException"/>.</param>
        /// <param name="streamingContext">Экземпляр класса <see cref="StreamingContext"/>, содержащий источник сериализованного потока, связанного с новым экземпляром класса <see cref="HttpException"/>.</param>
        protected HttpException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext) { }


        /// <summary>
        /// Заполняет экземпляр <see cref="SerializationInfo"/> данными, необходимыми для сериализации исключения <see cref="HttpException"/>.
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