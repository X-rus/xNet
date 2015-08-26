
namespace xNet
{
    /// <summary>
    /// Определяет состояния для класса <see cref="HttpException"/>.
    /// </summary>
    public enum HttpExceptionStatus
    {
        /// <summary>
        /// Произошла другая ошибка.
        /// </summary>
        Other,
        /// <summary>
        /// Ответ, принятый от сервера, был завершен, но указал на ошибку на уровне протокола. Допустим, сервер вернул ошибку 404 или Not Found ("не найдено").
        /// </summary>
        ProtocolError,
        /// <summary>
        /// Не удалось соединиться с HTTP-сервером.
        /// </summary>
        ConnectFailure,
        /// <summary>
        /// Не удалось отправить запрос HTTP-серверу.
        /// </summary>
        SendFailure,
        /// <summary>
        /// Не удалось загрузить ответ от HTTP-сервера.
        /// </summary>
        ReceiveFailure
    }
}