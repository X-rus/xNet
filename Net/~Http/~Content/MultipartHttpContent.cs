using System.Text;

namespace xNet.Net
{
    /// <summary>
    /// Представляет контент в виде Multipart данных, которые отправляются HTTP-серверу.
    /// </summary>
    public class MultipartHttpContent : HttpContent
    {
        #region Поля (закрытые)

        private long _multipartDataLength = -1;
        private MultipartDataCollection _multipartData;

        #endregion


        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="StreamHttpContent"/>.
        /// </summary>
        /// <param name="multipartData">Multipart данные контента.</param>
        public MultipartHttpContent(MultipartDataCollection multipartData)
        {
            _multipartData = multipartData;
        }


        #region Методы (открытые)

        /// <summary>
        /// Возвращает MIME-тип контента.
        /// </summary>
        /// <returns>MIME-тип контента.</returns>
        override public string GetType()
        {
            return _multipartData.GenerateContentType();
        }

        /// <summary>
        /// Возвращает длину контента в байтах.
        /// </summary>
        /// <returns>Длина контента в байтах.</returns>
        override public long GetLength()
        {
            if (_multipartDataLength == -1)
            {
                _multipartDataLength = _multipartData.CalculateLength(_request.CharacterSet ?? Encoding.Default);
            }

            return _multipartDataLength;
        }

        /// <summary>
        /// Вызывается, когда требуется отправить данные.
        /// </summary>
        override public void Send()
        {
            _multipartData.Send(_writeBytesCallback, _request.CharacterSet ?? Encoding.Default);
        }

        #endregion
    }
}