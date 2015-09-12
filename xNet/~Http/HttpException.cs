using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace xNet
{
    /// <summary>
    /// Исключение, которое выбрасывается, в случае возникновения ошибки при работе с HTTP-протоколом.
    /// </summary>
    [Serializable]
    public sealed class HttpException : NetException
    {
        #region Свойства (открытые)

        /// <summary>
        /// Возвращает состояние исключения.
        /// </summary>
        public HttpExceptionStatus Status { get; internal set; }

        /// <summary>
        /// Возвращает код состояния ответа от HTTP-сервера.
        /// </summary>
        public HttpStatusCode HttpStatusCode { get; private set; }

        #endregion


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
        /// <param name="statusCode">Код состояния ответа от HTTP-сервера.</param>
        /// <param name="innerException">Исключение, вызвавшее текущие исключение, или значение <see langword="null"/>.</param>
        public HttpException(string message, HttpExceptionStatus status,
            HttpStatusCode httpStatusCode = HttpStatusCode.None, Exception innerException = null)
            : base(message, innerException)
        {
            Status = status;
            HttpStatusCode = httpStatusCode;
        }

        #endregion


        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="HttpException"/> заданными экземплярами <see cref="SerializationInfo"/> и <see cref="StreamingContext"/>.
        /// </summary>
        /// <param name="serializationInfo">Экземпляр класса <see cref="SerializationInfo"/>, который содержит сведения, требуемые для сериализации нового экземпляра класса <see cref="HttpException"/>.</param>
        /// <param name="streamingContext">Экземпляр класса <see cref="StreamingContext"/>, содержащий источник сериализованного потока, связанного с новым экземпляром класса <see cref="HttpException"/>.</param>
        protected HttpException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
            if (serializationInfo != null)
            {
                Status = (HttpExceptionStatus)serializationInfo.GetInt32("Status");
                HttpStatusCode = (HttpStatusCode)serializationInfo.GetInt32("HttpStatusCode");
            }
        }


        /// <summary>
        /// Заполняет экземпляр <see cref="SerializationInfo"/> данными, необходимыми для сериализации исключения <see cref="HttpException"/>.
        /// </summary>
        /// <param name="serializationInfo">Данные о сериализации, <see cref="SerializationInfo"/>, которые должны использоваться.</param>
        /// <param name="streamingContext">Данные о сериализации, <see cref="StreamingContext"/>, которые должны использоваться.</param>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);

            if (serializationInfo != null)
            {
                serializationInfo.AddValue("Status", (int)Status);
                serializationInfo.AddValue("HttpStatusCode", (int)HttpStatusCode);
            }
        }
    }
}