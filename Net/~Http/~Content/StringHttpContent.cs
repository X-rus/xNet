using System.Text;

namespace xNet.Net
{
    /// <summary>
    /// Представляет контент в виде строки, который отправляется HTTP-серверу.
    /// </summary>
    public class StringHttpContent : HttpContent
    {
        private byte[] _data;


        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="StringHttpContent"/>.
        /// </summary>
        /// <param name="data">Строка представляющая контент.</param>
        public StringHttpContent(string data)
        {
            _data = Encoding.ASCII.GetBytes(data);
        }


        #region Методы (открытые)

        /// <summary>
        /// Возвращает MIME-тип контента.
        /// </summary>
        /// <returns>MIME-тип контента.</returns>
        override public string GetType()
        {
            return "application/x-www-form-urlencoded";
        }

        /// <summary>
        /// Возвращает длину контента в байтах.
        /// </summary>
        /// <returns>Длина контента в байтах.</returns>
        override public long GetLength()
        {
            return _data.Length;
        }

        /// <summary>
        /// Вызывается, когда требуется отправить данные.
        /// </summary>
        override public void Send()
        {
            _writeBytesCallback(_data, _data.Length);
        }

        #endregion
    }
}